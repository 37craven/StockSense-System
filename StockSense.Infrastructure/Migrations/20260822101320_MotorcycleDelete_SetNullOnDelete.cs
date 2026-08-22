using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSense.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MotorcycleDelete_SetNullOnDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Motorcycles_MotorcycleId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_BuildRequests_Motorcycles_MotorcycleId",
                table: "BuildRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_PreBuiltPackageMotor_Motorcycles_MotorcycleId",
                table: "PreBuiltPackageMotor");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Motorcycles_MotorcycleId",
                table: "Appointments",
                column: "MotorcycleId",
                principalTable: "Motorcycles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_BuildRequests_Motorcycles_MotorcycleId",
                table: "BuildRequests",
                column: "MotorcycleId",
                principalTable: "Motorcycles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PreBuiltPackageMotor_Motorcycles_MotorcycleId",
                table: "PreBuiltPackageMotor",
                column: "MotorcycleId",
                principalTable: "Motorcycles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
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

            migrationBuilder.DropForeignKey(
                name: "FK_PreBuiltPackageMotor_Motorcycles_MotorcycleId",
                table: "PreBuiltPackageMotor");

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

            migrationBuilder.AddForeignKey(
                name: "FK_PreBuiltPackageMotor_Motorcycles_MotorcycleId",
                table: "PreBuiltPackageMotor",
                column: "MotorcycleId",
                principalTable: "Motorcycles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
