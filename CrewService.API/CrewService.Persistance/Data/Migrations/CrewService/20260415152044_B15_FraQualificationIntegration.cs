using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class B15_FraQualificationIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSystemSeeded",
                table: "QualificationTypes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "RegulatoryQualificationCtrlNbr",
                table: "QualificationTypes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_QualificationTypes_RegulatoryQualificationCtrlNbr",
                table: "QualificationTypes",
                column: "RegulatoryQualificationCtrlNbr");

            migrationBuilder.AddForeignKey(
                name: "FK_QualificationTypes_RegulatoryQualifications_RegulatoryQualificationCtrlNbr",
                table: "QualificationTypes",
                column: "RegulatoryQualificationCtrlNbr",
                principalTable: "RegulatoryQualifications",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QualificationTypes_RegulatoryQualifications_RegulatoryQualificationCtrlNbr",
                table: "QualificationTypes");

            migrationBuilder.DropIndex(
                name: "IX_QualificationTypes_RegulatoryQualificationCtrlNbr",
                table: "QualificationTypes");

            migrationBuilder.DropColumn(
                name: "IsSystemSeeded",
                table: "QualificationTypes");

            migrationBuilder.DropColumn(
                name: "RegulatoryQualificationCtrlNbr",
                table: "QualificationTypes");
        }
    }
}
