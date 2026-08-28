using System.Windows;
using System.Windows.Controls;
using UI.ViewModels.Dialogs.AddEdit;

namespace UI.Views.Dialogs.AddEdit;

public partial class EntryAddEditView : UserControl
{
    public EntryAddEditView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => ApplyTabStripVisibility();

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e) =>
        ApplyTabStripVisibility();

    private void ApplyTabStripVisibility()
    {
        if (DataContext is not EntryViewModel viewModel)
            return;
        if (viewModel.ShowEntryTypeTabs)
            EntryTabControl.ClearValue(TemplateProperty);
        else if (Resources["EntryAddEditTabControlContentOnlyTemplate"] is ControlTemplate contentOnlyTemplate)
            EntryTabControl.Template = contentOnlyTemplate;
    }
}
