using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace NationalInstruments.Tools.Extensions
{
    public static partial class StringExtensions
    {
        private static readonly Dictionary<string, Func<string>> DefaultSubstitutions = new Dictionary<string, Func<string>>(StringComparer.OrdinalIgnoreCase)
        {
            { "Date", () => DateTime.Now.ToString("d", CultureInfo.InvariantCulture) },
            { "DateTime", () => DateTime.Now.ToString(CultureInfo.InvariantCulture) },
            { "CurrentDirectory", () => Directory.GetCurrentDirectory() },
            { "AssemblyDirectory", GetAssemblyDirectory },
        };

        public static bool IsWildcardMatch(string wildcard, string text, bool caseSensitive)
        {
            var sb = new StringBuilder(wildcard.Length + 10);
            sb.Append("^");
            foreach (var c in wildcard)
            {
                switch (c)
                {
                    case '*':
                        sb.Append(".*");
                        break;
                    default:
                        sb.Append(Regex.Escape(c.ToString(CultureInfo.CurrentCulture)));
                        break;
                }
            }

            sb.Append("$");

            var regex = new Regex(sb.ToString(), caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);

            return regex.IsMatch(text);
        }

        /// <summary>
        /// remove trailing '\' from the string, if it exists
        /// </summary>
        /// <param name="path">any path</param>
        /// <returns>path without '\' at the end</returns>
        public static string TrimTrailingSlash(this string path)
        {
            if (path.EndsWith(@"\", StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(0, path.Length - 1);
            }

            return path;
        }

        public static float CompareStrings(string itemOne, string itemTwo)
        {
            var size = itemTwo.Length;
            if (size <= 0)
            {
                return 0.0f;
            }

            if (itemOne.Length > itemTwo.Length)
            {
                size = itemOne.Length;
            }

            var current = 0;
            float equalchars = 0;
            while (current < itemOne.Length && current < itemTwo.Length)
            {
                if (itemOne[current] == itemTwo[current])
                {
                    equalchars++;
                }

                current++;
            }

            return equalchars / size;
        }

        public static byte[] GetBytesFromString(string text)
        {
            var utf8 = new UTF8Encoding();
            return utf8.GetBytes(text);
        }

        public static string GetStringFromBytes(byte[] value)
        {
            var utf8 = new UTF8Encoding();
            return utf8.GetString(value);
        }

        /// <summary>
        /// convert hex string to byte array
        /// </summary>
        /// <param name="hex">hex string</param>
        /// <returns>A byte array</returns>
        public static byte[] HexStringToByteArray(string hex)
        {
            var numberChars = hex.Length;
            var bytes = new byte[numberChars / 2];
            for (var i = 0; i < numberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return bytes;
        }

        /// <summary>
        /// replace that allows comparison type
        /// </summary>
        /// <param name="original">original string</param>
        /// <param name="oldValue">value to be replaced</param>
        /// <param name="newValue">replacement value</param>
        /// <param name="comparisonType">string comparison type</param>
        /// <returns>The new string</returns>
        public static string Replace(this string original, string oldValue, string newValue, StringComparison comparisonType)
        {
            var startIndex = 0;
            while (true)
            {
                startIndex = original.IndexOf(oldValue, startIndex, comparisonType);
                if (startIndex == -1)
                {
                    break;
                }

                original = original.Substring(0, startIndex) + newValue + original.Substring(startIndex + oldValue.Length);

                startIndex += newValue.Length;
            }

            return original;
        }

        public static int ToInt32InvariantCulture(this string input)
        {
            return Convert.ToInt32(input, CultureInfo.InvariantCulture);
        }

        public static int FindSubstring(this string line, string search, out int indexAfter, int startAt = 0)
        {
            var index = line.IndexOf(search, startAt, StringComparison.Ordinal);
            indexAfter = index + search.Length;
            return index;
        }

        /// <summary>
        /// Applies XML escaping to a string.
        /// </summary>
        /// <param name="original">The string to escape.</param>
        /// <returns>The XML-escaped equivalent of <paramref name="original"/>.</returns>
        public static string XmlEscape(this string original)
        {
            var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
            var xmlTextWriter = new XmlTextWriter(stringWriter);
            try
            {
                xmlTextWriter.WriteString(original);
            }
            finally
            {
                xmlTextWriter.Close();
            }

            return stringWriter.ToString();
        }

        /// <summary>
        /// Allows values between $( and ) to be substituted at runtime.
        /// </summary>
        /// <param name="template">The string to do the substitutions on.</param>
        /// <param name="lookupFunc">Function which returns the values to substitute.</param>
        /// <returns>String with values substituted from <param name="lookupFunc">lookupFunc</param></returns>
        /// <exception cref="ArgumentException">The template parameter has invalid formatting or key has null value.</exception>
        /// <example>"Hello $(name)!".Substitute(x => "world"); // returns "Hello world!"</example>
        public static string Substitute(this string template, Func<string, string> lookupFunc)
        {
            var outputBuilder = new StringBuilder(template.Length);
            var macroNameBuilder = new StringBuilder();
            var state = ParserState.RawText;
            var index = 0;
            foreach (var character in template)
            {
                index++;
                switch (state)
                {
                    case ParserState.RawText:
                        if (character == '$')
                        {
                            state = ParserState.OpeningDelimiter;
                        }
                        else
                        {
                            outputBuilder.Append(character);
                        }

                        break;

                    case ParserState.OpeningDelimiter:
                        switch (character)
                        {
                            case '(':
                                state = ParserState.SecondDelimiter;
                                break;
                            case '$':
                                outputBuilder.Append("$");
                                break;
                            default:
                                state = ParserState.RawText;
                                outputBuilder.Append("$");
                                break;
                        }

                        break;
                    case ParserState.SecondDelimiter:
                        if (character == '(')
                        {
                            state = ParserState.RawText;
                            outputBuilder.Append("$(");
                        }
                        else
                        {
                            state = ParserState.ReadingTemplateName;
                            macroNameBuilder.Clear();
                            macroNameBuilder.Append(character);
                        }

                        break;
                    case ParserState.ReadingTemplateName:
                        if (character == ')')
                        {
                            state = ParserState.RawText;
                            var macroName = macroNameBuilder.ToString();
                            var resolved = lookupFunc(macroName);
                            if (resolved == null)
                            {
                                throw new ArgumentException("Failed to resolve macro: " + macroName, nameof(template));
                            }

                            outputBuilder.Append(resolved);
                        }
                        else
                        {
                            macroNameBuilder.Append(character);
                        }

                        break;
                    default:
                        throw new ArgumentException("Invalid character at " + index, nameof(template));
                }
            }

            switch (state)
            {
                case ParserState.OpeningDelimiter:
                    outputBuilder.Append("$");
                    return outputBuilder.ToString();
                case ParserState.RawText:
                    return outputBuilder.ToString();
                case ParserState.ReadingTemplateName:
                case ParserState.SecondDelimiter:
                    var startPosition = template.Length - macroNameBuilder.Length;
                    throw new ArgumentException("Template name starting at " + startPosition + " is not closed");
                default:
                    throw new NotImplementedException("Invalid final state: " + state);
            }
        }

        /// <summary>
        /// Allows values between $( and ) to be substituted at runtime.
        /// </summary>
        /// <param name="template">The string to do the substitutions on.</param>
        /// <exception cref="ArgumentException">The template parameter has invalid formatting or key cannot be found.</exception>
        /// <returns>String with values substituted from default sources (e.g.: default values, environment variables)</returns>
        /// <remarks>If no values from <paramref name="lookup"/> are found, the hard-coded values are tried,
        /// than finally the environment variables. If all of those fail, an ArgumentException is thrown.</remarks>
        public static string Substitute(this string template)
        {
            return template.Substitute(
                x =>
                {
                    if (DefaultSubstitutions.TryGetValue(x, out Func<string> defaultResult))
                    {
                        return defaultResult() ?? string.Empty;
                    }

                    return Environment.GetEnvironmentVariable(x);
                });
        }

        /// <summary>
        /// Allows values between $( and ) to be substituted at runtime.
        /// </summary>
        /// <param name="template">The string to do the substitutions on.</param>
        /// <param name="lookup">Dictionary which contains what to replace as keys without $( ), and with what as values.</param>
        /// <exception cref="ArgumentException">The template parameter has invalid formatting or key cannot be found.</exception>
        /// <returns>String with values substituted from <param name="lookup">lookup</param></returns>
        public static string Substitute(this string template, Dictionary<string, string> lookup)
        {
            return template.Substitute((IReadOnlyDictionary<string, string>)new ReadOnlyDictionary<string, string>(lookup));
        }

        /// <summary>
        /// Allows values between $( and ) to be substituted at runtime.
        /// </summary>
        /// <param name="template">The string to do the substitutions on.</param>
        /// <param name="lookup">IDictionary which contains what to replace as keys without $( ), and with what as values.</param>
        /// <exception cref="ArgumentException">The template parameter has invalid formatting or key cannot be found.</exception>
        /// <returns>String with values substituted from <param name="lookup">lookup</param></returns>
        public static string Substitute(this string template, IDictionary<string, string> lookup)
        {
            return template.Substitute((IReadOnlyDictionary<string, string>)new ReadOnlyDictionary<string, string>(lookup));
        }

        /// <summary>
        /// Allows values between $( and ) to be substituted at runtime.
        /// </summary>
        /// <param name="template">The string to do the substitutions on.</param>
        /// <param name="lookup">Read-only dictionary which contains what to replace as keys without $( ), and with what as values.</param>
        /// <exception cref="ArgumentException">The template parameter has invalid formatting or key cannot be found.</exception>
        /// <returns>String with values substituted from <paramref name="lookup" /></returns>
        /// <remarks>If no values from <paramref name="lookup"/> are found, the hard-coded values are tried,
        /// than finally the environment variables. If all of those fail, an ArgumentException is thrown.</remarks>
        public static string Substitute(this string template, IReadOnlyDictionary<string, string> lookup)
        {
            return Substitute(
                template,
                x =>
                {
                    if (lookup.TryGetValue(x, out var result))
                    {
                        return result ?? string.Empty;
                    }

                    if (DefaultSubstitutions.TryGetValue(x, out Func<string> defaultResult))
                    {
                        return defaultResult() ?? string.Empty;
                    }

                    return Environment.GetEnvironmentVariable(x);
                });
        }

        /// <summary>
        /// Allows values between $( and ) to be substituted at runtime.
        /// </summary>
        /// <param name="template">The string to do the substitutions on.</param>
        /// <param name="lookup">Object which contains what to replace as properties without $( ), and with what their return values.</param>
        /// <param name="comparer">String comparer to use when determining equality for property names and macro name.</param>
        /// <returns>String with values substituted from <param name="lookup">lookup</param></returns>
        /// <exception cref="ArgumentException">The template parameter has invalid formatting or key cannot be found.</exception>
        /// <example>"Hello $(name)!".Substitute(new {Name = "world"}); // returns "Hello world!"</example>
        public static string Substitute(this string template, object lookup, StringComparer comparer = null)
        {
            var lookupTable = new Dictionary<string, string>(comparer ?? StringComparer.OrdinalIgnoreCase);
            var properties = lookup.GetType()
                .GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.CanRead);
            foreach (var property in properties)
            {
                MethodInfo getMethod = property.GetGetMethod();
                if (getMethod.GetParameters().Length > 0)
                {
                    // Skip indexer
                    continue;
                }

                var propertyValue = getMethod.Invoke(lookup, null)?.ToString() ?? string.Empty;
                lookupTable.Add(property.Name, propertyValue);
            }

            return Substitute(template, lookupTable);
        }

        public static string UnescapeText(string text)
        {
            var doc = new XmlDocument();
            var n = doc.CreateElement("root");
            n.InnerXml = text;
            return n.InnerText;
        }

        public static string WithTrailingPathSeparator(this string input)
        {
            if (input.EndsWith(Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
            {
                return input;
            }

            return input + Path.DirectorySeparatorChar;
        }

        public static string ToNativePathSeparator(this string input)
        {
            return input.Replace(Path.DirectorySeparatorChar == '/' ? '\\' : '/', Path.DirectorySeparatorChar);
        }

        public static string ReplaceInvalidFileSystemChars(string filename)
        {
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
        }

        private static string GetAssemblyDirectory()
        {
            var assembly = typeof(StringExtensions).Assembly;
            if (assembly == null)
            {
                return null;
            }

            var uri = new UriBuilder(assembly.EscapedCodeBase);
            return Path.GetDirectoryName(Uri.UnescapeDataString(uri.Path));
        }
    }
}
