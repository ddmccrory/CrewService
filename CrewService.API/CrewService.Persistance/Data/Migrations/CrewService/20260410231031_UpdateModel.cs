using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class UpdateModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Crews_DynamicGroups_HomeGroupCtrlNbr",
                table: "Crews");

            migrationBuilder.RenameColumn(
                name: "HomeGroupCtrlNbr",
                table: "Crews",
                newName: "WorkAreaCtrlNbr");

            migrationBuilder.RenameIndex(
                name: "IX_Crews_HomeGroupCtrlNbr",
                table: "Crews",
                newName: "IX_Crews_WorkAreaCtrlNbr");

            migrationBuilder.AddColumn<DateTime>(
                name: "AbolishedDate",
                table: "Crews",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveDate",
                table: "Crews",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddForeignKey(
                name: "FK_Crews_DynamicGroups_WorkAreaCtrlNbr",
                table: "Crews",
                column: "WorkAreaCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Crews_DynamicGroups_WorkAreaCtrlNbr",
                table: "Crews");

            migrationBuilder.DropColumn(
                name: "AbolishedDate",
                table: "Crews");

            migrationBuilder.DropColumn(
                name: "EffectiveDate",
                table: "Crews");

            migrationBuilder.RenameColumn(
                name: "WorkAreaCtrlNbr",
                table: "Crews",
                newName: "HomeGroupCtrlNbr");

            migrationBuilder.RenameIndex(
                name: "IX_Crews_WorkAreaCtrlNbr",
                table: "Crews",
                newName: "IX_Crews_HomeGroupCtrlNbr");

            migrationBuilder.AddForeignKey(
                name: "FK_Crews_DynamicGroups_HomeGroupCtrlNbr",
                table: "Crews",
                column: "HomeGroupCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
