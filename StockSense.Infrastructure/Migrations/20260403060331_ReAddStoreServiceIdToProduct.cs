using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSense.Web.Migrations
{
    /// <inheritdoc />
    public partial class ReAddStoreServiceIdToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StoreServiceId",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_StoreServiceId",
                table: "Products",
                column: "StoreServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_StoreServices_StoreServiceId",
                table: "Products",
                column: "StoreServiceId",
                principalTable: "StoreServices",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_StoreServices_StoreServiceId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_StoreServiceId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "StoreServiceId",
                table: "Products");
        }
    }
}
