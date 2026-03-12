using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class SeedReferenceGroupTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            var groupTypes = new[]
            {
                ("Location", "Operational locations used by FRA segments and billing"),
                ("Zone", "Geographic zones for billing and reporting"),
                ("AFE", "Authorization for Expenditure codes"),
                ("WorkCode", "Work/job classification codes"),
                ("Material", "Material and supply codes"),
                ("LocomotiveType", "Locomotive type classification codes")
            };

            foreach (var (name, description) in groupTypes)
            {
                migrationBuilder.Sql($@"
                    INSERT INTO GroupTypes (CtrlNbr, Name, Description, IsWorkArea, FlagsJson, IsDeleted, CreatedBy_AuditName, CreatedBy_AuditDateTime)
                    SELECT {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + Array.IndexOf(groupTypes, (name, description))},
                           '{name}', '{description}', 0, NULL, 0, 'SYSTEM', '{now}'
                    WHERE NOT EXISTS (SELECT 1 FROM GroupTypes WHERE Name = '{name}')");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
