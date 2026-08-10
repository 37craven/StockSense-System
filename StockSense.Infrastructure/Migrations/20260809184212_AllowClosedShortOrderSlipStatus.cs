using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSense.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AllowClosedShortOrderSlipStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderSlips_Status",
                table: "OrderSlips");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderSlips_Status",
                table: "OrderSlips",
                sql: "[Status] IN ('Draft','Approved','Ordered','PartiallyReceived','ClosedShort','Completed','Cancelled')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderSlips_Status",
                table: "OrderSlips");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderSlips_Status",
                table: "OrderSlips",
                sql: "[Status] IN ('Draft','Approved','Ordered','PartiallyReceived','Completed','Cancelled')");
        }
    }
}
