using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddSoftDeleteFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "SeniorityStates",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedBy_AuditDateTime",
                table: "SeniorityStates",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy_AuditName",
                table: "SeniorityStates",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SeniorityStates",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Seniority",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedBy_AuditDateTime",
                table: "Seniority",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy_AuditName",
                table: "Seniority",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Seniority",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Rosters",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedBy_AuditDateTime",
                table: "Rosters",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy_AuditName",
                table: "Rosters",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Rosters",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Railroads",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedBy_AuditDateTime",
                table: "Railroads",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy_AuditName",
                table: "Railroads",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Railroads",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "RailroadPools",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedBy_AuditDateTime",
                table: "RailroadPools",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy_AuditName",
                table: "RailroadPools",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "RailroadPools",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "RailroadPoolPayrollTiers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedBy_AuditDateTime",
                table: "RailroadPoolPayrollTiers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy_AuditName",
                table: "RailroadPoolPayrollTiers",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "RailroadPoolPayrollTiers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "RailroadPoolEmployees",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedBy_AuditDateTime",
                table: "RailroadPoolEmployees",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy_AuditName",
                table: "RailroadPoolEmployees",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "RailroadPoolEmployees",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "RailroadEmployees",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedBy_AuditDateTime",
                table: "RailroadEmployees",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy_AuditName",
                table: "RailroadEmployees",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "RailroadEmployees",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "PhoneNumberTypes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedBy_AuditDateTime",
                table: "PhoneNumberTypes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy_AuditName",
                table: "PhoneNumberTypes",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PhoneNumberTypes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "PhoneNumbers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedBy_AuditDateTime",
                table: "PhoneNumbers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy_AuditName",
                table: "PhoneNumbers",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PhoneNumbers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Parents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedBy_AuditDateTime",
                table: "Parents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy_AuditName",
                table: "Parents",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Parents",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "EmploymentStatusHistory",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedBy_AuditDateTime",
                table: "EmploymentStatusHistory",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy_AuditName",
                table: "EmploymentStatusHistory",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "EmploymentStatusHistory",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "EmploymentStatuses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedBy_AuditDateTime",
                table: "EmploymentStatuses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy_AuditName",
                table: "EmploymentStatuses",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "EmploymentStatuses",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Employees",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedBy_AuditDateTime",
                table: "Employees",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy_AuditName",
                table: "Employees",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Employees",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "EmployeePriorServiceCredits",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedBy_AuditDateTime",
                table: "EmployeePriorServiceCredits",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy_AuditName",
                table: "EmployeePriorServiceCredits",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "EmployeePriorServiceCredits",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "EmailAddressTypes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedBy_AuditDateTime",
                table: "EmailAddressTypes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy_AuditName",
                table: "EmailAddressTypes",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "EmailAddressTypes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "EmailAddresses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedBy_AuditDateTime",
                table: "EmailAddresses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy_AuditName",
                table: "EmailAddresses",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "EmailAddresses",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Crafts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedBy_AuditDateTime",
                table: "Crafts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy_AuditName",
                table: "Crafts",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Crafts",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "AddressTypes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedBy_AuditDateTime",
                table: "AddressTypes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy_AuditName",
                table: "AddressTypes",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AddressTypes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Addresses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedBy_AuditDateTime",
                table: "Addresses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy_AuditName",
                table: "Addresses",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Addresses",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SeniorityStates");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditDateTime",
                table: "SeniorityStates");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditName",
                table: "SeniorityStates");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SeniorityStates");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Seniority");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditDateTime",
                table: "Seniority");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditName",
                table: "Seniority");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Seniority");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Rosters");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditDateTime",
                table: "Rosters");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditName",
                table: "Rosters");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Rosters");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Railroads");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditDateTime",
                table: "Railroads");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditName",
                table: "Railroads");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Railroads");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "RailroadPools");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditDateTime",
                table: "RailroadPools");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditName",
                table: "RailroadPools");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "RailroadPools");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "RailroadPoolPayrollTiers");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditDateTime",
                table: "RailroadPoolPayrollTiers");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditName",
                table: "RailroadPoolPayrollTiers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "RailroadPoolPayrollTiers");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "RailroadPoolEmployees");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditDateTime",
                table: "RailroadPoolEmployees");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditName",
                table: "RailroadPoolEmployees");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "RailroadPoolEmployees");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "RailroadEmployees");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditDateTime",
                table: "RailroadEmployees");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditName",
                table: "RailroadEmployees");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "RailroadEmployees");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "PhoneNumberTypes");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditDateTime",
                table: "PhoneNumberTypes");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditName",
                table: "PhoneNumberTypes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PhoneNumberTypes");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "PhoneNumbers");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditDateTime",
                table: "PhoneNumbers");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditName",
                table: "PhoneNumbers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PhoneNumbers");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Parents");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditDateTime",
                table: "Parents");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditName",
                table: "Parents");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Parents");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "EmploymentStatusHistory");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditDateTime",
                table: "EmploymentStatusHistory");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditName",
                table: "EmploymentStatusHistory");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "EmploymentStatusHistory");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "EmploymentStatuses");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditDateTime",
                table: "EmploymentStatuses");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditName",
                table: "EmploymentStatuses");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "EmploymentStatuses");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditDateTime",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditName",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "EmployeePriorServiceCredits");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditDateTime",
                table: "EmployeePriorServiceCredits");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditName",
                table: "EmployeePriorServiceCredits");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "EmployeePriorServiceCredits");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "EmailAddressTypes");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditDateTime",
                table: "EmailAddressTypes");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditName",
                table: "EmailAddressTypes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "EmailAddressTypes");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "EmailAddresses");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditDateTime",
                table: "EmailAddresses");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditName",
                table: "EmailAddresses");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "EmailAddresses");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Crafts");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditDateTime",
                table: "Crafts");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditName",
                table: "Crafts");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Crafts");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AddressTypes");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditDateTime",
                table: "AddressTypes");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditName",
                table: "AddressTypes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AddressTypes");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditDateTime",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "DeletedBy_AuditName",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Addresses");
        }
    }
}
