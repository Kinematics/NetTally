/*
    NetTally
    Copyright(C) 2015  David Smith <dsmith@datasync.com>

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along
    with this program; if not, write to the Free Software Foundation, Inc.,
    51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/

using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetTally.Navigation;
using NetTally.SystemInfo;
using NetTally.Systems;
using NetTally.Views;

namespace NetTally
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly ILogger<App> logger;

        public App()
        {
            // Initialize host
            AppX.Initialize(SetupUIServices);

            // Create handlers for unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // Create logger for the app.
            var loggerFactory = AppX.Services.GetRequiredService<ILoggerFactory>();
            logger = loggerFactory.CreateLogger<App>();
        }

        private void SetupUIServices(IServiceCollection services)
        {
            // Add IoCNavigationService for the application.
            services.AddSingleton<WPFNavigationService>();

            // Register all the Windows of the applications.
            services.AddTransient<MainWindow>();
            services.AddTransient<GlobalOptions>();
            services.AddTransient<QuestOptions>();
            services.AddTransient<ManageVotes>();
            services.AddTransient<ReorderTasks>();
        }

        #region Startup and Shutdown
        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                // Start the app
                await AppX.Host.StartAsync();

                logger.LogInformation("Starting application. Version: {version}", ProductInfo.Version);

                // Request the navigation service and create our main window.
                var navigationService = AppX.Services.GetRequiredService<WPFNavigationService>();
                await navigationService.ShowAsync<MainWindow>();
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Error during application startup");
            }
        }

        private async void Application_Exit(object sender, ExitEventArgs e)
        {
            using (AppX.Host)
            {
                // Save user config
                AppX.SaveConfiguration();

                // Wait up to 5 seconds before forcing a shutdown.
                await AppX.Host.StopAsync(TimeSpan.FromSeconds(5));
            }
        }
        #endregion Startup and Shutdown

        #region Error Handling
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            logger.LogCritical((Exception)e.ExceptionObject, "Unhandled exception");
        }
        #endregion Error Handling
    }
}
