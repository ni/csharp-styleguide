using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;

namespace NationalInstruments.Tools
{
    public static class Retry
    {
        // This is the original RetryOperation from the DirectoryCleanup command line utility, unmodified with the exception of moving it to Core.
        // Suggest using one of the other overloads below - this one seems rather situation-specific, and ideally we would get rid of it.
        public static void RetryOperation<T>(Action<T> operation, Func<T, Exception, int, bool> doRetry, T state)
        {
            var retryLength = 1;

            while (true)
            {
                try
                {
                    operation(state);
                    break;
                }
                catch (Exception exception)
                {
                    retryLength *= 2;

                    if (!doRetry(state, exception, retryLength))
                    {
                        break;
                    }

                    Thread.Sleep(retryLength * 1000);
                }
            }
        }

        public static async Task RetryOperationAsync(Func<Task> operation, CancellationToken token, bool throwOnFailure = false, IEnumerable<TimeSpan> progressiveTimeout = null)
        {
            await RetryOperationAsync(operation, ex => true, token, throwOnFailure, progressiveTimeout).ConfigureAwait(false);
        }

        public static async Task<T> RetryOperationAsync<T>(Func<Task<T>> operation, CancellationToken token, bool throwOnFailure = false, IEnumerable<TimeSpan> progressiveTimeout = null)
        {
            return await RetryOperationAsync(operation, ex => true, token, throwOnFailure, progressiveTimeout).ConfigureAwait(false);
        }

        public static async Task RetryOperationAsync(Func<Task> operation, Func<Exception, bool> canRetryFunc, CancellationToken token, bool throwOnFailure = false, IEnumerable<TimeSpan> progressiveTimeout = null)
        {
            await RetryOperationAsync(
                async () =>
                {
                    await operation().ConfigureAwait(false);
                    return 0;
                },
                canRetryFunc,
                token,
                throwOnFailure,
                progressiveTimeout).ConfigureAwait(false);
        }

        public static async Task<T> RetryOperationAsync<T>(Func<Task<T>> operation, Func<Exception, bool> canRetryFunc, CancellationToken token, bool throwOnFailure = false, IEnumerable<TimeSpan> progressiveTimeout = null, ILogger logger = null)
        {
            if (progressiveTimeout == null)
            {
                progressiveTimeout = CreateDefaultProgressiveTimeout();
            }

            var retryCount = 0;
            using (var timeoutEnumerator = progressiveTimeout.GetEnumerator())
            {
                while (true)
                {
                    token.ThrowIfCancellationRequested();

                    ++retryCount;
                    try
                    {
                        return await operation().ConfigureAwait(false);
                    }
                    catch (Exception e) when (canRetryFunc(e))
                    {
                        if (timeoutEnumerator.MoveNext())
                        {
                            logger?.LogInformation("Retrying operation (try #" + retryCount + "), waiting for " + timeoutEnumerator.Current + " ms due to exception: " + e.Message);
                            await Task.Delay(timeoutEnumerator.Current, token).ConfigureAwait(false);
                        }
                        else
                        {
                            logger?.LogError("RH0001: " + "Unable to perform operation. Final Exception:" + e);

                            if (throwOnFailure)
                            {
                                throw;
                            }
                        }
                    }
                }
            }
        }

        public static Task<T> RetryOperationAsync<T>(Func<T> operation, CancellationToken token, bool throwOnFailure = false, IEnumerable<TimeSpan> progressiveTimeout = null)
        {
            return RetryOperationAsync(operation, ex => true, token, throwOnFailure, progressiveTimeout);
        }

        public static Task RetryOperationAsync(Action operation, CancellationToken token, bool throwOnFailure = false, IEnumerable<TimeSpan> progressiveTimeout = null)
        {
            return RetryOperationAsync(operation, ex => true, token, throwOnFailure, progressiveTimeout);
        }

        public static Task RetryOperationAsync(Action operation, Func<Exception, bool> canRetryFunc, CancellationToken token, bool throwOnFailure = false, IEnumerable<TimeSpan> progressiveTimeout = null)
        {
            return RetryOperationAsync(
                () =>
                {
                    operation();
                    return 0;
                },
                canRetryFunc,
                token,
                throwOnFailure,
                progressiveTimeout);
        }

        public static async Task<T> RetryOperationAsync<T>(Func<T> operation, Func<Exception, bool> canRetryFunc, CancellationToken token, bool throwOnFailure = false, IEnumerable<TimeSpan> progressiveTimeout = null, ILogger logger = null)
        {
            if (progressiveTimeout == null)
            {
                progressiveTimeout = CreateDefaultProgressiveTimeout();
            }

            var retryCount = 0;
            using (var timeoutEnumerator = progressiveTimeout.GetEnumerator())
            {
                while (true)
                {
                    token.ThrowIfCancellationRequested();

                    ++retryCount;
                    try
                    {
                        return operation();
                    }
                    catch (Exception e) when (canRetryFunc(e))
                    {
                        if (timeoutEnumerator.MoveNext())
                        {
                            logger?.LogInformation("Retrying operation (try #" + retryCount + "), waiting for " + timeoutEnumerator.Current + " ms due to exception: " + e.Message);

                            await Task.Delay(timeoutEnumerator.Current, token).ConfigureAwait(false);
                        }
                        else
                        {
                            logger?.LogError("RH0001: " + "Unable to perform operation. Final Exception:" + e);

                            if (throwOnFailure)
                            {
                                throw;
                            }
                        }
                    }
                }
            }
        }

