using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSense.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMotorcycles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider?.Contains("SqlServer") == true)
            {
                migrationBuilder.Sql(@"
                    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE Name = N'Motorcycles')
                    BEGIN
                        CREATE TABLE [Motorcycles] (
                            [Id] int NOT NULL IDENTITY,
                            [Brand] nvarchar(max) NOT NULL,
                            [Model] nvarchar(max) NOT NULL,
                            [BaseCC] nvarchar(max) NOT NULL,
                            CONSTRAINT [PK_Motorcycles] PRIMARY KEY ([Id])
                        );
                    END
                ");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Motorcycles");
        }
    }
}
