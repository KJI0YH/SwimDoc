using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using UI.ViewModels.Pages;
using BaseTimesView = UI.Views.Controls.BaseTimesSettingsView.BaseTimesSettingsView;

namespace UI.Views.Controls.ScoringSettingsView;

public partial class ScoringSettingsView : UserControl
{
    public ScoringSettingsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += (_, _) => EnsureBaseTimesHost();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyPropertyChanged oldVm)
            oldVm.PropertyChanged -= OnViewModelPropertyChanged;
        if (e.NewValue is INotifyPropertyChanged newVm)
            newVm.PropertyChanged += OnViewModelPropertyChanged;
        EnsureBaseTimesHost();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SettingsViewModel.IsWorldAquaticsMode)
            or nameof(SettingsViewModel.SelectedScoringMode))
            EnsureBaseTimesHost();
    }

    private void EnsureBaseTimesHost()
    {
        if (BaseTimesHost is null)
            return;
        if (DataContext is not SettingsViewModel vm || !vm.IsWorldAquaticsMode)
            return;
        if (BaseTimesHost.Content is not null)
            return;

        vm.BaseTimes.EnsureLoaded();
        BaseTimesHost.Content = new BaseTimesView
        {
            DataContext = vm.BaseTimes,
            ShowHeader = false,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
    }
}
