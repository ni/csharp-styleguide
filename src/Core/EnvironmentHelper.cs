using System;

namespace NationalInstruments.Tools.Core
{
    public static class EnvironmentHelper
    {
        /// <summary>
        /// Attempts to look up:
        /// 1. Processor count override environment variable
        /// 2. Hardcoded docker environment variable with scaling
        /// 3. The environment processor number with scaling
        /// Returns first found in that order
        /// </summary>
        /// <param name="environmentVariableName">Name of the environment variable containing the concurrency override value</param>
        /// <param name="defaultScalingFactor"></param>
        /// <returns>Processor count from environment variable or Environment.ProcessorCount with scaling factor applied</returns>
        public static int GetProcessorCount(string environmentVariableName, int defaultScalingFactor = 1)
        {
            var concurrencyOverrideRaw = Environment.GetEnvironmentVariable(environmentVariableName);
            if (!string.IsNullOrEmpty(concurrencyOverrideRaw) && int.TryParse(concurrencyOverrideRaw, out var concurrencyOverride) && concurrencyOverride > 0)
            {
                return concurrencyOverride;
            }

            // When experimenting with running builds inside Docker, there is a limitation in the Windows implementation of Docker:
            // Using process isolation, the number of cores the container can see is the actual amount of cores the machine has
            // This environment variable is for the Docker override use case
            concurrencyOverrideRaw = Environment.GetEnvironmentVariable("PROCESSOR_COUNT");
            if (!string.IsNullOrEmpty(concurrencyOverrideRaw) && int.TryParse(concurrencyOverrideRaw, out concurrencyOverride) && concurrencyOverride > 0)
            {
                return concurrencyOverride * defaultScalingFactor;
            }

            return Math.Max(1, Environment.ProcessorCount) * defaultScalingFactor;
        }
    }
}
