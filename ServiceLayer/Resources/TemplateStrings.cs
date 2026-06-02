using System.Globalization;
using System.Resources;

namespace ServiceLayer.Resources;

public static class TemplateStrings
{
    private static readonly ResourceManager ResourceManagerImpl =
        new("ServiceLayer.Resources.TemplateStrings", typeof(TemplateStrings).Assembly);

    public static ResourceManager ResourceManager => ResourceManagerImpl;

    public static string Get(string name) =>
        ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? $"[[{name}]]";

    public static string Sheet_Entries => Get(nameof(Sheet_Entries));
    public static string Sheet_Settings => Get(nameof(Sheet_Settings));

    public static string Entries_Title_A1 => Get(nameof(Entries_Title_A1));
    public static string Entries_Title_B1 => Get(nameof(Entries_Title_B1));

    public static string Entries_Header_FirstName => Get(nameof(Entries_Header_FirstName));
    public static string Entries_Header_LastName => Get(nameof(Entries_Header_LastName));
    public static string Entries_Header_BirthYear => Get(nameof(Entries_Header_BirthYear));
    public static string Entries_Header_Gender => Get(nameof(Entries_Header_Gender));
    public static string Entries_Header_Category => Get(nameof(Entries_Header_Category));

    public static string Settings_Header_Gender => Get(nameof(Settings_Header_Gender));
    public static string Settings_Header_Category => Get(nameof(Settings_Header_Category));
    public static string Settings_Header_Distance => Get(nameof(Settings_Header_Distance));
    public static string Settings_Header_Stroke => Get(nameof(Settings_Header_Stroke));
    public static string Settings_Header_TeamType => Get(nameof(Settings_Header_TeamType));

    public static string Gender_Male => Get(nameof(Gender_Male));
    public static string Gender_Female => Get(nameof(Gender_Female));

    public static string Stroke_Fly => Get(nameof(Stroke_Fly));
    public static string Stroke_Back => Get(nameof(Stroke_Back));
    public static string Stroke_Breast => Get(nameof(Stroke_Breast));
    public static string Stroke_Free => Get(nameof(Stroke_Free));
    public static string Stroke_Medley => Get(nameof(Stroke_Medley));

    public static string Entries_Example_FirstName => Get(nameof(Entries_Example_FirstName));
    public static string Entries_Example_LastName => Get(nameof(Entries_Example_LastName));
    public static string Entries_Example_Category => Get(nameof(Entries_Example_Category));

    public static string Category_IMoS => Get(nameof(Category_IMoS));
    public static string Category_MoS => Get(nameof(Category_MoS));
    public static string Category_CMoS => Get(nameof(Category_CMoS));
    public static string Category_FirstAdult => Get(nameof(Category_FirstAdult));
    public static string Category_SecondAdult => Get(nameof(Category_SecondAdult));
    public static string Category_ThirdAdult => Get(nameof(Category_ThirdAdult));
    public static string Category_FirstJunior => Get(nameof(Category_FirstJunior));
    public static string Category_SecondJunior => Get(nameof(Category_SecondJunior));
    public static string Category_NoCategory => Get(nameof(Category_NoCategory));
}

