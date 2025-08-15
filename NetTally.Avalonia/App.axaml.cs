using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetTally.Avalonia.Views;

namespace NetTally.Avalonia;

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
    /// <summary>
    /// Register UI views and navigation service.
    /// </summary>
    /// <param name="services">The DI service collection being built at startup.</param>
    private void SetupUIServices(IServiceCollection services)
    {
        // Add NavigationService for the application.
        services.AddSingleton<Navigation.AvaloniaNavigationService>();

        // Register all the windows that the applications can display.
        services.AddTransient<MainWindow>();
        services.AddTransient<GlobalOptions>();
        services.AddTransient<QuestOptions>();
        services.AddTransient<ManageVotes>();
        services.AddTransient<ReorderTasks>();
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
