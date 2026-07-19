using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Turbo.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyQuests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Real "daily" campaign pool (localization codes match quests.daily.<code> in the client
            // texts). One is picked per player per day; quest_type maps to a progression trigger.
            migrationBuilder.InsertData(
                table: "quests",
                columns: new[]
                {
                    "id",
                    "campaign_code",
                    "chain_code",
                    "localization_code",
                    "quest_type",
                    "total_steps",
                    "reward_type",
                    "reward_amount",
                    "sort_order",
                    "easy",
                },
                values: new object[,]
                {
                    { 7, "daily", "daily", "EXPLORE", "RoomEntry", 10, 0, 20, 1, true },
                    { 8, "daily", "daily", "CHANGELOOK1", "AvatarLooks", 3, 0, 20, 2, true },
                    { 9, "daily", "daily", "DANCE", "Dance", 1, 0, 20, 3, true },
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM `quests` WHERE `id` BETWEEN 7 AND 9;");
        }
    }
}
