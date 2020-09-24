using System;
using Newtonsoft.Json.Linq;

namespace COLID.Graph.Metadata.Extensions
{
    public static class DynamicExtension
    {
        public static bool IsType<TResult>(dynamic value, out TResult result)
        {
            if (value is TResult)
            {
                result = value;
                return true;
            }
            else if (value is JObject)
            {
                JObject jObject = value;

                try
                {
                    result = jObject.ToObject<TResult>();
                    return true;
                }
                catch (System.Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            result = default;
            return false;
        }
    }
}
