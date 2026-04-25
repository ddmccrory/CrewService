using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddSlotRequirementRegulatoryQualFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CraftRoleQualifications_CraftRoles_CraftRoleCtrlNbr1",
                table: "CraftRoleQualifications");

            migrationBuilder.DropIndex(
                name: "IX_CraftRoleQualifications_CraftRoleCtrlNbr1",
                table: "CraftRoleQualifications");

            migrationBuilder.DropColumn(
                name: "CraftRoleCtrlNbr1",
                table: "CraftRoleQualifications");

            migrationBuilder.AddColumn<long>(
                name: "RegulatoryQualificationCtrlNbr",
                table: "SlotRequirements",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlotRequirements_RegulatoryQualificationCtrlNbr",
                table: "SlotRequirements",
                column: "RegulatoryQualificationCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_QualificationRequirements_RequiredRegulatoryQualCtrlNbr",
                table: "QualificationRequirements",
                column: "RequiredRegulatoryQualCtrlNbr");

            migrationBuilder.AddForeignKey(
                name: "FK_QualificationRequirements_RegulatoryQualifications_RequiredRegulatoryQualCtrlNbr",
                table: "QualificationRequirements",
                column: "RequiredRegulatoryQualCtrlNbr",
                principalTable: "RegulatoryQualifications",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SlotRequirements_RegulatoryQualifications_RegulatoryQualificationCtrlNbr",
                table: "SlotRequirements",
                column: "RegulatoryQualificationCtrlNbr",
                principalTable: "RegulatoryQualifications",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QualificationRequirements_RegulatoryQualifications_RequiredRegulatoryQualCtrlNbr",
                table: "QualificationRequirements");

            migrationBuilder.DropForeignKey(
                name: "FK_SlotRequirements_RegulatoryQualifications_RegulatoryQualificationCtrlNbr",
                table: "SlotRequirements");

            migrationBuilder.DropIndex(
                name: "IX_SlotRequirements_RegulatoryQualificationCtrlNbr",
                table: "SlotRequirements");

            migrationBuilder.DropIndex(
                name: "IX_QualificationRequirements_RequiredRegulatoryQualCtrlNbr",
                table: "QualificationRequirements");

            migrationBuilder.DropColumn(
                name: "RegulatoryQualificationCtrlNbr",
                table: "SlotRequirements");

            migrationBuilder.AddColumn<long>(
                name: "CraftRoleCtrlNbr1",
                table: "CraftRoleQualifications",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CraftRoleQualifications_CraftRoleCtrlNbr1",
                table: "CraftRoleQualifications",
                column: "CraftRoleCtrlNbr1");

            migrationBuilder.AddForeignKey(
                name: "FK_CraftRoleQualifications_CraftRoles_CraftRoleCtrlNbr1",
                table: "CraftRoleQualifications",
                column: "CraftRoleCtrlNbr1",
                principalTable: "CraftRoles",
                principalColumn: "CtrlNbr");
        }
    }
}
