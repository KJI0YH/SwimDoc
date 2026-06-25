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
}
