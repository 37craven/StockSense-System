using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSense.Web.Migrations
{
    /// <inheritdoc />
    public partial class addnewsaleshistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SalesHistories",
                table: "SalesHistories");

            migrationBuilder.RenameTable(
                name: "SalesHistories",
                newName: "SalesHistory");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SalesHistory",
                table: "SalesHistory",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SalesHistory",
                table: "SalesHistory");

            migrationBuilder.RenameTable(
                name: "SalesHistory",
                newName: "SalesHistories");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SalesHistories",
                table: "SalesHistories",
                column: "Id");
        }
    }
}
