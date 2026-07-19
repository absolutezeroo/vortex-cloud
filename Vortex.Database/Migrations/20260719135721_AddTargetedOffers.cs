using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddTargetedOffers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder
                .CreateTable(
                    name: "targeted_offers",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        identifier = table
                            .Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        offer_type = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        title = table
                            .Column<string>(
                                type: "varchar(512)",
                                maxLength: 512,
                                nullable: false,
                                defaultValue: ""
                            )
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        description = table
                            .Column<string>(
                                type: "varchar(512)",
                                maxLength: 512,
                                nullable: false,
                                defaultValue: ""
                            )
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        image_url = table
                            .Column<string>(
                                type: "varchar(512)",
                                maxLength: 512,
                                nullable: false,
                                defaultValue: ""
                            )
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        icon_image_url = table
                            .Column<string>(
                                type: "varchar(512)",
                                maxLength: 512,
                                nullable: false,
                                defaultValue: ""
                            )
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        product_code = table
                            .Column<string>(
                                type: "varchar(512)",
                                maxLength: 512,
                                nullable: false,
                                defaultValue: ""
                            )
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        price_credits = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        price_activity_points = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        activity_point_type = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        purchase_limit = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 1
                        ),
                        expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                        active = table.Column<bool>(
                            type: "tinyint(1)",
                            nullable: false,
                            defaultValue: true
                        ),
                        sort_order = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
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
                        table.PrimaryKey("PK_targeted_offers", x => x.id);
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "player_targeted_offers",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        player_id = table.Column<int>(type: "int", nullable: false),
                        targeted_offer_id = table.Column<int>(type: "int", nullable: false),
                        purchase_count = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        tracking_state = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
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
                        table.PrimaryKey("PK_player_targeted_offers", x => x.id);
                        table.ForeignKey(
                            name: "FK_player_targeted_offers_players_player_id",
                            column: x => x.player_id,
                            principalTable: "players",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                        table.ForeignKey(
                            name: "FK_player_targeted_offers_targeted_offers_targeted_offer_id",
                            column: x => x.targeted_offer_id,
                            principalTable: "targeted_offers",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Restrict
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "targeted_offer_products",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        targeted_offer_id = table.Column<int>(type: "int", nullable: false),
                        product_code = table
                            .Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        definition_id = table.Column<int>(type: "int", nullable: true),
                        quantity = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
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
                        table.PrimaryKey("PK_targeted_offer_products", x => x.id);
                        table.ForeignKey(
                            name: "FK_targeted_offer_products_furniture_definitions_definition_id",
                            column: x => x.definition_id,
                            principalTable: "furniture_definitions",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Restrict
                        );
                        table.ForeignKey(
                            name: "FK_targeted_offer_products_targeted_offers_targeted_offer_id",
                            column: x => x.targeted_offer_id,
                            principalTable: "targeted_offers",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_player_targeted_offers_player_id_targeted_offer_id",
                table: "player_targeted_offers",
                columns: new[] { "player_id", "targeted_offer_id" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_player_targeted_offers_targeted_offer_id",
                table: "player_targeted_offers",
                column: "targeted_offer_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_targeted_offer_products_definition_id",
                table: "targeted_offer_products",
                column: "definition_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_targeted_offer_products_targeted_offer_id",
                table: "targeted_offer_products",
                column: "targeted_offer_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_targeted_offers_identifier",
                table: "targeted_offers",
                column: "identifier"
            );

            // Demo targeted offer: a "Starter Bundle" (a table + two chairs) at a special price,
            // one purchase per player, expiring 30 days after this migration runs. Raw SQL because
            // the expiry is relative to NOW(). Bundle furniture ids exist in the seed data.
            migrationBuilder.Sql(
                "INSERT INTO `targeted_offers` "
                    + "(`id`,`identifier`,`offer_type`,`title`,`description`,`image_url`,`icon_image_url`,`product_code`,`price_credits`,`price_activity_points`,`activity_point_type`,`purchase_limit`,`expires_at`,`active`,`sort_order`) "
                    + "VALUES (1,'starter_bundle',1,'Starter Bundle','A table and two chairs to get your room started!','','','table_polyfon_small',25,0,0,1,DATE_ADD(NOW(), INTERVAL 30 DAY),1,0);"
            );

            migrationBuilder.InsertData(
                table: "targeted_offer_products",
                columns: new[]
                {
                    "id",
                    "targeted_offer_id",
                    "product_code",
                    "definition_id",
                    "quantity",
                },
                values: new object[,]
                {
                    { 1, 1, "table_polyfon_small", 17, 1 },
                    { 2, 1, "chair_polyfon", 18, 2 },
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "player_targeted_offers");

            // targeted_offer_products rows are dropped with the table below.

            migrationBuilder.DropTable(name: "targeted_offer_products");

            migrationBuilder.DropTable(name: "targeted_offers");
        }
    }
}
