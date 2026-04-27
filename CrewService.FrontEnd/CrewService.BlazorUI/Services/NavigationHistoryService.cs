using Microsoft.AspNetCore.Components;

namespace CrewService.BlazorUI.Services;

/// <summary>
/// Scoped service that tracks navigation history so components can navigate back
/// to the previous page without JS interop or per-page hardcoding.
/// </summary>
public class NavigationHistoryService : IDisposable
{
    private readonly NavigationManager _navigationManager;
    private string? _previousUrl;
    private string _currentUrl;

    public NavigationHistoryService(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager;
        _currentUrl = _navigationManager.Uri;
        _navigationManager.LocationChanged += OnLocationChanged;
    }

    private void OnLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
    {
        _previousUrl = _currentUrl;
        _currentUrl = e.Location;
    }

    public void GoBack(string fallbackUrl = "/")
    {
        var target = _previousUrl ?? fallbackUrl;
        _navigationManager.NavigateTo(target);
    }

    public void Dispose()
    {
        _navigationManager.LocationChanged -= OnLocationChanged;
    }
}
