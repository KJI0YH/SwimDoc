using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using UI.Helpers.Controls;
using UI.Resources;

namespace UI.Views.Controls.SearchableComboBox;

public partial class SearchableComboBox : UserControl
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo?> IdPropertyCache = new();
    private static readonly TimeSpan SearchDebounceInterval = TimeSpan.FromMilliseconds(200);

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(ObservableCollection<SearchableItem>),
            typeof(SearchableComboBox), new PropertyMetadata(null, OnItemsSourceChanged));

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(nameof(SelectedItem), typeof(SearchableItem),
            typeof(SearchableComboBox),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSelectedItemChanged));

    public static readonly DependencyProperty DisplayMemberPathProperty =
        DependencyProperty.Register(nameof(DisplayMemberPath), typeof(string),
            typeof(SearchableComboBox), new PropertyMetadata("DisplayText"));

    public static readonly DependencyProperty WatermarkProperty =
        DependencyProperty.Register(nameof(Watermark), typeof(string),
            typeof(SearchableComboBox), new PropertyMetadata(Strings.Common_SearchPlaceholder));

    private readonly DispatcherTimer _searchRefreshTimer;
    private bool _isSearchActive;
    private bool _isSyncingSelection;
    private bool _preserveNullSelection = true;
    private bool _isUpdatingText;
    private bool _isOpeningDropDownForSearch;
    private ICollectionView? _itemsView;
    private ObservableCollection<SearchableItem>? _boundItemsSource;
    private string _searchText = string.Empty;

    public SearchableComboBox()
    {
        InitializeComponent();
        _searchRefreshTimer = new DispatcherTimer { Interval = SearchDebounceInterval };
        _searchRefreshTimer.Tick += OnSearchRefreshTimerTick;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        HookEditableTextBox();
        if (ItemsSource is not null && _itemsView is null)
            BindItemsView(ItemsSource);
        else
            SyncComboBoxSelection();
        // WPF may auto-select the first item after ItemsSource is applied; re-sync once layout is done.
        Dispatcher.BeginInvoke(SyncComboBoxSelection, DispatcherPriority.Loaded);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _searchRefreshTimer.Stop();
    }

    public ObservableCollection<SearchableItem>? ItemsSource
    {
        get => (ObservableCollection<SearchableItem>?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public SearchableItem? SelectedItem
    {
        get => (SearchableItem?)GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public string DisplayMemberPath
    {
        get => (string)GetValue(DisplayMemberPathProperty);
        set => SetValue(DisplayMemberPathProperty, value);
    }

    public string Watermark
    {
        get => (string)GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not SearchableComboBox control) return;
        control.BindItemsView(e.NewValue as ObservableCollection<SearchableItem>);
        control.SyncSelectedItem();
    }

    private void BindItemsView(ObservableCollection<SearchableItem>? itemsSource)
    {
        if (_boundItemsSource is not null)
            _boundItemsSource.CollectionChanged -= OnBoundItemsSourceChanged;
        _boundItemsSource = itemsSource;
        if (itemsSource == null)
        {
            _itemsView = null;
            _isSyncingSelection = true;
            try
            {
                ComboBoxControl.ItemsSource = null;
                ComboBoxControl.SelectedItem = null;
                ComboBoxControl.SelectedIndex = -1;
            }
            finally
            {
                _isSyncingSelection = false;
            }
            return;
        }
        itemsSource.CollectionChanged += OnBoundItemsSourceChanged;
        _itemsView = new ListCollectionView(itemsSource);
        _itemsView.Filter = FilterItem;
        _isSyncingSelection = true;
        try
        {
            ComboBoxControl.ItemsSource = _itemsView;
            RefreshFilterNow();
        }
        finally
        {
            _isSyncingSelection = false;
        }
        SyncComboBoxSelection();
        ScheduleNullSelectionEnforcement();
    }

    private void ScheduleNullSelectionEnforcement()
    {
        if (!_preserveNullSelection || SelectedItem is not null)
            return;
        Dispatcher.BeginInvoke(EnforceNullSelection, DispatcherPriority.Loaded);
    }

    private void EnforceNullSelection()
    {
        if (!_preserveNullSelection || SelectedItem is not null || _isSearchActive)
            return;
        if (ComboBoxControl.SelectedItem is null &&
            string.IsNullOrEmpty(ComboBoxControl.Text))
            return;
        _isSyncingSelection = true;
        try
        {
            ComboBoxControl.SelectedItem = null;
            if (!string.IsNullOrEmpty(ComboBoxControl.Text))
                RestoreEditableText(string.Empty);
        }
        finally
        {
            _isSyncingSelection = false;
        }
    }

    private void SyncComboBoxSelection()
    {
        if (_itemsView is null || _isSearchActive)
            return;
        _isSyncingSelection = true;
        try
        {
            if (SelectedItem is null)
            {
                ComboBoxControl.SelectedItem = null;
                ComboBoxControl.SelectedIndex = -1;
                if (!string.IsNullOrEmpty(ComboBoxControl.Text))
                    RestoreEditableText(string.Empty);
                return;
            }

            var itemInSource = ItemsSource?.FirstOrDefault(i =>
                ReferenceEquals(i, SelectedItem) ||
                AreValuesEqual(i.Value, SelectedItem.Value));
            if (itemInSource is null)
            {
                ComboBoxControl.SelectedItem = null;
                ComboBoxControl.SelectedIndex = -1;
                return;
            }

            if (!ReferenceEquals(SelectedItem, itemInSource))
                SelectedItem = itemInSource;
            ComboBoxControl.SelectedItem = itemInSource;
            UpdateDisplayTextFromSelection(itemInSource);
        }
        finally
        {
            _isSyncingSelection = false;
        }
    }

    private void UpdateDisplayTextFromSelection(SearchableItem? item)
    {
        if (_isSearchActive || item is null)
            return;
        var text = item.DisplayText ?? string.Empty;
        if (ComboBoxControl.Text != text)
            RestoreEditableText(text);
    }

    private void OnBoundItemsSourceChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // ListCollectionView already applies incremental CollectionChanged updates.
        // A full Refresh here on every Add during bulk rebuilds is O(n²) and freezes the UI.
        if (_isSearchActive)
            return;
        if (e.Action is NotifyCollectionChangedAction.Reset or NotifyCollectionChangedAction.Replace)
            SyncComboBoxSelection();
        ScheduleNullSelectionEnforcement();
    }

    private void SyncSelectedItem()
    {
        if (_isSyncingSelection || SelectedItem == null || ItemsSource == null) return;
        var itemInSource = ItemsSource.FirstOrDefault(i =>
            ReferenceEquals(i, SelectedItem) ||
            AreValuesEqual(i.Value, SelectedItem.Value));
        if (itemInSource != null && !ReferenceEquals(SelectedItem, itemInSource))
        {
            _isSyncingSelection = true;
            SelectedItem = itemInSource;
            _isSyncingSelection = false;
        }

        SyncComboBoxSelection();
    }

    private static bool AreValuesEqual(object? value1, object? value2)
    {
        if (value1 == null && value2 == null) return true;
        if (value1 == null || value2 == null) return false;
        if (ReferenceEquals(value1, value2)) return true;
        if (value1.Equals(value2)) return true;
        var idProperty1 = GetIdProperty(value1.GetType());
        var idProperty2 = GetIdProperty(value2.GetType());
        if (idProperty1 != null && idProperty2 != null &&
            idProperty1.PropertyType == idProperty2.PropertyType)
        {
            var id1 = idProperty1.GetValue(value1);
            var id2 = idProperty2.GetValue(value2);
            if (id1 != null && id2 != null && id1.Equals(id2)) return true;
        }
        return false;
    }

    private static PropertyInfo? GetIdProperty(Type type) =>
        IdPropertyCache.GetOrAdd(type,
            static t => t.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance));

    private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not SearchableComboBox control) return;
        if (control._isSyncingSelection) return;
        control._preserveNullSelection = e.NewValue is null;
        if (e.NewValue is SearchableItem item)
        {
            control.ResetSearchState();
            control.SyncSelectedItem();
            control.UpdateDisplayTextFromSelection(item);
            return;
        }
        if (!control._isSearchActive)
            control.SyncComboBoxSelection();
    }

    private void ComboBoxControl_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Back or Key.Delete)
        {
            _preserveNullSelection = false;
            _isSearchActive = true;
        }
        if (e.Key == Key.Enter)
        {
            ComboBoxControl.IsDropDownOpen = false;
            e.Handled = true;
        }
    }

    private void ComboBoxControl_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        _preserveNullSelection = false;
        _isSearchActive = true;
    }

    private void ComboBoxControl_OnDropDownOpened(object sender, EventArgs e)
    {
        if (_isOpeningDropDownForSearch)
        {
            _isOpeningDropDownForSearch = false;
            return;
        }
        _preserveNullSelection = false;
        ResetSearchState();
    }

    private void ComboBoxControl_OnDropDownClosed(object sender, EventArgs e)
    {
        if (!_isSearchActive)
            return;
        ResetSearchState();
        SyncComboBoxSelection();
    }

    private void ComboBoxControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncingSelection)
            return;
        if (_preserveNullSelection && ComboBoxControl.SelectedItem is SearchableItem)
        {
            EnforceNullSelection();
            return;
        }
        if (ComboBoxControl.SelectedItem is SearchableItem selected)
        {
            if (SelectedItem is SearchableItem bound &&
                !ReferenceEquals(selected, bound) &&
                !AreValuesEqual(selected.Value, bound.Value) &&
                !ComboBoxControl.IsDropDownOpen &&
                !_isSearchActive)
            {
                SyncComboBoxSelection();
                return;
            }

            _preserveNullSelection = false;
            ResetSearchState();
            var itemInSource = ItemsSource?.FirstOrDefault(i =>
                                    ReferenceEquals(i, selected) ||
                                    AreValuesEqual(i.Value, selected.Value))
                                ?? selected;
            if (!ReferenceEquals(SelectedItem, itemInSource))
                SelectedItem = itemInSource;
            UpdateDisplayTextFromSelection(itemInSource);
            return;
        }
        if (ComboBoxControl.SelectedItem is null && SelectedItem is not null && !_isSearchActive && !_isSyncingSelection)
        {
            var selectedStillAvailable = ItemsSource?.Any(item =>
                ReferenceEquals(item, SelectedItem) ||
                AreValuesEqual(item.Value, SelectedItem.Value)) == true;
            if (!selectedStillAvailable)
                SelectedItem = null;
        }
    }

    private void ComboBoxControl_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_itemsView == null || _isUpdatingText) return;
        if (!_isSearchActive) return;
        var typedText = ComboBoxControl.Text ?? string.Empty;
        if (typedText == _searchText)
        {
            ScheduleCaretToEnd(typedText);
            return;
        }
        _searchText = typedText;
        ClearComboBoxSelectionDuringSearch();
        typedText = ComboBoxControl.Text ?? _searchText;
        _searchText = typedText;
        if (string.IsNullOrWhiteSpace(_searchText))
            RefreshFilterNow();
        else
            ScheduleSearchRefresh();
        if (!ComboBoxControl.IsKeyboardFocusWithin)
            return;
        _isOpeningDropDownForSearch = true;
        ComboBoxControl.IsDropDownOpen = true;
        ScheduleCaretToEnd(typedText);
    }

    private void ScheduleSearchRefresh()
    {
        _searchRefreshTimer.Stop();
        _searchRefreshTimer.Start();
    }

    private void OnSearchRefreshTimerTick(object? sender, EventArgs e)
    {
        _searchRefreshTimer.Stop();
        _itemsView?.Refresh();
    }

    private void RefreshFilterNow()
    {
        _searchRefreshTimer.Stop();
        _itemsView?.Refresh();
    }

    private void ClearComboBoxSelectionDuringSearch()
    {
        if (!_isSearchActive || ComboBoxControl.SelectedItem == null)
            return;
        var preservedText = ComboBoxControl.Text ?? _searchText;
        _isSyncingSelection = true;
        try
        {
            ComboBoxControl.SelectedItem = null;
        }
        finally
        {
            _isSyncingSelection = false;
        }
        if (!string.IsNullOrEmpty(preservedText) && ComboBoxControl.Text != preservedText)
            RestoreEditableText(preservedText);
    }

    private void ResetSearchState()
    {
        _isSearchActive = false;
        _searchText = string.Empty;
        RefreshFilterNow();
    }

    private void HookEditableTextBox()
    {
        ComboBoxControl.ApplyTemplate();
        EditableComboBoxBehavior.SetIsEnabled(ComboBoxControl, true);
        if (GetEditableTextBox() is not { } textBox)
            return;
        textBox.GotFocus += (_, _) =>
        {
            if (_isSearchActive)
                RestoreEditableText(ComboBoxControl.Text ?? string.Empty);
        };
    }

    private void ScheduleCaretToEnd(string text)
    {
        if (GetEditableTextBox() is not { } textBox)
            return;
        textBox.Dispatcher.BeginInvoke(
            () =>
            {
                if (!_isSearchActive)
                    return;
                RestoreEditableText(text);
            },
            DispatcherPriority.Input);
    }

    private TextBox? GetEditableTextBox() =>
        ComboBoxControl.Template?.FindName("PART_EditableTextBox", ComboBoxControl) as TextBox;

    private void RestoreEditableText(string text)
    {
        if (GetEditableTextBox() is not { } textBox)
            return;
        _isUpdatingText = true;
        try
        {
            if (textBox.Text != text)
                textBox.Text = text;
            EditableComboBoxBehavior.MoveCaretToEnd(textBox);
        }
        finally
        {
            _isUpdatingText = false;
        }
    }

    private bool FilterItem(object obj)
    {
        if (!_isSearchActive || string.IsNullOrWhiteSpace(_searchText)) return true;
        if (obj is not SearchableItem item) return false;
        var text = item.DisplayText ?? string.Empty;
        return text.IndexOf(_searchText, StringComparison.CurrentCultureIgnoreCase) >= 0;
    }
}
