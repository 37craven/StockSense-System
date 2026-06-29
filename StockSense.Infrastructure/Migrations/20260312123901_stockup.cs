using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSense.Web.Migrations
{
    /// <inheritdoc />
    public partial class stockup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // --- SYNC FIX: COMMENTED OUT ---
            /* We are commenting this out because 'IsReceived' already exists 
               in your physical 'OrderSlips' table. 
               By leaving the code inside Up() empty, EF Core will just add 
               a row to [__EFMigrationsHistory] and stop the error.
            */
            /*
            migrationBuilder.AddColumn<bool>(
                name: "IsReceived",
                table: "OrderSlips",
                type: "bit",
                nullable: false,
                defaultValue: false);
            */
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Commented out to match the Up method
            /*
            migrationBuilder.DropColumn(
                name: "IsReceived",
                table: "OrderSlips");
            */
        }
    }
}
