using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class RemoveRailroadEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_Railroads_RailroadCtrlNbr",
                table: "Invitations");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamsWebhookConfigs_Railroads_RailroadCtrlNbr",
                table: "TeamsWebhookConfigs");

            migrationBuilder.DropForeignKey(
                name: "FK_UserParentAssignments_Railroads_RailroadCtrlNbr",
                table: "UserParentAssignments");

            migrationBuilder.DropTable(
                name: "RailroadGroupPlacements");

            migrationBuilder.DropTable(
                name: "Railroads");

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_DynamicGroups_RailroadCtrlNbr",
                table: "Invitations",
                column: "RailroadCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TeamsWebhookConfigs_DynamicGroups_RailroadCtrlNbr",
                table: "TeamsWebhookConfigs",
                column: "RailroadCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserParentAssignments_DynamicGroups_RailroadCtrlNbr",
                table: "UserParentAssignments",
                column: "RailroadCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_DynamicGroups_RailroadCtrlNbr",
                table: "Invitations");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamsWebhookConfigs_DynamicGroups_RailroadCtrlNbr",
                table: "TeamsWebhookConfigs");

            migrationBuilder.DropForeignKey(
                name: "FK_UserParentAssignments_DynamicGroups_RailroadCtrlNbr",
                table: "UserParentAssignments");

            migrationBuilder.CreateTable(
                name: "Railroads",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ParentCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    RailroadMark = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Railroads", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_Railroads_Parents_ParentCtrlNbr",
                        column: x => x.ParentCtrlNbr,
                        principalTable: "Parents",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RailroadGroupPlacements",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    GroupCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    RailroadCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RailroadGroupPlacements", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_RailroadGroupPlacements_DynamicGroups_GroupCtrlNbr",
                        column: x => x.GroupCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RailroadGroupPlacements_Railroads_RailroadCtrlNbr",
                        column: x => x.RailroadCtrlNbr,
                        principalTable: "Railroads",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RailroadGroupPlacements_GroupCtrlNbr",
                table: "RailroadGroupPlacements",
                column: "GroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_RailroadGroupPlacements_RailroadCtrlNbr",
                table: "RailroadGroupPlacements",
                column: "RailroadCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_RailroadGroupPlacements_RailroadCtrlNbr_GroupCtrlNbr",
                table: "RailroadGroupPlacements",
                columns: new[] { "RailroadCtrlNbr", "GroupCtrlNbr" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Railroads_Name",
                table: "Railroads",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Railroads_ParentCtrlNbr",
                table: "Railroads",
                column: "ParentCtrlNbr");

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_Railroads_RailroadCtrlNbr",
                table: "Invitations",
                column: "RailroadCtrlNbr",
                principalTable: "Railroads",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TeamsWebhookConfigs_Railroads_RailroadCtrlNbr",
                table: "TeamsWebhookConfigs",
                column: "RailroadCtrlNbr",
                principalTable: "Railroads",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserParentAssignments_Railroads_RailroadCtrlNbr",
                table: "UserParentAssignments",
                column: "RailroadCtrlNbr",
                principalTable: "Railroads",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
