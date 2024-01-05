using System.Text.RegularExpressions;

namespace NationalInstruments.Analyzers.Utilities.Extensions
{
    /// <summary>
    /// Class that contains helpful extensions to strings.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Returns <c>true</c> if the <paramref name="input"/> matches the <paramref name="wildcardPattern"/>,
        /// <c>false</c> otherwise.
        /// </summary>
        /// <remarks>
        /// The wildcard pattern is not a regular expression. Instead, it works similar to Windows Explorer's
        /// search where an asterisk can be any number of characters.
        /// </remarks>
        /// <param name="input">Any text.</param>
        /// <param name="wildcardPattern">Any text that optionally includes "wildcards", or asterisks.</param>
        /// <returns>True or false depending on whether the given input matches the given wildcard pattern or not.</returns>
        public static bool MatchesWildcardPattern(this string? input, string? wildcardPattern)
        {
            var pattern = string.Concat("^", Regex.Escape(wildcardPattern).Replace(@"\*", ".*"), "$");
            return Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase);
        }
    }
}
