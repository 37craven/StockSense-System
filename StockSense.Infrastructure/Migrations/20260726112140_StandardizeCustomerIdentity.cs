using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSense.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StandardizeCustomerIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CustomerName",
                table: "BuildRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "CustomerEmail",
                table: "BuildRequests",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerUserId",
                table: "BuildRequests",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerName",
                table: "Appointments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "CustomerEmail",
                table: "Appointments",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerUserId",
                table: "Appointments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BuildRequests_CustomerEmail",
                table: "BuildRequests",
                column: "CustomerEmail");

            migrationBuilder.CreateIndex(
                name: "IX_BuildRequests_CustomerUserId",
                table: "BuildRequests",
                column: "CustomerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_CustomerEmail",
                table: "Appointments",
                column: "CustomerEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_CustomerUserId",
                table: "Appointments",
                column: "CustomerUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BuildRequests_CustomerEmail",
                table: "BuildRequests");

            migrationBuilder.DropIndex(
                name: "IX_BuildRequests_CustomerUserId",
                table: "BuildRequests");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_CustomerEmail",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_CustomerUserId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "CustomerEmail",
                table: "BuildRequests");

            migrationBuilder.DropColumn(
                name: "CustomerUserId",
                table: "BuildRequests");

            migrationBuilder.DropColumn(
                name: "CustomerEmail",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "CustomerUserId",
                table: "Appointments");

            migrationBuilder.AlterColumn<string>(
                name: "CustomerName",
                table: "BuildRequests",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerName",
                table: "Appointments",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);
        }
    }
}
