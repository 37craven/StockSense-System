using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSense.Web.Migrations
{
    /// <inheritdoc />
    public partial class RenamePreBuildToPreBuilt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "PreBuildPackageProduct",
                newName: "PreBuiltPackageProduct");

            migrationBuilder.RenameTable(
                name: "PreBuildPackages",
                newName: "PreBuiltPackages");

            migrationBuilder.RenameColumn(
                name: "PreBuildPackagesId",
                table: "PreBuiltPackageProduct",
                newName: "PreBuiltPackagesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PreBuiltPackagesId",
                table: "PreBuiltPackageProduct",
                newName: "PreBuildPackagesId");

            migrationBuilder.RenameTable(
                name: "PreBuiltPackageProduct",
                newName: "PreBuildPackageProduct");

            migrationBuilder.RenameTable(
                name: "PreBuiltPackages",
                newName: "PreBuildPackages");
        }
    }
}
