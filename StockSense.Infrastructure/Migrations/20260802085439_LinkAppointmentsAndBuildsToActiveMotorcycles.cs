using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSense.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LinkAppointmentsAndBuildsToActiveMotorcycles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Motorcycles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MotorcycleId",
                table: "BuildRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MotorcycleId",
                table: "Appointments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BuildRequests_MotorcycleId",
                table: "BuildRequests",
                column: "MotorcycleId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_MotorcycleId",
                table: "Appointments",
                column: "MotorcycleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Motorcycles_MotorcycleId",
                table: "Appointments",
                column: "MotorcycleId",
                principalTable: "Motorcycles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BuildRequests_Motorcycles_MotorcycleId",
                table: "BuildRequests",
                column: "MotorcycleId",
                principalTable: "Motorcycles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Motorcycles_MotorcycleId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_BuildRequests_Motorcycles_MotorcycleId",
                table: "BuildRequests");

            migrationBuilder.DropIndex(
                name: "IX_BuildRequests_MotorcycleId",
                table: "BuildRequests");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_MotorcycleId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Motorcycles");

            migrationBuilder.DropColumn(
                name: "MotorcycleId",
                table: "BuildRequests");

            migrationBuilder.DropColumn(
                name: "MotorcycleId",
                table: "Appointments");
        }
    }
}
