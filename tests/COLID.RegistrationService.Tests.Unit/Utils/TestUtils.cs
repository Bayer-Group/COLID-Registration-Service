using System;
using System.Text.Json;
using System.Web;
using COLID.Common.Extensions;
using COLID.RegistrationService.Common.Extensions;
using Xunit;

namespace COLID.RegistrationService.Tests.Common.Utils
{
    public static class TestUtils
    {
        private static Random _random = new Random();

        public static string GetRandomEnumValue<T>() where T : Enum
        {
            var values = Enum.GetValues(typeof(T));
            T randomValue = (T)values.GetValue(_random.Next(values.Length));
            return randomValue.GetDescription();
        }

        public static string GenerateRandomId()
        {
            return $"{Graph.Metadata.Constants.Entity.IdPrefix}{new Guid(Guid.NewGuid().ToString())}";
        }

        public static string EncodeIfNecessary(string uri)
        {
            if (uri.Contains("#"))
            {
                return HttpUtility.UrlEncode(uri).ToString();
            }
            return uri;
        }

        public static void AssertSameEntityContent<TEntity>(TEntity expected, TEntity actual)
        {
            var comparer = new JsonElementComparer();
            using var doc1 = JsonDocument.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(expected));
            using var doc2 = JsonDocument.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(actual));

            Assert.True(comparer.Equals(doc1.RootElement, doc2.RootElement));
        }
    }
}
