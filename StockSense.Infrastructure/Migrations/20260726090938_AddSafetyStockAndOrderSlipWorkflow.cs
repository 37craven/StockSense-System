using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSense.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSafetyStockAndOrderSlipWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderSlips_Suppliers_SupplierId",
                table: "OrderSlips");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Suppliers_SupplierId",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TransactionItems_StockAudit",
                table: "TransactionItems");

            migrationBuilder.DropIndex(
                name: "IX_OrderSlips_SupplierId",
                table: "OrderSlips");

            migrationBuilder.DropIndex(
                name: "IX_OrderSlipItems_OrderSlipId",
                table: "OrderSlipItems");

            migrationBuilder.AddColumn<int>(
                name: "OrderSlipId",
                table: "Transactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LostSalesQuantity",
                table: "TransactionItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrderSlipItemId",
                table: "TransactionItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequestedQuantity",
                table: "TransactionItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "StockoutOccurred",
                table: "TransactionItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "OrderSlips",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedByUserId",
                table: "OrderSlips",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "OrderSlips",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "OrderSlips",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpectedDeliveryDate",
                table: "OrderSlips",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GeneratedAt",
                table: "OrderSlips",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "LocationId",
                table: "OrderSlips",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "MAIN");

            migrationBuilder.AddColumn<string>(
                name: "OrderSlipNumber",
                table: "OrderSlips",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "OrderedAt",
                table: "OrderSlips",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "OrderSlips",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "OrderSlips",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "OrderSlips",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Draft");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalEstimatedCost",
                table: "OrderSlips",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AverageDailyDemandSnapshot",
                table: "OrderSlipItems",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "BackorderStockSnapshot",
                table: "OrderSlipItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentStockSnapshot",
                table: "OrderSlipItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedLineTotal",
                table: "OrderSlipItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "IncomingStockSnapshot",
                table: "OrderSlipItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InventoryPositionSnapshot",
                table: "OrderSlipItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "LeadTimeDaysSnapshot",
                table: "OrderSlipItems",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "MinimumOrderQuantitySnapshot",
                table: "OrderSlipItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrderedQuantity",
                table: "OrderSlipItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PackageSizeSnapshot",
                table: "OrderSlipItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "OrderSlipItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RecommendationReason",
                table: "OrderSlipItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ReorderPointSnapshot",
                table: "OrderSlipItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ReservedStockSnapshot",
                table: "OrderSlipItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SafetyStockSnapshot",
                table: "OrderSlipItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SuggestedQuantity",
                table: "OrderSlipItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TargetStockSnapshot",
                table: "OrderSlipItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitCostSnapshot",
                table: "OrderSlipItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "ProductInventoryMetrics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AverageDailyDemand = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    DemandStandardDeviation = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    AverageLeadTimeDays = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    LeadTimeStandardDeviation = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    SafetyStock = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TargetStock = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    UsableDataDays = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TotalObservedDemand = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CalculationStage = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ConfidenceLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CalculationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LastCalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CalculationVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductInventoryMetrics", x => x.Id);
                    table.CheckConstraint("CK_ProductInventoryMetrics_Confidence", "[ConfidenceLevel] IN ('Low','Medium','High')");
                    table.CheckConstraint("CK_ProductInventoryMetrics_Demand", "[AverageDailyDemand] >= 0 AND [DemandStandardDeviation] >= 0 AND [TotalObservedDemand] >= 0 AND [UsableDataDays] >= 0");
                    table.CheckConstraint("CK_ProductInventoryMetrics_LeadTime", "[AverageLeadTimeDays] >= 0 AND [LeadTimeStandardDeviation] >= 0");
                    table.CheckConstraint("CK_ProductInventoryMetrics_Stage", "[CalculationStage] IN ('ColdStart','Learning','DataDriven','Manual')");
                    table.CheckConstraint("CK_ProductInventoryMetrics_Stock", "[SafetyStock] >= 0 AND [TargetStock] >= 0");
                    table.ForeignKey(
                        name: "FK_ProductInventoryMetrics_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductInventorySettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "MAIN"),
                    CalculationMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Auto"),
                    InitialEstimatedWeeklyDemand = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    DefaultLeadTimeDays = table.Column<int>(type: "int", nullable: false, defaultValue: 7),
                    ReviewPeriodDays = table.Column<int>(type: "int", nullable: false, defaultValue: 7),
                    BufferDays = table.Column<int>(type: "int", nullable: false, defaultValue: 7),
                    ServiceLevel = table.Column<decimal>(type: "decimal(6,4)", precision: 6, scale: 4, nullable: false, defaultValue: 0.9500m),
                    MinimumSafetyStock = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    MaximumSafetyStock = table.Column<int>(type: "int", nullable: true),
                    MinimumOrderQuantity = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    PackageSize = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    MaximumStockLevel = table.Column<int>(type: "int", nullable: true),
                    ManualSafetyStock = table.Column<int>(type: "int", nullable: true),
                    ManualReorderPoint = table.Column<int>(type: "int", nullable: true),
                    IsAutomaticOrderEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    InventoryTrackingStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductInventorySettings", x => x.Id);
                    table.CheckConstraint("CK_ProductInventorySettings_Demand", "[InitialEstimatedWeeklyDemand] >= 0");
                    table.CheckConstraint("CK_ProductInventorySettings_LeadReviewBuffer", "[DefaultLeadTimeDays] >= 1 AND [ReviewPeriodDays] >= 1 AND [BufferDays] >= 0");
                    table.CheckConstraint("CK_ProductInventorySettings_ManualValues", "([ManualSafetyStock] IS NULL OR [ManualSafetyStock] >= 0) AND ([ManualReorderPoint] IS NULL OR [ManualReorderPoint] >= 0)");
                    table.CheckConstraint("CK_ProductInventorySettings_Mode", "[CalculationMode] IN ('Auto', 'Manual')");
                    table.CheckConstraint("CK_ProductInventorySettings_OrderRules", "[MinimumOrderQuantity] >= 1 AND [PackageSize] >= 1 AND ([MaximumStockLevel] IS NULL OR [MaximumStockLevel] > 0)");
                    table.CheckConstraint("CK_ProductInventorySettings_SafetyLimits", "[MinimumSafetyStock] >= 0 AND ([MaximumSafetyStock] IS NULL OR [MaximumSafetyStock] >= [MinimumSafetyStock])");
                    table.CheckConstraint("CK_ProductInventorySettings_ServiceLevel", "[ServiceLevel] >= 0.5000 AND [ServiceLevel] <= 0.9990");
                    table.ForeignKey(
                        name: "FK_ProductInventorySettings_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Preserve the legacy order-slip columns while populating the new workflow fields.
            // A generated fallback number is used only when the legacy number is blank, too long,
            // or duplicated; it is stable because it is based on the existing primary key.
            migrationBuilder.Sql(
                """
                ;WITH LegacyNumbers AS
                (
                    SELECT
                        [Id],
                        [SlipNumber],
                        COUNT(*) OVER (PARTITION BY NULLIF(LTRIM(RTRIM([SlipNumber])), '')) AS [NumberCount]
                    FROM [OrderSlips]
                )
                UPDATE target
                SET
                    [OrderSlipNumber] = CASE
                        WHEN NULLIF(LTRIM(RTRIM(source.[SlipNumber])), '') IS NOT NULL
                             AND LEN(LTRIM(RTRIM(source.[SlipNumber]))) <= 80
                             AND source.[NumberCount] = 1
                            THEN LTRIM(RTRIM(source.[SlipNumber]))
                        ELSE CONCAT('OS-LEGACY-', RIGHT(REPLICATE('0', 10) + CONVERT(varchar(10), target.[Id]), 10))
                    END,
                    [GeneratedAt] = target.[DateGenerated],
                    [Status] = CASE WHEN target.[IsReceived] = 1 THEN 'Completed' ELSE 'Ordered' END,
                    [OrderedAt] = CASE WHEN target.[IsReceived] = 0 THEN target.[DateGenerated] ELSE NULL END,
                    [CompletedAt] = CASE WHEN target.[IsReceived] = 1 THEN target.[DateGenerated] ELSE NULL END,
                    [LocationId] = 'MAIN'
                FROM [OrderSlips] AS target
                INNER JOIN LegacyNumbers AS source ON source.[Id] = target.[Id];
                """);

            // Match a historical item only when ProductName + Brand identifies exactly one product.
            // Ambiguous or missing matches are deliberately left as zero and rejected below rather
            // than silently linking historical purchasing data to an arbitrary product.
            migrationBuilder.Sql(
                """
                ;WITH ProductCandidates AS
                (
                    SELECT
                        item.[Id] AS [OrderSlipItemId],
                        MIN(product.[Id]) AS [ProductId],
                        COUNT_BIG(*) AS [CandidateCount]
                    FROM [OrderSlipItems] AS item
                    INNER JOIN [Products] AS product
                        ON LTRIM(RTRIM(product.[Name])) = LTRIM(RTRIM(item.[ProductName]))
                        AND LTRIM(RTRIM(product.[Brand])) = LTRIM(RTRIM(item.[Brand]))
                    GROUP BY item.[Id]
                )
                UPDATE item
                SET
                    [ProductId] = candidates.[ProductId],
                    [CurrentStockSnapshot] = CASE WHEN item.[CurrentStock] < 0 THEN 0 ELSE item.[CurrentStock] END,
                    [InventoryPositionSnapshot] = CASE WHEN item.[CurrentStock] < 0 THEN 0 ELSE item.[CurrentStock] END,
                    [ReorderPointSnapshot] = CASE WHEN item.[ReorderTarget] < 0 THEN 0 ELSE item.[ReorderTarget] END,
                    [TargetStockSnapshot] = CASE WHEN item.[ReorderTarget] < 0 THEN 0 ELSE item.[ReorderTarget] END,
                    [SuggestedQuantity] = CASE WHEN item.[Quantity] < 1 THEN 1 ELSE item.[Quantity] END,
                    [OrderedQuantity] = CASE
                        WHEN item.[Quantity] < item.[ReceivedQuantity] THEN item.[ReceivedQuantity]
                        WHEN item.[Quantity] < 1 THEN 1
                        ELSE item.[Quantity]
                    END,
                    [PackageSizeSnapshot] = 1,
                    [MinimumOrderQuantitySnapshot] = 1,
                    [UnitCostSnapshot] = CASE WHEN product.[UnitCost] < 0 THEN 0 ELSE product.[UnitCost] END,
                    [EstimatedLineTotal] =
                        CONVERT(decimal(18,2), CASE
                            WHEN item.[Quantity] < item.[ReceivedQuantity] THEN item.[ReceivedQuantity]
                            WHEN item.[Quantity] < 1 THEN 1
                            ELSE item.[Quantity]
                        END * CASE WHEN product.[UnitCost] < 0 THEN 0 ELSE product.[UnitCost] END),
                    [RecommendationReason] = CASE
                        WHEN NULLIF(LTRIM(RTRIM(item.[Reasoning])), '') IS NULL
                            THEN 'Migrated from the legacy order-slip workflow.'
                        ELSE LEFT(item.[Reasoning], 500)
                    END
                FROM [OrderSlipItems] AS item
                INNER JOIN ProductCandidates AS candidates
                    ON candidates.[OrderSlipItemId] = item.[Id]
                    AND candidates.[CandidateCount] = 1
                INNER JOIN [Products] AS product ON product.[Id] = candidates.[ProductId];

                IF EXISTS (SELECT 1 FROM [OrderSlipItems] WHERE [ProductId] = 0)
                BEGIN
                    THROW 51000, 'Safety-stock migration could not uniquely match every legacy order-slip item to a product by ProductName and Brand. Resolve missing or duplicate product matches, then retry the migration.', 1;
                END;

                IF EXISTS
                (
                    SELECT 1
                    FROM [OrderSlipItems]
                    GROUP BY [OrderSlipId], [ProductId]
                    HAVING COUNT_BIG(*) > 1
                )
                BEGIN
                    THROW 51001, 'Safety-stock migration found duplicate legacy items for the same product in one order slip. Consolidate those historical duplicates, then retry the migration.', 1;
                END;

                UPDATE slip
                SET [TotalEstimatedCost] = totals.[TotalEstimatedCost]
                FROM [OrderSlips] AS slip
                INNER JOIN
                (
                    SELECT [OrderSlipId], SUM([EstimatedLineTotal]) AS [TotalEstimatedCost]
                    FROM [OrderSlipItems]
                    GROUP BY [OrderSlipId]
                ) AS totals ON totals.[OrderSlipId] = slip.[Id];
                """);

            // Seed one MAIN setting for every existing product without changing Products.ReorderTarget.
            // 2026-07-26 is the deterministic feature-introduction fallback for products with no sale.
            migrationBuilder.Sql(
                """
                INSERT INTO [ProductInventorySettings]
                (
                    [ProductId], [LocationId], [CalculationMode], [InitialEstimatedWeeklyDemand],
                    [DefaultLeadTimeDays], [ReviewPeriodDays], [BufferDays], [ServiceLevel],
                    [MinimumSafetyStock], [MaximumSafetyStock], [MinimumOrderQuantity], [PackageSize],
                    [MaximumStockLevel], [ManualSafetyStock], [ManualReorderPoint],
                    [IsAutomaticOrderEnabled], [InventoryTrackingStartDate], [CreatedAt], [UpdatedAt]
                )
                SELECT
                    product.[Id],
                    'MAIN',
                    'Auto',
                    CONVERT(decimal(18,4), 0),
                    7,
                    7,
                    7,
                    CONVERT(decimal(6,4), 0.9500),
                    0,
                    NULL,
                    1,
                    1,
                    NULL,
                    NULL,
                    NULL,
                    1,
                    COALESCE(firstSale.[FirstSaleDate], CONVERT(datetime2, '2026-07-26T00:00:00', 126)),
                    CONVERT(datetime2, '2026-07-26T00:00:00', 126),
                    CONVERT(datetime2, '2026-07-26T00:00:00', 126)
                FROM [Products] AS product
                OUTER APPLY
                (
                    SELECT MIN(transactionRow.[TransactionDate]) AS [FirstSaleDate]
                    FROM [TransactionItems] AS item
                    INNER JOIN [Transactions] AS transactionRow
                        ON transactionRow.[Id] = item.[TransactionId]
                    WHERE item.[ProductId] = product.[Id]
                      AND UPPER(LTRIM(RTRIM(transactionRow.[TransactionType]))) = 'SALE'
                      AND UPPER(LTRIM(RTRIM(transactionRow.[LocationId]))) = 'MAIN'
                ) AS firstSale;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_LocationId",
                table: "Transactions",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_LocationId_TransactionType_TransactionDate",
                table: "Transactions",
                columns: new[] { "LocationId", "TransactionType", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_OrderSlipId",
                table: "Transactions",
                column: "OrderSlipId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_TransactionDate",
                table: "Transactions",
                column: "TransactionDate");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_TransactionType",
                table: "Transactions",
                column: "TransactionType");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionItems_OrderSlipItemId",
                table: "TransactionItems",
                column: "OrderSlipItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionItems_TransactionId",
                table: "TransactionItems",
                column: "TransactionId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TransactionItems_DemandAudit",
                table: "TransactionItems",
                sql: "[LostSalesQuantity] >= 0 AND ([RequestedQuantity] IS NULL OR [RequestedQuantity] >= [Quantity])");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TransactionItems_StockAudit",
                table: "TransactionItems",
                sql: "[StockBefore] >= 0 AND [StockAfter] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_OrderSlips_OrderSlipNumber",
                table: "OrderSlips",
                column: "OrderSlipNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderSlips_SupplierId_LocationId_Status",
                table: "OrderSlips",
                columns: new[] { "SupplierId", "LocationId", "Status" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderSlips_Status",
                table: "OrderSlips",
                sql: "[Status] IN ('Draft','Approved','Ordered','PartiallyReceived','Completed','Cancelled')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderSlips_TotalEstimatedCost",
                table: "OrderSlips",
                sql: "[TotalEstimatedCost] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_OrderSlipItems_OrderSlipId_ProductId",
                table: "OrderSlipItems",
                columns: new[] { "OrderSlipId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderSlipItems_ProductId",
                table: "OrderSlipItems",
                column: "ProductId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderSlipItems_Amounts",
                table: "OrderSlipItems",
                sql: "[UnitCostSnapshot] >= 0 AND [EstimatedLineTotal] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderSlipItems_OrderRules",
                table: "OrderSlipItems",
                sql: "[PackageSizeSnapshot] >= 1 AND [MinimumOrderQuantitySnapshot] >= 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderSlipItems_Quantities",
                table: "OrderSlipItems",
                sql: "[OrderedQuantity] > 0 AND [SuggestedQuantity] >= 0 AND [ReceivedQuantity] >= 0 AND [ReceivedQuantity] <= [OrderedQuantity]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderSlipItems_StockSnapshots",
                table: "OrderSlipItems",
                sql: "[CurrentStockSnapshot] >= 0 AND [IncomingStockSnapshot] >= 0 AND [ReservedStockSnapshot] >= 0 AND [BackorderStockSnapshot] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductInventoryMetrics_ProductId_LocationId",
                table: "ProductInventoryMetrics",
                columns: new[] { "ProductId", "LocationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductInventorySettings_ProductId_LocationId",
                table: "ProductInventorySettings",
                columns: new[] { "ProductId", "LocationId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderSlipItems_Products_ProductId",
                table: "OrderSlipItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderSlips_Suppliers_SupplierId",
                table: "OrderSlips",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Suppliers_SupplierId",
                table: "Products",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionItems_OrderSlipItems_OrderSlipItemId",
                table: "TransactionItems",
                column: "OrderSlipItemId",
                principalTable: "OrderSlipItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_OrderSlips_OrderSlipId",
                table: "Transactions",
                column: "OrderSlipId",
                principalTable: "OrderSlips",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderSlipItems_Products_ProductId",
                table: "OrderSlipItems");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderSlips_Suppliers_SupplierId",
                table: "OrderSlips");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Suppliers_SupplierId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_TransactionItems_OrderSlipItems_OrderSlipItemId",
                table: "TransactionItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_OrderSlips_OrderSlipId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "ProductInventoryMetrics");

            migrationBuilder.DropTable(
                name: "ProductInventorySettings");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_LocationId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_LocationId_TransactionType_TransactionDate",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_OrderSlipId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_TransactionDate",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_TransactionType",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_TransactionItems_OrderSlipItemId",
                table: "TransactionItems");

            migrationBuilder.DropIndex(
                name: "IX_TransactionItems_TransactionId",
                table: "TransactionItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TransactionItems_DemandAudit",
                table: "TransactionItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TransactionItems_StockAudit",
                table: "TransactionItems");

            migrationBuilder.DropIndex(
                name: "IX_OrderSlips_OrderSlipNumber",
                table: "OrderSlips");

            migrationBuilder.DropIndex(
                name: "IX_OrderSlips_SupplierId_LocationId_Status",
                table: "OrderSlips");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderSlips_Status",
                table: "OrderSlips");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderSlips_TotalEstimatedCost",
                table: "OrderSlips");

            migrationBuilder.DropIndex(
                name: "IX_OrderSlipItems_OrderSlipId_ProductId",
                table: "OrderSlipItems");

            migrationBuilder.DropIndex(
                name: "IX_OrderSlipItems_ProductId",
                table: "OrderSlipItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderSlipItems_Amounts",
                table: "OrderSlipItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderSlipItems_OrderRules",
                table: "OrderSlipItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderSlipItems_Quantities",
                table: "OrderSlipItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderSlipItems_StockSnapshots",
                table: "OrderSlipItems");

            migrationBuilder.DropColumn(
                name: "OrderSlipId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "LostSalesQuantity",
                table: "TransactionItems");

            migrationBuilder.DropColumn(
                name: "OrderSlipItemId",
                table: "TransactionItems");

            migrationBuilder.DropColumn(
                name: "RequestedQuantity",
                table: "TransactionItems");

            migrationBuilder.DropColumn(
                name: "StockoutOccurred",
                table: "TransactionItems");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "OrderSlips");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "OrderSlips");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "OrderSlips");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "OrderSlips");

            migrationBuilder.DropColumn(
                name: "ExpectedDeliveryDate",
                table: "OrderSlips");

            migrationBuilder.DropColumn(
                name: "GeneratedAt",
                table: "OrderSlips");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "OrderSlips");

            migrationBuilder.DropColumn(
                name: "OrderSlipNumber",
                table: "OrderSlips");

            migrationBuilder.DropColumn(
                name: "OrderedAt",
                table: "OrderSlips");

            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "OrderSlips");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "OrderSlips");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "OrderSlips");

            migrationBuilder.DropColumn(
                name: "TotalEstimatedCost",
                table: "OrderSlips");

            migrationBuilder.DropColumn(
                name: "AverageDailyDemandSnapshot",
                table: "OrderSlipItems");

            migrationBuilder.DropColumn(
                name: "BackorderStockSnapshot",
                table: "OrderSlipItems");

            migrationBuilder.DropColumn(
                name: "CurrentStockSnapshot",
                table: "OrderSlipItems");

            migrationBuilder.DropColumn(
                name: "EstimatedLineTotal",
                table: "OrderSlipItems");

            migrationBuilder.DropColumn(
                name: "IncomingStockSnapshot",
                table: "OrderSlipItems");

            migrationBuilder.DropColumn(
                name: "InventoryPositionSnapshot",
                table: "OrderSlipItems");

            migrationBuilder.DropColumn(
                name: "LeadTimeDaysSnapshot",
                table: "OrderSlipItems");

            migrationBuilder.DropColumn(
                name: "MinimumOrderQuantitySnapshot",
                table: "OrderSlipItems");

            migrationBuilder.DropColumn(
                name: "OrderedQuantity",
                table: "OrderSlipItems");

            migrationBuilder.DropColumn(
                name: "PackageSizeSnapshot",
                table: "OrderSlipItems");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "OrderSlipItems");

            migrationBuilder.DropColumn(
                name: "RecommendationReason",
                table: "OrderSlipItems");

            migrationBuilder.DropColumn(
                name: "ReorderPointSnapshot",
                table: "OrderSlipItems");

            migrationBuilder.DropColumn(
                name: "ReservedStockSnapshot",
                table: "OrderSlipItems");

            migrationBuilder.DropColumn(
                name: "SafetyStockSnapshot",
                table: "OrderSlipItems");

            migrationBuilder.DropColumn(
                name: "SuggestedQuantity",
                table: "OrderSlipItems");

            migrationBuilder.DropColumn(
                name: "TargetStockSnapshot",
                table: "OrderSlipItems");

            migrationBuilder.DropColumn(
                name: "UnitCostSnapshot",
                table: "OrderSlipItems");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TransactionItems_StockAudit",
                table: "TransactionItems",
                sql: "[StockBefore] >= 0 AND [StockAfter] >= 0 AND [StockAfter] <= [StockBefore]");

            migrationBuilder.CreateIndex(
                name: "IX_OrderSlips_SupplierId",
                table: "OrderSlips",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderSlipItems_OrderSlipId",
                table: "OrderSlipItems",
                column: "OrderSlipId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderSlips_Suppliers_SupplierId",
                table: "OrderSlips",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Suppliers_SupplierId",
                table: "Products",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id");
        }
    }
}
