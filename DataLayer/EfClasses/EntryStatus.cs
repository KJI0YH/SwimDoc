using System.ComponentModel;

namespace DataLayer.EfClasses;

public enum EntryStatus
{
    [Description("Вне программы")] ENTRY,
    [Description("В заявке")] EVENT,
    [Description("В заплыве")] HEAT,
    [Description("Дисквалифицирован")] DSQ,
    [Description("Не стартовал")] DNS,
    [Description("Не финишировал")] DNF,
    [Description("Финишировал")] FINISH,
}