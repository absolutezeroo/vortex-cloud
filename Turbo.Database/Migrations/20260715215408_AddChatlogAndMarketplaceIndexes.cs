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
            migrationBuilder.DropIndex(name: "IX_room_chatlogs_player_id", table: "room_chatlogs");

            migrationBuilder.DropIndex(name: "IX_room_chatlogs_room_id", table: "room_chatlogs");

            migrationBuilder.DropIndex(
                name: "IX_marketplace_offers_seller_id",
                table: "marketplace_offers"
            );

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
