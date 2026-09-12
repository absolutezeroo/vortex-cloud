using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddShop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder
                .CreateTable(
                    name: "shop_orders",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        public_id = table.Column<Guid>(
                            type: "char(36)",
                            nullable: false,
                            collation: "ascii_general_ci"
                        ),
                        player_id = table.Column<int>(type: "int", nullable: false),
                        product_code = table
                            .Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        kind = table.Column<int>(type: "int", nullable: false),
                        amount = table.Column<int>(type: "int", nullable: false),
                        price_minor = table.Column<int>(type: "int", nullable: false),
                        currency = table
                            .Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        state = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                        provider = table
                            .Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        provider_reference = table
                            .Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        paid_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                        fulfilled_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
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
                        table.PrimaryKey("PK_shop_orders", x => x.id);
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "shop_products",
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
                        kind = table.Column<int>(type: "int", nullable: false),
                        amount = table.Column<int>(type: "int", nullable: false),
                        price_minor = table.Column<int>(type: "int", nullable: false),
                        currency = table
                            .Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        section = table
                            .Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        sort_order = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        icon = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                        featured = table.Column<bool>(
                            type: "tinyint(1)",
                            nullable: false,
                            defaultValue: false
                        ),
                        is_active = table.Column<bool>(
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
                        table.PrimaryKey("PK_shop_products", x => x.id);
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_shop_orders_player_id_id",
                table: "shop_orders",
                columns: new[] { "player_id", "id" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_shop_orders_provider_provider_reference",
                table: "shop_orders",
                columns: new[] { "provider", "provider_reference" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_shop_orders_public_id",
                table: "shop_orders",
                column: "public_id",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_shop_orders_state",
                table: "shop_orders",
                column: "state"
            );

            migrationBuilder.CreateIndex(
                name: "IX_shop_products_code",
                table: "shop_products",
                column: "code",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_shop_products_is_active_section_sort_order",
                table: "shop_products",
                columns: new[] { "is_active", "section", "sort_order" }
            );

            SeedProducts(migrationBuilder);
        }

        /// <summary>
        /// The bundles the website's mock used to hard-code, now content an operator owns.
        /// </summary>
        /// <remarks>
        /// Seeded so the store page has something on it the moment the table exists — an empty shop
        /// on a beta's first day reads as a broken feature rather than as unconfigured content. The
        /// prices are habbo.fr's own and are the first thing a hotel should change; nothing in the
        /// code reads any of these codes, so editing, repricing or deleting a row is safe.
        ///
        /// `kind` is <c>ShopProductKind</c>: 0 credits, 3 club, 4 club at VIP level. For the two club
        /// kinds `amount` is a number of MONTHS, not a currency amount.
        /// </remarks>
        private static void SeedProducts(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "shop_products",
                columns: new[]
                {
                    "code",
                    "kind",
                    "amount",
                    "price_minor",
                    "currency",
                    "section",
                    "sort_order",
                    "icon",
                    "featured",
                    "is_active",
                },
                values: new object[,]
                {
                    { "c-25", 0, 25, 150, "EUR", "credits", 1, 1, false, true },
                    { "c-50", 0, 50, 250, "EUR", "credits", 2, 2, false, true },
                    { "c-100", 0, 100, 450, "EUR", "credits", 3, 3, true, true },
                    { "c-250", 0, 250, 950, "EUR", "credits", 4, 4, false, true },
                    { "c-500", 0, 500, 1750, "EUR", "credits", 5, 5, false, true },
                    { "c-1000", 0, 1000, 2950, "EUR", "credits", 6, 6, false, true },
                    { "hc-1", 4, 1, 550, "EUR", "club", 1, 3, false, true },
                    { "hc-3", 4, 3, 1450, "EUR", "club", 2, 4, true, true },
                    { "hc-12", 4, 12, 4950, "EUR", "club", 3, 6, false, true },
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "shop_orders");

            migrationBuilder.DropTable(name: "shop_products");
        }
    }
}
