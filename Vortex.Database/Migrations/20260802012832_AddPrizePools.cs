using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddPrizePools : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder
                .CreateTable(
                    name: "prize_pools",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        code = table
                            .Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        name = table
                            .Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        variants = table
                            .Column<string>(
                                type: "varchar(512)",
                                maxLength: 512,
                                nullable: false,
                                defaultValue: ""
                            )
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        enabled = table.Column<bool>(
                            type: "tinyint(1)",
                            nullable: false,
                            defaultValue: true
                        ),
                        notes = table
                            .Column<string>(
                                type: "varchar(512)",
                                maxLength: 512,
                                nullable: false,
                                defaultValue: ""
                            )
                            .Annotation("MySql:CharSet", "utf8mb4"),
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
                        table.PrimaryKey("PK_prize_pools", x => x.id);
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "prize_pool_entries",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        pool_id = table.Column<int>(type: "int", nullable: false),
                        variant = table
                            .Column<string>(
                                type: "varchar(32)",
                                maxLength: 32,
                                nullable: false,
                                defaultValue: ""
                            )
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        product_type = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        furniture_definition_id = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        extra_param = table
                            .Column<string>(
                                type: "varchar(128)",
                                maxLength: 128,
                                nullable: false,
                                defaultValue: ""
                            )
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        weight = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                        enabled = table.Column<bool>(
                            type: "tinyint(1)",
                            nullable: false,
                            defaultValue: true
                        ),
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
                        table.PrimaryKey("PK_prize_pool_entries", x => x.id);
                        table.ForeignKey(
                            name: "FK_prize_pool_entries_prize_pools_pool_id",
                            column: x => x.pool_id,
                            principalTable: "prize_pools",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_prize_pool_entries_pool_id_enabled",
                table: "prize_pool_entries",
                columns: new[] { "pool_id", "enabled" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_prize_pools_code",
                table: "prize_pools",
                column: "code",
                unique: true
            );

            // The two pools the server draws by code. They must exist before the rows move, and
            // before any hotel that already tuned its odds loses them.
            migrationBuilder.Sql(
                """
                INSERT INTO prize_pools (code, name, variants, enabled, notes)
                VALUES
                    ('mystery-box', 'Mystery box', 'purple,blue,green,yellow,lilac,orange,turquoise,red', 1,
                     'Drawn when a box and a matching key are opened together.'),
                    ('mystery-trophy', 'Mystery trophy', '', 1,
                     'Drawn when a mystery trophy is inscribed and opened.');
                """
            );

            // Carry every tuned row across. The old pool enum was 0 = box, 1 = trophy; anything else
            // was never writable through the admin surface, so it maps to the box pool rather than
            // being dropped on the floor.
            migrationBuilder.Sql(
                """
                INSERT INTO prize_pool_entries
                    (pool_id, variant, product_type, furniture_definition_id, extra_param, weight, enabled, deleted_at)
                SELECT p.id, m.color, m.product_type, m.furniture_definition_id, m.extra_param,
                       m.weight, m.enabled, m.deleted_at
                FROM mystery_box_prizes m
                JOIN prize_pools p
                  ON p.code = CASE WHEN m.pool = 1 THEN 'mystery-trophy' ELSE 'mystery-box' END;
                """
            );

            migrationBuilder.DropTable(name: "mystery_box_prizes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder
                .CreateTable(
                    name: "mystery_box_prizes",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        color = table
                            .Column<string>(
                                type: "varchar(32)",
                                maxLength: 32,
                                nullable: false,
                                defaultValue: ""
                            )
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        created_at = table
                            .Column<DateTime>(type: "datetime(6)", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                        enabled = table.Column<bool>(
                            type: "tinyint(1)",
                            nullable: false,
                            defaultValue: true
                        ),
                        extra_param = table
                            .Column<string>(
                                type: "varchar(128)",
                                maxLength: 128,
                                nullable: false,
                                defaultValue: ""
                            )
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        furniture_definition_id = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        pool = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                        product_type = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        updated_at = table
                            .Column<DateTime>(type: "datetime(6)", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.ComputedColumn
                            ),
                        weight = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_mystery_box_prizes", x => x.id);
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_mystery_box_prizes_pool_enabled",
                table: "mystery_box_prizes",
                columns: new[] { "pool", "enabled" }
            );

            // Only the two box pools can round-trip: the old table had nowhere to put an entry from
            // a crackable or seasonal pool, so those are left behind by design rather than silently
            // refiled as box prizes.
            migrationBuilder.Sql(
                """
                INSERT INTO mystery_box_prizes
                    (pool, color, product_type, furniture_definition_id, extra_param, weight, enabled, deleted_at)
                SELECT CASE WHEN p.code = 'mystery-trophy' THEN 1 ELSE 0 END, e.variant, e.product_type,
                       e.furniture_definition_id, e.extra_param, e.weight, e.enabled, e.deleted_at
                FROM prize_pool_entries e
                JOIN prize_pools p ON p.id = e.pool_id
                WHERE p.code IN ('mystery-box', 'mystery-trophy');
                """
            );

            migrationBuilder.DropTable(name: "prize_pool_entries");

            migrationBuilder.DropTable(name: "prize_pools");
        }
    }
}
