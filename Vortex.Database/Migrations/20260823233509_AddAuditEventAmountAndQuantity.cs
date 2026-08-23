using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditEventAmountAndQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "amount",
                table: "audit_events",
                type: "bigint",
                nullable: true
            );

            migrationBuilder.AddColumn<int>(
                name: "quantity",
                table: "audit_events",
                type: "int",
                nullable: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "amount", table: "audit_events");

            migrationBuilder.DropColumn(name: "quantity", table: "audit_events");
        }
    }
}
