using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using StockSense.Infrastructure.Data;

namespace StockSense.Infrastructure.Migrations;

public partial class AddMotorCompatibility : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable("MotorCompatibility", (ColumnsBuilder table) => new
			{
				CompatibilityID = table.Column<int>("int").Annotation("SqlServer:Identity", "1, 1"),
				Manufacturer = table.Column<string>("varchar(50)", false, 50),
				ModelName = table.Column<string>("varchar(100)", false, 100),
				VersionName = table.Column<string>("varchar(50)", false, 50),
				YearStart = table.Column<int>("int"),
				YearEnd = table.Column<int>("int", null, null, rowVersion: false, null, nullable: true),
				EngineOilSpec = table.Column<string>("varchar(100)", false, 100, rowVersion: false, null, nullable: true),
				GearOilSpec = table.Column<string>("varchar(100)", false, 100, rowVersion: false, null, nullable: true),
				CoolantSpec = table.Column<string>("varchar(100)", false, 100, rowVersion: false, null, nullable: true),
				SparkPlugSpec = table.Column<string>("varchar(100)", false, 100, rowVersion: false, null, nullable: true),
				FuelFilterSpec = table.Column<string>("varchar(100)", false, 100, rowVersion: false, null, nullable: true),
				DriveBeltSpec = table.Column<string>("varchar(100)", false, 100, rowVersion: false, null, nullable: true),
				FlyBallWeight = table.Column<string>("varchar(50)", false, 50, rowVersion: false, null, nullable: true),
				CenterSpringSpec = table.Column<string>("varchar(50)", false, 50, rowVersion: false, null, nullable: true),
				BrakePadFront = table.Column<string>("varchar(100)", false, 100, rowVersion: false, null, nullable: true),
				BrakePadRear = table.Column<string>("varchar(100)", false, 100, rowVersion: false, null, nullable: true),
				BrakeShoeRear = table.Column<string>("varchar(100)", false, 100, rowVersion: false, null, nullable: true),
				AirFilterSpec = table.Column<string>("varchar(100)", false, 100, rowVersion: false, null, nullable: true)
			}, null, table =>
			{
				table.PrimaryKey("PK_MotorCompatibility", x => x.CompatibilityID);
				table.CheckConstraint("CK_MotorCompatibility_Manufacturer", "[Manufacturer] IN ('Honda', 'Yamaha', 'Suzuki', 'Kawasaki', 'Rusi')");
				table.CheckConstraint("CK_MotorCompatibility_YearRange", "[YearStart] >= 1885 AND ([YearEnd] IS NULL OR [YearEnd] >= [YearStart])");
			});
			migrationBuilder.CreateTable("ProductCompatibilityMapping", (ColumnsBuilder table) => new
			{
				MappingID = table.Column<int>("int").Annotation("SqlServer:Identity", "1, 1"),
				CompatibilityID = table.Column<int>("int"),
				ProductID = table.Column<int>("int"),
				PartFunction = table.Column<string>("varchar(50)", false, 50),
				IsOEM = table.Column<bool>("bit", null, null, rowVersion: false, null, nullable: false, false),
				Notes = table.Column<string>("varchar(255)", false, 255, rowVersion: false, null, nullable: true)
			}, null, table =>
			{
				table.PrimaryKey("PK_ProductCompatibilityMapping", x => x.MappingID);
				table.ForeignKey("FK_ProductCompatibilityMapping_MotorCompatibility", x => x.CompatibilityID, "MotorCompatibility", "CompatibilityID", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
				table.ForeignKey("FK_ProductCompatibilityMapping_Products", x => x.ProductID, "Products", "Id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
			});
			migrationBuilder.CreateIndex("UX_MotorCompatibility_ModelVersionYears", "MotorCompatibility", new string[5] { "Manufacturer", "ModelName", "VersionName", "YearStart", "YearEnd" }, null, unique: true);
			migrationBuilder.CreateIndex("IX_ProductCompatibilityMapping_ProductID", "ProductCompatibilityMapping", "ProductID").Annotation("SqlServer:Include", new string[3] { "CompatibilityID", "PartFunction", "IsOEM" });
			migrationBuilder.CreateIndex("UX_ProductCompatibilityMapping_CompatibilityProductFunction", "ProductCompatibilityMapping", new string[3] { "CompatibilityID", "ProductID", "PartFunction" }, null, unique: true);
		}

	protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable("ProductCompatibilityMapping");
			migrationBuilder.DropTable("MotorCompatibility");
		}
}
