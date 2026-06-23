using DataLayer.Display;
using DataLayer.EfClasses;
using DataLayer.EfCore;

namespace Tests.EntryResults;

[TestFixture]
public sealed class EntryResultRulesTest
{
    [Test]
    public void FormatFinishTime_DsqWithTime_ShowsStatusAndTimeInParentheses()
    {
        var entry = new DataLayer.EfClasses.Entry
        {
            Status = EntryStatus.DSQ,
            FinishTime = 2525
        };

        Assert.That(EntryTimeDisplay.FormatFinishTime(entry), Is.EqualTo("DSQ (25.25)"));
    }

    [Test]
    public void FormatFinishTime_DsqWithoutTime_ShowsStatusOnly()
    {
        var entry = new DataLayer.EfClasses.Entry
        {
            Status = EntryStatus.DSQ
        };

        Assert.That(EntryTimeDisplay.FormatFinishTime(entry), Is.EqualTo("DSQ"));
    }

    [Test]
    public void ApplyNonFinishResultRules_Dsq_KeepsFinishTimeAndClearsPoints()
    {
        var entry = new DataLayer.EfClasses.Entry
        {
            Status = EntryStatus.DSQ,
            FinishTime = 2525,
            Points = 450
        };

        entry.ApplyNonFinishResultRules();

        Assert.That(entry.FinishTime, Is.EqualTo(2525));
        Assert.That(entry.Points, Is.EqualTo(0));
    }

    [Test]
    public void ApplyNonFinishResultRules_Dns_ClearsFinishTimeAndPoints()
    {
        var entry = new DataLayer.EfClasses.Entry
        {
            Status = EntryStatus.DNS,
            FinishTime = 2525,
            Points = 450
        };

        entry.ApplyNonFinishResultRules();

        Assert.That(entry.FinishTime, Is.Null);
        Assert.That(entry.Points, Is.EqualTo(0));
    }
}
