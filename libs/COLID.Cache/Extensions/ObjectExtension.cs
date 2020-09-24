using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace COLID.Cache.Extensions
{
    public static class ObjectExtension
    {
        private static List<string> _defaultIgnoreProps = new List<string>() { "Id", "Hash" };

        private enum TypeEnum { COLLECTION, SIMPLE, COMPLEX, ANONYMOUS }

        public static string CalculateHash(this object o)
        {
            return Calculate(o);
        }

        private static string Calculate(object obj)
        {
#pragma warning disable CA5350 // Do Not Use Weak Cryptographic Algorithms
#pragma warning disable CA2000 // Dispose objects before losing scope
            var sha1Hash = SHA1.Create();
#pragma warning restore CA2000 // Dispose objects before losing scope
#pragma warning restore CA5350 // Do Not Use Weak Cryptographic Algorithms

            if (GetTypeEnum(obj.GetType()) == TypeEnum.SIMPLE)
            {
                return ConvertToHash(sha1Hash, obj.ToString());
            }

            IEnumerable<PropertyInfo> props = GetProperties(obj);
            var sb = new StringBuilder();
            foreach (var prop in props)
            {
                string hexValue;
                switch (GetTypeEnum(prop.PropertyType))
                {
                    case TypeEnum.SIMPLE:
                        hexValue = ConvertToHash(sha1Hash, GetValueToHash(prop, obj));
                        break;
                    case TypeEnum.COLLECTION:
                        hexValue = CalculateForCollection(prop, obj, sha1Hash);
                        break;
                    case TypeEnum.COMPLEX:
                    case TypeEnum.ANONYMOUS:
                        hexValue = prop.GetValue(obj, null) == null
                            ? ConvertToHash(sha1Hash, prop.Name)
                            : Calculate(prop.GetValue(obj, null));
                        break;
                    default:
                        throw new ArgumentException(string.Empty);
                }
                sb.Append(hexValue);
            }
            return ConvertToHash(sha1Hash, sb.ToString());
        }

        private static IEnumerable<PropertyInfo> GetProperties(object obj) =>
            obj
            .GetType()
            .GetProperties()
            .OrderBy(x => x.Name)
#pragma warning disable CA1307 // Specify StringComparison
            .Where(p => !_defaultIgnoreProps.Any(d => p.Name.Contains(d)));
#pragma warning restore CA1307 // Specify StringComparison

        private static TypeEnum GetTypeEnum(Type type)
        {
            if (typeof(IEnumerable<object>).IsAssignableFrom(type)) return TypeEnum.COLLECTION;
            if (IsAnonymousType(type)) return TypeEnum.ANONYMOUS;
            if (IsSimpleType(type))
            {
                return TypeEnum.SIMPLE;
            }
            else
            {
                return TypeEnum.COMPLEX;
            }
        }

        private static bool IsAnonymousType(Type type)
        {
            return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
#pragma warning disable CA1307 // Specify StringComparison
                && type.IsGenericType && type.Name.Contains("AnonymousType")
#pragma warning restore CA1307 // Specify StringComparison
                && type.Name.StartsWith("<>", StringComparison.OrdinalIgnoreCase)
                && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
        }

        private static string CalculateForCollection(PropertyInfo prop, object obj, SHA1 sha1Hash)
        {
            var collection = (IEnumerable<object>)prop.GetValue(obj, null);
            if (collection == null || !collection.Any())
            {
                return ConvertToHash(sha1Hash, prop.Name);
            }

            // TODO: Refactor in order to not convert from byte to string back and forth...
            char[] result = new char[32];
            foreach (var item in collection)
            {
                char[] child = Calculate(item).ToCharArray();
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = (char)(result[i] ^ child[i]);
                }
            }
            return new string(result);
        }

        private static string GetValueToHash(PropertyInfo prop, object obj)
        {
            return prop.GetValue(obj) == null
                ? prop.Name
                : prop.Name + prop.GetValue(obj).ToString();
        }

        private static bool IsSimpleType(Type type) =>
            type.IsValueType ||
            type.IsPrimitive ||
            new Type[] {
            typeof(String),
            typeof(Decimal),
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(TimeSpan),
            typeof(Guid)
            }.Contains(type) ||
            Convert.GetTypeCode(type) != TypeCode.Object;

        private static string ConvertToHash(HashAlgorithm hashAlgorithm, string input)
        {
            byte[] hashBytes = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
            StringBuilder hash = new StringBuilder(hashBytes.Length * 2);
            foreach (byte b in hashBytes)
            {
                hash.AppendFormat("{0:x2}", b);
            }
            return hash.ToString();
        }
    }
}
