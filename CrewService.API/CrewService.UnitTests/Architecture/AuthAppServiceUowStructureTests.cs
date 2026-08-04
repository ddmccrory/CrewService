using Xunit;

namespace CrewService.UnitTests.Architecture;

public sealed class AuthAppServiceUowStructureTests
{
    private const string AuthAppServiceFilePath = @"C:\Projects\CrewService\CrewService.API\CrewService.Application\UserAccess\AuthAppService.cs";

    [Fact]
    public void AcceptInvitationAsync_DoesNotCallUserAccountService_InsideFinalOrchestrationUowScope()
    {
        var lines = File.ReadAllLines(AuthAppServiceFilePath);

        var finalUowLineIndex = Array.FindLastIndex(
            lines,
            line => line.Contains("await using var uow = await uowFactory.CreateAsync", StringComparison.Ordinal));

        Assert.True(finalUowLineIndex >= 0, "Could not locate the final orchestration UoW scope in AuthAppService.AcceptInvitationAsync.");

        var violations = lines
            .Skip(finalUowLineIndex + 1)
            .Select((line, index) => new { LineNumber = finalUowLineIndex + index + 2, Line = line })
            .Where(x => x.Line.Contains("userAccountService.", StringComparison.Ordinal))
            .ToList();

        Assert.True(
            violations.Count == 0,
            "Identity writes/queries must not execute inside the final orchestration UoW scope. Violations: " +
            string.Join(", ", violations.Select(v => $"line {v.LineNumber}")));
    }

    [Fact]
    public void AcceptInvitationAsync_DoesNotContainPlaceholderOrBypassCode()
    {
        var source = File.ReadAllText(AuthAppServiceFilePath);

        Assert.DoesNotContain("placeholder", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("FindByEmailAsync(string.Empty)", source, StringComparison.Ordinal);
    }
}
