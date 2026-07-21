using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestTargetAndEnabled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "enabled",
                table: "quests",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true
            );

            migrationBuilder
                .AddColumn<string>(
                    name: "target_type",
                    table: "quests",
                    type: "varchar(512)",
                    maxLength: 512,
                    nullable: false,
                    defaultValue: ""
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .AddColumn<string>(
                    name: "target_value",
                    table: "quests",
                    type: "varchar(512)",
                    maxLength: 512,
                    nullable: false,
                    defaultValue: ""
                )
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "enabled", table: "quests");

            migrationBuilder.DropColumn(name: "target_type", table: "quests");

            migrationBuilder.DropColumn(name: "target_value", table: "quests");
        }
    }
}
