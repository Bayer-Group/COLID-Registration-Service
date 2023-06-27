using System;
using System.Collections.Generic;
using System.Linq;

namespace COLID.Graph.TripleStore.Extensions
{
    public static class DictionaryExtension
    {
        public static void AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            if (dict != null && dict.ContainsKey(key))
            {
                dict[key] = value;
                return;
            }

            dict.Add(key, value);
        }

        public static dynamic GetValueOrNull(this IDictionary<string, dynamic> dict, string key, bool singleValue)
        {
            if (dict.TryGetValue(key, out dynamic value))
            {
                try
                {
                    return value;
                }
                catch (System.Exception ex)
                {
                    Console.WriteLine(ex);
                }

                return null;
            }

            return null;
        }

        public static dynamic GetValueOrNull(this IDictionary<string, List<dynamic>> dict, string key, bool singleValue)
        {
            if (dict.TryGetValue(key, out List<dynamic> value))
            {
                try
                {
                    return singleValue ? value?.FirstOrDefault() : value;
                }
                catch (System.Exception ex)
                {
                    Console.WriteLine(ex);
                }

                return null;
            }
            return singleValue ? null : new List<dynamic>();
        }

        public static bool TryRemoveKey<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
        {
            if (dict.ContainsKey(key))
            {
                dict.Remove(key);
                return true;
            }

            return false;
        }

#pragma warning disable CA1715 // Identifiers should have correct prefix
        public static void AddRange<T, S>(this IDictionary<T, S> source, IEnumerable<KeyValuePair<T, S>> collection)
#pragma warning restore CA1715 // Identifiers should have correct prefix
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            foreach (var item in collection)
            {
                if (!source.ContainsKey(item.Key))
                {
                    source.Add(item.Key, item.Value);
                }
            }
        }

        public static TValue GetValueOrDefault<TValue, TKey>(this IDictionary<TKey, dynamic> properties, TKey key, TValue defaultValue)
        {
            TValue retValue;
            retValue = defaultValue;
            if (properties.TryGetValue(key, out var dynamicValue))
            {
                if (IsCompatible<TValue>(defaultValue, dynamicValue))
                {
                    retValue = (TValue)dynamicValue;
                }
                else if (dynamicValue is string)
                {
                    // TODO: (Rajesh) due to time constraint could not think of better way to handle.
                    if (typeof(TValue) == typeof(short))
                    {
                        if (short.TryParse(dynamicValue, out short shortValue))
                        {
                            object someValue = shortValue;
                            retValue = (TValue)someValue;
                        }
                    }
                    else if (typeof(TValue) == typeof(int))
                    {
                        if (int.TryParse(dynamicValue, out int intValue))
                        {
                            object someValue = intValue;
                            retValue = (TValue)someValue;
                        }
                    }
                    else if (typeof(TValue) == typeof(long))
                    {
                        if (long.TryParse(dynamicValue, out long longValue))
                        {
                            object someValue = longValue;
                            retValue = (TValue)someValue;
                        }
                    }
                }
            }

            return retValue;
        }

        private static dynamic IsCompatible<TValue>(TValue defaultValue, dynamic dynamicValue)
        {
            return (IsInteger(defaultValue) && IsInteger(dynamicValue)) ||
                   (dynamicValue.GetType() == typeof(TValue));
        }

        private static bool IsInteger(object o) => o is short || o is int || o is long;
    }
}
