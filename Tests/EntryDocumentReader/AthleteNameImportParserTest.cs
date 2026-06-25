using BizLogic.EntryDocumentReader;

namespace Tests.EntryDocumentReader;

[TestFixture]
public class AthleteNameImportParserTest
{
    [TestCase("Иванов Иван", "Иванов", "Иван")]
    [TestCase("Smith John", "Smith", "John")]
    [TestCase("  Петров   Сергей  Алексеевич  ", "Петров", "Сергей Алексеевич")]
    [TestCase("Одинок", "Одинок", "")]
    [TestCase("", "", "")]
    public void SplitFullName_SplitsLastNameAndFirstName(string fullName, string expectedLastName, string expectedFirstName)
    {
        var (lastName, firstName) = AthleteNameImportParser.SplitFullName(fullName);
        Assert.That(lastName, Is.EqualTo(expectedLastName));
        Assert.That(firstName, Is.EqualTo(expectedFirstName));
    }

    [TestCase("Алексей Крыжановский", "Крыжановский", "Алексей")]
    [TestCase("John Smith", "Smith", "John")]
    [TestCase("Алексей Сергеевич Крыжановский", "Крыжановский", "Алексей Сергеевич")]
    public void SplitFullName_SplitsFirstNameAndLastName_WhenFirstNameFirst(
        string fullName,
        string expectedLastName,
        string expectedFirstName)
    {
        var (lastName, firstName) = AthleteNameImportParser.SplitFullName(fullName, firstNameFirst: true);
        Assert.That(lastName, Is.EqualTo(expectedLastName));
        Assert.That(firstName, Is.EqualTo(expectedFirstName));
    }
}
