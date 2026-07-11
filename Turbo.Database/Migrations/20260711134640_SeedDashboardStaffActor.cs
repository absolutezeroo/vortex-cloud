using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Turbo.Database.Migrations
{
    /// <summary>
    /// Seeds one reserved, account-less <c>players</c> row used as the acting player identity for
    /// dashboard-initiated room-scoped moderation (mute, kick-from-room) — those grain methods
    /// require a real, valid <c>PlayerId</c> as the actor and reject <c>ActionContext.System</c>.
    /// Resolved at runtime by name via <c>IPlayerDirectoryGrain.GetPlayerIdAsync</c>
    /// (see <c>Turbo.Dashboard.API.Operations.DashboardOperationsService</c>), so no id needs to be
    /// hardcoded anywhere.
    /// </summary>
    public partial class SeedDashboardStaffActor : Migration
    {
        private const string StaffActorName = "__dashboard_staff__";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "players",
                columns: new[] { "name" },
                values: new object[] { StaffActorName }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "players",
                keyColumn: "name",
                keyValue: StaffActorName
            );
        }
    }
}
