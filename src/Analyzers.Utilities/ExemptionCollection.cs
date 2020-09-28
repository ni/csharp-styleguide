using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace NationalInstruments.Tools.Analyzers.Utilities
{
    /// <summary>
    /// Data structure for efficiently storing and retrieving <see cref="Exemption"/>s and their associated attributes.
    /// </summary>
    /// <remarks>
    /// This class is a thin wrapper around the real data structure: a dictionary of exemption keys mapped to
    /// attribute values. It allows clients to easily determine if an exemption/attributes combo is present by
    /// calling <see cref="Contains"/> or <see cref="Matches"/>, where the latter allows wildcards in the
    /// exemption's value.
    /// </remarks>
    /// <example>
    /// <code>
    /// var attributes = new AttributeCollection(Tuple.Create("Assembly", "A"));
    /// var exemptions = new ExemptionCollection(Tuple.Create("foo", attributes));
    ///
    /// exemptions.Contains("foo")              // false, we are more generic than the original exemption
    /// exemptions.Contains("foo", attributes); // true, everything matches
    /// exemptions.Matches("fo*", attributes);  // true, 'Match' allows wildcards
    ///
    /// attributes.Add("Parameter", "B");
    /// exemptions.Contains("foo", attributes); // true, we are more specific than the original exemption
    /// </code>
    /// </example>
    public sealed class ExemptionCollection : IEnumerable<string>
    {
        private readonly Dictionary<Exemption, AttributeCollection> _exemptions = new Dictionary<Exemption, AttributeCollection>();

        /// <summary>
        /// Default parameterless constructor.
        /// </summary>
        public ExemptionCollection()
        {
        }

        /// <summary>
        /// Constructor that accepts any number of exemption values.
        /// </summary>
        /// <param name="values">An array of exemption values.</param>
        public ExemptionCollection(params string[] values)
            : this(values.Select(x => Tuple.Create<string, AttributeCollection>(x, null)).ToArray())
        {
        }

        /// <summary>
        /// Constructor that accepts any number of value-attribute exemptions.
        /// </summary>
        /// <param name="exemptions">An array of tuples containing exemption value and associated attributes.</param>
        public ExemptionCollection(params Tuple<string, AttributeCollection>[] exemptions)
        {
            foreach (var exemption in exemptions)
            {
                Add(exemption.Item1, exemption.Item2);
            }
        }

        /// <summary>
        /// Gets the number of exemptions.
        /// </summary>
        public int Count => _exemptions.Count;

        /// <summary>
        /// Gets the attributes associated with <paramref name="value"/>.
        /// </summary>
        /// <param name="value">A string pattern that should be exempt.</param>
        /// <returns>A set of attributes that applies to this exemption.</returns>
        public AttributeCollection this[string value] => _exemptions[new Exemption(value)];

        /// <inheritdoc />
        public IEnumerator<string> GetEnumerator()
        {
            return _exemptions.Keys.Select(x => x.Value).GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Adds an <see cref="Exemption"/> and it's optional attributes to this collection.
        /// </summary>
        /// <param name="value">A string pattern that should be exempt.</param>
        /// <param name="attributes">A set of attributes that applies to this exemption.</param>
        public void Add(string value, AttributeCollection attributes = null)
        {
            var exemption = new Exemption(value);

            if (_exemptions.TryGetValue(exemption, out var existingAttributes))
            {
                if (existingAttributes != null)
                {
                    if (attributes != null)
                    {
                        existingAttributes.Merge(attributes);
                    }
                    else
                    {
                        _exemptions[exemption] = null;
                    }
                }
                else
                {
                    _exemptions[exemption] = attributes;
                }
            }
            else
            {
                _exemptions.Add(exemption, attributes);
            }
        }

        /// <summary>
        /// Adds a set of exemptions (and their attributes) from in-memory XML to this collection.
        /// </summary>
        /// <param name="exemptions">An enumerable of XML nodes with a value and optional attributes.</param>
        public void UnionWith(IEnumerable<XElement> exemptions)
        {
            foreach (var exemption in exemptions)
            {
                var attributes = exemption.Attributes().Select(x => Tuple.Create(x.Name.LocalName, x.Value));

                Add(exemption.Value, new AttributeCollection(attributes.ToArray()));
            }
        }

        /// <summary>
        /// Returns true if the <paramref name="value"/> and its optional <paramref name="attributes"/> are
        /// contained in this collection.
        /// </summary>
        /// <remarks>This is an O(1) operation.</remarks>
        /// <param name="value">Any string value.</param>
        /// <param name="attributes">A set of attributes that applies to the current <paramref name="value"/>.</param>
        /// <returns>A Boolean that indicates if the value/attributes combo is contained in/satisfied by this collection.</returns>
        public bool Contains(string value, AttributeCollection attributes = null)
        {
            if (_exemptions.TryGetValue(new Exemption(value), out var existingAttributes))
            {
                return !existingAttributes?.MoreSpecificThan(attributes) ?? true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if the <paramref name="value"/> and its optional <paramref name="attributes"/>
        /// match an existing element in this collection.
        /// </summary>
        /// <remarks>This is an O(n) operation.</remarks>
        /// <param name="value">Any string value.</param>
        /// <param name="attributes">A set of attributes that applies to the current <paramref name="value"/>.</param>
        /// <returns>A Boolean that indicates f the value/attributes combo matches an entry in this collection.</returns>
        public bool Matches(string value, AttributeCollection attributes = null)
        {
            return _exemptions.Any(x => x.Key.Pattern.IsMatch(value) && (!x.Value?.MoreSpecificThan(attributes) ?? true));
        }

        private struct Exemption : IEquatable<Exemption>
        {
            private readonly string _comparisonValue;

            public Exemption(string value)
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _comparisonValue = value.ToUpperInvariant();

                Value = value;
                Pattern = new Regex(string.Concat("^", Regex.Escape(value).Replace(@"\*", ".*"), "$"), RegexOptions.IgnoreCase);    // PCL doesn't support RegexOptions.Compiled
            }

            public string Value { get; }

            public Regex Pattern { get; }

            public static bool operator ==(Exemption a, Exemption b)
            {
                return Equals(a, b);
            }

            public static bool operator !=(Exemption a, Exemption b)
            {
                return !Equals(a, b);
            }

            public override bool Equals(object obj)
            {
                // From: https://stackoverflow.com/a/2542712/116047
                if (!(obj is Exemption))
                {
                    return false;
                }

                return Equals((Exemption)obj);
            }

            public override int GetHashCode()
            {
                return _comparisonValue.GetHashCode();
            }

            public bool Equals(Exemption other)
            {
                return _comparisonValue == other._comparisonValue;
            }
        }
    }
}
