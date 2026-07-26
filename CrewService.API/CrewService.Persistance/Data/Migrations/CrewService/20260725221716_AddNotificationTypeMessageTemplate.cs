using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddNotificationTypeMessageTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MessageTemplate",
                table: "NotificationTypeConfigs",
                type: "TEXT",
                maxLength: 2000,
                nullable: false,
                defaultValue: "{message}");

            migrationBuilder.Sql(@"
UPDATE NotificationTypeConfigs
SET MessageTemplate = CASE Key
    WHEN 'BulletinAward' THEN 'You have been awarded {position} effective {effective}.'
    WHEN 'BulletinLost' THEN 'Your bid for {position} was not awarded.'
    WHEN 'ForceAssign' THEN 'You have been force-assigned to {position} effective {effective}.'
    WHEN 'BulletinCancellation' THEN 'The bulletin for {position} has been cancelled and your bid is no longer active.'
    WHEN 'SeniorityMove' THEN 'You have been assigned to {position} effective {effective}.'
    WHEN 'SeniorityMoveCancelled' THEN 'The seniority move that would have bumped you from {position} has been cancelled.'
    WHEN 'PositionChange' THEN 'You will be bumped from {position}{byClause}, effective {effective}.'
    WHEN 'BoardPlacement' THEN 'You have been placed on {board}.'
    WHEN 'WaitListPromotion' THEN 'Waitlist request was assigned. {absenceCode} absence request was created and approved for {datetime}.'
    WHEN 'TieUp' THEN 'You have an outstanding on-duty record from {assignment} on duty at {onDuty} that requires completion.'
    WHEN 'GeneralInformation' THEN '{message}'
    ELSE '{message}'
END
WHERE MessageTemplate IS NULL OR TRIM(MessageTemplate) = '';
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MessageTemplate",
                table: "NotificationTypeConfigs");
        }
    }
}
