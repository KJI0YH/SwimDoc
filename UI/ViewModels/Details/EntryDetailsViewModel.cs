using ServiceLayer.EntryService;
using ServiceLayer.EntryDocumentReaderService;
using UI.Services;
using UI.ViewModels.Generic;

namespace UI.ViewModels.Details;

public partial class EntryDetailsViewModel : ViewModelBase, INavigationAware
{
    private readonly EntriesByIdViewModel _entryTable;
    public ViewModelBase EntryTable => _entryTable;

    public EntryDetailsViewModel(IEntryService entryService, IEntryDocumentReaderService entryDocumentReaderService)
    {
        _entryTable = new EntriesByIdViewModel(entryService, entryDocumentReaderService);
    }

    public void OnNavigatedTo(object? parameter)
    {
        if (parameter is not int idValue)
            return;

        _entryTable.SetEntryId(idValue);
    }
}

