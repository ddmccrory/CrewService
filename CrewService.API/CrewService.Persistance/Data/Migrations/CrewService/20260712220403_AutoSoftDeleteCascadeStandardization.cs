using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AutoSoftDeleteCascadeStandardization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BoardCascadePolicies_Crafts_CraftCtrlNbr",
                table: "BoardCascadePolicies");

            migrationBuilder.DropForeignKey(
                name: "FK_BulletinPolicies_Crafts_CraftCtrlNbr",
                table: "BulletinPolicies");

            migrationBuilder.DropForeignKey(
                name: "FK_CallSheetRule_Department_DepartmentCtrlNbr",
                table: "CallSheetRule");

            migrationBuilder.DropForeignKey(
                name: "FK_CraftDisplacementPolicies_Crafts_CraftCtrlNbr",
                table: "CraftDisplacementPolicies");

            migrationBuilder.DropForeignKey(
                name: "FK_CraftOperationsPolicies_Crafts_CraftCtrlNbr",
                table: "CraftOperationsPolicies");

            migrationBuilder.DropForeignKey(
                name: "FK_CraftRegulatoryQualifications_Crafts_CraftCtrlNbr",
                table: "CraftRegulatoryQualifications");

            migrationBuilder.DropForeignKey(
                name: "FK_CraftRoles_Crafts_CraftCtrlNbr",
                table: "CraftRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_Crafts_Department_DepartmentCtrlNbr",
                table: "Crafts");

            migrationBuilder.DropForeignKey(
                name: "FK_DepartmentReassignmentRules_Department_DepartmentCtrlNbr",
                table: "DepartmentReassignmentRules");

            migrationBuilder.DropForeignKey(
                name: "FK_DisplacementCases_Crafts_CraftCtrlNbr",
                table: "DisplacementCases");

            migrationBuilder.DropForeignKey(
                name: "FK_HolidayQualificationRules_Crafts_CraftCtrlNbr",
                table: "HolidayQualificationRules");

            migrationBuilder.DropForeignKey(
                name: "FK_PayRates_Crafts_CraftCtrlNbr",
                table: "PayRates");

            migrationBuilder.DropForeignKey(
                name: "FK_QualificationTypes_Crafts_CraftCtrlNbr",
                table: "QualificationTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_RosterBoards_Rosters_RosterCtrlNbr",
                table: "RosterBoards");

            migrationBuilder.DropForeignKey(
                name: "FK_Rosters_Crafts_CraftCtrlNbr",
                table: "Rosters");

            migrationBuilder.DropForeignKey(
                name: "FK_Seniority_Rosters_RosterCtrlNbr",
                table: "Seniority");

            migrationBuilder.DropForeignKey(
                name: "FK_SeniorityMovePolicies_Crafts_CraftCtrlNbr",
                table: "SeniorityMovePolicies");

            migrationBuilder.DropForeignKey(
                name: "FK_SeniorityMoves_Crafts_CraftCtrlNbr",
                table: "SeniorityMoves");

            migrationBuilder.AddForeignKey(
                name: "FK_BoardCascadePolicies_Crafts_CraftCtrlNbr",
                table: "BoardCascadePolicies",
                column: "CraftCtrlNbr",
                principalTable: "Crafts",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BulletinPolicies_Crafts_CraftCtrlNbr",
                table: "BulletinPolicies",
                column: "CraftCtrlNbr",
                principalTable: "Crafts",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CallSheetRule_Department_DepartmentCtrlNbr",
                table: "CallSheetRule",
                column: "DepartmentCtrlNbr",
                principalTable: "Department",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CraftDisplacementPolicies_Crafts_CraftCtrlNbr",
                table: "CraftDisplacementPolicies",
                column: "CraftCtrlNbr",
                principalTable: "Crafts",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CraftOperationsPolicies_Crafts_CraftCtrlNbr",
                table: "CraftOperationsPolicies",
                column: "CraftCtrlNbr",
                principalTable: "Crafts",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CraftRegulatoryQualifications_Crafts_CraftCtrlNbr",
                table: "CraftRegulatoryQualifications",
                column: "CraftCtrlNbr",
                principalTable: "Crafts",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CraftRoles_Crafts_CraftCtrlNbr",
                table: "CraftRoles",
                column: "CraftCtrlNbr",
                principalTable: "Crafts",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Crafts_Department_DepartmentCtrlNbr",
                table: "Crafts",
                column: "DepartmentCtrlNbr",
                principalTable: "Department",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DepartmentReassignmentRules_Department_DepartmentCtrlNbr",
                table: "DepartmentReassignmentRules",
                column: "DepartmentCtrlNbr",
                principalTable: "Department",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DisplacementCases_Crafts_CraftCtrlNbr",
                table: "DisplacementCases",
                column: "CraftCtrlNbr",
                principalTable: "Crafts",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HolidayQualificationRules_Crafts_CraftCtrlNbr",
                table: "HolidayQualificationRules",
                column: "CraftCtrlNbr",
                principalTable: "Crafts",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PayRates_Crafts_CraftCtrlNbr",
                table: "PayRates",
                column: "CraftCtrlNbr",
                principalTable: "Crafts",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QualificationTypes_Crafts_CraftCtrlNbr",
                table: "QualificationTypes",
                column: "CraftCtrlNbr",
                principalTable: "Crafts",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RosterBoards_Rosters_RosterCtrlNbr",
                table: "RosterBoards",
                column: "RosterCtrlNbr",
                principalTable: "Rosters",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Rosters_Crafts_CraftCtrlNbr",
                table: "Rosters",
                column: "CraftCtrlNbr",
                principalTable: "Crafts",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Seniority_Rosters_RosterCtrlNbr",
                table: "Seniority",
                column: "RosterCtrlNbr",
                principalTable: "Rosters",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SeniorityMovePolicies_Crafts_CraftCtrlNbr",
                table: "SeniorityMovePolicies",
                column: "CraftCtrlNbr",
                principalTable: "Crafts",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SeniorityMoves_Crafts_CraftCtrlNbr",
                table: "SeniorityMoves",
                column: "CraftCtrlNbr",
                principalTable: "Crafts",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BoardCascadePolicies_Crafts_CraftCtrlNbr",
                table: "BoardCascadePolicies");

            migrationBuilder.DropForeignKey(
                name: "FK_BulletinPolicies_Crafts_CraftCtrlNbr",
                table: "BulletinPolicies");

            migrationBuilder.DropForeignKey(
                name: "FK_CallSheetRule_Department_DepartmentCtrlNbr",
                table: "CallSheetRule");

            migrationBuilder.DropForeignKey(
                name: "FK_CraftDisplacementPolicies_Crafts_CraftCtrlNbr",
                table: "CraftDisplacementPolicies");

            migrationBuilder.DropForeignKey(
                name: "FK_CraftOperationsPolicies_Crafts_CraftCtrlNbr",
                table: "CraftOperationsPolicies");

            migrationBuilder.DropForeignKey(
                name: "FK_CraftRegulatoryQualifications_Crafts_CraftCtrlNbr",
                table: "CraftRegulatoryQualifications");

            migrationBuilder.DropForeignKey(
                name: "FK_CraftRoles_Crafts_CraftCtrlNbr",
                table: "CraftRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_Crafts_Department_DepartmentCtrlNbr",
                table: "Crafts");

            migrationBuilder.DropForeignKey(
                name: "FK_DepartmentReassignmentRules_Department_DepartmentCtrlNbr",
                table: "DepartmentReassignmentRules");

            migrationBuilder.DropForeignKey(
                name: "FK_DisplacementCases_Crafts_CraftCtrlNbr",
                table: "DisplacementCases");

            migrationBuilder.DropForeignKey(
                name: "FK_HolidayQualificationRules_Crafts_CraftCtrlNbr",
                table: "HolidayQualificationRules");

            migrationBuilder.DropForeignKey(
                name: "FK_PayRates_Crafts_CraftCtrlNbr",
                table: "PayRates");

            migrationBuilder.DropForeignKey(
                name: "FK_QualificationTypes_Crafts_CraftCtrlNbr",
                table: "QualificationTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_RosterBoards_Rosters_RosterCtrlNbr",
                table: "RosterBoards");

            migrationBuilder.DropForeignKey(
                name: "FK_Rosters_Crafts_CraftCtrlNbr",
                table: "Rosters");

            migrationBuilder.DropForeignKey(
                name: "FK_Seniority_Rosters_RosterCtrlNbr",
                table: "Seniority");

            migrationBuilder.DropForeignKey(
                name: "FK_SeniorityMovePolicies_Crafts_CraftCtrlNbr",
                table: "SeniorityMovePolicies");

            migrationBuilder.DropForeignKey(
                name: "FK_SeniorityMoves_Crafts_CraftCtrlNbr",
                table: "SeniorityMoves");

            migrationBuilder.AddForeignKey(
                name: "FK_BoardCascadePolicies_Crafts_CraftCtrlNbr",
                table: "BoardCascadePolicies",
                column: "CraftCtrlNbr",
                principalTable: "Crafts",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BulletinPolicies_Crafts_CraftCtrlNbr",
                table: "BulletinPolicies",
                column: "CraftCtrlNbr",
                principalTable: "Crafts",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CallSheetRule_Department_DepartmentCtrlNbr",
                table: "CallSheetRule",
                column: "DepartmentCtrlNbr",
                principalTable: "Department",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CraftDisplacementPolicies_Crafts_CraftCtrlNbr",
                table: "CraftDisplacementPolicies",
                column: "CraftCtrlNbr",
                principalTable: "Crafts",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CraftOperationsPolicies_Crafts_CraftCtrlNbr",
                table: "CraftOperationsPolicies",
                column: "CraftCtrlNbr",
                principalTable: "Crafts",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CraftRegulatoryQualifications_Crafts_CraftCtrlNbr",
                table: "CraftRegulatoryQualifications",
                column: "CraftCtrlNbr",
                principalTable: "Crafts",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CraftRoles_Crafts_CraftCtrlNbr",
                table: "CraftRoles",
                column: "CraftCtrlNbr",
                principalTable: "Crafts",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Crafts_Department_DepartmentCtrlNbr",
                table: "Crafts",
                column: "DepartmentCtrlNbr",
                principalTable: "Department",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DepartmentReassignmentRules_Department_DepartmentCtrlNbr",
                table: "DepartmentReassignmentRules",
                column: "DepartmentCtrlNbr",
                principalTable: "Department",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DisplacementCases_Crafts_CraftCtrlNbr",
                table: "DisplacementCases",
                column: "CraftCtrlNbr",
                principalTable: "Crafts",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HolidayQualificationRules_Crafts_CraftCtrlNbr",
                table: "HolidayQualificationRules",
                column: "CraftCtrlNbr",
                principalTable: "Crafts",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PayRates_Crafts_CraftCtrlNbr",
                table: "PayRates",
                column: "CraftCtrlNbr",
                principalTable: "Crafts",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QualificationTypes_Crafts_CraftCtrlNbr",
                table: "QualificationTypes",
                column: "CraftCtrlNbr",
                principalTable: "Crafts",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RosterBoards_Rosters_RosterCtrlNbr",
                table: "RosterBoards",
                column: "RosterCtrlNbr",
                principalTable: "Rosters",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Rosters_Crafts_CraftCtrlNbr",
                table: "Rosters",
                column: "CraftCtrlNbr",
                principalTable: "Crafts",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Seniority_Rosters_RosterCtrlNbr",
                table: "Seniority",
                column: "RosterCtrlNbr",
                principalTable: "Rosters",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SeniorityMovePolicies_Crafts_CraftCtrlNbr",
                table: "SeniorityMovePolicies",
                column: "CraftCtrlNbr",
                principalTable: "Crafts",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SeniorityMoves_Crafts_CraftCtrlNbr",
                table: "SeniorityMoves",
                column: "CraftCtrlNbr",
                principalTable: "Crafts",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
