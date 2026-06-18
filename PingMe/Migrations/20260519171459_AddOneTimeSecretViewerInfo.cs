using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PingMe.Migrations
{
    /// <inheritdoc />
    public partial class AddOneTimeSecretViewerInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ViewedByUserId",
                table: "OneTimeSecrets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ViewedIpHash",
                table: "OneTimeSecrets",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ViewedUserAgent",
                table: "OneTimeSecrets",
                type: "varchar(512)",
                maxLength: 512,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_OneTimeSecrets_ViewedByUserId",
                table: "OneTimeSecrets",
                column: "ViewedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_OneTimeSecrets_Users_ViewedByUserId",
                table: "OneTimeSecrets",
                column: "ViewedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OneTimeSecrets_Users_ViewedByUserId",
                table: "OneTimeSecrets");

            migrationBuilder.DropIndex(
                name: "IX_OneTimeSecrets_ViewedByUserId",
                table: "OneTimeSecrets");

            migrationBuilder.DropColumn(
                name: "ViewedByUserId",
                table: "OneTimeSecrets");

            migrationBuilder.DropColumn(
                name: "ViewedIpHash",
                table: "OneTimeSecrets");

            migrationBuilder.DropColumn(
                name: "ViewedUserAgent",
                table: "OneTimeSecrets");
        }
    }
}
