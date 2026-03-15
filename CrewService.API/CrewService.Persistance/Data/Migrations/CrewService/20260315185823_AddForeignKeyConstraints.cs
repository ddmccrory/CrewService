using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddForeignKeyConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_WorkInstances_AssignmentTemplateCtrlNbr",
                table: "WorkInstances",
                column: "AssignmentTemplateCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_WorkInstances_WorkAreaGroupCtrlNbr",
                table: "WorkInstances",
                column: "WorkAreaGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerSchedules_WorkAreaGroupCtrlNbr",
                table: "WorkerSchedules",
                column: "WorkAreaGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerExecutionLogs_WorkerScheduleCtrlNbr",
                table: "WorkerExecutionLogs",
                column: "WorkerScheduleCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_VoluntaryReferrals_EmployeeCtrlNbr",
                table: "VoluntaryReferrals",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_VacancyResolutionRuns_ShiftInstanceCtrlNbr",
                table: "VacancyResolutionRuns",
                column: "ShiftInstanceCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_VacancyResolutionRuns_WorkAreaGroupCtrlNbr",
                table: "VacancyResolutionRuns",
                column: "WorkAreaGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_VacancyImpacts_AbsenceRequestCtrlNbr",
                table: "VacancyImpacts",
                column: "AbsenceRequestCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_VacancyImpacts_PositionSlotCtrlNbr",
                table: "VacancyImpacts",
                column: "PositionSlotCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_EmployeeCtrlNbr",
                table: "TimeEntries",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_TeamsWebhookConfigs_RailroadCtrlNbr",
                table: "TeamsWebhookConfigs",
                column: "RailroadCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_TeamsWebhookConfigs_WorkAreaGroupCtrlNbr",
                table: "TeamsWebhookConfigs",
                column: "WorkAreaGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_SlotRequirements_PositionSlotCtrlNbr",
                table: "SlotRequirements",
                column: "PositionSlotCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftInstances_WorkInstanceCtrlNbr",
                table: "ShiftInstances",
                column: "WorkInstanceCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftDefinitions_WorkAreaGroupCtrlNbr",
                table: "ShiftDefinitions",
                column: "WorkAreaGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_SeniorityMoves_CraftCtrlNbr",
                table: "SeniorityMoves",
                column: "CraftCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_SeniorityMoves_EmployeeCtrlNbr",
                table: "SeniorityMoves",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_SeniorityMovePolicies_CraftCtrlNbr",
                table: "SeniorityMovePolicies",
                column: "CraftCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_Seniority_EmployeeCtrlNbr",
                table: "Seniority",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_Seniority_RosterCtrlNbr",
                table: "Seniority",
                column: "RosterCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_SafetyObservations_ObserverEmployeeCtrlNbr",
                table: "SafetyObservations",
                column: "ObserverEmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_SafetyObservations_WorkAreaGroupCtrlNbr",
                table: "SafetyObservations",
                column: "WorkAreaGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_SafetyObservationResolutions_ResolvedByCtrlNbr",
                table: "SafetyObservationResolutions",
                column: "ResolvedByCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_SafetyObservationActions_TakenByCtrlNbr",
                table: "SafetyObservationActions",
                column: "TakenByCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_SafetyCategories_WorkAreaGroupCtrlNbr",
                table: "SafetyCategories",
                column: "WorkAreaGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_Rosters_CraftCtrlNbr",
                table: "Rosters",
                column: "CraftCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_RosterBoards_CraftCtrlNbr",
                table: "RosterBoards",
                column: "CraftCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_RosterBoards_WorkAreaGroupCtrlNbr",
                table: "RosterBoards",
                column: "WorkAreaGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_RosterBoardPositions_EmployeeCtrlNbr",
                table: "RosterBoardPositions",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_ReliefCoverageRules_AssignmentTemplateCtrlNbr",
                table: "ReliefCoverageRules",
                column: "AssignmentTemplateCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_ReliefCoverageRules_ReliefCrewCtrlNbr",
                table: "ReliefCoverageRules",
                column: "ReliefCrewCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_RailroadInformations_WorkAreaGroupCtrlNbr",
                table: "RailroadInformations",
                column: "WorkAreaGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_RailroadInformationReadReceipts_EmployeeCtrlNbr",
                table: "RailroadInformationReadReceipts",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_PositionVacancies_CraftCtrlNbr",
                table: "PositionVacancies",
                column: "CraftCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_PositionVacancies_PreviousIncumbentCtrlNbr",
                table: "PositionVacancies",
                column: "PreviousIncumbentCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_PositionSlots_BoundEmployeeCtrlNbr",
                table: "PositionSlots",
                column: "BoundEmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_PositionSlots_PositionRoleCtrlNbr",
                table: "PositionSlots",
                column: "PositionRoleCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_PositionSlots_WorkInstanceCtrlNbr",
                table: "PositionSlots",
                column: "WorkInstanceCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_PositionSlotInstances_CrewPositionCtrlNbr",
                table: "PositionSlotInstances",
                column: "CrewPositionCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_PositionSlotInstances_IncumbentEmployeeCtrlNbr",
                table: "PositionSlotInstances",
                column: "IncumbentEmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_PositionRoles_CraftCtrlNbr",
                table: "PositionRoles",
                column: "CraftCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollTiers_DynamicGroupCtrlNbr",
                table: "PayrollTiers",
                column: "DynamicGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRecords_EmployeeCtrlNbr",
                table: "PayrollRecords",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRecords_OnDutyRecordCtrlNbr",
                table: "PayrollRecords",
                column: "OnDutyRecordCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRecords_PayrollRunCtrlNbr",
                table: "PayrollRecords",
                column: "PayrollRunCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollImportRecords_EmployeeCtrlNbr",
                table: "PayrollImportRecords",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollExportBatches_PayrollRunCtrlNbr",
                table: "PayrollExportBatches",
                column: "PayrollRunCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_PayRates_CraftCtrlNbr",
                table: "PayRates",
                column: "CraftCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_PayRates_PositionRoleCtrlNbr",
                table: "PayRates",
                column: "PositionRoleCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_OnDutyRecords_EmployeeCtrlNbr",
                table: "OnDutyRecords",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_OnDutyRecords_PositionSlotCtrlNbr",
                table: "OnDutyRecords",
                column: "PositionSlotCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_OnDutyMaterialRecords_OnDutyRecordCtrlNbr",
                table: "OnDutyMaterialRecords",
                column: "OnDutyRecordCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_OnDutyLocomotiveRecords_OnDutyRecordCtrlNbr",
                table: "OnDutyLocomotiveRecords",
                column: "OnDutyRecordCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_OnDutyBillingRecords_OnDutyRecordCtrlNbr",
                table: "OnDutyBillingRecords",
                column: "OnDutyRecordCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_OffDutyRecords_EmployeeCtrlNbr",
                table: "OffDutyRecords",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_OffDutyRecords_OnDutyRecordCtrlNbr",
                table: "OffDutyRecords",
                column: "OnDutyRecordCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRequests_EmployeeCtrlNbr",
                table: "NotificationRequests",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRequests_PositionSlotCtrlNbr",
                table: "NotificationRequests",
                column: "PositionSlotCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationProviderConfigs_WorkAreaGroupCtrlNbr",
                table: "NotificationProviderConfigs",
                column: "WorkAreaGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_WorkAreaGroupCtrlNbr",
                table: "Holidays",
                column: "WorkAreaGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_HolidayQualificationRules_CraftCtrlNbr",
                table: "HolidayQualificationRules",
                column: "CraftCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_HolidayQualificationRules_HolidayCtrlNbr",
                table: "HolidayQualificationRules",
                column: "HolidayCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_HolidayPayrollRecords_EmployeeCtrlNbr",
                table: "HolidayPayrollRecords",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_HolidayPayrollRecords_HolidayCtrlNbr",
                table: "HolidayPayrollRecords",
                column: "HolidayCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_GroupAttributeValues_AttributeDefinitionCtrlNbr",
                table: "GroupAttributeValues",
                column: "AttributeDefinitionCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_GroupAttributeValues_GroupCtrlNbr",
                table: "GroupAttributeValues",
                column: "GroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_GroupAttributeDefinitions_GroupTypeCtrlNbr",
                table: "GroupAttributeDefinitions",
                column: "GroupTypeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_FraExcessServiceReports_DutyTourCtrlNbr",
                table: "FraExcessServiceReports",
                column: "DutyTourCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_FraExcessServiceReports_EmployeeCtrlNbr",
                table: "FraExcessServiceReports",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_FraDutyTourSegments_OnDutyRecordCtrlNbr",
                table: "FraDutyTourSegments",
                column: "OnDutyRecordCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_FraDutyTours_EmployeeCtrlNbr",
                table: "FraDutyTours",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_FraDutyTours_RegulatoryStandardCtrlNbr",
                table: "FraDutyTours",
                column: "RegulatoryStandardCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_ExtraBoards_CraftCtrlNbr",
                table: "ExtraBoards",
                column: "CraftCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_ExtraBoards_PlacedGroupCtrlNbr",
                table: "ExtraBoards",
                column: "PlacedGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentStatusHistory_EmployeeCtrlNbr",
                table: "EmploymentStatusHistory",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentStatusHistory_EmploymentStatusCtrlNbr",
                table: "EmploymentStatusHistory",
                column: "EmploymentStatusCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_EmploymentStatusCtrlNbr",
                table: "Employees",
                column: "EmploymentStatusCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePriorServiceCredits_EmployeeCtrlNbr",
                table: "EmployeePriorServiceCredits",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCertifications_EmployeeCtrlNbr",
                table: "EmployeeCertifications",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCertifications_RegulatoryQualificationCtrlNbr",
                table: "EmployeeCertifications",
                column: "RegulatoryQualificationCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeBookings_EmployeeCtrlNbr",
                table: "EmployeeBookings",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_EarningCodeRules_WorkAreaGroupCtrlNbr",
                table: "EarningCodeRules",
                column: "WorkAreaGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_EarningApprovals_OfficerCtrlNbr",
                table: "EarningApprovals",
                column: "OfficerCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_EarningApprovals_PayrollRecordCtrlNbr",
                table: "EarningApprovals",
                column: "PayrollRecordCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_DynamicGroups_GroupTypeCtrlNbr",
                table: "DynamicGroups",
                column: "GroupTypeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_DrugAlcoholTestRecords_EmployeeCtrlNbr",
                table: "DrugAlcoholTestRecords",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_DrugAlcoholActions_EmployeeCtrlNbr",
                table: "DrugAlcoholActions",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_DrugAlcoholActions_TestRecordCtrlNbr",
                table: "DrugAlcoholActions",
                column: "TestRecordCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_DisplacementClaims_CaseCtrlNbr",
                table: "DisplacementClaims",
                column: "CaseCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_DisplacementClaims_TargetEmployeeCtrlNbr",
                table: "DisplacementClaims",
                column: "TargetEmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_DisplacementCases_CraftCtrlNbr",
                table: "DisplacementCases",
                column: "CraftCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_DisplacementCases_EmployeeCtrlNbr",
                table: "DisplacementCases",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchProjections_PositionSlotCtrlNbr",
                table: "DispatchProjections",
                column: "PositionSlotCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchProjections_ProjectedEmployeeCtrlNbr",
                table: "DispatchProjections",
                column: "ProjectedEmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOverrides_EmployeeCtrlNbr",
                table: "DispatchOverrides",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOverrides_PositionSlotCtrlNbr",
                table: "DispatchOverrides",
                column: "PositionSlotCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchDecisionLogs_PositionSlotCtrlNbr",
                table: "DispatchDecisionLogs",
                column: "PositionSlotCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchDecisionLogs_SelectedEmployeeCtrlNbr",
                table: "DispatchDecisionLogs",
                column: "SelectedEmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_DailyEmployeeStatusRecords_WorkAreaGroupCtrlNbr",
                table: "DailyEmployeeStatusRecords",
                column: "WorkAreaGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_CrewPositions_CrewCtrlNbr",
                table: "CrewPositions",
                column: "CrewCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_CrewPositions_PositionRoleCtrlNbr",
                table: "CrewPositions",
                column: "PositionRoleCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_CrewOffDays_CrewPositionCtrlNbr",
                table: "CrewOffDays",
                column: "CrewPositionCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_CrewIncumbencies_CrewPositionCtrlNbr",
                table: "CrewIncumbencies",
                column: "CrewPositionCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_CrewIncumbencies_EmployeeCtrlNbr",
                table: "CrewIncumbencies",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_CrewAttachmentTemplates_AssignmentTemplateCtrlNbr",
                table: "CrewAttachmentTemplates",
                column: "AssignmentTemplateCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_CrewAttachmentTemplates_CrewCtrlNbr",
                table: "CrewAttachmentTemplates",
                column: "CrewCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_CrewAttachmentInstances_CrewCtrlNbr",
                table: "CrewAttachmentInstances",
                column: "CrewCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_CrewAttachmentInstances_WorkInstanceCtrlNbr",
                table: "CrewAttachmentInstances",
                column: "WorkInstanceCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_Crafts_DynamicGroupCtrlNbr",
                table: "Crafts",
                column: "DynamicGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_Crafts_RegulatoryStandardCtrlNbr",
                table: "Crafts",
                column: "RegulatoryStandardCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_CraftRegulatoryQualifications_RegulatoryQualificationCtrlNbr",
                table: "CraftRegulatoryQualifications",
                column: "RegulatoryQualificationCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_CraftDisplacementPolicies_CraftCtrlNbr",
                table: "CraftDisplacementPolicies",
                column: "CraftCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeNotifications_WorkAreaGroupCtrlNbr",
                table: "ChangeNotifications",
                column: "WorkAreaGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_CertificationRevocationRecords_EmployeeCertificationCtrlNbr",
                table: "CertificationRevocationRecords",
                column: "EmployeeCertificationCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_Bulletins_AwardedEmployeeCtrlNbr",
                table: "Bulletins",
                column: "AwardedEmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_Bulletins_CraftCtrlNbr",
                table: "Bulletins",
                column: "CraftCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_Bulletins_PositionVacancyCtrlNbr",
                table: "Bulletins",
                column: "PositionVacancyCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_BulletinPolicies_CraftCtrlNbr",
                table: "BulletinPolicies",
                column: "CraftCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_BulletinBids_BulletinCtrlNbr",
                table: "BulletinBids",
                column: "BulletinCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_BulletinBids_EmployeeCtrlNbr",
                table: "BulletinBids",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_BoardMembers_EmployeeCtrlNbr",
                table: "BoardMembers",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_BoardMembers_ExtraBoardCtrlNbr",
                table: "BoardMembers",
                column: "ExtraBoardCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_BoardCascadePolicies_CraftCtrlNbr",
                table: "BoardCascadePolicies",
                column: "CraftCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_BoardCascadePolicies_WorkAreaGroupCtrlNbr",
                table: "BoardCascadePolicies",
                column: "WorkAreaGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentTemplates_WorkAreaGroupCtrlNbr",
                table: "AssignmentTemplates",
                column: "WorkAreaGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceRequests_AbsenceCodeCtrlNbr",
                table: "AbsenceRequests",
                column: "AbsenceCodeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceRequests_EmployeeCtrlNbr",
                table: "AbsenceRequests",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceCodeCraftOverrides_CraftCtrlNbr",
                table: "AbsenceCodeCraftOverrides",
                column: "CraftCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceApprovals_ApprovalOfficerCtrlNbr",
                table: "AbsenceApprovals",
                column: "ApprovalOfficerCtrlNbr");

            migrationBuilder.AddForeignKey(
                name: "FK_AbsenceApprovals_Employees_ApprovalOfficerCtrlNbr",
                table: "AbsenceApprovals",
                column: "ApprovalOfficerCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AbsenceCodeCraftOverrides_AbsenceCodes_AbsenceCodeCtrlNbr",
                table: "AbsenceCodeCraftOverrides",
                column: "AbsenceCodeCtrlNbr",
                principalTable: "AbsenceCodes",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AbsenceCodeCraftOverrides_Crafts_CraftCtrlNbr",
                table: "AbsenceCodeCraftOverrides",
                column: "CraftCtrlNbr",
                principalTable: "Crafts",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AbsenceRequests_AbsenceCodes_AbsenceCodeCtrlNbr",
                table: "AbsenceRequests",
                column: "AbsenceCodeCtrlNbr",
                principalTable: "AbsenceCodes",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AbsenceRequests_Employees_EmployeeCtrlNbr",
                table: "AbsenceRequests",
                column: "EmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AssignmentTemplates_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "AssignmentTemplates",
                column: "WorkAreaGroupCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BoardCascadePolicies_Crafts_CraftCtrlNbr",
                table: "BoardCascadePolicies",
                column: "CraftCtrlNbr",
                principalTable: "Crafts",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BoardCascadePolicies_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "BoardCascadePolicies",
                column: "WorkAreaGroupCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BoardMembers_Employees_EmployeeCtrlNbr",
                table: "BoardMembers",
                column: "EmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BoardMembers_ExtraBoards_ExtraBoardCtrlNbr",
                table: "BoardMembers",
                column: "ExtraBoardCtrlNbr",
                principalTable: "ExtraBoards",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BulletinBids_Bulletins_BulletinCtrlNbr",
                table: "BulletinBids",
                column: "BulletinCtrlNbr",
                principalTable: "Bulletins",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BulletinBids_Employees_EmployeeCtrlNbr",
                table: "BulletinBids",
                column: "EmployeeCtrlNbr",
                principalTable: "Employees",
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
                name: "FK_Bulletins_Crafts_CraftCtrlNbr",
                table: "Bulletins",
                column: "CraftCtrlNbr",
                principalTable: "Crafts",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bulletins_Employees_AwardedEmployeeCtrlNbr",
                table: "Bulletins",
                column: "AwardedEmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Bulletins_PositionVacancies_PositionVacancyCtrlNbr",
                table: "Bulletins",
                column: "PositionVacancyCtrlNbr",
                principalTable: "PositionVacancies",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CertificationRevocationRecords_EmployeeCertifications_EmployeeCertificationCtrlNbr",
                table: "CertificationRevocationRecords",
                column: "EmployeeCertificationCtrlNbr",
                principalTable: "EmployeeCertifications",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChangeNotifications_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "ChangeNotifications",
                column: "WorkAreaGroupCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CompensationBalances_Employees_EmployeeCtrlNbr",
                table: "CompensationBalances",
                column: "EmployeeCtrlNbr",
                principalTable: "Employees",
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
                name: "FK_CraftRegulatoryQualifications_RegulatoryQualifications_RegulatoryQualificationCtrlNbr",
                table: "CraftRegulatoryQualifications",
                column: "RegulatoryQualificationCtrlNbr",
                principalTable: "RegulatoryQualifications",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Crafts_DynamicGroups_DynamicGroupCtrlNbr",
                table: "Crafts",
                column: "DynamicGroupCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Crafts_RegulatoryStandards_RegulatoryStandardCtrlNbr",
                table: "Crafts",
                column: "RegulatoryStandardCtrlNbr",
                principalTable: "RegulatoryStandards",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CrewAttachmentInstances_Crews_CrewCtrlNbr",
                table: "CrewAttachmentInstances",
                column: "CrewCtrlNbr",
                principalTable: "Crews",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CrewAttachmentInstances_WorkInstances_WorkInstanceCtrlNbr",
                table: "CrewAttachmentInstances",
                column: "WorkInstanceCtrlNbr",
                principalTable: "WorkInstances",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CrewAttachmentTemplates_AssignmentTemplates_AssignmentTemplateCtrlNbr",
                table: "CrewAttachmentTemplates",
                column: "AssignmentTemplateCtrlNbr",
                principalTable: "AssignmentTemplates",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CrewAttachmentTemplates_Crews_CrewCtrlNbr",
                table: "CrewAttachmentTemplates",
                column: "CrewCtrlNbr",
                principalTable: "Crews",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CrewIncumbencies_CrewPositions_CrewPositionCtrlNbr",
                table: "CrewIncumbencies",
                column: "CrewPositionCtrlNbr",
                principalTable: "CrewPositions",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CrewIncumbencies_Employees_EmployeeCtrlNbr",
                table: "CrewIncumbencies",
                column: "EmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CrewOffDays_CrewPositions_CrewPositionCtrlNbr",
                table: "CrewOffDays",
                column: "CrewPositionCtrlNbr",
                principalTable: "CrewPositions",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CrewPositions_Crews_CrewCtrlNbr",
                table: "CrewPositions",
                column: "CrewCtrlNbr",
                principalTable: "Crews",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CrewPositions_PositionRoles_PositionRoleCtrlNbr",
                table: "CrewPositions",
                column: "PositionRoleCtrlNbr",
                principalTable: "PositionRoles",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DailyEmployeeStatusRecords_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "DailyEmployeeStatusRecords",
                column: "WorkAreaGroupCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DailyEmployeeStatusRecords_Employees_EmployeeCtrlNbr",
                table: "DailyEmployeeStatusRecords",
                column: "EmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchDecisionLogs_Employees_SelectedEmployeeCtrlNbr",
                table: "DispatchDecisionLogs",
                column: "SelectedEmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchDecisionLogs_PositionSlots_PositionSlotCtrlNbr",
                table: "DispatchDecisionLogs",
                column: "PositionSlotCtrlNbr",
                principalTable: "PositionSlots",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchOverrides_Employees_EmployeeCtrlNbr",
                table: "DispatchOverrides",
                column: "EmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchOverrides_PositionSlots_PositionSlotCtrlNbr",
                table: "DispatchOverrides",
                column: "PositionSlotCtrlNbr",
                principalTable: "PositionSlots",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchProjections_Employees_ProjectedEmployeeCtrlNbr",
                table: "DispatchProjections",
                column: "ProjectedEmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchProjections_PositionSlots_PositionSlotCtrlNbr",
                table: "DispatchProjections",
                column: "PositionSlotCtrlNbr",
                principalTable: "PositionSlots",
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
                name: "FK_DisplacementCases_Employees_EmployeeCtrlNbr",
                table: "DisplacementCases",
                column: "EmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DisplacementClaims_DisplacementCases_CaseCtrlNbr",
                table: "DisplacementClaims",
                column: "CaseCtrlNbr",
                principalTable: "DisplacementCases",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DisplacementClaims_Employees_TargetEmployeeCtrlNbr",
                table: "DisplacementClaims",
                column: "TargetEmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DrugAlcoholActions_DrugAlcoholTestRecords_TestRecordCtrlNbr",
                table: "DrugAlcoholActions",
                column: "TestRecordCtrlNbr",
                principalTable: "DrugAlcoholTestRecords",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DrugAlcoholActions_Employees_EmployeeCtrlNbr",
                table: "DrugAlcoholActions",
                column: "EmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DrugAlcoholTestRecords_Employees_EmployeeCtrlNbr",
                table: "DrugAlcoholTestRecords",
                column: "EmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DynamicGroups_DynamicGroups_ParentGroupCtrlNbr",
                table: "DynamicGroups",
                column: "ParentGroupCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_DynamicGroups_GroupTypes_GroupTypeCtrlNbr",
                table: "DynamicGroups",
                column: "GroupTypeCtrlNbr",
                principalTable: "GroupTypes",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EarningApprovals_Employees_OfficerCtrlNbr",
                table: "EarningApprovals",
                column: "OfficerCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EarningApprovals_PayrollRecords_PayrollRecordCtrlNbr",
                table: "EarningApprovals",
                column: "PayrollRecordCtrlNbr",
                principalTable: "PayrollRecords",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EarningCodeRules_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "EarningCodeRules",
                column: "WorkAreaGroupCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeBookings_Employees_EmployeeCtrlNbr",
                table: "EmployeeBookings",
                column: "EmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeCertifications_Employees_EmployeeCtrlNbr",
                table: "EmployeeCertifications",
                column: "EmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeCertifications_RegulatoryQualifications_RegulatoryQualificationCtrlNbr",
                table: "EmployeeCertifications",
                column: "RegulatoryQualificationCtrlNbr",
                principalTable: "RegulatoryQualifications",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeePriorServiceCredits_Employees_EmployeeCtrlNbr",
                table: "EmployeePriorServiceCredits",
                column: "EmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_EmploymentStatuses_EmploymentStatusCtrlNbr",
                table: "Employees",
                column: "EmploymentStatusCtrlNbr",
                principalTable: "EmploymentStatuses",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmploymentStatusHistory_Employees_EmployeeCtrlNbr",
                table: "EmploymentStatusHistory",
                column: "EmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmploymentStatusHistory_EmploymentStatuses_EmploymentStatusCtrlNbr",
                table: "EmploymentStatusHistory",
                column: "EmploymentStatusCtrlNbr",
                principalTable: "EmploymentStatuses",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExtraBoards_Crafts_CraftCtrlNbr",
                table: "ExtraBoards",
                column: "CraftCtrlNbr",
                principalTable: "Crafts",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExtraBoards_DynamicGroups_PlacedGroupCtrlNbr",
                table: "ExtraBoards",
                column: "PlacedGroupCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FraDutyTours_Employees_EmployeeCtrlNbr",
                table: "FraDutyTours",
                column: "EmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FraDutyTours_RegulatoryStandards_RegulatoryStandardCtrlNbr",
                table: "FraDutyTours",
                column: "RegulatoryStandardCtrlNbr",
                principalTable: "RegulatoryStandards",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FraDutyTourSegments_OnDutyRecords_OnDutyRecordCtrlNbr",
                table: "FraDutyTourSegments",
                column: "OnDutyRecordCtrlNbr",
                principalTable: "OnDutyRecords",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FraExcessServiceReports_Employees_EmployeeCtrlNbr",
                table: "FraExcessServiceReports",
                column: "EmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FraExcessServiceReports_FraDutyTours_DutyTourCtrlNbr",
                table: "FraExcessServiceReports",
                column: "DutyTourCtrlNbr",
                principalTable: "FraDutyTours",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FraMonthlyAccumulators_Employees_EmployeeCtrlNbr",
                table: "FraMonthlyAccumulators",
                column: "EmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupAttributeDefinitions_GroupTypes_GroupTypeCtrlNbr",
                table: "GroupAttributeDefinitions",
                column: "GroupTypeCtrlNbr",
                principalTable: "GroupTypes",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupAttributeValues_DynamicGroups_GroupCtrlNbr",
                table: "GroupAttributeValues",
                column: "GroupCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupAttributeValues_GroupAttributeDefinitions_AttributeDefinitionCtrlNbr",
                table: "GroupAttributeValues",
                column: "AttributeDefinitionCtrlNbr",
                principalTable: "GroupAttributeDefinitions",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HolidayPayrollRecords_Employees_EmployeeCtrlNbr",
                table: "HolidayPayrollRecords",
                column: "EmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HolidayPayrollRecords_Holidays_HolidayCtrlNbr",
                table: "HolidayPayrollRecords",
                column: "HolidayCtrlNbr",
                principalTable: "Holidays",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HolidayQualificationRules_Crafts_CraftCtrlNbr",
                table: "HolidayQualificationRules",
                column: "CraftCtrlNbr",
                principalTable: "Crafts",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_HolidayQualificationRules_Holidays_HolidayCtrlNbr",
                table: "HolidayQualificationRules",
                column: "HolidayCtrlNbr",
                principalTable: "Holidays",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Holidays_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "Holidays",
                column: "WorkAreaGroupCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_Parents_ParentCtrlNbr",
                table: "Invitations",
                column: "ParentCtrlNbr",
                principalTable: "Parents",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationProviderConfigs_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "NotificationProviderConfigs",
                column: "WorkAreaGroupCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationRequests_Employees_EmployeeCtrlNbr",
                table: "NotificationRequests",
                column: "EmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationRequests_PositionSlots_PositionSlotCtrlNbr",
                table: "NotificationRequests",
                column: "PositionSlotCtrlNbr",
                principalTable: "PositionSlots",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OffDutyRecords_Employees_EmployeeCtrlNbr",
                table: "OffDutyRecords",
                column: "EmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OffDutyRecords_OnDutyRecords_OnDutyRecordCtrlNbr",
                table: "OffDutyRecords",
                column: "OnDutyRecordCtrlNbr",
                principalTable: "OnDutyRecords",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OnDutyBillingRecords_OnDutyRecords_OnDutyRecordCtrlNbr",
                table: "OnDutyBillingRecords",
                column: "OnDutyRecordCtrlNbr",
                principalTable: "OnDutyRecords",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OnDutyLocomotiveRecords_OnDutyRecords_OnDutyRecordCtrlNbr",
                table: "OnDutyLocomotiveRecords",
                column: "OnDutyRecordCtrlNbr",
                principalTable: "OnDutyRecords",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OnDutyMaterialRecords_OnDutyRecords_OnDutyRecordCtrlNbr",
                table: "OnDutyMaterialRecords",
                column: "OnDutyRecordCtrlNbr",
                principalTable: "OnDutyRecords",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OnDutyRecords_Employees_EmployeeCtrlNbr",
                table: "OnDutyRecords",
                column: "EmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OnDutyRecords_PositionSlots_PositionSlotCtrlNbr",
                table: "OnDutyRecords",
                column: "PositionSlotCtrlNbr",
                principalTable: "PositionSlots",
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
                name: "FK_PayRates_PositionRoles_PositionRoleCtrlNbr",
                table: "PayRates",
                column: "PositionRoleCtrlNbr",
                principalTable: "PositionRoles",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollExportBatches_PayrollRuns_PayrollRunCtrlNbr",
                table: "PayrollExportBatches",
                column: "PayrollRunCtrlNbr",
                principalTable: "PayrollRuns",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollImportRecords_Employees_EmployeeCtrlNbr",
                table: "PayrollImportRecords",
                column: "EmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollRecords_Employees_EmployeeCtrlNbr",
                table: "PayrollRecords",
                column: "EmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollRecords_OnDutyRecords_OnDutyRecordCtrlNbr",
                table: "PayrollRecords",
                column: "OnDutyRecordCtrlNbr",
                principalTable: "OnDutyRecords",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollRecords_PayrollRuns_PayrollRunCtrlNbr",
                table: "PayrollRecords",
                column: "PayrollRunCtrlNbr",
                principalTable: "PayrollRuns",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollTiers_DynamicGroups_DynamicGroupCtrlNbr",
                table: "PayrollTiers",
                column: "DynamicGroupCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PositionRoles_Crafts_CraftCtrlNbr",
                table: "PositionRoles",
                column: "CraftCtrlNbr",
                principalTable: "Crafts",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PositionSlotInstances_CrewPositions_CrewPositionCtrlNbr",
                table: "PositionSlotInstances",
                column: "CrewPositionCtrlNbr",
                principalTable: "CrewPositions",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

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
                name: "FK_PositionSlots_PositionRoles_PositionRoleCtrlNbr",
                table: "PositionSlots",
                column: "PositionRoleCtrlNbr",
                principalTable: "PositionRoles",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PositionSlots_WorkInstances_WorkInstanceCtrlNbr",
                table: "PositionSlots",
                column: "WorkInstanceCtrlNbr",
                principalTable: "WorkInstances",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PositionVacancies_Crafts_CraftCtrlNbr",
                table: "PositionVacancies",
                column: "CraftCtrlNbr",
                principalTable: "Crafts",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PositionVacancies_Employees_PreviousIncumbentCtrlNbr",
                table: "PositionVacancies",
                column: "PreviousIncumbentCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_RailroadGroupPlacements_DynamicGroups_GroupCtrlNbr",
                table: "RailroadGroupPlacements",
                column: "GroupCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RailroadGroupPlacements_Railroads_RailroadCtrlNbr",
                table: "RailroadGroupPlacements",
                column: "RailroadCtrlNbr",
                principalTable: "Railroads",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RailroadHolidaySelections_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "RailroadHolidaySelections",
                column: "WorkAreaGroupCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RailroadInformationReadReceipts_Employees_EmployeeCtrlNbr",
                table: "RailroadInformationReadReceipts",
                column: "EmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RailroadInformationReadReceipts_RailroadInformations_InformationCtrlNbr",
                table: "RailroadInformationReadReceipts",
                column: "InformationCtrlNbr",
                principalTable: "RailroadInformations",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RailroadInformations_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "RailroadInformations",
                column: "WorkAreaGroupCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReliefCoverageRules_AssignmentTemplates_AssignmentTemplateCtrlNbr",
                table: "ReliefCoverageRules",
                column: "AssignmentTemplateCtrlNbr",
                principalTable: "AssignmentTemplates",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReliefCoverageRules_Crews_ReliefCrewCtrlNbr",
                table: "ReliefCoverageRules",
                column: "ReliefCrewCtrlNbr",
                principalTable: "Crews",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RosterBoardPositions_Employees_EmployeeCtrlNbr",
                table: "RosterBoardPositions",
                column: "EmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RosterBoards_Crafts_CraftCtrlNbr",
                table: "RosterBoards",
                column: "CraftCtrlNbr",
                principalTable: "Crafts",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RosterBoards_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "RosterBoards",
                column: "WorkAreaGroupCtrlNbr",
                principalTable: "DynamicGroups",
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
                name: "FK_SafetyCategories_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "SafetyCategories",
                column: "WorkAreaGroupCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SafetyObservationActions_Employees_TakenByCtrlNbr",
                table: "SafetyObservationActions",
                column: "TakenByCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SafetyObservationResolutions_Employees_ResolvedByCtrlNbr",
                table: "SafetyObservationResolutions",
                column: "ResolvedByCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SafetyObservationResolutions_SafetyObservations_ObservationCtrlNbr",
                table: "SafetyObservationResolutions",
                column: "ObservationCtrlNbr",
                principalTable: "SafetyObservations",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SafetyObservations_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "SafetyObservations",
                column: "WorkAreaGroupCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SafetyObservations_Employees_ObserverEmployeeCtrlNbr",
                table: "SafetyObservations",
                column: "ObserverEmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Seniority_Employees_EmployeeCtrlNbr",
                table: "Seniority",
                column: "EmployeeCtrlNbr",
                principalTable: "Employees",
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

            migrationBuilder.AddForeignKey(
                name: "FK_SeniorityMoves_Employees_EmployeeCtrlNbr",
                table: "SeniorityMoves",
                column: "EmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ShiftDefinitions_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "ShiftDefinitions",
                column: "WorkAreaGroupCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ShiftInstances_WorkInstances_WorkInstanceCtrlNbr",
                table: "ShiftInstances",
                column: "WorkInstanceCtrlNbr",
                principalTable: "WorkInstances",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SlotRequirements_PositionSlots_PositionSlotCtrlNbr",
                table: "SlotRequirements",
                column: "PositionSlotCtrlNbr",
                principalTable: "PositionSlots",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TeamsWebhookConfigs_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "TeamsWebhookConfigs",
                column: "WorkAreaGroupCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TeamsWebhookConfigs_Railroads_RailroadCtrlNbr",
                table: "TeamsWebhookConfigs",
                column: "RailroadCtrlNbr",
                principalTable: "Railroads",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TimeEntries_Employees_EmployeeCtrlNbr",
                table: "TimeEntries",
                column: "EmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserParentAssignments_Parents_ParentCtrlNbr",
                table: "UserParentAssignments",
                column: "ParentCtrlNbr",
                principalTable: "Parents",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VacancyImpacts_AbsenceRequests_AbsenceRequestCtrlNbr",
                table: "VacancyImpacts",
                column: "AbsenceRequestCtrlNbr",
                principalTable: "AbsenceRequests",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VacancyImpacts_PositionSlots_PositionSlotCtrlNbr",
                table: "VacancyImpacts",
                column: "PositionSlotCtrlNbr",
                principalTable: "PositionSlots",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VacancyResolutionRuns_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "VacancyResolutionRuns",
                column: "WorkAreaGroupCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VacancyResolutionRuns_ShiftInstances_ShiftInstanceCtrlNbr",
                table: "VacancyResolutionRuns",
                column: "ShiftInstanceCtrlNbr",
                principalTable: "ShiftInstances",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VoluntaryReferrals_Employees_EmployeeCtrlNbr",
                table: "VoluntaryReferrals",
                column: "EmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkerExecutionLogs_WorkerSchedules_WorkerScheduleCtrlNbr",
                table: "WorkerExecutionLogs",
                column: "WorkerScheduleCtrlNbr",
                principalTable: "WorkerSchedules",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkerSchedules_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "WorkerSchedules",
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
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkInstances_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "WorkInstances",
                column: "WorkAreaGroupCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AbsenceApprovals_Employees_ApprovalOfficerCtrlNbr",
                table: "AbsenceApprovals");

            migrationBuilder.DropForeignKey(
                name: "FK_AbsenceCodeCraftOverrides_AbsenceCodes_AbsenceCodeCtrlNbr",
                table: "AbsenceCodeCraftOverrides");

            migrationBuilder.DropForeignKey(
                name: "FK_AbsenceCodeCraftOverrides_Crafts_CraftCtrlNbr",
                table: "AbsenceCodeCraftOverrides");

            migrationBuilder.DropForeignKey(
                name: "FK_AbsenceRequests_AbsenceCodes_AbsenceCodeCtrlNbr",
                table: "AbsenceRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_AbsenceRequests_Employees_EmployeeCtrlNbr",
                table: "AbsenceRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_AssignmentTemplates_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "AssignmentTemplates");

            migrationBuilder.DropForeignKey(
                name: "FK_BoardCascadePolicies_Crafts_CraftCtrlNbr",
                table: "BoardCascadePolicies");

            migrationBuilder.DropForeignKey(
                name: "FK_BoardCascadePolicies_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "BoardCascadePolicies");

            migrationBuilder.DropForeignKey(
                name: "FK_BoardMembers_Employees_EmployeeCtrlNbr",
                table: "BoardMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_BoardMembers_ExtraBoards_ExtraBoardCtrlNbr",
                table: "BoardMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_BulletinBids_Bulletins_BulletinCtrlNbr",
                table: "BulletinBids");

            migrationBuilder.DropForeignKey(
                name: "FK_BulletinBids_Employees_EmployeeCtrlNbr",
                table: "BulletinBids");

            migrationBuilder.DropForeignKey(
                name: "FK_BulletinPolicies_Crafts_CraftCtrlNbr",
                table: "BulletinPolicies");

            migrationBuilder.DropForeignKey(
                name: "FK_Bulletins_Crafts_CraftCtrlNbr",
                table: "Bulletins");

            migrationBuilder.DropForeignKey(
                name: "FK_Bulletins_Employees_AwardedEmployeeCtrlNbr",
                table: "Bulletins");

            migrationBuilder.DropForeignKey(
                name: "FK_Bulletins_PositionVacancies_PositionVacancyCtrlNbr",
                table: "Bulletins");

            migrationBuilder.DropForeignKey(
                name: "FK_CertificationRevocationRecords_EmployeeCertifications_EmployeeCertificationCtrlNbr",
                table: "CertificationRevocationRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_ChangeNotifications_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "ChangeNotifications");

            migrationBuilder.DropForeignKey(
                name: "FK_CompensationBalances_Employees_EmployeeCtrlNbr",
                table: "CompensationBalances");

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
                name: "FK_CraftRegulatoryQualifications_RegulatoryQualifications_RegulatoryQualificationCtrlNbr",
                table: "CraftRegulatoryQualifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Crafts_DynamicGroups_DynamicGroupCtrlNbr",
                table: "Crafts");

            migrationBuilder.DropForeignKey(
                name: "FK_Crafts_RegulatoryStandards_RegulatoryStandardCtrlNbr",
                table: "Crafts");

            migrationBuilder.DropForeignKey(
                name: "FK_CrewAttachmentInstances_Crews_CrewCtrlNbr",
                table: "CrewAttachmentInstances");

            migrationBuilder.DropForeignKey(
                name: "FK_CrewAttachmentInstances_WorkInstances_WorkInstanceCtrlNbr",
                table: "CrewAttachmentInstances");

            migrationBuilder.DropForeignKey(
                name: "FK_CrewAttachmentTemplates_AssignmentTemplates_AssignmentTemplateCtrlNbr",
                table: "CrewAttachmentTemplates");

            migrationBuilder.DropForeignKey(
                name: "FK_CrewAttachmentTemplates_Crews_CrewCtrlNbr",
                table: "CrewAttachmentTemplates");

            migrationBuilder.DropForeignKey(
                name: "FK_CrewIncumbencies_CrewPositions_CrewPositionCtrlNbr",
                table: "CrewIncumbencies");

            migrationBuilder.DropForeignKey(
                name: "FK_CrewIncumbencies_Employees_EmployeeCtrlNbr",
                table: "CrewIncumbencies");

            migrationBuilder.DropForeignKey(
                name: "FK_CrewOffDays_CrewPositions_CrewPositionCtrlNbr",
                table: "CrewOffDays");

            migrationBuilder.DropForeignKey(
                name: "FK_CrewPositions_Crews_CrewCtrlNbr",
                table: "CrewPositions");

            migrationBuilder.DropForeignKey(
                name: "FK_CrewPositions_PositionRoles_PositionRoleCtrlNbr",
                table: "CrewPositions");

            migrationBuilder.DropForeignKey(
                name: "FK_DailyEmployeeStatusRecords_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "DailyEmployeeStatusRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_DailyEmployeeStatusRecords_Employees_EmployeeCtrlNbr",
                table: "DailyEmployeeStatusRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_DispatchDecisionLogs_Employees_SelectedEmployeeCtrlNbr",
                table: "DispatchDecisionLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_DispatchDecisionLogs_PositionSlots_PositionSlotCtrlNbr",
                table: "DispatchDecisionLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_DispatchOverrides_Employees_EmployeeCtrlNbr",
                table: "DispatchOverrides");

            migrationBuilder.DropForeignKey(
                name: "FK_DispatchOverrides_PositionSlots_PositionSlotCtrlNbr",
                table: "DispatchOverrides");

            migrationBuilder.DropForeignKey(
                name: "FK_DispatchProjections_Employees_ProjectedEmployeeCtrlNbr",
                table: "DispatchProjections");

            migrationBuilder.DropForeignKey(
                name: "FK_DispatchProjections_PositionSlots_PositionSlotCtrlNbr",
                table: "DispatchProjections");

            migrationBuilder.DropForeignKey(
                name: "FK_DisplacementCases_Crafts_CraftCtrlNbr",
                table: "DisplacementCases");

            migrationBuilder.DropForeignKey(
                name: "FK_DisplacementCases_Employees_EmployeeCtrlNbr",
                table: "DisplacementCases");

            migrationBuilder.DropForeignKey(
                name: "FK_DisplacementClaims_DisplacementCases_CaseCtrlNbr",
                table: "DisplacementClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_DisplacementClaims_Employees_TargetEmployeeCtrlNbr",
                table: "DisplacementClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_DrugAlcoholActions_DrugAlcoholTestRecords_TestRecordCtrlNbr",
                table: "DrugAlcoholActions");

            migrationBuilder.DropForeignKey(
                name: "FK_DrugAlcoholActions_Employees_EmployeeCtrlNbr",
                table: "DrugAlcoholActions");

            migrationBuilder.DropForeignKey(
                name: "FK_DrugAlcoholTestRecords_Employees_EmployeeCtrlNbr",
                table: "DrugAlcoholTestRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_DynamicGroups_DynamicGroups_ParentGroupCtrlNbr",
                table: "DynamicGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_DynamicGroups_GroupTypes_GroupTypeCtrlNbr",
                table: "DynamicGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_EarningApprovals_Employees_OfficerCtrlNbr",
                table: "EarningApprovals");

            migrationBuilder.DropForeignKey(
                name: "FK_EarningApprovals_PayrollRecords_PayrollRecordCtrlNbr",
                table: "EarningApprovals");

            migrationBuilder.DropForeignKey(
                name: "FK_EarningCodeRules_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "EarningCodeRules");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeBookings_Employees_EmployeeCtrlNbr",
                table: "EmployeeBookings");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeCertifications_Employees_EmployeeCtrlNbr",
                table: "EmployeeCertifications");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeCertifications_RegulatoryQualifications_RegulatoryQualificationCtrlNbr",
                table: "EmployeeCertifications");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeePriorServiceCredits_Employees_EmployeeCtrlNbr",
                table: "EmployeePriorServiceCredits");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_EmploymentStatuses_EmploymentStatusCtrlNbr",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_EmploymentStatusHistory_Employees_EmployeeCtrlNbr",
                table: "EmploymentStatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_EmploymentStatusHistory_EmploymentStatuses_EmploymentStatusCtrlNbr",
                table: "EmploymentStatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_ExtraBoards_Crafts_CraftCtrlNbr",
                table: "ExtraBoards");

            migrationBuilder.DropForeignKey(
                name: "FK_ExtraBoards_DynamicGroups_PlacedGroupCtrlNbr",
                table: "ExtraBoards");

            migrationBuilder.DropForeignKey(
                name: "FK_FraDutyTours_Employees_EmployeeCtrlNbr",
                table: "FraDutyTours");

            migrationBuilder.DropForeignKey(
                name: "FK_FraDutyTours_RegulatoryStandards_RegulatoryStandardCtrlNbr",
                table: "FraDutyTours");

            migrationBuilder.DropForeignKey(
                name: "FK_FraDutyTourSegments_OnDutyRecords_OnDutyRecordCtrlNbr",
                table: "FraDutyTourSegments");

            migrationBuilder.DropForeignKey(
                name: "FK_FraExcessServiceReports_Employees_EmployeeCtrlNbr",
                table: "FraExcessServiceReports");

            migrationBuilder.DropForeignKey(
                name: "FK_FraExcessServiceReports_FraDutyTours_DutyTourCtrlNbr",
                table: "FraExcessServiceReports");

            migrationBuilder.DropForeignKey(
                name: "FK_FraMonthlyAccumulators_Employees_EmployeeCtrlNbr",
                table: "FraMonthlyAccumulators");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupAttributeDefinitions_GroupTypes_GroupTypeCtrlNbr",
                table: "GroupAttributeDefinitions");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupAttributeValues_DynamicGroups_GroupCtrlNbr",
                table: "GroupAttributeValues");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupAttributeValues_GroupAttributeDefinitions_AttributeDefinitionCtrlNbr",
                table: "GroupAttributeValues");

            migrationBuilder.DropForeignKey(
                name: "FK_HolidayPayrollRecords_Employees_EmployeeCtrlNbr",
                table: "HolidayPayrollRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_HolidayPayrollRecords_Holidays_HolidayCtrlNbr",
                table: "HolidayPayrollRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_HolidayQualificationRules_Crafts_CraftCtrlNbr",
                table: "HolidayQualificationRules");

            migrationBuilder.DropForeignKey(
                name: "FK_HolidayQualificationRules_Holidays_HolidayCtrlNbr",
                table: "HolidayQualificationRules");

            migrationBuilder.DropForeignKey(
                name: "FK_Holidays_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "Holidays");

            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_Parents_ParentCtrlNbr",
                table: "Invitations");

            migrationBuilder.DropForeignKey(
                name: "FK_NotificationProviderConfigs_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "NotificationProviderConfigs");

            migrationBuilder.DropForeignKey(
                name: "FK_NotificationRequests_Employees_EmployeeCtrlNbr",
                table: "NotificationRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_NotificationRequests_PositionSlots_PositionSlotCtrlNbr",
                table: "NotificationRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_OffDutyRecords_Employees_EmployeeCtrlNbr",
                table: "OffDutyRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_OffDutyRecords_OnDutyRecords_OnDutyRecordCtrlNbr",
                table: "OffDutyRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_OnDutyBillingRecords_OnDutyRecords_OnDutyRecordCtrlNbr",
                table: "OnDutyBillingRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_OnDutyLocomotiveRecords_OnDutyRecords_OnDutyRecordCtrlNbr",
                table: "OnDutyLocomotiveRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_OnDutyMaterialRecords_OnDutyRecords_OnDutyRecordCtrlNbr",
                table: "OnDutyMaterialRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_OnDutyRecords_Employees_EmployeeCtrlNbr",
                table: "OnDutyRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_OnDutyRecords_PositionSlots_PositionSlotCtrlNbr",
                table: "OnDutyRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_PayRates_Crafts_CraftCtrlNbr",
                table: "PayRates");

            migrationBuilder.DropForeignKey(
                name: "FK_PayRates_PositionRoles_PositionRoleCtrlNbr",
                table: "PayRates");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollExportBatches_PayrollRuns_PayrollRunCtrlNbr",
                table: "PayrollExportBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollImportRecords_Employees_EmployeeCtrlNbr",
                table: "PayrollImportRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollRecords_Employees_EmployeeCtrlNbr",
                table: "PayrollRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollRecords_OnDutyRecords_OnDutyRecordCtrlNbr",
                table: "PayrollRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollRecords_PayrollRuns_PayrollRunCtrlNbr",
                table: "PayrollRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollTiers_DynamicGroups_DynamicGroupCtrlNbr",
                table: "PayrollTiers");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionRoles_Crafts_CraftCtrlNbr",
                table: "PositionRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionSlotInstances_CrewPositions_CrewPositionCtrlNbr",
                table: "PositionSlotInstances");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionSlotInstances_Employees_IncumbentEmployeeCtrlNbr",
                table: "PositionSlotInstances");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionSlots_Employees_BoundEmployeeCtrlNbr",
                table: "PositionSlots");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionSlots_PositionRoles_PositionRoleCtrlNbr",
                table: "PositionSlots");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionSlots_WorkInstances_WorkInstanceCtrlNbr",
                table: "PositionSlots");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionVacancies_Crafts_CraftCtrlNbr",
                table: "PositionVacancies");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionVacancies_Employees_PreviousIncumbentCtrlNbr",
                table: "PositionVacancies");

            migrationBuilder.DropForeignKey(
                name: "FK_RailroadGroupPlacements_DynamicGroups_GroupCtrlNbr",
                table: "RailroadGroupPlacements");

            migrationBuilder.DropForeignKey(
                name: "FK_RailroadGroupPlacements_Railroads_RailroadCtrlNbr",
                table: "RailroadGroupPlacements");

            migrationBuilder.DropForeignKey(
                name: "FK_RailroadHolidaySelections_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "RailroadHolidaySelections");

            migrationBuilder.DropForeignKey(
                name: "FK_RailroadInformationReadReceipts_Employees_EmployeeCtrlNbr",
                table: "RailroadInformationReadReceipts");

            migrationBuilder.DropForeignKey(
                name: "FK_RailroadInformationReadReceipts_RailroadInformations_InformationCtrlNbr",
                table: "RailroadInformationReadReceipts");

            migrationBuilder.DropForeignKey(
                name: "FK_RailroadInformations_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "RailroadInformations");

            migrationBuilder.DropForeignKey(
                name: "FK_ReliefCoverageRules_AssignmentTemplates_AssignmentTemplateCtrlNbr",
                table: "ReliefCoverageRules");

            migrationBuilder.DropForeignKey(
                name: "FK_ReliefCoverageRules_Crews_ReliefCrewCtrlNbr",
                table: "ReliefCoverageRules");

            migrationBuilder.DropForeignKey(
                name: "FK_RosterBoardPositions_Employees_EmployeeCtrlNbr",
                table: "RosterBoardPositions");

            migrationBuilder.DropForeignKey(
                name: "FK_RosterBoards_Crafts_CraftCtrlNbr",
                table: "RosterBoards");

            migrationBuilder.DropForeignKey(
                name: "FK_RosterBoards_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "RosterBoards");

            migrationBuilder.DropForeignKey(
                name: "FK_Rosters_Crafts_CraftCtrlNbr",
                table: "Rosters");

            migrationBuilder.DropForeignKey(
                name: "FK_SafetyCategories_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "SafetyCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_SafetyObservationActions_Employees_TakenByCtrlNbr",
                table: "SafetyObservationActions");

            migrationBuilder.DropForeignKey(
                name: "FK_SafetyObservationResolutions_Employees_ResolvedByCtrlNbr",
                table: "SafetyObservationResolutions");

            migrationBuilder.DropForeignKey(
                name: "FK_SafetyObservationResolutions_SafetyObservations_ObservationCtrlNbr",
                table: "SafetyObservationResolutions");

            migrationBuilder.DropForeignKey(
                name: "FK_SafetyObservations_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "SafetyObservations");

            migrationBuilder.DropForeignKey(
                name: "FK_SafetyObservations_Employees_ObserverEmployeeCtrlNbr",
                table: "SafetyObservations");

            migrationBuilder.DropForeignKey(
                name: "FK_Seniority_Employees_EmployeeCtrlNbr",
                table: "Seniority");

            migrationBuilder.DropForeignKey(
                name: "FK_Seniority_Rosters_RosterCtrlNbr",
                table: "Seniority");

            migrationBuilder.DropForeignKey(
                name: "FK_SeniorityMovePolicies_Crafts_CraftCtrlNbr",
                table: "SeniorityMovePolicies");

            migrationBuilder.DropForeignKey(
                name: "FK_SeniorityMoves_Crafts_CraftCtrlNbr",
                table: "SeniorityMoves");

            migrationBuilder.DropForeignKey(
                name: "FK_SeniorityMoves_Employees_EmployeeCtrlNbr",
                table: "SeniorityMoves");

            migrationBuilder.DropForeignKey(
                name: "FK_ShiftDefinitions_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "ShiftDefinitions");

            migrationBuilder.DropForeignKey(
                name: "FK_ShiftInstances_WorkInstances_WorkInstanceCtrlNbr",
                table: "ShiftInstances");

            migrationBuilder.DropForeignKey(
                name: "FK_SlotRequirements_PositionSlots_PositionSlotCtrlNbr",
                table: "SlotRequirements");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamsWebhookConfigs_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "TeamsWebhookConfigs");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamsWebhookConfigs_Railroads_RailroadCtrlNbr",
                table: "TeamsWebhookConfigs");

            migrationBuilder.DropForeignKey(
                name: "FK_TimeEntries_Employees_EmployeeCtrlNbr",
                table: "TimeEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_UserParentAssignments_Parents_ParentCtrlNbr",
                table: "UserParentAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_VacancyImpacts_AbsenceRequests_AbsenceRequestCtrlNbr",
                table: "VacancyImpacts");

            migrationBuilder.DropForeignKey(
                name: "FK_VacancyImpacts_PositionSlots_PositionSlotCtrlNbr",
                table: "VacancyImpacts");

            migrationBuilder.DropForeignKey(
                name: "FK_VacancyResolutionRuns_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "VacancyResolutionRuns");

            migrationBuilder.DropForeignKey(
                name: "FK_VacancyResolutionRuns_ShiftInstances_ShiftInstanceCtrlNbr",
                table: "VacancyResolutionRuns");

            migrationBuilder.DropForeignKey(
                name: "FK_VoluntaryReferrals_Employees_EmployeeCtrlNbr",
                table: "VoluntaryReferrals");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkerExecutionLogs_WorkerSchedules_WorkerScheduleCtrlNbr",
                table: "WorkerExecutionLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkerSchedules_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "WorkerSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkInstances_AssignmentTemplates_AssignmentTemplateCtrlNbr",
                table: "WorkInstances");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkInstances_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "WorkInstances");

            migrationBuilder.DropIndex(
                name: "IX_WorkInstances_AssignmentTemplateCtrlNbr",
                table: "WorkInstances");

            migrationBuilder.DropIndex(
                name: "IX_WorkInstances_WorkAreaGroupCtrlNbr",
                table: "WorkInstances");

            migrationBuilder.DropIndex(
                name: "IX_WorkerSchedules_WorkAreaGroupCtrlNbr",
                table: "WorkerSchedules");

            migrationBuilder.DropIndex(
                name: "IX_WorkerExecutionLogs_WorkerScheduleCtrlNbr",
                table: "WorkerExecutionLogs");

            migrationBuilder.DropIndex(
                name: "IX_VoluntaryReferrals_EmployeeCtrlNbr",
                table: "VoluntaryReferrals");

            migrationBuilder.DropIndex(
                name: "IX_VacancyResolutionRuns_ShiftInstanceCtrlNbr",
                table: "VacancyResolutionRuns");

            migrationBuilder.DropIndex(
                name: "IX_VacancyResolutionRuns_WorkAreaGroupCtrlNbr",
                table: "VacancyResolutionRuns");

            migrationBuilder.DropIndex(
                name: "IX_VacancyImpacts_AbsenceRequestCtrlNbr",
                table: "VacancyImpacts");

            migrationBuilder.DropIndex(
                name: "IX_VacancyImpacts_PositionSlotCtrlNbr",
                table: "VacancyImpacts");

            migrationBuilder.DropIndex(
                name: "IX_TimeEntries_EmployeeCtrlNbr",
                table: "TimeEntries");

            migrationBuilder.DropIndex(
                name: "IX_TeamsWebhookConfigs_RailroadCtrlNbr",
                table: "TeamsWebhookConfigs");

            migrationBuilder.DropIndex(
                name: "IX_TeamsWebhookConfigs_WorkAreaGroupCtrlNbr",
                table: "TeamsWebhookConfigs");

            migrationBuilder.DropIndex(
                name: "IX_SlotRequirements_PositionSlotCtrlNbr",
                table: "SlotRequirements");

            migrationBuilder.DropIndex(
                name: "IX_ShiftInstances_WorkInstanceCtrlNbr",
                table: "ShiftInstances");

            migrationBuilder.DropIndex(
                name: "IX_ShiftDefinitions_WorkAreaGroupCtrlNbr",
                table: "ShiftDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_SeniorityMoves_CraftCtrlNbr",
                table: "SeniorityMoves");

            migrationBuilder.DropIndex(
                name: "IX_SeniorityMoves_EmployeeCtrlNbr",
                table: "SeniorityMoves");

            migrationBuilder.DropIndex(
                name: "IX_SeniorityMovePolicies_CraftCtrlNbr",
                table: "SeniorityMovePolicies");

            migrationBuilder.DropIndex(
                name: "IX_Seniority_EmployeeCtrlNbr",
                table: "Seniority");

            migrationBuilder.DropIndex(
                name: "IX_Seniority_RosterCtrlNbr",
                table: "Seniority");

            migrationBuilder.DropIndex(
                name: "IX_SafetyObservations_ObserverEmployeeCtrlNbr",
                table: "SafetyObservations");

            migrationBuilder.DropIndex(
                name: "IX_SafetyObservations_WorkAreaGroupCtrlNbr",
                table: "SafetyObservations");

            migrationBuilder.DropIndex(
                name: "IX_SafetyObservationResolutions_ResolvedByCtrlNbr",
                table: "SafetyObservationResolutions");

            migrationBuilder.DropIndex(
                name: "IX_SafetyObservationActions_TakenByCtrlNbr",
                table: "SafetyObservationActions");

            migrationBuilder.DropIndex(
                name: "IX_SafetyCategories_WorkAreaGroupCtrlNbr",
                table: "SafetyCategories");

            migrationBuilder.DropIndex(
                name: "IX_Rosters_CraftCtrlNbr",
                table: "Rosters");

            migrationBuilder.DropIndex(
                name: "IX_RosterBoards_CraftCtrlNbr",
                table: "RosterBoards");

            migrationBuilder.DropIndex(
                name: "IX_RosterBoards_WorkAreaGroupCtrlNbr",
                table: "RosterBoards");

            migrationBuilder.DropIndex(
                name: "IX_RosterBoardPositions_EmployeeCtrlNbr",
                table: "RosterBoardPositions");

            migrationBuilder.DropIndex(
                name: "IX_ReliefCoverageRules_AssignmentTemplateCtrlNbr",
                table: "ReliefCoverageRules");

            migrationBuilder.DropIndex(
                name: "IX_ReliefCoverageRules_ReliefCrewCtrlNbr",
                table: "ReliefCoverageRules");

            migrationBuilder.DropIndex(
                name: "IX_RailroadInformations_WorkAreaGroupCtrlNbr",
                table: "RailroadInformations");

            migrationBuilder.DropIndex(
                name: "IX_RailroadInformationReadReceipts_EmployeeCtrlNbr",
                table: "RailroadInformationReadReceipts");

            migrationBuilder.DropIndex(
                name: "IX_PositionVacancies_CraftCtrlNbr",
                table: "PositionVacancies");

            migrationBuilder.DropIndex(
                name: "IX_PositionVacancies_PreviousIncumbentCtrlNbr",
                table: "PositionVacancies");

            migrationBuilder.DropIndex(
                name: "IX_PositionSlots_BoundEmployeeCtrlNbr",
                table: "PositionSlots");

            migrationBuilder.DropIndex(
                name: "IX_PositionSlots_PositionRoleCtrlNbr",
                table: "PositionSlots");

            migrationBuilder.DropIndex(
                name: "IX_PositionSlots_WorkInstanceCtrlNbr",
                table: "PositionSlots");

            migrationBuilder.DropIndex(
                name: "IX_PositionSlotInstances_CrewPositionCtrlNbr",
                table: "PositionSlotInstances");

            migrationBuilder.DropIndex(
                name: "IX_PositionSlotInstances_IncumbentEmployeeCtrlNbr",
                table: "PositionSlotInstances");

            migrationBuilder.DropIndex(
                name: "IX_PositionRoles_CraftCtrlNbr",
                table: "PositionRoles");

            migrationBuilder.DropIndex(
                name: "IX_PayrollTiers_DynamicGroupCtrlNbr",
                table: "PayrollTiers");

            migrationBuilder.DropIndex(
                name: "IX_PayrollRecords_EmployeeCtrlNbr",
                table: "PayrollRecords");

            migrationBuilder.DropIndex(
                name: "IX_PayrollRecords_OnDutyRecordCtrlNbr",
                table: "PayrollRecords");

            migrationBuilder.DropIndex(
                name: "IX_PayrollRecords_PayrollRunCtrlNbr",
                table: "PayrollRecords");

            migrationBuilder.DropIndex(
                name: "IX_PayrollImportRecords_EmployeeCtrlNbr",
                table: "PayrollImportRecords");

            migrationBuilder.DropIndex(
                name: "IX_PayrollExportBatches_PayrollRunCtrlNbr",
                table: "PayrollExportBatches");

            migrationBuilder.DropIndex(
                name: "IX_PayRates_CraftCtrlNbr",
                table: "PayRates");

            migrationBuilder.DropIndex(
                name: "IX_PayRates_PositionRoleCtrlNbr",
                table: "PayRates");

            migrationBuilder.DropIndex(
                name: "IX_OnDutyRecords_EmployeeCtrlNbr",
                table: "OnDutyRecords");

            migrationBuilder.DropIndex(
                name: "IX_OnDutyRecords_PositionSlotCtrlNbr",
                table: "OnDutyRecords");

            migrationBuilder.DropIndex(
                name: "IX_OnDutyMaterialRecords_OnDutyRecordCtrlNbr",
                table: "OnDutyMaterialRecords");

            migrationBuilder.DropIndex(
                name: "IX_OnDutyLocomotiveRecords_OnDutyRecordCtrlNbr",
                table: "OnDutyLocomotiveRecords");

            migrationBuilder.DropIndex(
                name: "IX_OnDutyBillingRecords_OnDutyRecordCtrlNbr",
                table: "OnDutyBillingRecords");

            migrationBuilder.DropIndex(
                name: "IX_OffDutyRecords_EmployeeCtrlNbr",
                table: "OffDutyRecords");

            migrationBuilder.DropIndex(
                name: "IX_OffDutyRecords_OnDutyRecordCtrlNbr",
                table: "OffDutyRecords");

            migrationBuilder.DropIndex(
                name: "IX_NotificationRequests_EmployeeCtrlNbr",
                table: "NotificationRequests");

            migrationBuilder.DropIndex(
                name: "IX_NotificationRequests_PositionSlotCtrlNbr",
                table: "NotificationRequests");

            migrationBuilder.DropIndex(
                name: "IX_NotificationProviderConfigs_WorkAreaGroupCtrlNbr",
                table: "NotificationProviderConfigs");

            migrationBuilder.DropIndex(
                name: "IX_Holidays_WorkAreaGroupCtrlNbr",
                table: "Holidays");

            migrationBuilder.DropIndex(
                name: "IX_HolidayQualificationRules_CraftCtrlNbr",
                table: "HolidayQualificationRules");

            migrationBuilder.DropIndex(
                name: "IX_HolidayQualificationRules_HolidayCtrlNbr",
                table: "HolidayQualificationRules");

            migrationBuilder.DropIndex(
                name: "IX_HolidayPayrollRecords_EmployeeCtrlNbr",
                table: "HolidayPayrollRecords");

            migrationBuilder.DropIndex(
                name: "IX_HolidayPayrollRecords_HolidayCtrlNbr",
                table: "HolidayPayrollRecords");

            migrationBuilder.DropIndex(
                name: "IX_GroupAttributeValues_AttributeDefinitionCtrlNbr",
                table: "GroupAttributeValues");

            migrationBuilder.DropIndex(
                name: "IX_GroupAttributeValues_GroupCtrlNbr",
                table: "GroupAttributeValues");

            migrationBuilder.DropIndex(
                name: "IX_GroupAttributeDefinitions_GroupTypeCtrlNbr",
                table: "GroupAttributeDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_FraExcessServiceReports_DutyTourCtrlNbr",
                table: "FraExcessServiceReports");

            migrationBuilder.DropIndex(
                name: "IX_FraExcessServiceReports_EmployeeCtrlNbr",
                table: "FraExcessServiceReports");

            migrationBuilder.DropIndex(
                name: "IX_FraDutyTourSegments_OnDutyRecordCtrlNbr",
                table: "FraDutyTourSegments");

            migrationBuilder.DropIndex(
                name: "IX_FraDutyTours_EmployeeCtrlNbr",
                table: "FraDutyTours");

            migrationBuilder.DropIndex(
                name: "IX_FraDutyTours_RegulatoryStandardCtrlNbr",
                table: "FraDutyTours");

            migrationBuilder.DropIndex(
                name: "IX_ExtraBoards_CraftCtrlNbr",
                table: "ExtraBoards");

            migrationBuilder.DropIndex(
                name: "IX_ExtraBoards_PlacedGroupCtrlNbr",
                table: "ExtraBoards");

            migrationBuilder.DropIndex(
                name: "IX_EmploymentStatusHistory_EmployeeCtrlNbr",
                table: "EmploymentStatusHistory");

            migrationBuilder.DropIndex(
                name: "IX_EmploymentStatusHistory_EmploymentStatusCtrlNbr",
                table: "EmploymentStatusHistory");

            migrationBuilder.DropIndex(
                name: "IX_Employees_EmploymentStatusCtrlNbr",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_EmployeePriorServiceCredits_EmployeeCtrlNbr",
                table: "EmployeePriorServiceCredits");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeCertifications_EmployeeCtrlNbr",
                table: "EmployeeCertifications");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeCertifications_RegulatoryQualificationCtrlNbr",
                table: "EmployeeCertifications");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeBookings_EmployeeCtrlNbr",
                table: "EmployeeBookings");

            migrationBuilder.DropIndex(
                name: "IX_EarningCodeRules_WorkAreaGroupCtrlNbr",
                table: "EarningCodeRules");

            migrationBuilder.DropIndex(
                name: "IX_EarningApprovals_OfficerCtrlNbr",
                table: "EarningApprovals");

            migrationBuilder.DropIndex(
                name: "IX_EarningApprovals_PayrollRecordCtrlNbr",
                table: "EarningApprovals");

            migrationBuilder.DropIndex(
                name: "IX_DynamicGroups_GroupTypeCtrlNbr",
                table: "DynamicGroups");

            migrationBuilder.DropIndex(
                name: "IX_DrugAlcoholTestRecords_EmployeeCtrlNbr",
                table: "DrugAlcoholTestRecords");

            migrationBuilder.DropIndex(
                name: "IX_DrugAlcoholActions_EmployeeCtrlNbr",
                table: "DrugAlcoholActions");

            migrationBuilder.DropIndex(
                name: "IX_DrugAlcoholActions_TestRecordCtrlNbr",
                table: "DrugAlcoholActions");

            migrationBuilder.DropIndex(
                name: "IX_DisplacementClaims_CaseCtrlNbr",
                table: "DisplacementClaims");

            migrationBuilder.DropIndex(
                name: "IX_DisplacementClaims_TargetEmployeeCtrlNbr",
                table: "DisplacementClaims");

            migrationBuilder.DropIndex(
                name: "IX_DisplacementCases_CraftCtrlNbr",
                table: "DisplacementCases");

            migrationBuilder.DropIndex(
                name: "IX_DisplacementCases_EmployeeCtrlNbr",
                table: "DisplacementCases");

            migrationBuilder.DropIndex(
                name: "IX_DispatchProjections_PositionSlotCtrlNbr",
                table: "DispatchProjections");

            migrationBuilder.DropIndex(
                name: "IX_DispatchProjections_ProjectedEmployeeCtrlNbr",
                table: "DispatchProjections");

            migrationBuilder.DropIndex(
                name: "IX_DispatchOverrides_EmployeeCtrlNbr",
                table: "DispatchOverrides");

            migrationBuilder.DropIndex(
                name: "IX_DispatchOverrides_PositionSlotCtrlNbr",
                table: "DispatchOverrides");

            migrationBuilder.DropIndex(
                name: "IX_DispatchDecisionLogs_PositionSlotCtrlNbr",
                table: "DispatchDecisionLogs");

            migrationBuilder.DropIndex(
                name: "IX_DispatchDecisionLogs_SelectedEmployeeCtrlNbr",
                table: "DispatchDecisionLogs");

            migrationBuilder.DropIndex(
                name: "IX_DailyEmployeeStatusRecords_WorkAreaGroupCtrlNbr",
                table: "DailyEmployeeStatusRecords");

            migrationBuilder.DropIndex(
                name: "IX_CrewPositions_CrewCtrlNbr",
                table: "CrewPositions");

            migrationBuilder.DropIndex(
                name: "IX_CrewPositions_PositionRoleCtrlNbr",
                table: "CrewPositions");

            migrationBuilder.DropIndex(
                name: "IX_CrewOffDays_CrewPositionCtrlNbr",
                table: "CrewOffDays");

            migrationBuilder.DropIndex(
                name: "IX_CrewIncumbencies_CrewPositionCtrlNbr",
                table: "CrewIncumbencies");

            migrationBuilder.DropIndex(
                name: "IX_CrewIncumbencies_EmployeeCtrlNbr",
                table: "CrewIncumbencies");

            migrationBuilder.DropIndex(
                name: "IX_CrewAttachmentTemplates_AssignmentTemplateCtrlNbr",
                table: "CrewAttachmentTemplates");

            migrationBuilder.DropIndex(
                name: "IX_CrewAttachmentTemplates_CrewCtrlNbr",
                table: "CrewAttachmentTemplates");

            migrationBuilder.DropIndex(
                name: "IX_CrewAttachmentInstances_CrewCtrlNbr",
                table: "CrewAttachmentInstances");

            migrationBuilder.DropIndex(
                name: "IX_CrewAttachmentInstances_WorkInstanceCtrlNbr",
                table: "CrewAttachmentInstances");

            migrationBuilder.DropIndex(
                name: "IX_Crafts_DynamicGroupCtrlNbr",
                table: "Crafts");

            migrationBuilder.DropIndex(
                name: "IX_Crafts_RegulatoryStandardCtrlNbr",
                table: "Crafts");

            migrationBuilder.DropIndex(
                name: "IX_CraftRegulatoryQualifications_RegulatoryQualificationCtrlNbr",
                table: "CraftRegulatoryQualifications");

            migrationBuilder.DropIndex(
                name: "IX_CraftDisplacementPolicies_CraftCtrlNbr",
                table: "CraftDisplacementPolicies");

            migrationBuilder.DropIndex(
                name: "IX_ChangeNotifications_WorkAreaGroupCtrlNbr",
                table: "ChangeNotifications");

            migrationBuilder.DropIndex(
                name: "IX_CertificationRevocationRecords_EmployeeCertificationCtrlNbr",
                table: "CertificationRevocationRecords");

            migrationBuilder.DropIndex(
                name: "IX_Bulletins_AwardedEmployeeCtrlNbr",
                table: "Bulletins");

            migrationBuilder.DropIndex(
                name: "IX_Bulletins_CraftCtrlNbr",
                table: "Bulletins");

            migrationBuilder.DropIndex(
                name: "IX_Bulletins_PositionVacancyCtrlNbr",
                table: "Bulletins");

            migrationBuilder.DropIndex(
                name: "IX_BulletinPolicies_CraftCtrlNbr",
                table: "BulletinPolicies");

            migrationBuilder.DropIndex(
                name: "IX_BulletinBids_BulletinCtrlNbr",
                table: "BulletinBids");

            migrationBuilder.DropIndex(
                name: "IX_BulletinBids_EmployeeCtrlNbr",
                table: "BulletinBids");

            migrationBuilder.DropIndex(
                name: "IX_BoardMembers_EmployeeCtrlNbr",
                table: "BoardMembers");

            migrationBuilder.DropIndex(
                name: "IX_BoardMembers_ExtraBoardCtrlNbr",
                table: "BoardMembers");

            migrationBuilder.DropIndex(
                name: "IX_BoardCascadePolicies_CraftCtrlNbr",
                table: "BoardCascadePolicies");

            migrationBuilder.DropIndex(
                name: "IX_BoardCascadePolicies_WorkAreaGroupCtrlNbr",
                table: "BoardCascadePolicies");

            migrationBuilder.DropIndex(
                name: "IX_AssignmentTemplates_WorkAreaGroupCtrlNbr",
                table: "AssignmentTemplates");

            migrationBuilder.DropIndex(
                name: "IX_AbsenceRequests_AbsenceCodeCtrlNbr",
                table: "AbsenceRequests");

            migrationBuilder.DropIndex(
                name: "IX_AbsenceRequests_EmployeeCtrlNbr",
                table: "AbsenceRequests");

            migrationBuilder.DropIndex(
                name: "IX_AbsenceCodeCraftOverrides_CraftCtrlNbr",
                table: "AbsenceCodeCraftOverrides");

            migrationBuilder.DropIndex(
                name: "IX_AbsenceApprovals_ApprovalOfficerCtrlNbr",
                table: "AbsenceApprovals");
        }
    }
}
