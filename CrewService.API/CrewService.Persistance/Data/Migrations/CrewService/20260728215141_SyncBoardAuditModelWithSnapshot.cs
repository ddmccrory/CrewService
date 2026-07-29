using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class SyncBoardAuditModelWithSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BoardSnapshotRows_BoardSnapshots_BoardSnapshotCtrlNbr1",
                table: "BoardSnapshotRows");

            migrationBuilder.DropIndex(
                name: "IX_BoardSnapshotRows_BoardSnapshotCtrlNbr1",
                table: "BoardSnapshotRows");

            migrationBuilder.DropColumn(
                name: "BoardSnapshotCtrlNbr1",
                table: "BoardSnapshotRows");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "BoardSnapshotCtrlNbr1",
                table: "BoardSnapshotRows",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BoardSnapshotRows_BoardSnapshotCtrlNbr1",
                table: "BoardSnapshotRows",
                column: "BoardSnapshotCtrlNbr1");

            migrationBuilder.AddForeignKey(
                name: "FK_BoardSnapshotRows_BoardSnapshots_BoardSnapshotCtrlNbr1",
                table: "BoardSnapshotRows",
                column: "BoardSnapshotCtrlNbr1",
                principalTable: "BoardSnapshots",
                principalColumn: "CtrlNbr");
        }
    }
}