        public static T RetryOperation<T>(Func<T> operation, CancellationToken token, bool throwOnFailure = false, IEnumerable<TimeSpan> progressiveTimeout = null)
        {
            return RetryOperation(operation, ex => true, token, throwOnFailure, progressiveTimeout);
        }

        public static void RetryOperation(Action operation, CancellationToken token, bool throwOnFailure = false, IEnumerable<TimeSpan> progressiveTimeout = null)
        {
            RetryOperation(operation, ex => true, token, throwOnFailure, progressiveTimeout);
        }

        public static void RetryOperation(Action operation, Func<Exception, bool> canRetryFunc, CancellationToken token, bool throwOnFailure = false, IEnumerable<TimeSpan> progressiveTimeout = null)
        {
            RetryOperation(
                () =>
                {
                    operation();
                    return 0;
                },
                canRetryFunc,
                token,
                throwOnFailure,
                progressiveTimeout);
        }

        public static T RetryOperation<T>(Func<T> operation, Func<Exception, bool> canRetryFunc, CancellationToken token, bool throwOnFailure = false, IEnumerable<TimeSpan> progressiveTimeout = null, ILogger logger = null)
        {
            if (progressiveTimeout == null)
            {
                progressiveTimeout = CreateDefaultProgressiveTimeout();
            }

            var retryCount = 0;
            using (var timeoutEnumerator = progressiveTimeout.GetEnumerator())
            {
                while (true)
                {
                    token.ThrowIfCancellationRequested();

                    ++retryCount;
                    try
                    {
                        return operation();
                    }
                    catch (Exception e) when (canRetryFunc(e))
                    {
                        if (timeoutEnumerator.MoveNext())
                        {
                            logger?.LogInformation("Retrying operation (try #" + retryCount + "), waiting for " + timeoutEnumerator.Current + " ms due to exception: " + e.Message);
                            var cancelled = token.WaitHandle.WaitOne(timeoutEnumerator.Current);

                            if (cancelled)
                            {
                                return default;
                            }
                        }
                        else
                        {
                            logger?.LogError("RH0001: " + "Unable to perform operation. Final Exception:" + e);

                            if (throwOnFailure)
                            {
                                throw;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Exponential backoff algorithm for calculating retry intervals.
        /// </summary>
        /// <param name="frameSize">The expected duration of the operation.</param>
        /// <param name="maxValue">The maximum duration to wait.</param>
        /// <param name="retryCount">Number of results to return.</param>
        /// <param name="jitter">Randomness in the waiting, in the range of [0.0, 1.0]. See remarks for details.</param>
        /// <remarks>The jitter can be used to set randomness in the resulting waiting times. A value of 0.0 means deterministic wait times and a value of
        /// 1.0 means totally random waiting times. If the retry is necessary because of contention (e.g.: file access) a higher value is recommended, to
        /// make sure the retries are not occuring at the same time, otherwise a lower value should be used.</remarks>
        /// <returns>A collection of calculated waiting times.</returns>
        public static IEnumerable<TimeSpan> ExponentialBackoff(TimeSpan frameSize, TimeSpan maxValue, int retryCount, double jitter = 0.0)
        {
            return ExponentialBackoff(frameSize.Ticks, maxValue.Ticks, retryCount, jitter).Select(x => new TimeSpan(x));
        }

        /// <summary>
        /// Exponential backoff algorithm for calculating retry intervals.
        /// </summary>
        /// <param name="frameSize">The expected unit of time the operation is expected to complete in.</param>
        /// <param name="maxValue">The maximum unit of time to wait.</param>
        /// <param name="retryCount">Number of results to return.</param>
        /// <param name="jitter">Randomness in the waiting, in the range of [0.0, 1.0]. See remarks for details.</param>
        /// <remarks>The jitter can be used to set randomness in the resulting waiting times. A value of 0.0 means deterministic wait times and a value of
        /// 1.0 means totally random waiting times. If the retry is necessary because of contention (e.g.: file access) a higher value is recommended, to
        /// make sure the retries are not occuring at the same time, otherwise a lower value should be used.</remarks>
        /// <returns>A collection of calculated waiting times.</returns>
        public static IEnumerable<long> ExponentialBackoff(long frameSize, long maxValue, int retryCount, double jitter = 0.0)
        {
            if (frameSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(frameSize));
            }

            if (maxValue < frameSize)
            {
                throw new ArgumentOutOfRangeException(nameof(maxValue));
            }

            if (jitter < 0.0 || jitter > 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(jitter));
            }

            IEnumerable<long> ExponentialBackoffEnumerator()
            {
                var currentMaxValue = frameSize;
                var random = new Random();
                for (var i = 0; i < retryCount; i++)
                {
                    var jitterRange = (long)(currentMaxValue * jitter);
                    var minValue = currentMaxValue - jitterRange;
                    var jitterValue = (long)(random.NextDouble() * jitterRange);
                    yield return minValue + jitterValue;

                    if (currentMaxValue < maxValue)
                    {
                        unchecked
                        {
                            currentMaxValue = currentMaxValue << 1;
                        }

                        if (currentMaxValue <= 0 || currentMaxValue > maxValue)
                        {
                            currentMaxValue = maxValue;
                        }
                    }
                }
            }

            return ExponentialBackoffEnumerator();
        }

        private static IEnumerable<TimeSpan> CreateDefaultProgressiveTimeout()
        {
            return ExponentialBackoff(TimeSpan.FromMilliseconds(100), TimeSpan.FromMinutes(1), 10, 1.0);
        }
    }
}
