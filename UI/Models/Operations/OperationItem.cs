using CommunityToolkit.Mvvm.ComponentModel;
using UI.Resources;

namespace UI.Models.Operations;

public partial class OperationItem : ObservableObject
{
    public OperationItem(string eventName) => EventName = eventName;

    [ObservableProperty] private string _eventName;
    [ObservableProperty] private bool _isDetailsOpen;
    [ObservableProperty] private bool _isSummaryRow;
    [ObservableProperty] private OperationItemStatus _status = OperationItemStatus.Pending;
    [ObservableProperty] private IReadOnlyList<string> _warnings = [];
    [ObservableProperty] private IReadOnlyList<string> _errors = [];
    [ObservableProperty] private int _warningsCount;
    [ObservableProperty] private int _errorsCount;
    [ObservableProperty] private int? _heatsCreatedCount;

    public string? HeatsCreatedDisplay =>
        HeatsCreatedCount.HasValue ? HeatsCreatedCount.Value.ToString() : null;

    public string StatusDisplay =>
        IsSummaryRow ? string.Empty : Strings.GetEnumDisplay(Status);

    partial void OnHeatsCreatedCountChanged(int? value) =>
        OnPropertyChanged(nameof(HeatsCreatedDisplay));

    partial void OnStatusChanged(OperationItemStatus value) =>
        OnPropertyChanged(nameof(StatusDisplay));

    partial void OnIsSummaryRowChanged(bool value) =>
        OnPropertyChanged(nameof(StatusDisplay));
}
