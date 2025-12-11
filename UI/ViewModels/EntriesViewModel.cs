using ServiceLayer.EntryService;

namespace UI.ViewModels;

public class EntriesViewModel : ViewModelBase
{
    private readonly IEntryService _entryService;

    public EntriesViewModel(IEntryService entryService)
    {
        _entryService = entryService;
    }

    public string Title => "Entries Page";
}

