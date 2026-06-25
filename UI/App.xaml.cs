using System.Windows;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OfficeOpenXml;
using ServiceLayer.AgeGroupService;
using ServiceLayer.AthleteService;
using ServiceLayer.BaseTimeRepository;
using ServiceLayer.ClubService;
using ServiceLayer.ConnectionService;
using ServiceLayer.Logging;
using ServiceLayer.DependencyInjection;
using ServiceLayer.EntryDocumentReaderService;
using ServiceLayer.AppSettings;
using ServiceLayer.EntryImportSettings;
using ServiceLayer.EntryDocumentTemplateService;
using ServiceLayer.EntryService;
using ServiceLayer.EventService;
using ServiceLayer.HeatService;
using ServiceLayer.PointScoreProvider;
using ServiceLayer.ReportGeneratorService;
using ServiceLayer.SwimStyleService;
using System.Globalization;
using System.Net.Http;
using UI.Helpers;
using UI.Localization;
using UI.Services.FontScale;
using UI.ViewModels.Pages;
using UI.Views.Controls.DataGridView;
using UI.Views.Pages;
using UI.Views.Windows;
using AgeGroupDetailsViewModel = UI.ViewModels.Pages.AgeGroupDetailsViewModel;
using AthleteDetailsViewModel = UI.ViewModels.Pages.AthleteDetailsViewModel;
using ClubDetailsViewModel = UI.ViewModels.Pages.ClubDetailsViewModel;
using CompetitionSelectionViewModel = UI.ViewModels.Pages.CompetitionSelectionViewModel;
using EntriesViewModel = UI.ViewModels.Pages.EntriesViewModel;
using EventDetailsViewModel = UI.ViewModels.Pages.EventDetailsViewModel;
using EventsViewModel = UI.ViewModels.Pages.EventsViewModel;
using HeatsViewModel = UI.ViewModels.Pages.HeatsViewModel;
using ResultsViewModel = UI.ViewModels.Pages.ResultsViewModel;
using AboutViewModel = UI.ViewModels.Pages.AboutViewModel;
using SettingsViewModel = UI.ViewModels.Pages.SettingsViewModel;
using MainViewModel = UI.ViewModels.Windows.MainViewModel;
using SwimStyleDetailsViewModel = UI.ViewModels.Pages.SwimStyleDetailsViewModel;
using AboutPage = UI.Views.Pages.AboutPage;
using SettingsPage = UI.Views.Pages.SettingsPage;

namespace UI;

public partial class App : Application
{
    private const string InstallerMutexName = "SwimDoc.06F6DAE8-9B05-41C5-AE43-51FBD9684B9A";

    private readonly IServiceProvider? _serviceProvider;
    private static Mutex? _installerMutex;

    public App()
    {
        _installerMutex = new Mutex(true, InstallerMutexName, out _);
        ExcelPackage.License.SetNonCommercialPersonal("Aliaksei Kryzhanouski");
        _serviceProvider = ConfigureServiceProvider();
        RegisterViewModelMappings();
    }

    public new static App Current => (App)Application.Current;
    public string? StartupCompetitionFilePath { get; private set; }
    public IServiceProvider Services
    {
        get
        {
            var serviceProvider = Current._serviceProvider;
            return serviceProvider ?? throw new InvalidOperationException("The service provider is not initialized");
        }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var log = Services.GetRequiredService<IAppLog>();
        RegisterExceptionLogging(log);
        log.Info($"Application startup. Install directory: {ServiceLayer.ApplicationPaths.InstallDirectory}");
        if (e.Args.Length > 0)
            log.Info($"Command line arguments: {string.Join(' ', e.Args)}");

        StartupCompetitionFilePath = CompetitionFile.ResolveStartupPath(e.Args);
        if (StartupCompetitionFilePath is not null)
            log.Info($"Startup competition file requested: {StartupCompetitionFilePath}");

        Services.GetRequiredService<IFontScaleService>().ApplyCurrent();
        var mainWindow = new MainWindow();
        mainWindow.Show();
        log.Info("Main window shown.");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (Services.GetService<IAppLog>() is { } log)
            log.Info("Application exit.");

        if (_installerMutex is not null)
        {
            _installerMutex.ReleaseMutex();
            _installerMutex.Dispose();
            _installerMutex = null;
        }

        base.OnExit(e);
    }

