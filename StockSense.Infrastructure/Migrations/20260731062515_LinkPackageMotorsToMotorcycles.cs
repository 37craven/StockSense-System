using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSense.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LinkPackageMotorsToMotorcycles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider?.Contains("SqlServer") == true)
            {
                migrationBuilder.Sql(@"
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'MotorcycleId' AND Object_ID = Object_ID(N'PreBuiltPackageMotor'))
                    BEGIN
                        ALTER TABLE [PreBuiltPackageMotor] ADD [MotorcycleId] int NULL;
                    END
                ");

                migrationBuilder.Sql(@"
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE Name = N'IX_PreBuiltPackageMotor_MotorcycleId')
                    BEGIN
                        CREATE INDEX [IX_PreBuiltPackageMotor_MotorcycleId] ON [PreBuiltPackageMotor]([MotorcycleId]);
                    END

                    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE Name = N'FK_PreBuiltPackageMotor_Motorcycles_MotorcycleId')
                    BEGIN
                        ALTER TABLE [PreBuiltPackageMotor] ADD CONSTRAINT [FK_PreBuiltPackageMotor_Motorcycles_MotorcycleId] FOREIGN KEY ([MotorcycleId]) REFERENCES [Motorcycles]([Id]) ON DELETE NO ACTION;
                    END
                ");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PreBuiltPackageMotor_Motorcycles_MotorcycleId",
                table: "PreBuiltPackageMotor");

            migrationBuilder.DropIndex(
                name: "IX_PreBuiltPackageMotor_MotorcycleId",
                table: "PreBuiltPackageMotor");

            migrationBuilder.DropColumn(
                name: "MotorcycleId",
                table: "PreBuiltPackageMotor");
        }
    }
}
