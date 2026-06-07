using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using UI.Resources;

namespace UI.Views.Controls.SearchableComboBox;

public partial class SearchableComboBox : UserControl
{
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

    private bool _isSearchActive;
    private bool _isSyncingSelection;
    private bool _isOpeningDropDownForSearch;
    private ICollectionView? _itemsView;
    private ObservableCollection<SearchableItem>? _boundItemsSource;
    private string _searchText = string.Empty;
    public SearchableComboBox()
    {
        InitializeComponent();
        Loaded += (_, _) => HookEditableTextBox();
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
            ComboBoxControl.ItemsSource = null;
            return;
        }
        itemsSource.CollectionChanged += OnBoundItemsSourceChanged;
        _itemsView = new ListCollectionView(itemsSource);
        _itemsView.Filter = FilterItem;
        ComboBoxControl.ItemsSource = _itemsView;
        _itemsView.Refresh();
    }

    private void OnBoundItemsSourceChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _itemsView?.Refresh();
        SyncSelectedItem();
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
    }

    private static bool AreValuesEqual(object? value1, object? value2)
    {
        if (value1 == null && value2 == null) return true;
        if (value1 == null || value2 == null) return false;
        if (ReferenceEquals(value1, value2)) return true;
        if (value1.Equals(value2)) return true;
        var idProperty1 = value1.GetType().GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
        var idProperty2 = value2.GetType().GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
        if (idProperty1 != null && idProperty2 != null &&
            idProperty1.PropertyType == idProperty2.PropertyType)
        {
            var id1 = idProperty1.GetValue(value1);
            var id2 = idProperty2.GetValue(value2);
            if (id1 != null && id2 != null && id1.Equals(id2)) return true;
        }
        return false;
    }

    private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not SearchableComboBox control) return;
        if (control._isSyncingSelection) return;
        if (e.NewValue is SearchableItem) control.SyncSelectedItem();
    }

    private void ComboBoxControl_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Back or Key.Delete)
            BeginSearch(clearSelection: ComboBoxControl.SelectedItem != null);
        if (e.Key == Key.Enter)
        {
            ComboBoxControl.IsDropDownOpen = false;
            e.Handled = true;
        }
    }

    private void ComboBoxControl_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        BeginSearch(clearSelection: ComboBoxControl.SelectedItem != null);
    }

    private void ComboBoxControl_OnDropDownOpened(object sender, EventArgs e)
    {
        if (_isOpeningDropDownForSearch)
        {
            _isOpeningDropDownForSearch = false;
            return;
        }
        _isSearchActive = false;
        _searchText = string.Empty;
        _itemsView?.Refresh();
    }

    private void ComboBoxControl_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_itemsView == null) return;
        if (!_isSearchActive) return;
        var typedText = ComboBoxControl.Text ?? string.Empty;
        _searchText = typedText;
        _itemsView.Refresh();
        if (!ComboBoxControl.IsKeyboardFocusWithin)
            return;
        _isOpeningDropDownForSearch = true;
        ComboBoxControl.IsDropDownOpen = true;
        RestoreEditableText(typedText);
    }

    private void BeginSearch(bool clearSelection)
    {
        _isSearchActive = true;
        if (!clearSelection)
            return;
        _isSyncingSelection = true;
        ComboBoxControl.SelectedItem = null;
        SelectedItem = null;
        _isSyncingSelection = false;
    }

    private void HookEditableTextBox()
    {
        if (GetEditableTextBox() is not { } textBox)
            return;
        textBox.GotFocus += (_, _) =>
        {
            if (_isSearchActive)
                RestoreEditableText(ComboBoxControl.Text ?? string.Empty);
        };
    }

    private TextBox? GetEditableTextBox() =>
        ComboBoxControl.Template?.FindName("PART_EditableTextBox", ComboBoxControl) as TextBox;

    private void RestoreEditableText(string text)
    {
        if (GetEditableTextBox() is not { } textBox)
            return;
        if (textBox.Text != text)
            textBox.Text = text;
        textBox.CaretIndex = text.Length;
        textBox.SelectionLength = 0;
    }

    private bool FilterItem(object obj)
    {
        if (!_isSearchActive || string.IsNullOrWhiteSpace(_searchText)) return true;
        if (obj is not SearchableItem item) return false;
        var text = item.DisplayText ?? string.Empty;
        return text.IndexOf(_searchText, StringComparison.CurrentCultureIgnoreCase) >= 0;
    }
}
