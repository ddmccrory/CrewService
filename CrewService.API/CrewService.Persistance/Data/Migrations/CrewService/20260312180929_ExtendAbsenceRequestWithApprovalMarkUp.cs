using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class ExtendAbsenceRequestWithApprovalMarkUp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "AbsenceCodeCtrlNbr",
                table: "AbsenceRequest",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSystemGenerated",
                table: "AbsenceRequest",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "MarkOffStartUtc",
                table: "AbsenceRequest",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PositionSlotCtrlNbr",
                table: "AbsenceRequest",
                type: "INTEGER",
                nullable: true);

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
                        name: "FK_AbsenceApprovals_AbsenceRequest_AbsenceRequestCtrlNbr",
                        column: x => x.AbsenceRequestCtrlNbr,
                        principalTable: "AbsenceRequest",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
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
                        name: "FK_AbsenceMarkUps_AbsenceRequest_AbsenceRequestCtrlNbr",
                        column: x => x.AbsenceRequestCtrlNbr,
                        principalTable: "AbsenceRequest",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceApprovals_AbsenceRequestCtrlNbr",
                table: "AbsenceApprovals",
                column: "AbsenceRequestCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceMarkUps_AbsenceRequestCtrlNbr",
                table: "AbsenceMarkUps",
                column: "AbsenceRequestCtrlNbr");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AbsenceApprovals");

            migrationBuilder.DropTable(
                name: "AbsenceMarkUps");

            migrationBuilder.DropColumn(
                name: "AbsenceCodeCtrlNbr",
                table: "AbsenceRequest");

            migrationBuilder.DropColumn(
                name: "IsSystemGenerated",
                table: "AbsenceRequest");

            migrationBuilder.DropColumn(
                name: "MarkOffStartUtc",
                table: "AbsenceRequest");

            migrationBuilder.DropColumn(
                name: "PositionSlotCtrlNbr",
                table: "AbsenceRequest");
        }
    }
}
