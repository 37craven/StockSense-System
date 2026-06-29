using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSense.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // --- SYNC MODE ENABLED ---
            /* We are leaving this method EMPTY because your SQL database 
               already contains the tables (OrderSlips, Appointments, AspNetUsers, etc.).
               
               When you run the app, the 'Automatic Migration Helper' in your Program.cs 
               will run this empty method. It won't crash because there are no commands, 
               but it WILL add a row to the [__EFMigrationsHistory] table.
               
               This successfully 'Syncs' your code and your database.
            */
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // We keep the Down method populated so you can 
            // drop the tables later if you ever want a total reset.
            migrationBuilder.DropTable(name: "OrderSlipItems");
            migrationBuilder.DropTable(name: "OrderSlips");
            migrationBuilder.DropTable(name: "Appointments");
            migrationBuilder.DropTable(name: "Products");
            migrationBuilder.DropTable(name: "StoreServices");
            migrationBuilder.DropTable(name: "Suppliers");
            migrationBuilder.DropTable(name: "Mechanics");
            migrationBuilder.DropTable(name: "BuildRequests");
            migrationBuilder.DropTable(name: "AspNetRoles");
            migrationBuilder.DropTable(name: "AspNetUsers");
        }
    }
}
