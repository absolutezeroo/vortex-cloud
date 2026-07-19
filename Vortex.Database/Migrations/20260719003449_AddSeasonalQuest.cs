using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSeasonalQuest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ends_at",
                table: "quests",
                type: "datetime(6)",
                nullable: true
            );

            // A real seasonal quest (globe/expljungle: "visit at least 3 rooms") ending 7 days after
            // this migration runs; the wire seconds-left counts down to ends_at. Raw SQL because the
            // end time is relative to NOW().
            migrationBuilder.Sql(
                "INSERT INTO `quests` "
                    + "(`id`,`campaign_code`,`chain_code`,`localization_code`,`quest_type`,`total_steps`,`reward_type`,`reward_amount`,`sort_order`,`easy`,`seasonal`,`ends_at`) "
                    + "VALUES (10,'globe','globe','expljungle','RoomEntry',3,0,30,1,1,1,DATE_ADD(NOW(), INTERVAL 7 DAY));"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM `quests` WHERE `id` = 10;");

            migrationBuilder.DropColumn(name: "ends_at", table: "quests");
        }
    }
}
