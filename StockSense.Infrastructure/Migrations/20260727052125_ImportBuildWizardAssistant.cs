using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSense.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ImportBuildWizardAssistant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BikeModels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Brand = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    YearStart = table.Column<int>(type: "int", nullable: false),
                    YearEnd = table.Column<int>(type: "int", nullable: false),
                    BaseCC = table.Column<int>(type: "int", nullable: false),
                    BaseHP = table.Column<int>(type: "int", nullable: false),
                    BaseTorque = table.Column<int>(type: "int", nullable: false),
                    EngineCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BikeModels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SynergyRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RequiredCategoryIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HPBonusPercent = table.Column<int>(type: "int", nullable: false),
                    TorqueBonusPercent = table.Column<int>(type: "int", nullable: false),
                    ReliabilityBonus = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SynergyRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UpgradeCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    AllowsMultiple = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CompatibilityNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpgradeCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UpgradeStages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BikeModelId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StageNumber = table.Column<int>(type: "int", nullable: false),
                    TargetCC = table.Column<int>(type: "int", nullable: false),
                    EstimatedHP = table.Column<int>(type: "int", nullable: false),
                    EstimatedTorque = table.Column<int>(type: "int", nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RequiredCategoryIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecommendedPartIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsGuidedPath = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpgradeStages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UpgradeStages_BikeModels_BikeModelId",
                        column: x => x.BikeModelId,
                        principalTable: "BikeModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UpgradeParts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    UpgradeCategoryId = table.Column<int>(type: "int", nullable: false),
                    CCGain = table.Column<int>(type: "int", nullable: false),
                    HPGain = table.Column<int>(type: "int", nullable: false),
                    TorqueGain = table.Column<int>(type: "int", nullable: false),
                    ReliabilityImpact = table.Column<int>(type: "int", nullable: false),
                    RenderImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ListPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EstimatedLaborHours = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: false),
                    CompatibleModelsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiredPartIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConflictingPartIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiredForStagesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompressionRatioImpact = table.Column<double>(type: "float", nullable: false),
                    RedlineRPMChange = table.Column<int>(type: "int", nullable: false),
                    BottomEndStressMultiplier = table.Column<double>(type: "float", nullable: false),
                    ValvetrainStressMultiplier = table.Column<double>(type: "float", nullable: false),
                    RequiresRaceFuel = table.Column<bool>(type: "bit", nullable: false),
                    BreakInRequired = table.Column<bool>(type: "bit", nullable: false),
                    BreakInNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    InstallNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PresetTemplate = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpgradeParts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UpgradeParts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UpgradeParts_UpgradeCategories_UpgradeCategoryId",
                        column: x => x.UpgradeCategoryId,
                        principalTable: "UpgradeCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerBuilds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    BikeModelId = table.Column<int>(type: "int", nullable: true),
                    UpgradeStageId = table.Column<int>(type: "int", nullable: true),
                    SelectedPartIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentCC = table.Column<int>(type: "int", nullable: false),
                    ProjectedHP = table.Column<int>(type: "int", nullable: false),
                    ProjectedTorque = table.Column<int>(type: "int", nullable: false),
                    ReliabilityScore = table.Column<int>(type: "int", nullable: false),
                    TotalPartsCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EstimatedLaborCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ValidationWarningsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValidationErrorsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MissingRequirementsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaintenanceProjectionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerBuilds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerBuilds_BikeModels_BikeModelId",
                        column: x => x.BikeModelId,
                        principalTable: "BikeModels",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CustomerBuilds_UpgradeStages_UpgradeStageId",
                        column: x => x.UpgradeStageId,
                        principalTable: "UpgradeStages",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerBuilds_BikeModelId",
                table: "CustomerBuilds",
                column: "BikeModelId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerBuilds_UpgradeStageId",
                table: "CustomerBuilds",
                column: "UpgradeStageId");

            migrationBuilder.CreateIndex(
                name: "IX_UpgradeParts_ProductId",
                table: "UpgradeParts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_UpgradeParts_UpgradeCategoryId",
                table: "UpgradeParts",
                column: "UpgradeCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_UpgradeStages_BikeModelId",
                table: "UpgradeStages",
                column: "BikeModelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerBuilds");

            migrationBuilder.DropTable(
                name: "SynergyRules");

            migrationBuilder.DropTable(
                name: "UpgradeParts");

            migrationBuilder.DropTable(
                name: "UpgradeStages");

            migrationBuilder.DropTable(
                name: "UpgradeCategories");

            migrationBuilder.DropTable(
                name: "BikeModels");
        }
    }
}
