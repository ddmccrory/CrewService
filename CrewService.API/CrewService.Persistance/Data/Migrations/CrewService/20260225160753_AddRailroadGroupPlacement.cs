using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddRailroadGroupPlacement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RailroadGroupPlacements",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    RailroadCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    GroupCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RailroadGroupPlacements", x => x.CtrlNbr);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RailroadGroupPlacements_GroupCtrlNbr",
                table: "RailroadGroupPlacements",
                column: "GroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_RailroadGroupPlacements_RailroadCtrlNbr",
                table: "RailroadGroupPlacements",
                column: "RailroadCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_RailroadGroupPlacements_RailroadCtrlNbr_GroupCtrlNbr",
                table: "RailroadGroupPlacements",
                columns: new[] { "RailroadCtrlNbr", "GroupCtrlNbr" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RailroadGroupPlacements");
        }
    }
}
