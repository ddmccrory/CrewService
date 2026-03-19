using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class ChangeAssignmentUniqueIndexToIncludeRailroad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserParentAssignments_UserId_ParentCtrlNbr",
                table: "UserParentAssignments");

            migrationBuilder.CreateIndex(
                name: "IX_UserParentAssignments_UserId_ParentCtrlNbr_RailroadCtrlNbr",
                table: "UserParentAssignments",
                columns: new[] { "UserId", "ParentCtrlNbr", "RailroadCtrlNbr" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserParentAssignments_UserId_ParentCtrlNbr_RailroadCtrlNbr",
                table: "UserParentAssignments");

            migrationBuilder.CreateIndex(
                name: "IX_UserParentAssignments_UserId_ParentCtrlNbr",
                table: "UserParentAssignments",
                columns: new[] { "UserId", "ParentCtrlNbr" },
                unique: true);
        }
    }
}
