using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LolStats.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerHistoryCoverage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "history_complete",
                table: "players",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "history_covered_until",
                table: "players",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "history_complete",
                table: "players");

            migrationBuilder.DropColumn(
                name: "history_covered_until",
                table: "players");
        }
    }
}
