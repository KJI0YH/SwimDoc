using System.ComponentModel;

namespace DataLayer.EfClasses;

public enum HeatStatus
{
    [Description("Рассев")]
    SEEDED,
    [Description("Неофициальный")]
    INOFFICIAL,
    [Description("Официальный")]
    OFFICIAL
}