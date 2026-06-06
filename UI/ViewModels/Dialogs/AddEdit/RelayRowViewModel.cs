using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DataLayer.EfClasses;
using UI.Helpers;

namespace UI.ViewModels.Dialogs.AddEdit;

public partial class RelayRowViewModel : ObservableObject
{
    private Action _onChanged;
    private string _entryTimeText = string.Empty;
    private int? _entryTime;

    public RelayRowViewModel(int order, Action onChanged)
    {
        Order = order;
        _onChanged = onChanged;
    }

    public int Order { get; }

    public void SetOnChanged(Action onChanged) => _onChanged = onChanged;

    private SearchableItem? _selectedAthlete;

    public SearchableItem? SelectedAthlete
    {
        get => _selectedAthlete;
        set
        {
            if (SetProperty(ref _selectedAthlete, value))
                _onChanged();
        }
    }

    public int? EntryTime
    {
        get => _entryTime;
        set
        {
            if (!SetProperty(ref _entryTime, value))
                return;

            var formatted = SwimTimeInput.Format(value);
            if (_entryTimeText != formatted)
                SetProperty(ref _entryTimeText, formatted, nameof(EntryTimeText));

            _onChanged();
        }
    }

    public string EntryTimeText
    {
        get => _entryTimeText;
        set
        {
            var update = SwimTimeInput.ApplyText(value);

            if (_entryTimeText != update.Text)
                SetProperty(ref _entryTimeText, update.Text);

            if (_entryTime != update.Hundredths)
            {
                _entryTime = update.Hundredths;
                OnPropertyChanged(nameof(EntryTime));
                _onChanged();
            }
        }
    }

    public void EnsureSelectionIsValid(ObservableCollection<SearchableItem> available)
    {
        if (SelectedAthlete?.Value is not Athlete athlete)
            return;

        var stillExists = available.Any(a => a.Value is Athlete at && at.Id == athlete.Id);
        if (!stillExists)
            SelectedAthlete = null;
    }
}