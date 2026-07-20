using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelAfterAbsenceRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
            migrationBuilder.DropForeignKey(
                name: "FK_AbsenceCodes_DynamicGroups_RailroadCtrlNbr",
                table: "AbsenceCodes");
        }
    }
}
