using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class UnifyEmployeeNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChangeNotifications");

            migrationBuilder.DropTable(
                name: "NotificationResponses");

            migrationBuilder.DropTable(
                name: "NotificationRequests");

            migrationBuilder.CreateTable(
                name: "EmployeeNotifications",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    RailroadCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SubjectType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    SubjectCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    Message = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    EffectiveAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RequiresAcknowledgement = table.Column<bool>(type: "INTEGER", nullable: false),
                    Audience = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IncludeInHistory = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeNotifications", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_EmployeeNotifications_DynamicGroups_RailroadCtrlNbr",
                        column: x => x.RailroadCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeNotifications_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VacancyCallRequests",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    PositionSlotCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    TemplateType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
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
                    table.PrimaryKey("PK_VacancyCallRequests", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_VacancyCallRequests_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VacancyCallRequests_PositionSlots_PositionSlotCtrlNbr",
                        column: x => x.PositionSlotCtrlNbr,
                        principalTable: "PositionSlots",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationAcknowledgements",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeNotificationCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    NotifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Method = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Confirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
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
                    table.PrimaryKey("PK_NotificationAcknowledgements", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_NotificationAcknowledgements_EmployeeNotifications_EmployeeNotificationCtrlNbr",
                        column: x => x.EmployeeNotificationCtrlNbr,
                        principalTable: "EmployeeNotifications",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VacancyCallResponses",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    VacancyCallRequestCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ResponseType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DeviceType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
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
                    table.PrimaryKey("PK_VacancyCallResponses", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_VacancyCallResponses_VacancyCallRequests_VacancyCallRequestCtrlNbr",
                        column: x => x.VacancyCallRequestCtrlNbr,
                        principalTable: "VacancyCallRequests",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeNotifications_EmployeeCtrlNbr",
                table: "EmployeeNotifications",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeNotifications_RailroadCtrlNbr",
                table: "EmployeeNotifications",
                column: "RailroadCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationAcknowledgements_EmployeeNotificationCtrlNbr",
                table: "NotificationAcknowledgements",
                column: "EmployeeNotificationCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_VacancyCallRequests_EmployeeCtrlNbr",
                table: "VacancyCallRequests",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_VacancyCallRequests_PositionSlotCtrlNbr",
                table: "VacancyCallRequests",
                column: "PositionSlotCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_VacancyCallResponses_VacancyCallRequestCtrlNbr",
                table: "VacancyCallResponses",
                column: "VacancyCallRequestCtrlNbr");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationAcknowledgements");

            migrationBuilder.DropTable(
                name: "VacancyCallResponses");

            migrationBuilder.DropTable(
                name: "EmployeeNotifications");

            migrationBuilder.DropTable(
                name: "VacancyCallRequests");

            migrationBuilder.CreateTable(
                name: "ChangeNotifications",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ChangeType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    WorkAreaGroupCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeNotifications", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_ChangeNotifications_DynamicGroups_WorkAreaGroupCtrlNbr",
                        column: x => x.WorkAreaGroupCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationRequests",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    PositionSlotCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    TemplateType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationRequests", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_NotificationRequests_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotificationRequests_PositionSlots_PositionSlotCtrlNbr",
                        column: x => x.PositionSlotCtrlNbr,
                        principalTable: "PositionSlots",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationResponses",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeviceType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    NotificationRequestCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ResponseType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationResponses", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_NotificationResponses_NotificationRequests_NotificationRequestCtrlNbr",
                        column: x => x.NotificationRequestCtrlNbr,
                        principalTable: "NotificationRequests",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChangeNotifications_WorkAreaGroupCtrlNbr",
                table: "ChangeNotifications",
                column: "WorkAreaGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRequests_EmployeeCtrlNbr",
                table: "NotificationRequests",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRequests_PositionSlotCtrlNbr",
                table: "NotificationRequests",
                column: "PositionSlotCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationResponses_NotificationRequestCtrlNbr",
                table: "NotificationResponses",
                column: "NotificationRequestCtrlNbr");
        }
    }
}
