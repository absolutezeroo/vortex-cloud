using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddRelicProvenanceAndEditions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "edition_size",
                table: "nft_mintable_item_types",
                type: "int",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<int>(
                name: "edition_size",
                table: "nft_assets",
                type: "int",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<int>(
                name: "serial_number",
                table: "nft_assets",
                type: "int",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder
                .CreateTable(
                    name: "nft_asset_ledger",
                    columns: table => new
                    {
                        id = table
                            .Column<int>(type: "int", nullable: false)
                            .Annotation(
                                "MySql:ValueGenerationStrategy",
                                MySqlValueGenerationStrategy.IdentityColumn
                            ),
                        nft_asset_id = table.Column<int>(type: "int", nullable: false),
                        from_player_id = table.Column<int>(type: "int", nullable: true),
                        to_player_id = table.Column<int>(type: "int", nullable: false),
                        reason = table
                            .Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
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
                        table.PrimaryKey("PK_nft_asset_ledger", x => x.id);
                        table.ForeignKey(
                            name: "FK_nft_asset_ledger_nft_assets_nft_asset_id",
                            column: x => x.nft_asset_id,
                            principalTable: "nft_assets",
                            principalColumn: "id",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            // Relics minted before this migration all carry serial 0, and the unique index below
            // would refuse to build the moment two of them share a classname. They are numbered by
            // age first -- the order they were converted in, which is the order they would have been
            // given had the column existed.
            migrationBuilder.Sql(
                """
                UPDATE nft_assets AS a
                JOIN (
                    SELECT id, ROW_NUMBER() OVER (PARTITION BY product_code ORDER BY id) AS rn
                    FROM nft_assets
                ) AS numbered ON numbered.id = a.id
                SET a.serial_number = numbered.rn;
                """
            );

            migrationBuilder.CreateIndex(
                name: "IX_nft_assets_product_code_serial_number",
                table: "nft_assets",
                columns: new[] { "product_code", "serial_number" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_nft_asset_ledger_nft_asset_id",
                table: "nft_asset_ledger",
                column: "nft_asset_id"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "nft_asset_ledger");

            migrationBuilder.DropIndex(
                name: "IX_nft_assets_product_code_serial_number",
                table: "nft_assets"
            );

            migrationBuilder.DropColumn(name: "edition_size", table: "nft_mintable_item_types");

            migrationBuilder.DropColumn(name: "edition_size", table: "nft_assets");

            migrationBuilder.DropColumn(name: "serial_number", table: "nft_assets");
        }
    }
}
