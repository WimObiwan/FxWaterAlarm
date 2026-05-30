using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Core.Repositories;

#nullable disable

namespace Core.Migrations
{
    [DbContext(typeof(WaterAlarmDbContext))]
    [Migration("20260530123000_AddAccountSensorDensityAndGeometry")]
    public partial class AddAccountSensorDensityAndGeometry : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "DensityKgPerM3",
                table: "AccountSensor",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Geometry",
                table: "AccountSensor",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DensityKgPerM3",
                table: "AccountSensor");

            migrationBuilder.DropColumn(
                name: "Geometry",
                table: "AccountSensor");
        }
    }
}