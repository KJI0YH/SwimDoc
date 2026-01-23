using System.ComponentModel;

namespace DataLayer.EfClasses;

public enum ClubType
{
    [Description("Команда")] Club,
    [Description("Национальная команда")] NationalTeam,
    [Description("Областная команда")] RegionalTeam,
    [Description("Неопределено")] Unattached
}