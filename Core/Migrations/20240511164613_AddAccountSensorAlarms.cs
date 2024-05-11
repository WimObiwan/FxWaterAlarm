using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountSensorAlarms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountSensorAlarm",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Uid = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountSensorAccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    AccountSensorSensorId = table.Column<int>(type: "INTEGER", nullable: false),
                    AlarmType = table.Column<int>(type: "INTEGER", nullable: false),
                    AlarmThreshold = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountSensorAlarm", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountSensorAlarm_AccountSensor_AccountSensorAccountId_AccountSensorSensorId",
                        columns: x => new { x.AccountSensorAccountId, x.AccountSensorSensorId },
                        principalTable: "AccountSensor",
                        principalColumns: new[] { "AccountId", "SensorId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountSensorAlarm_AccountSensorAccountId_AccountSensorSensorId",
                table: "AccountSensorAlarm",
                columns: new[] { "AccountSensorAccountId", "AccountSensorSensorId" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountSensorAlarm_Uid",
                table: "AccountSensorAlarm",
                column: "Uid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountSensorAlarm");
        }
    }
}
