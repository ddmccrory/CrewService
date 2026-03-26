namespace CrewService.BlazorUI.Services;

public class AppContextService
{
    public long? SelectedParentCtrlNbr { get; private set; }
    public string? SelectedParentName { get; private set; }
    public long? SelectedRailroadCtrlNbr { get; private set; }
    public string? SelectedRailroadName { get; private set; }
    public string? DisplayName { get; set; }

    public bool HasParent => SelectedParentCtrlNbr.HasValue;
    public bool HasRailroad => SelectedRailroadCtrlNbr.HasValue;
    public bool IsFullySelected => HasParent && HasRailroad;

    public event Action? OnContextChanged;

    public void SetDisplayName(string name)
    {
        DisplayName = name;
        OnContextChanged?.Invoke();
    }

    public void SetParent(long ctrlNbr, string name)
    {
        SelectedParentCtrlNbr = ctrlNbr;
        SelectedParentName = name;
        SelectedRailroadCtrlNbr = null;
        SelectedRailroadName = null;
        OnContextChanged?.Invoke();
    }

    public void SetRailroad(long ctrlNbr, string name)
    {
        if (!HasParent)
        {
            return;
        }

        SelectedRailroadCtrlNbr = ctrlNbr;
        SelectedRailroadName = name;
        OnContextChanged?.Invoke();
    }

    /// <summary>
    /// Sets parent and railroad in one batch, firing <see cref="OnContextChanged"/>
    /// only once.  Use when restoring from session to avoid duplicate page reloads.
    /// </summary>
    public void SetContext(long parentCtrlNbr, string parentName, long? railroadCtrlNbr, string? railroadName)
    {
        SelectedParentCtrlNbr = parentCtrlNbr;
        SelectedParentName = parentName;
        SelectedRailroadCtrlNbr = null;
        SelectedRailroadName = null;

        if (railroadCtrlNbr.HasValue && railroadName is not null)
        {
            SelectedRailroadCtrlNbr = railroadCtrlNbr;
            SelectedRailroadName = railroadName;
        }

        OnContextChanged?.Invoke();
    }

    public void ClearRailroad()
    {
        SelectedRailroadCtrlNbr = null;
        SelectedRailroadName = null;
        OnContextChanged?.Invoke();
    }

    public void Clear()
    {
        SelectedParentCtrlNbr = null;
        SelectedParentName = null;
        SelectedRailroadCtrlNbr = null;
        SelectedRailroadName = null;
        OnContextChanged?.Invoke();
    }
}
