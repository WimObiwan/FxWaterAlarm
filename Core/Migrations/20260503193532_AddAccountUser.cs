using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountUser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    LoginType = table.Column<int>(type: "INTEGER", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    Provider = table.Column<string>(type: "TEXT", nullable: true),
                    ProviderSubjectId = table.Column<string>(type: "TEXT", nullable: true),
                    CreationTimestamp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountUser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountUser_Account_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountUser_AccountId_LoginType_Email",
                table: "AccountUser",
                columns: new[] { "AccountId", "LoginType", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountUser_Provider_ProviderSubjectId",
                table: "AccountUser",
                columns: new[] { "Provider", "ProviderSubjectId" },
                unique: true);

            // Backfill: create a Mail AccountUser entry for every existing account owner
            migrationBuilder.Sql(@"
                INSERT INTO AccountUser (AccountId, LoginType, Email, Provider, ProviderSubjectId, CreationTimestamp)
                SELECT Id, 0, Email, NULL, NULL, CreationTimestamp FROM Account;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountUser");
        }
    }
}
