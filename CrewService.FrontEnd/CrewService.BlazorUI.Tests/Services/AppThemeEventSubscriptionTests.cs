using CrewService.BlazorUI.Services;
using Xunit;

namespace CrewService.BlazorUI.Tests.Services;

public class AppThemeEventSubscriptionTests
{
    [Fact]
    public void ThemeChanged_UnsubscribedHandler_IsNotInvoked()
    {
        var svc = new AppThemeService();
        var calls = 0;

        void Handler() => calls++;

        svc.ThemeChanged += Handler;
        svc.ThemeChanged -= Handler;

        svc.SetTheme("Lumen");

        Assert.Equal(0, calls);
    }

    [Fact]
    public void ThemeChanged_MultipleSubscribers_AllInvoked()
    {
        var svc = new AppThemeService();
        var callsA = 0;
        var callsB = 0;

        svc.ThemeChanged += () => callsA++;
        svc.ThemeChanged += () => callsB++;

        svc.SetMode("Dark");

        Assert.Equal(1, callsA);
        Assert.Equal(1, callsB);
    }

    [Fact]
    public void ThemeChanged_UnsubscribingOneHandler_DoesNotAffectOthers()
    {
        var svc = new AppThemeService();
        var callsA = 0;
        var callsB = 0;

        void HandlerA() => callsA++;
        void HandlerB() => callsB++;

        svc.ThemeChanged += HandlerA;
        svc.ThemeChanged += HandlerB;
        svc.ThemeChanged -= HandlerA;

        svc.SetTheme("Darkly");

        Assert.Equal(0, callsA);
        Assert.Equal(1, callsB);
    }
}
