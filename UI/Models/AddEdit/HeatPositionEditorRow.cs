using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.EntryService;
using ServiceLayer.EventService;
using ServiceLayer.HeatService;
using UI.Helpers;
using UI.Resources;
using UI.Services;
using UI.ViewModels;
using UI.Models;

namespace UI.Models.AddEdit;

public partial class HeatPositionEditorRow : ObservableObject
{
    public ObservableCollection<SearchableItem> AvailableEntries { get; } = new();

    internal bool SuppressSelectionChanges { get; set; }

    [ObservableProperty] private int _lane;
    [ObservableProperty] private int _entryId;
    [ObservableProperty] private SearchableItem? _selectedEntry;
    [ObservableProperty] private IRelayCommand<HeatPositionEditorRow>? _removeCommand;

    public event EventHandler? SelectedEntryChanged;

    partial void OnSelectedEntryChanged(SearchableItem? value)
    {
        if (!SuppressSelectionChanges)
        {
            if (value?.Value is Entry entry)
                EntryId = entry.Id;
            else if (value is null)
                EntryId = 0;

            SelectedEntryChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
