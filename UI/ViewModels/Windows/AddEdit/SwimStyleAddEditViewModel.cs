using DataLayer.EfClasses;
using ServiceLayer.SwimStyleService;

namespace UI.ViewModels.Windows.AddEdit;

public class SwimStyleAddViewModel(int? id, ISwimStyleService crudService)
    : AddEditViewModel<SwimStyle, int?>(id, crudService)
{
    public override string WindowTitle => IsAdd ? "Создание стиля плавания" : "Редактирование стиля плавания";

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

    public Array StrokeValues => Enum.GetValues<Stroke>();
}