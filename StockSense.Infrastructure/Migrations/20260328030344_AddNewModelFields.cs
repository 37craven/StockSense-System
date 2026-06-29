using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSense.Web.Migrations
{
    public partial class AddNewModelFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. ADD NEW COLUMNS TO APPOINTMENTS
            // These stay because they are unique to this migration
            //migrationBuilder.AddColumn<string>(
            //    name: "MechanicName",
            //    table: "Appointments",
            //    type: "nvarchar(max)",
            //    nullable: false,
            //    defaultValue: "");

            //migrationBuilder.AddColumn<int>(
            //    name: "DurationMinutes",
            //    table: "Appointments",
            //    type: "int",
            //    nullable: false,
            //    defaultValue: 0);

            //// 2. ADD ISBLOCKED TO ASPNETUSERS
            //migrationBuilder.AddColumn<bool>(
            //    name: "IsBlocked",
            //    table: "AspNetUsers",
            //    type: "bit",
            //    nullable: false,
            //    defaultValue: false);

            // --- CRITICAL SYNC FIX ---
            /* We have REMOVED the 'IsReceived' column from this Up() method 
               because it was already included in your 'AddOrderTables' migration.
               If we leave it here, SQL Server throws the "Unique Column" error.
            */
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "MechanicName", table: "Appointments");
            migrationBuilder.DropColumn(name: "DurationMinutes", table: "Appointments");
            migrationBuilder.DropColumn(name: "IsBlocked", table: "AspNetUsers");

            // We also comment out the drop here to match the Up method
            // migrationBuilder.DropColumn(name: "IsReceived", table: "OrderSlips");
        }
    }
}
