using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetTally.SystemInfo;
using NetTally.Utility.Comparers;

namespace NetTally.Tests
{
    public static class TestStartup
    {
        public static IServiceProvider ConfigureServices()
        {
            IServiceCollection services = new ServiceCollection();

            // Get the services provided by the core library.
            Startup.ConfigureServices(services);

            //services.AddSingleton<IPageProvider, TestPageProvider>();
            //services.AddSingleton<IHash, NormalHash>();
            services.AddSingleton<IClock, StaticClock>();

            var serviceProvider = services.BuildServiceProvider();

            var hash = serviceProvider.GetRequiredService<IHash>();
            Agnostic.Init(hash);

            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger2 = serviceProvider.GetRequiredService<Logger2>();
            var agnostic = serviceProvider.GetRequiredService<IAgnostic>();

            var logger = loggerFactory.CreateLogger("TestStartup");
            logger.LogInformation("Services defined for testing!");

            return serviceProvider;
        }
    }
}
