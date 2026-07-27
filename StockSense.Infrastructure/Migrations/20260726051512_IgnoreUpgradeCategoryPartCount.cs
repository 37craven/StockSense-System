using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSense.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IgnoreUpgradeCategoryPartCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PartCount",
                table: "UpgradeCategories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PartCount",
                table: "UpgradeCategories",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
