using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NetTally.Avalonia
{
    public class App : Application
    {
        /// <summary>
        /// The IoC Service Provider for this app.
        /// </summary>
        public IServiceProvider ServiceProvider => this.InternalServiceProvider ?? throw new InvalidOperationException("No service provider set.");

        /// <summary>
        /// The backing property for our Service Provider.
        /// </summary>
        /// <remarks>
        /// Nullable because it is set outside of the constructor.
        /// </remarks>
        private IServiceProvider? InternalServiceProvider { get; set; }

        /// <summary>
        /// Initializes the Application Framework, loading resources and the like from the XAML.
        /// </summary>
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);

            // I believe this is a noop currently.
            base.Initialize();
        }

        /// <summary>
        /// Intializes app functions and IoC services. Called after framework Initialization.
        /// </summary>
        public override void OnFrameworkInitializationCompleted()
        {
            // Create a service collection and configure our dependencies, and get our service provider.
            this.InternalServiceProvider = this.ConfigureServiceProvider(new ServiceCollection());

            // Intialize the Comparers (this code is weird, not sure why it's done like this).
            Utility.Comparers.Agnostic.Init(this.InternalServiceProvider.GetRequiredService<Utility.Comparers.IHash>());

            var logger = this.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger<App>();
            logger?.LogInformation($"Services defined. Starting application. Version: {SystemInfo.ProductInfo.Version}");

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // set our MainWindow and Shutdown mode. This tells our app how/what window to start, and how to close.
                desktop.MainWindow = this.ServiceProvider.GetRequiredService<Views.MainWindow>();
                desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
            }

            // I believe this is a noop currently.
            base.OnFrameworkInitializationCompleted();
        }

        /// <summary>
        /// Configures a IoC service provider for this App, add and configuring all its services.
        /// </summary>
        /// <param name="serviceCollection">The service collection the app.</param>
        /// <returns>A configured service provider </returns>
        private ServiceProvider ConfigureServiceProvider(IServiceCollection serviceCollection)
        {
            // Get the services provided by the core library.
            NetTally.Startup.ConfigureServices(serviceCollection);

            serviceCollection.Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Debug);

            // Then add services known by the current assembly,
            // or override services provided by the core library.

            // Add IoCNavigationService for the application.
            serviceCollection.AddSingleton<Navigation.AvaloniaNavigationService>();

            // Register all the Windows of the applications via the service provider.
            serviceCollection.AddTransient<Views.MainWindow>();
            serviceCollection.AddTransient<Views.GlobalOptions>();
            serviceCollection.AddTransient<Views.QuestOptions>();
            serviceCollection.AddTransient<Views.ManageVotes>();
            //            services.AddTransient<ReorderTasksWindow>();

            // Build the IServiceProvider and set our reference to it
            return serviceCollection.BuildServiceProvider();
        }
    }
}
