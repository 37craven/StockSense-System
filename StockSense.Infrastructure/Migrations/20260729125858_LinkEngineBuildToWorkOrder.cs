using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSense.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LinkEngineBuildToWorkOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerBuilds_BikeModels_BikeModelId",
                table: "CustomerBuilds");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerBuilds_UpgradeStages_UpgradeStageId",
                table: "CustomerBuilds");

            migrationBuilder.DropForeignKey(
                name: "FK_UpgradeParts_Products_ProductId",
                table: "UpgradeParts");

            migrationBuilder.DropForeignKey(
                name: "FK_UpgradeParts_UpgradeCategories_UpgradeCategoryId",
                table: "UpgradeParts");

            migrationBuilder.DropIndex(
                name: "IX_UpgradeStages_BikeModelId",
                table: "UpgradeStages");

            migrationBuilder.DropIndex(
                name: "IX_UpgradeParts_ProductId",
                table: "UpgradeParts");

            migrationBuilder.DropIndex(
                name: "IX_UpgradeParts_UpgradeCategoryId",
                table: "UpgradeParts");

            migrationBuilder.AddColumn<int>(
                name: "BuildRequestId",
                table: "CustomerBuilds",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UpgradeStages_BikeModelId_StageNumber",
                table: "UpgradeStages",
                columns: new[] { "BikeModelId", "StageNumber" },
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
                name: "IX_UpgradeCategories_DisplayOrder",
                table: "UpgradeCategories",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_UpgradeCategories_Name",
                table: "UpgradeCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerBuilds_BuildRequestId",
                table: "CustomerBuilds",
                column: "BuildRequestId",
                unique: true,
                filter: "[BuildRequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerBuilds_UserId_Status_UpdatedAt",
                table: "CustomerBuilds",
                columns: new[] { "UserId", "Status", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BikeModels_Brand_Model_YearStart_YearEnd",
                table: "BikeModels",
                columns: new[] { "Brand", "Model", "YearStart", "YearEnd" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerBuilds_BikeModels_BikeModelId",
                table: "CustomerBuilds",
                column: "BikeModelId",
                principalTable: "BikeModels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerBuilds_BuildRequests_BuildRequestId",
                table: "CustomerBuilds",
                column: "BuildRequestId",
                principalTable: "BuildRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerBuilds_UpgradeStages_UpgradeStageId",
                table: "CustomerBuilds",
                column: "UpgradeStageId",
                principalTable: "UpgradeStages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UpgradeParts_Products_ProductId",
                table: "UpgradeParts",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UpgradeParts_UpgradeCategories_UpgradeCategoryId",
                table: "UpgradeParts",
                column: "UpgradeCategoryId",
                principalTable: "UpgradeCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerBuilds_BikeModels_BikeModelId",
                table: "CustomerBuilds");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerBuilds_BuildRequests_BuildRequestId",
                table: "CustomerBuilds");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerBuilds_UpgradeStages_UpgradeStageId",
                table: "CustomerBuilds");

            migrationBuilder.DropForeignKey(
                name: "FK_UpgradeParts_Products_ProductId",
                table: "UpgradeParts");

            migrationBuilder.DropForeignKey(
                name: "FK_UpgradeParts_UpgradeCategories_UpgradeCategoryId",
                table: "UpgradeParts");

            migrationBuilder.DropIndex(
                name: "IX_UpgradeStages_BikeModelId_StageNumber",
                table: "UpgradeStages");

            migrationBuilder.DropIndex(
                name: "IX_UpgradeParts_ProductId",
                table: "UpgradeParts");

            migrationBuilder.DropIndex(
                name: "IX_UpgradeParts_UpgradeCategoryId_IsActive",
                table: "UpgradeParts");

            migrationBuilder.DropIndex(
                name: "IX_UpgradeCategories_DisplayOrder",
                table: "UpgradeCategories");

            migrationBuilder.DropIndex(
                name: "IX_UpgradeCategories_Name",
                table: "UpgradeCategories");

            migrationBuilder.DropIndex(
                name: "IX_CustomerBuilds_BuildRequestId",
                table: "CustomerBuilds");

            migrationBuilder.DropIndex(
                name: "IX_CustomerBuilds_UserId_Status_UpdatedAt",
                table: "CustomerBuilds");

            migrationBuilder.DropIndex(
                name: "IX_BikeModels_Brand_Model_YearStart_YearEnd",
                table: "BikeModels");

            migrationBuilder.DropColumn(
                name: "BuildRequestId",
                table: "CustomerBuilds");

            migrationBuilder.CreateIndex(
                name: "IX_UpgradeStages_BikeModelId",
                table: "UpgradeStages",
                column: "BikeModelId");

            migrationBuilder.CreateIndex(
                name: "IX_UpgradeParts_ProductId",
                table: "UpgradeParts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_UpgradeParts_UpgradeCategoryId",
                table: "UpgradeParts",
                column: "UpgradeCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerBuilds_BikeModels_BikeModelId",
                table: "CustomerBuilds",
                column: "BikeModelId",
                principalTable: "BikeModels",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerBuilds_UpgradeStages_UpgradeStageId",
                table: "CustomerBuilds",
                column: "UpgradeStageId",
                principalTable: "UpgradeStages",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UpgradeParts_Products_ProductId",
                table: "UpgradeParts",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UpgradeParts_UpgradeCategories_UpgradeCategoryId",
                table: "UpgradeParts",
                column: "UpgradeCategoryId",
                principalTable: "UpgradeCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
