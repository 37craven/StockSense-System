using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration;
using StockSense.Application.DTOs;
using StockSense.Application.Interfaces;
using StockSense.Domain.Entities;
using StockSense.Infrastructure.Data;
using StockSense.Infrastructure.Services;
using StockSense.Web.Controllers;

namespace StockSense.Tests;

public sealed class OrderSlipsControllerTransitionTests
{
    [Fact]
    public void Approve_IsAdminOnly_AndUsesExpectedRoute()
    {
        var method = typeof(OrderSlipsController).GetMethod(nameof(OrderSlipsController.Approve))!;

        Assert.Equal("Admin", Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>()).Roles);
        Assert.Equal("{id:int}/approve", Assert.Single(method.GetCustomAttributes<HttpPostAttribute>()).Template);
    }

    [Fact]
    public void LegacyMarkOrderedRoute_IsAuthenticatedAndRetainedForFriendlyGuidance()
    {
        var controllerAuthorization = Assert.Single(typeof(OrderSlipsController).GetCustomAttributes<AuthorizeAttribute>());
        var method = typeof(OrderSlipsController).GetMethod(nameof(OrderSlipsController.MarkOrdered))!;

        Assert.Equal("Admin, Employee", controllerAuthorization.Roles);
        Assert.Empty(method.GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("{id:int}/mark-ordered", Assert.Single(method.GetCustomAttributes<HttpPostAttribute>()).Template);
    }

    [Fact]
    public void SendToSupplier_AllowsAdminsAndEmployees_AndUsesExpectedRoute()
    {
        var method = typeof(OrderSlipsController).GetMethod(nameof(OrderSlipsController.SendToSupplier))!;
        Assert.Empty(method.GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("{id:int}/send-to-supplier", Assert.Single(method.GetCustomAttributes<HttpPostAttribute>()).Template);
    }

    [Fact]
    public void CloseRemaining_AllowsAuthenticatedAdminsAndEmployees_AndUsesExpectedRoute()
    {
        var method = typeof(OrderSlipsController).GetMethod(nameof(OrderSlipsController.CloseRemaining))!;
        Assert.Empty(method.GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("{id:int}/close-remaining", Assert.Single(method.GetCustomAttributes<HttpPostAttribute>()).Template);
    }

    [Fact]
    public async Task CloseRemaining_UsesRouteIdAndAuthenticatedUser()
    {
        var workflow = new CapturingWorkflow();
        var controller = CreateController(workflow, "admin-42", "Admin");
        var command = new CloseOrderSlipShortCommand
        {
            OrderSlipId = 999, ActingUserId = "spoofed", Reason = "Supplier cannot fulfill", RowVersion = [8]
        };

        var result = await controller.CloseRemaining(31, command, default);

        Assert.IsType<OkObjectResult>(result);
        Assert.Same(command, workflow.CloseShortCommand);
        Assert.Equal(31, command.OrderSlipId);
        Assert.Equal("admin-42", command.ActingUserId);
        Assert.Equal("Admin", command.ActorRole);
        Assert.Null(command.ApproverUserId);
    }

    [Fact]
    public async Task EmployeeCloseRemaining_RequiresAdminOverride()
    {
        var workflow = new CapturingWorkflow();
        var controller = CreateController(workflow, "employee-7", "Employee");

        var result = await controller.CloseRemaining(31, new CloseOrderSlipShortCommand
        {
            Reason = "Supplier cancelled the balance", RowVersion = [8]
        }, default);

        Assert.Equal(StatusCodes.Status403Forbidden, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Null(workflow.CloseShortCommand);
    }

    [Fact]
    public async Task EmployeeCloseRemaining_WithValidOverrideCapturesActorAndApprover()
    {
        var workflow = new CapturingWorkflow();
        var pin = new FakeAdminPinService(new(true, "admin-9", "admin@example.test"));
        var controller = CreateController(workflow, "employee-7", "Employee", pin);
        var command = new CloseOrderSlipShortCommand
        {
            AdminUserId = "admin-9", AdminPin = "123456",
            Reason = "  Supplier cancelled the balance  ", RowVersion = [8]
        };

        var result = await controller.CloseRemaining(31, command, default);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("employee-7", command.ActingUserId);
        Assert.Equal("Employee", command.ActorRole);
        Assert.Equal("admin-9", command.ApproverUserId);
        Assert.Equal("admin@example.test", command.ApproverEmail);
        Assert.Equal("Supplier cancelled the balance", command.Reason);
        Assert.Null(command.AdminPin);
        Assert.Equal(("admin-9", "123456"), pin.LastVerification);
    }

    [Fact]
    public async Task EmployeeCloseRemaining_WithInvalidPinDoesNotRunWorkflow()
    {
        var workflow = new CapturingWorkflow();
        var pin = new FakeAdminPinService(new(false, Error: "The selected admin or PIN is incorrect."));
        var controller = CreateController(workflow, "employee-7", "Employee", pin);

        var result = await controller.CloseRemaining(31, new CloseOrderSlipShortCommand
        {
            AdminUserId = "admin-9", AdminPin = "000000",
            Reason = "Supplier cancelled the balance", RowVersion = [8]
        }, default);

        Assert.Equal(StatusCodes.Status403Forbidden, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Null(workflow.CloseShortCommand);
    }

    [Fact]
    public async Task EmployeeCloseRemaining_WhenPinIsLockedReturnsTooManyRequests()
    {
        var workflow = new CapturingWorkflow();
        var pin = new FakeAdminPinService(new(false, Error: "Too many incorrect attempts. Please try again later.",
            LockedUntil: DateTimeOffset.UtcNow.AddMinutes(15)));
        var controller = CreateController(workflow, "employee-7", "Employee", pin);

        var result = await controller.CloseRemaining(31, new CloseOrderSlipShortCommand
        {
            AdminUserId = "admin-9", AdminPin = "000000",
            Reason = "Supplier cancelled the balance", RowVersion = [8]
        }, default);

        Assert.Equal(StatusCodes.Status429TooManyRequests, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Null(workflow.CloseShortCommand);
    }

    [Fact]
    public async Task Approve_UsesRouteIdApprovedTargetAndAuthenticatedUser()
    {
        var workflow = new CapturingWorkflow();
        var controller = CreateController(workflow, "admin-42");
        var rowVersion = new byte[] { 1, 2, 3 };
        var command = new OrderSlipTransitionCommand
        {
            OrderSlipId = 999,
            TargetStatus = OrderSlipStatuses.Cancelled,
            ActingUserId = "spoofed-user",
            RowVersion = rowVersion,
            Remarks = "Approved after review"
        };

        var result = await controller.Approve(17, command, default);

        Assert.IsType<OkObjectResult>(result);
        Assert.Same(command, workflow.ApproveCommand);
        Assert.Equal(17, command.OrderSlipId);
        Assert.Equal(OrderSlipStatuses.Approved, command.TargetStatus);
        Assert.Equal("admin-42", command.ActingUserId);
        Assert.Same(rowVersion, command.RowVersion);
        Assert.Equal("Approved after review", command.Remarks);
    }

    [Fact]
    public void MarkOrdered_DirectTransitionIsBlockedAndPointsToSupplierDispatch()
    {
        var workflow = new CapturingWorkflow();
        var controller = CreateController(workflow, "employee-7");
        var result = controller.MarkOrdered(23, new OrderSlipTransitionCommand(), default);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("send-to-supplier", badRequest.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Null(workflow.MarkOrderedCommand);
    }

    [Fact]
    public async Task TransitionConcurrencyConflict_ReturnsConflictWithFriendlyError()
    {
        var workflow = new CapturingWorkflow
        {
            ApproveResult = OperationResult<OrderSlipDto>.Failure(
                "CONCURRENCY_CONFLICT", "This order changed. Reload it and try again.", true)
        };
        var controller = CreateController(workflow, "employee-7");

        var result = await controller.Approve(23, new OrderSlipTransitionCommand(), default);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Contains("Reload", conflict.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static OrderSlipsController CreateController(
        CapturingWorkflow workflow, string userId, string role = "Employee", IAdminPinService? adminPinService = null)
    {
        var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);
        return new OrderSlipsController(
            context,
            workflow,
            new DocumentService(),
            new PdfDownloadCache(),
            new OrderEmailSender(new ConfigurationBuilder().Build()),
            NullLogger<OrderSlipsController>.Instance,
            adminPinService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, userId), new Claim(ClaimTypes.Role, role)], "Test"))
                }
            }
        };
    }

    private sealed class FakeAdminPinService(AdminPinVerificationResult result) : IAdminPinService
    {
        public (string UserId, string Pin)? LastVerification { get; private set; }

        public Task<AdminPinOperationResult> SetPinAsync(string adminUserId, string currentPassword, string newPin,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<AdminPinVerificationResult> VerifyAsync(string adminEmail, string pin,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<AdminPinVerificationResult> VerifyByUserIdAsync(string adminUserId, string pin,
            CancellationToken cancellationToken = default)
        {
            LastVerification = (adminUserId, pin);
            return Task.FromResult(result);
        }
    }

    private sealed class CapturingWorkflow : IOrderSlipWorkflowService
    {
        public OrderSlipTransitionCommand? ApproveCommand { get; private set; }
        public OrderSlipTransitionCommand? MarkOrderedCommand { get; private set; }
        public CloseOrderSlipShortCommand? CloseShortCommand { get; private set; }
        public OperationResult<OrderSlipDto> ApproveResult { get; set; } =
            OperationResult<OrderSlipDto>.Success(new OrderSlipDto());
        public OperationResult<OrderSlipDto> MarkOrderedResult { get; set; } =
            OperationResult<OrderSlipDto>.Success(new OrderSlipDto());

        public Task<OperationResult<OrderSlipDto>> ApproveAsync(
            OrderSlipTransitionCommand command, CancellationToken cancellationToken = default)
        {
            ApproveCommand = command;
            return Task.FromResult(ApproveResult);
        }

        public Task<OperationResult<OrderSlipDto>> MarkOrderedAsync(
            OrderSlipTransitionCommand command, CancellationToken cancellationToken = default)
        {
            MarkOrderedCommand = command;
            return Task.FromResult(MarkOrderedResult);
        }

        public Task<OperationResult<OrderSlipDto>> CloseShortAsync(
            CloseOrderSlipShortCommand command, CancellationToken cancellationToken = default)
        {
            CloseShortCommand = command;
            return Task.FromResult(OperationResult<OrderSlipDto>.Success(new OrderSlipDto()));
        }

        public Task<OperationResult<OrderSlipPreviewDto>> PreviewAsync(string locationId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<OperationResult<CreateDraftOrderSlipsResult>> CreateDraftsAsync(CreateOrderSlipDraftsCommand command, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<OperationResult<ManualOrderSlipCatalogDto>> GetManualCatalogAsync(string locationId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<OperationResult<OrderSlipDto>> CreateManualDraftAsync(CreateManualOrderSlipDraftCommand command, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<OperationResult<OrderSlipDto>> CancelAsync(CancelOrderSlipCommand command, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<OperationResult<OrderSlipReceiptResult>> ReceiveAsync(ReceiveOrderSlipCommand command, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
