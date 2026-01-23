using System.ComponentModel;

namespace DataLayer.EfClasses;

public enum EntryStatus
{
    [Description("В заявке")]
    INS,
    [Description("Зарегистрирован")]
    REG,
    [Description("Дисквалификация")]
    DSQ,
    [Description("Не вышел")]
    DNS,
    [Description("Не финишировал")]
    DNF,
    [Description("Финишировал")]
    FIN,
}