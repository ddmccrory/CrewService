using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddGroupToAssignmentAndPositionSlot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "Assignments");

            migrationBuilder.RenameColumn(
                name: "WorkAreaGroupCtrlNbr",
                table: "Assignments",
                newName: "GroupCtrlNbr");

            migrationBuilder.RenameIndex(
                name: "IX_Assignments_WorkAreaGroupCtrlNbr",
                table: "Assignments",
                newName: "IX_Assignments_GroupCtrlNbr");

            migrationBuilder.AddColumn<string>(
                name: "GroupCode",
                table: "PositionSlotInstances",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GroupName",
                table: "PositionSlotInstances",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_DynamicGroups_GroupCtrlNbr",
                table: "Assignments",
                column: "GroupCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_DynamicGroups_GroupCtrlNbr",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "GroupCode",
                table: "PositionSlotInstances");

            migrationBuilder.DropColumn(
                name: "GroupName",
                table: "PositionSlotInstances");

            migrationBuilder.RenameColumn(
                name: "GroupCtrlNbr",
                table: "Assignments",
                newName: "WorkAreaGroupCtrlNbr");

            migrationBuilder.RenameIndex(
                name: "IX_Assignments_GroupCtrlNbr",
                table: "Assignments",
                newName: "IX_Assignments_WorkAreaGroupCtrlNbr");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "Assignments",
                column: "WorkAreaGroupCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
