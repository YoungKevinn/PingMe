using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PingMe.Migrations
{
    /// <inheritdoc />
    public partial class AddChatReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatReminders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Text = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RemindAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsSent = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    GroupId = table.Column<int>(type: "int", nullable: true),
                    PeerUserId = table.Column<int>(type: "int", nullable: true),
                    SourceMessageId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "NOW(6)"),
                    SentAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatReminders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatReminders_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatReminders_Messages_SourceMessageId",
                        column: x => x.SourceMessageId,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ChatReminders_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatReminders_Users_PeerUserId",
                        column: x => x.PeerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ChatReminders_CreatedByUserId",
                table: "ChatReminders",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatReminders_GroupId",
                table: "ChatReminders",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatReminders_PeerUserId",
                table: "ChatReminders",
                column: "PeerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatReminders_RemindAtUtc",
                table: "ChatReminders",
                column: "RemindAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ChatReminders_SourceMessageId",
                table: "ChatReminders",
                column: "SourceMessageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatReminders");
        }
    }
}
