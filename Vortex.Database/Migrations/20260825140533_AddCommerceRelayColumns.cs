using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddCommerceRelayColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder
                .AddColumn<string>(
                    name: "relay_payload",
                    table: "commerce_operations",
                    type: "varchar(4096)",
                    maxLength: 4096,
                    nullable: true
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .AddColumn<string>(
                    name: "relay_type",
                    table: "commerce_operations",
                    type: "varchar(128)",
                    maxLength: 128,
                    nullable: true
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "relayed_at",
                table: "commerce_operations",
                type: "datetime(6)",
                nullable: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_commerce_operations_relayed_at",
                table: "commerce_operations",
                column: "relayed_at"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_commerce_operations_relayed_at",
                table: "commerce_operations"
            );

            migrationBuilder.DropColumn(name: "relay_payload", table: "commerce_operations");

            migrationBuilder.DropColumn(name: "relay_type", table: "commerce_operations");

            migrationBuilder.DropColumn(name: "relayed_at", table: "commerce_operations");
        }
    }
}
