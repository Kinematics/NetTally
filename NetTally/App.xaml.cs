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
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using NetTally.Navigation;
using NetTally.ViewModels;

namespace NetTally
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IServiceProvider? _serviceProvider;
        public IServiceProvider ServiceProvider
        {
            get
            {
                return _serviceProvider ?? throw new InvalidOperationException("No service provider set.");
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // Create a service collection and configure our dependencies
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            // Build the IServiceProvider and set our reference to it
            _serviceProvider = serviceCollection.BuildServiceProvider();

            _serviceProvider.GetRequiredService<ViewModelService>();

            // Request the navigation service and create our main window.
            var navigationService = ServiceProvider.GetRequiredService<IoCNavigationService>();
            var task = navigationService.ShowAsync<MainWindow>();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Get the services provided by the core library.
            NetTally.Startup.ConfigureServices(services);

            // Then add services known by the current assembly,
            // or override services provided by the core library.

            // Add IoCNavigationService for the application.
            services.AddSingleton<IoCNavigationService>();

            // Register all the Windows of the applications via the service provider.
            services.AddTransient<MainWindow>();
            services.AddTransient<GlobalOptionsWindow>();
            services.AddTransient<QuestOptionsWindow>();
            services.AddTransient<ManageVotesWindow>();
            services.AddTransient<ReorderTasksWindow>();
        }

    }
}
