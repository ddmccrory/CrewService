namespace CrewService.BlazorUI.Services;

public class AppContextService
{
    public long? SelectedParentCtrlNbr { get; private set; }
    public string? SelectedParentName { get; private set; }
    public long? SelectedRailroadCtrlNbr { get; private set; }
    public string? SelectedRailroadName { get; private set; }

    public bool HasParent => SelectedParentCtrlNbr.HasValue;
    public bool HasRailroad => SelectedRailroadCtrlNbr.HasValue;
    public bool IsFullySelected => HasParent && HasRailroad;

    public event Action? OnContextChanged;

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

    public void Clear()
    {
        SelectedParentCtrlNbr = null;
        SelectedParentName = null;
        SelectedRailroadCtrlNbr = null;
        SelectedRailroadName = null;
        OnContextChanged?.Invoke();
    }
}
