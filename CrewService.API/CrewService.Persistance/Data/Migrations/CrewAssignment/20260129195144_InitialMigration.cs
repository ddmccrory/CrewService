using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewAssignment
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AddressTypes",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ClientCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Number = table.Column<int>(type: "INTEGER", nullable: false),
                    EmergencyType = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddressTypes", x => x.CtrlNbr);
                });

            migrationBuilder.CreateTable(
                name: "Crafts",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    RailroadPoolCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CraftName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CraftPluralName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CraftNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    AutoMarkUp = table.Column<bool>(type: "INTEGER", nullable: false),
                    ApproveAllMarkOffs = table.Column<bool>(type: "INTEGER", nullable: false),
                    MarkOffHours = table.Column<int>(type: "INTEGER", nullable: false),
                    MarkUpHours = table.Column<int>(type: "INTEGER", nullable: false),
                    RequiredRestHours = table.Column<int>(type: "INTEGER", nullable: false),
                    MaximumVacationDayTime = table.Column<int>(type: "INTEGER", nullable: false),
                    UnpaidMealPeriodMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    HoursofService = table.Column<bool>(type: "INTEGER", nullable: false),
                    ProcessPayroll = table.Column<bool>(type: "INTEGER", nullable: false),
                    ShowNotifications = table.Column<bool>(type: "INTEGER", nullable: false),
                    VacationAssignmentType = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Crafts", x => x.CtrlNbr);
                });

            migrationBuilder.CreateTable(
                name: "EmailAddressTypes",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ClientCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Number = table.Column<int>(type: "INTEGER", nullable: false),
                    EmergencyType = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailAddressTypes", x => x.CtrlNbr);
                });

            migrationBuilder.CreateTable(
                name: "EmployeePriorServiceCredits",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ServiceYears = table.Column<int>(type: "INTEGER", nullable: false),
                    ServiceMonths = table.Column<int>(type: "INTEGER", nullable: false),
                    ServiceDays = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeePriorServiceCredits", x => x.CtrlNbr);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ClientCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    EmployeeNumber = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
                    SocialSecurityNumber = table.Column<string>(type: "TEXT", maxLength: 9, nullable: false),
                    DriversLicenseNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    IssuingState = table.Column<string>(type: "TEXT", maxLength: 2, nullable: true),
                    Gender = table.Column<string>(type: "TEXT", maxLength: 1, nullable: false),
                    Race = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    MarriageStatus = table.Column<bool>(type: "INTEGER", nullable: false),
                    BirthDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EmploymentDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EmploymentStatusCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    AllowFMLAMarkOff = table.Column<bool>(type: "INTEGER", nullable: false),
                    CallForOvertime = table.Column<bool>(type: "INTEGER", nullable: false),
                    ProcessPayroll = table.Column<bool>(type: "INTEGER", nullable: false),
                    TieUpOffProperty = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.CtrlNbr);
                });

            migrationBuilder.CreateTable(
                name: "EmploymentStatuses",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ClientCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    StatusCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    StatusName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    StatusNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    EmploymentCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmploymentStatuses", x => x.CtrlNbr);
                });

            migrationBuilder.CreateTable(
                name: "EmploymentStatusHistory",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmploymentStatusCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    StatusChangeDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmploymentStatusHistory", x => x.CtrlNbr);
                });

            migrationBuilder.CreateTable(
                name: "PhoneNumberTypes",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ClientCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Number = table.Column<int>(type: "INTEGER", nullable: false),
                    EmergencyType = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneNumberTypes", x => x.CtrlNbr);
                });

            migrationBuilder.CreateTable(
                name: "RailroadPools",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    RailroadCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    PoolName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PoolNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RailroadPools", x => x.CtrlNbr);
                });

            migrationBuilder.CreateTable(
                name: "Rosters",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CraftCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    RailroadPayrollDepartmentCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    RosterName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RosterPluralName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RosterNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Training = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExtraBoard = table.Column<bool>(type: "INTEGER", nullable: false),
                    OvertimeBoard = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rosters", x => x.CtrlNbr);
                });

            migrationBuilder.CreateTable(
                name: "Seniority",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    RosterCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    RailroadPoolEmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    LastActiveRoster = table.Column<bool>(type: "INTEGER", nullable: false),
                    RosterDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Rank = table.Column<int>(type: "INTEGER", nullable: false),
                    StateID = table.Column<int>(type: "INTEGER", nullable: false),
                    CanTrain = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seniority", x => x.CtrlNbr);
                });

            migrationBuilder.CreateTable(
                name: "SeniorityStates",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    StateDescription = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false),
                    CutBack = table.Column<bool>(type: "INTEGER", nullable: false),
                    Inactive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeniorityStates", x => x.CtrlNbr);
                });

            migrationBuilder.CreateTable(
                name: "Addresses",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    AddressTypeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Address1 = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    Address2 = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    City = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    State = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    ZipCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Addresses", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_Addresses_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmailAddresses",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmailTypeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailAddresses", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_EmailAddresses_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhoneNumbers",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    PhoneTypeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Number = table.Column<string>(type: "TEXT", maxLength: 12, nullable: false),
                    CallingOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    DialOne = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneNumbers", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_PhoneNumbers_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RailroadPoolEmployees",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    RailroadPoolCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RailroadPoolEmployees", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_RailroadPoolEmployees_RailroadPools_RailroadPoolCtrlNbr",
                        column: x => x.RailroadPoolCtrlNbr,
                        principalTable: "RailroadPools",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RailroadPoolPayrollTiers",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    RailroadPoolCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    NumberOfDays = table.Column<int>(type: "INTEGER", nullable: false),
                    TypeOfDay = table.Column<int>(type: "INTEGER", nullable: false),
                    RatePercentage = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RailroadPoolPayrollTiers", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_RailroadPoolPayrollTiers_RailroadPools_RailroadPoolCtrlNbr",
                        column: x => x.RailroadPoolCtrlNbr,
                        principalTable: "RailroadPools",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_EmployeeCtrlNbr",
                table: "Addresses",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAddresses_EmployeeCtrlNbr",
                table: "EmailAddresses",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_EmployeeNumber",
                table: "Employees",
                column: "EmployeeNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_SocialSecurityNumber",
                table: "Employees",
                column: "SocialSecurityNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhoneNumbers_EmployeeCtrlNbr",
                table: "PhoneNumbers",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_RailroadPoolEmployees_RailroadPoolCtrlNbr",
                table: "RailroadPoolEmployees",
                column: "RailroadPoolCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_RailroadPoolPayrollTiers_RailroadPoolCtrlNbr",
                table: "RailroadPoolPayrollTiers",
                column: "RailroadPoolCtrlNbr");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Addresses");

            migrationBuilder.DropTable(
                name: "AddressTypes");

            migrationBuilder.DropTable(
                name: "Crafts");

            migrationBuilder.DropTable(
                name: "EmailAddresses");

            migrationBuilder.DropTable(
                name: "EmailAddressTypes");

            migrationBuilder.DropTable(
                name: "EmployeePriorServiceCredits");

            migrationBuilder.DropTable(
                name: "EmploymentStatuses");

            migrationBuilder.DropTable(
                name: "EmploymentStatusHistory");

            migrationBuilder.DropTable(
                name: "PhoneNumbers");

            migrationBuilder.DropTable(
                name: "PhoneNumberTypes");

            migrationBuilder.DropTable(
                name: "RailroadPoolEmployees");

            migrationBuilder.DropTable(
                name: "RailroadPoolPayrollTiers");

            migrationBuilder.DropTable(
                name: "Rosters");

            migrationBuilder.DropTable(
                name: "Seniority");

            migrationBuilder.DropTable(
                name: "SeniorityStates");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "RailroadPools");
        }
    }
}
