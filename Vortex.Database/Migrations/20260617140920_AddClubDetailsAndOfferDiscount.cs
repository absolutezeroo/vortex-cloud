using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddClubDetailsAndOfferDiscount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "first_subscribed_at",
                table: "player_subscriptions",
                type: "datetime(6)",
                nullable: true
            );

            migrationBuilder.AddColumn<bool>(
                name: "hc_badge_granted",
                table: "player_subscriptions",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "last_expired_at",
                table: "player_subscriptions",
                type: "datetime(6)",
                nullable: true
            );

            migrationBuilder.AddColumn<int>(
                name: "past_club_days",
                table: "player_subscriptions",
                type: "int",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<int>(
                name: "past_vip_days",
                table: "player_subscriptions",
                type: "int",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<int>(
                name: "discount_percent",
                table: "catalog_offers",
                type: "int",
                nullable: false,
                defaultValue: 0
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "first_subscribed_at", table: "player_subscriptions");

            migrationBuilder.DropColumn(name: "hc_badge_granted", table: "player_subscriptions");

            migrationBuilder.DropColumn(name: "last_expired_at", table: "player_subscriptions");

            migrationBuilder.DropColumn(name: "past_club_days", table: "player_subscriptions");

            migrationBuilder.DropColumn(name: "past_vip_days", table: "player_subscriptions");

            migrationBuilder.DropColumn(name: "discount_percent", table: "catalog_offers");
        }
    }
}
