using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class RosterBoardUnification_BoardTypeRotationType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoardMembers");

            migrationBuilder.DropTable(
                name: "ExtraBoards");

            migrationBuilder.AddColumn<string>(
                name: "BoardType",
                table: "RosterBoards",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "RosterCtrlNbr",
                table: "RosterBoards",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "RotationType",
                table: "RosterBoards",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_RosterBoards_RosterCtrlNbr",
                table: "RosterBoards",
                column: "RosterCtrlNbr");

            migrationBuilder.AddForeignKey(
                name: "FK_RosterBoards_Rosters_RosterCtrlNbr",
                table: "RosterBoards",
                column: "RosterCtrlNbr",
                principalTable: "Rosters",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RosterBoards_Rosters_RosterCtrlNbr",
                table: "RosterBoards");

            migrationBuilder.DropIndex(
                name: "IX_RosterBoards_RosterCtrlNbr",
                table: "RosterBoards");

            migrationBuilder.DropColumn(
                name: "BoardType",
                table: "RosterBoards");

            migrationBuilder.DropColumn(
                name: "RosterCtrlNbr",
                table: "RosterBoards");

            migrationBuilder.DropColumn(
                name: "RotationType",
                table: "RosterBoards");

            migrationBuilder.CreateTable(
                name: "ExtraBoards",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    AuxBoardType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    BoardKind = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CraftCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PlacedGroupCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtraBoards", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_ExtraBoards_Crafts_CraftCtrlNbr",
                        column: x => x.CraftCtrlNbr,
                        principalTable: "Crafts",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExtraBoards_DynamicGroups_PlacedGroupCtrlNbr",
                        column: x => x.PlacedGroupCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BoardMembers",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExtraBoardCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StateJson = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardMembers", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_BoardMembers_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BoardMembers_ExtraBoards_ExtraBoardCtrlNbr",
                        column: x => x.ExtraBoardCtrlNbr,
                        principalTable: "ExtraBoards",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BoardMembers_EmployeeCtrlNbr",
                table: "BoardMembers",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_BoardMembers_ExtraBoardCtrlNbr",
                table: "BoardMembers",
                column: "ExtraBoardCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_ExtraBoards_CraftCtrlNbr",
                table: "ExtraBoards",
                column: "CraftCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_ExtraBoards_PlacedGroupCtrlNbr",
                table: "ExtraBoards",
                column: "PlacedGroupCtrlNbr");
        }
    }
}
