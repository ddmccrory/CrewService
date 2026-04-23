using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddCraftRoleQualifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SlotRequirements_RegulatoryQualifications_QualificationTypeCtrlNbr",
                table: "SlotRequirements");

            migrationBuilder.CreateTable(
                name: "CraftRoleQualifications",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CraftRoleCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    QualificationTypeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CraftRoleCtrlNbr1 = table.Column<long>(type: "INTEGER", nullable: true),
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
                    table.PrimaryKey("PK_CraftRoleQualifications", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_CraftRoleQualifications_CraftRoles_CraftRoleCtrlNbr",
                        column: x => x.CraftRoleCtrlNbr,
                        principalTable: "CraftRoles",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CraftRoleQualifications_CraftRoles_CraftRoleCtrlNbr1",
                        column: x => x.CraftRoleCtrlNbr1,
                        principalTable: "CraftRoles",
                        principalColumn: "CtrlNbr");
                    table.ForeignKey(
                        name: "FK_CraftRoleQualifications_QualificationTypes_QualificationTypeCtrlNbr",
                        column: x => x.QualificationTypeCtrlNbr,
                        principalTable: "QualificationTypes",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CraftRoleQualifications_CraftRoleCtrlNbr_QualificationTypeCtrlNbr",
                table: "CraftRoleQualifications",
                columns: new[] { "CraftRoleCtrlNbr", "QualificationTypeCtrlNbr" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CraftRoleQualifications_CraftRoleCtrlNbr1",
                table: "CraftRoleQualifications",
                column: "CraftRoleCtrlNbr1");

            migrationBuilder.CreateIndex(
                name: "IX_CraftRoleQualifications_QualificationTypeCtrlNbr",
                table: "CraftRoleQualifications",
                column: "QualificationTypeCtrlNbr");

            migrationBuilder.AddForeignKey(
                name: "FK_SlotRequirements_QualificationTypes_QualificationTypeCtrlNbr",
                table: "SlotRequirements",
                column: "QualificationTypeCtrlNbr",
                principalTable: "QualificationTypes",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SlotRequirements_QualificationTypes_QualificationTypeCtrlNbr",
                table: "SlotRequirements");

            migrationBuilder.DropTable(
                name: "CraftRoleQualifications");

            migrationBuilder.AddForeignKey(
                name: "FK_SlotRequirements_RegulatoryQualifications_QualificationTypeCtrlNbr",
                table: "SlotRequirements",
                column: "QualificationTypeCtrlNbr",
                principalTable: "RegulatoryQualifications",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
