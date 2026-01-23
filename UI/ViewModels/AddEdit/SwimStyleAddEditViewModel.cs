using CommunityToolkit.Mvvm.ComponentModel;
using DataLayer.EfClasses;
using ServiceLayer.Crud;
using ServiceLayer.SwimStyleService;
using UI.ViewModels.Generic;

namespace UI.ViewModels.AddEdit;

public partial class SwimStyleAddAddEditViewModel(int? id, ISwimStyleService crudService)
    : GenericAddEditViewModel<SwimStyle, int?>(id, crudService)
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
