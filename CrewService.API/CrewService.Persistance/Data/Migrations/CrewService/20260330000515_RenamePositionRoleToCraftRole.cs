using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class RenamePositionRoleToCraftRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CrewPositions_PositionRoles_PositionRoleCtrlNbr",
                table: "CrewPositions");

            migrationBuilder.DropForeignKey(
                name: "FK_PayRates_PositionRoles_PositionRoleCtrlNbr",
                table: "PayRates");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionSlots_PositionRoles_PositionRoleCtrlNbr",
                table: "PositionSlots");

            migrationBuilder.DropForeignKey(
                name: "FK_SlotRequirements_PositionRoles_PositionRoleCtrlNbr",
                table: "SlotRequirements");

            migrationBuilder.DropTable(
                name: "PositionRoles");

            migrationBuilder.RenameColumn(
                name: "PositionRoleCtrlNbr",
                table: "SlotRequirements",
                newName: "CraftRoleCtrlNbr");

            migrationBuilder.RenameIndex(
                name: "IX_SlotRequirements_PositionRoleCtrlNbr",
                table: "SlotRequirements",
                newName: "IX_SlotRequirements_CraftRoleCtrlNbr");

            migrationBuilder.RenameColumn(
                name: "PositionRoleCtrlNbr",
                table: "PositionSlots",
                newName: "CraftRoleCtrlNbr");

            migrationBuilder.RenameIndex(
                name: "IX_PositionSlots_PositionRoleCtrlNbr",
                table: "PositionSlots",
                newName: "IX_PositionSlots_CraftRoleCtrlNbr");

            migrationBuilder.RenameColumn(
                name: "PositionRoleCtrlNbr",
                table: "PayRates",
                newName: "CraftRoleCtrlNbr");

            migrationBuilder.RenameIndex(
                name: "IX_PayRates_PositionRoleCtrlNbr",
                table: "PayRates",
                newName: "IX_PayRates_CraftRoleCtrlNbr");

            migrationBuilder.RenameColumn(
                name: "PositionRoleCtrlNbr",
                table: "CrewPositions",
                newName: "CraftRoleCtrlNbr");

            migrationBuilder.RenameIndex(
                name: "IX_CrewPositions_PositionRoleCtrlNbr",
                table: "CrewPositions",
                newName: "IX_CrewPositions_CraftRoleCtrlNbr");

            migrationBuilder.CreateTable(
                name: "CraftRoles",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CraftCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AlternateName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_CraftRoles", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_CraftRoles_Crafts_CraftCtrlNbr",
                        column: x => x.CraftCtrlNbr,
                        principalTable: "Crafts",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CraftRoles_CraftCtrlNbr",
                table: "CraftRoles",
                column: "CraftCtrlNbr");

            migrationBuilder.AddForeignKey(
                name: "FK_CrewPositions_CraftRoles_CraftRoleCtrlNbr",
                table: "CrewPositions",
                column: "CraftRoleCtrlNbr",
                principalTable: "CraftRoles",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PayRates_CraftRoles_CraftRoleCtrlNbr",
                table: "PayRates",
                column: "CraftRoleCtrlNbr",
                principalTable: "CraftRoles",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PositionSlots_CraftRoles_CraftRoleCtrlNbr",
                table: "PositionSlots",
                column: "CraftRoleCtrlNbr",
                principalTable: "CraftRoles",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SlotRequirements_CraftRoles_CraftRoleCtrlNbr",
                table: "SlotRequirements",
                column: "CraftRoleCtrlNbr",
                principalTable: "CraftRoles",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CrewPositions_CraftRoles_CraftRoleCtrlNbr",
                table: "CrewPositions");

            migrationBuilder.DropForeignKey(
                name: "FK_PayRates_CraftRoles_CraftRoleCtrlNbr",
                table: "PayRates");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionSlots_CraftRoles_CraftRoleCtrlNbr",
                table: "PositionSlots");

            migrationBuilder.DropForeignKey(
                name: "FK_SlotRequirements_CraftRoles_CraftRoleCtrlNbr",
                table: "SlotRequirements");

            migrationBuilder.DropTable(
                name: "CraftRoles");

            migrationBuilder.RenameColumn(
                name: "CraftRoleCtrlNbr",
                table: "SlotRequirements",
                newName: "PositionRoleCtrlNbr");

            migrationBuilder.RenameIndex(
                name: "IX_SlotRequirements_CraftRoleCtrlNbr",
                table: "SlotRequirements",
                newName: "IX_SlotRequirements_PositionRoleCtrlNbr");

            migrationBuilder.RenameColumn(
                name: "CraftRoleCtrlNbr",
                table: "PositionSlots",
                newName: "PositionRoleCtrlNbr");

            migrationBuilder.RenameIndex(
                name: "IX_PositionSlots_CraftRoleCtrlNbr",
                table: "PositionSlots",
                newName: "IX_PositionSlots_PositionRoleCtrlNbr");

            migrationBuilder.RenameColumn(
                name: "CraftRoleCtrlNbr",
                table: "PayRates",
                newName: "PositionRoleCtrlNbr");

            migrationBuilder.RenameIndex(
                name: "IX_PayRates_CraftRoleCtrlNbr",
                table: "PayRates",
                newName: "IX_PayRates_PositionRoleCtrlNbr");

            migrationBuilder.RenameColumn(
                name: "CraftRoleCtrlNbr",
                table: "CrewPositions",
                newName: "PositionRoleCtrlNbr");

            migrationBuilder.RenameIndex(
                name: "IX_CrewPositions_CraftRoleCtrlNbr",
                table: "CrewPositions",
                newName: "IX_CrewPositions_PositionRoleCtrlNbr");

            migrationBuilder.CreateTable(
                name: "PositionRoles",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    AlternateName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CraftCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PositionRoles", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_PositionRoles_Crafts_CraftCtrlNbr",
                        column: x => x.CraftCtrlNbr,
                        principalTable: "Crafts",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PositionRoles_CraftCtrlNbr",
                table: "PositionRoles",
                column: "CraftCtrlNbr");

            migrationBuilder.AddForeignKey(
                name: "FK_CrewPositions_PositionRoles_PositionRoleCtrlNbr",
                table: "CrewPositions",
                column: "PositionRoleCtrlNbr",
                principalTable: "PositionRoles",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PayRates_PositionRoles_PositionRoleCtrlNbr",
                table: "PayRates",
                column: "PositionRoleCtrlNbr",
                principalTable: "PositionRoles",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PositionSlots_PositionRoles_PositionRoleCtrlNbr",
                table: "PositionSlots",
                column: "PositionRoleCtrlNbr",
                principalTable: "PositionRoles",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SlotRequirements_PositionRoles_PositionRoleCtrlNbr",
                table: "SlotRequirements",
                column: "PositionRoleCtrlNbr",
                principalTable: "PositionRoles",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
