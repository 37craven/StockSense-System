using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSense.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitCostAndRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransactionItem_Transactions_TransactionId",
                table: "TransactionItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TransactionItem",
                table: "TransactionItem");

            migrationBuilder.DropIndex(
                name: "IX_TransactionItem_TransactionId",
                table: "TransactionItem");

            migrationBuilder.RenameTable(
                name: "TransactionItem",
                newName: "TransactionItems");

            migrationBuilder.AlterColumn<string>(
                name: "InvoiceNumber",
                table: "Transactions",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "Transactions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "LocationId",
                table: "Transactions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "MAIN");

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "Transactions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Cash");

            migrationBuilder.AddColumn<string>(
                name: "ReferenceNumber",
                table: "Transactions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "Transactions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransactionType",
                table: "Transactions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Sale");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Transactions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Products",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Brand",
                table: "Products",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Products",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitCost",
                table: "Products",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "ProductName",
                table: "TransactionItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "TransactionItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LineTotal",
                table: "TransactionItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "StockAfter",
                table: "TransactionItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StockBefore",
                table: "TransactionItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitCost",
                table: "TransactionItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TransactionItems",
                table: "TransactionItems",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "ReportingProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Brand = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportingProducts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesImportBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceSystem = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ContentSha256 = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RowsRead = table.Column<int>(type: "int", nullable: false),
                    RowsInserted = table.Column<int>(type: "int", nullable: false),
                    RowsUpdated = table.Column<int>(type: "int", nullable: false),
                    ReportingProductsCreated = table.Column<int>(type: "int", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesImportBatches", x => x.Id);
                    table.CheckConstraint("CK_SalesImportBatches_RowCounts", "[RowsRead] >= 0 AND [RowsInserted] >= 0 AND [RowsUpdated] >= 0 AND [ReportingProductsCreated] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "HistoricalProductMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportingProductId = table.Column<int>(type: "int", nullable: false),
                    SourceSystem = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ExternalProductKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricalProductMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoricalProductMappings_ReportingProducts_ReportingProductId",
                        column: x => x.ReportingProductId,
                        principalTable: "ReportingProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LiveProductMappings",
                columns: table => new
                {
                    ReportingProductId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    UseTransactionsFrom = table.Column<DateTime>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiveProductMappings", x => x.ReportingProductId);
                    table.ForeignKey(
                        name: "FK_LiveProductMappings_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LiveProductMappings_ReportingProducts_ReportingProductId",
                        column: x => x.ReportingProductId,
                        principalTable: "ReportingProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistoricalMonthlyProductSales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportingProductId = table.Column<int>(type: "int", nullable: false),
                    SalesImportBatchId = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<short>(type: "smallint", nullable: false),
                    Month = table.Column<byte>(type: "tinyint", nullable: false),
                    QuantitySold = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricalMonthlyProductSales", x => x.Id);
                    table.CheckConstraint("CK_HistoricalMonthlyProductSales_Month", "[Month] BETWEEN 1 AND 12");
                    table.CheckConstraint("CK_HistoricalMonthlyProductSales_QuantitySold", "[QuantitySold] >= 0");
                    table.CheckConstraint("CK_HistoricalMonthlyProductSales_Year", "[Year] BETWEEN 1900 AND 9999");
                    table.ForeignKey(
                        name: "FK_HistoricalMonthlyProductSales_ReportingProducts_ReportingProductId",
                        column: x => x.ReportingProductId,
                        principalTable: "ReportingProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HistoricalMonthlyProductSales_SalesImportBatches_SalesImportBatchId",
                        column: x => x.SalesImportBatchId,
                        principalTable: "SalesImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_InvoiceNumber",
                table: "Transactions",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_TransactionType_TransactionDate",
                table: "Transactions",
                columns: new[] { "TransactionType", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserId",
                table: "Transactions",
                column: "UserId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Transactions_DiscountAmount",
                table: "Transactions",
                sql: "[DiscountAmount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Transactions_TotalAmount",
                table: "Transactions",
                sql: "[TotalAmount] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Category_Brand",
                table: "Products",
                columns: new[] { "Category", "Brand" });

            migrationBuilder.CreateIndex(
                name: "IX_TransactionItems_ProductId",
                table: "TransactionItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionItems_TransactionId_ProductId",
                table: "TransactionItems",
                columns: new[] { "TransactionId", "ProductId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_TransactionItems_Amounts",
                table: "TransactionItems",
                sql: "[UnitPrice] >= 0 AND [UnitCost] >= 0 AND [DiscountAmount] >= 0 AND [LineTotal] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TransactionItems_Quantity",
                table: "TransactionItems",
                sql: "[Quantity] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TransactionItems_StockAudit",
                table: "TransactionItems",
                sql: "[StockBefore] >= 0 AND [StockAfter] >= 0 AND [StockAfter] <= [StockBefore]");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalMonthlyProductSales_ReportingProductId_Year_Month",
                table: "HistoricalMonthlyProductSales",
                columns: new[] { "ReportingProductId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalMonthlyProductSales_SalesImportBatchId",
                table: "HistoricalMonthlyProductSales",
                column: "SalesImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalProductMappings_ReportingProductId_SourceSystem",
                table: "HistoricalProductMappings",
                columns: new[] { "ReportingProductId", "SourceSystem" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalProductMappings_SourceSystem_ExternalProductKey",
                table: "HistoricalProductMappings",
                columns: new[] { "SourceSystem", "ExternalProductKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LiveProductMappings_ProductId",
                table: "LiveProductMappings",
                column: "ProductId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportingProducts_Name",
                table: "ReportingProducts",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_SalesImportBatches_SourceSystem_ContentSha256",
                table: "SalesImportBatches",
                columns: new[] { "SourceSystem", "ContentSha256" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionItems_Products_ProductId",
                table: "TransactionItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionItems_Transactions_TransactionId",
                table: "TransactionItems",
                column: "TransactionId",
                principalTable: "Transactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_AspNetUsers_UserId",
                table: "Transactions",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransactionItems_Products_ProductId",
                table: "TransactionItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TransactionItems_Transactions_TransactionId",
                table: "TransactionItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_AspNetUsers_UserId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "HistoricalMonthlyProductSales");

            migrationBuilder.DropTable(
                name: "HistoricalProductMappings");

            migrationBuilder.DropTable(
                name: "LiveProductMappings");

            migrationBuilder.DropTable(
                name: "SalesImportBatches");

            migrationBuilder.DropTable(
                name: "ReportingProducts");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_InvoiceNumber",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_TransactionType_TransactionDate",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_UserId",
                table: "Transactions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Transactions_DiscountAmount",
                table: "Transactions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Transactions_TotalAmount",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Products_Category_Brand",
                table: "Products");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TransactionItems",
                table: "TransactionItems");

            migrationBuilder.DropIndex(
                name: "IX_TransactionItems_ProductId",
                table: "TransactionItems");

            migrationBuilder.DropIndex(
                name: "IX_TransactionItems_TransactionId_ProductId",
                table: "TransactionItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TransactionItems_Amounts",
                table: "TransactionItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TransactionItems_Quantity",
                table: "TransactionItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TransactionItems_StockAudit",
                table: "TransactionItems");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ReferenceNumber",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "TransactionType",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "UnitCost",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "TransactionItems");

            migrationBuilder.DropColumn(
                name: "LineTotal",
                table: "TransactionItems");

            migrationBuilder.DropColumn(
                name: "StockAfter",
                table: "TransactionItems");

            migrationBuilder.DropColumn(
                name: "StockBefore",
                table: "TransactionItems");

            migrationBuilder.DropColumn(
                name: "UnitCost",
                table: "TransactionItems");

            migrationBuilder.RenameTable(
                name: "TransactionItems",
                newName: "TransactionItem");

            migrationBuilder.AlterColumn<string>(
                name: "InvoiceNumber",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(80)",
                oldMaxLength: 80);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Brand",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "ProductName",
                table: "TransactionItem",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TransactionItem",
                table: "TransactionItem",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionItem_TransactionId",
                table: "TransactionItem",
                column: "TransactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionItem_Transactions_TransactionId",
                table: "TransactionItem",
                column: "TransactionId",
                principalTable: "Transactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
