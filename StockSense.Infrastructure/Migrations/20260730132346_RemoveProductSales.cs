using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSense.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProductSales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider?.Contains("SqlServer") == true)
            {
                migrationBuilder.Sql(@"
                    IF OBJECT_ID(N'HistoricalMonthlyProductSales', N'U') IS NOT NULL DROP TABLE [HistoricalMonthlyProductSales];
                    IF OBJECT_ID(N'HistoricalProductMappings', N'U') IS NOT NULL DROP TABLE [HistoricalProductMappings];
                    IF OBJECT_ID(N'LiveProductMappings', N'U') IS NOT NULL DROP TABLE [LiveProductMappings];
                    IF OBJECT_ID(N'SalesHistory', N'U') IS NOT NULL DROP TABLE [SalesHistory];
                    IF OBJECT_ID(N'SalesImportBatches', N'U') IS NOT NULL DROP TABLE [SalesImportBatches];
                    IF OBJECT_ID(N'ReportingProducts', N'U') IS NOT NULL DROP TABLE [ReportingProducts];
                ");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReportingProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Brand = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportingProducts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Brand = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Date = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MonthNum = table.Column<float>(type: "real", nullable: false),
                    ProductID = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QtySold = table.Column<float>(type: "real", nullable: false),
                    TotalSales = table.Column<float>(type: "real", nullable: false),
                    UnitPrice = table.Column<float>(type: "real", nullable: false),
                    Year = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesImportBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    ContentSha256 = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ReportingProductsCreated = table.Column<int>(type: "int", nullable: false),
                    RowsInserted = table.Column<int>(type: "int", nullable: false),
                    RowsRead = table.Column<int>(type: "int", nullable: false),
                    RowsUpdated = table.Column<int>(type: "int", nullable: false),
                    SourceSystem = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
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
                    ExternalProductKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceSystem = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
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
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UseTransactionsFrom = table.Column<DateTime>(type: "date", nullable: false)
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
                    Month = table.Column<byte>(type: "tinyint", nullable: false),
                    QuantitySold = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<short>(type: "smallint", nullable: false)
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
        }
    }
}
