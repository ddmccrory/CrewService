using CrewService.BlazorUI.Services;
using Xunit;

namespace CrewService.BlazorUI.Tests.Services;

public class AppThemeServiceTests
{
    [Fact]
    public void SetTheme_WhenThemeChanges_RaisesThemeChanged()
    {
        var svc = new AppThemeService();
        var raised = 0;
        svc.ThemeChanged += () => raised++;

        svc.SetTheme("Lumen");

        Assert.Equal(1, raised);
        Assert.Equal("Lumen", svc.ThemeName);
        Assert.Equal("Light and shadow", svc.ThemeDescription);
    }

    [Fact]
    public void SetMode_WhenModeChanges_RaisesThemeChanged()
    {
        var svc = new AppThemeService();
        var raised = 0;
        svc.ThemeChanged += () => raised++;

        svc.SetMode("Dark");

        Assert.Equal(1, raised);
        Assert.Equal("Dark", svc.Mode);
    }

    [Fact]
    public void SetMode_WhenModeDoesNotChange_DoesNotRaiseThemeChanged()
    {
        var svc = new AppThemeService();
        var raised = 0;
        svc.ThemeChanged += () => raised++;

        svc.SetMode("Light");

        Assert.Equal(0, raised);
        Assert.Equal("Light", svc.Mode);
    }

    [Fact]
    public void SetTheme_CapturesPreviousThemeOnlyOnFirstChange()
    {
        var svc = new AppThemeService();

        svc.SetTheme("Lumen");
        svc.SetTheme("Darkly");

        Assert.Equal("Spacelab", svc.PreviousTheme);
        Assert.Equal("Darkly", svc.ThemeName);
    }

    [Fact]
    public void SetMode_CapturesPreviousModeOnlyOnFirstChange()
    {
        var svc = new AppThemeService();

        svc.SetMode("Dark");
        svc.SetMode("Light");

        Assert.Equal("Light", svc.PreviousMode);
        Assert.Equal("Light", svc.Mode);
    }
}
