using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <summary>
    /// Repoints twelve reward-track action codes onto the client's own artwork vocabulary.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>RewardTrackTaskRowView.as</c> builds a task's icon name as
    /// <c>"reward_track_tasks_" + actionType.toLowerCase()</c> and does nothing else with the
    /// string, so the set of legal action codes is fixed by the thirty <c>reward_track_tasks_*</c>
    /// embeds the client ships. <c>reward_track_introduction.sql</c> copied each task's
    /// <c>task_id</c> into its <c>action_code</c> instead — the two are different vocabularies, the
    /// first being the localization stem from <c>external_flash_texts</c> — so twelve of the sixteen
    /// seeded tasks named an icon that does not exist. Every one of them drew a blank square and
    /// logged <c>ResourceManager: Asset not found: reward_track_tasks_visit_rooms</c>.
    /// </para>
    /// <para>
    /// The seed is fixed for fresh installs; this is for the hotels it already ran on, where
    /// <c>INSERT IGNORE</c> will never revisit those rows. Written as one guarded <c>CASE</c> so it
    /// touches only rows still carrying an old code: a hotel that has already repointed one by hand,
    /// or authored a campaign on the correct codes, is left alone. It applies to every track, not
    /// just the Introduction Track, because any content written before this fix used the same wrong
    /// vocabulary.
    /// </para>
    /// <para>
    /// Player progress is unaffected: <c>player_reward_track_tasks</c> references a task by id and
    /// stores no action code, so nobody loses a stage.
    /// </para>
    /// </remarks>
    public partial class FixRewardTrackActionCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.Sql(
                """
                UPDATE reward_track_tasks
                SET action_code = CASE action_code
                    WHEN 'visit_rooms'       THEN 'enter_other_users_room'
                    WHEN 'chat_with_users'   THEN 'chat_with_someone'
                    WHEN 'make_friends'      THEN 'request_friend'
                    WHEN 'change_outfit'     THEN 'change_figure'
                    WHEN 'place_furniture'   THEN 'place_item'
                    WHEN 'move_furniture'    THEN 'move_item'
                    WHEN 'rotate_furniture'  THEN 'rotate_item'
                    WHEN 'use_teleport'      THEN 'teleport'
                    WHEN 'buy_catalog_furni' THEN 'buy_from_catalogue'
                    WHEN 'dance_in_room'     THEN 'dance'
                    WHEN 'wave_at_user'      THEN 'wave'
                    WHEN 'level_pet'         THEN 'pet_level'
                    ELSE action_code
                END
                WHERE action_code IN (
                    'visit_rooms', 'chat_with_users', 'make_friends', 'change_outfit',
                    'place_furniture', 'move_furniture', 'rotate_furniture', 'use_teleport',
                    'buy_catalog_furni', 'dance_in_room', 'wave_at_user', 'level_pet'
                );
                """
            );

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) =>
            migrationBuilder.Sql(
                """
                UPDATE reward_track_tasks
                SET action_code = CASE action_code
                    WHEN 'enter_other_users_room' THEN 'visit_rooms'
                    WHEN 'chat_with_someone'      THEN 'chat_with_users'
                    WHEN 'request_friend'         THEN 'make_friends'
                    WHEN 'change_figure'          THEN 'change_outfit'
                    WHEN 'place_item'             THEN 'place_furniture'
                    WHEN 'move_item'              THEN 'move_furniture'
                    WHEN 'rotate_item'            THEN 'rotate_furniture'
                    WHEN 'teleport'               THEN 'use_teleport'
                    WHEN 'buy_from_catalogue'     THEN 'buy_catalog_furni'
                    WHEN 'dance'                  THEN 'dance_in_room'
                    WHEN 'wave'                   THEN 'wave_at_user'
                    WHEN 'pet_level'              THEN 'level_pet'
                    ELSE action_code
                END
                WHERE action_code IN (
                    'enter_other_users_room', 'chat_with_someone', 'request_friend', 'change_figure',
                    'place_item', 'move_item', 'rotate_item', 'teleport',
                    'buy_from_catalogue', 'dance', 'wave', 'pet_level'
                );
                """
            );
    }
}
