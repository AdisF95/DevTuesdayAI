using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorldCuppy.Migrations
{
    /// <inheritdoc />
    public partial class AddPointsToPrediction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Points",
                table: "Predictions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PointsAwardedAtUtc",
                table: "Predictions",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Points",
                table: "Predictions");

            migrationBuilder.DropColumn(
                name: "PointsAwardedAtUtc",
                table: "Predictions");
        }
    }
}
