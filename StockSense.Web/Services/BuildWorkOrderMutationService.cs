using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StockSense.Application.DTOs;
using StockSense.Application.Interfaces;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data;

namespace StockSense.Web.Services;

public sealed class BuildWorkOrderMutationService(ApplicationDbContext context, IAdminPinService adminPinService) : IBuildWorkOrderMutationService
{
    public async Task<WorkOrderMutationResult> UpdateStatusAsync(
        int id, UpdateWorkOrderStatusDto request, ClaimsPrincipal actor, CancellationToken cancellationToken = default)
    {
        var build = await context.BuildRequests.FindAsync([id], cancellationToken);
        if (build is null) return new(false, 404, "Build not found.");

        var target = new[] { WorkOrderStatuses.Pending, WorkOrderStatuses.Confirmed, WorkOrderStatuses.Cancelled }
            .SingleOrDefault(value => string.Equals(value, request.Status?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (target is null) return new(false, 400, "Unsupported build status.");
        var isAdmin = actor.IsInRole("Admin");
        AdminPinVerificationResult? approval = null;
        if (!isAdmin && build.Status != WorkOrderStatuses.Pending)
        {
            approval = !string.IsNullOrWhiteSpace(request.AdminUserId)
                ? await adminPinService.VerifyByUserIdAsync(request.AdminUserId, request.AdminPin ?? "", cancellationToken)
                : await adminPinService.VerifyAsync(request.AdminEmail ?? "", request.AdminPin ?? "", cancellationToken);
            if (!approval.Succeeded) return new(false, approval.LockedUntil.HasValue ? 429 : 403, approval.Error ?? "Admin approval failed.");
        }
        var error = WorkOrderRules.ValidateStatusTransition(build.Status, target, isAdmin || approval?.Succeeded == true);
        if (error is not null) return new(false, 409, error);
        var reason = request.Reason?.Trim();
        if (WorkOrderRules.RequiresAdminReason(build.Status, target) && string.IsNullOrWhiteSpace(reason))
            return new(false, 400, "A reason is required for this admin action.");
        if (reason?.Length > 500) return new(false, 400, "The reason cannot exceed 500 characters.");

        if (target == WorkOrderStatuses.Pending && build.TransactionId.HasValue)
        {
            var transaction = await context.Transactions.Include(value => value.Items)
                .SingleOrDefaultAsync(value => value.Id == build.TransactionId.Value, cancellationToken);
            if (transaction is not null) RestoreStock(transaction);
            if (transaction is not null) transaction.IsVoided = true;
            build.TransactionId = null;
            build.CompletedAt = null;
        }

        var previous = build.Status;
        build.Status = target;
        AddAudit(actor, id, "StatusChanged", previous, target, reason, approval);
        await context.SaveChangesAsync(cancellationToken);
        return new(true, 200, "Status updated.");
    }

    public async Task<WorkOrderMutationResult> UpdatePartsAsync(
        int id, UpdateBuildPartsDto request, ClaimsPrincipal actor, CancellationToken cancellationToken = default)
    {
        var productIds = request.ProductIds;
        var build = await context.BuildRequests.FindAsync([id], cancellationToken);
        if (build is null) return new(false, 404, "Build not found.");
        if (build.Status is WorkOrderStatuses.Completed or WorkOrderStatuses.Cancelled)
            return new(false, 409, "Cannot modify parts on a completed or cancelled build.");
        AdminPinVerificationResult? approval = null;
        if (build.Status != WorkOrderStatuses.Pending && !actor.IsInRole("Admin"))
        {
            approval = !string.IsNullOrWhiteSpace(request.AdminUserId)
                ? await adminPinService.VerifyByUserIdAsync(request.AdminUserId, request.AdminPin ?? "", cancellationToken)
                : await adminPinService.VerifyAsync(request.AdminEmail ?? "", request.AdminPin ?? "", cancellationToken);
            if (!approval.Succeeded) return new(false, approval.LockedUntil.HasValue ? 429 : 403, approval.Error ?? "Admin approval failed.");
        }
        var reason = request.Reason?.Trim();
        if (build.Status != WorkOrderStatuses.Pending && string.IsNullOrWhiteSpace(reason))
            return new(false, 400, "Please provide a reason for this change.");
        if (reason?.Length > 500) return new(false, 400, "The reason cannot exceed 500 characters.");
        if (productIds.Count != productIds.Distinct().Count())
            return new(false, 400, "Duplicate products are not allowed.");

        var products = await context.Products.Include(value => value.Supplier)
            .Where(value => productIds.Contains(value.Id) && value.IsActive).ToListAsync(cancellationToken);
        if (products.Count != productIds.Count) return new(false, 400, "One or more products were not found or are inactive.");

        build.SelectedPartsJson = JsonSerializer.Serialize(products.Select(product => new
        {
            product.Id, product.Name, product.Category, product.Brand, product.Price,
            product.CurrentStock, product.ReorderTarget, SupplierId = product.SupplierId ?? 0,
            SupplierName = product.Supplier?.Name ?? "", ImageUrl = product.ImageUrl ?? ""
        }));
        build.TotalPrice = products.Sum(value => value.Price);
        AddAudit(actor, id, "PartsChanged", null, string.Join(',', productIds), reason, approval);
        await context.SaveChangesAsync(cancellationToken);
        return new(true, 200, "Parts updated.", build.TotalPrice);
    }

    private void RestoreStock(Transaction transaction)
    {
        var productIds = transaction.Items.Select(value => value.ProductId).Distinct().ToList();
        var products = context.Products.Where(value => productIds.Contains(value.Id)).ToDictionary(value => value.Id);
        var now = DateTime.Now;
        var reversal = new Transaction
        {
            InvoiceNumber = $"RVT-{now:yyMMdd-HHss}-{InvoiceHelper.ShortCode()}", TransactionDate = now,
            TransactionType = TransactionTypes.StockCorrection, PaymentMethod = "N/A", LocationId = transaction.LocationId,
            Remarks = $"Stock restored from voided sale {transaction.InvoiceNumber}", TotalAmount = 0
        };
        foreach (var item in transaction.Items.Where(value => products.ContainsKey(value.ProductId)))
        {
            var product = products[item.ProductId];
            var before = product.CurrentStock;
            product.AddStock(item.Quantity);
            reversal.Items.Add(new TransactionItem { ProductId = item.ProductId, ProductName = item.ProductName,
                UnitPrice = item.UnitPrice, UnitCost = item.UnitCost, Quantity = item.Quantity,
                StockBefore = before, StockAfter = product.CurrentStock, LineTotal = 0 });
        }
        context.Transactions.Add(reversal);
    }

    private void AddAudit(ClaimsPrincipal actor, int id, string action, string? previous, string? next, string? reason,
        AdminPinVerificationResult? approval = null) =>
        context.WorkOrderAudits.Add(new WorkOrderAudit
        {
            WorkOrderType = "Build", WorkOrderId = id, Action = action, PreviousValue = previous, NewValue = next,
            ActorUserId = actor.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown",
            ActorRole = actor.IsInRole("Admin") ? "Admin" : "Employee", ApproverUserId = approval?.AdminUserId,
            ApproverEmail = approval?.AdminEmail, Reason = reason, CreatedAt = DateTime.Now
        });
}
