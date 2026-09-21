using System.Globalization;
using DataLayer.EfClasses;

namespace DataLayer.Display;

public static class CategoryDisplay
{
    public static readonly Category[] FromHighest =
    [
        Category.IMoS,
        Category.MoS,
        Category.CMoS,
        Category.FirstAdult,
        Category.SecondAdult,
        Category.ThirdAdult,
        Category.FirstJunior,
        Category.SecondJunior
    ];

    public static string Format(Category? category, CultureInfo? culture = null)
    {
        if (category is null or Category.NoCategory)
            return string.Empty;
        var isRu = (culture ?? CultureInfo.CurrentUICulture).TwoLetterISOLanguageName
            .Equals("ru", StringComparison.OrdinalIgnoreCase);
        return category.Value switch
        {
            Category.IMoS => isRu ? "МСМК" : "MSMK",
            Category.MoS => isRu ? "МС" : "MS",
            Category.CMoS => isRu ? "КМС" : "CMS",
            Category.FirstAdult => "I",
            Category.SecondAdult => "II",
            Category.ThirdAdult => "III",
            Category.FirstJunior => isRu ? "I юн." : "I Jun.",
            Category.SecondJunior => isRu ? "II юн." : "II Jun.",
            _ => string.Empty
        };
    }
}
