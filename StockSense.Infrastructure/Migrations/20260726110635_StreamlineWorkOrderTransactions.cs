using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSense.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StreamlineWorkOrderTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ServiceAmount",
                table: "Transactions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "BuildRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TransactionId",
                table: "BuildRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Appointments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TransactionId",
                table: "Appointments",
                type: "int",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Transactions_ServiceAmount",
                table: "Transactions",
                sql: "[ServiceAmount] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_BuildRequests_TransactionId",
                table: "BuildRequests",
                column: "TransactionId",
                unique: true,
                filter: "[TransactionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_TransactionId",
                table: "Appointments",
                column: "TransactionId",
                unique: true,
                filter: "[TransactionId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Transactions_TransactionId",
                table: "Appointments",
                column: "TransactionId",
                principalTable: "Transactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_BuildRequests_Transactions_TransactionId",
                table: "BuildRequests",
                column: "TransactionId",
                principalTable: "Transactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Transactions_TransactionId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_BuildRequests_Transactions_TransactionId",
                table: "BuildRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Transactions_ServiceAmount",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_BuildRequests_TransactionId",
                table: "BuildRequests");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_TransactionId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ServiceAmount",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "BuildRequests");

            migrationBuilder.DropColumn(
                name: "TransactionId",
                table: "BuildRequests");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "TransactionId",
                table: "Appointments");
        }
    }
}
