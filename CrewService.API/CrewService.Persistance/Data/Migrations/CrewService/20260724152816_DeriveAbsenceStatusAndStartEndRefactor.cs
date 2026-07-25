using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class DeriveAbsenceStatusAndStartEndRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AbsenceApprovals");

            migrationBuilder.DropTable(
                name: "AbsenceMarkUps");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AbsenceRequests");

            migrationBuilder.RenameColumn(
                name: "MarkOffStartUtc",
                table: "AbsenceRequests",
                newName: "ScheduledEndUtc");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAtUtc",
                table: "AbsenceRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAtUtc",
                table: "AbsenceRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CancelledByCtrlNbr",
                table: "AbsenceRequests",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeniedAtUtc",
                table: "AbsenceRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeniedByCtrlNbr",
                table: "AbsenceRequests",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AbsenceEndRecords",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    AbsenceRequestCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ActualEndUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsAutoEndRecord = table.Column<bool>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_AbsenceEndRecords", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_AbsenceEndRecords_AbsenceRequests_AbsenceRequestCtrlNbr",
                        column: x => x.AbsenceRequestCtrlNbr,
                        principalTable: "AbsenceRequests",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AbsenceStartRecords",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    AbsenceRequestCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ActualStartUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
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
                    table.PrimaryKey("PK_AbsenceStartRecords", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_AbsenceStartRecords_AbsenceRequests_AbsenceRequestCtrlNbr",
                        column: x => x.AbsenceRequestCtrlNbr,
                        principalTable: "AbsenceRequests",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceRequests_CancelledByCtrlNbr",
                table: "AbsenceRequests",
                column: "CancelledByCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceRequests_DeniedByCtrlNbr",
                table: "AbsenceRequests",
                column: "DeniedByCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceEndRecords_AbsenceRequestCtrlNbr",
                table: "AbsenceEndRecords",
                column: "AbsenceRequestCtrlNbr",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceStartRecords_AbsenceRequestCtrlNbr",
                table: "AbsenceStartRecords",
                column: "AbsenceRequestCtrlNbr",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AbsenceRequests_Employees_CancelledByCtrlNbr",
                table: "AbsenceRequests",
                column: "CancelledByCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AbsenceRequests_Employees_DeniedByCtrlNbr",
                table: "AbsenceRequests",
                column: "DeniedByCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AbsenceRequests_Employees_CancelledByCtrlNbr",
                table: "AbsenceRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_AbsenceRequests_Employees_DeniedByCtrlNbr",
                table: "AbsenceRequests");

            migrationBuilder.DropTable(
                name: "AbsenceEndRecords");

            migrationBuilder.DropTable(
                name: "AbsenceStartRecords");

            migrationBuilder.DropIndex(
                name: "IX_AbsenceRequests_CancelledByCtrlNbr",
                table: "AbsenceRequests");

            migrationBuilder.DropIndex(
                name: "IX_AbsenceRequests_DeniedByCtrlNbr",
                table: "AbsenceRequests");

            migrationBuilder.DropColumn(
                name: "ApprovedAtUtc",
                table: "AbsenceRequests");

            migrationBuilder.DropColumn(
                name: "CancelledAtUtc",
                table: "AbsenceRequests");

            migrationBuilder.DropColumn(
                name: "CancelledByCtrlNbr",
                table: "AbsenceRequests");

            migrationBuilder.DropColumn(
                name: "DeniedAtUtc",
                table: "AbsenceRequests");

            migrationBuilder.DropColumn(
                name: "DeniedByCtrlNbr",
                table: "AbsenceRequests");

            migrationBuilder.RenameColumn(
                name: "ScheduledEndUtc",
                table: "AbsenceRequests",
                newName: "MarkOffStartUtc");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "AbsenceRequests",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AbsenceApprovals",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    AbsenceRequestCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ApprovalOfficerCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    DecidedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
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
                    ActualMarkUpUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsAutoMarkUp = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    ScheduledMarkUpUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceApprovals_AbsenceRequestCtrlNbr",
                table: "AbsenceApprovals",
                column: "AbsenceRequestCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceApprovals_ApprovalOfficerCtrlNbr",
                table: "AbsenceApprovals",
                column: "ApprovalOfficerCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceMarkUps_AbsenceRequestCtrlNbr",
                table: "AbsenceMarkUps",
                column: "AbsenceRequestCtrlNbr");
        }
    }
}
