using System;
using Microsoft.Extensions.DependencyInjection;
using NetTally.Utility.Comparers;

namespace NetTally.Data;
public class CoreApp
{
    public static IServiceProvider? ServiceProvider { get; private set; }

    public static void SetServiceProvider(IServiceProvider? serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        ServiceProvider = serviceProvider;
        _ = serviceProvider.GetRequiredService<Agnostic>();
    }
}
