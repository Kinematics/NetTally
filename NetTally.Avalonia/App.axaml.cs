using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NetTally.Avalonia
{
    public class App : Application
    {
        private IServiceProvider? InternalServiceProvider { get; set; }
        public IServiceProvider ServiceProvider => this.InternalServiceProvider ?? throw new InvalidOperationException("No service provider set.");

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Create a service collection and configure our dependencies
                var serviceCollection = new ServiceCollection();
                this.ConfigureServices(serviceCollection);

                // Build the IServiceProvider and set our reference to it
                this.InternalServiceProvider = serviceCollection.BuildServiceProvider();

                Utility.Comparers.Agnostic.Init(this.InternalServiceProvider.GetRequiredService<Utility.Comparers.IHash>());

                var logger = ServiceProvider.GetService<ILoggerFactory>().CreateLogger<App>();
                logger.LogInformation($"Services defined. Starting application. Version: {SystemInfo.ProductInfo.Version}");

                // Request the navigation service and create our main window.
                ServiceProvider.GetRequiredService<Navigation.AvaloniaNavigationService>().Show<Views.MainWindow>();
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Get the services provided by the core library.
            NetTally.Startup.ConfigureServices(services);

            services.Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Debug);

            // Then add services known by the current assembly,
            // or override services provided by the core library.

            // Add IoCNavigationService for the application.
            services.AddSingleton<Navigation.AvaloniaNavigationService>();

            // Register all the Windows of the applications via the service provider.
            services.AddTransient<Views.MainWindow>();
            services.AddTransient<Views.GlobalOptions>();
            services.AddTransient<Views.QuestOptions>();
            services.AddTransient<Views.ManageVotes>();
//            services.AddTransient<ReorderTasksWindow>();
        }
    }
}
