using System.ComponentModel;

namespace DataLayer.EfClasses;

public enum Gender
{
    [Description("Мужчины")] Male,
    [Description("Женщины")] Female,
    [Description("Смешанная")] Mixed
}