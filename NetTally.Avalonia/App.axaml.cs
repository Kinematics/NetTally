using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetTally.Collections;
using NetTally.SystemInfo;
using NetTally.Utility.Comparers;
using NetTally.ViewModels;

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
                ConfigureServices(serviceCollection);

                // Build the IServiceProvider and set our reference to it
                this.InternalServiceProvider = serviceCollection.BuildServiceProvider();

                var hash = this.InternalServiceProvider.GetRequiredService<IHash>();
                Agnostic.Init(hash);

                var loggerFactory = ServiceProvider.GetService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger<App>();
                logger.LogInformation($"Services defined. Starting application. Version: {ProductInfo.Version}");

                // Request the navigation service and create our main window.
                var navigationService = ServiceProvider.GetRequiredService<Navigation.AvaloniaNavigationService>();
                _ = navigationService.ShowAsync<Views.MainWindow>();
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
