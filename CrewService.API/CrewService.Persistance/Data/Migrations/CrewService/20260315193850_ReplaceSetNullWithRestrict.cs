using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class ReplaceSetNullWithRestrict : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AbsenceRequests_AbsenceCodes_AbsenceCodeCtrlNbr",
                table: "AbsenceRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_AbsenceRequests_Employees_ApprovedByCtrlNbr",
                table: "AbsenceRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_AbsenceRequests_PositionSlots_PositionSlotCtrlNbr",
                table: "AbsenceRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Bulletins_Employees_AwardedEmployeeCtrlNbr",
                table: "Bulletins");

            migrationBuilder.DropForeignKey(
                name: "FK_CertificationRevocationRecords_Employees_PresidingOfficerCtrlNbr",
                table: "CertificationRevocationRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_Crafts_RegulatoryStandards_RegulatoryStandardCtrlNbr",
                table: "Crafts");

            migrationBuilder.DropForeignKey(
                name: "FK_DispatchDecisionLogs_Employees_SelectedEmployeeCtrlNbr",
                table: "DispatchDecisionLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_DispatchOverrides_Employees_ApprovedByCtrlNbr",
                table: "DispatchOverrides");

            migrationBuilder.DropForeignKey(
                name: "FK_DispatchProjections_Employees_ProjectedEmployeeCtrlNbr",
                table: "DispatchProjections");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeBookings_PositionSlots_PositionSlotCtrlNbr",
                table: "EmployeeBookings");

            migrationBuilder.DropForeignKey(
                name: "FK_HolidayPayrollRecords_PayrollRecords_PayrollRecordCtrlNbr",
                table: "HolidayPayrollRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_HolidayQualificationRules_Crafts_CraftCtrlNbr",
                table: "HolidayQualificationRules");

            migrationBuilder.DropForeignKey(
                name: "FK_OnDutyRecords_EmployeeBookings_BookingCtrlNbr",
                table: "OnDutyRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_PayRates_PositionRoles_PositionRoleCtrlNbr",
                table: "PayRates");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollImportRecords_PayrollRecords_PayrollRecordCtrlNbr",
                table: "PayrollImportRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollRecords_OnDutyRecords_OnDutyRecordCtrlNbr",
                table: "PayrollRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionSlotInstances_Employees_IncumbentEmployeeCtrlNbr",
                table: "PositionSlotInstances");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionSlots_Employees_BoundEmployeeCtrlNbr",
                table: "PositionSlots");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionVacancies_Employees_PreviousIncumbentCtrlNbr",
                table: "PositionVacancies");

            migrationBuilder.DropForeignKey(
                name: "FK_SeniorityMoves_Employees_DisplacedEmployeeCtrlNbr",
                table: "SeniorityMoves");

            migrationBuilder.DropForeignKey(
                name: "FK_SlotRequirements_PositionRoles_PositionRoleCtrlNbr",
                table: "SlotRequirements");

            migrationBuilder.DropForeignKey(
                name: "FK_SlotRequirements_RegulatoryQualifications_QualificationTypeCtrlNbr",
                table: "SlotRequirements");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamsWebhookConfigs_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "TeamsWebhookConfigs");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkInstances_AssignmentTemplates_AssignmentTemplateCtrlNbr",
                table: "WorkInstances");

            migrationBuilder.AddForeignKey(
                name: "FK_AbsenceRequests_AbsenceCodes_AbsenceCodeCtrlNbr",
                table: "AbsenceRequests",
                column: "AbsenceCodeCtrlNbr",
                principalTable: "AbsenceCodes",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AbsenceRequests_Employees_ApprovedByCtrlNbr",
                table: "AbsenceRequests",
                column: "ApprovedByCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AbsenceRequests_PositionSlots_PositionSlotCtrlNbr",
                table: "AbsenceRequests",
                column: "PositionSlotCtrlNbr",
                principalTable: "PositionSlots",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bulletins_Employees_AwardedEmployeeCtrlNbr",
                table: "Bulletins",
                column: "AwardedEmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CertificationRevocationRecords_Employees_PresidingOfficerCtrlNbr",
                table: "CertificationRevocationRecords",
                column: "PresidingOfficerCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Crafts_RegulatoryStandards_RegulatoryStandardCtrlNbr",
                table: "Crafts",
                column: "RegulatoryStandardCtrlNbr",
                principalTable: "RegulatoryStandards",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchDecisionLogs_Employees_SelectedEmployeeCtrlNbr",
                table: "DispatchDecisionLogs",
                column: "SelectedEmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchOverrides_Employees_ApprovedByCtrlNbr",
                table: "DispatchOverrides",
                column: "ApprovedByCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchProjections_Employees_ProjectedEmployeeCtrlNbr",
                table: "DispatchProjections",
                column: "ProjectedEmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeBookings_PositionSlots_PositionSlotCtrlNbr",
                table: "EmployeeBookings",
                column: "PositionSlotCtrlNbr",
                principalTable: "PositionSlots",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HolidayPayrollRecords_PayrollRecords_PayrollRecordCtrlNbr",
                table: "HolidayPayrollRecords",
                column: "PayrollRecordCtrlNbr",
                principalTable: "PayrollRecords",
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
                name: "FK_OnDutyRecords_EmployeeBookings_BookingCtrlNbr",
                table: "OnDutyRecords",
                column: "BookingCtrlNbr",
                principalTable: "EmployeeBookings",
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
                name: "FK_PayrollImportRecords_PayrollRecords_PayrollRecordCtrlNbr",
                table: "PayrollImportRecords",
                column: "PayrollRecordCtrlNbr",
                principalTable: "PayrollRecords",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollRecords_OnDutyRecords_OnDutyRecordCtrlNbr",
                table: "PayrollRecords",
                column: "OnDutyRecordCtrlNbr",
                principalTable: "OnDutyRecords",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PositionSlotInstances_Employees_IncumbentEmployeeCtrlNbr",
                table: "PositionSlotInstances",
                column: "IncumbentEmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PositionSlots_Employees_BoundEmployeeCtrlNbr",
                table: "PositionSlots",
                column: "BoundEmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PositionVacancies_Employees_PreviousIncumbentCtrlNbr",
                table: "PositionVacancies",
                column: "PreviousIncumbentCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SeniorityMoves_Employees_DisplacedEmployeeCtrlNbr",
                table: "SeniorityMoves",
                column: "DisplacedEmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SlotRequirements_PositionRoles_PositionRoleCtrlNbr",
                table: "SlotRequirements",
                column: "PositionRoleCtrlNbr",
                principalTable: "PositionRoles",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SlotRequirements_RegulatoryQualifications_QualificationTypeCtrlNbr",
                table: "SlotRequirements",
                column: "QualificationTypeCtrlNbr",
                principalTable: "RegulatoryQualifications",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TeamsWebhookConfigs_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "TeamsWebhookConfigs",
                column: "WorkAreaGroupCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkInstances_AssignmentTemplates_AssignmentTemplateCtrlNbr",
                table: "WorkInstances",
                column: "AssignmentTemplateCtrlNbr",
                principalTable: "AssignmentTemplates",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AbsenceRequests_AbsenceCodes_AbsenceCodeCtrlNbr",
                table: "AbsenceRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_AbsenceRequests_Employees_ApprovedByCtrlNbr",
                table: "AbsenceRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_AbsenceRequests_PositionSlots_PositionSlotCtrlNbr",
                table: "AbsenceRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Bulletins_Employees_AwardedEmployeeCtrlNbr",
                table: "Bulletins");

            migrationBuilder.DropForeignKey(
                name: "FK_CertificationRevocationRecords_Employees_PresidingOfficerCtrlNbr",
                table: "CertificationRevocationRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_Crafts_RegulatoryStandards_RegulatoryStandardCtrlNbr",
                table: "Crafts");

            migrationBuilder.DropForeignKey(
                name: "FK_DispatchDecisionLogs_Employees_SelectedEmployeeCtrlNbr",
                table: "DispatchDecisionLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_DispatchOverrides_Employees_ApprovedByCtrlNbr",
                table: "DispatchOverrides");

            migrationBuilder.DropForeignKey(
                name: "FK_DispatchProjections_Employees_ProjectedEmployeeCtrlNbr",
                table: "DispatchProjections");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeBookings_PositionSlots_PositionSlotCtrlNbr",
                table: "EmployeeBookings");

            migrationBuilder.DropForeignKey(
                name: "FK_HolidayPayrollRecords_PayrollRecords_PayrollRecordCtrlNbr",
                table: "HolidayPayrollRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_HolidayQualificationRules_Crafts_CraftCtrlNbr",
                table: "HolidayQualificationRules");

            migrationBuilder.DropForeignKey(
                name: "FK_OnDutyRecords_EmployeeBookings_BookingCtrlNbr",
                table: "OnDutyRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_PayRates_PositionRoles_PositionRoleCtrlNbr",
                table: "PayRates");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollImportRecords_PayrollRecords_PayrollRecordCtrlNbr",
                table: "PayrollImportRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollRecords_OnDutyRecords_OnDutyRecordCtrlNbr",
                table: "PayrollRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionSlotInstances_Employees_IncumbentEmployeeCtrlNbr",
                table: "PositionSlotInstances");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionSlots_Employees_BoundEmployeeCtrlNbr",
                table: "PositionSlots");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionVacancies_Employees_PreviousIncumbentCtrlNbr",
                table: "PositionVacancies");

            migrationBuilder.DropForeignKey(
                name: "FK_SeniorityMoves_Employees_DisplacedEmployeeCtrlNbr",
                table: "SeniorityMoves");

            migrationBuilder.DropForeignKey(
                name: "FK_SlotRequirements_PositionRoles_PositionRoleCtrlNbr",
                table: "SlotRequirements");

            migrationBuilder.DropForeignKey(
                name: "FK_SlotRequirements_RegulatoryQualifications_QualificationTypeCtrlNbr",
                table: "SlotRequirements");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamsWebhookConfigs_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "TeamsWebhookConfigs");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkInstances_AssignmentTemplates_AssignmentTemplateCtrlNbr",
                table: "WorkInstances");

            migrationBuilder.AddForeignKey(
                name: "FK_AbsenceRequests_AbsenceCodes_AbsenceCodeCtrlNbr",
                table: "AbsenceRequests",
                column: "AbsenceCodeCtrlNbr",
                principalTable: "AbsenceCodes",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AbsenceRequests_Employees_ApprovedByCtrlNbr",
                table: "AbsenceRequests",
                column: "ApprovedByCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AbsenceRequests_PositionSlots_PositionSlotCtrlNbr",
                table: "AbsenceRequests",
                column: "PositionSlotCtrlNbr",
                principalTable: "PositionSlots",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Bulletins_Employees_AwardedEmployeeCtrlNbr",
                table: "Bulletins",
                column: "AwardedEmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CertificationRevocationRecords_Employees_PresidingOfficerCtrlNbr",
                table: "CertificationRevocationRecords",
                column: "PresidingOfficerCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Crafts_RegulatoryStandards_RegulatoryStandardCtrlNbr",
                table: "Crafts",
                column: "RegulatoryStandardCtrlNbr",
                principalTable: "RegulatoryStandards",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchDecisionLogs_Employees_SelectedEmployeeCtrlNbr",
                table: "DispatchDecisionLogs",
                column: "SelectedEmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchOverrides_Employees_ApprovedByCtrlNbr",
                table: "DispatchOverrides",
                column: "ApprovedByCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchProjections_Employees_ProjectedEmployeeCtrlNbr",
                table: "DispatchProjections",
                column: "ProjectedEmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeBookings_PositionSlots_PositionSlotCtrlNbr",
                table: "EmployeeBookings",
                column: "PositionSlotCtrlNbr",
                principalTable: "PositionSlots",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_HolidayPayrollRecords_PayrollRecords_PayrollRecordCtrlNbr",
                table: "HolidayPayrollRecords",
                column: "PayrollRecordCtrlNbr",
                principalTable: "PayrollRecords",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_HolidayQualificationRules_Crafts_CraftCtrlNbr",
                table: "HolidayQualificationRules",
                column: "CraftCtrlNbr",
                principalTable: "Crafts",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_OnDutyRecords_EmployeeBookings_BookingCtrlNbr",
                table: "OnDutyRecords",
                column: "BookingCtrlNbr",
                principalTable: "EmployeeBookings",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PayRates_PositionRoles_PositionRoleCtrlNbr",
                table: "PayRates",
                column: "PositionRoleCtrlNbr",
                principalTable: "PositionRoles",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollImportRecords_PayrollRecords_PayrollRecordCtrlNbr",
                table: "PayrollImportRecords",
                column: "PayrollRecordCtrlNbr",
                principalTable: "PayrollRecords",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollRecords_OnDutyRecords_OnDutyRecordCtrlNbr",
                table: "PayrollRecords",
                column: "OnDutyRecordCtrlNbr",
                principalTable: "OnDutyRecords",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PositionSlotInstances_Employees_IncumbentEmployeeCtrlNbr",
                table: "PositionSlotInstances",
                column: "IncumbentEmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PositionSlots_Employees_BoundEmployeeCtrlNbr",
                table: "PositionSlots",
                column: "BoundEmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PositionVacancies_Employees_PreviousIncumbentCtrlNbr",
                table: "PositionVacancies",
                column: "PreviousIncumbentCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SeniorityMoves_Employees_DisplacedEmployeeCtrlNbr",
                table: "SeniorityMoves",
                column: "DisplacedEmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SlotRequirements_PositionRoles_PositionRoleCtrlNbr",
                table: "SlotRequirements",
                column: "PositionRoleCtrlNbr",
                principalTable: "PositionRoles",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SlotRequirements_RegulatoryQualifications_QualificationTypeCtrlNbr",
                table: "SlotRequirements",
                column: "QualificationTypeCtrlNbr",
                principalTable: "RegulatoryQualifications",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TeamsWebhookConfigs_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "TeamsWebhookConfigs",
                column: "WorkAreaGroupCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkInstances_AssignmentTemplates_AssignmentTemplateCtrlNbr",
                table: "WorkInstances",
                column: "AssignmentTemplateCtrlNbr",
                principalTable: "AssignmentTemplates",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
