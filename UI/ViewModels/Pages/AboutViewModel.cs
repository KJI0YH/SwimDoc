using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UI.Resources;
using UI.ViewModels;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace UI.ViewModels.Pages;

public sealed partial class AboutViewModel(
    IGitHubUpdateCheckService gitHubUpdateCheckService,
    IContentDialogService contentDialogService) : ViewModelBase
{
    public const string ContactEmail = "aliaksei.kryzhanouski@gmail.com";
    public const string GitHubIssuesUrl = "https://github.com/KJI0YH/SwimDoc/issues/new";
    public const string GitHubReleasesUrl = "https://github.com/KJI0YH/SwimDoc/releases";
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CheckForUpdatesCommand))]
    private bool _isCheckingUpdates;
    public string ApplicationName => Strings.App_Title;
    public string VersionText => string.Format(Strings.About_Version_Format, AppVersionInformation.Display);
    public string VersionSectionTitle => Strings.About_Section_Version;
    public string UpdatesDescription => Strings.About_Updates_Description;
    public string AuthorSectionTitle => Strings.About_Section_Author;
    public string AuthorName => Strings.About_Author_Name;
    public string AuthorLocation => Strings.About_Author_Location;
    public string CopyrightText => Strings.About_Copyright_Text;
    public string SupportSectionTitle => Strings.About_Section_Support;
    public string SupportDescription => Strings.About_Support_Description;
    public string OpenEmailLabel => Strings.About_Action_Email;
    public string OpenGitHubLabel => Strings.About_Action_GitHub;
    public string CheckForUpdatesLabel =>
        IsCheckingUpdates ? Strings.About_Updates_Checking : Strings.About_Action_CheckUpdates;

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

    [RelayCommand(CanExecute = nameof(CanCheckForUpdates))]
    private async Task CheckForUpdatesAsync()
    {
        IsCheckingUpdates = true;
        OnPropertyChanged(nameof(CheckForUpdatesLabel));
        try
        {
            var result = await gitHubUpdateCheckService.CheckForUpdatesAsync();
            await ShowUpdateCheckResultAsync(result);
        }
        finally
        {
            IsCheckingUpdates = false;
            OnPropertyChanged(nameof(CheckForUpdatesLabel));
        }
    }

    private bool CanCheckForUpdates() => !IsCheckingUpdates;
    private async Task ShowUpdateCheckResultAsync(GitHubUpdateCheckResult result)
    {
        switch (result.Status)
        {
            case GitHubUpdateCheckStatus.UpToDate:
                await ShowDialogAsync(
                    Strings.About_Updates_Title,
                    string.Format(Strings.About_Updates_UpToDate, AppVersionInformation.Display));
                break;
            case GitHubUpdateCheckStatus.UpdateAvailable:
                var dialog = new ContentDialog
                {
                    Title = Strings.About_Updates_Title,
                    Content = string.Format(
                        Strings.About_Updates_Available,
                        NormalizeVersionLabel(result.LatestVersion),
                        AppVersionInformation.Display),
                    PrimaryButtonText = Strings.About_Updates_Download,
                    CloseButtonText = Strings.Common_Cancel,
                    DefaultButton = ContentDialogButton.Primary
                };
                if (await contentDialogService.ShowAsync(dialog, CancellationToken.None) == ContentDialogResult.Primary)
                    OpenUrl(result.DownloadUrl ?? GitHubReleasesUrl);
                break;
            case GitHubUpdateCheckStatus.NoReleases:
                await ShowDialogAsync(Strings.About_Updates_Title, Strings.About_Updates_NoReleases);
                break;
            default:
                var message = string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? Strings.About_Updates_Error
                    : string.Format(Strings.About_Updates_ErrorWithDetails, result.ErrorMessage);
                await ShowDialogAsync(Strings.About_Updates_Title, message);
                break;
        }
    }

    private Task<ContentDialogResult> ShowDialogAsync(string title, string content)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            CloseButtonText = Strings.Common_Ok,
            DefaultButton = ContentDialogButton.Close
        };
        return contentDialogService.ShowAsync(dialog, CancellationToken.None);
    }

    private static string NormalizeVersionLabel(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return version ?? string.Empty;
        return version.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? version[1..] : version;
    }

    private static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
