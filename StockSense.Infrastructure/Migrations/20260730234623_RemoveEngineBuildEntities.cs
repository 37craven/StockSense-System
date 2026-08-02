using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSense.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEngineBuildEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider?.Contains("SqlServer") == true)
            {
                migrationBuilder.Sql(@"
                    IF OBJECT_ID(N'CustomerBuilds', N'U') IS NOT NULL DROP TABLE [CustomerBuilds];
                    IF OBJECT_ID(N'SynergyRules', N'U') IS NOT NULL DROP TABLE [SynergyRules];
                    IF OBJECT_ID(N'UpgradeParts', N'U') IS NOT NULL DROP TABLE [UpgradeParts];
                    IF OBJECT_ID(N'UpgradeStages', N'U') IS NOT NULL DROP TABLE [UpgradeStages];
                    IF OBJECT_ID(N'UpgradeCategories', N'U') IS NOT NULL DROP TABLE [UpgradeCategories];
                    IF OBJECT_ID(N'BikeModels', N'U') IS NOT NULL DROP TABLE [BikeModels];
                ");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BikeModels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BaseCC = table.Column<int>(type: "int", nullable: false),
                    BaseHP = table.Column<int>(type: "int", nullable: false),
                    BaseTorque = table.Column<int>(type: "int", nullable: false),
                    Brand = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EngineCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    YearEnd = table.Column<int>(type: "int", nullable: false),
                    YearStart = table.Column<int>(type: "int", nullable: false)
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
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HPBonusPercent = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReliabilityBonus = table.Column<int>(type: "int", nullable: false),
                    RequiredCategoryIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TorqueBonusPercent = table.Column<int>(type: "int", nullable: false)
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
                    AllowsMultiple = table.Column<bool>(type: "bit", nullable: false),
                    CompatibilityNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
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
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EstimatedHP = table.Column<int>(type: "int", nullable: false),
                    EstimatedTorque = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsGuidedPath = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RecommendedPartIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiredCategoryIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StageNumber = table.Column<int>(type: "int", nullable: false),
                    TargetCC = table.Column<int>(type: "int", nullable: false)
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
                    BottomEndStressMultiplier = table.Column<double>(type: "float", nullable: false),
                    BreakInNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BreakInRequired = table.Column<bool>(type: "bit", nullable: false),
                    CCGain = table.Column<int>(type: "int", nullable: false),
                    CompatibleModelsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompressionRatioImpact = table.Column<double>(type: "float", nullable: false),
                    ConflictingPartIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EstimatedLaborHours = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: false),
                    HPGain = table.Column<int>(type: "int", nullable: false),
                    InstallNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ListPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PresetTemplate = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RedlineRPMChange = table.Column<int>(type: "int", nullable: false),
                    ReliabilityImpact = table.Column<int>(type: "int", nullable: false),
                    RenderImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RequiredForStagesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiredPartIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiresRaceFuel = table.Column<bool>(type: "bit", nullable: false),
                    TorqueGain = table.Column<int>(type: "int", nullable: false),
                    ValvetrainStressMultiplier = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpgradeParts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UpgradeParts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UpgradeParts_UpgradeCategories_UpgradeCategoryId",
                        column: x => x.UpgradeCategoryId,
                        principalTable: "UpgradeCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerBuilds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BikeModelId = table.Column<int>(type: "int", nullable: true),
                    BuildRequestId = table.Column<int>(type: "int", nullable: true),
                    UpgradeStageId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrentCC = table.Column<int>(type: "int", nullable: false),
                    EstimatedLaborCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaintenanceProjectionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MissingRequirementsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProjectedHP = table.Column<int>(type: "int", nullable: false),
                    ProjectedTorque = table.Column<int>(type: "int", nullable: false),
                    ReliabilityScore = table.Column<int>(type: "int", nullable: false),
                    SelectedPartIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalPartsCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ValidationErrorsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValidationWarningsJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerBuilds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerBuilds_BikeModels_BikeModelId",
                        column: x => x.BikeModelId,
                        principalTable: "BikeModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerBuilds_BuildRequests_BuildRequestId",
                        column: x => x.BuildRequestId,
                        principalTable: "BuildRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CustomerBuilds_UpgradeStages_UpgradeStageId",
                        column: x => x.UpgradeStageId,
                        principalTable: "UpgradeStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BikeModels_Brand_Model_YearStart_YearEnd",
                table: "BikeModels",
                columns: new[] { "Brand", "Model", "YearStart", "YearEnd" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerBuilds_BikeModelId",
                table: "CustomerBuilds",
                column: "BikeModelId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerBuilds_BuildRequestId",
                table: "CustomerBuilds",
                column: "BuildRequestId",
                unique: true,
                filter: "[BuildRequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerBuilds_UpgradeStageId",
                table: "CustomerBuilds",
                column: "UpgradeStageId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerBuilds_UserId_Status_UpdatedAt",
                table: "CustomerBuilds",
                columns: new[] { "UserId", "Status", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UpgradeCategories_DisplayOrder",
                table: "UpgradeCategories",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_UpgradeCategories_Name",
                table: "UpgradeCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UpgradeParts_ProductId",
                table: "UpgradeParts",
                column: "ProductId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UpgradeParts_UpgradeCategoryId_IsActive",
                table: "UpgradeParts",
                columns: new[] { "UpgradeCategoryId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_UpgradeStages_BikeModelId_StageNumber",
                table: "UpgradeStages",
                columns: new[] { "BikeModelId", "StageNumber" },
                unique: true);
        }
    }
}
