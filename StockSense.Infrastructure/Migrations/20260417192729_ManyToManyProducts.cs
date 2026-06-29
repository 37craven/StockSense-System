using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSense.Web.Migrations
{
    /// <inheritdoc />
    public partial class ManyToManyProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.CreateTable(
                name: "ProductStoreService",
                columns: table => new
                {
                    RequiredProductsId = table.Column<int>(type: "int", nullable: false),
                    StoreServicesId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductStoreService", x => new { x.RequiredProductsId, x.StoreServicesId });
                    table.ForeignKey(
                        name: "FK_ProductStoreService_Products_RequiredProductsId",
                        column: x => x.RequiredProductsId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductStoreService_StoreServices_StoreServicesId",
                        column: x => x.StoreServicesId,
                        principalTable: "StoreServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductStoreService_StoreServicesId",
                table: "ProductStoreService",
                column: "StoreServicesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductStoreService");

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
    }
}
