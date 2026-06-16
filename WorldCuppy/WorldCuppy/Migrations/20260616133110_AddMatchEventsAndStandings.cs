using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorldCuppy.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchEventsAndStandings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExtraTimeAwayScore",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExtraTimeHomeScore",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HalfTimeAwayScore",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HalfTimeHomeScore",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MatchDuration",
                table: "Matches",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PenaltyAwayScore",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PenaltyHomeScore",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BookingEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Minute = table.Column<int>(type: "integer", nullable: false),
                    PlayerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CardType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsHomeTeam = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingEvents_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GoalEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Minute = table.Column<int>(type: "integer", nullable: false),
                    ScorerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AssistName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TeamName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsHomeTeam = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoalEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoalEvents_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupStandings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Group = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    PlayedGames = table.Column<int>(type: "integer", nullable: false),
                    Won = table.Column<int>(type: "integer", nullable: false),
                    Draw = table.Column<int>(type: "integer", nullable: false),
                    Lost = table.Column<int>(type: "integer", nullable: false),
                    GoalsFor = table.Column<int>(type: "integer", nullable: false),
                    GoalsAgainst = table.Column<int>(type: "integer", nullable: false),
                    GoalDifference = table.Column<int>(type: "integer", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    Form = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupStandings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupStandings_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingEvents_MatchId",
                table: "BookingEvents",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_GoalEvents_MatchId",
                table: "GoalEvents",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupStandings_Group_TeamId",
                table: "GroupStandings",
                columns: new[] { "Group", "TeamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupStandings_TeamId",
                table: "GroupStandings",
                column: "TeamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingEvents");

            migrationBuilder.DropTable(
                name: "GoalEvents");

            migrationBuilder.DropTable(
                name: "GroupStandings");

            migrationBuilder.DropColumn(
                name: "ExtraTimeAwayScore",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "ExtraTimeHomeScore",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HalfTimeAwayScore",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HalfTimeHomeScore",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "MatchDuration",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "PenaltyAwayScore",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "PenaltyHomeScore",
                table: "Matches");
        }
    }
}
