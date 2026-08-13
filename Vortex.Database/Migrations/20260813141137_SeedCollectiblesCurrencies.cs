using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class SeedCollectiblesCurrencies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data only, no schema change. The wallet resolves a currency through currency_types,
            // so silver and emeralds having no row there meant every grant of them found nothing to
            // credit and returned without doing anything.
            migrationBuilder.Sql(Seeds.SeedScripts.Read("collectibles_currencies.sql"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Only the rows this migration could have added, and only while no player holds a
            // balance in them — dropping a currency somebody owns would delete that balance through
            // the cascade.
            migrationBuilder.Sql(
                """
                DELETE FROM `currency_types`
                 WHERE `type` IN (2, 3)
                   AND NOT EXISTS (
                       SELECT 1 FROM `player_currencies` AS `held`
                        WHERE `held`.`currency_type_id` = `currency_types`.`id`
                   );
                """
            );
        }
    }
}
