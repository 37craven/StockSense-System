using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSense.Web.Migrations
{
    /// <inheritdoc />
    public partial class dbupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ////migrationBuilder.DropForeignKey(
            ////    name: "FK_OrderSlips_Supplier_SupplierId",
            ////    table: "OrderSlips");

            ////migrationBuilder.DropForeignKey(
            ////    name: "FK_Products_Supplier_SupplierId",
            ////    table: "Products");

            //migrationBuilder.DropPrimaryKey(
            //    name: "PK_Supplier",
            //    table: "Supplier");

            //migrationBuilder.RenameTable(
            //    name: "Supplier",
            //    newName: "Suppliers");

            //migrationBuilder.AddPrimaryKey(
            //    name: "PK_Suppliers",
            //    table: "Suppliers",
            //    column: "Id");

            //migrationBuilder.AddForeignKey(
            //    name: "FK_OrderSlips_Suppliers_SupplierId",
            //    table: "OrderSlips",
            //    column: "SupplierId",
            //    principalTable: "Suppliers",
            //    principalColumn: "Id",
            //    onDelete: ReferentialAction.Cascade);

            //migrationBuilder.AddForeignKey(
            //    name: "FK_Products_Suppliers_SupplierId",
            //    table: "Products",
            //    column: "SupplierId",
            //    principalTable: "Suppliers",
            //    principalColumn: "Id",
            //    onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderSlips_Suppliers_SupplierId",
                table: "OrderSlips");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Suppliers_SupplierId",
                table: "Products");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Suppliers",
                table: "Suppliers");

            migrationBuilder.RenameTable(
                name: "Suppliers",
                newName: "Supplier");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Supplier",
                table: "Supplier",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderSlips_Supplier_SupplierId",
                table: "OrderSlips",
                column: "SupplierId",
                principalTable: "Supplier",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Supplier_SupplierId",
                table: "Products",
                column: "SupplierId",
                principalTable: "Supplier",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
