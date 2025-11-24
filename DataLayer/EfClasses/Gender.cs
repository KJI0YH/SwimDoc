using System.ComponentModel;

namespace DataLayer.EfClasses;

public enum Gender
{
    [Description("Муж.")] Male,
    [Description("Жен.")] Female,
    [Description("Комб.")] Mixed
}