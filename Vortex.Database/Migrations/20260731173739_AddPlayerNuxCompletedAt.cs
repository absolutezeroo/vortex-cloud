using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerNuxCompletedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "nux_completed_at",
                table: "players",
                type: "datetime(6)",
                nullable: true
            );

            // Everyone who already exists has been through sign-up; without this backfill the new
            // NULL default would hand every returning player the onboarding flow on next login.
            migrationBuilder.Sql(
                "UPDATE `players` SET `nux_completed_at` = UTC_TIMESTAMP() WHERE `nux_completed_at` IS NULL;"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "nux_completed_at", table: "players");
        }
    }
}
