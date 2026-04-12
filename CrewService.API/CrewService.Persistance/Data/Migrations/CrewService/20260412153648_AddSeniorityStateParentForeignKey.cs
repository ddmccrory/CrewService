using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddSeniorityStateParentForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SeniorityStates_ParentCtrlNbr",
                table: "SeniorityStates",
                column: "ParentCtrlNbr");

            migrationBuilder.AddForeignKey(
                name: "FK_SeniorityStates_Parents_ParentCtrlNbr",
                table: "SeniorityStates",
                column: "ParentCtrlNbr",
                principalTable: "Parents",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SeniorityStates_Parents_ParentCtrlNbr",
                table: "SeniorityStates");

            migrationBuilder.DropIndex(
                name: "IX_SeniorityStates_ParentCtrlNbr",
                table: "SeniorityStates");
        }
    }
}
