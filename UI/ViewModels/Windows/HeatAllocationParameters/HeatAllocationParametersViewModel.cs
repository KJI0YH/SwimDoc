using System.Collections.ObjectModel;
using System.Collections.Specialized;
using BizLogic.HeatLogic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UI.Services;

namespace UI.ViewModels.Windows.HeatAllocationParameters;

public sealed class HeatAllocationParametersResult(HeatOrder heatOrder, int minHeatSize)
{
    public HeatOrder HeatOrder { get; } = heatOrder;
    public int MinHeatSize { get; } = minHeatSize;
}

public partial class HeatAllocationParametersViewModel : ViewModelBase, IWindowResult
{
    [ObservableProperty] private int _minHeatSize = 2;
    [ObservableProperty] private HeatOrder _selectedHeatOrder = HeatOrder.FromWeakToStrong;
    [ObservableProperty] private ObservableCollection<string> _validationErrors = [];

    public HeatAllocationParametersViewModel()
    {
        ValidationErrors.CollectionChanged += OnValidationErrorsChanged;
    }

    public string WindowTitle => "Параметры формирования заплывов";

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
            ValidationErrors.Add("Минимальный размер заплыва должен быть больше или равен 1.");
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