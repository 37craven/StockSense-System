using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSense.Infrastructure.Migrations
{
    public partial class RestoreLegacyOrderSlipFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateGenerated",
                table: "OrderSlips",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<bool>(
                name: "IsReceived",
                table: "OrderSlips",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SlipNumber",
                table: "OrderSlips",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "PinnedSlips",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SlipData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PinnedSlips", x => x.Id);
                });

            if (migrationBuilder.ActiveProvider?.Contains("SqlServer") == true)
            {
                migrationBuilder.Sql("UPDATE [OrderSlips] SET [SlipNumber] = [OrderSlipNumber], [DateGenerated] = [GeneratedAt] WHERE [SlipNumber] = '' OR [SlipNumber] IS NULL");
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PinnedSlips");

            migrationBuilder.DropColumn(name: "DateGenerated", table: "OrderSlips");
            migrationBuilder.DropColumn(name: "IsReceived", table: "OrderSlips");
            migrationBuilder.DropColumn(name: "SlipNumber", table: "OrderSlips");
        }
    }
}
