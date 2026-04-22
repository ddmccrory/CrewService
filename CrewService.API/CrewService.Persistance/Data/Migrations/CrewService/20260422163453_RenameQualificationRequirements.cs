using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class RenameQualificationRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QualificationEvidence_QualificationPrerequisites_PrerequisiteCtrlNbr",
                table: "QualificationEvidence");

            migrationBuilder.DropTable(
                name: "QualificationPrerequisites");

            migrationBuilder.RenameColumn(
                name: "PrerequisiteCtrlNbr",
                table: "QualificationEvidence",
                newName: "RequirementCtrlNbr");

            migrationBuilder.RenameIndex(
                name: "IX_QualificationEvidence_PrerequisiteCtrlNbr",
                table: "QualificationEvidence",
                newName: "IX_QualificationEvidence_RequirementCtrlNbr");

            migrationBuilder.CreateTable(
                name: "QualificationRequirements",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    QualificationTypeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    RequirementKind = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Threshold = table.Column<int>(type: "INTEGER", nullable: false),
                    ThresholdUnit = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    EventSource = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    ActivityFilter = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    RequiredQualTypeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("PK_QualificationRequirements", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_QualificationRequirements_QualificationTypes_QualificationTypeCtrlNbr",
                        column: x => x.QualificationTypeCtrlNbr,
                        principalTable: "QualificationTypes",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QualificationRequirements_QualificationTypes_RequiredQualTypeCtrlNbr",
                        column: x => x.RequiredQualTypeCtrlNbr,
                        principalTable: "QualificationTypes",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QualificationRequirements_QualificationTypeCtrlNbr",
                table: "QualificationRequirements",
                column: "QualificationTypeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_QualificationRequirements_RequiredQualTypeCtrlNbr",
                table: "QualificationRequirements",
                column: "RequiredQualTypeCtrlNbr");

            migrationBuilder.AddForeignKey(
                name: "FK_QualificationEvidence_QualificationRequirements_RequirementCtrlNbr",
                table: "QualificationEvidence",
                column: "RequirementCtrlNbr",
                principalTable: "QualificationRequirements",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QualificationEvidence_QualificationRequirements_RequirementCtrlNbr",
                table: "QualificationEvidence");

            migrationBuilder.DropTable(
                name: "QualificationRequirements");

            migrationBuilder.RenameColumn(
                name: "RequirementCtrlNbr",
                table: "QualificationEvidence",
                newName: "PrerequisiteCtrlNbr");

            migrationBuilder.RenameIndex(
                name: "IX_QualificationEvidence_RequirementCtrlNbr",
                table: "QualificationEvidence",
                newName: "IX_QualificationEvidence_PrerequisiteCtrlNbr");

            migrationBuilder.CreateTable(
                name: "QualificationPrerequisites",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ActivityFilter = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    EventSource = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    PrerequisiteKind = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    QualificationTypeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    RequiredQualTypeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    Threshold = table.Column<int>(type: "INTEGER", nullable: false),
                    ThresholdUnit = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualificationPrerequisites", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_QualificationPrerequisites_QualificationTypes_QualificationTypeCtrlNbr",
                        column: x => x.QualificationTypeCtrlNbr,
                        principalTable: "QualificationTypes",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QualificationPrerequisites_QualificationTypes_RequiredQualTypeCtrlNbr",
                        column: x => x.RequiredQualTypeCtrlNbr,
                        principalTable: "QualificationTypes",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QualificationPrerequisites_QualificationTypeCtrlNbr",
                table: "QualificationPrerequisites",
                column: "QualificationTypeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_QualificationPrerequisites_RequiredQualTypeCtrlNbr",
                table: "QualificationPrerequisites",
                column: "RequiredQualTypeCtrlNbr");

            migrationBuilder.AddForeignKey(
                name: "FK_QualificationEvidence_QualificationPrerequisites_PrerequisiteCtrlNbr",
                table: "QualificationEvidence",
                column: "PrerequisiteCtrlNbr",
                principalTable: "QualificationPrerequisites",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
