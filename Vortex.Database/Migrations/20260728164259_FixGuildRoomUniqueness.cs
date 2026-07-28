using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <summary>
    /// Moves the "one guild per room" constraint from <c>groups.room_id</c> to
    /// <c>rooms.group_id</c>. The guild side kept its room id after a soft delete, and MySQL has no
    /// partial indexes, so a unique constraint there permanently burned a room as a future guild
    /// base. <c>rooms.group_id</c> is nullable and goes NULL when a guild is dissolved, and MySQL
    /// allows repeated NULLs in a unique index, so the invariant survives while reuse works.
    /// </summary>
    /// <remarks>
    /// Both columns back a foreign key, and MySQL refuses to drop the last index a foreign key
    /// relies on. So each swap adds the replacement index first, drops the old one while the
    /// replacement is already satisfying the constraint, then renames it into place — rather than
    /// EF's default drop-then-create, which fails with "needed in a foreign key constraint".
    /// </remarks>
    public partial class FixGuildRoomUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE `rooms` ADD UNIQUE INDEX `IX_rooms_group_id_new` (`group_id`);"
            );
            migrationBuilder.Sql("ALTER TABLE `rooms` DROP INDEX `IX_rooms_group_id`;");
            migrationBuilder.Sql(
                "ALTER TABLE `rooms` RENAME INDEX `IX_rooms_group_id_new` TO `IX_rooms_group_id`;"
            );

            migrationBuilder.Sql(
                "ALTER TABLE `groups` ADD INDEX `IX_groups_room_id_new` (`room_id`);"
            );
            migrationBuilder.Sql("ALTER TABLE `groups` DROP INDEX `IX_groups_room_id`;");
            migrationBuilder.Sql(
                "ALTER TABLE `groups` RENAME INDEX `IX_groups_room_id_new` TO `IX_groups_room_id`;"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE `groups` ADD UNIQUE INDEX `IX_groups_room_id_new` (`room_id`);"
            );
            migrationBuilder.Sql("ALTER TABLE `groups` DROP INDEX `IX_groups_room_id`;");
            migrationBuilder.Sql(
                "ALTER TABLE `groups` RENAME INDEX `IX_groups_room_id_new` TO `IX_groups_room_id`;"
            );

            migrationBuilder.Sql(
                "ALTER TABLE `rooms` ADD INDEX `IX_rooms_group_id_new` (`group_id`);"
            );
            migrationBuilder.Sql("ALTER TABLE `rooms` DROP INDEX `IX_rooms_group_id`;");
            migrationBuilder.Sql(
                "ALTER TABLE `rooms` RENAME INDEX `IX_rooms_group_id_new` TO `IX_rooms_group_id`;"
            );
        }
    }
}
