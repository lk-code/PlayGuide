using System.Diagnostics.CodeAnalysis;
using PlayGuide.Core.Cache;
using PlayGuide.Core.Input;
using PlayGuide.Core.Launch;
using PlayGuide.Core.Library;
using PlayGuide.Core.Steam;
using PlayGuide.Input;
using Uno.Resizetizer;

namespace PlayGuide;

public partial class App : Application
{
    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        // PlayGuide uses a dark, console-style aesthetic.
        this.RequestedTheme = ApplicationTheme.Dark;
        this.InitializeComponent();
    }

    protected Window? MainWindow { get; private set; }
    protected IHost? Host { get; private set; }

    [SuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Uno.Extensions APIs are used in a way that is safe for trimming in this template context.")]
    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            // Add navigation support for toolkit controls such as TabBar and NavigationView
            .UseToolkitNavigation()
            .Configure(host => host
#if DEBUG
                // Switch to Development environment when running in DEBUG
                .UseEnvironment(Environments.Development)
#endif
                .UseLogging(configure: (context, logBuilder) =>
                {
                    // Configure log levels for different categories of logging
                    logBuilder
                        .SetMinimumLevel(
                            context.HostingEnvironment.IsDevelopment() ?
                                LogLevel.Information :
                                LogLevel.Warning)

                        // Default filters for core Uno Platform namespaces
                        .CoreLogLevel(LogLevel.Warning);

                    // Uno Platform namespace filter groups
                    // Uncomment individual methods to see more detailed logging
                    //// Generic Xaml events
                    //logBuilder.XamlLogLevel(LogLevel.Debug);
                    //// Layout specific messages
                    //logBuilder.XamlLayoutLogLevel(LogLevel.Debug);
                    //// Storage messages
                    //logBuilder.StorageLogLevel(LogLevel.Debug);
                    //// Binding related messages
                    //logBuilder.XamlBindingLogLevel(LogLevel.Debug);
                    //// Binder memory references tracking
                    //logBuilder.BinderMemoryReferenceLogLevel(LogLevel.Debug);
                    //// DevServer and HotReload related
                    //logBuilder.HotReloadCoreLogLevel(LogLevel.Information);
                    //// Debug JS interop
                    //logBuilder.WebAssemblyLogLevel(LogLevel.Debug);

                }, enableUnoLogging: true)
                .UseSerilog(consoleLoggingEnabled: true, fileLoggingEnabled: true)
                .UseConfiguration(configure: configBuilder =>
                    configBuilder
                        .EmbeddedSource<App>()
                        .Section<AppConfig>()
                )
                // Enable localization (see appsettings.json for supported languages)
                .UseLocalization()
                .ConfigureServices((context, services) =>
                {
                    // Filesystem / persistence
                    services.AddSingleton(_ => new UserPaths());
                    services.AddSingleton<IGameCacheService, GameCacheService>();
                    services.AddSingleton<ISettingsStore, JsonSettingsStore>();

                    // Steam provider (scanner + artwork). Add further stores by registering
                    // additional ILibraryScanner / IArtworkResolver implementations.
                    services.AddSingleton<SteamPathLocator>();
                    services.AddSingleton<ILibraryScanner, SteamLibraryScanner>();
                    services.AddSingleton<IArtworkResolver, SteamArtworkResolver>();

                    // Orchestration, launching and input
                    services.AddSingleton<IGameLibraryService, GameLibraryService>();
                    services.AddSingleton<IGameLauncher, GameLauncher>();
                    services.AddSingleton<IGamepadService, SdlGamepadService>();
                    services.AddSingleton<FocusNavigationController>();
                })
                .UseNavigation(ReactiveViewModelMappings.ViewModelMappings, RegisterRoutes)
            );
        MainWindow = builder.Window;

        #if DEBUG
        MainWindow.UseStudio();
#endif
                MainWindow.SetWindowIcon();

        Host = await builder.NavigateAsync<Shell>();

        // Start cross-platform gamepad input once the UI is up.
        Host.Services.GetRequiredService<FocusNavigationController>().Attach(MainWindow!);
    }

    private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
    {
        views.Register(
            new ViewMap(ViewModel: typeof(ShellModel)),
            new ViewMap<LibraryPage, LibraryModel>(),
            new ViewMap<SettingsPage, SettingsModel>()
        );

        routes.Register(
            new RouteMap("", View: views.FindByViewModel<ShellModel>(),
                Nested:
                [
                    new ("Library", View: views.FindByViewModel<LibraryModel>(), IsDefault:true),
                    new ("Settings", View: views.FindByViewModel<SettingsModel>()),
                ]
            )
        );
    }
}
