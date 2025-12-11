using System.ComponentModel.Design;
using System.Configuration;
using System.Data;
using System.Windows;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
using UI.ViewModels;
using UI.Views;

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

        serviceCollection.AddDbContext<EfCoreContext>(options =>
            options.UseSqlite());

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

        services.AddSingleton<IAgeGroupService, AgeGroupService>();
        services.AddSingleton<IAthleteService, AthleteService>();
        services.AddSingleton<IClubService, ClubService>();
        services.AddSingleton<IEntryDocumentReaderService, EntryDocumentReaderService>();
        services.AddSingleton<IEntryService, EntryService>();
        services.AddSingleton<IEventService, EventService>();
        services.AddSingleton<IHeatService, HeatService>();
        services.AddSingleton<ISwimStyleService, SwimStyleService>();

    }

    private static void ConfigureViewModels(IServiceCollection services)
    {
        services.AddTransient<CompetitionSelectionViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<EventsViewModel>();
        services.AddTransient<HeatsViewModel>();
        services.AddTransient<EntriesViewModel>();
        services.AddTransient<AthletesViewModel>();
        services.AddTransient<ClubsViewModel>();
        services.AddTransient<AgeGroupsViewModel>();
        services.AddTransient<SwimStylesViewModel>();
    }
}