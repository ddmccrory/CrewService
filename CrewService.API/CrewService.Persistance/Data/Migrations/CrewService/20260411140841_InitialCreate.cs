using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AbolishmentRecords",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    TargetCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    AbolishmentType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    RestoredDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_AbolishmentRecords", x => x.CtrlNbr);
                });

            migrationBuilder.CreateTable(
                name: "AbsenceCodes",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsExcused = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsCompensated = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSystemOnly = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsHolidayExempt = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultAutoMarkUpHours = table.Column<decimal>(type: "TEXT", precision: 6, scale: 2, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_AbsenceCodes", x => x.CtrlNbr);
                });

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
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddressTypes", x => x.CtrlNbr);
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
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailAddressTypes", x => x.CtrlNbr);
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
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmploymentStatuses", x => x.CtrlNbr);
                });

            migrationBuilder.CreateTable(
                name: "Features",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Route = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("PK_Features", x => x.CtrlNbr);
                });

            migrationBuilder.CreateTable(
                name: "GroupTypes",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsWorkArea = table.Column<bool>(type: "INTEGER", nullable: false),
                    FlagsJson = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ParentCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    RailroadCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    ParentGroupTypeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
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
                    table.PrimaryKey("PK_GroupTypes", x => x.CtrlNbr);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    MessageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    AggregateType = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    AggregateId = table.Column<long>(type: "INTEGER", nullable: false),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    OrchestrationId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    EventVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Retries = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.MessageId);
                });

            migrationBuilder.CreateTable(
                name: "Parents",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
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
                    table.PrimaryKey("PK_Parents", x => x.CtrlNbr);
                });

            migrationBuilder.CreateTable(
                name: "PayrollRuns",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    PayPeriod = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CalculatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LockedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_PayrollRuns", x => x.CtrlNbr);
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
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneNumberTypes", x => x.CtrlNbr);
                });

            migrationBuilder.CreateTable(
                name: "ProcessingLocks",
                columns: table => new
                {
                    LockKey = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AcquiredByInstance = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AcquiredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessingLocks", x => x.LockKey);
                });

            migrationBuilder.CreateTable(
                name: "RegulatoryQualifications",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CfrPart = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RequiresCertification = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecertificationIntervalMonths = table.Column<int>(type: "INTEGER", nullable: true),
                    EffectiveDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
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
                    table.PrimaryKey("PK_RegulatoryQualifications", x => x.CtrlNbr);
                });

            migrationBuilder.CreateTable(
                name: "RegulatoryStandards",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    MaxOnDutyMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    MinRestMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    Min8hRestInPreceding24h = table.Column<bool>(type: "INTEGER", nullable: false),
                    ConsecutiveDayLimit6 = table.Column<int>(type: "INTEGER", nullable: false),
                    ConsecutiveDayLimit7 = table.Column<int>(type: "INTEGER", nullable: false),
                    RestAfter6DaysMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    RestAfter7DaysMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    MonthlyCapMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    DeadheadAfter12hMonthlyCapMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    WreckReliefExtraMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
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
                    table.PrimaryKey("PK_RegulatoryStandards", x => x.CtrlNbr);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsSystem = table.Column<bool>(type: "INTEGER", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_Roles", x => x.CtrlNbr);
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
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeniorityStates", x => x.CtrlNbr);
                });

            migrationBuilder.CreateTable(
                name: "StaffablePositions",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    PositionType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_StaffablePositions", x => x.CtrlNbr);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ClientCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    EmployeeNumber = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
                    SocialSecurityNumber = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    DriversLicenseNumber = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    IssuingState = table.Column<string>(type: "TEXT", maxLength: 2, nullable: true),
                    Gender = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Race = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    MaritalStatus = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
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
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_Employees_EmploymentStatuses_EmploymentStatusCtrlNbr",
                        column: x => x.EmploymentStatusCtrlNbr,
                        principalTable: "EmploymentStatuses",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DynamicGroups",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    GroupTypeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ParentGroupCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    Path = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    IsWorkArea = table.Column<bool>(type: "INTEGER", nullable: false),
                    ParentCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    RailroadCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
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
                    table.PrimaryKey("PK_DynamicGroups", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_DynamicGroups_DynamicGroups_ParentGroupCtrlNbr",
                        column: x => x.ParentGroupCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DynamicGroups_GroupTypes_GroupTypeCtrlNbr",
                        column: x => x.GroupTypeCtrlNbr,
                        principalTable: "GroupTypes",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GroupAttributeDefinitions",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    GroupTypeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    AttributeName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DataType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultValue = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_GroupAttributeDefinitions", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_GroupAttributeDefinitions_GroupTypes_GroupTypeCtrlNbr",
                        column: x => x.GroupTypeCtrlNbr,
                        principalTable: "GroupTypes",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollExportBatches",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    PayrollRunCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ExportFormat = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RecordCount = table.Column<int>(type: "INTEGER", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_PayrollExportBatches", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_PayrollExportBatches_PayrollRuns_PayrollRunCtrlNbr",
                        column: x => x.PayrollRunCtrlNbr,
                        principalTable: "PayrollRuns",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
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
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Addresses", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_Addresses_AddressTypes_AddressTypeCtrlNbr",
                        column: x => x.AddressTypeCtrlNbr,
                        principalTable: "AddressTypes",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Addresses_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompensationBalances",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CompensationType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    BalanceHours = table.Column<decimal>(type: "TEXT", precision: 8, scale: 2, nullable: false),
                    AsOfUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
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
                    table.PrimaryKey("PK_CompensationBalances", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_CompensationBalances_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DrugAlcoholTestRecords",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    TestType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TestDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AlcoholResult = table.Column<decimal>(type: "TEXT", precision: 4, scale: 3, nullable: true),
                    DrugResult = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    SubstancesDetected = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsViolation = table.Column<bool>(type: "INTEGER", nullable: false),
                    FederalAuthority = table.Column<bool>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_DrugAlcoholTestRecords", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_DrugAlcoholTestRecords_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
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
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailAddresses", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_EmailAddresses_EmailAddressTypes_EmailTypeCtrlNbr",
                        column: x => x.EmailTypeCtrlNbr,
                        principalTable: "EmailAddressTypes",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmailAddresses_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeCertifications",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    RegulatoryQualificationCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CertificationType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CertificationDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ExpirationDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CertificationNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    SuspendedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SuspensionReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RevocationPeriodEndUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastMonitoringObservationUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastComplianceTestUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_EmployeeCertifications", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_EmployeeCertifications_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeCertifications_RegulatoryQualifications_RegulatoryQualificationCtrlNbr",
                        column: x => x.RegulatoryQualificationCtrlNbr,
                        principalTable: "RegulatoryQualifications",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
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
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeePriorServiceCredits", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_EmployeePriorServiceCredits_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
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
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmploymentStatusHistory", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_EmploymentStatusHistory_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmploymentStatusHistory_EmploymentStatuses_EmploymentStatusCtrlNbr",
                        column: x => x.EmploymentStatusCtrlNbr,
                        principalTable: "EmploymentStatuses",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FraDutyTours",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    RegulatoryStandardCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    DutyTourStartUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DutyTourEndUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TotalTimeOnDutyMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    ExcessMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    ExcessServiceReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PriorTimeOffMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    EmployeeReportedPriorTimeOffMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    PriorTimeOffReconciled = table.Column<bool>(type: "INTEGER", nullable: false),
                    ConsecutiveDays = table.Column<int>(type: "INTEGER", nullable: false),
                    IsQuickTieUp = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsCertified = table.Column<bool>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_FraDutyTours", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_FraDutyTours_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FraDutyTours_RegulatoryStandards_RegulatoryStandardCtrlNbr",
                        column: x => x.RegulatoryStandardCtrlNbr,
                        principalTable: "RegulatoryStandards",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FraMonthlyAccumulators",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    YearMonth = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                    CoveredServiceMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    DeadheadToReleaseMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    OtherServiceMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    DeadheadAfter12hMinutes = table.Column<int>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_FraMonthlyAccumulators", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_FraMonthlyAccumulators_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
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
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
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
                    table.ForeignKey(
                        name: "FK_PhoneNumbers_PhoneNumberTypes_PhoneTypeCtrlNbr",
                        column: x => x.PhoneTypeCtrlNbr,
                        principalTable: "PhoneNumberTypes",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PositionAssignments",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    StaffablePositionCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    AssignmentType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    AssignmentSourceCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    AssignedDateUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
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
                    table.PrimaryKey("PK_PositionAssignments", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_PositionAssignments_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PositionAssignments_StaffablePositions_StaffablePositionCtrlNbr",
                        column: x => x.StaffablePositionCtrlNbr,
                        principalTable: "StaffablePositions",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TimeEntries",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    DateUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EntryType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Hours = table.Column<decimal>(type: "TEXT", precision: 8, scale: 2, nullable: false),
                    ReasonCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    IsAdjustment = table.Column<bool>(type: "INTEGER", nullable: false),
                    OriginalEntryCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
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
                    table.PrimaryKey("PK_TimeEntries", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_TimeEntries_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimeEntries_TimeEntries_OriginalEntryCtrlNbr",
                        column: x => x.OriginalEntryCtrlNbr,
                        principalTable: "TimeEntries",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "VoluntaryReferrals",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ReferralDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SapEvaluationDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TreatmentCompletedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReturnToDutyTestDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReturnToDutyResult = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    FollowUpTestsRequired = table.Column<int>(type: "INTEGER", nullable: false),
                    FollowUpEndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_VoluntaryReferrals", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_VoluntaryReferrals_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChangeNotifications",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    WorkAreaGroupCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ChangeType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
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
                    table.PrimaryKey("PK_ChangeNotifications", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_ChangeNotifications_DynamicGroups_WorkAreaGroupCtrlNbr",
                        column: x => x.WorkAreaGroupCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DailyEmployeeStatusRecords",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    WorkAreaGroupCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    RecordDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    StatusCode = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    SnapshotJson = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
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
                    table.PrimaryKey("PK_DailyEmployeeStatusRecords", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_DailyEmployeeStatusRecords_DynamicGroups_WorkAreaGroupCtrlNbr",
                        column: x => x.WorkAreaGroupCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DailyEmployeeStatusRecords_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Department",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ParentCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    DynamicGroupCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DefaultCallSheetView = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "Vertical"),
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
                    table.PrimaryKey("PK_Department", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_Department_DynamicGroups_DynamicGroupCtrlNbr",
                        column: x => x.DynamicGroupCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EarningCodeRules",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    WorkAreaGroupCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    ConditionsJson = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    ResultCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    RequiresApproval = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_EarningCodeRules", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_EarningCodeRules_DynamicGroups_WorkAreaGroupCtrlNbr",
                        column: x => x.WorkAreaGroupCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Holidays",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    WorkAreaGroupCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ObservedDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_Holidays", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_Holidays_DynamicGroups_WorkAreaGroupCtrlNbr",
                        column: x => x.WorkAreaGroupCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Invitations",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ParentCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    Role = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    InvitedByUserId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Token = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SupersededAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RailroadCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_Invitations", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_Invitations_DynamicGroups_RailroadCtrlNbr",
                        column: x => x.RailroadCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invitations_Parents_ParentCtrlNbr",
                        column: x => x.ParentCtrlNbr,
                        principalTable: "Parents",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationProviderConfigs",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    WorkAreaGroupCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ProviderType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ConfigJson = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    PollingIntervalSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    PollingTimeoutMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    BatchSize = table.Column<int>(type: "INTEGER", nullable: false),
                    BatchPauseSeconds = table.Column<int>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_NotificationProviderConfigs", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_NotificationProviderConfigs_DynamicGroups_WorkAreaGroupCtrlNbr",
                        column: x => x.WorkAreaGroupCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollTiers",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    DynamicGroupCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    NumberOfDays = table.Column<int>(type: "INTEGER", nullable: false),
                    TypeOfDay = table.Column<int>(type: "INTEGER", nullable: false),
                    RatePercentage = table.Column<int>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_PayrollTiers", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_PayrollTiers_DynamicGroups_DynamicGroupCtrlNbr",
                        column: x => x.DynamicGroupCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RailroadHolidaySelections",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    WorkAreaGroupCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    HolidayCode = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_RailroadHolidaySelections", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_RailroadHolidaySelections_DynamicGroups_WorkAreaGroupCtrlNbr",
                        column: x => x.WorkAreaGroupCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RailroadInformations",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    WorkAreaGroupCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    InformationType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ClosedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_RailroadInformations", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_RailroadInformations_DynamicGroups_WorkAreaGroupCtrlNbr",
                        column: x => x.WorkAreaGroupCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SafetyCategories",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    WorkAreaGroupCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_SafetyCategories", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_SafetyCategories_DynamicGroups_WorkAreaGroupCtrlNbr",
                        column: x => x.WorkAreaGroupCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SafetyObservations",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    WorkAreaGroupCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ObserverEmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CategoryCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    AreaCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SubdivisionCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    ObservedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_SafetyObservations", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_SafetyObservations_DynamicGroups_WorkAreaGroupCtrlNbr",
                        column: x => x.WorkAreaGroupCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SafetyObservations_Employees_ObserverEmployeeCtrlNbr",
                        column: x => x.ObserverEmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShiftDefinitions",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    WorkAreaGroupCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ShiftCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_ShiftDefinitions", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_ShiftDefinitions_DynamicGroups_WorkAreaGroupCtrlNbr",
                        column: x => x.WorkAreaGroupCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeamsWebhookConfigs",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    RailroadCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    WorkAreaGroupCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    Channel = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    WebhookUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_TeamsWebhookConfigs", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_TeamsWebhookConfigs_DynamicGroups_RailroadCtrlNbr",
                        column: x => x.RailroadCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamsWebhookConfigs_DynamicGroups_WorkAreaGroupCtrlNbr",
                        column: x => x.WorkAreaGroupCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserParentAssignments",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ParentCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RailroadCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
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
                    table.PrimaryKey("PK_UserParentAssignments", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_UserParentAssignments_DynamicGroups_RailroadCtrlNbr",
                        column: x => x.RailroadCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserParentAssignments_Parents_ParentCtrlNbr",
                        column: x => x.ParentCtrlNbr,
                        principalTable: "Parents",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkerSchedules",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    WorkAreaGroupCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    WorkerType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CronExpression = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    NextFireUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastRunUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastRunStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
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
                    table.PrimaryKey("PK_WorkerSchedules", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_WorkerSchedules_DynamicGroups_WorkAreaGroupCtrlNbr",
                        column: x => x.WorkAreaGroupCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkInstances",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    AssignmentGroupCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    WorkAreaGroupCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CallTimeUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
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
                    table.PrimaryKey("PK_WorkInstances", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_WorkInstances_DynamicGroups_AssignmentGroupCtrlNbr",
                        column: x => x.AssignmentGroupCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkInstances_DynamicGroups_WorkAreaGroupCtrlNbr",
                        column: x => x.WorkAreaGroupCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GroupAttributeValues",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    GroupCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    AttributeDefinitionCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_GroupAttributeValues", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_GroupAttributeValues_DynamicGroups_GroupCtrlNbr",
                        column: x => x.GroupCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroupAttributeValues_GroupAttributeDefinitions_AttributeDefinitionCtrlNbr",
                        column: x => x.AttributeDefinitionCtrlNbr,
                        principalTable: "GroupAttributeDefinitions",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DrugAlcoholActions",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    TestRecordCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ActionType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ActionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_DrugAlcoholActions", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_DrugAlcoholActions_DrugAlcoholTestRecords_TestRecordCtrlNbr",
                        column: x => x.TestRecordCtrlNbr,
                        principalTable: "DrugAlcoholTestRecords",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DrugAlcoholActions_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CertificationEligibilityChecks",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCertificationCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CheckType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    EvaluationDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    StalenessLimitDays = table.Column<int>(type: "INTEGER", nullable: false),
                    ExpiresAtDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Result = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    EvaluatorName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_CertificationEligibilityChecks", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_CertificationEligibilityChecks_EmployeeCertifications_EmployeeCertificationCtrlNbr",
                        column: x => x.EmployeeCertificationCtrlNbr,
                        principalTable: "EmployeeCertifications",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CertificationRevocationRecords",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCertificationCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ViolationType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ViolationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SuspendedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    WrittenNoticeAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    HearingScheduledUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    HearingHeldUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PresidingOfficerCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    Decision = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    DecisionDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RevocationPeriodMonths = table.Column<int>(type: "INTEGER", nullable: true),
                    RevocationEndsUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    HearingRecordRetainUntil = table.Column<DateOnly>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_CertificationRevocationRecords", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_CertificationRevocationRecords_EmployeeCertifications_EmployeeCertificationCtrlNbr",
                        column: x => x.EmployeeCertificationCtrlNbr,
                        principalTable: "EmployeeCertifications",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CertificationRevocationRecords_Employees_PresidingOfficerCtrlNbr",
                        column: x => x.PresidingOfficerCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FraExcessServiceReports",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    DutyTourCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ViolationType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DetectedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExplanationText = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ReportedToFra = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReportedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_FraExcessServiceReports", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_FraExcessServiceReports_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FraExcessServiceReports_FraDutyTours_DutyTourCtrlNbr",
                        column: x => x.DutyTourCtrlNbr,
                        principalTable: "FraDutyTours",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FraOtherServiceSegments",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    DutyTourCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ServiceTypeCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    StartLocationCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    StartUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndLocationCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    EndUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsCommingled = table.Column<bool>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_FraOtherServiceSegments", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_FraOtherServiceSegments_FraDutyTours_DutyTourCtrlNbr",
                        column: x => x.DutyTourCtrlNbr,
                        principalTable: "FraDutyTours",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FraTransportationSegments",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    DutyTourCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    StartLocationCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    StartUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndLocationCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    EndUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TransportMode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsToAssignment = table.Column<bool>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_FraTransportationSegments", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_FraTransportationSegments_FraDutyTours_DutyTourCtrlNbr",
                        column: x => x.DutyTourCtrlNbr,
                        principalTable: "FraDutyTours",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Assignments",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    GroupCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    DepartmentCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsExtra = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_Assignments", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_Assignments_Department_DepartmentCtrlNbr",
                        column: x => x.DepartmentCtrlNbr,
                        principalTable: "Department",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assignments_DynamicGroups_GroupCtrlNbr",
                        column: x => x.GroupCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Crafts",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ParentCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    DynamicGroupCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
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
                    RegulatoryStandardCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    ProcessPayroll = table.Column<bool>(type: "INTEGER", nullable: false),
                    ShowNotifications = table.Column<bool>(type: "INTEGER", nullable: false),
                    VacationAssignmentType = table.Column<int>(type: "INTEGER", nullable: false),
                    DepartmentCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
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
                    table.PrimaryKey("PK_Crafts", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_Crafts_Department_DepartmentCtrlNbr",
                        column: x => x.DepartmentCtrlNbr,
                        principalTable: "Department",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Crafts_DynamicGroups_DynamicGroupCtrlNbr",
                        column: x => x.DynamicGroupCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Crafts_RegulatoryStandards_RegulatoryStandardCtrlNbr",
                        column: x => x.RegulatoryStandardCtrlNbr,
                        principalTable: "RegulatoryStandards",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Crews",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CrewType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    WorkAreaCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    DepartmentCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AbolishedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_Crews", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_Crews_Department_DepartmentCtrlNbr",
                        column: x => x.DepartmentCtrlNbr,
                        principalTable: "Department",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Crews_DynamicGroups_WorkAreaCtrlNbr",
                        column: x => x.WorkAreaCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RailroadInformationReadReceipts",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    InformationCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
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
                    table.PrimaryKey("PK_RailroadInformationReadReceipts", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_RailroadInformationReadReceipts_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RailroadInformationReadReceipts_RailroadInformations_InformationCtrlNbr",
                        column: x => x.InformationCtrlNbr,
                        principalTable: "RailroadInformations",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SafetyObservationActions",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ObservationCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ActionDescription = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    TakenByCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    TakenAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
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
                    table.PrimaryKey("PK_SafetyObservationActions", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_SafetyObservationActions_Employees_TakenByCtrlNbr",
                        column: x => x.TakenByCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SafetyObservationActions_SafetyObservations_ObservationCtrlNbr",
                        column: x => x.ObservationCtrlNbr,
                        principalTable: "SafetyObservations",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SafetyObservationResolutions",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ObservationCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ResolutionDescription = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    ResolvedByCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
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
                    table.PrimaryKey("PK_SafetyObservationResolutions", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_SafetyObservationResolutions_Employees_ResolvedByCtrlNbr",
                        column: x => x.ResolvedByCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SafetyObservationResolutions_SafetyObservations_ObservationCtrlNbr",
                        column: x => x.ObservationCtrlNbr,
                        principalTable: "SafetyObservations",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkerExecutionLogs",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    WorkerScheduleCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_WorkerExecutionLogs", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_WorkerExecutionLogs_WorkerSchedules_WorkerScheduleCtrlNbr",
                        column: x => x.WorkerScheduleCtrlNbr,
                        principalTable: "WorkerSchedules",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShiftInstances",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    WorkInstanceCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ShiftDefinitionCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ShiftCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ShiftDisplayName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DepartmentCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    DepartmentName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IsComplete = table.Column<bool>(type: "INTEGER", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_ShiftInstances", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_ShiftInstances_WorkInstances_WorkInstanceCtrlNbr",
                        column: x => x.WorkInstanceCtrlNbr,
                        principalTable: "WorkInstances",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssignmentSchedules",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    AssignmentCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ShiftDefinitionCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    OperatingDaysMask = table.Column<int>(type: "INTEGER", nullable: false),
                    OnDutyTime = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    OffDutyTime = table.Column<TimeOnly>(type: "TEXT", nullable: false),
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
                    table.PrimaryKey("PK_AssignmentSchedules", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_AssignmentSchedules_Assignments_AssignmentCtrlNbr",
                        column: x => x.AssignmentCtrlNbr,
                        principalTable: "Assignments",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssignmentSchedules_ShiftDefinitions_ShiftDefinitionCtrlNbr",
                        column: x => x.ShiftDefinitionCtrlNbr,
                        principalTable: "ShiftDefinitions",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AbsenceCodeCraftOverrides",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    AbsenceCodeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CraftCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    OverrideAutoMarkUpHours = table.Column<decimal>(type: "TEXT", precision: 6, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_AbsenceCodeCraftOverrides", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_AbsenceCodeCraftOverrides_AbsenceCodes_AbsenceCodeCtrlNbr",
                        column: x => x.AbsenceCodeCtrlNbr,
                        principalTable: "AbsenceCodes",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AbsenceCodeCraftOverrides_Crafts_CraftCtrlNbr",
                        column: x => x.CraftCtrlNbr,
                        principalTable: "Crafts",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BoardCascadePolicies",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    WorkAreaGroupCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CraftCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CascadeMode = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    MaxLevels = table.Column<int>(type: "INTEGER", nullable: true),
                    AuxEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AuxMaxLevels = table.Column<int>(type: "INTEGER", nullable: true),
                    SelectionStrategy = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
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
                    table.PrimaryKey("PK_BoardCascadePolicies", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_BoardCascadePolicies_Crafts_CraftCtrlNbr",
                        column: x => x.CraftCtrlNbr,
                        principalTable: "Crafts",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BoardCascadePolicies_DynamicGroups_WorkAreaGroupCtrlNbr",
                        column: x => x.WorkAreaGroupCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BulletinPolicies",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CraftCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    BidWindowHours = table.Column<int>(type: "INTEGER", nullable: false),
                    ForcedAssignmentEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    ForcedAssignmentBasis = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
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
                    table.PrimaryKey("PK_BulletinPolicies", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_BulletinPolicies_Crafts_CraftCtrlNbr",
                        column: x => x.CraftCtrlNbr,
                        principalTable: "Crafts",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CraftDisplacementPolicies",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CraftCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    WindowHours = table.Column<int>(type: "INTEGER", nullable: false),
                    SeniorityBasis = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DefaultAction = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    EligibilitySelectorJson = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
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
                    table.PrimaryKey("PK_CraftDisplacementPolicies", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_CraftDisplacementPolicies_Crafts_CraftCtrlNbr",
                        column: x => x.CraftCtrlNbr,
                        principalTable: "Crafts",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CraftOperationsPolicies",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CraftCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    LateCallThresholdMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    RestCalculationStrategy = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FixedRestHours = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                    ConsecutiveDayResetHours = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    DeleteConflictingNextShift = table.Column<bool>(type: "INTEGER", nullable: false),
                    AutoAnnulCreatesOffDuty = table.Column<bool>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_CraftOperationsPolicies", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_CraftOperationsPolicies_Crafts_CraftCtrlNbr",
                        column: x => x.CraftCtrlNbr,
                        principalTable: "Crafts",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CraftRegulatoryQualifications",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CraftCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    RegulatoryQualificationCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_CraftRegulatoryQualifications", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_CraftRegulatoryQualifications_Crafts_CraftCtrlNbr",
                        column: x => x.CraftCtrlNbr,
                        principalTable: "Crafts",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CraftRegulatoryQualifications_RegulatoryQualifications_RegulatoryQualificationCtrlNbr",
                        column: x => x.RegulatoryQualificationCtrlNbr,
                        principalTable: "RegulatoryQualifications",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CraftRoles",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CraftCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AlternateName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_CraftRoles", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_CraftRoles_Crafts_CraftCtrlNbr",
                        column: x => x.CraftCtrlNbr,
                        principalTable: "Crafts",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DisplacementCases",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CraftCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    OpenedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
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
                    table.PrimaryKey("PK_DisplacementCases", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_DisplacementCases_Crafts_CraftCtrlNbr",
                        column: x => x.CraftCtrlNbr,
                        principalTable: "Crafts",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DisplacementCases_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExtraBoards",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CraftCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    PlacedGroupCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    BoardKind = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    AuxBoardType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
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
                    table.PrimaryKey("PK_ExtraBoards", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_ExtraBoards_Crafts_CraftCtrlNbr",
                        column: x => x.CraftCtrlNbr,
                        principalTable: "Crafts",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExtraBoards_DynamicGroups_PlacedGroupCtrlNbr",
                        column: x => x.PlacedGroupCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HolidayQualificationRules",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    HolidayCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CraftCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    RequireWorkDayBefore = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequireWorkDayAfter = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExemptAbsenceCodes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_HolidayQualificationRules", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_HolidayQualificationRules_Crafts_CraftCtrlNbr",
                        column: x => x.CraftCtrlNbr,
                        principalTable: "Crafts",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HolidayQualificationRules_Holidays_HolidayCtrlNbr",
                        column: x => x.HolidayCtrlNbr,
                        principalTable: "Holidays",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    RoleCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    FeatureCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    AccessLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    ParentCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    CraftCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
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
                    table.PrimaryKey("PK_Permissions", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_Permissions_Crafts_CraftCtrlNbr",
                        column: x => x.CraftCtrlNbr,
                        principalTable: "Crafts",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Permissions_Features_FeatureCtrlNbr",
                        column: x => x.FeatureCtrlNbr,
                        principalTable: "Features",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Permissions_Roles_RoleCtrlNbr",
                        column: x => x.RoleCtrlNbr,
                        principalTable: "Roles",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PositionVacancies",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    TargetType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    TargetCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CraftCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    VacancyReasonCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PreviousIncumbentCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    OpenedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClosedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_PositionVacancies", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_PositionVacancies_Crafts_CraftCtrlNbr",
                        column: x => x.CraftCtrlNbr,
                        principalTable: "Crafts",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PositionVacancies_Employees_PreviousIncumbentCtrlNbr",
                        column: x => x.PreviousIncumbentCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RosterBoards",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    WorkAreaGroupCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CraftCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_RosterBoards", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_RosterBoards_Crafts_CraftCtrlNbr",
                        column: x => x.CraftCtrlNbr,
                        principalTable: "Crafts",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RosterBoards_DynamicGroups_WorkAreaGroupCtrlNbr",
                        column: x => x.WorkAreaGroupCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
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
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rosters", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_Rosters_Crafts_CraftCtrlNbr",
                        column: x => x.CraftCtrlNbr,
                        principalTable: "Crafts",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SeniorityMovePolicies",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CraftCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EligibilityDays = table.Column<int>(type: "INTEGER", nullable: false),
                    SeniorityBasis = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
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
                    table.PrimaryKey("PK_SeniorityMovePolicies", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_SeniorityMovePolicies_Crafts_CraftCtrlNbr",
                        column: x => x.CraftCtrlNbr,
                        principalTable: "Crafts",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SeniorityMoves",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CraftCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    TargetPositionCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    DisplacedEmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    ExercisedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DaysOnCurrentPosition = table.Column<int>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_SeniorityMoves", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_SeniorityMoves_Crafts_CraftCtrlNbr",
                        column: x => x.CraftCtrlNbr,
                        principalTable: "Crafts",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SeniorityMoves_Employees_DisplacedEmployeeCtrlNbr",
                        column: x => x.DisplacedEmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SeniorityMoves_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CrewAssignments",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CrewCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    AssignmentCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    DaysOfWeekMask = table.Column<int>(type: "INTEGER", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_CrewAssignments", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_CrewAssignments_Assignments_AssignmentCtrlNbr",
                        column: x => x.AssignmentCtrlNbr,
                        principalTable: "Assignments",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CrewAssignments_Crews_CrewCtrlNbr",
                        column: x => x.CrewCtrlNbr,
                        principalTable: "Crews",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CrewAttachmentInstances",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    WorkInstanceCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CrewCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_CrewAttachmentInstances", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_CrewAttachmentInstances_Crews_CrewCtrlNbr",
                        column: x => x.CrewCtrlNbr,
                        principalTable: "Crews",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CrewAttachmentInstances_WorkInstances_WorkInstanceCtrlNbr",
                        column: x => x.WorkInstanceCtrlNbr,
                        principalTable: "WorkInstances",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssignmentNote",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ShiftInstanceCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    AssignmentCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    NoteText = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
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
                    table.PrimaryKey("PK_AssignmentNote", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_AssignmentNote_ShiftInstances_ShiftInstanceCtrlNbr",
                        column: x => x.ShiftInstanceCtrlNbr,
                        principalTable: "ShiftInstances",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VacancyResolutionRuns",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    WorkAreaGroupCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ShiftInstanceCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SlotsEvaluated = table.Column<int>(type: "INTEGER", nullable: false),
                    SlotsFilled = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_VacancyResolutionRuns", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_VacancyResolutionRuns_DynamicGroups_WorkAreaGroupCtrlNbr",
                        column: x => x.WorkAreaGroupCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VacancyResolutionRuns_ShiftInstances_ShiftInstanceCtrlNbr",
                        column: x => x.ShiftInstanceCtrlNbr,
                        principalTable: "ShiftInstances",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CrewPositions",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CrewCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CraftRoleCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    StaffablePositionCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_CrewPositions", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_CrewPositions_CraftRoles_CraftRoleCtrlNbr",
                        column: x => x.CraftRoleCtrlNbr,
                        principalTable: "CraftRoles",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CrewPositions_Crews_CrewCtrlNbr",
                        column: x => x.CrewCtrlNbr,
                        principalTable: "Crews",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CrewPositions_StaffablePositions_StaffablePositionCtrlNbr",
                        column: x => x.StaffablePositionCtrlNbr,
                        principalTable: "StaffablePositions",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayRates",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CraftRoleCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    CraftCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    HourlyRate = table.Column<decimal>(type: "TEXT", precision: 10, scale: 4, nullable: false),
                    OvertimeMultiplier = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_PayRates", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_PayRates_CraftRoles_CraftRoleCtrlNbr",
                        column: x => x.CraftRoleCtrlNbr,
                        principalTable: "CraftRoles",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayRates_Crafts_CraftCtrlNbr",
                        column: x => x.CraftCtrlNbr,
                        principalTable: "Crafts",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PositionSlots",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    WorkInstanceCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CraftRoleCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    BoundEmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    BindingSource = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
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
                    table.PrimaryKey("PK_PositionSlots", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_PositionSlots_CraftRoles_CraftRoleCtrlNbr",
                        column: x => x.CraftRoleCtrlNbr,
                        principalTable: "CraftRoles",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PositionSlots_Employees_BoundEmployeeCtrlNbr",
                        column: x => x.BoundEmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PositionSlots_WorkInstances_WorkInstanceCtrlNbr",
                        column: x => x.WorkInstanceCtrlNbr,
                        principalTable: "WorkInstances",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DisplacementClaims",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CaseCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    TargetEmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    SubmittedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Decision = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    DecidedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_DisplacementClaims", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_DisplacementClaims_DisplacementCases_CaseCtrlNbr",
                        column: x => x.CaseCtrlNbr,
                        principalTable: "DisplacementCases",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DisplacementClaims_Employees_TargetEmployeeCtrlNbr",
                        column: x => x.TargetEmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BoardMembers",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ExtraBoardCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    StateJson = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    StartUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_BoardMembers", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_BoardMembers_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BoardMembers_ExtraBoards_ExtraBoardCtrlNbr",
                        column: x => x.ExtraBoardCtrlNbr,
                        principalTable: "ExtraBoards",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bulletins",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    PositionVacancyCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CraftCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    BidWindowOpensUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BidWindowClosesUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    AwardedEmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    AwardType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
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
                    table.PrimaryKey("PK_Bulletins", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_Bulletins_Crafts_CraftCtrlNbr",
                        column: x => x.CraftCtrlNbr,
                        principalTable: "Crafts",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bulletins_Employees_AwardedEmployeeCtrlNbr",
                        column: x => x.AwardedEmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bulletins_PositionVacancies_PositionVacancyCtrlNbr",
                        column: x => x.PositionVacancyCtrlNbr,
                        principalTable: "PositionVacancies",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RosterBoardPositions",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    RosterBoardCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    StaffablePositionCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    PositionOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    HangoutStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    HangoutAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_RosterBoardPositions", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_RosterBoardPositions_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RosterBoardPositions_RosterBoards_RosterBoardCtrlNbr",
                        column: x => x.RosterBoardCtrlNbr,
                        principalTable: "RosterBoards",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RosterBoardPositions_StaffablePositions_StaffablePositionCtrlNbr",
                        column: x => x.StaffablePositionCtrlNbr,
                        principalTable: "StaffablePositions",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Seniority",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    RosterCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    LastActiveRoster = table.Column<bool>(type: "INTEGER", nullable: false),
                    RosterDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Rank = table.Column<int>(type: "INTEGER", nullable: false),
                    StateID = table.Column<int>(type: "INTEGER", nullable: false),
                    CanTrain = table.Column<bool>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_Seniority", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_Seniority_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Seniority_Rosters_RosterCtrlNbr",
                        column: x => x.RosterCtrlNbr,
                        principalTable: "Rosters",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CrewIncumbencies",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CrewPositionCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_CrewIncumbencies", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_CrewIncumbencies_CrewPositions_CrewPositionCtrlNbr",
                        column: x => x.CrewPositionCtrlNbr,
                        principalTable: "CrewPositions",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CrewIncumbencies_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PositionSlotInstances",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ShiftInstanceCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CrewPositionCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    IncumbentEmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    AssignmentCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    AssignmentCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    AssignmentName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CraftRoleName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    GroupName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false, defaultValue: ""),
                    GroupCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: ""),
                    OnDutyTime = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    OffDutyTime = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IsIncumbent = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsAnnulled = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDoNotFill = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSkipped = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsAdHoc = table.Column<bool>(type: "INTEGER", nullable: false),
                    AnnulmentReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    AnnulmentDateTimeUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CrewName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, defaultValue: ""),
                    CrewType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: ""),
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
                    table.PrimaryKey("PK_PositionSlotInstances", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_PositionSlotInstances_CrewPositions_CrewPositionCtrlNbr",
                        column: x => x.CrewPositionCtrlNbr,
                        principalTable: "CrewPositions",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PositionSlotInstances_Employees_IncumbentEmployeeCtrlNbr",
                        column: x => x.IncumbentEmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PositionSlotInstances_ShiftInstances_ShiftInstanceCtrlNbr",
                        column: x => x.ShiftInstanceCtrlNbr,
                        principalTable: "ShiftInstances",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AbsenceRequests",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReasonCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ApprovedByCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    AbsenceCodeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    PositionSlotCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    MarkOffStartUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsSystemGenerated = table.Column<bool>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_AbsenceRequests", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_AbsenceRequests_AbsenceCodes_AbsenceCodeCtrlNbr",
                        column: x => x.AbsenceCodeCtrlNbr,
                        principalTable: "AbsenceCodes",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AbsenceRequests_Employees_ApprovedByCtrlNbr",
                        column: x => x.ApprovedByCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AbsenceRequests_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AbsenceRequests_PositionSlots_PositionSlotCtrlNbr",
                        column: x => x.PositionSlotCtrlNbr,
                        principalTable: "PositionSlots",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DispatchDecisionLogs",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    PositionSlotCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    AsOfUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Phase = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SelectedEmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    SelectionSource = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DecisionJson = table.Column<string>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_DispatchDecisionLogs", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_DispatchDecisionLogs_Employees_SelectedEmployeeCtrlNbr",
                        column: x => x.SelectedEmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchDecisionLogs_PositionSlots_PositionSlotCtrlNbr",
                        column: x => x.PositionSlotCtrlNbr,
                        principalTable: "PositionSlots",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DispatchOverrides",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    PositionSlotCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    OverrideType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ReasonCode = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ReasonText = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ApprovedByCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_DispatchOverrides", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_DispatchOverrides_Employees_ApprovedByCtrlNbr",
                        column: x => x.ApprovedByCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchOverrides_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchOverrides_PositionSlots_PositionSlotCtrlNbr",
                        column: x => x.PositionSlotCtrlNbr,
                        principalTable: "PositionSlots",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DispatchProjections",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    PositionSlotCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    AsOfUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProjectedEmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    TraceJson = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: true),
                    ComputedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
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
                    table.PrimaryKey("PK_DispatchProjections", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_DispatchProjections_Employees_ProjectedEmployeeCtrlNbr",
                        column: x => x.ProjectedEmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchProjections_PositionSlots_PositionSlotCtrlNbr",
                        column: x => x.PositionSlotCtrlNbr,
                        principalTable: "PositionSlots",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeBookings",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PositionSlotCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
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
                    table.PrimaryKey("PK_EmployeeBookings", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_EmployeeBookings_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeBookings_PositionSlots_PositionSlotCtrlNbr",
                        column: x => x.PositionSlotCtrlNbr,
                        principalTable: "PositionSlots",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationRequests",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    PositionSlotCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    TemplateType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
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
                    table.PrimaryKey("PK_NotificationRequests", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_NotificationRequests_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotificationRequests_PositionSlots_PositionSlotCtrlNbr",
                        column: x => x.PositionSlotCtrlNbr,
                        principalTable: "PositionSlots",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SlotRequirements",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    PositionSlotCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    CraftRoleCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    QualificationTypeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_SlotRequirements", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_SlotRequirements_CraftRoles_CraftRoleCtrlNbr",
                        column: x => x.CraftRoleCtrlNbr,
                        principalTable: "CraftRoles",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SlotRequirements_PositionSlots_PositionSlotCtrlNbr",
                        column: x => x.PositionSlotCtrlNbr,
                        principalTable: "PositionSlots",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SlotRequirements_RegulatoryQualifications_QualificationTypeCtrlNbr",
                        column: x => x.QualificationTypeCtrlNbr,
                        principalTable: "RegulatoryQualifications",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BulletinBids",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    BulletinCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    SubmittedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SeniorityRank = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_BulletinBids", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_BulletinBids_Bulletins_BulletinCtrlNbr",
                        column: x => x.BulletinCtrlNbr,
                        principalTable: "Bulletins",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BulletinBids_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AbsenceApprovals",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    AbsenceRequestCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ApprovalOfficerCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DecidedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_AbsenceApprovals", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_AbsenceApprovals_AbsenceRequests_AbsenceRequestCtrlNbr",
                        column: x => x.AbsenceRequestCtrlNbr,
                        principalTable: "AbsenceRequests",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AbsenceApprovals_Employees_ApprovalOfficerCtrlNbr",
                        column: x => x.ApprovalOfficerCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AbsenceMarkUps",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    AbsenceRequestCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ScheduledMarkUpUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ActualMarkUpUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsAutoMarkUp = table.Column<bool>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_AbsenceMarkUps", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_AbsenceMarkUps_AbsenceRequests_AbsenceRequestCtrlNbr",
                        column: x => x.AbsenceRequestCtrlNbr,
                        principalTable: "AbsenceRequests",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VacancyImpacts",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    AbsenceRequestCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    PositionSlotCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ImpactStartUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ImpactEndUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_VacancyImpacts", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_VacancyImpacts_AbsenceRequests_AbsenceRequestCtrlNbr",
                        column: x => x.AbsenceRequestCtrlNbr,
                        principalTable: "AbsenceRequests",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VacancyImpacts_PositionSlots_PositionSlotCtrlNbr",
                        column: x => x.PositionSlotCtrlNbr,
                        principalTable: "PositionSlots",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OnDutyRecords",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    PositionSlotCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    BookingCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    OnDutyTimeUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ScheduledOnDutyTimeUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsLateCall = table.Column<bool>(type: "INTEGER", nullable: false),
                    LateCallAdjustedTimeUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PreviousRestHours = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    ConsecutiveDays = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IsAssigned = table.Column<bool>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_OnDutyRecords", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_OnDutyRecords_EmployeeBookings_BookingCtrlNbr",
                        column: x => x.BookingCtrlNbr,
                        principalTable: "EmployeeBookings",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnDutyRecords_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OnDutyRecords_PositionSlots_PositionSlotCtrlNbr",
                        column: x => x.PositionSlotCtrlNbr,
                        principalTable: "PositionSlots",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationResponses",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    NotificationRequestCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ResponseType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DeviceType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
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
                    table.PrimaryKey("PK_NotificationResponses", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_NotificationResponses_NotificationRequests_NotificationRequestCtrlNbr",
                        column: x => x.NotificationRequestCtrlNbr,
                        principalTable: "NotificationRequests",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FraDutyTourSegments",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    DutyTourCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    OnDutyRecordCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    PositionDescription = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    StartLocationCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    StartUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndLocationCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    EndUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SegmentOrder = table.Column<int>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_FraDutyTourSegments", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_FraDutyTourSegments_FraDutyTours_DutyTourCtrlNbr",
                        column: x => x.DutyTourCtrlNbr,
                        principalTable: "FraDutyTours",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FraDutyTourSegments_OnDutyRecords_OnDutyRecordCtrlNbr",
                        column: x => x.OnDutyRecordCtrlNbr,
                        principalTable: "OnDutyRecords",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OffDutyRecords",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    OnDutyRecordCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    OffDutyTimeUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TotalTimeOnDutyMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    RestHoursRequired = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    RestedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ConsecutiveDayRestedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReleaseReason = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
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
                    table.PrimaryKey("PK_OffDutyRecords", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_OffDutyRecords_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OffDutyRecords_OnDutyRecords_OnDutyRecordCtrlNbr",
                        column: x => x.OnDutyRecordCtrlNbr,
                        principalTable: "OnDutyRecords",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OnDutyBillingRecords",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    OnDutyRecordCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    BillingType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    BillingCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    Hours = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
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
                    table.PrimaryKey("PK_OnDutyBillingRecords", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_OnDutyBillingRecords_OnDutyRecords_OnDutyRecordCtrlNbr",
                        column: x => x.OnDutyRecordCtrlNbr,
                        principalTable: "OnDutyRecords",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OnDutyLocomotiveRecords",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    OnDutyRecordCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    LocomotiveNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    LocomotiveTypeCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Hours = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_OnDutyLocomotiveRecords", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_OnDutyLocomotiveRecords_OnDutyRecords_OnDutyRecordCtrlNbr",
                        column: x => x.OnDutyRecordCtrlNbr,
                        principalTable: "OnDutyRecords",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OnDutyMaterialRecords",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    OnDutyRecordCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    MaterialCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CategoryCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    UnitCost = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_OnDutyMaterialRecords", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_OnDutyMaterialRecords_OnDutyRecords_OnDutyRecordCtrlNbr",
                        column: x => x.OnDutyRecordCtrlNbr,
                        principalTable: "OnDutyRecords",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayrollRecords",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    PayrollRunCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EarningsType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    Hours = table.Column<decimal>(type: "TEXT", precision: 8, scale: 2, nullable: false),
                    PolicyRef = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    OnDutyRecordCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    ResolvedEarningCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    RequiresApproval = table.Column<bool>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_PayrollRecords", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_PayrollRecords_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollRecords_OnDutyRecords_OnDutyRecordCtrlNbr",
                        column: x => x.OnDutyRecordCtrlNbr,
                        principalTable: "OnDutyRecords",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollRecords_PayrollRuns_PayrollRunCtrlNbr",
                        column: x => x.PayrollRunCtrlNbr,
                        principalTable: "PayrollRuns",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EarningApprovals",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    PayrollRecordCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ApprovalTier = table.Column<int>(type: "INTEGER", nullable: false),
                    OfficerCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DecidedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_EarningApprovals", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_EarningApprovals_Employees_OfficerCtrlNbr",
                        column: x => x.OfficerCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EarningApprovals_PayrollRecords_PayrollRecordCtrlNbr",
                        column: x => x.PayrollRecordCtrlNbr,
                        principalTable: "PayrollRecords",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HolidayPayrollRecords",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    HolidayCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    PayrollRecordCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    IsQualified = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisqualificationReason = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
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
                    table.PrimaryKey("PK_HolidayPayrollRecords", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_HolidayPayrollRecords_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HolidayPayrollRecords_Holidays_HolidayCtrlNbr",
                        column: x => x.HolidayCtrlNbr,
                        principalTable: "Holidays",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HolidayPayrollRecords_PayrollRecords_PayrollRecordCtrlNbr",
                        column: x => x.PayrollRecordCtrlNbr,
                        principalTable: "PayrollRecords",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollImportRecords",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    SourceFile = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    PayrollRecordCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    PaidAmount = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    ImportedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MatchStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_PayrollImportRecords", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_PayrollImportRecords_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollImportRecords_PayrollRecords_PayrollRecordCtrlNbr",
                        column: x => x.PayrollRecordCtrlNbr,
                        principalTable: "PayrollRecords",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceApprovals_AbsenceRequestCtrlNbr",
                table: "AbsenceApprovals",
                column: "AbsenceRequestCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceApprovals_ApprovalOfficerCtrlNbr",
                table: "AbsenceApprovals",
                column: "ApprovalOfficerCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceCodeCraftOverrides_AbsenceCodeCtrlNbr_CraftCtrlNbr",
                table: "AbsenceCodeCraftOverrides",
                columns: new[] { "AbsenceCodeCtrlNbr", "CraftCtrlNbr" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceCodeCraftOverrides_CraftCtrlNbr",
                table: "AbsenceCodeCraftOverrides",
                column: "CraftCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceCodes_Code",
                table: "AbsenceCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceMarkUps_AbsenceRequestCtrlNbr",
                table: "AbsenceMarkUps",
                column: "AbsenceRequestCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceRequests_AbsenceCodeCtrlNbr",
                table: "AbsenceRequests",
                column: "AbsenceCodeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceRequests_ApprovedByCtrlNbr",
                table: "AbsenceRequests",
                column: "ApprovedByCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceRequests_EmployeeCtrlNbr",
                table: "AbsenceRequests",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceRequests_PositionSlotCtrlNbr",
                table: "AbsenceRequests",
                column: "PositionSlotCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_AddressTypeCtrlNbr",
                table: "Addresses",
                column: "AddressTypeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_EmployeeCtrlNbr",
                table: "Addresses",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentNote_ShiftInstanceCtrlNbr_AssignmentCtrlNbr",
                table: "AssignmentNote",
                columns: new[] { "ShiftInstanceCtrlNbr", "AssignmentCtrlNbr" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_DepartmentCtrlNbr",
                table: "Assignments",
                column: "DepartmentCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_GroupCtrlNbr",
                table: "Assignments",
                column: "GroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentSchedules_AssignmentCtrlNbr",
                table: "AssignmentSchedules",
                column: "AssignmentCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentSchedules_ShiftDefinitionCtrlNbr",
                table: "AssignmentSchedules",
                column: "ShiftDefinitionCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_BoardCascadePolicies_CraftCtrlNbr",
                table: "BoardCascadePolicies",
                column: "CraftCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_BoardCascadePolicies_WorkAreaGroupCtrlNbr",
                table: "BoardCascadePolicies",
                column: "WorkAreaGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_BoardMembers_EmployeeCtrlNbr",
                table: "BoardMembers",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_BoardMembers_ExtraBoardCtrlNbr",
                table: "BoardMembers",
                column: "ExtraBoardCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_BulletinBids_BulletinCtrlNbr",
                table: "BulletinBids",
                column: "BulletinCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_BulletinBids_EmployeeCtrlNbr",
                table: "BulletinBids",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_BulletinPolicies_CraftCtrlNbr",
                table: "BulletinPolicies",
                column: "CraftCtrlNbr");

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
                name: "IX_CertificationEligibilityChecks_EmployeeCertificationCtrlNbr",
                table: "CertificationEligibilityChecks",
                column: "EmployeeCertificationCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_CertificationRevocationRecords_EmployeeCertificationCtrlNbr",
                table: "CertificationRevocationRecords",
                column: "EmployeeCertificationCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_CertificationRevocationRecords_PresidingOfficerCtrlNbr",
                table: "CertificationRevocationRecords",
                column: "PresidingOfficerCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeNotifications_WorkAreaGroupCtrlNbr",
                table: "ChangeNotifications",
                column: "WorkAreaGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_CompensationBalances_EmployeeCtrlNbr_CompensationType",
                table: "CompensationBalances",
                columns: new[] { "EmployeeCtrlNbr", "CompensationType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CraftDisplacementPolicies_CraftCtrlNbr",
                table: "CraftDisplacementPolicies",
                column: "CraftCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_CraftOperationsPolicies_CraftCtrlNbr",
                table: "CraftOperationsPolicies",
                column: "CraftCtrlNbr",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CraftRegulatoryQualifications_CraftCtrlNbr_RegulatoryQualificationCtrlNbr",
                table: "CraftRegulatoryQualifications",
                columns: new[] { "CraftCtrlNbr", "RegulatoryQualificationCtrlNbr" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CraftRegulatoryQualifications_RegulatoryQualificationCtrlNbr",
                table: "CraftRegulatoryQualifications",
                column: "RegulatoryQualificationCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_CraftRoles_CraftCtrlNbr",
                table: "CraftRoles",
                column: "CraftCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_Crafts_DepartmentCtrlNbr",
                table: "Crafts",
                column: "DepartmentCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_Crafts_DynamicGroupCtrlNbr",
                table: "Crafts",
                column: "DynamicGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_Crafts_RegulatoryStandardCtrlNbr",
                table: "Crafts",
                column: "RegulatoryStandardCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_CrewAssignments_AssignmentCtrlNbr",
                table: "CrewAssignments",
                column: "AssignmentCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_CrewAssignments_CrewCtrlNbr",
                table: "CrewAssignments",
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
                name: "IX_CrewIncumbencies_CrewPositionCtrlNbr",
                table: "CrewIncumbencies",
                column: "CrewPositionCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_CrewIncumbencies_EmployeeCtrlNbr",
                table: "CrewIncumbencies",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_CrewPositions_CraftRoleCtrlNbr",
                table: "CrewPositions",
                column: "CraftRoleCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_CrewPositions_CrewCtrlNbr",
                table: "CrewPositions",
                column: "CrewCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_CrewPositions_StaffablePositionCtrlNbr",
                table: "CrewPositions",
                column: "StaffablePositionCtrlNbr",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Crews_DepartmentCtrlNbr",
                table: "Crews",
                column: "DepartmentCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_Crews_WorkAreaCtrlNbr_Name",
                table: "Crews",
                columns: new[] { "WorkAreaCtrlNbr", "Name" },
                unique: true,
                filter: "IsDeleted = 0");

            migrationBuilder.CreateIndex(
                name: "IX_DailyEmployeeStatusRecords_EmployeeCtrlNbr_RecordDate",
                table: "DailyEmployeeStatusRecords",
                columns: new[] { "EmployeeCtrlNbr", "RecordDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DailyEmployeeStatusRecords_WorkAreaGroupCtrlNbr",
                table: "DailyEmployeeStatusRecords",
                column: "WorkAreaGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_Department_DynamicGroupCtrlNbr",
                table: "Department",
                column: "DynamicGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchDecisionLogs_PositionSlotCtrlNbr",
                table: "DispatchDecisionLogs",
                column: "PositionSlotCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchDecisionLogs_SelectedEmployeeCtrlNbr",
                table: "DispatchDecisionLogs",
                column: "SelectedEmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOverrides_ApprovedByCtrlNbr",
                table: "DispatchOverrides",
                column: "ApprovedByCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOverrides_EmployeeCtrlNbr",
                table: "DispatchOverrides",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOverrides_PositionSlotCtrlNbr",
                table: "DispatchOverrides",
                column: "PositionSlotCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchProjections_PositionSlotCtrlNbr",
                table: "DispatchProjections",
                column: "PositionSlotCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchProjections_ProjectedEmployeeCtrlNbr",
                table: "DispatchProjections",
                column: "ProjectedEmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_DisplacementCases_CraftCtrlNbr",
                table: "DisplacementCases",
                column: "CraftCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_DisplacementCases_EmployeeCtrlNbr",
                table: "DisplacementCases",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_DisplacementClaims_CaseCtrlNbr",
                table: "DisplacementClaims",
                column: "CaseCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_DisplacementClaims_TargetEmployeeCtrlNbr",
                table: "DisplacementClaims",
                column: "TargetEmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_DrugAlcoholActions_EmployeeCtrlNbr",
                table: "DrugAlcoholActions",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_DrugAlcoholActions_TestRecordCtrlNbr",
                table: "DrugAlcoholActions",
                column: "TestRecordCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_DrugAlcoholTestRecords_EmployeeCtrlNbr",
                table: "DrugAlcoholTestRecords",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_DynamicGroups_GroupTypeCtrlNbr",
                table: "DynamicGroups",
                column: "GroupTypeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_DynamicGroups_ParentGroupCtrlNbr_Name",
                table: "DynamicGroups",
                columns: new[] { "ParentGroupCtrlNbr", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_EarningApprovals_OfficerCtrlNbr",
                table: "EarningApprovals",
                column: "OfficerCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_EarningApprovals_PayrollRecordCtrlNbr",
                table: "EarningApprovals",
                column: "PayrollRecordCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_EarningCodeRules_WorkAreaGroupCtrlNbr",
                table: "EarningCodeRules",
                column: "WorkAreaGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAddresses_EmailTypeCtrlNbr",
                table: "EmailAddresses",
                column: "EmailTypeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAddresses_EmployeeCtrlNbr",
                table: "EmailAddresses",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeBookings_EmployeeCtrlNbr",
                table: "EmployeeBookings",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeBookings_PositionSlotCtrlNbr",
                table: "EmployeeBookings",
                column: "PositionSlotCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCertifications_EmployeeCtrlNbr",
                table: "EmployeeCertifications",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCertifications_RegulatoryQualificationCtrlNbr",
                table: "EmployeeCertifications",
                column: "RegulatoryQualificationCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePriorServiceCredits_EmployeeCtrlNbr",
                table: "EmployeePriorServiceCredits",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_EmployeeNumber",
                table: "Employees",
                column: "EmployeeNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_EmploymentStatusCtrlNbr",
                table: "Employees",
                column: "EmploymentStatusCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_SocialSecurityNumber",
                table: "Employees",
                column: "SocialSecurityNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentStatusHistory_EmployeeCtrlNbr",
                table: "EmploymentStatusHistory",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentStatusHistory_EmploymentStatusCtrlNbr",
                table: "EmploymentStatusHistory",
                column: "EmploymentStatusCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_ExtraBoards_CraftCtrlNbr",
                table: "ExtraBoards",
                column: "CraftCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_ExtraBoards_PlacedGroupCtrlNbr",
                table: "ExtraBoards",
                column: "PlacedGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_Features_Key",
                table: "Features",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FraDutyTours_EmployeeCtrlNbr",
                table: "FraDutyTours",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_FraDutyTours_RegulatoryStandardCtrlNbr",
                table: "FraDutyTours",
                column: "RegulatoryStandardCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_FraDutyTourSegments_DutyTourCtrlNbr",
                table: "FraDutyTourSegments",
                column: "DutyTourCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_FraDutyTourSegments_OnDutyRecordCtrlNbr",
                table: "FraDutyTourSegments",
                column: "OnDutyRecordCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_FraExcessServiceReports_DutyTourCtrlNbr",
                table: "FraExcessServiceReports",
                column: "DutyTourCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_FraExcessServiceReports_EmployeeCtrlNbr",
                table: "FraExcessServiceReports",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_FraMonthlyAccumulators_EmployeeCtrlNbr_YearMonth",
                table: "FraMonthlyAccumulators",
                columns: new[] { "EmployeeCtrlNbr", "YearMonth" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FraOtherServiceSegments_DutyTourCtrlNbr",
                table: "FraOtherServiceSegments",
                column: "DutyTourCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_FraTransportationSegments_DutyTourCtrlNbr",
                table: "FraTransportationSegments",
                column: "DutyTourCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_GroupAttributeDefinitions_GroupTypeCtrlNbr",
                table: "GroupAttributeDefinitions",
                column: "GroupTypeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_GroupAttributeValues_AttributeDefinitionCtrlNbr",
                table: "GroupAttributeValues",
                column: "AttributeDefinitionCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_GroupAttributeValues_GroupCtrlNbr",
                table: "GroupAttributeValues",
                column: "GroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_GroupTypes_Name_ParentCtrlNbr_RailroadCtrlNbr",
                table: "GroupTypes",
                columns: new[] { "Name", "ParentCtrlNbr", "RailroadCtrlNbr" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HolidayPayrollRecords_EmployeeCtrlNbr",
                table: "HolidayPayrollRecords",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_HolidayPayrollRecords_HolidayCtrlNbr",
                table: "HolidayPayrollRecords",
                column: "HolidayCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_HolidayPayrollRecords_PayrollRecordCtrlNbr",
                table: "HolidayPayrollRecords",
                column: "PayrollRecordCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_HolidayQualificationRules_CraftCtrlNbr",
                table: "HolidayQualificationRules",
                column: "CraftCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_HolidayQualificationRules_HolidayCtrlNbr",
                table: "HolidayQualificationRules",
                column: "HolidayCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_WorkAreaGroupCtrlNbr",
                table: "Holidays",
                column: "WorkAreaGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_Email",
                table: "Invitations",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_Email_ParentCtrlNbr_Status",
                table: "Invitations",
                columns: new[] { "Email", "ParentCtrlNbr", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_ParentCtrlNbr",
                table: "Invitations",
                column: "ParentCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_RailroadCtrlNbr",
                table: "Invitations",
                column: "RailroadCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_Token",
                table: "Invitations",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationProviderConfigs_WorkAreaGroupCtrlNbr",
                table: "NotificationProviderConfigs",
                column: "WorkAreaGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRequests_EmployeeCtrlNbr",
                table: "NotificationRequests",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRequests_PositionSlotCtrlNbr",
                table: "NotificationRequests",
                column: "PositionSlotCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationResponses_NotificationRequestCtrlNbr",
                table: "NotificationResponses",
                column: "NotificationRequestCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_OffDutyRecords_EmployeeCtrlNbr",
                table: "OffDutyRecords",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_OffDutyRecords_OnDutyRecordCtrlNbr",
                table: "OffDutyRecords",
                column: "OnDutyRecordCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_OnDutyBillingRecords_OnDutyRecordCtrlNbr",
                table: "OnDutyBillingRecords",
                column: "OnDutyRecordCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_OnDutyLocomotiveRecords_OnDutyRecordCtrlNbr",
                table: "OnDutyLocomotiveRecords",
                column: "OnDutyRecordCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_OnDutyMaterialRecords_OnDutyRecordCtrlNbr",
                table: "OnDutyMaterialRecords",
                column: "OnDutyRecordCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_OnDutyRecords_BookingCtrlNbr",
                table: "OnDutyRecords",
                column: "BookingCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_OnDutyRecords_EmployeeCtrlNbr",
                table: "OnDutyRecords",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_OnDutyRecords_PositionSlotCtrlNbr",
                table: "OnDutyRecords",
                column: "PositionSlotCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_CorrelationId",
                table: "OutboxMessages",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_IdempotencyKey",
                table: "OutboxMessages",
                column: "IdempotencyKey",
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_OrchestrationId",
                table: "OutboxMessages",
                column: "OrchestrationId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Status_CreatedAt",
                table: "OutboxMessages",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Parents_Name",
                table: "Parents",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayRates_CraftCtrlNbr",
                table: "PayRates",
                column: "CraftCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_PayRates_CraftRoleCtrlNbr",
                table: "PayRates",
                column: "CraftRoleCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollExportBatches_PayrollRunCtrlNbr",
                table: "PayrollExportBatches",
                column: "PayrollRunCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollImportRecords_EmployeeCtrlNbr",
                table: "PayrollImportRecords",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollImportRecords_PayrollRecordCtrlNbr",
                table: "PayrollImportRecords",
                column: "PayrollRecordCtrlNbr");

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
                name: "IX_PayrollRuns_PayPeriod",
                table: "PayrollRuns",
                column: "PayPeriod");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollTiers_DynamicGroupCtrlNbr",
                table: "PayrollTiers",
                column: "DynamicGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_CraftCtrlNbr",
                table: "Permissions",
                column: "CraftCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_FeatureCtrlNbr",
                table: "Permissions",
                column: "FeatureCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_RoleCtrlNbr_FeatureCtrlNbr_ParentCtrlNbr_CraftCtrlNbr",
                table: "Permissions",
                columns: new[] { "RoleCtrlNbr", "FeatureCtrlNbr", "ParentCtrlNbr", "CraftCtrlNbr" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhoneNumbers_EmployeeCtrlNbr",
                table: "PhoneNumbers",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneNumbers_PhoneTypeCtrlNbr",
                table: "PhoneNumbers",
                column: "PhoneTypeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_PositionAssignments_EmployeeCtrlNbr",
                table: "PositionAssignments",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_PositionAssignments_StaffablePositionCtrlNbr",
                table: "PositionAssignments",
                column: "StaffablePositionCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_PositionSlotInstances_CrewPositionCtrlNbr",
                table: "PositionSlotInstances",
                column: "CrewPositionCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_PositionSlotInstances_IncumbentEmployeeCtrlNbr",
                table: "PositionSlotInstances",
                column: "IncumbentEmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_PositionSlotInstances_ShiftInstanceCtrlNbr",
                table: "PositionSlotInstances",
                column: "ShiftInstanceCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_PositionSlots_BoundEmployeeCtrlNbr",
                table: "PositionSlots",
                column: "BoundEmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_PositionSlots_CraftRoleCtrlNbr",
                table: "PositionSlots",
                column: "CraftRoleCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_PositionSlots_WorkInstanceCtrlNbr",
                table: "PositionSlots",
                column: "WorkInstanceCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_PositionVacancies_CraftCtrlNbr",
                table: "PositionVacancies",
                column: "CraftCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_PositionVacancies_PreviousIncumbentCtrlNbr",
                table: "PositionVacancies",
                column: "PreviousIncumbentCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_RailroadHolidaySelections_WorkAreaGroupCtrlNbr_HolidayCode",
                table: "RailroadHolidaySelections",
                columns: new[] { "WorkAreaGroupCtrlNbr", "HolidayCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RailroadInformationReadReceipts_EmployeeCtrlNbr",
                table: "RailroadInformationReadReceipts",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_RailroadInformationReadReceipts_InformationCtrlNbr_EmployeeCtrlNbr",
                table: "RailroadInformationReadReceipts",
                columns: new[] { "InformationCtrlNbr", "EmployeeCtrlNbr" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RailroadInformations_WorkAreaGroupCtrlNbr",
                table: "RailroadInformations",
                column: "WorkAreaGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_RegulatoryQualifications_Code",
                table: "RegulatoryQualifications",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegulatoryStandards_Code",
                table: "RegulatoryStandards",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RosterBoardPositions_EmployeeCtrlNbr",
                table: "RosterBoardPositions",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_RosterBoardPositions_RosterBoardCtrlNbr",
                table: "RosterBoardPositions",
                column: "RosterBoardCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_RosterBoardPositions_StaffablePositionCtrlNbr",
                table: "RosterBoardPositions",
                column: "StaffablePositionCtrlNbr",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RosterBoards_CraftCtrlNbr",
                table: "RosterBoards",
                column: "CraftCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_RosterBoards_WorkAreaGroupCtrlNbr",
                table: "RosterBoards",
                column: "WorkAreaGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_Rosters_CraftCtrlNbr",
                table: "Rosters",
                column: "CraftCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_SafetyCategories_WorkAreaGroupCtrlNbr",
                table: "SafetyCategories",
                column: "WorkAreaGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_SafetyObservationActions_ObservationCtrlNbr",
                table: "SafetyObservationActions",
                column: "ObservationCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_SafetyObservationActions_TakenByCtrlNbr",
                table: "SafetyObservationActions",
                column: "TakenByCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_SafetyObservationResolutions_ObservationCtrlNbr",
                table: "SafetyObservationResolutions",
                column: "ObservationCtrlNbr",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SafetyObservationResolutions_ResolvedByCtrlNbr",
                table: "SafetyObservationResolutions",
                column: "ResolvedByCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_SafetyObservations_ObserverEmployeeCtrlNbr",
                table: "SafetyObservations",
                column: "ObserverEmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_SafetyObservations_WorkAreaGroupCtrlNbr",
                table: "SafetyObservations",
                column: "WorkAreaGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_Seniority_EmployeeCtrlNbr",
                table: "Seniority",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_Seniority_RosterCtrlNbr",
                table: "Seniority",
                column: "RosterCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_SeniorityMovePolicies_CraftCtrlNbr",
                table: "SeniorityMovePolicies",
                column: "CraftCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_SeniorityMoves_CraftCtrlNbr",
                table: "SeniorityMoves",
                column: "CraftCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_SeniorityMoves_DisplacedEmployeeCtrlNbr",
                table: "SeniorityMoves",
                column: "DisplacedEmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_SeniorityMoves_EmployeeCtrlNbr",
                table: "SeniorityMoves",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftDefinitions_WorkAreaGroupCtrlNbr",
                table: "ShiftDefinitions",
                column: "WorkAreaGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftInstances_WorkInstanceCtrlNbr",
                table: "ShiftInstances",
                column: "WorkInstanceCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_SlotRequirements_CraftRoleCtrlNbr",
                table: "SlotRequirements",
                column: "CraftRoleCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_SlotRequirements_PositionSlotCtrlNbr",
                table: "SlotRequirements",
                column: "PositionSlotCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_SlotRequirements_QualificationTypeCtrlNbr",
                table: "SlotRequirements",
                column: "QualificationTypeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_TeamsWebhookConfigs_RailroadCtrlNbr",
                table: "TeamsWebhookConfigs",
                column: "RailroadCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_TeamsWebhookConfigs_WorkAreaGroupCtrlNbr",
                table: "TeamsWebhookConfigs",
                column: "WorkAreaGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_EmployeeCtrlNbr",
                table: "TimeEntries",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_OriginalEntryCtrlNbr",
                table: "TimeEntries",
                column: "OriginalEntryCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_UserParentAssignments_ParentCtrlNbr",
                table: "UserParentAssignments",
                column: "ParentCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_UserParentAssignments_RailroadCtrlNbr",
                table: "UserParentAssignments",
                column: "RailroadCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_UserParentAssignments_UserId",
                table: "UserParentAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserParentAssignments_UserId_ParentCtrlNbr_RailroadCtrlNbr",
                table: "UserParentAssignments",
                columns: new[] { "UserId", "ParentCtrlNbr", "RailroadCtrlNbr" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VacancyImpacts_AbsenceRequestCtrlNbr",
                table: "VacancyImpacts",
                column: "AbsenceRequestCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_VacancyImpacts_PositionSlotCtrlNbr",
                table: "VacancyImpacts",
                column: "PositionSlotCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_VacancyResolutionRuns_ShiftInstanceCtrlNbr",
                table: "VacancyResolutionRuns",
                column: "ShiftInstanceCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_VacancyResolutionRuns_WorkAreaGroupCtrlNbr",
                table: "VacancyResolutionRuns",
                column: "WorkAreaGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_VoluntaryReferrals_EmployeeCtrlNbr",
                table: "VoluntaryReferrals",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerExecutionLogs_WorkerScheduleCtrlNbr",
                table: "WorkerExecutionLogs",
                column: "WorkerScheduleCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerSchedules_WorkAreaGroupCtrlNbr",
                table: "WorkerSchedules",
                column: "WorkAreaGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_WorkInstances_AssignmentGroupCtrlNbr",
                table: "WorkInstances",
                column: "AssignmentGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_WorkInstances_WorkAreaGroupCtrlNbr",
                table: "WorkInstances",
                column: "WorkAreaGroupCtrlNbr");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AbolishmentRecords");

            migrationBuilder.DropTable(
                name: "AbsenceApprovals");

            migrationBuilder.DropTable(
                name: "AbsenceCodeCraftOverrides");

            migrationBuilder.DropTable(
                name: "AbsenceMarkUps");

            migrationBuilder.DropTable(
                name: "Addresses");

            migrationBuilder.DropTable(
                name: "AssignmentNote");

            migrationBuilder.DropTable(
                name: "AssignmentSchedules");

            migrationBuilder.DropTable(
                name: "BoardCascadePolicies");

            migrationBuilder.DropTable(
                name: "BoardMembers");

            migrationBuilder.DropTable(
                name: "BulletinBids");

            migrationBuilder.DropTable(
                name: "BulletinPolicies");

            migrationBuilder.DropTable(
                name: "CertificationEligibilityChecks");

            migrationBuilder.DropTable(
                name: "CertificationRevocationRecords");

            migrationBuilder.DropTable(
                name: "ChangeNotifications");

            migrationBuilder.DropTable(
                name: "CompensationBalances");

            migrationBuilder.DropTable(
                name: "CraftDisplacementPolicies");

            migrationBuilder.DropTable(
                name: "CraftOperationsPolicies");

            migrationBuilder.DropTable(
                name: "CraftRegulatoryQualifications");

            migrationBuilder.DropTable(
                name: "CrewAssignments");

            migrationBuilder.DropTable(
                name: "CrewAttachmentInstances");

            migrationBuilder.DropTable(
                name: "CrewIncumbencies");

            migrationBuilder.DropTable(
                name: "DailyEmployeeStatusRecords");

            migrationBuilder.DropTable(
                name: "DispatchDecisionLogs");

            migrationBuilder.DropTable(
                name: "DispatchOverrides");

            migrationBuilder.DropTable(
                name: "DispatchProjections");

            migrationBuilder.DropTable(
                name: "DisplacementClaims");

            migrationBuilder.DropTable(
                name: "DrugAlcoholActions");

            migrationBuilder.DropTable(
                name: "EarningApprovals");

            migrationBuilder.DropTable(
                name: "EarningCodeRules");

            migrationBuilder.DropTable(
                name: "EmailAddresses");

            migrationBuilder.DropTable(
                name: "EmployeePriorServiceCredits");

            migrationBuilder.DropTable(
                name: "EmploymentStatusHistory");

            migrationBuilder.DropTable(
                name: "FraDutyTourSegments");

            migrationBuilder.DropTable(
                name: "FraExcessServiceReports");

            migrationBuilder.DropTable(
                name: "FraMonthlyAccumulators");

            migrationBuilder.DropTable(
                name: "FraOtherServiceSegments");

            migrationBuilder.DropTable(
                name: "FraTransportationSegments");

            migrationBuilder.DropTable(
                name: "GroupAttributeValues");

            migrationBuilder.DropTable(
                name: "HolidayPayrollRecords");

            migrationBuilder.DropTable(
                name: "HolidayQualificationRules");

            migrationBuilder.DropTable(
                name: "Invitations");

            migrationBuilder.DropTable(
                name: "NotificationProviderConfigs");

            migrationBuilder.DropTable(
                name: "NotificationResponses");

            migrationBuilder.DropTable(
                name: "OffDutyRecords");

            migrationBuilder.DropTable(
                name: "OnDutyBillingRecords");

            migrationBuilder.DropTable(
                name: "OnDutyLocomotiveRecords");

            migrationBuilder.DropTable(
                name: "OnDutyMaterialRecords");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "PayRates");

            migrationBuilder.DropTable(
                name: "PayrollExportBatches");

            migrationBuilder.DropTable(
                name: "PayrollImportRecords");

            migrationBuilder.DropTable(
                name: "PayrollTiers");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "PhoneNumbers");

            migrationBuilder.DropTable(
                name: "PositionAssignments");

            migrationBuilder.DropTable(
                name: "PositionSlotInstances");

            migrationBuilder.DropTable(
                name: "ProcessingLocks");

            migrationBuilder.DropTable(
                name: "RailroadHolidaySelections");

            migrationBuilder.DropTable(
                name: "RailroadInformationReadReceipts");

            migrationBuilder.DropTable(
                name: "RosterBoardPositions");

            migrationBuilder.DropTable(
                name: "SafetyCategories");

            migrationBuilder.DropTable(
                name: "SafetyObservationActions");

            migrationBuilder.DropTable(
                name: "SafetyObservationResolutions");

            migrationBuilder.DropTable(
                name: "Seniority");

            migrationBuilder.DropTable(
                name: "SeniorityMovePolicies");

            migrationBuilder.DropTable(
                name: "SeniorityMoves");

            migrationBuilder.DropTable(
                name: "SeniorityStates");

            migrationBuilder.DropTable(
                name: "SlotRequirements");

            migrationBuilder.DropTable(
                name: "TeamsWebhookConfigs");

            migrationBuilder.DropTable(
                name: "TimeEntries");

            migrationBuilder.DropTable(
                name: "UserParentAssignments");

            migrationBuilder.DropTable(
                name: "VacancyImpacts");

            migrationBuilder.DropTable(
                name: "VacancyResolutionRuns");

            migrationBuilder.DropTable(
                name: "VoluntaryReferrals");

            migrationBuilder.DropTable(
                name: "WorkerExecutionLogs");

            migrationBuilder.DropTable(
                name: "AddressTypes");

            migrationBuilder.DropTable(
                name: "ShiftDefinitions");

            migrationBuilder.DropTable(
                name: "ExtraBoards");

            migrationBuilder.DropTable(
                name: "Bulletins");

            migrationBuilder.DropTable(
                name: "EmployeeCertifications");

            migrationBuilder.DropTable(
                name: "Assignments");

            migrationBuilder.DropTable(
                name: "DisplacementCases");

            migrationBuilder.DropTable(
                name: "DrugAlcoholTestRecords");

            migrationBuilder.DropTable(
                name: "EmailAddressTypes");

            migrationBuilder.DropTable(
                name: "FraDutyTours");

            migrationBuilder.DropTable(
                name: "GroupAttributeDefinitions");

            migrationBuilder.DropTable(
                name: "Holidays");

            migrationBuilder.DropTable(
                name: "NotificationRequests");

            migrationBuilder.DropTable(
                name: "PayrollRecords");

            migrationBuilder.DropTable(
                name: "Features");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "PhoneNumberTypes");

            migrationBuilder.DropTable(
                name: "CrewPositions");

            migrationBuilder.DropTable(
                name: "RailroadInformations");

            migrationBuilder.DropTable(
                name: "RosterBoards");

            migrationBuilder.DropTable(
                name: "SafetyObservations");

            migrationBuilder.DropTable(
                name: "Rosters");

            migrationBuilder.DropTable(
                name: "Parents");

            migrationBuilder.DropTable(
                name: "AbsenceRequests");

            migrationBuilder.DropTable(
                name: "ShiftInstances");

            migrationBuilder.DropTable(
                name: "WorkerSchedules");

            migrationBuilder.DropTable(
                name: "PositionVacancies");

            migrationBuilder.DropTable(
                name: "RegulatoryQualifications");

            migrationBuilder.DropTable(
                name: "OnDutyRecords");

            migrationBuilder.DropTable(
                name: "PayrollRuns");

            migrationBuilder.DropTable(
                name: "Crews");

            migrationBuilder.DropTable(
                name: "StaffablePositions");

            migrationBuilder.DropTable(
                name: "AbsenceCodes");

            migrationBuilder.DropTable(
                name: "EmployeeBookings");

            migrationBuilder.DropTable(
                name: "PositionSlots");

            migrationBuilder.DropTable(
                name: "CraftRoles");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "WorkInstances");

            migrationBuilder.DropTable(
                name: "Crafts");

            migrationBuilder.DropTable(
                name: "EmploymentStatuses");

            migrationBuilder.DropTable(
                name: "Department");

            migrationBuilder.DropTable(
                name: "RegulatoryStandards");

            migrationBuilder.DropTable(
                name: "DynamicGroups");

            migrationBuilder.DropTable(
                name: "GroupTypes");
        }
    }
}
