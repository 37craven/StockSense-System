using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSense.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBackorderStockFromOrderSlipItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderSlipItems_StockSnapshots",
                table: "OrderSlipItems");

            migrationBuilder.DropColumn(
                name: "BackorderStockSnapshot",
                table: "OrderSlipItems");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderSlipItems_StockSnapshots",
                table: "OrderSlipItems",
                sql: "[CurrentStockSnapshot] >= 0 AND [IncomingStockSnapshot] >= 0 AND [ReservedStockSnapshot] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderSlipItems_StockSnapshots",
                table: "OrderSlipItems");

            migrationBuilder.AddColumn<int>(
                name: "BackorderStockSnapshot",
                table: "OrderSlipItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderSlipItems_StockSnapshots",
                table: "OrderSlipItems",
                sql: "[CurrentStockSnapshot] >= 0 AND [IncomingStockSnapshot] >= 0 AND [ReservedStockSnapshot] >= 0 AND [BackorderStockSnapshot] >= 0");
        }
    }
}
