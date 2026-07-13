using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddNoAccessPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NoAccessPolicies",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    RailroadCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CraftCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowEmployeeSelfRequest = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequireBulletinAccessAudit = table.Column<bool>(type: "INTEGER", nullable: false),
                    BlockIfOnExtendedAbsence = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequirePositionCurrentlyAssigned = table.Column<bool>(type: "INTEGER", nullable: false),
                    ApplyExtraBoardSpecialCase = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequireBoardAvailableForMoveOff = table.Column<bool>(type: "INTEGER", nullable: false),
                    AutoApproveNoAccess = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowAdminOverride = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultEffectiveMode = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
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
                    table.PrimaryKey("PK_NoAccessPolicies", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_NoAccessPolicies_Crafts_CraftCtrlNbr",
                        column: x => x.CraftCtrlNbr,
                        principalTable: "Crafts",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NoAccessPolicies_DynamicGroups_RailroadCtrlNbr",
                        column: x => x.RailroadCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NoAccessPolicies_CraftCtrlNbr",
                table: "NoAccessPolicies",
                column: "CraftCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_NoAccessPolicies_RailroadCtrlNbr_CraftCtrlNbr",
                table: "NoAccessPolicies",
                columns: new[] { "RailroadCtrlNbr", "CraftCtrlNbr" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NoAccessPolicies");
        }
    }
}
