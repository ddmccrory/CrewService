using CrewService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CrewService.Persistance.Data;

/// <summary>
/// Enables <c>dotnet ef</c> commands (migrations add / remove / etc.) for
/// <see cref="CrewServiceDbContext"/>, which is <c>internal sealed</c> and
/// requires <see cref="ICurrentUserService"/> and <see cref="IFieldEncryptor"/>
/// that cannot be resolved at design time.
/// </summary>
internal sealed class DesignTimeCrewServiceDbContextFactory
    : IDesignTimeDbContextFactory<CrewServiceDbContext>
{
    public CrewServiceDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CrewServiceDbContext>();
        optionsBuilder.UseSqlite("Data Source=design_time.db");

        return new CrewServiceDbContext(
            optionsBuilder.Options,
            new StubCurrentUserService(),
            new StubFieldEncryptor());
    }

    /// <summary>Stub — never called during migration scaffolding.</summary>
    private sealed class StubCurrentUserService : ICurrentUserService
    {
        public Guid GetUserId() => Guid.Empty;
        public string GetUserName() => "design-time";
        public void SetAuditOverride(string name) { }
    }

    /// <summary>Stub — encryption converters are registered but never invoked at design time.</summary>
    private sealed class StubFieldEncryptor : IFieldEncryptor
    {
        public string Encrypt(string plainText) => plainText;
        public string Decrypt(string cipherText) => cipherText;
    }
}
