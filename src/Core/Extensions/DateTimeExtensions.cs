using System;

namespace NationalInstruments.Tools.Extensions
{
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Returns whether or not <paramref name="current"/> is after <paramref name="other"/>.
        /// </summary>
        /// <param name="current">The first time.</param>
        /// <param name="other">The other time.</param>
        /// <returns>True if current is after other, false otherwise.</returns>
        public static bool After(this DateTime current, DateTime other)
        {
            return current > other;
        }

        /// <summary>
        /// Returns whether or not <paramref name="current"/> is before <paramref name="other"/>.
        /// </summary>
        /// <param name="current">The first time.</param>
        /// <param name="other">The other time.</param>
        /// <returns>True if current is before other, false otherwise.</returns>
        public static bool Before(this DateTime current, DateTime other)
        {
            return current < other;
        }
    }
}
