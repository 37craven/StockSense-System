using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockSense.Domain.Entities;

namespace StockSense.Infrastructure.Data.Configurations;

public sealed class MotorCompatibilityConfiguration : IEntityTypeConfiguration<MotorCompatibility>
{
    public void Configure(EntityTypeBuilder<MotorCompatibility> builder)
    {
        builder.ToTable("MotorCompatibility", table =>
        {
            table.HasCheckConstraint(
                "CK_MotorCompatibility_Manufacturer",
                "[Manufacturer] IN ('Honda', 'Yamaha', 'Suzuki', 'Kawasaki', 'Rusi')");
            table.HasCheckConstraint(
                "CK_MotorCompatibility_YearRange",
                "[YearStart] >= 1885 AND ([YearEnd] IS NULL OR [YearEnd] >= [YearStart])");
        });

        builder.HasKey(compatibility => compatibility.CompatibilityId)
            .HasName("PK_MotorCompatibility");

        builder.Property(compatibility => compatibility.CompatibilityId)
            .HasColumnName("CompatibilityID");
        builder.Property(compatibility => compatibility.Manufacturer)
            .HasMaxLength(50)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(compatibility => compatibility.ModelName)
            .HasMaxLength(100)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(compatibility => compatibility.VersionName)
            .HasMaxLength(50)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(compatibility => compatibility.EngineOilSpec).HasMaxLength(100).IsUnicode(false);
        builder.Property(compatibility => compatibility.GearOilSpec).HasMaxLength(100).IsUnicode(false);
        builder.Property(compatibility => compatibility.CoolantSpec).HasMaxLength(100).IsUnicode(false);
        builder.Property(compatibility => compatibility.SparkPlugSpec).HasMaxLength(100).IsUnicode(false);
        builder.Property(compatibility => compatibility.FuelFilterSpec).HasMaxLength(100).IsUnicode(false);
        builder.Property(compatibility => compatibility.DriveBeltSpec).HasMaxLength(100).IsUnicode(false);
        builder.Property(compatibility => compatibility.FlyBallWeight).HasMaxLength(50).IsUnicode(false);
        builder.Property(compatibility => compatibility.CenterSpringSpec).HasMaxLength(50).IsUnicode(false);
        builder.Property(compatibility => compatibility.BrakePadFront).HasMaxLength(100).IsUnicode(false);
        builder.Property(compatibility => compatibility.BrakePadRear).HasMaxLength(100).IsUnicode(false);
        builder.Property(compatibility => compatibility.BrakeShoeRear).HasMaxLength(100).IsUnicode(false);
        builder.Property(compatibility => compatibility.AirFilterSpec).HasMaxLength(100).IsUnicode(false);

        builder.HasIndex(compatibility => new
            {
                compatibility.Manufacturer,
                compatibility.ModelName,
                compatibility.VersionName,
                compatibility.YearStart,
                compatibility.YearEnd
            })
            .IsUnique()
            .HasFilter(null)
            .HasDatabaseName("UX_MotorCompatibility_ModelVersionYears");
    }
}

public sealed class ProductCompatibilityMappingConfiguration : IEntityTypeConfiguration<ProductCompatibilityMapping>
{
    public void Configure(EntityTypeBuilder<ProductCompatibilityMapping> builder)
    {
        builder.ToTable("ProductCompatibilityMapping");

        builder.HasKey(mapping => mapping.MappingId)
            .HasName("PK_ProductCompatibilityMapping");

        builder.Property(mapping => mapping.MappingId).HasColumnName("MappingID");
        builder.Property(mapping => mapping.CompatibilityId).HasColumnName("CompatibilityID");
        builder.Property(mapping => mapping.ProductId).HasColumnName("ProductID");
        builder.Property(mapping => mapping.PartFunction)
            .HasMaxLength(50)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(mapping => mapping.IsOEM)
            .HasDefaultValue(false);
        builder.Property(mapping => mapping.Notes)
            .HasMaxLength(255)
            .IsUnicode(false);

        builder.HasIndex(mapping => new
            {
                mapping.CompatibilityId,
                mapping.ProductId,
                mapping.PartFunction
            })
            .IsUnique()
            .HasDatabaseName("UX_ProductCompatibilityMapping_CompatibilityProductFunction");

        builder.HasIndex(mapping => mapping.ProductId)
            .HasDatabaseName("IX_ProductCompatibilityMapping_ProductID")
            .IncludeProperties(mapping => new
            {
                mapping.CompatibilityId,
                mapping.PartFunction,
                mapping.IsOEM
            });

        builder.HasOne(mapping => mapping.MotorCompatibility)
            .WithMany(compatibility => compatibility.ProductMappings)
            .HasForeignKey(mapping => mapping.CompatibilityId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ProductCompatibilityMapping_MotorCompatibility");

        builder.HasOne(mapping => mapping.Product)
            .WithMany(product => product.CompatibilityMappings)
            .HasForeignKey(mapping => mapping.ProductId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ProductCompatibilityMapping_Products");
    }
}
