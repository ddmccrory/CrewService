using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddStaffablePositionAndPositionAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "StaffablePositionCtrlNbr",
                table: "RosterBoardPositions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "StaffablePositionCtrlNbr",
                table: "CrewPositions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "StaffablePositions",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    PositionType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffablePositions", x => x.CtrlNbr);
                });

            migrationBuilder.CreateTable(
                name: "PositionAssignments",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    StaffablePositionCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    AssignmentType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    AssignmentSourceCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    AssignedDateUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PositionAssignments", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_PositionAssignments_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PositionAssignments_StaffablePositions_StaffablePositionCtrlNbr",
                        column: x => x.StaffablePositionCtrlNbr,
                        principalTable: "StaffablePositions",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RosterBoardPositions_StaffablePositionCtrlNbr",
                table: "RosterBoardPositions",
                column: "StaffablePositionCtrlNbr",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CrewPositions_StaffablePositionCtrlNbr",
                table: "CrewPositions",
                column: "StaffablePositionCtrlNbr",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PositionAssignments_EmployeeCtrlNbr",
                table: "PositionAssignments",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_PositionAssignments_StaffablePositionCtrlNbr",
                table: "PositionAssignments",
                column: "StaffablePositionCtrlNbr");

            migrationBuilder.AddForeignKey(
                name: "FK_CrewPositions_StaffablePositions_StaffablePositionCtrlNbr",
                table: "CrewPositions",
                column: "StaffablePositionCtrlNbr",
                principalTable: "StaffablePositions",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RosterBoardPositions_StaffablePositions_StaffablePositionCtrlNbr",
                table: "RosterBoardPositions",
                column: "StaffablePositionCtrlNbr",
                principalTable: "StaffablePositions",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CrewPositions_StaffablePositions_StaffablePositionCtrlNbr",
                table: "CrewPositions");

            migrationBuilder.DropForeignKey(
                name: "FK_RosterBoardPositions_StaffablePositions_StaffablePositionCtrlNbr",
                table: "RosterBoardPositions");

            migrationBuilder.DropTable(
                name: "PositionAssignments");

            migrationBuilder.DropTable(
                name: "StaffablePositions");

            migrationBuilder.DropIndex(
                name: "IX_RosterBoardPositions_StaffablePositionCtrlNbr",
                table: "RosterBoardPositions");

            migrationBuilder.DropIndex(
                name: "IX_CrewPositions_StaffablePositionCtrlNbr",
                table: "CrewPositions");

            migrationBuilder.DropColumn(
                name: "StaffablePositionCtrlNbr",
                table: "RosterBoardPositions");

            migrationBuilder.DropColumn(
                name: "StaffablePositionCtrlNbr",
                table: "CrewPositions");
        }
    }
}
