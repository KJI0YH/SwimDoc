using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using UI.ViewModels.Pages;

namespace UI.ViewModels.Pages.Data;

public interface IParticipantResultsViewModel
{
    ObservableCollection<ParticipantResultEntryView> Results { get; }
    ParticipantResultEntryView? SelectedResult { get; set; }
    IRelayCommand OpenEventResultsCommand { get; }
}
