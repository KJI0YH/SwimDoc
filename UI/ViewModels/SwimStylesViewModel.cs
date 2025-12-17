using DataLayer.EfClasses;
using ServiceLayer.SwimStyleService;

namespace UI.ViewModels;

public class SwimStylesViewModel : GenericTableViewModel<SwimStyle, int>
{
    public SwimStylesViewModel(ISwimStyleService swimStyleService) : base(swimStyleService)
    {
    }

    public string Title => "Стили плавания";

    protected override void InitializeColumns()
    {
        base.InitializeColumns();
        AutoGenerateColumns = false;
        ColumnConfigurations.Clear();

        ColumnConfigurations.Add(ColumnConfiguration.Create("Stroke", "Стиль", 150));
        ColumnConfigurations.Add(ColumnConfiguration.Create("Distance", "Дистанция", 150));
    }
}