using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetTally.SystemInfo;
using NetTally.Utility.Comparers;
using NetTally.Global;

namespace NetTally.Tests
{
    public static class TestStartup
    {
        static IHost? host;

        public static IServiceProvider ConfigureServices()
        {
            host = Host.CreateDefaultBuilder()
                    .ConfigureServices((context, services) =>
                    {
                        Startup.ConfigureServices(services);
                        services.Configure<GlobalSettings>(context.Configuration.GetSection(nameof(GlobalSettings)));
                        services.Configure<UserQuests>(context.Configuration.GetSection(nameof(UserQuests)));
                        services.AddSingleton<IClock, StaticClock>();
                    })
                    .ConfigureLogging((context, builder) =>
                    {
                        builder.AddDebug();
                    })
                    .Build();

            var hash = host.Services.GetRequiredService<IHash>();
            Agnostic.Init(hash);

            return host.Services;
        }
    }
}
