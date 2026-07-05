using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Turbo.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountBanAndTradingLock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder
                .AddColumn<string>(
                    name: "ban_reason",
                    table: "player_accounts",
                    type: "varchar(255)",
                    maxLength: 255,
                    nullable: true
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "banned_until",
                table: "player_accounts",
                type: "datetime(6)",
                nullable: true
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "trading_locked_until",
                table: "player_accounts",
                type: "datetime(6)",
                nullable: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ban_reason", table: "player_accounts");

            migrationBuilder.DropColumn(name: "banned_until", table: "player_accounts");

            migrationBuilder.DropColumn(name: "trading_locked_until", table: "player_accounts");
        }
    }
}
