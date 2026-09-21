using DataLayer.Display;

namespace Tests.Input;

public class SwimTimeInputTest
{
    [Test]
    public void ApplyText_Typing82900_YieldsEightTwentyNine()
    {
        var digits = string.Empty;
        var display = string.Empty;

        (digits, display) = Type(digits, display, "8");
        Assert.That(display, Is.EqualTo("0.08"));

        (digits, display) = Type(digits, display, "2");
        Assert.That(display, Is.EqualTo("0.82"));

        (digits, display) = Type(digits, display, "9");
        Assert.That(display, Is.EqualTo("8.29"));

        (digits, display) = Type(digits, display, "0");
        Assert.That(display, Is.EqualTo("1:22.90"));

        (digits, display) = Type(digits, display, "0");
        Assert.That(display, Is.EqualTo("8:29.00"));
        Assert.That(digits, Is.EqualTo("82900"));
    }

    [Test]
    public void ApplyText_WithoutDigitBuffer_WouldBecomeTwelveTwentyNine()
    {
        // Simulates the old bug: reformatted "1:22.90" + typed "0" → digits 122900
        var broken = SwimTimeInput.ApplyText("1:22.900");
        Assert.That(broken.Text, Is.EqualTo("12:29.00"));
    }

    [Test]
    public void ApplyText_PasteEightTwentyNine_ParsesCorrectly()
    {
        var update = SwimTimeInput.ApplyText("8:29.00");
        Assert.That(update.Text, Is.EqualTo("8:29.00"));
        Assert.That(update.Hundredths, Is.EqualTo(8 * 6000 + 29 * 100));
    }

    [Test]
    public void ApplyText_BackspaceFromEightTwentyNine_StepsDown()
    {
        var digits = "82900";
        var display = "8:29.00";
        var update = SwimTimeInput.ApplyText("8:29.0", digits, display);
        Assert.That(update.Text, Is.EqualTo("1:22.90"));
        Assert.That(update.Digits, Is.EqualTo("8290"));
    }

    private static (string Digits, string Display) Type(string digits, string display, string digit)
    {
        var update = SwimTimeInput.ApplyText(display + digit, digits, display);
        return (update.Digits, update.Text);
    }
}
