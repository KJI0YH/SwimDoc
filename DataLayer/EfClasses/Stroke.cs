using System.ComponentModel;

namespace DataLayer.EfClasses;

public enum Stroke
{
    [Description("Баттерфляй")] Fly,
    [Description("На спине")] Back,
    [Description("Брасс")] Breast,
    [Description("Вольный стиль")] Free,
    [Description("Комплексное плавание")] Medley
}
