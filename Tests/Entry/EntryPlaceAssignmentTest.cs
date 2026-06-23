using DataLayer.Display;
using DataLayer.EfClasses;

namespace Tests.EntryResults;

[TestFixture]
public sealed class EntryPlaceAssignmentTest
{
    [Test]
    public void OrderForResults_SortsByPointsDescThenTime_WithUnrankedAtEnd()
    {
        var entries = new List<DataLayer.EfClasses.Entry>
        {
            CreateEntry(1, EntryStatus.FINISH, 2100, 450),
            CreateEntry(2, EntryStatus.FINISH, 2000, 500),
            CreateEntry(3, EntryStatus.DSQ, 2525, 0),
            CreateEntry(4, EntryStatus.DNS, null, 0),
        };

        var ordered = EntryPlaceAssignment.OrderForResults(entries);

        Assert.That(ordered.Select(e => e.Id), Is.EqualTo(new[] { 2, 1, 3, 4 }));
    }

    [Test]
    public void AssignPlaces_TiedPoints_KeepSharedPlace()
    {
        var entries = new List<DataLayer.EfClasses.Entry>
        {
            CreateEntry(1, EntryStatus.FINISH, 2000, 500),
            CreateEntry(2, EntryStatus.FINISH, 2100, 500),
            CreateEntry(3, EntryStatus.FINISH, 2200, 400),
        };

        var places = EntryPlaceAssignment.AssignPlaces(EntryPlaceAssignment.OrderForResults(entries));

        Assert.That(places.Select(p => p.Place), Is.EqualTo(new[] { 1, 1, 3 }));
    }

    [Test]
    public void AssignPlaces_DsqAndZeroPointEntries_ShareFinalPlace()
    {
        var entries = new List<DataLayer.EfClasses.Entry>
        {
            CreateEntry(1, EntryStatus.FINISH, 2000, 500),
            CreateEntry(2, EntryStatus.FINISH, 2100, 450),
            CreateEntry(3, EntryStatus.DSQ, 2525, 0),
            CreateEntry(4, EntryStatus.DNS, null, 0),
            CreateEntry(5, EntryStatus.FINISH, 3000, 0),
        };

        var places = EntryPlaceAssignment.AssignPlaces(EntryPlaceAssignment.OrderForResults(entries));

        Assert.That(places.Select(p => p.Place), Is.EqualTo(new[] { 1, 2, 3, 3, 3 }));
    }

    [Test]
    public void AssignPlaces_TiedFinishers_KeepSharedPlaceBeforeFinalPlace()
    {
        var entries = new List<DataLayer.EfClasses.Entry>
        {
            CreateEntry(1, EntryStatus.FINISH, 2000, 500),
            CreateEntry(2, EntryStatus.FINISH, 2000, 500),
            CreateEntry(3, EntryStatus.FINISH, 2200, 400),
            CreateEntry(4, EntryStatus.DNF, null, 0),
            CreateEntry(5, EntryStatus.DSQ, 2400, 0),
        };

        var places = EntryPlaceAssignment.AssignPlaces(EntryPlaceAssignment.OrderForResults(entries));

        Assert.That(places.Select(p => p.Place), Is.EqualTo(new[] { 1, 1, 3, 4, 4 }));
    }

    [Test]
    public void AssignPlaces_AllUnrankedEntries_ShareFirstPlace()
    {
        var entries = new List<DataLayer.EfClasses.Entry>
        {
            CreateEntry(1, EntryStatus.DSQ, 2525, 0),
            CreateEntry(2, EntryStatus.DNS, null, 0),
            CreateEntry(3, EntryStatus.DNF, null, 0),
        };

        var places = EntryPlaceAssignment.AssignPlaces(EntryPlaceAssignment.OrderForResults(entries));

        Assert.That(places.Select(p => p.Place), Is.All.EqualTo(1));
    }

    private static DataLayer.EfClasses.Entry CreateEntry(
        int id,
        EntryStatus status,
        int? finishTime,
        int points) =>
        new()
        {
            Id = id,
            Status = status,
            FinishTime = finishTime,
            Points = points,
            SwimStyleId = 1
        };
}
