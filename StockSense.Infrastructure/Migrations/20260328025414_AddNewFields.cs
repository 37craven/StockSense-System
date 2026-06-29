using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSense.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddNewFields : Migration
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
            //    name: "Status",
            //    table: "StoreServices",
            //    type: "nvarchar(max)",
            //    nullable: false,
            //    defaultValue: "");

            //migrationBuilder.AddColumn<string>(
            //    name: "Role",
            //    table: "AspNetUsers",
            //    type: "nvarchar(max)",
            //    nullable: false,
            //    defaultValue: "");

            //migrationBuilder.AddColumn<DateTime>(
            //    name: "CreatedAt",
            //    table: "Appointments",
            //    type: "datetime2",
            //    nullable: false,
            //    defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            //migrationBuilder.AddColumn<int>(
            //    name: "DurationMinutes",
            //    table: "Appointments",
            //    type: "int",
            //    nullable: false,
            //    defaultValue: 0);

            //migrationBuilder.AddColumn<string>(
            //    name: "MechanicName",
            //    table: "Appointments",
            //    type: "nvarchar(max)",
            //    nullable: false,
            //    defaultValue: "");

            //migrationBuilder.CreateTable(
            //    name: "Mechanics",
            //    columns: table => new
            //    {
            //        Id = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //        IsActive = table.Column<bool>(type: "bit", nullable: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_Mechanics", x => x.Id);
            //    });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Mechanics");

            migrationBuilder.DropColumn(
                name: "EstimatedMinutes",
                table: "StoreServices");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "StoreServices");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "DurationMinutes",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "MechanicName",
                table: "Appointments");
        }
    }
}
