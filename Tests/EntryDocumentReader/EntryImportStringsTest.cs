using System.Globalization;
using BizLogic.Resources;

namespace Tests.EntryDocumentReader;

[TestFixture]
public class EntryImportStringsTest
{
    [Test]
    public void FormatHeaderNotFound_UsesLocalizedHeaderName_Russian()
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ru-RU");
            var message = EntryImportStrings.FormatHeaderNotFound("BirthYear");
            Assert.That(message, Is.EqualTo("Заголовок «Год рождения» не найден."));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Test]
    public void FormatHeaderNotFound_UsesLocalizedHeaderName_English()
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var message = EntryImportStrings.FormatHeaderNotFound("ClubName");
            Assert.That(message, Is.EqualTo("Header \"Team\" not found."));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
