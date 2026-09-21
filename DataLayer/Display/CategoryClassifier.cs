using DataLayer.EfClasses;

namespace DataLayer.Display;

public static class CategoryClassifier
{
    public static Category? Classify(int finishTimeHundredths, IReadOnlyDictionary<Category, int> norms)
    {
        if (finishTimeHundredths <= 0 || norms.Count == 0)
            return null;
        foreach (var category in CategoryDisplay.FromHighest)
        {
            if (!norms.TryGetValue(category, out var normTime) || normTime <= 0)
                continue;
            if (finishTimeHundredths <= normTime)
                return category;
        }
        return null;
    }
}
