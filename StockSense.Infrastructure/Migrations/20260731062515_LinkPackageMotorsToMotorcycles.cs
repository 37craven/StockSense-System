using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSense.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LinkPackageMotorsToMotorcycles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MotorcycleId",
                table: "PreBuiltPackageMotor",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PreBuiltPackageMotor_MotorcycleId",
                table: "PreBuiltPackageMotor",
                column: "MotorcycleId");

            migrationBuilder.AddForeignKey(
                name: "FK_PreBuiltPackageMotor_Motorcycles_MotorcycleId",
                table: "PreBuiltPackageMotor",
                column: "MotorcycleId",
                principalTable: "Motorcycles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PreBuiltPackageMotor_Motorcycles_MotorcycleId",
                table: "PreBuiltPackageMotor");

            migrationBuilder.DropIndex(
                name: "IX_PreBuiltPackageMotor_MotorcycleId",
                table: "PreBuiltPackageMotor");

            migrationBuilder.DropColumn(
                name: "MotorcycleId",
                table: "PreBuiltPackageMotor");
        }
    }
}
