using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class B15Qualifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QualificationTypes",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ParentCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ScopeGroupCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    CraftCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    EvaluationStrategy = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ExpirationMonths = table.Column<int>(type: "INTEGER", nullable: true),
                    CalendarYearExpiry = table.Column<bool>(type: "INTEGER", nullable: false),
                    GraceDays = table.Column<int>(type: "INTEGER", nullable: false),
                    RenewalLeadDays = table.Column<int>(type: "INTEGER", nullable: false),
                    IsBlocking = table.Column<bool>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_QualificationTypes", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_QualificationTypes_Crafts_CraftCtrlNbr",
                        column: x => x.CraftCtrlNbr,
                        principalTable: "Crafts",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualificationTypes_DynamicGroups_ScopeGroupCtrlNbr",
                        column: x => x.ScopeGroupCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualificationTypes_Parents_ParentCtrlNbr",
                        column: x => x.ParentCtrlNbr,
                        principalTable: "Parents",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeQualifications",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    QualificationTypeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    AchievedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    GrantedBy = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RevocationReason = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
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
                    table.PrimaryKey("PK_EmployeeQualifications", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_EmployeeQualifications_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeQualifications_QualificationTypes_QualificationTypeCtrlNbr",
                        column: x => x.QualificationTypeCtrlNbr,
                        principalTable: "QualificationTypes",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QualificationPrerequisites",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    QualificationTypeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    PrerequisiteKind = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Threshold = table.Column<int>(type: "INTEGER", nullable: false),
                    ThresholdUnit = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    EventSource = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    ActivityFilter = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    RequiredQualTypeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("PK_QualificationPrerequisites", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_QualificationPrerequisites_QualificationTypes_QualificationTypeCtrlNbr",
                        column: x => x.QualificationTypeCtrlNbr,
                        principalTable: "QualificationTypes",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QualificationPrerequisites_QualificationTypes_RequiredQualTypeCtrlNbr",
                        column: x => x.RequiredQualTypeCtrlNbr,
                        principalTable: "QualificationTypes",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QualificationEvidence",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeQualificationCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    PrerequisiteCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    EvidenceType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    EvidenceValue = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RecordedBy = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
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
                    table.PrimaryKey("PK_QualificationEvidence", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_QualificationEvidence_EmployeeQualifications_EmployeeQualificationCtrlNbr",
                        column: x => x.EmployeeQualificationCtrlNbr,
                        principalTable: "EmployeeQualifications",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QualificationEvidence_QualificationPrerequisites_PrerequisiteCtrlNbr",
                        column: x => x.PrerequisiteCtrlNbr,
                        principalTable: "QualificationPrerequisites",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeQualifications_EmployeeCtrlNbr_QualificationTypeCtrlNbr",
                table: "EmployeeQualifications",
                columns: new[] { "EmployeeCtrlNbr", "QualificationTypeCtrlNbr" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeQualifications_QualificationTypeCtrlNbr",
                table: "EmployeeQualifications",
                column: "QualificationTypeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_QualificationEvidence_EmployeeQualificationCtrlNbr",
                table: "QualificationEvidence",
                column: "EmployeeQualificationCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_QualificationEvidence_PrerequisiteCtrlNbr",
                table: "QualificationEvidence",
                column: "PrerequisiteCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_QualificationPrerequisites_QualificationTypeCtrlNbr",
                table: "QualificationPrerequisites",
                column: "QualificationTypeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_QualificationPrerequisites_RequiredQualTypeCtrlNbr",
                table: "QualificationPrerequisites",
                column: "RequiredQualTypeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_QualificationTypes_CraftCtrlNbr",
                table: "QualificationTypes",
                column: "CraftCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_QualificationTypes_ParentCtrlNbr_Code",
                table: "QualificationTypes",
                columns: new[] { "ParentCtrlNbr", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_QualificationTypes_ScopeGroupCtrlNbr",
                table: "QualificationTypes",
                column: "ScopeGroupCtrlNbr");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QualificationEvidence");

            migrationBuilder.DropTable(
                name: "EmployeeQualifications");

            migrationBuilder.DropTable(
                name: "QualificationPrerequisites");

            migrationBuilder.DropTable(
                name: "QualificationTypes");
        }
    }
}
