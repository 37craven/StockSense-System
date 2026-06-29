using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSense.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // --- SYNC MODE: DO NOT REMOVE THESE COMMENTS ---
            /* We are leaving the Up method EMPTY here because 'OrderSlips', 
               'OrderSlipItems', and the 'IsReceived' column ALREADY exist 
               in your database from a previous (manual) update.
               
               Running this empty will simply 'check the box' in the 
               [__EFMigrationsHistory] table so your app can start.
            */
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Down can stay populated in case you ever need to wipe the DB
            migrationBuilder.DropTable(name: "OrderSlipItems");
            migrationBuilder.DropTable(name: "OrderSlips");
        }
    }
}
