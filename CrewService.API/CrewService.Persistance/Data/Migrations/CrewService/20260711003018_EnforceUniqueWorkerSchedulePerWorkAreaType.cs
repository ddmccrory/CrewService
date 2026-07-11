using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class EnforceUniqueWorkerSchedulePerWorkAreaType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM WorkerSchedules
                WHERE CtrlNbr IN (
                    SELECT duplicate.CtrlNbr
                    FROM WorkerSchedules AS duplicate
                    JOIN (
                        SELECT WorkAreaGroupCtrlNbr, WorkerType, MAX(CtrlNbr) AS KeepCtrlNbr
                        FROM WorkerSchedules
                        GROUP BY WorkAreaGroupCtrlNbr, WorkerType
                        HAVING COUNT(*) > 1
                    ) AS grouped
                      ON grouped.WorkAreaGroupCtrlNbr = duplicate.WorkAreaGroupCtrlNbr
                     AND grouped.WorkerType = duplicate.WorkerType
                    WHERE duplicate.CtrlNbr <> grouped.KeepCtrlNbr
                );
                """);

            migrationBuilder.DropIndex(
                name: "IX_WorkerSchedules_WorkAreaGroupCtrlNbr",
                table: "WorkerSchedules");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerSchedules_WorkAreaGroupCtrlNbr_WorkerType",
                table: "WorkerSchedules",
                columns: new[] { "WorkAreaGroupCtrlNbr", "WorkerType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkerSchedules_WorkAreaGroupCtrlNbr_WorkerType",
                table: "WorkerSchedules");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerSchedules_WorkAreaGroupCtrlNbr",
                table: "WorkerSchedules",
                column: "WorkAreaGroupCtrlNbr");
        }
    }
}
