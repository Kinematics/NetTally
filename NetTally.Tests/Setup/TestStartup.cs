using System;
using Microsoft.Extensions.DependencyInjection;
using NetTally.SystemInfo;
using NetTally.Systems;

namespace NetTally.Tests
{
    public static class TestStartup
    {
        public static IServiceProvider ConfigureServices()
        {
            AppX.Initialize(AddTestServices);
            return AppX.Services;
        }

        private static void AddTestServices(IServiceCollection services)
        {
            services.AddSingleton<IClock, StaticClock>();
        }
    }
}
