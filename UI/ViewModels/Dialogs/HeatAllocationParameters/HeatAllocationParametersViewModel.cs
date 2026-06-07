using System.Collections.ObjectModel;
using System.Collections.Specialized;
using BizLogic.HeatAllocation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UI.Resources;

namespace UI.ViewModels.Dialogs.HeatAllocationParameters;

public partial class HeatAllocationParametersViewModel : ViewModelBase, IWindowResult
{
    [ObservableProperty] private int _minHeatSize = 2;
    [ObservableProperty] private HeatOrder _selectedHeatOrder = HeatOrder.FromWeakToStrong;
    [ObservableProperty] private ObservableCollection<string> _validationErrors = [];
    public HeatAllocationParametersViewModel()
    {
        ValidationErrors.CollectionChanged += OnValidationErrorsChanged;
    }

    public string WindowTitle => Strings.HeatAlloc_WindowTitle;
    public bool HasErrors => ValidationErrors.Count > 0;
    public HeatAllocationParametersResult? Result { get; private set; }
    object? IWindowResult.Result => Result;
    public event EventHandler? CloseRequested;
    private void OnValidationErrorsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasErrors));
    }

    [RelayCommand]
    private void Save()
    {
        ValidationErrors.Clear();
        if (MinHeatSize < 1)
        {
            ValidationErrors.Add(Strings.HeatAlloc_Validation_MinHeatSize);
            return;
        }
        Result = new HeatAllocationParametersResult(SelectedHeatOrder, MinHeatSize);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel()
    {
        Result = null;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
