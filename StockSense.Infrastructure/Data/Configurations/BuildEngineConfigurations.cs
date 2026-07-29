using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockSense.Domain.Entities;

namespace StockSense.Infrastructure.Data.Configurations;

public sealed class BikeModelConfiguration : IEntityTypeConfiguration<BikeModel>
{
    public void Configure(EntityTypeBuilder<BikeModel> builder)
    {
        builder.Property(model => model.Brand).HasMaxLength(50).IsRequired();
        builder.Property(model => model.Model).HasMaxLength(100).IsRequired();
        builder.Property(model => model.EngineCode).HasMaxLength(20);
        builder.Property(model => model.Notes).HasMaxLength(500);
        builder.HasIndex(model => new { model.Brand, model.Model, model.YearStart, model.YearEnd }).IsUnique();
    }
}

public sealed class UpgradeCategoryConfiguration : IEntityTypeConfiguration<UpgradeCategory>
{
    public void Configure(EntityTypeBuilder<UpgradeCategory> builder)
    {
        builder.Property(category => category.Name).HasMaxLength(100).IsRequired();
        builder.Property(category => category.Icon).HasMaxLength(50);
        builder.Property(category => category.Description).HasMaxLength(500);
        builder.Property(category => category.CompatibilityNotes).HasMaxLength(500);
        builder.HasIndex(category => category.Name).IsUnique();
        builder.HasIndex(category => category.DisplayOrder);
    }
}

public sealed class UpgradePartConfiguration : IEntityTypeConfiguration<UpgradePart>
{
    public void Configure(EntityTypeBuilder<UpgradePart> builder)
    {
        builder.Property(part => part.ListPrice).HasPrecision(18, 2);
        builder.Property(part => part.EstimatedLaborHours).HasPrecision(4, 2);
        builder.Property(part => part.RenderImageUrl).HasMaxLength(500);
        builder.Property(part => part.BreakInNotes).HasMaxLength(500);
        builder.Property(part => part.InstallNotes).HasMaxLength(1000);
        builder.Property(part => part.PresetTemplate).HasMaxLength(50);
        builder.HasIndex(part => part.ProductId).IsUnique();
        builder.HasIndex(part => new { part.UpgradeCategoryId, part.IsActive });

        builder.HasOne(part => part.Product)
            .WithMany()
            .HasForeignKey(part => part.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(part => part.Category)
            .WithMany(category => category.UpgradeParts)
            .HasForeignKey(part => part.UpgradeCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class UpgradeStageConfiguration : IEntityTypeConfiguration<UpgradeStage>
{
    public void Configure(EntityTypeBuilder<UpgradeStage> builder)
    {
        builder.Property(stage => stage.Name).HasMaxLength(100).IsRequired();
        builder.Property(stage => stage.Description).HasMaxLength(1000);
        builder.Property(stage => stage.EstimatedCost).HasPrecision(18, 2);
        builder.HasIndex(stage => new { stage.BikeModelId, stage.StageNumber }).IsUnique();

        builder.HasOne(stage => stage.BikeModel)
            .WithMany(model => model.Stages)
            .HasForeignKey(stage => stage.BikeModelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CustomerBuildConfiguration : IEntityTypeConfiguration<CustomerBuild>
{
    public void Configure(EntityTypeBuilder<CustomerBuild> builder)
    {
        builder.Property(build => build.UserId).HasMaxLength(450).IsRequired();
        builder.Property(build => build.Status).HasMaxLength(20).IsRequired();
        builder.Property(build => build.TotalPartsCost).HasPrecision(18, 2);
        builder.Property(build => build.EstimatedLaborCost).HasPrecision(18, 2);
        builder.HasIndex(build => new { build.UserId, build.Status, build.UpdatedAt });
        builder.HasIndex(build => build.BuildRequestId).IsUnique().HasFilter("[BuildRequestId] IS NOT NULL");

        builder.HasOne(build => build.BikeModel)
            .WithMany()
            .HasForeignKey(build => build.BikeModelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(build => build.UpgradeStage)
            .WithMany()
            .HasForeignKey(build => build.UpgradeStageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(build => build.BuildRequest)
            .WithOne()
            .HasForeignKey<CustomerBuild>(build => build.BuildRequestId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
