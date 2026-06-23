using CommunityToolkit.Mvvm.ComponentModel;

namespace UI.ViewModels;

public class ViewModelBase : ObservableObject
{
    public virtual double ContentWidth => 520;
}
