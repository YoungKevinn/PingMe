using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PingMe.Migrations
{
    /// <inheritdoc />
    public partial class AddOneTimeSecrets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OneTimeSecrets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TokenHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SecretCipherText = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ViewedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsViewed = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    IsRevoked = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "NOW(6)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OneTimeSecrets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OneTimeSecrets_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_OneTimeSecrets_CreatedByUserId_CreatedAt",
                table: "OneTimeSecrets",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OneTimeSecrets_ExpiresAt",
                table: "OneTimeSecrets",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_OneTimeSecrets_TokenHash",
                table: "OneTimeSecrets",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OneTimeSecrets");
        }
    }
}
