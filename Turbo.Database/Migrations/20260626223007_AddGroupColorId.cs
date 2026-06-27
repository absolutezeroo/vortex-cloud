using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Turbo.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupColorId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Wipe seed data inserted before color_id column existed (all rows had color_id=0).
            migrationBuilder.Sql("TRUNCATE TABLE `group_colors`;");

            // Idempotent: add color_id only if it does not already exist.
            migrationBuilder.Sql(
                """
                SET @_col = (
                    SELECT COUNT(*) FROM information_schema.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                    AND TABLE_NAME = 'group_colors'
                    AND COLUMN_NAME = 'color_id'
                );
                SET @_sql = IF(@_col = 0,
                    'ALTER TABLE `group_colors` ADD COLUMN `color_id` int NOT NULL DEFAULT 0',
                    'SELECT 1');
                PREPARE _s FROM @_sql;
                EXECUTE _s;
                DEALLOCATE PREPARE _s;
                """
            );

            // Idempotent: create the unique index only if it does not already exist.
            migrationBuilder.Sql(
                """
                SET @_idx = (
                    SELECT COUNT(*) FROM information_schema.STATISTICS
                    WHERE TABLE_SCHEMA = DATABASE()
                    AND TABLE_NAME = 'group_colors'
                    AND INDEX_NAME = 'IX_group_colors_color_id'
                );
                SET @_sql2 = IF(@_idx = 0,
                    'CREATE UNIQUE INDEX `IX_group_colors_color_id` ON `group_colors` (`color_id`)',
                    'SELECT 1');
                PREPARE _s2 FROM @_sql2;
                EXECUTE _s2;
                DEALLOCATE PREPARE _s2;
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_group_colors_color_id", table: "group_colors");

            migrationBuilder.DropColumn(name: "color_id", table: "group_colors");
        }
    }
}
