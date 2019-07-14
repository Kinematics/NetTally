using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using NetTally.SystemInfo;
using NetTally.ViewModels;

namespace NetTally.Tests
{
    public static class TestStartup
    {
        public static IServiceProvider ConfigureServices()
        {
            IServiceCollection services = new ServiceCollection();

            // Get the services provided by the core library.
            NetTally.Startup.ConfigureServices(services);

            //services.AddSingleton<IPageProvider, TestPageProvider>();
            //services.AddSingleton<IHash, NormalHash>();
            services.AddSingleton<IClock, StaticClock>();

            var serviceProvider = services.BuildServiceProvider();
            serviceProvider.GetRequiredService<ViewModelService>();

            return serviceProvider;
        }
    }
}
