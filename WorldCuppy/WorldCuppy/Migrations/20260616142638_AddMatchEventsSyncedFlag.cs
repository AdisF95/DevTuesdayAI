using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorldCuppy.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchEventsSyncedFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EventsSynced",
                table: "Matches",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EventsSynced",
                table: "Matches");
        }
    }
}
