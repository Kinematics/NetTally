﻿/*
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
using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using NetTally.Debugging.FileLogger;
using NetTally.Navigation;
using NetTally.Options;
using NetTally.SystemInfo;
using NetTally.Utility.Comparers;
using NetTally.Views;

namespace NetTally
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly IHost host;

        public App()
        {
            host = Host.CreateDefaultBuilder(Environment.GetCommandLineArgs())
                .ConfigureHostConfiguration(ConfigureConfiguration)
                .ConfigureServices(ConfigureServices)
                .ConfigureLogging(ConfigureLogging)
                .Build();
        }

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            await host.StartAsync();

            // Initialize
            var hash = host.Services.GetRequiredService<IHash>();
            Agnostic.Init(hash);

            var loggerFactory = host.Services.GetService<ILoggerFactory>();
            if (loggerFactory != null)
            {
                var logger = loggerFactory.CreateLogger<App>();
                logger.LogInformation("Starting application. Version: {ProductInfo.Version}", ProductInfo.Version);
            }

            // Request the navigation service and create our main window.
            var navigationService = host.Services.GetRequiredService<IoCNavigationService>();
            await navigationService.ShowAsync<MainWindow>();
        }

        private async void Application_Exit(object sender, ExitEventArgs e)
        {
            using (host)
            {
                // Wait up to 5 seconds before forcing a shutdown.
                await host.StopAsync(TimeSpan.FromSeconds(5));
            }
        }

        /// <summary>
        /// Add user configuration files to the configuration builder.
        /// </summary>
        /// <param name="builder"></param>
        private void ConfigureConfiguration(IConfigurationBuilder builder)
        {
            // Try to find the AppSettings path on Windows, and use it
            // first when trying to load user config info.
            string? appSettingsPath = GetWindowsAppSettingsConfigPath();

            if (Path.Exists(appSettingsPath))
            {
                builder.AddJsonFile(Path.Combine(appSettingsPath, "userconfig.json"));
            }

            // A user config file in the local directory takes priority
            // over the AppSettings version, if any.
            builder.AddJsonFile("userconfig.json");
        }

        /// <summary>
        /// Get the AppSettings path where config files are stored,
        /// if we're running on Windows.
        /// </summary>
        /// <returns>Returns a string containing the AppSettings path, if available.</returns>
        private string? GetWindowsAppSettingsConfigPath()
        {
            if (OperatingSystem.IsWindows())
            {
                // Adapt from NetTallyConfig
            }

            return null;
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Get the services provided by the core library.
            NetTally.Startup.ConfigureServices(services);

            // Then add services known by the current assembly,
            // or override services provided by the core library.

            //services.Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Debug);

            // Add IoCNavigationService for the application.
            services.AddSingleton<IoCNavigationService>();

            // Register all the Windows of the applications via the service provider.
            services.AddTransient<MainWindow>();
            services.AddTransient<GlobalOptions>();
            services.AddTransient<QuestOptions>();
            services.AddTransient<ManageVotes>();
            services.AddTransient<ReorderTasks>();
        }

        private void ConfigureLogging(HostBuilderContext context, ILoggingBuilder builder)
        {
            builder
                .AddDebug()
                .AddFile(options =>
                {
                    options.LogDirectory = GetLoggingDirectoryPath();
                    options.Periodicity = PeriodicityOptions.Daily;
                    options.RetainedFileCountLimit = 7;
                })
                .AddFilter<DebugLoggerProvider>(DebugLoggingFilter)
                .AddFilter<FileLoggerProvider>(FileLoggingFilter);
        }

        /// <summary>
        /// Get a logging directory to save file logs to.
        /// </summary>
        /// <returns>Returns a path to a directory to store log files in.</returns>
        private static string GetLoggingDirectoryPath()
        {
            try
            {
                // First check where the runtime is located.  If it's the same as the application itself,
                // this is a self-contained app, and it should use a local Logs directory.
                string runtimePath = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
                string appLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string? appPath = Path.GetDirectoryName(appLocation);

                if (appPath is not null &&
                    string.Compare(Path.GetFullPath(runtimePath).TrimEnd('\\'),
                                   Path.GetFullPath(appPath).TrimEnd('\\'),
                                   StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    return "Logs";
                }

                // If we're not running a self-contained app, check for permissions to write
                // to the application data folder. Windows-only.
                if (OperatingSystem.IsWindows())
                {
                    string loggingPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

                    if (Directory.Exists(loggingPath))
                    {
                        loggingPath = Path.Combine(loggingPath, ProductInfo.Name);
                        Directory.CreateDirectory(loggingPath);

                        return loggingPath;
                    }
                }
            }
            catch (Exception)
            {
            }

            // If we don't have access to the AppData path, just fall back to a Logs subdirectory.
            return "Logs";
        }

        private static bool FileLoggingFilter(string? category, LogLevel logLevel)
        {
            if (AdvancedOptions.Instance.DebugMode)
                return logLevel >= LogLevel.Debug;

            return logLevel >= LogLevel.Warning;
        }

        private static bool DebugLoggingFilter(string? category, LogLevel logLevel)
        {
            if (AdvancedOptions.Instance.DebugMode)
                return true;

            return logLevel >= LogLevel.Debug;
        }
    }
}
