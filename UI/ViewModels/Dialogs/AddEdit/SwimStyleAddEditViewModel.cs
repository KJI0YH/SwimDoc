using DataLayer.EfClasses;
using ServiceLayer.SwimStyleService;
using UI.Resources;

namespace UI.ViewModels.Dialogs.AddEdit;

public class SwimStyleAddViewModel(int? id, ISwimStyleService crudService)
    : AddEditViewModel<SwimStyle, int?>(id, crudService)
{
    public override string WindowTitle => IsAdd ? Strings.WindowTitle_CreateSwimStyle : Strings.WindowTitle_EditSwimStyle;
    public Stroke Stroke
    {
        get => Entity.Stroke;
        set
        {
            Entity.Stroke = value;
            OnPropertyChanged();
        }
    }

    public int Distance
    {
        get => Entity.Distance;
        set
        {
            Entity.Distance = value;
            OnPropertyChanged();
        }
    }

    public int RelayCount
    {
        get => Entity.RelayCount;
        set
        {
            Entity.RelayCount = value;
            OnPropertyChanged();
        }
    }

    public IEnumerable<EnumOption<Stroke>> StrokeOptions =>
        Enum.GetValues<Stroke>().Select(s => new EnumOption<Stroke>(s));
}
