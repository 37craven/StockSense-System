using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSense.Web.Migrations
{
    /// <inheritdoc />
    public partial class editableSlip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.AddColumn<int>(
            //    name: "ReceivedQuantity",
            //    table: "OrderSlipItems",
            //    type: "int",
            //    nullable: false,
            //    defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceivedQuantity",
                table: "OrderSlipItems");
        }
    }
}
