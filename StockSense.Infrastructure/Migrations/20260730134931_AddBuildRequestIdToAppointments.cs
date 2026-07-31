using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSense.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBuildRequestIdToAppointments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BuildRequestId",
                table: "Appointments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_BuildRequestId",
                table: "Appointments",
                column: "BuildRequestId",
                unique: true,
                filter: "[BuildRequestId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_BuildRequests_BuildRequestId",
                table: "Appointments",
                column: "BuildRequestId",
                principalTable: "BuildRequests",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_BuildRequests_BuildRequestId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_BuildRequestId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "BuildRequestId",
                table: "Appointments");
        }
    }
}
