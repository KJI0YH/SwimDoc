using System.Collections;
using System.Collections.ObjectModel;

namespace UI.ViewModels.Pages.Data;

public abstract class DataViewModelBase : ViewModelBase
{
    public abstract ObservableCollection<ColumnConfiguration> GetColumnConfigurations();

    public abstract bool GetAutoGenerateColumns();

    public abstract void SyncSelectedItemsFromGrid(IList? gridSelection);
}