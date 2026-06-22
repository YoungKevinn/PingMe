using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PingMe.Migrations
{
    public partial class AddIocCenter : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IocIndicators",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),

                    Type = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),

                    Value = table.Column<string>(type: "varchar(2048)", maxLength: 2048, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),

                    Description = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),

                    Severity = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),

                    Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),

                    Source = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),

                    Tags = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),

                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),

                    MessageId = table.Column<int>(type: "int", nullable: true),

                    PeerUserId = table.Column<int>(type: "int", nullable: true),

                    GroupId = table.Column<int>(type: "int", nullable: true),

                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "NOW(6)"),

                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "NOW(6)"),

                    ResolvedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IocIndicators", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_IocIndicators_Type",
                table: "IocIndicators",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_IocIndicators_Severity",
                table: "IocIndicators",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_IocIndicators_Status",
                table: "IocIndicators",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_IocIndicators_GroupId",
                table: "IocIndicators",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_IocIndicators_PeerUserId",
                table: "IocIndicators",
                column: "PeerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IocIndicators_MessageId",
                table: "IocIndicators",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_IocIndicators_CreatedByUserId",
                table: "IocIndicators",
                column: "CreatedByUserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IocIndicators");
        }
    }
}