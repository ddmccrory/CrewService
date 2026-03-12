using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddRegulatoryQualification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CraftRegulatoryQualifications",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CraftCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    RegulatoryQualificationCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_CraftRegulatoryQualifications", x => x.CtrlNbr);
                });

            migrationBuilder.CreateTable(
                name: "RegulatoryQualifications",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CfrPart = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RequiresCertification = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecertificationIntervalMonths = table.Column<int>(type: "INTEGER", nullable: true),
                    EffectiveDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
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
                    table.PrimaryKey("PK_RegulatoryQualifications", x => x.CtrlNbr);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CraftRegulatoryQualifications_CraftCtrlNbr_RegulatoryQualificationCtrlNbr",
                table: "CraftRegulatoryQualifications",
                columns: new[] { "CraftCtrlNbr", "RegulatoryQualificationCtrlNbr" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegulatoryQualifications_Code",
                table: "RegulatoryQualifications",
                column: "Code",
                unique: true);

            // Seed CFR Parts 240/242 regulatory qualifications
            var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            migrationBuilder.InsertData(
                table: "RegulatoryQualifications",
                columns: new[] { "CtrlNbr", "Code", "CfrPart", "Description", "RequiresCertification", "RecertificationIntervalMonths", "EffectiveDate", "CreatedBy_AuditName", "CreatedBy_AuditDateTime", "IsDeleted" },
                values: new object[] { 101L, "CFR-240-ENGINEER", "49 CFR Part 240", "Locomotive Engineer Certification", true, 36, new DateOnly(1991, 3, 1), "SYSTEM", now, false });

            migrationBuilder.InsertData(
                table: "RegulatoryQualifications",
                columns: new[] { "CtrlNbr", "Code", "CfrPart", "Description", "RequiresCertification", "RecertificationIntervalMonths", "EffectiveDate", "CreatedBy_AuditName", "CreatedBy_AuditDateTime", "IsDeleted" },
                values: new object[] { 102L, "CFR-242-CONDUCTOR", "49 CFR Part 242", "Conductor Certification", true, 36, new DateOnly(2012, 1, 1), "SYSTEM", now, false });

            migrationBuilder.InsertData(
                table: "RegulatoryQualifications",
                columns: new[] { "CtrlNbr", "Code", "CfrPart", "Description", "RequiresCertification", "RecertificationIntervalMonths", "EffectiveDate", "CreatedBy_AuditName", "CreatedBy_AuditDateTime", "IsDeleted" },
                values: new object[] { 103L, "CFR-242-SWITCHMAN", "49 CFR Part 242", "Switchman Certification", true, 36, new DateOnly(2012, 1, 1), "SYSTEM", now, false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CraftRegulatoryQualifications");

            migrationBuilder.DropTable(
                name: "RegulatoryQualifications");
        }
    }
}
