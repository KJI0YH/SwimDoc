using DataLayer.Display;
using DataLayer.EfClasses;

namespace Tests.EntryResults;

[TestFixture]
public sealed class EntryPlaceAssignmentTest
{
    [Test]
    public void OrderForResults_SortsByFinishTimeAsc_WithUnrankedAtEnd()
    {
        var entries = new List<DataLayer.EfClasses.Entry>
        {
            CreateEntry(1, EntryStatus.FINISH, 2100),
            CreateEntry(2, EntryStatus.FINISH, 2000),
            CreateEntry(3, EntryStatus.DSQ, 2525),
            CreateEntry(4, EntryStatus.DNS, null),
        };

        var ordered = EntryPlaceAssignment.OrderForResults(entries);

        Assert.That(ordered.Select(e => e.Id), Is.EqualTo(new[] { 2, 1, 3, 4 }));
    }

    [Test]
    public void AssignPlaces_TiedFinishTimes_KeepSharedPlace()
    {
        var entries = new List<DataLayer.EfClasses.Entry>
        {
            CreateEntry(1, EntryStatus.FINISH, 2000),
            CreateEntry(2, EntryStatus.FINISH, 2000),
            CreateEntry(3, EntryStatus.FINISH, 2200),
        };

        var places = EntryPlaceAssignment.AssignPlaces(EntryPlaceAssignment.OrderForResults(entries));

        Assert.That(places.Select(p => p.Place), Is.EqualTo(new[] { 1, 1, 3 }));
    }

    [Test]
    public void AssignPlaces_DsqAndDns_ShareFinalPlaceAfterFinishers()
    {
        var entries = new List<DataLayer.EfClasses.Entry>
        {
            CreateEntry(1, EntryStatus.FINISH, 2000),
            CreateEntry(2, EntryStatus.FINISH, 2100),
            CreateEntry(3, EntryStatus.DSQ, 2525),
            CreateEntry(4, EntryStatus.DNS, null),
            CreateEntry(5, EntryStatus.FINISH, 3000),
        };

        var places = EntryPlaceAssignment.AssignPlaces(EntryPlaceAssignment.OrderForResults(entries));

        Assert.That(places.Select(p => (p.Entry.Id, p.Place)), Is.EqualTo(new[]
        {
            (1, 1),
            (2, 2),
            (5, 3),
            (3, 4),
            (4, 4),
        }));
    }

    [Test]
    public void AssignPlaces_TiedFinishers_KeepSharedPlaceBeforeFinalPlace()
    {
        var entries = new List<DataLayer.EfClasses.Entry>
        {
            CreateEntry(1, EntryStatus.FINISH, 2000),
            CreateEntry(2, EntryStatus.FINISH, 2000),
            CreateEntry(3, EntryStatus.FINISH, 2200),
            CreateEntry(4, EntryStatus.DNF, null),
            CreateEntry(5, EntryStatus.DSQ, 2400),
        };

        var places = EntryPlaceAssignment.AssignPlaces(EntryPlaceAssignment.OrderForResults(entries));

        Assert.That(places.Select(p => p.Place), Is.EqualTo(new[] { 1, 1, 3, 4, 4 }));
    }

    [Test]
    public void AssignPlaces_AllUnrankedEntries_ShareFirstPlace()
    {
        var entries = new List<DataLayer.EfClasses.Entry>
        {
            CreateEntry(1, EntryStatus.DSQ, 2525),
            CreateEntry(2, EntryStatus.DNS, null),
            CreateEntry(3, EntryStatus.DNF, null),
        };

        var places = EntryPlaceAssignment.AssignPlaces(EntryPlaceAssignment.OrderForResults(entries));

        Assert.That(places.Select(p => p.Place), Is.All.EqualTo(1));
    }

    private static DataLayer.EfClasses.Entry CreateEntry(
        int id,
        EntryStatus status,
        int? finishTime) =>
        new()
        {
            Id = id,
            Status = status,
            FinishTime = finishTime,
            SwimStyleId = 1
        };
}
