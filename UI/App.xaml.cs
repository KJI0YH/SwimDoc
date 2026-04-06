using System.Windows;
using System.Windows.Threading;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OfficeOpenXml;
using ServiceLayer.AgeGroupService;
using ServiceLayer.AthleteService;
using ServiceLayer.ClubService;
using ServiceLayer.ConnectionService;
using ServiceLayer.EntryDocumentReaderService;
using ServiceLayer.EntryService;
using ServiceLayer.EventService;
using ServiceLayer.HeatService;
using ServiceLayer.SwimStyleService;
using UI.Services;
using UI.ViewModels.Details;
using UI.ViewModels;
using UI.ViewModels.Generic;
using UI.ViewModels.Table;
using UI.Views;
using UI.Views.Pages;
using UI.Views.Table;

namespace UI;

public partial class App : Application
{
    public new static App Current => (App)Application.Current;

    private readonly IServiceProvider? _serviceProvider;

    public IServiceProvider Services
    {
        get
        {
            var serviceProvider = Current._serviceProvider;
            return serviceProvider ?? throw new InvalidOperationException("The service provider is not initialized");
        }
    }

    public App()
    {
        ExcelPackage.License.SetNonCommercialPersonal("Aliaksei Kryzhanouski");

        _serviceProvider = ConfigureServiceProvider();
        RegisterViewModelMappings();
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
            contextLifetime: ServiceLifetime.Transient,
            optionsLifetime: ServiceLifetime.Singleton);

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

        services.AddTransient<IAgeGroupService, AgeGroupService>();
        services.AddTransient<IAthleteService, AthleteService>();
        services.AddTransient<IClubService, ClubService>();
        services.AddTransient<IEntryDocumentReaderService, EntryDocumentReaderService>();
        services.AddTransient<IEntryService, EntryService>();
        services.AddTransient<IEventService, EventService>();
        services.AddTransient<IHeatService, HeatService>();
        services.AddTransient<ISwimStyleService, SwimStyleService>();

    }

    private static void ConfigureViewModels(IServiceCollection services)
    {
        services.AddSingleton<CompetitionSelectionViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<EventsViewModel>();
        services.AddTransient<HeatsViewModel>();
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
    }
}