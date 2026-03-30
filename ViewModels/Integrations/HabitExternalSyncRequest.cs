using HabitTrackerWeb.Core.Enums;

namespace HabitTrackerWeb.ViewModels.Integrations;

public sealed class HabitExternalSyncRequest
{
    public bool AutoCompleteFromExternal { get; set; }
    public ExternalActivitySource ExternalSource { get; set; }
    public string? ExternalMatchKey { get; set; }
}
