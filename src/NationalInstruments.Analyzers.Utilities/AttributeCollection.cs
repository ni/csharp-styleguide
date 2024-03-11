using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NationalInstruments.Analyzers.Utilities
{
    /// <summary>
    /// Data structure for efficiently storing and comparing attribute name-value pairs.
    /// </summary>
    /// <remarks>
    /// This class is a thin wrapper around the real data structure: a dictionary of attribute names mapped
    /// to attribute values. While not tighly coupled to <see cref="ExemptionCollection"/>, its main purpose
    /// is to allow exemptions to easily merge attributes and determine if their current attributes are more
    /// specific than what's being tested.
    /// </remarks>
    /// <example>
    /// <code>
    /// var attributes = new AttributeCollection(Tuple.Create("Assembly", "A"));
    /// attributes.Add("Assembly", "B");            // { "Assembly": ["A", "B"] }
    /// attributes.Add("Namespace", "C");           // { "Assembly": ["A", "B"], "Namespace": ["C"] }
    ///
    /// var attributes2 = new AttributeCollection(Tuple.Create("Assembly", "D"));
    /// attributes2.Add("Parameter", "E|F")         // { "Assembly": ["D"], "Parameter": ["E", "F"] }
    ///
    /// attributes.Merge(attributes2)               // { "Assembly": ["A", "B", "D"], "Parameter": ["E", "F"] }
    /// attributes.MoreSpecificThan(attributes2)    // true, attributes2 is missing assemblies "A" and "B"
    /// </code>
    /// </example>
    public sealed class AttributeCollection : IEnumerable<KeyValuePair<string, HashSet<string>>>
    {
        private readonly Dictionary<string, HashSet<string>> _attributes = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Constructor that accepts any number of name-value attributes.
        /// </summary>
        /// <param name="attributes">An array of tuples containing attribute name and value.</param>
        public AttributeCollection(params Tuple<string, string>[] attributes)
        {
            foreach (var attribute in attributes)
            {
                Add(attribute.Item1, attribute.Item2);
            }
        }

        /// <summary>
        /// Gets the number of attributes.
        /// </summary>
        public int Count => _attributes.Keys.Count;

        /// <summary>
        /// Gets a collection containing the names in the <see cref="AttributeCollection"/>.
        /// </summary>
        public ICollection<string> Names => _attributes.Keys;

        /// <summary>
        /// Gets the values associated with the attribute <paramref name="name"/>.
        /// </summary>
        /// <param name="name">Name of the attribute.</param>
        /// <returns>The values associated to the named attribute.</returns>
        public HashSet<string> this[string name] => _attributes[name];

        public static bool operator ==(AttributeCollection left, AttributeCollection right) => Equals(left, right);

        public static bool operator !=(AttributeCollection left, AttributeCollection right) => !Equals(left, right);

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, HashSet<string>>> GetEnumerator()
        {
            return _attributes.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Adds a new attribute or adds a new <paramref name="value"/> to an existing attribute.
        /// </summary>
        /// <param name="name">Name of the attribute.</param>
        /// <param name="value">Value of the attribute.</param>
        public void Add(string name, string? value)
        {
            if (value is null)
            {
                return;
            }

            if (_attributes.TryGetValue(name, out var values))
            {
                values.Add(value);
            }
            else
            {
                _attributes.Add(
                    name,
                    new HashSet<string>(
                        value?.Split("|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries),
                        StringComparer.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Merges this <see cref="AttributeCollection"/> with another.
        /// </summary>
        /// <remarks>
        /// "Merging" attributes is slightly different from merging dictionaries. While missing keys and
        /// values are added in both circumstances, an attribute merge removes entries that do not exist
        /// in the "other" set. This is due to the fact that attributes constrain, and the only way
        /// to remove constraints is to not specify them.
        /// </remarks>
        /// <param name="attributes">A set of attributes that should be added to this collection.</param>
        public void Merge(AttributeCollection attributes)
        {
            // if an attribute is not present in 'attributes', remove it
            var attributeNames = _attributes.Keys.Except(attributes.Names).ToArray();
            foreach (var name in attributeNames)
            {
                _attributes.Remove(name);
            }

            foreach (var attribute in attributes)
            {
                if (_attributes.TryGetValue(attribute.Key, out var values))
                {
                    values.UnionWith(attribute.Value);
                }
                else
                {
                    _attributes[attribute.Key] = attribute.Value;
                }
            }
        }

        /// <summary>
        /// Returns true if the specified <paramref name="attributes"/> either don't exist in this
        /// collection or their values match.
        /// </summary>
        /// <param name="attributes">A set of attributes to compare against.</param>
        /// <returns>
        /// A Boolean that indicates that the <paramref name="attributes"/> either don't exist in this
        /// collection or their values match.
        /// </returns>
        public bool MoreSpecificThan(AttributeCollection? attributes)
        {
            if (attributes is null)
            {
                return _attributes.Any();
            }

            foreach (var attribute in _attributes)
            {
                if (!attributes.TryGetValues(attribute.Key, out var values))
                {
                    return true;
                }

                // Does one of the existing attributes have a value not specified in 'attributes'?
                if (!attribute.Value.Any(x => values.Contains(x)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if the specified attribute <paramref name="name"/> is in the collection.
        /// </summary>
        /// <param name="name">Name of an attribute.</param>
        /// <param name="values">Values associated with the attribute name.</param>
        /// <returns>A Boolean that indicates that the named attribute exists in the collection.</returns>
        public bool TryGetValues(string name, out HashSet<string> values)
        {
            return _attributes.TryGetValue(name, out values);
        }

        /// <summary>
        /// Returns true if the other <paramref name="obj"/> is equal to this one, where "equal" is defined as
        /// either same reference or same key-values.
        /// </summary>
        /// <param name="obj">Another object that should be of type <see cref="AttributeCollection"/>.</param>
        /// <returns>A Boolean that indicates if this instance is equal to another.</returns>
        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false; // 'this' cannot be null
            }

            if (ReferenceEquals(obj, this))
            {
                return true;
            }

            if (obj is not AttributeCollection other)
            {
                return false; // comparing against an object that isn't an 'AttributeSet'
            }

            if (_attributes.Count != other._attributes.Count)
            {
                return false;
            }

            foreach (var kvp in _attributes)
            {
                // Check keys
                if (!other._attributes.TryGetValue(kvp.Key, out var values))
                {
                    return false;
                }

                // Check values
                if (kvp.Value.Count != values.Count || values.Any(x => !kvp.Value.Contains(x)))
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            // Logic adapted from: https://stackoverflow.com/a/263416/116047
            // AFAIK, 19 and 31 could be other prime numbers
            return _attributes.Keys.SelectMany(x => _attributes[x]).Aggregate(19, (current, value) => current * 31);
        }
    }
}
