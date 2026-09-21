using DataLayer.Display;
using DataLayer.EfClasses;

namespace Tests.Ranks;

public class CategoryClassifierTest
{
    [Test]
    public void Classify_ReturnsHighestMatchingCategory()
    {
        var norms = new Dictionary<Category, int>
        {
            [Category.IMoS] = 5000,
            [Category.MoS] = 5500,
            [Category.CMoS] = 6000,
            [Category.FirstAdult] = 6500
        };

        Assert.That(CategoryClassifier.Classify(4999, norms), Is.EqualTo(Category.IMoS));
        Assert.That(CategoryClassifier.Classify(5000, norms), Is.EqualTo(Category.IMoS));
        Assert.That(CategoryClassifier.Classify(5200, norms), Is.EqualTo(Category.MoS));
        Assert.That(CategoryClassifier.Classify(6000, norms), Is.EqualTo(Category.CMoS));
        Assert.That(CategoryClassifier.Classify(6500, norms), Is.EqualTo(Category.FirstAdult));
        Assert.That(CategoryClassifier.Classify(6501, norms), Is.Null);
    }

    [Test]
    public void Classify_IgnoresZeroNorms()
    {
        var norms = new Dictionary<Category, int>
        {
            [Category.MoS] = 0,
            [Category.CMoS] = 6000
        };

        Assert.That(CategoryClassifier.Classify(5500, norms), Is.EqualTo(Category.CMoS));
    }
}
