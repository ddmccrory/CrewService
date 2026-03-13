using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class PluralizeModuleTableNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AbsenceApprovals_AbsenceRequest_AbsenceRequestCtrlNbr",
                table: "AbsenceApprovals");

            migrationBuilder.DropForeignKey(
                name: "FK_AbsenceMarkUps_AbsenceRequest_AbsenceRequestCtrlNbr",
                table: "AbsenceMarkUps");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VacancyImpact",
                table: "VacancyImpact");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TimeEntry",
                table: "TimeEntry");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SeniorityMovePolicy",
                table: "SeniorityMovePolicy");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SeniorityMove",
                table: "SeniorityMove");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ReliefCoverageRule",
                table: "ReliefCoverageRule");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PositionVacancy",
                table: "PositionVacancy");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PayrollRun",
                table: "PayrollRun");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PayrollRecord",
                table: "PayrollRecord");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExtraBoard",
                table: "ExtraBoard");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EmployeeBooking",
                table: "EmployeeBooking");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DisplacementClaim",
                table: "DisplacementClaim");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DisplacementCase",
                table: "DisplacementCase");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DispatchProjection",
                table: "DispatchProjection");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DispatchOverride",
                table: "DispatchOverride");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DispatchDecisionLog",
                table: "DispatchDecisionLog");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CrewPosition",
                table: "CrewPosition");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CrewIncumbency",
                table: "CrewIncumbency");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CrewAttachmentTemplate",
                table: "CrewAttachmentTemplate");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CrewAttachmentInstance",
                table: "CrewAttachmentInstance");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Crew",
                table: "Crew");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CraftDisplacementPolicy",
                table: "CraftDisplacementPolicy");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BulletinPolicy",
                table: "BulletinPolicy");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BulletinBid",
                table: "BulletinBid");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Bulletin",
                table: "Bulletin");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BoardMember",
                table: "BoardMember");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BoardCascadePolicy",
                table: "BoardCascadePolicy");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AbsenceRequest",
                table: "AbsenceRequest");

            migrationBuilder.RenameTable(
                name: "VacancyImpact",
                newName: "VacancyImpacts");

            migrationBuilder.RenameTable(
                name: "TimeEntry",
                newName: "TimeEntries");

            migrationBuilder.RenameTable(
                name: "SeniorityMovePolicy",
                newName: "SeniorityMovePolicies");

            migrationBuilder.RenameTable(
                name: "SeniorityMove",
                newName: "SeniorityMoves");

            migrationBuilder.RenameTable(
                name: "ReliefCoverageRule",
                newName: "ReliefCoverageRules");

            migrationBuilder.RenameTable(
                name: "PositionVacancy",
                newName: "PositionVacancies");

            migrationBuilder.RenameTable(
                name: "PayrollRun",
                newName: "PayrollRuns");

            migrationBuilder.RenameTable(
                name: "PayrollRecord",
                newName: "PayrollRecords");

            migrationBuilder.RenameTable(
                name: "ExtraBoard",
                newName: "ExtraBoards");

            migrationBuilder.RenameTable(
                name: "EmployeeBooking",
                newName: "EmployeeBookings");

            migrationBuilder.RenameTable(
                name: "DisplacementClaim",
                newName: "DisplacementClaims");

            migrationBuilder.RenameTable(
                name: "DisplacementCase",
                newName: "DisplacementCases");

            migrationBuilder.RenameTable(
                name: "DispatchProjection",
                newName: "DispatchProjections");

            migrationBuilder.RenameTable(
                name: "DispatchOverride",
                newName: "DispatchOverrides");

            migrationBuilder.RenameTable(
                name: "DispatchDecisionLog",
                newName: "DispatchDecisionLogs");

            migrationBuilder.RenameTable(
                name: "CrewPosition",
                newName: "CrewPositions");

            migrationBuilder.RenameTable(
                name: "CrewIncumbency",
                newName: "CrewIncumbencies");

            migrationBuilder.RenameTable(
                name: "CrewAttachmentTemplate",
                newName: "CrewAttachmentTemplates");

            migrationBuilder.RenameTable(
                name: "CrewAttachmentInstance",
                newName: "CrewAttachmentInstances");

            migrationBuilder.RenameTable(
                name: "Crew",
                newName: "Crews");

            migrationBuilder.RenameTable(
                name: "CraftDisplacementPolicy",
                newName: "CraftDisplacementPolicies");

            migrationBuilder.RenameTable(
                name: "BulletinPolicy",
                newName: "BulletinPolicies");

            migrationBuilder.RenameTable(
                name: "BulletinBid",
                newName: "BulletinBids");

            migrationBuilder.RenameTable(
                name: "Bulletin",
                newName: "Bulletins");

            migrationBuilder.RenameTable(
                name: "BoardMember",
                newName: "BoardMembers");

            migrationBuilder.RenameTable(
                name: "BoardCascadePolicy",
                newName: "BoardCascadePolicies");

            migrationBuilder.RenameTable(
                name: "AbsenceRequest",
                newName: "AbsenceRequests");

            migrationBuilder.RenameIndex(
                name: "IX_PayrollRun_PayPeriod",
                table: "PayrollRuns",
                newName: "IX_PayrollRuns_PayPeriod");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VacancyImpacts",
                table: "VacancyImpacts",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TimeEntries",
                table: "TimeEntries",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SeniorityMovePolicies",
                table: "SeniorityMovePolicies",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SeniorityMoves",
                table: "SeniorityMoves",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReliefCoverageRules",
                table: "ReliefCoverageRules",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PositionVacancies",
                table: "PositionVacancies",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PayrollRuns",
                table: "PayrollRuns",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PayrollRecords",
                table: "PayrollRecords",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExtraBoards",
                table: "ExtraBoards",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EmployeeBookings",
                table: "EmployeeBookings",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DisplacementClaims",
                table: "DisplacementClaims",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DisplacementCases",
                table: "DisplacementCases",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DispatchProjections",
                table: "DispatchProjections",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DispatchOverrides",
                table: "DispatchOverrides",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DispatchDecisionLogs",
                table: "DispatchDecisionLogs",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CrewPositions",
                table: "CrewPositions",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CrewIncumbencies",
                table: "CrewIncumbencies",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CrewAttachmentTemplates",
                table: "CrewAttachmentTemplates",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CrewAttachmentInstances",
                table: "CrewAttachmentInstances",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Crews",
                table: "Crews",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CraftDisplacementPolicies",
                table: "CraftDisplacementPolicies",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BulletinPolicies",
                table: "BulletinPolicies",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BulletinBids",
                table: "BulletinBids",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Bulletins",
                table: "Bulletins",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BoardMembers",
                table: "BoardMembers",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BoardCascadePolicies",
                table: "BoardCascadePolicies",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AbsenceRequests",
                table: "AbsenceRequests",
                column: "CtrlNbr");

            migrationBuilder.AddForeignKey(
                name: "FK_AbsenceApprovals_AbsenceRequests_AbsenceRequestCtrlNbr",
                table: "AbsenceApprovals",
                column: "AbsenceRequestCtrlNbr",
                principalTable: "AbsenceRequests",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AbsenceMarkUps_AbsenceRequests_AbsenceRequestCtrlNbr",
                table: "AbsenceMarkUps",
                column: "AbsenceRequestCtrlNbr",
                principalTable: "AbsenceRequests",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AbsenceApprovals_AbsenceRequests_AbsenceRequestCtrlNbr",
                table: "AbsenceApprovals");

            migrationBuilder.DropForeignKey(
                name: "FK_AbsenceMarkUps_AbsenceRequests_AbsenceRequestCtrlNbr",
                table: "AbsenceMarkUps");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VacancyImpacts",
                table: "VacancyImpacts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TimeEntries",
                table: "TimeEntries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SeniorityMoves",
                table: "SeniorityMoves");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SeniorityMovePolicies",
                table: "SeniorityMovePolicies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ReliefCoverageRules",
                table: "ReliefCoverageRules");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PositionVacancies",
                table: "PositionVacancies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PayrollRuns",
                table: "PayrollRuns");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PayrollRecords",
                table: "PayrollRecords");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExtraBoards",
                table: "ExtraBoards");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EmployeeBookings",
                table: "EmployeeBookings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DisplacementClaims",
                table: "DisplacementClaims");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DisplacementCases",
                table: "DisplacementCases");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DispatchProjections",
                table: "DispatchProjections");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DispatchOverrides",
                table: "DispatchOverrides");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DispatchDecisionLogs",
                table: "DispatchDecisionLogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Crews",
                table: "Crews");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CrewPositions",
                table: "CrewPositions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CrewIncumbencies",
                table: "CrewIncumbencies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CrewAttachmentTemplates",
                table: "CrewAttachmentTemplates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CrewAttachmentInstances",
                table: "CrewAttachmentInstances");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CraftDisplacementPolicies",
                table: "CraftDisplacementPolicies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Bulletins",
                table: "Bulletins");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BulletinPolicies",
                table: "BulletinPolicies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BulletinBids",
                table: "BulletinBids");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BoardMembers",
                table: "BoardMembers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BoardCascadePolicies",
                table: "BoardCascadePolicies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AbsenceRequests",
                table: "AbsenceRequests");

            migrationBuilder.RenameTable(
                name: "VacancyImpacts",
                newName: "VacancyImpact");

            migrationBuilder.RenameTable(
                name: "TimeEntries",
                newName: "TimeEntry");

            migrationBuilder.RenameTable(
                name: "SeniorityMoves",
                newName: "SeniorityMove");

            migrationBuilder.RenameTable(
                name: "SeniorityMovePolicies",
                newName: "SeniorityMovePolicy");

            migrationBuilder.RenameTable(
                name: "ReliefCoverageRules",
                newName: "ReliefCoverageRule");

            migrationBuilder.RenameTable(
                name: "PositionVacancies",
                newName: "PositionVacancy");

            migrationBuilder.RenameTable(
                name: "PayrollRuns",
                newName: "PayrollRun");

            migrationBuilder.RenameTable(
                name: "PayrollRecords",
                newName: "PayrollRecord");

            migrationBuilder.RenameTable(
                name: "ExtraBoards",
                newName: "ExtraBoard");

            migrationBuilder.RenameTable(
                name: "EmployeeBookings",
                newName: "EmployeeBooking");

            migrationBuilder.RenameTable(
                name: "DisplacementClaims",
                newName: "DisplacementClaim");

            migrationBuilder.RenameTable(
                name: "DisplacementCases",
                newName: "DisplacementCase");

            migrationBuilder.RenameTable(
                name: "DispatchProjections",
                newName: "DispatchProjection");

            migrationBuilder.RenameTable(
                name: "DispatchOverrides",
                newName: "DispatchOverride");

            migrationBuilder.RenameTable(
                name: "DispatchDecisionLogs",
                newName: "DispatchDecisionLog");

            migrationBuilder.RenameTable(
                name: "Crews",
                newName: "Crew");

            migrationBuilder.RenameTable(
                name: "CrewPositions",
                newName: "CrewPosition");

            migrationBuilder.RenameTable(
                name: "CrewIncumbencies",
                newName: "CrewIncumbency");

            migrationBuilder.RenameTable(
                name: "CrewAttachmentTemplates",
                newName: "CrewAttachmentTemplate");

            migrationBuilder.RenameTable(
                name: "CrewAttachmentInstances",
                newName: "CrewAttachmentInstance");

            migrationBuilder.RenameTable(
                name: "CraftDisplacementPolicies",
                newName: "CraftDisplacementPolicy");

            migrationBuilder.RenameTable(
                name: "Bulletins",
                newName: "Bulletin");

            migrationBuilder.RenameTable(
                name: "BulletinPolicies",
                newName: "BulletinPolicy");

            migrationBuilder.RenameTable(
                name: "BulletinBids",
                newName: "BulletinBid");

            migrationBuilder.RenameTable(
                name: "BoardMembers",
                newName: "BoardMember");

            migrationBuilder.RenameTable(
                name: "BoardCascadePolicies",
                newName: "BoardCascadePolicy");

            migrationBuilder.RenameTable(
                name: "AbsenceRequests",
                newName: "AbsenceRequest");

            migrationBuilder.RenameIndex(
                name: "IX_PayrollRuns_PayPeriod",
                table: "PayrollRun",
                newName: "IX_PayrollRun_PayPeriod");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VacancyImpact",
                table: "VacancyImpact",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TimeEntry",
                table: "TimeEntry",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SeniorityMove",
                table: "SeniorityMove",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SeniorityMovePolicy",
                table: "SeniorityMovePolicy",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReliefCoverageRule",
                table: "ReliefCoverageRule",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PositionVacancy",
                table: "PositionVacancy",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PayrollRun",
                table: "PayrollRun",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PayrollRecord",
                table: "PayrollRecord",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExtraBoard",
                table: "ExtraBoard",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EmployeeBooking",
                table: "EmployeeBooking",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DisplacementClaim",
                table: "DisplacementClaim",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DisplacementCase",
                table: "DisplacementCase",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DispatchProjection",
                table: "DispatchProjection",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DispatchOverride",
                table: "DispatchOverride",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DispatchDecisionLog",
                table: "DispatchDecisionLog",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Crew",
                table: "Crew",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CrewPosition",
                table: "CrewPosition",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CrewIncumbency",
                table: "CrewIncumbency",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CrewAttachmentTemplate",
                table: "CrewAttachmentTemplate",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CrewAttachmentInstance",
                table: "CrewAttachmentInstance",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CraftDisplacementPolicy",
                table: "CraftDisplacementPolicy",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Bulletin",
                table: "Bulletin",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BulletinPolicy",
                table: "BulletinPolicy",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BulletinBid",
                table: "BulletinBid",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BoardMember",
                table: "BoardMember",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BoardCascadePolicy",
                table: "BoardCascadePolicy",
                column: "CtrlNbr");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AbsenceRequest",
                table: "AbsenceRequest",
                column: "CtrlNbr");

            migrationBuilder.AddForeignKey(
                name: "FK_AbsenceApprovals_AbsenceRequest_AbsenceRequestCtrlNbr",
                table: "AbsenceApprovals",
                column: "AbsenceRequestCtrlNbr",
                principalTable: "AbsenceRequest",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AbsenceMarkUps_AbsenceRequest_AbsenceRequestCtrlNbr",
                table: "AbsenceMarkUps",
                column: "AbsenceRequestCtrlNbr",
                principalTable: "AbsenceRequest",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
