using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using NetTally.Systems;

namespace NetTally.Tests
{
    /// <summary>
    /// Class to initialize the hosting/logging/DI systems when
    /// running tests.
    /// </summary>
    public static class TestStartup
    {
        private static FakeTimeProvider? fakeTimeProvider;

        /// <summary>
        /// Call the initialization in the core library, with a callback to
        /// initialize testing-specific service injection.
        /// </summary>
        /// <param name="fakeTimeProvider">An optional fake time provider
        /// that a test class may need, to be injected into the DI system.</param>
        /// <returns>The service provider that the hosting system built.</returns>
        public static IServiceProvider ConfigureServices(FakeTimeProvider? fakeTimeProvider = null)
        {
            TestStartup.fakeTimeProvider = fakeTimeProvider;

            AppX.Initialize(AddTestServices);

            return AppX.Services;
        }

        /// <summary>
        /// Callback for setting up DI services that will override the
        /// services added by the core library.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        private static void AddTestServices(IServiceCollection services)
        {
            if (fakeTimeProvider != null)
            {
                services.AddSingleton<TimeProvider>(fakeTimeProvider);
            }
        }
    }
}
