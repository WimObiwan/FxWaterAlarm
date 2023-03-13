using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class AccountSensor_AddDistancesCapacity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CapacityL",
                table: "AccountSensor",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DistanceMmEmpty",
                table: "AccountSensor",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DistanceMmFull",
                table: "AccountSensor",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CapacityL",
                table: "AccountSensor");

            migrationBuilder.DropColumn(
                name: "DistanceMmEmpty",
                table: "AccountSensor");

            migrationBuilder.DropColumn(
                name: "DistanceMmFull",
                table: "AccountSensor");
        }
    }
}
