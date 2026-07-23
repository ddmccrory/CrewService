using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace CrewService.Persistance.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelAfterAbsenceRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.Sql(@"
PRAGMA foreign_keys = OFF;

CREATE TABLE IF NOT EXISTS ""ef_temp_AbsenceRequests"" (
    ""CtrlNbr"" INTEGER NOT NULL CONSTRAINT ""PK_AbsenceRequests"" PRIMARY KEY,
    ""AbsenceCodeCtrlNbr"" INTEGER NULL,
    ""ApprovedByCtrlNbr"" INTEGER NULL,
    ""CreatedBy_AuditDateTime"" TEXT NULL,
    ""CreatedBy_AuditName"" TEXT NULL,
    ""DeletedAt"" TEXT NULL,
    ""DeletedBy_AuditDateTime"" TEXT NULL,
    ""DeletedBy_AuditName"" TEXT NULL,
    ""EmployeeCtrlNbr"" INTEGER NOT NULL,
    ""IsDeleted"" INTEGER NOT NULL,
    ""IsSystemGenerated"" INTEGER NOT NULL,
    ""MarkOffStartUtc"" TEXT NULL,
    ""ModifiedBy_AuditDateTime"" TEXT NULL,
    ""ModifiedBy_AuditName"" TEXT NULL,
    ""Notes"" TEXT NULL,
    ""ReasonCode"" TEXT NOT NULL,
    ""StartUtc"" TEXT NOT NULL,
    ""Status"" TEXT NOT NULL,
    CONSTRAINT ""FK_AbsenceRequests_AbsenceCodes_AbsenceCodeCtrlNbr"" FOREIGN KEY (""AbsenceCodeCtrlNbr"") REFERENCES ""AbsenceCodes"" (""CtrlNbr"") ON DELETE RESTRICT,
    CONSTRAINT ""FK_AbsenceRequests_Employees_ApprovedByCtrlNbr"" FOREIGN KEY (""ApprovedByCtrlNbr"") REFERENCES ""Employees"" (""CtrlNbr"") ON DELETE RESTRICT,
    CONSTRAINT ""FK_AbsenceRequests_Employees_EmployeeCtrlNbr"" FOREIGN KEY (""EmployeeCtrlNbr"") REFERENCES ""Employees"" (""CtrlNbr"") ON DELETE RESTRICT
);

INSERT INTO ""ef_temp_AbsenceRequests"" (
    ""CtrlNbr"", ""AbsenceCodeCtrlNbr"", ""ApprovedByCtrlNbr"", ""CreatedBy_AuditDateTime"", ""CreatedBy_AuditName"",
    ""DeletedAt"", ""DeletedBy_AuditDateTime"", ""DeletedBy_AuditName"", ""EmployeeCtrlNbr"", ""IsDeleted"",
    ""IsSystemGenerated"", ""MarkOffStartUtc"", ""ModifiedBy_AuditDateTime"", ""ModifiedBy_AuditName"", ""Notes"",
    ""ReasonCode"", ""StartUtc"", ""Status"")
SELECT
    ""CtrlNbr"", ""AbsenceCodeCtrlNbr"", ""ApprovedByCtrlNbr"", ""CreatedBy_AuditDateTime"", ""CreatedBy_AuditName"",
    ""DeletedAt"", ""DeletedBy_AuditDateTime"", ""DeletedBy_AuditName"", ""EmployeeCtrlNbr"", ""IsDeleted"",
    ""IsSystemGenerated"", ""MarkOffStartUtc"", ""ModifiedBy_AuditDateTime"", ""ModifiedBy_AuditName"", ""Notes"",
    ""ReasonCode"", ""StartUtc"", ""Status""
FROM ""AbsenceRequests"";

DROP TABLE ""AbsenceRequests"";
ALTER TABLE ""ef_temp_AbsenceRequests"" RENAME TO ""AbsenceRequests"";

CREATE INDEX IF NOT EXISTS ""IX_AbsenceRequests_AbsenceCodeCtrlNbr"" ON ""AbsenceRequests"" (""AbsenceCodeCtrlNbr"");
CREATE INDEX IF NOT EXISTS ""IX_AbsenceRequests_ApprovedByCtrlNbr"" ON ""AbsenceRequests"" (""ApprovedByCtrlNbr"");
CREATE INDEX IF NOT EXISTS ""IX_AbsenceRequests_EmployeeCtrlNbr"" ON ""AbsenceRequests"" (""EmployeeCtrlNbr"");

PRAGMA foreign_keys = ON;
");
            }
            else
            {
                migrationBuilder.DropForeignKey(
                    name: "FK_AbsenceRequests_PositionSlots_PositionSlotCtrlNbr",
                    table: "AbsenceRequests");

                migrationBuilder.DropIndex(
                    name: "IX_AbsenceRequests_PositionSlotCtrlNbr",
                    table: "AbsenceRequests");

                migrationBuilder.DropColumn(
                    name: "PositionSlotCtrlNbr",
                    table: "AbsenceRequests");

                migrationBuilder.DropColumn(
                    name: "EndUtc",
                    table: "AbsenceRequests");
            }

            migrationBuilder.Sql(@"
                DELETE FROM AbsenceCodes
                WHERE (RailroadCtrlNbr = 0
                    OR RailroadCtrlNbr NOT IN (SELECT CtrlNbr FROM DynamicGroups))
                  AND EXISTS (
                    SELECT 1
                    FROM AbsenceCodes existing
                    WHERE existing.RailroadCtrlNbr = (
                            SELECT CtrlNbr
                            FROM DynamicGroups
                            ORDER BY CtrlNbr
                            LIMIT 1
                        )
                      AND existing.Code = AbsenceCodes.Code
                      AND existing.CtrlNbr <> AbsenceCodes.CtrlNbr
                );
            ");

            migrationBuilder.Sql(@"
                UPDATE AbsenceCodes
                SET RailroadCtrlNbr = (
                    SELECT CtrlNbr
                    FROM DynamicGroups
                    ORDER BY CtrlNbr
                    LIMIT 1
                )
                WHERE RailroadCtrlNbr = 0
                   OR RailroadCtrlNbr NOT IN (SELECT CtrlNbr FROM DynamicGroups)
                  AND EXISTS (SELECT 1 FROM DynamicGroups);
            ");

            migrationBuilder.Sql(@"
                DELETE FROM AbsenceCodes
                WHERE RailroadCtrlNbr = 0
                   OR RailroadCtrlNbr NOT IN (SELECT CtrlNbr FROM DynamicGroups);
            ");

            migrationBuilder.AddForeignKey(
                name: "FK_AbsenceCodes_DynamicGroups_RailroadCtrlNbr",
                table: "AbsenceCodes",
                column: "RailroadCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EndUtc",
                table: "AbsenceRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PositionSlotCtrlNbr",
                table: "AbsenceRequests",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceRequests_PositionSlotCtrlNbr",
                table: "AbsenceRequests",
                column: "PositionSlotCtrlNbr");

            migrationBuilder.AddForeignKey(
                name: "FK_AbsenceRequests_PositionSlots_PositionSlotCtrlNbr",
                table: "AbsenceRequests",
                column: "PositionSlotCtrlNbr",
                principalTable: "PositionSlots",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropForeignKey(
                name: "FK_AbsenceCodes_DynamicGroups_RailroadCtrlNbr",
                table: "AbsenceCodes");
        }
    }
}
