using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LolStats.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "league_snapshots",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    puuid = table.Column<string>(type: "text", nullable: false),
                    queue = table.Column<string>(type: "text", nullable: false),
                    tier = table.Column<string>(type: "text", nullable: false),
                    division = table.Column<string>(type: "text", nullable: false),
                    lp = table.Column<short>(type: "smallint", nullable: false),
                    wins = table.Column<int>(type: "integer", nullable: false),
                    losses = table.Column<int>(type: "integer", nullable: false),
                    captured_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_league_snapshots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "matches",
                columns: table => new
                {
                    match_id = table.Column<string>(type: "text", nullable: false),
                    platform = table.Column<string>(type: "text", nullable: false),
                    queue_id = table.Column<int>(type: "integer", nullable: false),
                    patch = table.Column<string>(type: "text", nullable: false),
                    game_start = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    duration_seconds = table.Column<int>(type: "integer", nullable: false),
                    winning_team_id = table.Column<short>(type: "smallint", nullable: false),
                    has_timeline = table.Column<bool>(type: "boolean", nullable: false),
                    raw_ref = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_matches", x => x.match_id);
                });

            migrationBuilder.CreateTable(
                name: "outbox",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    type = table.Column<string>(type: "text", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "players",
                columns: table => new
                {
                    puuid = table.Column<string>(type: "text", nullable: false),
                    platform = table.Column<string>(type: "text", nullable: false),
                    game_name = table.Column<string>(type: "text", nullable: false),
                    tag_line = table.Column<string>(type: "text", nullable: false),
                    riot_id_normalized = table.Column<string>(type: "text", nullable: false),
                    profile_icon_id = table.Column<int>(type: "integer", nullable: true),
                    summoner_level = table.Column<int>(type: "integer", nullable: true),
                    last_match_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_refreshed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    data_version = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_players", x => x.puuid);
                });

            migrationBuilder.CreateTable(
                name: "match_participants",
                columns: table => new
                {
                    match_id = table.Column<string>(type: "text", nullable: false),
                    puuid = table.Column<string>(type: "text", nullable: false),
                    participant_id = table.Column<short>(type: "smallint", nullable: false),
                    team_id = table.Column<short>(type: "smallint", nullable: false),
                    champion_id = table.Column<int>(type: "integer", nullable: false),
                    queue_id = table.Column<int>(type: "integer", nullable: false),
                    game_start = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    team_position = table.Column<short>(type: "smallint", nullable: false),
                    win = table.Column<bool>(type: "boolean", nullable: false),
                    kills = table.Column<short>(type: "smallint", nullable: false),
                    deaths = table.Column<short>(type: "smallint", nullable: false),
                    assists = table.Column<short>(type: "smallint", nullable: false),
                    cs = table.Column<int>(type: "integer", nullable: false),
                    gold = table.Column<int>(type: "integer", nullable: false),
                    damage_to_champions = table.Column<int>(type: "integer", nullable: false),
                    damage_to_buildings = table.Column<int>(type: "integer", nullable: false),
                    damage_taken = table.Column<int>(type: "integer", nullable: false),
                    vision_score = table.Column<int>(type: "integer", nullable: false),
                    final_items = table.Column<int[]>(type: "integer[]", nullable: false),
                    summoner_spells = table.Column<int[]>(type: "integer[]", nullable: false),
                    runes_json = table.Column<string>(type: "jsonb", nullable: true),
                    perf_score = table.Column<float>(type: "real", nullable: false),
                    team_rank = table.Column<short>(type: "smallint", nullable: false),
                    game_rank = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_match_participants", x => new { x.match_id, x.puuid });
                    table.ForeignKey(
                        name: "fk_match_participants_matches_match_id",
                        column: x => x.match_id,
                        principalTable: "matches",
                        principalColumn: "match_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_league_snapshots_puuid_queue_captured_at",
                table: "league_snapshots",
                columns: new[] { "puuid", "queue", "captured_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_match_participants_champion_id_puuid",
                table: "match_participants",
                columns: new[] { "champion_id", "puuid" });

            migrationBuilder.CreateIndex(
                name: "ix_match_participants_puuid_game_start_match_id",
                table: "match_participants",
                columns: new[] { "puuid", "game_start", "match_id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_id",
                table: "outbox",
                column: "id",
                filter: "published_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_players_platform_riot_id_normalized",
                table: "players",
                columns: new[] { "platform", "riot_id_normalized" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "league_snapshots");

            migrationBuilder.DropTable(
                name: "match_participants");

            migrationBuilder.DropTable(
                name: "outbox");

            migrationBuilder.DropTable(
                name: "players");

            migrationBuilder.DropTable(
                name: "matches");
        }
    }
}