    private static void RegisterExceptionLogging(IAppLog log)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
                log.Error("Unhandled application exception.", ex);
            else
                log.Error($"Unhandled application exception: {args.ExceptionObject}");
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            log.Error("Unobserved task exception.", args.Exception);
            args.SetObserved();
        };

        Current.DispatcherUnhandledException += (_, args) =>
        {
            log.Error("Unhandled UI thread exception.", args.Exception);
        };
    }

    private IServiceProvider ConfigureServiceProvider()
    {
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        serviceCollection.AddDbContext<EfCoreContext>((_, options) =>
        {
            options.UseSwimDocSqlite();
        }, ServiceLifetime.Transient, ServiceLifetime.Singleton);
        ConfigureViewModels(serviceCollection);
        var provider = serviceCollection.BuildServiceProvider();
        var localization = provider.GetRequiredService<ILocalizationService>();
        LocalizationProvider.Instance.Culture = CultureInfo.CurrentUICulture;
        localization.CultureChanged += culture => LocalizationProvider.Instance.Culture = culture;
        return provider;
    }

    public void SetConnectionString(string connectionString)
    {
        var connectionService = Services.GetRequiredService<IDatabaseConnection>();
        connectionService.SetConnection(connectionString);
    }

    private void RegisterViewModelMappings()
    {
        if (Services.GetRequiredService<INavigationService>() is not NavigationService navigationService) return;
        navigationService.RegisterMapping<EventsViewModel, EventsView>();
        navigationService.RegisterMapping<HeatsViewModel, HeatsView>();
        navigationService.RegisterMapping<EntriesViewModel, EntriesView>();
        navigationService.RegisterMapping<AthletesViewModel, AthletesView>();
        navigationService.RegisterMapping<ClubsViewModel, ClubsView>();
        navigationService.RegisterMapping<AgeGroupsViewModel, AgeGroupsView>();
        navigationService.RegisterMapping<SwimStylesViewModel, SwimStylesView>();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IAppLog, FileAppLog>();
        services.AddSingleton<IDatabaseConnection, DatabaseConnectionService>();
        services.AddTransient<ICompetitionDatabaseService, CompetitionDatabaseService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IAddEditWindowFactory, AddEditWindowFactory>();
        services.AddSingleton<IBaseTimeRepository>(sp => new CsvBaseTimeRepository(sp.GetRequiredService<IAppLog>()));
        services.AddSingleton<IPointScoreProvider, PointScoreProvider>();
        services.AddSingleton<Wpf.Ui.IContentDialogService, Wpf.Ui.ContentDialogService>();
        services.AddTransient<IConfirmDialogService, ConfirmDialogService>();
        services.AddTransient<IErrorDialogService, ErrorDialogService>();
        services.AddSingleton<IAppSettingsStore, AppSettingsStore>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<IFontScaleService, FontScaleService>();
        services.AddSingleton<IEntryImportSettingsService>(sp =>
            new EntryImportSettingsService(sp.GetRequiredService<IAppSettingsStore>(), sp.GetRequiredService<IAppLog>()));
        services.AddSingleton<IPagingSettingsService, PagingSettingsService>();
        services.AddSingleton<IGitHubUpdateCheckService>(_ =>
        {
            var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("SwimDoc-UpdateChecker");
            httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            return new GitHubUpdateCheckService(httpClient);
        });
        services.AddTransient<IAgeGroupService, AgeGroupService>();
        services.AddTransient<IAthleteService, AthleteService>();
        services.AddTransient<IClubService, ClubService>();
        services.AddTransient<IEntryDocumentReaderService, EntryDocumentReaderService>();
        services.AddTransient<IEntryDocumentTemplateService, EntryDocumentTemplateService>();
        services.AddTransient<IEntryService, EntryService>();
        services.AddTransient<IEventService, EventService>();
        services.AddTransient<IHeatService, HeatService>();
        services.AddTransient<IReportExportService>(SharedDbContextServices.CreateReportExportService);
        services.AddTransient<ISwimStyleService, SwimStyleService>();
    }

    private static void ConfigureViewModels(IServiceCollection services)
    {
        services.AddSingleton<CompetitionSelectionViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<AboutViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<BaseTimesSettingsViewModel>();
        services.AddTransient<EventsViewModel>();
        services.AddTransient<HeatsViewModel>();
        services.AddTransient<FixationViewModel>();
        services.AddTransient<ResultsViewModel>();
        services.AddTransient<EntriesViewModel>();
        services.AddTransient<AthletesViewModel>();
        services.AddTransient<ClubsViewModel>();
        services.AddTransient<AgeGroupsViewModel>();
        services.AddTransient<SwimStylesViewModel>();
        services.AddTransient<AthleteDetailsViewModel>();
        services.AddTransient<ClubDetailsViewModel>();
        services.AddTransient<EntryDetailsViewModel>();
        services.AddTransient<EventDetailsViewModel>();
        services.AddTransient<AgeGroupDetailsViewModel>();
        services.AddTransient<SwimStyleDetailsViewModel>();
        services.AddTransient<CompetitionSelectionPage>();
        services.AddTransient<EventsPage>();
        services.AddTransient<HeatsPage>();
        services.AddTransient<FixationPage>();
        services.AddTransient<ResultsPage>();
        services.AddTransient<EntriesPage>();
        services.AddTransient<AthletesPage>();
        services.AddTransient<ClubsPage>();
        services.AddTransient<AgeGroupsPage>();
        services.AddTransient<SwimStylesPage>();
        services.AddTransient<AthleteDetailsPage>();
        services.AddTransient<ClubDetailsPage>();
        services.AddTransient<EntryDetailsPage>();
        services.AddTransient<EventDetailsPage>();
        services.AddTransient<AgeGroupDetailsPage>();
        services.AddTransient<SwimStyleDetailsPage>();
        services.AddTransient<SettingsPage>();
        services.AddTransient<AboutPage>();
    }
}
