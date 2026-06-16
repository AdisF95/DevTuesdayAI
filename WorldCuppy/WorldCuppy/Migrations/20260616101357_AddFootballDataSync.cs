using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorldCuppy.Migrations
{
    /// <inheritdoc />
    public partial class AddFootballDataSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CrestUrl",
                table: "Teams",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExternalId",
                table: "Teams",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Round",
                table: "Matches",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "ExternalId",
                table: "Matches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Group",
                table: "Matches",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Matches",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_ExternalId",
                table: "Teams",
                column: "ExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Matches_ExternalId",
                table: "Matches",
                column: "ExternalId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Teams_ExternalId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Matches_ExternalId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "CrestUrl",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Group",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Matches");

            migrationBuilder.AlterColumn<string>(
                name: "Round",
                table: "Matches",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
