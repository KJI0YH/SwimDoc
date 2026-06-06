using System.ComponentModel;

namespace DataLayer.EfClasses;

public enum EventRound
{
    [Description("Предварительный")]
    PRE,
    [Description("Переплыв предварительный")]
    SOP,
    [Description("Полуфинал")]
    SEM,
    [Description("Переплыв полуфинал")]
    SOS,
    [Description("Финал")]
    FIN
}
