using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddUniqueCrewNamePerWorkArea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Crews_WorkAreaCtrlNbr_Name",
                table: "Crews",
                columns: new[] { "WorkAreaCtrlNbr", "Name" },
                unique: true,
                filter: "IsDeleted = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Crews_WorkAreaCtrlNbr_Name",
                table: "Crews");
        }
    }
}
