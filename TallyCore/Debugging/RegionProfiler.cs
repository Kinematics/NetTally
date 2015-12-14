using System;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="RegionProfiler"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public RegionProfiler(string name)
            : this(name, TimeSpan.Zero)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RegionProfiler"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="watermark">The watermark param.</param>
        public RegionProfiler(string name, TimeSpan watermark)
        {
            this.watermark = watermark;
            regionName = name;
            if (regionName != null)
                Trace.WriteLine($"Start Profiling: {regionName}");

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
                    Trace.WriteLine($"End Profiling: {stopwatch.Elapsed.TotalMilliseconds} ms in region {regionName}");
                }
            }
            else
            {
                Debug.WriteLine($"Region {regionName ?? "<unnamed>"} not finalized by Dispose call!");
            }
        }
    }
}
