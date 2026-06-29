using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSense.Web.Migrations
{
    /// <inheritdoc />
    public partial class dbappointment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.AddColumn<string>(
            //    name: "Category",
            //    table: "StoreServices",
            //    type: "nvarchar(max)",
            //    nullable: false,
            //    defaultValue: "");

            //migrationBuilder.AddColumn<string>(
            //    name: "Category",
            //    table: "Appointments",
            //    type: "nvarchar(max)",
            //    nullable: false,
            //    defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "StoreServices");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Appointments");
        }
    }
}
