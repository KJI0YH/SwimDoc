using System.ComponentModel;

namespace DataLayer.EfClasses;

public enum Category
{
    [Description("МСМК")] IMoS,
    [Description("МС")] MoS,
    [Description("КМС")] CMoS,
    [Description("I")] FirstAdult,
    [Description("II")] SecondAdult,
    [Description("III")] ThirdAdult,
    [Description("I юн.")] FirstJunior,
    [Description("II юн.")] SecondJunior,
    [Description("-")] NoCategory
}
