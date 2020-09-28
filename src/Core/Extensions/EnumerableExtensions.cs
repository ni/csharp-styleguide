using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using NationalInstruments.Tools.Core;

namespace NationalInstruments.Tools.Extensions
{
    /// <summary>
    /// Class that contains helper methods when working with the Enumerable type
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Performs the specified action on each item of the enumeration.
        /// </summary>
        /// <typeparam name="T">Type in the enumeration</typeparam>
        /// <param name="items">items collection</param>
        /// <param name="action">Delegate specifying what action to be performed</param>
        public static void ForEach<T>(this IEnumerable<T> items, Action<T> action)
        {
            items.Verify(Verifier.IsNotNull, ExceptionGenerator.ArgumentNull, "items");
            action.Verify(Verifier.IsNotNull, ExceptionGenerator.ArgumentNull, "action");

            foreach (T t in items)
            {
                action(t);
            }
        }

        /// <summary>
        /// Performs the specified action asynchronously on each item of the enumeration in parallel.
        /// </summary>
        /// <typeparam name="T">Type in the enumeration</typeparam>
        /// <param name="items">Items collection</param>
        /// <param name="action">Delegate specifying what action to be performed</param>
        /// <param name="maxParallelCount">Max number of parallel actions, if zero the number of CPU cores is used</param>
        /// <returns></returns>
        public static async Task ForEachAsync<T>(this IEnumerable<T> items, Func<T, Task> action, int maxParallelCount = 0)
        {
            await ForEachAsync(items, (param, token) => action(param), CancellationToken.None, maxParallelCount).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs the specified action asynchronously on each item of the enumeration in parallel.
        /// </summary>
        /// <typeparam name="T">Type in the enumeration</typeparam>
        /// <param name="items">Items collection</param>
        /// <param name="action">Delegate specifying what action to be performed</param>
        /// <param name="token">The cancellation token which can be used to cancel the enumeration</param>
        /// <param name="maxParallelCount">Max number of parallel actions, if zero the number of CPU cores is used</param>
        /// <returns></returns>
        public static async Task ForEachAsync<T>(this IEnumerable<T> items, Func<T, CancellationToken, Task> action, CancellationToken token, int maxParallelCount = 0)
        {
            items.Verify(Verifier.IsNotNull, ExceptionGenerator.ArgumentNull, nameof(items));
            action.Verify(Verifier.IsNotNull, ExceptionGenerator.ArgumentNull, nameof(action));

            if (maxParallelCount == 0)
            {
                maxParallelCount = EnvironmentHelper.GetProcessorCount("FOREACHASYNC_CONCURRENCY");
            }

            var tasks = Partitioner.Create(items).GetPartitions(maxParallelCount).Select(
                partition => Task.Run(
                    async () =>
                    {
                        using (partition)
                        {
                            while (partition.MoveNext())
                            {
                                token.ThrowIfCancellationRequested();

                                await action(partition.Current, token).ConfigureAwait(false);
                            }
                        }
                    },
                    token));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <summary>
        /// Accumulates the result of the specified function on each item of the enumeration.
        /// </summary>
        /// <typeparam name="TResult">Return type</typeparam>
        /// <typeparam name="T">The type in the enumeration.</typeparam>
        /// <param name="list">The enumeration over which to accumulate.</param>
        /// <param name="initial">The initial result to combine.</param>
        /// <param name="func">A function mapping an accumulator T and another T to T.</param>
        /// <returns>The result of combining successive result of func with each element in list, or initial if list is empty.</returns>
        /// <remarks>A simple application of Inject is summing over an <code>IEnumerable&lt;int&gt;</code>: <code>list.Inject(0, (accum, i) => accum + i);</code></remarks>
        public static TResult Inject<TResult, T>(this IEnumerable<T> list, TResult initial, Func<TResult, T, TResult> func)
        {
            list.VerifyArgumentIsNotNull(nameof(list));
            func.VerifyArgumentIsNotNull(nameof(func));

            var accum = initial;
            foreach (var t in list)
            {
                accum = func(accum, t);
            }

            return accum;
        }

        /// <summary>
        /// Adds the contents of an IEnumerable to a Collection.
        /// </summary>
        /// <typeparam name="T">The type in the IEnumerable and in the Collection.</typeparam>
        /// <param name="collection">The collection to add to.</param>
        /// <param name="enumeration">The enumerable to add from.</param>
        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> enumeration)
        {
            collection.VerifyArgumentIsNotNull(nameof(collection));
            enumeration.VerifyArgumentIsNotNull(nameof(enumeration));

            foreach (var t in enumeration)
            {
                collection.Add(t);
            }
        }

        /// <summary>
        /// Creates a HashSet from the given enumerable
        /// </summary>
        /// <typeparam name="T">Element type</typeparam>
        /// <param name="source">Enumerable to convert</param>
        /// <param name="comparer">HashSet comparer</param>
        /// <returns>A HashSet</returns>
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer = null)
        {
            return new HashSet<T>(source, comparer);
        }

        /// <summary>
        /// Creates an enumerator that steps linearly over an array.
        /// </summary>
        /// <typeparam name="T">The type of the array's contents.</typeparam>
        /// <param name="array">The array over which to step.</param>
        /// <param name="start">The index to start at.</param>
        /// <param name="end">The index to stop at (is included in the enumeration if applicable).</param>
        /// <param name="stepSize">The stride length.</param>
        /// <returns>An IEnumerable that enumerates all the indices of the array between start and end inclusive, stepping by step.</returns>
        public static IEnumerable<T> Step<T>(this T[] array, int start, int end, int stepSize)
        {
            array.VerifyArgumentIsNotNull(nameof(array));

            if (start < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Start index must be greater than or equal to 0.");
            }

            if (end >= array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(end), "End index must be less than the length of the array.");
            }

            for (var i = start; i <= end; i += stepSize)
            {
                yield return array[i];
            }
        }

        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> collection, int batchSize)
        {
            var nextbatch = new List<T>(batchSize);
            foreach (T item in collection)
            {
                nextbatch.Add(item);
                if (nextbatch.Count == batchSize)
                {
                    yield return nextbatch;
                    nextbatch = new List<T>();
                }
            }

            if (nextbatch.Count > 0)
            {
                yield return nextbatch;
            }
        }

        public static byte[] ComputeHash(this IEnumerable<string> source)
        {
            var list = new List<string>(source);
            list.Sort();

            var result = string.Join(string.Empty, list);

            using (var sha = new SHA512Managed())
            {
                return sha.ComputeHash(StringExtensions.GetBytesFromString(result));
            }
        }

        /// <summary>
        /// Randomizes the order of elements
        /// </summary>
        /// <remarks>
        /// Uses a variant of Fisher-Yates shuffle. Based on the pseudocode in https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle
        /// </remarks>
        /// <typeparam name="T">Type of elements to shuffle</typeparam>
        /// <param name="source">Elements to shuffle</param>
        /// <returns>Returns the items in source in a randomized order.</returns>
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
        {
            var random = new Random();
            var result = new List<T>();

            foreach (var item in source)
            {
                var j = random.Next(result.Count + 1);
                if (j == result.Count)
                {
                    result.Add(item);
                }
                else
                {
                    result.Add(result[j]);
                    result[j] = item;
                }
            }

            return result;
        }

        public static bool CountExceedsThreshold<T>(this IEnumerable<T> items, Func<T, bool> predicate, int threshold)
        {
            return items.Count(b => predicate(b)) > threshold;
        }

        public static bool CountExceedsThreshold(this IEnumerable<bool> items, int threshold)
        {
            return items.Count(b => b) > threshold;
        }
    }
}
