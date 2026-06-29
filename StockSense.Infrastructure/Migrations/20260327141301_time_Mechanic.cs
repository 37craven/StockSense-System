using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSense.Web.Migrations
{
    /// <inheritdoc />
    public partial class time_Mechanic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.AddColumn<int>(
            //    name: "EstimatedMinutes",
            //    table: "StoreServices",
            //    type: "int",
            //    nullable: false,
            //    defaultValue: 0);

            //migrationBuilder.AddColumn<string>(
            //    name: "MechanicName",
            //    table: "Appointments",
            //    type: "nvarchar(max)",
            //    nullable: false,
            //    defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimatedMinutes",
                table: "StoreServices");

            migrationBuilder.DropColumn(
                name: "MechanicName",
                table: "Appointments");
        }
    }
}
