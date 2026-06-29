using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSense.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddYearToSalesHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "Year",
                table: "SalesHistory",
                type: "real",
                nullable: false,
                defaultValue: 0f);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Year",
                table: "SalesHistory");
        }
    }
}
