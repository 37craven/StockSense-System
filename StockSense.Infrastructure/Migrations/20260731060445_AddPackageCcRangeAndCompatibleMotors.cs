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
            if (migrationBuilder.ActiveProvider?.Contains("SqlServer") == true)
            {
                migrationBuilder.Sql(@"
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'MaxAddedCC' AND Object_ID = Object_ID(N'PreBuiltPackages'))
                    BEGIN
                        ALTER TABLE [PreBuiltPackages] ADD [MaxAddedCC] int NOT NULL DEFAULT 0;
                    END
                ");

                migrationBuilder.Sql(@"
                    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE Name = N'PreBuiltPackageMotor')
                    BEGIN
                        CREATE TABLE [PreBuiltPackageMotor] (
                            [Id] int NOT NULL IDENTITY,
                            [PreBuiltPackageId] int NOT NULL,
                            [Brand] nvarchar(max) NOT NULL,
                            [Model] nvarchar(max) NOT NULL,
                            [StockCC] nvarchar(max) NOT NULL,
                            CONSTRAINT [PK_PreBuiltPackageMotor] PRIMARY KEY ([Id]),
                            CONSTRAINT [FK_PreBuiltPackageMotor_PreBuiltPackages_PreBuiltPackageId] FOREIGN KEY ([PreBuiltPackageId]) REFERENCES [PreBuiltPackages]([Id]) ON DELETE CASCADE
                        );
                    END
                ");

                migrationBuilder.Sql(@"
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE Name = N'IX_PreBuiltPackageMotor_PreBuiltPackageId')
                    BEGIN
                        CREATE INDEX [IX_PreBuiltPackageMotor_PreBuiltPackageId] ON [PreBuiltPackageMotor]([PreBuiltPackageId]);
                    END
                ");

                migrationBuilder.Sql(@"
                    IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'CompatibleBrand' AND Object_ID = Object_ID(N'PreBuiltPackages'))
                    BEGIN
                        INSERT INTO [PreBuiltPackageMotor] ([PreBuiltPackageId], [Brand], [Model], [StockCC])
                        SELECT [Id], [CompatibleBrand], [CompatibleModel], [TargetCC]
                        FROM [PreBuiltPackages]
                        WHERE [CompatibleBrand] <> N'' AND [CompatibleModel] <> N'' AND [TargetCC] <> N'';
                    END
                ");

                migrationBuilder.Sql(@"
                    IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'EstimatedAddedCC' AND Object_ID = Object_ID(N'PreBuiltPackages'))
                    BEGIN
                        IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'MinAddedCC' AND Object_ID = Object_ID(N'PreBuiltPackages'))
                        BEGIN
                            ALTER TABLE [PreBuiltPackages] ADD [MinAddedCC] int NOT NULL DEFAULT 0;
                        END
                        EXEC sp_executesql N'UPDATE [PreBuiltPackages] SET [MinAddedCC] = [EstimatedAddedCC], [MaxAddedCC] = [EstimatedAddedCC] WHERE [EstimatedAddedCC] > 0';
                    END
                ");

                migrationBuilder.Sql(@"
                    IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'CompatibleBrand' AND Object_ID = Object_ID(N'PreBuiltPackages'))
                        ALTER TABLE [PreBuiltPackages] DROP COLUMN [CompatibleBrand];
                    IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'CompatibleModel' AND Object_ID = Object_ID(N'PreBuiltPackages'))
                        ALTER TABLE [PreBuiltPackages] DROP COLUMN [CompatibleModel];
                    IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'TargetCC' AND Object_ID = Object_ID(N'PreBuiltPackages'))
                        ALTER TABLE [PreBuiltPackages] DROP COLUMN [TargetCC];
                    IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'EstimatedAddedCC' AND Object_ID = Object_ID(N'PreBuiltPackages'))
                    BEGIN
                        IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'MinAddedCC' AND Object_ID = Object_ID(N'PreBuiltPackages'))
                            ALTER TABLE [PreBuiltPackages] DROP COLUMN [EstimatedAddedCC];
                        ELSE
                            EXEC sp_rename N'PreBuiltPackages.EstimatedAddedCC', N'MinAddedCC', N'COLUMN';
                    END
                ");
            }
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
