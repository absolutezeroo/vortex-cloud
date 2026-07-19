using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class FixDeletedAtColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "security_tickets",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .OldAnnotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "rooms",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .OldAnnotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "room_rights",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .OldAnnotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "room_mutes",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .OldAnnotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "room_models",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .OldAnnotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "room_entry_logs",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .OldAnnotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "room_chatlogs",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .OldAnnotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "room_bans",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .OldAnnotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "players",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .OldAnnotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "player_navigator_view_modes",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .OldAnnotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "player_navigator_saved_searches",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .OldAnnotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "player_navigator_preferences",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .OldAnnotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "player_navigator_collapsed_categories",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .OldAnnotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "player_favorite_rooms",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .OldAnnotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "player_currencies",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .OldAnnotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "player_chat_styles_owned",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .OldAnnotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "player_chat_styles",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .OldAnnotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "player_badges",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .OldAnnotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "navigator_top_level_contexts",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .OldAnnotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "navigator_quick_links",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .OldAnnotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "navigator_flatcats",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .OldAnnotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "navigator_eventcats",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .OldAnnotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "messenger_requests",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .OldAnnotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "messenger_friends",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .OldAnnotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "messenger_categories",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .OldAnnotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "furniture_teleport_links",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .OldAnnotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "furniture_definitions",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .OldAnnotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "furniture",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .OldAnnotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "currency_types",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .OldAnnotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "catalog_products",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .OldAnnotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "catalog_pages",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .OldAnnotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "catalog_offers",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .OldAnnotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "security_tickets",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .Annotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "rooms",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .Annotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "room_rights",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .Annotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "room_mutes",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .Annotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "room_models",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .Annotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "room_entry_logs",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .Annotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "room_chatlogs",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .Annotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "room_bans",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .Annotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "players",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .Annotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "player_navigator_view_modes",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .Annotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "player_navigator_saved_searches",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .Annotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "player_navigator_preferences",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .Annotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "player_navigator_collapsed_categories",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .Annotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "player_favorite_rooms",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .Annotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "player_currencies",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .Annotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "player_chat_styles_owned",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .Annotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "player_chat_styles",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .Annotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "player_badges",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .Annotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "navigator_top_level_contexts",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .Annotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "navigator_quick_links",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .Annotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "navigator_flatcats",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .Annotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "navigator_eventcats",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .Annotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "messenger_requests",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .Annotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "messenger_friends",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .Annotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "messenger_categories",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .Annotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "furniture_teleport_links",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .Annotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "furniture_definitions",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .Annotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "furniture",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .Annotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "currency_types",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .Annotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "catalog_products",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .Annotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "catalog_pages",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .Annotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );

            migrationBuilder
                .AlterColumn<DateTime>(
                    name: "deleted_at",
                    table: "catalog_offers",
                    type: "datetime(6)",
                    nullable: true,
                    oldClrType: typeof(DateTime),
                    oldType: "datetime(6)",
                    oldNullable: true
                )
                .Annotation(
                    "MySql:ValueGenerationStrategy",
                    MySqlValueGenerationStrategy.ComputedColumn
                );
        }
    }
}
