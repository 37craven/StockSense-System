using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSense.Infrastructure.Migrations
{
    public partial class RestoreLegacyOrderSlipFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider?.Contains("SqlServer") == true)
            {
                migrationBuilder.Sql(@"
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'DateGenerated' AND Object_ID = Object_ID(N'OrderSlips'))
                    BEGIN
                        ALTER TABLE [OrderSlips] ADD [DateGenerated] datetime2 NOT NULL DEFAULT (GETDATE());
                    END

                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'IsReceived' AND Object_ID = Object_ID(N'OrderSlips'))
                    BEGIN
                        ALTER TABLE [OrderSlips] ADD [IsReceived] bit NOT NULL DEFAULT CAST(0 AS bit);
                    END

                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'SlipNumber' AND Object_ID = Object_ID(N'OrderSlips'))
                    BEGIN
                        ALTER TABLE [OrderSlips] ADD [SlipNumber] nvarchar(max) NOT NULL DEFAULT '';
                    END

                    UPDATE [OrderSlips] SET [SlipNumber] = [OrderSlipNumber], [DateGenerated] = [GeneratedAt]
                    WHERE ([SlipNumber] = '' OR [SlipNumber] IS NULL)
                      AND EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'GeneratedAt' AND Object_ID = Object_ID(N'OrderSlips'));
                ");
            }

            if (migrationBuilder.ActiveProvider?.Contains("SqlServer") == true)
            {
                migrationBuilder.Sql(@"
                    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE Name = N'PinnedSlips')
                    BEGIN
                        CREATE TABLE [PinnedSlips] (
                            [Id] int NOT NULL IDENTITY,
                            [UserId] nvarchar(max) NOT NULL,
                            [SlipData] nvarchar(max) NOT NULL,
                            [UpdatedAt] datetime2 NOT NULL,
                            CONSTRAINT [PK_PinnedSlips] PRIMARY KEY ([Id])
                        );
                    END
                ");
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
