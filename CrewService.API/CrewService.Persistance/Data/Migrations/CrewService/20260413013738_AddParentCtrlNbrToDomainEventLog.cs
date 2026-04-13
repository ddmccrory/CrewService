using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddParentCtrlNbrToDomainEventLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ParentCtrlNbr",
                table: "DomainEventLogs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DomainEventLogs_ParentCtrlNbr",
                table: "DomainEventLogs",
                column: "ParentCtrlNbr");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DomainEventLogs_ParentCtrlNbr",
                table: "DomainEventLogs");

            migrationBuilder.DropColumn(
                name: "ParentCtrlNbr",
                table: "DomainEventLogs");
        }
    }
}
