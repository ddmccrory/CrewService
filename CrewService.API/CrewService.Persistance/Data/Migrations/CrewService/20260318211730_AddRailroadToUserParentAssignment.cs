using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddRailroadToUserParentAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "RailroadCtrlNbr",
                table: "UserParentAssignments",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserParentAssignments_RailroadCtrlNbr",
                table: "UserParentAssignments",
                column: "RailroadCtrlNbr");

            migrationBuilder.AddForeignKey(
                name: "FK_UserParentAssignments_Railroads_RailroadCtrlNbr",
                table: "UserParentAssignments",
                column: "RailroadCtrlNbr",
                principalTable: "Railroads",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserParentAssignments_Railroads_RailroadCtrlNbr",
                table: "UserParentAssignments");

            migrationBuilder.DropIndex(
                name: "IX_UserParentAssignments_RailroadCtrlNbr",
                table: "UserParentAssignments");

            migrationBuilder.DropColumn(
                name: "RailroadCtrlNbr",
                table: "UserParentAssignments");
        }
    }
}
