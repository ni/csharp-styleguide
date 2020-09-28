using System.Collections.Generic;
using System.Linq;

namespace NationalInstruments.Tools.Analyzers.Utilities.Extensions
{
    /// <summary>
    /// Class that contains useful extensions to <see cref="IEnumerable{T}"/>.
    /// </summary>
    public static class IEnumerableExtensions
    {
        /// <summary>
        /// Safe enumerable (force non-null).
        /// </summary>
        /// <typeparam name="T">Any type</typeparam>
        /// <param name="enumerable">The enumerable to return, or empty if null.</param>
        /// <returns><paramref name="enumerable"/>, or <see cref="Enumerable.Empty{T}"/> if null</returns>
        public static IEnumerable<T> ToSafeEnumerable<T>(this IEnumerable<T> enumerable)
        {
            return enumerable ?? Enumerable.Empty<T>();
        }
    }
}
