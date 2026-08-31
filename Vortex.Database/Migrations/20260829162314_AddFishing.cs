using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddFishing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder
                .CreateTable(
                    name: "fishing_derbies",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        name_key = table
                            .Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        starts_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                        ends_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                        zone_id = table.Column<int>(type: "int", nullable: false),
                        created_at = table
                            .Column<DateTime>(type: "datetime(6)", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        updated_at = table
                            .Column<DateTime>(type: "datetime(6)", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.ComputedColumn
                            ),
                        deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_fishing_derbies", x => x.id);
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "fishing_derby_entries",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        derby_id = table.Column<int>(type: "int", nullable: false),
                        player_id = table.Column<int>(type: "int", nullable: false),
                        best_weight = table.Column<int>(type: "int", nullable: false),
                        created_at = table
                            .Column<DateTime>(type: "datetime(6)", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        updated_at = table
                            .Column<DateTime>(type: "datetime(6)", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.ComputedColumn
                            ),
                        deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_fishing_derby_entries", x => x.id);
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "fishing_levels",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        level = table.Column<int>(type: "int", nullable: false),
                        xp_threshold = table.Column<int>(type: "int", nullable: false),
                        created_at = table
                            .Column<DateTime>(type: "datetime(6)", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        updated_at = table
                            .Column<DateTime>(type: "datetime(6)", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.ComputedColumn
                            ),
                        deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_fishing_levels", x => x.id);
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "fishing_player_state",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        player_id = table.Column<int>(type: "int", nullable: false),
                        fishing_level = table.Column<int>(type: "int", nullable: false),
                        fishing_xp = table.Column<int>(type: "int", nullable: false),
                        rod_quality = table.Column<int>(type: "int", nullable: false),
                        rod_xp = table.Column<int>(type: "int", nullable: false),
                        currency = table.Column<int>(type: "int", nullable: false),
                        currency_earned_today = table.Column<int>(type: "int", nullable: false),
                        currency_earned_on = table.Column<DateOnly>(type: "date", nullable: false),
                        total_catches = table.Column<int>(type: "int", nullable: false),
                        golden_catches = table.Column<int>(type: "int", nullable: false),
                        created_at = table
                            .Column<DateTime>(type: "datetime(6)", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        updated_at = table
                            .Column<DateTime>(type: "datetime(6)", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.ComputedColumn
                            ),
                        deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_fishing_player_state", x => x.id);
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "fishing_records",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        player_id = table.Column<int>(type: "int", nullable: false),
                        species_id = table.Column<int>(type: "int", nullable: false),
                        best_weight = table.Column<int>(type: "int", nullable: false),
                        caught_count = table.Column<int>(type: "int", nullable: false),
                        best_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                        created_at = table
                            .Column<DateTime>(type: "datetime(6)", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        updated_at = table
                            .Column<DateTime>(type: "datetime(6)", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.ComputedColumn
                            ),
                        deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_fishing_records", x => x.id);
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "fishing_rod_tiers",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        quality = table.Column<int>(type: "int", nullable: false),
                        xp_threshold = table.Column<int>(type: "int", nullable: false),
                        name_key = table
                            .Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        hand_item_id = table.Column<int>(type: "int", nullable: false),
                        catch_multiplier = table.Column<int>(type: "int", nullable: false),
                        golden_multiplier = table.Column<int>(type: "int", nullable: false),
                        hook_havoc_chance = table.Column<int>(type: "int", nullable: false),
                        created_at = table
                            .Column<DateTime>(type: "datetime(6)", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        updated_at = table
                            .Column<DateTime>(type: "datetime(6)", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.ComputedColumn
                            ),
                        deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_fishing_rod_tiers", x => x.id);
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "fishing_species",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        zone_id = table.Column<int>(type: "int", nullable: false),
                        name_key = table
                            .Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        required_level = table.Column<int>(type: "int", nullable: false),
                        rarity_stars = table.Column<int>(type: "int", nullable: false),
                        catch_rate = table.Column<int>(type: "int", nullable: false),
                        rarity_weight = table.Column<int>(type: "int", nullable: false),
                        min_weight = table.Column<int>(type: "int", nullable: false),
                        max_weight = table.Column<int>(type: "int", nullable: false),
                        xp_reward = table.Column<int>(type: "int", nullable: false),
                        golden_xp_bonus = table.Column<int>(type: "int", nullable: false),
                        currency_reward = table.Column<int>(type: "int", nullable: false),
                        active_hours = table.Column<int>(type: "int", nullable: false),
                        active_weekdays = table.Column<int>(type: "int", nullable: false),
                        active_seasons = table.Column<int>(type: "int", nullable: false),
                        created_at = table
                            .Column<DateTime>(type: "datetime(6)", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        updated_at = table
                            .Column<DateTime>(type: "datetime(6)", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.ComputedColumn
                            ),
                        deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_fishing_species", x => x.id);
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "fishing_zones",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        name_key = table
                            .Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        furni_class = table
                            .Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        required_level = table.Column<int>(type: "int", nullable: false),
                        min_catches = table.Column<int>(type: "int", nullable: false),
                        max_catches = table.Column<int>(type: "int", nullable: false),
                        created_at = table
                            .Column<DateTime>(type: "datetime(6)", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        updated_at = table
                            .Column<DateTime>(type: "datetime(6)", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.ComputedColumn
                            ),
                        deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_fishing_zones", x => x.id);
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_fishing_derbies_starts_at_ends_at",
                table: "fishing_derbies",
                columns: new[] { "starts_at", "ends_at" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_fishing_derby_entries_derby_id_best_weight",
                table: "fishing_derby_entries",
                columns: new[] { "derby_id", "best_weight" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_fishing_derby_entries_derby_id_player_id",
                table: "fishing_derby_entries",
                columns: new[] { "derby_id", "player_id" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_fishing_levels_level",
                table: "fishing_levels",
                column: "level",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_fishing_player_state_player_id",
                table: "fishing_player_state",
                column: "player_id",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_fishing_records_player_id_species_id",
                table: "fishing_records",
                columns: new[] { "player_id", "species_id" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_fishing_records_species_id_best_weight",
                table: "fishing_records",
                columns: new[] { "species_id", "best_weight" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_fishing_rod_tiers_quality",
                table: "fishing_rod_tiers",
                column: "quality",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_fishing_species_zone_id",
                table: "fishing_species",
                column: "zone_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_fishing_zones_furni_class",
                table: "fishing_zones",
                column: "furni_class",
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "fishing_derbies");

            migrationBuilder.DropTable(name: "fishing_derby_entries");

            migrationBuilder.DropTable(name: "fishing_levels");

            migrationBuilder.DropTable(name: "fishing_player_state");

            migrationBuilder.DropTable(name: "fishing_records");

            migrationBuilder.DropTable(name: "fishing_rod_tiers");

            migrationBuilder.DropTable(name: "fishing_species");

            migrationBuilder.DropTable(name: "fishing_zones");
        }
    }
}
