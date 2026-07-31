using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSense.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPackageCcRangeAndCompatibleMotors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxAddedCC",
                table: "PreBuiltPackages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PreBuiltPackageMotor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PreBuiltPackageId = table.Column<int>(type: "int", nullable: false),
                    Brand = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Model = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StockCC = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreBuiltPackageMotor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreBuiltPackageMotor_PreBuiltPackages_PreBuiltPackageId",
                        column: x => x.PreBuiltPackageId,
                        principalTable: "PreBuiltPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PreBuiltPackageMotor_PreBuiltPackageId",
                table: "PreBuiltPackageMotor",
                column: "PreBuiltPackageId");

            // Preserve legacy single brand/model/cc + added-cc values into the new shape before dropping the old columns
            migrationBuilder.Sql("""
                INSERT INTO [PreBuiltPackageMotor] ([PreBuiltPackageId], [Brand], [Model], [StockCC])
                SELECT [Id], [CompatibleBrand], [CompatibleModel], [TargetCC]
                FROM [PreBuiltPackages]
                WHERE [CompatibleBrand] <> N'' AND [CompatibleModel] <> N'' AND [TargetCC] <> N'';
                """);

            migrationBuilder.Sql("""
                UPDATE [PreBuiltPackages]
                SET [MinAddedCC] = [EstimatedAddedCC], [MaxAddedCC] = [EstimatedAddedCC]
                WHERE [EstimatedAddedCC] > 0;
                """);

            migrationBuilder.DropColumn(
                name: "CompatibleBrand",
                table: "PreBuiltPackages");

            migrationBuilder.DropColumn(
                name: "CompatibleModel",
                table: "PreBuiltPackages");

            migrationBuilder.DropColumn(
                name: "TargetCC",
                table: "PreBuiltPackages");

            migrationBuilder.RenameColumn(
                name: "EstimatedAddedCC",
                table: "PreBuiltPackages",
                newName: "MinAddedCC");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PreBuiltPackageMotor");

            migrationBuilder.DropColumn(
                name: "MaxAddedCC",
                table: "PreBuiltPackages");

            migrationBuilder.RenameColumn(
                name: "MinAddedCC",
                table: "PreBuiltPackages",
                newName: "EstimatedAddedCC");

            migrationBuilder.AddColumn<string>(
                name: "CompatibleBrand",
                table: "PreBuiltPackages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CompatibleModel",
                table: "PreBuiltPackages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TargetCC",
                table: "PreBuiltPackages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            // Best-effort restore of the first compatible motor and the added-cc range
            migrationBuilder.Sql("""
                UPDATE [PreBuiltPackages]
                SET [CompatibleBrand] = m.[Brand], [CompatibleModel] = m.[Model], [TargetCC] = m.[StockCC]
                FROM [PreBuiltPackages] p
                CROSS APPLY (SELECT TOP 1 * FROM [PreBuiltPackageMotor] WHERE [PreBuiltPackageId] = p.[Id]) m;
                """);
        }
    }
}
