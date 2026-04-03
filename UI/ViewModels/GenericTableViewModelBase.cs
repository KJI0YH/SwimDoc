using System.Collections;
using System.Collections.ObjectModel;
using UI.ViewModels.Generic;

namespace UI.ViewModels;

public abstract class GenericTableViewModelBase : ViewModelBase
{
    public abstract ObservableCollection<ColumnConfiguration> GetColumnConfigurations();

    public abstract bool GetAutoGenerateColumns();

    public abstract void SyncSelectedItemsFromGrid(IList? gridSelection);
}
