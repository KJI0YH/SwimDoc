using ServiceLayer.Scoring;

namespace Tests.Scoring;

[TestFixture]
public sealed class PlacePointsPasteTest
{
    [Test]
    public void ParseColumn_ParsesExcelColumn()
    {
        var text = "50\r\n45\r\n40\r\n36\tignored\r\n\r\n32";
        var values = PlacePointsClipboard.ParseColumn(text);
        Assert.That(values, Is.EqualTo(new[] { 50, 45, 40, 36, 32 }));
    }

    [Test]
    public void DefaultPlacePoints_HasTwentyPlaces()
    {
        Assert.That(ActiveScoringSettings.DefaultPlacePoints, Is.EqualTo(new[]
        {
            50, 45, 40, 36, 32, 28, 25, 22, 19, 16, 14, 12, 10, 8, 6, 5, 4, 3, 2, 1
        }));
    }
}
