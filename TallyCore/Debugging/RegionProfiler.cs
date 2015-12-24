using System;
using System.Collections.Generic;
using System.Diagnostics;

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
    public sealed class RegionProfiler : IDisposable
    {
        readonly Stopwatch stopwatch = new Stopwatch();

        readonly TimeSpan watermark;
        readonly string regionName;
        readonly bool accumulate;

        readonly static Dictionary<string, double> accumulator = new Dictionary<string, double>();
        readonly static Dictionary<string, int> counter = new Dictionary<string, int>();

        /// <summary>
        /// Initializes a new instance of the <see cref="RegionProfiler"/> class.
        /// </summary>
        /// <param name="name">Name to identify this object when results are output.</param>
        public RegionProfiler(string name)
            : this(name, TimeSpan.Zero, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RegionProfiler"/> class.
        /// </summary>
        /// <param name="name">Name to identify this object when results are output.</param>
        /// <param name="accumulate">Flag for whether to accumulate results for this named region.</param>
        public RegionProfiler(string name, bool accumulate)
            : this(name, TimeSpan.Zero, accumulate)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RegionProfiler"/> class.
        /// </summary>
        /// <param name="name">Name to identify this object when results are output.</param>
        /// <param name="watermark">The watermark param.</param>
        /// <param name="accumulate">Flag for whether to accumulate results for this named region.</param>
        public RegionProfiler(string name, TimeSpan watermark, bool accumulate)
        {
            this.accumulate = accumulate;
            this.watermark = watermark;

            regionName = name;
            if (regionName != null && !accumulate)
                Trace.WriteLine($"Start profiling in region [{regionName}]");

            stopwatch.Start();
        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="RegionProfiler"/> is reclaimed by garbage collection.
        /// </summary>
        ~RegionProfiler()
        {
            Dispose(false);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposed"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        public void Dispose(bool disposed)
        {
            if (disposed)
            {
                stopwatch.Stop();

                if (regionName != null && stopwatch.Elapsed > watermark)
                {
                    if (accumulate)
                    {
                        if (!counter.ContainsKey(regionName))
                            counter[regionName] = 0;
                        if (!accumulator.ContainsKey(regionName))
                            accumulator[regionName] = 0.0;

                        counter[regionName]++;
                        accumulator[regionName] = accumulator[regionName] + stopwatch.Elapsed.TotalMilliseconds;

                        Trace.WriteLine($"Region [{regionName}]: Hit {counter[regionName]} times for {accumulator[regionName]} total ms (+{stopwatch.Elapsed.TotalMilliseconds} ms). Average: {accumulator[regionName]/counter[regionName]} ms.");
                    }
                    else
                    {
                        Trace.WriteLine($"End profiling in region [{regionName}]: {stopwatch.Elapsed.TotalMilliseconds} ms");
                    }
                }
            }
            else
            {
                Debug.WriteLine($"Region {regionName ?? "<unnamed>"} not finalized by Dispose call!");
            }
        }

        /// <summary>
        /// Resets this instance.
        /// </summary>
        public static void Reset()
        {
            accumulator.Clear();
            counter.Clear();
        }
    }
}
