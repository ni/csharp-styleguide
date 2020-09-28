using System;
using System.Diagnostics;
using Xunit;

namespace NationalInstruments.Tools.TestUtilities
{
    public static class Performance
    {
        /// <summary>
        /// Perform an operation and assert that the best case performance is not worse than some upper bound.
        /// </summary>
        /// <param name="action">Action to perform</param>
        /// <param name="iterations">Iterations to run</param>
        /// <param name="maxBestCase">Upper bound for best case performance, worse than this will fail.</param>
        public static void PerformanceGutCheck(Action action, int iterations, long maxBestCase)
        {
            var watch = new Stopwatch();
            TimeSpan min = TimeSpan.MaxValue;
            TimeSpan max = TimeSpan.MinValue;
            TimeSpan total = TimeSpan.Zero;

            for (var i = 0; i < iterations; ++i)
            {
                watch.Reset();
                watch.Start();
                action();
                watch.Stop();

                total += watch.Elapsed;
                max = watch.Elapsed > max ? watch.Elapsed : max;
                min = watch.Elapsed < min ? watch.Elapsed : min;
            }

            var averageMilliseconds = total.TotalMilliseconds / iterations;

            Console.WriteLine("Min: {0} Max: {1} Avg: {2} msec", min.TotalMilliseconds, max.TotalMilliseconds, averageMilliseconds);
            Assert.True(min.TotalMilliseconds < maxBestCase);
        }
    }
}
