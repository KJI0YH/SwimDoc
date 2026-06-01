using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UI.Resources;

namespace UI.ViewModels.Pages;

public sealed partial class AboutViewModel : ObservableObject
{
    public const string ContactEmail = "aliaksei.kryzhanouski@gmail.com";
    public const string GitHubIssuesUrl = "https://github.com/KJI0YH/SwimDoc/issues/new";
    public const string GitHubRepositoryUrl = "https://github.com/KJI0YH/SwimDoc";

    public string ApplicationName => Strings.App_Title;

    public string VersionText => string.Format(Strings.About_Version_Format, AppVersionInformation.Display);

    public string AuthorSectionTitle => Strings.About_Section_Author;

    public string AuthorName => Strings.About_Author_Name;

    public string AuthorLocation => Strings.About_Author_Location;

    public string CopyrightText => Strings.About_Copyright_Text;

    public string SupportSectionTitle => Strings.About_Section_Support;

    public string SupportDescription => Strings.About_Support_Description;

    public string LicenseSectionTitle => Strings.About_Section_License;

    public string LicenseText => Strings.About_License_Text;

    public string OpenEmailLabel => Strings.About_Action_Email;

    public string OpenGitHubLabel => Strings.About_Action_GitHub;

    public string OpenGitHubRepositoryLabel => Strings.About_Action_GitHub_Repository;

    [RelayCommand]
    private void OpenEmail()
    {
        Process.Start(new ProcessStartInfo($"mailto:{ContactEmail}") { UseShellExecute = true });
    }

    [RelayCommand]
    private void OpenGitHubIssues()
    {
        Process.Start(new ProcessStartInfo(GitHubIssuesUrl) { UseShellExecute = true });
    }

    [RelayCommand]
    private void OpenGitHubRepository()
    {
        Process.Start(new ProcessStartInfo(GitHubRepositoryUrl) { UseShellExecute = true });
    }
}
