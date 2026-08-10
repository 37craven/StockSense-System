using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSense.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminOverridePins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(name: "AdminPinFailedAccessCount", table: "AspNetUsers", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<string>(name: "AdminPinHash", table: "AspNetUsers", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<System.DateTimeOffset>(name: "AdminPinLockoutEnd", table: "AspNetUsers", type: "datetimeoffset", nullable: true);
            migrationBuilder.AddColumn<string>(name: "ApproverEmail", table: "WorkOrderAudits", type: "nvarchar(256)", maxLength: 256, nullable: true);
            migrationBuilder.AddColumn<string>(name: "ApproverUserId", table: "WorkOrderAudits", type: "nvarchar(450)", maxLength: 450, nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "AdminPinFailedAccessCount", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "AdminPinHash", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "AdminPinLockoutEnd", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "ApproverEmail", table: "WorkOrderAudits");
            migrationBuilder.DropColumn(name: "ApproverUserId", table: "WorkOrderAudits");
        }
    }
}
