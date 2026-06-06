using System.ComponentModel;

namespace DataLayer.EfClasses;

public enum Gender
{
    [Description("Женщины")] Female,  
    [Description("Мужчины")] Male,
    [Description("Смешанная")] Mixed
}