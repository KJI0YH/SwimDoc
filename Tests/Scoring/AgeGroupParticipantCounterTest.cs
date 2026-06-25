using DataLayer.Display;
using DataLayer.EfClasses;

namespace Tests.Scoring;

[TestFixture]
public class AgeGroupParticipantCounterTest
{
    [Test]
    public void Count_UsesHighestMatchingAgeGroupBounds()
    {
        var athletes = new[]
        {
            new Athlete
            {
                FirstName = "A",
                LastName = "A",
                Gender = Gender.Male,
                YearOfBirth = 2005
            },
            new Athlete
            {
                FirstName = "B",
                LastName = "B",
                Gender = Gender.Female,
                YearOfBirth = 2008
            }
        };

        Assert.That(
            AgeGroupParticipantCounter.Count(
                birthYearMin: 2000,
                birthYearMax: 2010,
                gender: Gender.Male,
                athletes.Select(a => (a.YearOfBirth, a.Gender))),
            Is.EqualTo(1));
    }
}
