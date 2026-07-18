using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Turbo.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddChatlogAndMarketplaceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create the composite indexes first. Each leads with the FK column
            // (player_id / room_id / seller_id), so MySQL can use it to back the
            // foreign key. Only then can the old single-column FK indexes be dropped —
            // MySQL refuses to drop the only index supporting a foreign key.
            migrationBuilder.CreateIndex(
                name: "IX_room_chatlogs_player_id_created_at",
                table: "room_chatlogs",
                columns: new[] { "player_id", "created_at" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_room_chatlogs_room_id_created_at",
                table: "room_chatlogs",
                columns: new[] { "room_id", "created_at" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_marketplace_offers_seller_id_state",
                table: "marketplace_offers",
                columns: new[] { "seller_id", "state" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_marketplace_offers_state_expires_at",
                table: "marketplace_offers",
                columns: new[] { "state", "expires_at" }
            );

            migrationBuilder.DropIndex(name: "IX_room_chatlogs_player_id", table: "room_chatlogs");

            migrationBuilder.DropIndex(name: "IX_room_chatlogs_room_id", table: "room_chatlogs");

            migrationBuilder.DropIndex(
                name: "IX_marketplace_offers_seller_id",
                table: "marketplace_offers"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recreate the single-column FK indexes first so the foreign keys keep a
            // supporting index, then drop the composite indexes. Same MySQL constraint
            // as Up, in reverse.
            migrationBuilder.CreateIndex(
                name: "IX_room_chatlogs_player_id",
                table: "room_chatlogs",
                column: "player_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_room_chatlogs_room_id",
                table: "room_chatlogs",
                column: "room_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_marketplace_offers_seller_id",
                table: "marketplace_offers",
                column: "seller_id"
            );

            migrationBuilder.DropIndex(
                name: "IX_room_chatlogs_player_id_created_at",
                table: "room_chatlogs"
            );

            migrationBuilder.DropIndex(
                name: "IX_room_chatlogs_room_id_created_at",
                table: "room_chatlogs"
            );

            migrationBuilder.DropIndex(
                name: "IX_marketplace_offers_seller_id_state",
                table: "marketplace_offers"
            );

            migrationBuilder.DropIndex(
                name: "IX_marketplace_offers_state_expires_at",
                table: "marketplace_offers"
            );
        }
    }
}
