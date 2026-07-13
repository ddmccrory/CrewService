using CrewService.BlazorUI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace CrewService.BlazorUI.Components;

/// <summary>
/// Base component that provides <see cref="AppContextService"/> awareness,
/// <see cref="CurrentUserService"/> initialization, permission-based authorization,
/// and common page infrastructure.
/// Pages that inherit from this automatically receive the selected parent/railroad
/// context, re-render when the context switcher changes, and share standard message fields.
/// Override <see cref="OnAppContextChangedAsync"/> to reload scoped data.
/// Set <see cref="FeatureKey"/> to enable <see cref="IsFeatureReadOnly"/> and
/// <see cref="RequireFeatureAccess"/> enforcement.
/// </summary>
public abstract class AppComponentBase : ComponentBase, IDisposable
{
    [Inject]
    protected AppContextService AppContext { get; set; } = default!;

    [Inject]
    protected NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    protected CurrentUserService CurrentUser { get; set; } = default!;

    [Inject]
    protected UserPermissionService Permissions { get; set; } = default!;

    [Inject]
    protected CircuitBootstrapService CircuitBootstrap { get; set; } = default!;

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

    // ── Permission properties ───────────────────────────────────────────

    /// <summary>
    /// The feature key for this page (e.g. <c>"daily/call-board"</c>).
    /// Override in derived pages to enable permission enforcement.
    /// When <c>null</c>, permission checks are skipped.
    /// </summary>
    protected virtual string? FeatureKey => null;

    /// <summary>
    /// <c>true</c> when the user has ReadOnly access (not FullAccess) to this page's feature.
    /// Use this to hide create/edit/delete controls in the UI.
    /// Always <c>false</c> when <see cref="FeatureKey"/> is <c>null</c>.
    /// </summary>
    protected bool IsFeatureReadOnly =>
        FeatureKey is not null && Permissions.IsReadOnly(FeatureKey);

    // ── Common page state ───────────────────────────────────────────────

    protected string? successMessage;
    protected string? errorMessage;
    protected bool isSaving;

    // ── Lifecycle ───────────────────────────────────────────────────────

    // ── Initialization guard ────────────────────────────────────────────

    /// <summary>
    /// <c>true</c> until <see cref="LoadDataAsync"/> completes for the first time.
    /// Use this in page templates to suppress the "no context selected" warning
    /// during the brief window where the context switcher hasn't yet restored from session.
    /// </summary>
    protected bool IsInitializing { get; private set; } = true;

    protected override void OnInitialized()
    {
        AppContext.OnContextChanged += HandleContextChanged;
    }

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateTask;

        // gRPC calls require an interactive circuit; skip during prerender
        // to avoid wasted round-trips whose results are discarded.
        if (RendererInfo.IsInteractive)
        {
            // Bootstrap seeds CurrentUser, catalogs, permissions, and context
            // options in a single gRPC call. If it fails, the individual calls
            // below run normally as a fallback.
            await CircuitBootstrap.EnsureInitializedAsync(authState.User);
            await CurrentUser.InitializeAsync(authState.User);
            await Permissions.InitializeAsync(authState.User);
            await Permissions.LoadPermissionsAsync(SelectedParentCtrlNbr);
            await LoadDataAsync();
        }

        IsInitializing = false;
    }

    /// <summary>
    /// Override to load page data after the user and permissions are initialized.
    /// Only called during interactive render (skipped during prerender to avoid
    /// unnecessary failed gRPC round-trips).
    /// </summary>
    protected virtual Task LoadDataAsync() => Task.CompletedTask;

    /// <summary>
    /// Called when the user changes the parent or railroad in the context switcher.
    /// Override this to reload data that is scoped to the selected context.
    /// The base implementation calls <see cref="ComponentBase.StateHasChanged"/>.
    /// </summary>
    protected virtual Task OnAppContextChangedAsync() => Task.CompletedTask;

    private readonly CancellationTokenSource _cts = new();

    /// <summary>
    /// Cancelled when the component is disposed. Pass to gRPC/async calls in derived pages
    /// so in-flight operations abort on navigation rather than rendering into a disposed component.
    /// </summary>
    protected CancellationToken ComponentToken => _cts.Token;

    protected override bool ShouldRender() => !_cts.IsCancellationRequested;

    private void HandleContextChanged()
    {
        if (_cts.IsCancellationRequested) return;
        InvokeAsync(async () =>
        {
            if (_cts.IsCancellationRequested) return;
            await Permissions.LoadPermissionsAsync(SelectedParentCtrlNbr);
            await OnAppContextChangedAsync();
            if (!_cts.IsCancellationRequested) StateHasChanged();
        });
    }

    // ── Feature enforcement helpers ─────────────────────────────────────

    /// <summary>
    /// Checks that the user has at least ReadOnly access to this page's
    /// <see cref="FeatureKey"/>. If not, navigates to <c>/Account/AccessDenied</c>.
    /// Call from <see cref="ComponentBase.OnInitializedAsync"/> after <c>base</c>.
    /// No-op when <see cref="FeatureKey"/> is <c>null</c>.
    /// </summary>
    protected void RequireFeatureAccess()
    {
        // During prerender, navigation can throw NavigationException on server circuits.
        // Defer feature-based redirects until interactive execution.
        if (!RendererInfo.IsInteractive)
            return;

        if (FeatureKey is not null && !Permissions.HasAccess(FeatureKey))
        {
            NavigationManager.NavigateTo("Account/AccessDenied");
        }
    }

    // ── Formatting helpers ──────────────────────────────────────────────

    /// <summary>Formats an ISO 8601 UTC date string as <c>MM/dd/yyyy</c> for display, or "—" when empty.</summary>
    protected static string FormatDate(string? isoUtc)
    {
        if (!string.IsNullOrWhiteSpace(isoUtc) && DateTime.TryParse(isoUtc, out var dt))
            return dt.ToString("MM/dd/yyyy");
        return "\u2014";
    }

    protected static string FormatDisplayLocal(string? iso)
        => DateTimeOffset.TryParse(iso, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.RoundtripKind, out var dto)
            ? dto.DateTime.ToString("MM/dd/yyyy HH:mm")
            : (string.IsNullOrWhiteSpace(iso) ? "\u2014" : iso);

    protected static string FormatDisplayLocalDate(string? iso)
        => DateTimeOffset.TryParse(iso, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.RoundtripKind, out var dto)
            ? dto.DateTime.ToString("MM/dd/yyyy")
            : (string.IsNullOrWhiteSpace(iso) ? "\u2014" : iso);

    public virtual void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        AppContext.OnContextChanged -= HandleContextChanged;
        GC.SuppressFinalize(this);
    }
}
