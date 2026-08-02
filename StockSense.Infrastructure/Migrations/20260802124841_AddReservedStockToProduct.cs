using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSense.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReservedStockToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Motorcycles");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns
                    WHERE Name = N'ReservedStock' AND Object_ID = Object_ID(N'Products'))
                BEGIN
                    ALTER TABLE [Products] ADD [ReservedStock] int NOT NULL DEFAULT 0;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReservedStock",
                table: "Products");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Motorcycles",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
