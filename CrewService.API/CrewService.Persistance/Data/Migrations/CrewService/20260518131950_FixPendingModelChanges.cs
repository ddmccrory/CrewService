using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class FixPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PositionVacancies_WorkAreaGroupCtrlNbr",
                table: "PositionVacancies",
                column: "WorkAreaGroupCtrlNbr");

            migrationBuilder.AddForeignKey(
                name: "FK_BulletinRules_Crafts_CraftCtrlNbr",
                table: "BulletinRules",
                column: "CraftCtrlNbr",
                principalTable: "Crafts",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PositionVacancies_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "PositionVacancies",
                column: "WorkAreaGroupCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BulletinRules_Crafts_CraftCtrlNbr",
                table: "BulletinRules");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionVacancies_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "PositionVacancies");

            migrationBuilder.DropIndex(
                name: "IX_PositionVacancies_WorkAreaGroupCtrlNbr",
                table: "PositionVacancies");
        }
    }
}
