using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockSense.Domain.Entities;

namespace StockSense.Infrastructure.Data.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(product => product.Price).HasPrecision(18, 2);
        builder.Property(product => product.UnitCost).HasPrecision(18, 2).HasDefaultValue(0m);
        builder.Property(product => product.RowVersion).IsRowVersion();
        builder.HasIndex(product => new { product.Category, product.Brand });
        builder.HasIndex(product => product.SupplierId);

        builder.HasOne(product => product.Supplier)
            .WithMany()
            .HasForeignKey(product => product.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.Property(appointment => appointment.CustomerName).HasMaxLength(200).IsRequired();
        builder.Property(appointment => appointment.CustomerEmail).HasMaxLength(256);
        builder.Property(appointment => appointment.CustomerUserId).HasMaxLength(450);
        builder.HasIndex(appointment => appointment.CustomerUserId);
        builder.HasIndex(appointment => appointment.CustomerEmail);

        builder.HasIndex(appointment => appointment.TransactionId)
            .IsUnique()
            .HasFilter("[TransactionId] IS NOT NULL");

        builder.HasIndex(appointment => appointment.PaymentLinkId)
            .HasFilter("[PaymentLinkId] IS NOT NULL");

        builder.Property(appointment => appointment.PaymentAmount).HasPrecision(18, 2);

        builder.HasOne(appointment => appointment.Transaction)
            .WithOne()
            .HasForeignKey<Appointment>(appointment => appointment.TransactionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class BuildRequestConfiguration : IEntityTypeConfiguration<BuildRequest>
{
    public void Configure(EntityTypeBuilder<BuildRequest> builder)
    {
        builder.Property(build => build.CustomerName).HasMaxLength(200).IsRequired();
        builder.Property(build => build.CustomerEmail).HasMaxLength(256);
        builder.Property(build => build.CustomerUserId).HasMaxLength(450);
        builder.HasIndex(build => build.CustomerUserId);
        builder.HasIndex(build => build.CustomerEmail);

        builder.HasIndex(build => build.TransactionId)
            .IsUnique()
            .HasFilter("[TransactionId] IS NOT NULL");

        builder.HasOne(build => build.Transaction)
            .WithOne()
            .HasForeignKey<BuildRequest>(build => build.TransactionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions", table =>
        {
            table.HasCheckConstraint("CK_Transactions_DiscountAmount", "[DiscountAmount] >= 0");
            table.HasCheckConstraint("CK_Transactions_ServiceAmount", "[ServiceAmount] >= 0");
            table.HasCheckConstraint("CK_Transactions_TotalAmount", "[TotalAmount] >= 0");
        });

        builder.Property(transaction => transaction.InvoiceNumber).HasMaxLength(80).IsRequired();
        builder.Property(transaction => transaction.TransactionType)
            .HasMaxLength(20)
            .HasDefaultValue("Sale")
            .IsRequired();
        builder.Property(transaction => transaction.PaymentMethod)
            .HasMaxLength(20)
            .HasDefaultValue("Cash")
            .IsRequired();
        builder.Property(transaction => transaction.LocationId)
            .HasMaxLength(50)
            .HasDefaultValue("MAIN")
            .IsRequired();
        builder.Property(transaction => transaction.ReferenceNumber).HasMaxLength(100);
        builder.Property(transaction => transaction.UserId).HasMaxLength(450);
        builder.Property(transaction => transaction.Remarks).HasMaxLength(500);
        builder.Property(transaction => transaction.DiscountAmount).HasPrecision(18, 2);
        builder.Property(transaction => transaction.ServiceAmount).HasPrecision(18, 2);
        builder.Property(transaction => transaction.TotalAmount).HasPrecision(18, 2);

        builder.HasIndex(transaction => transaction.InvoiceNumber).IsUnique();
        builder.HasIndex(transaction => new { transaction.TransactionType, transaction.TransactionDate });
        builder.HasIndex(transaction => transaction.UserId);
        builder.HasIndex(transaction => transaction.TransactionDate);
        builder.HasIndex(transaction => transaction.TransactionType);
        builder.HasIndex(transaction => transaction.LocationId);
        builder.HasIndex(transaction => new
        {
            transaction.LocationId,
            transaction.TransactionType,
            transaction.TransactionDate
        });

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(transaction => transaction.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(transaction => transaction.Items)
            .WithOne(item => item.Transaction)
            .HasForeignKey(item => item.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(transaction => transaction.OrderSlip)
            .WithMany(orderSlip => orderSlip.PurchaseReceipts)
            .HasForeignKey(transaction => transaction.OrderSlipId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class TransactionItemConfiguration : IEntityTypeConfiguration<TransactionItem>
{
    public void Configure(EntityTypeBuilder<TransactionItem> builder)
    {
        builder.ToTable("TransactionItems", table =>
        {
            table.HasCheckConstraint("CK_TransactionItems_Quantity", "[Quantity] > 0");
            table.HasCheckConstraint(
                "CK_TransactionItems_Amounts",
                "[UnitPrice] >= 0 AND [UnitCost] >= 0 AND [DiscountAmount] >= 0 AND [LineTotal] >= 0");
            table.HasCheckConstraint(
                "CK_TransactionItems_StockAudit",
                "[StockBefore] >= 0 AND [StockAfter] >= 0");
            table.HasCheckConstraint(
                "CK_TransactionItems_DemandAudit",
                "[LostSalesQuantity] >= 0 AND ([RequestedQuantity] IS NULL OR [RequestedQuantity] >= [Quantity])");
        });

        builder.Property(item => item.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(item => item.UnitPrice).HasPrecision(18, 2);
        builder.Property(item => item.UnitCost).HasPrecision(18, 2);
        builder.Property(item => item.DiscountAmount).HasPrecision(18, 2);
        builder.Property(item => item.LineTotal).HasPrecision(18, 2);
        builder.Property(item => item.LostSalesQuantity).HasDefaultValue(0);
        builder.Property(item => item.StockoutOccurred).HasDefaultValue(false);

        builder.HasIndex(item => item.ProductId);
        builder.HasIndex(item => item.TransactionId);
        builder.HasIndex(item => new { item.TransactionId, item.ProductId });

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(item => item.OrderSlipItem)
            .WithMany(orderSlipItem => orderSlipItem.ReceiptItems)
            .HasForeignKey(item => item.OrderSlipItemId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
