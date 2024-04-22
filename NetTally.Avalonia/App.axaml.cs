using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetTally.Systems;

namespace NetTally.Avalonia
{
    public class App : Application
    {
        private readonly ILogger<App> logger;

        public App()
        {
            AppX.Initialize(SetupUIServices);

            var loggerFactory = AppX.Services.GetRequiredService<ILoggerFactory>();
            logger = loggerFactory.CreateLogger<App>();

            // Create handlers for unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            logger.LogDebug("Application constructor completed.");
        }

        #region Hosting/DI setup
        private void SetupUIServices(IServiceCollection services)
        {
            // Get the services provided by the core library.
            Startup.ConfigureServices(services);

            services.Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Debug);

            // Then add services known by the current assembly,
            // or override services provided by the core library.

            // Add IoCNavigationService for the application.
            services.AddSingleton<Navigation.AvaloniaNavigationService>();

            // Register all the windows that the applications can display.
            services.AddTransient<Views.MainWindow>();
            services.AddTransient<Views.GlobalOptions>();
            services.AddTransient<Views.QuestOptions>();
            services.AddTransient<Views.ManageVotes>();
            services.AddTransient<Views.ReorderTasks>();
        }
        #endregion Hosting/DI setup

        #region Avalonia
        /// <summary>
        /// Initializes the Application Framework, loading resources and the like from the XAML.
        /// </summary>
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Line below is needed to remove Avalonia data validation.
                // Without this line you will get duplicate validations from both Avalonia and CT
                BindingPlugins.DataValidators.RemoveAt(0);

                // Add event handler for when the program exits.
                desktop.Exit += Desktop_Exit;

                Views.MainWindow mainWindow = AppX.Services.GetRequiredService<Views.MainWindow>();

                desktop.MainWindow = mainWindow;
                desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void Desktop_Exit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
        {
            // Save settings on exit.
            AppX.SaveConfiguration();

            logger.LogDebug("Application exit.");
        }
        #endregion Avalonia

        #region Error Handling
        /// <summary>
        /// Special handlers if an exception isn't handled by the program.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            logger.LogCritical((Exception)e.ExceptionObject, "Unhandled exception");
        }
        #endregion Error Handling
    }
}
