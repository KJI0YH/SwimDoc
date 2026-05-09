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
using ServiceLayer.EntryDocumentReaderService;
using ServiceLayer.EntryService;
using ServiceLayer.EventService;
using ServiceLayer.HeatService;
using ServiceLayer.PointScoreProvider;
using ServiceLayer.ReportGeneratorService;
using ServiceLayer.SwimStyleService;
using UI.Services;
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
using SettingsViewModel = UI.ViewModels.Pages.SettingsViewModel;
using MainViewModel = UI.ViewModels.Windows.MainViewModel;
using SwimStyleDetailsViewModel = UI.ViewModels.Pages.SwimStyleDetailsViewModel;
using SettingsPage = UI.Views.Pages.SettingsPage;

namespace UI;

public partial class App : Application
{
    private readonly IServiceProvider? _serviceProvider;

    public App()
    {
        ExcelPackage.License.SetNonCommercialPersonal("Aliaksei Kryzhanouski");

        _serviceProvider = ConfigureServiceProvider();
        RegisterViewModelMappings();
    }

    public new static App Current => (App)Application.Current;

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
        var mainWindow = new MainWindow();
        mainWindow.Show();
    }

    private IServiceProvider ConfigureServiceProvider()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddDbContext<EfCoreContext>(
            options => options.UseSqlite(),
            ServiceLifetime.Transient,
            ServiceLifetime.Singleton);

        ConfigureServices(serviceCollection);
        ConfigureViewModels(serviceCollection);
        return serviceCollection.BuildServiceProvider();
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
        services.AddSingleton<IDatabaseConnection, DatabaseConnectionService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IAddEditWindowFactory, AddEditWindowFactory>();
        services.AddSingleton<IBaseTimeRepository, CsvBaseTimeRepository>();
        services.AddSingleton<IPointScoreProvider, PointScoreProvider>();

        services.AddTransient<IAgeGroupService, AgeGroupService>();
        services.AddTransient<IAthleteService, AthleteService>();
        services.AddTransient<IClubService, ClubService>();
        services.AddTransient<IEntryDocumentReaderService, EntryDocumentReaderService>();
        services.AddTransient<IEntryService, EntryService>();
        services.AddTransient<IEventService, EventService>();
        services.AddTransient<IHeatService, HeatService>();
        services.AddTransient<IReportExportService, ReportExportService>();
        services.AddTransient<ISwimStyleService, SwimStyleService>();
    }

    private static void ConfigureViewModels(IServiceCollection services)
    {
        services.AddSingleton<CompetitionSelectionViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<SettingsViewModel>();
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
    }
}