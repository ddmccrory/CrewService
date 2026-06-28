using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddSeniorityMoveLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SeniorityMoves_CraftCtrlNbr",
                table: "SeniorityMoves");

            migrationBuilder.DropIndex(
                name: "IX_SeniorityMoves_EmployeeCtrlNbr",
                table: "SeniorityMoves");

            migrationBuilder.DropIndex(
                name: "IX_SeniorityMovePolicies_CraftCtrlNbr",
                table: "SeniorityMovePolicies");

            migrationBuilder.RenameColumn(
                name: "ExercisedUtc",
                table: "SeniorityMoves",
                newName: "RequestedUtc");

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "SeniorityMoves",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveUtc",
                table: "SeniorityMoves",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MoveType",
                table: "SeniorityMoves",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "SeniorityMoves",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "SeniorityMoves",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "AutoApprove",
                table: "SeniorityMovePolicies",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_SeniorityMoves_CraftCtrlNbr_Status",
                table: "SeniorityMoves",
                columns: new[] { "CraftCtrlNbr", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SeniorityMoves_EmployeeCtrlNbr_Status",
                table: "SeniorityMoves",
                columns: new[] { "EmployeeCtrlNbr", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SeniorityMovePolicies_CraftCtrlNbr",
                table: "SeniorityMovePolicies",
                column: "CraftCtrlNbr",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PendingSeniorityStateChanges_Employees_EmployeeCtrlNbr",
                table: "PendingSeniorityStateChanges",
                column: "EmployeeCtrlNbr",
                principalTable: "Employees",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PendingSeniorityStateChanges_Employees_EmployeeCtrlNbr",
                table: "PendingSeniorityStateChanges");

            migrationBuilder.DropIndex(
                name: "IX_SeniorityMoves_CraftCtrlNbr_Status",
                table: "SeniorityMoves");

            migrationBuilder.DropIndex(
                name: "IX_SeniorityMoves_EmployeeCtrlNbr_Status",
                table: "SeniorityMoves");

            migrationBuilder.DropIndex(
                name: "IX_SeniorityMovePolicies_CraftCtrlNbr",
                table: "SeniorityMovePolicies");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "SeniorityMoves");

            migrationBuilder.DropColumn(
                name: "EffectiveUtc",
                table: "SeniorityMoves");

            migrationBuilder.DropColumn(
                name: "MoveType",
                table: "SeniorityMoves");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "SeniorityMoves");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "SeniorityMoves");

            migrationBuilder.DropColumn(
                name: "AutoApprove",
                table: "SeniorityMovePolicies");

            migrationBuilder.RenameColumn(
                name: "RequestedUtc",
                table: "SeniorityMoves",
                newName: "ExercisedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SeniorityMoves_CraftCtrlNbr",
                table: "SeniorityMoves",
                column: "CraftCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_SeniorityMoves_EmployeeCtrlNbr",
                table: "SeniorityMoves",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_SeniorityMovePolicies_CraftCtrlNbr",
                table: "SeniorityMovePolicies",
                column: "CraftCtrlNbr");
        }
    }
}
