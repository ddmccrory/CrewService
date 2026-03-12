using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddFraDutyTourAggregate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateIndex(
                name: "IX_FraDutyTourSegments_DutyTourCtrlNbr",
                table: "FraDutyTourSegments",
                column: "DutyTourCtrlNbr");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                name: "FraDutyTours");
        }
    }
}
