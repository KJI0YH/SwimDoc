using System.ComponentModel;

namespace DataLayer.EfClasses;

public enum EventRound
{
    [Description("Предварительный")]
    PRE,
    [Description("Отборочный")]
    SOP,
    [Description("Полуфинал")]
    SEM,
    [Description("Доп. отбор")]
    SOS,
    [Description("Финал")]
    FIN
}