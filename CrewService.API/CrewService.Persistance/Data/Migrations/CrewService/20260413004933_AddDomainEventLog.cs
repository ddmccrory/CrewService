using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddDomainEventLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DomainEventLogs",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    AggregateType = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    AggregateId = table.Column<long>(type: "INTEGER", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: true),
                    PerformedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LoggedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DomainEventLogs", x => x.EventId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DomainEventLogs_AggregateId",
                table: "DomainEventLogs",
                column: "AggregateId");

            migrationBuilder.CreateIndex(
                name: "IX_DomainEventLogs_AggregateType",
                table: "DomainEventLogs",
                column: "AggregateType");

            migrationBuilder.CreateIndex(
                name: "IX_DomainEventLogs_EventType",
                table: "DomainEventLogs",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_DomainEventLogs_OccurredAt",
                table: "DomainEventLogs",
                column: "OccurredAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DomainEventLogs");
        }
    }
}
