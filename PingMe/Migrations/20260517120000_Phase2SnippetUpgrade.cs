using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PingMe.Migrations
{
    /// <inheritdoc />
    public partial class Phase2SnippetUpgrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 1. Extend CodeSnippets table ──────────────────────────────────
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "CodeSnippets",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRevoked",
                table: "CodeSnippets",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "AccessCount",
                table: "CodeSnippets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAccessedAt",
                table: "CodeSnippets",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CodeSnippets_ExpiresAt",
                table: "CodeSnippets",
                column: "ExpiresAt");

            // ── 2. Add SnippetId FK to Messages ──────────────────────────────
            migrationBuilder.AddColumn<int>(
                name: "SnippetId",
                table: "Messages",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SnippetId",
                table: "Messages",
                column: "SnippetId");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_CodeSnippets_SnippetId",
                table: "Messages",
                column: "SnippetId",
                principalTable: "CodeSnippets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // ── 3. Create SnippetAccessLogs table ────────────────────────────
            migrationBuilder.CreateTable(
                name: "SnippetAccessLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SnippetId = table.Column<int>(type: "int", nullable: false),
                    AccessedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "NOW(6)"),
                    IpAddress = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserAgent = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SnippetAccessLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SnippetAccessLogs_CodeSnippets_SnippetId",
                        column: x => x.SnippetId,
                        principalTable: "CodeSnippets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SnippetAccessLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_SnippetAccessLogs_AccessedAt",
                table: "SnippetAccessLogs",
                column: "AccessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SnippetAccessLogs_SnippetId",
                table: "SnippetAccessLogs",
                column: "SnippetId");

            migrationBuilder.CreateIndex(
                name: "IX_SnippetAccessLogs_UserId",
                table: "SnippetAccessLogs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SnippetAccessLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_CodeSnippets_SnippetId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_SnippetId",
                table: "Messages");

            migrationBuilder.DropColumn(name: "SnippetId", table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_CodeSnippets_ExpiresAt",
                table: "CodeSnippets");

            migrationBuilder.DropColumn(name: "ExpiresAt",       table: "CodeSnippets");
            migrationBuilder.DropColumn(name: "IsRevoked",       table: "CodeSnippets");
            migrationBuilder.DropColumn(name: "AccessCount",     table: "CodeSnippets");
            migrationBuilder.DropColumn(name: "LastAccessedAt",  table: "CodeSnippets");
        }
    }
}
