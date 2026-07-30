using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class RemoveRosterBoardPositionStatusColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HangoutAtUtc",
                table: "RosterBoardPositions");

            migrationBuilder.DropColumn(
                name: "HangoutStatus",
                table: "RosterBoardPositions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "HangoutAtUtc",
                table: "RosterBoardPositions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HangoutStatus",
                table: "RosterBoardPositions",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }
    }
}
