using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSense.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBuildRequestIdToAppointments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider?.Contains("SqlServer") == true)
            {
                migrationBuilder.Sql(@"
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'BuildRequestId' AND Object_ID = Object_ID(N'Appointments'))
                    BEGIN
                        ALTER TABLE [Appointments] ADD [BuildRequestId] int NULL;
                    END
                ");

                migrationBuilder.Sql(@"
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE Name = N'IX_Appointments_BuildRequestId')
                    BEGIN
                        CREATE UNIQUE NONCLUSTERED INDEX [IX_Appointments_BuildRequestId] ON [Appointments]([BuildRequestId]) WHERE [BuildRequestId] IS NOT NULL;
                    END

                    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE Name = N'FK_Appointments_BuildRequests_BuildRequestId')
                    BEGIN
                        ALTER TABLE [Appointments] ADD CONSTRAINT [FK_Appointments_BuildRequests_BuildRequestId] FOREIGN KEY ([BuildRequestId]) REFERENCES [BuildRequests]([Id]);
                    END
                ");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_BuildRequests_BuildRequestId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_BuildRequestId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "BuildRequestId",
                table: "Appointments");
        }
    }
}
