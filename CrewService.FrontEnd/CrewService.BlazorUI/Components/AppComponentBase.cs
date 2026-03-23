using CrewService.BlazorUI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace CrewService.BlazorUI.Components;

/// <summary>
/// Base component that provides <see cref="AppContextService"/> awareness,
/// <see cref="CurrentUserService"/> initialization, and common page infrastructure.
/// Pages that inherit from this automatically receive the selected parent/railroad
/// context, re-render when the context switcher changes, and share standard message fields.
/// Override <see cref="OnAppContextChangedAsync"/> to reload scoped data.
/// </summary>
public abstract class AppComponentBase : ComponentBase, IDisposable
{
    [Inject]
    protected AppContextService AppContext { get; set; } = default!;

    [Inject]
    protected NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    protected CurrentUserService CurrentUser { get; set; } = default!;

    [CascadingParameter]
    protected Task<AuthenticationState> AuthStateTask { get; set; } = default!;

    // ── Context convenience properties ──────────────────────────────────

    /// <summary>Current selected parent CtrlNbr, or <c>null</c> if none selected.</summary>
    protected long? SelectedParentCtrlNbr => AppContext.SelectedParentCtrlNbr;

    /// <summary>Current selected parent display name.</summary>
    protected string? SelectedParentName => AppContext.SelectedParentName;

    /// <summary>Current selected railroad CtrlNbr, or <c>null</c> if none selected.</summary>
    protected long? SelectedRailroadCtrlNbr => AppContext.SelectedRailroadCtrlNbr;

    /// <summary>Current selected railroad display name.</summary>
    protected string? SelectedRailroadName => AppContext.SelectedRailroadName;

    /// <summary><c>true</c> when a parent is selected.</summary>
    protected bool HasParent => AppContext.HasParent;

    /// <summary><c>true</c> when a railroad is selected.</summary>
    protected bool HasRailroad => AppContext.HasRailroad;

    /// <summary><c>true</c> when both a parent and railroad are selected.</summary>
    protected bool IsContextFullySelected => AppContext.IsFullySelected;

    // ── Common page state ───────────────────────────────────────────────

    protected string? successMessage;
    protected string? errorMessage;

    // ── Lifecycle ───────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        AppContext.OnContextChanged += HandleContextChanged;
    }

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateTask;
        await CurrentUser.InitializeAsync(authState.User);
    }

    /// <summary>
    /// Called when the user changes the parent or railroad in the context switcher.
    /// Override this to reload data that is scoped to the selected context.
    /// The base implementation calls <see cref="ComponentBase.StateHasChanged"/>.
    /// </summary>
    protected virtual Task OnAppContextChangedAsync() => Task.CompletedTask;

    private void HandleContextChanged()
    {
        InvokeAsync(async () =>
        {
            await OnAppContextChangedAsync();
            StateHasChanged();
        });
    }

    public virtual void Dispose()
    {
        AppContext.OnContextChanged -= HandleContextChanged;
        GC.SuppressFinalize(this);
    }
}
