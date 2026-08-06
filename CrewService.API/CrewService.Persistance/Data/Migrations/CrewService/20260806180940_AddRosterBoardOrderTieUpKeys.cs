using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddRosterBoardOrderTieUpKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderSeedBoardPosition",
                table: "RosterBoardPositions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OrderTieUpTimeUtc",
                table: "RosterBoardPositions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE RosterBoardPositions
                SET OrderSeedBoardPosition = PositionOrder
                WHERE OrderSeedBoardPosition IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderSeedBoardPosition",
                table: "RosterBoardPositions");

            migrationBuilder.DropColumn(
                name: "OrderTieUpTimeUtc",
                table: "RosterBoardPositions");
        }
    }
}
