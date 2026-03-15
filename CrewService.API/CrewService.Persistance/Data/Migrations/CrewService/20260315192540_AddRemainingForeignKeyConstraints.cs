using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddRemainingForeignKeyConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_OriginalEntryCtrlNbr",
                table: "TimeEntries",
                column: "OriginalEntryCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_SlotRequirements_PositionRoleCtrlNbr",
                table: "SlotRequirements",
                column: "PositionRoleCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_SlotRequirements_QualificationTypeCtrlNbr",
                table: "SlotRequirements",
                column: "QualificationTypeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_SeniorityMoves_DisplacedEmployeeCtrlNbr",
                table: "SeniorityMoves",
                column: "DisplacedEmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneNumbers_PhoneTypeCtrlNbr",
                table: "PhoneNumbers",
                column: "PhoneTypeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollImportRecords_PayrollRecordCtrlNbr",
                table: "PayrollImportRecords",
                column: "PayrollRecordCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_OnDutyRecords_BookingCtrlNbr",
                table: "OnDutyRecords",
                column: "BookingCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_HolidayPayrollRecords_PayrollRecordCtrlNbr",
                table: "HolidayPayrollRecords",
                column: "PayrollRecordCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeBookings_PositionSlotCtrlNbr",
                table: "EmployeeBookings",
                column: "PositionSlotCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAddresses_EmailTypeCtrlNbr",
                table: "EmailAddresses",
                column: "EmailTypeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOverrides_ApprovedByCtrlNbr",
                table: "DispatchOverrides",
                column: "ApprovedByCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_Crews_HomeGroupCtrlNbr",
                table: "Crews",
                column: "HomeGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_CertificationRevocationRecords_PresidingOfficerCtrlNbr",
                table: "CertificationRevocationRecords",
                column: "PresidingOfficerCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_AddressTypeCtrlNbr",
                table: "Addresses",
                column: "AddressTypeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceRequests_ApprovedByCtrlNbr",
                table: "AbsenceRequests",
                column: "ApprovedByCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceRequests_PositionSlotCtrlNbr",
                table: "AbsenceRequests",
                column: "PositionSlotCtrlNbr");

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
                name: "FK_Addresses_AddressTypes_AddressTypeCtrlNbr",
                table: "Addresses",
                column: "AddressTypeCtrlNbr",
                principalTable: "AddressTypes",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CertificationRevocationRecords_Employees_PresidingOfficerCtrlNbr",
                table: "CertificationRevocationRecords",
                column: "PresidingOfficerCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Crews_DynamicGroups_HomeGroupCtrlNbr",
                table: "Crews",
                column: "HomeGroupCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchOverrides_Employees_ApprovedByCtrlNbr",
                table: "DispatchOverrides",
                column: "ApprovedByCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_EmailAddresses_EmailAddressTypes_EmailTypeCtrlNbr",
                table: "EmailAddresses",
                column: "EmailTypeCtrlNbr",
                principalTable: "EmailAddressTypes",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

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
                name: "FK_OnDutyRecords_EmployeeBookings_BookingCtrlNbr",
                table: "OnDutyRecords",
                column: "BookingCtrlNbr",
                principalTable: "EmployeeBookings",
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
                name: "FK_PhoneNumbers_PhoneNumberTypes_PhoneTypeCtrlNbr",
                table: "PhoneNumbers",
                column: "PhoneTypeCtrlNbr",
                principalTable: "PhoneNumberTypes",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

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
                name: "FK_TimeEntries_TimeEntries_OriginalEntryCtrlNbr",
                table: "TimeEntries",
                column: "OriginalEntryCtrlNbr",
                principalTable: "TimeEntries",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AbsenceRequests_Employees_ApprovedByCtrlNbr",
                table: "AbsenceRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_AbsenceRequests_PositionSlots_PositionSlotCtrlNbr",
                table: "AbsenceRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Addresses_AddressTypes_AddressTypeCtrlNbr",
                table: "Addresses");

            migrationBuilder.DropForeignKey(
                name: "FK_CertificationRevocationRecords_Employees_PresidingOfficerCtrlNbr",
                table: "CertificationRevocationRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_Crews_DynamicGroups_HomeGroupCtrlNbr",
                table: "Crews");

            migrationBuilder.DropForeignKey(
                name: "FK_DispatchOverrides_Employees_ApprovedByCtrlNbr",
                table: "DispatchOverrides");

            migrationBuilder.DropForeignKey(
                name: "FK_EmailAddresses_EmailAddressTypes_EmailTypeCtrlNbr",
                table: "EmailAddresses");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeBookings_PositionSlots_PositionSlotCtrlNbr",
                table: "EmployeeBookings");

            migrationBuilder.DropForeignKey(
                name: "FK_HolidayPayrollRecords_PayrollRecords_PayrollRecordCtrlNbr",
                table: "HolidayPayrollRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_OnDutyRecords_EmployeeBookings_BookingCtrlNbr",
                table: "OnDutyRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollImportRecords_PayrollRecords_PayrollRecordCtrlNbr",
                table: "PayrollImportRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_PhoneNumbers_PhoneNumberTypes_PhoneTypeCtrlNbr",
                table: "PhoneNumbers");

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
                name: "FK_TimeEntries_TimeEntries_OriginalEntryCtrlNbr",
                table: "TimeEntries");

            migrationBuilder.DropIndex(
                name: "IX_TimeEntries_OriginalEntryCtrlNbr",
                table: "TimeEntries");

            migrationBuilder.DropIndex(
                name: "IX_SlotRequirements_PositionRoleCtrlNbr",
                table: "SlotRequirements");

            migrationBuilder.DropIndex(
                name: "IX_SlotRequirements_QualificationTypeCtrlNbr",
                table: "SlotRequirements");

            migrationBuilder.DropIndex(
                name: "IX_SeniorityMoves_DisplacedEmployeeCtrlNbr",
                table: "SeniorityMoves");

            migrationBuilder.DropIndex(
                name: "IX_PhoneNumbers_PhoneTypeCtrlNbr",
                table: "PhoneNumbers");

            migrationBuilder.DropIndex(
                name: "IX_PayrollImportRecords_PayrollRecordCtrlNbr",
                table: "PayrollImportRecords");

            migrationBuilder.DropIndex(
                name: "IX_OnDutyRecords_BookingCtrlNbr",
                table: "OnDutyRecords");

            migrationBuilder.DropIndex(
                name: "IX_HolidayPayrollRecords_PayrollRecordCtrlNbr",
                table: "HolidayPayrollRecords");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeBookings_PositionSlotCtrlNbr",
                table: "EmployeeBookings");

            migrationBuilder.DropIndex(
                name: "IX_EmailAddresses_EmailTypeCtrlNbr",
                table: "EmailAddresses");

            migrationBuilder.DropIndex(
                name: "IX_DispatchOverrides_ApprovedByCtrlNbr",
                table: "DispatchOverrides");

            migrationBuilder.DropIndex(
                name: "IX_Crews_HomeGroupCtrlNbr",
                table: "Crews");

            migrationBuilder.DropIndex(
                name: "IX_CertificationRevocationRecords_PresidingOfficerCtrlNbr",
                table: "CertificationRevocationRecords");

            migrationBuilder.DropIndex(
                name: "IX_Addresses_AddressTypeCtrlNbr",
                table: "Addresses");

            migrationBuilder.DropIndex(
                name: "IX_AbsenceRequests_ApprovedByCtrlNbr",
                table: "AbsenceRequests");

            migrationBuilder.DropIndex(
                name: "IX_AbsenceRequests_PositionSlotCtrlNbr",
                table: "AbsenceRequests");
        }
    }
}
