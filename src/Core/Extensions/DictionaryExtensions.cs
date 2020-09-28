using System;
using System.Collections.Generic;
using System.Linq;

namespace NationalInstruments.Tools.Extensions
{
    public static class DictionaryExtensions
    {
        public static void Add<TKey, TValue>(this IDictionary<TKey, List<TValue>> dictionary, TKey key, TValue value)
        {
            dictionary.GetOrAdd(key).Add(value);
        }

        public static bool Contains<TKey, TValue>(this IDictionary<TKey, List<TValue>> dictionary, TKey key, TValue value, IEqualityComparer<TValue> valueComparer)
        {
            return dictionary.TryGetValue(key, out List<TValue> values) && values.Contains(value, valueComparer);
        }

        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
            where TValue : new()
        {
            return dictionary.GetOrAdd(key, () => new TValue());
        }

        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> newValue)
        {
            if (!dictionary.TryGetValue(key, out TValue result))
            {
                result = newValue();
                dictionary[key] = result;
            }

            return result;
        }

        public static TValue GetOrAddThreadSafe<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> createDefaultValue)
        {
            lock (dictionary)
            {
                return dictionary.GetOrAdd(key, createDefaultValue);
            }
        }
    }
}
