using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NetTally
{
    /// <summary>
    /// This class describes a profiled region that can be used to
    /// time how long a small area of code takes to execute.
    /// 
    /// Usage:
    /// 
    /// using (new RegionProfiler("name of region"))
    /// {
    ///     [Code to be profiled]
    /// }
    /// </summary>
    public readonly struct RegionProfiler : IDisposable
    {
        readonly long startTime;

        readonly TimeSpan minimumTime;
        readonly string regionName;
        readonly bool accumulate;
        readonly ILogger logger;

        readonly static Dictionary<string, double> accumulator = [];
        readonly static Dictionary<string, int> counter = [];


        /// <summary>
        /// Initializes a new instance of the <see cref="RegionProfiler" /> class.
        /// </summary>
        /// <param name="regionName">Name to identify the region being profiled when results are output.</param>
        /// <param name="minimumTime">If the profiler runs for less than this amount of time,
        /// it will not log any output.</param>
        /// <param name="accumulate">If set to <c>true</c>, tracks an accumulated value for
        /// the given profiler name across multiple runs.</param>
        public RegionProfiler(
            string regionName,
            TimeSpan? minimumTime = null,
            bool accumulate = false)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(regionName);
            ArgumentNullException.ThrowIfNull(AppX.Services);

            this.regionName = regionName;
            this.minimumTime = minimumTime ?? TimeSpan.Zero;
            this.accumulate = accumulate;

            if (!counter.ContainsKey(regionName))
                counter[regionName] = 0;
            if (!accumulator.ContainsKey(regionName))
                accumulator[regionName] = 0.0;

            ILoggerFactory loggerFactory = AppX.Services.GetRequiredService<ILoggerFactory>();
            logger = loggerFactory.CreateLogger($"RegionProfiler:{regionName}");

            if (!accumulate)
            {
                logger.LogInformation("Profiling started.");
            }

            startTime = Stopwatch.GetTimestamp();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            var delta = Stopwatch.GetElapsedTime(startTime);

            if (delta > minimumTime)
            {
                if (accumulate)
                {
                    counter[regionName]++;
                    accumulator[regionName] = accumulator[regionName] + delta.TotalMilliseconds;

                    logger.LogInformation("Accumulated time: Hit {counter} times for {accumulator} total ms (+{elapsed:F6} ms). Average: {average} ms",
                        counter[regionName], accumulator[regionName], delta.TotalMilliseconds, accumulator[regionName] / counter[regionName]);
                }
                else
                {
                    logger.LogInformation("Profiling ended: {elapsed:F6} ms.",
                        delta.TotalMilliseconds);
                }
            }
        }


        /// <summary>
        /// Resets this instance.
        /// </summary>
        public static void Reset()
        {
            foreach (var item in counter.Keys)
                counter[item] = 0;
            foreach (var item in accumulator.Keys)
                accumulator[item] = 0.0;
        }
    }

}
