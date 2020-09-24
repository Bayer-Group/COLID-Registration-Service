using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace COLID.RegistrationService.Services.Extensions
{
    public static class HttpContextAccessorExtensions
    {
        public async static Task<TValue> GetContextRequest<TValue>(this IHttpContextAccessor httpContextAccessor)
        {
            var bodyString = string.Empty;
            var httpContext = httpContextAccessor.HttpContext;
            var httpRequest = httpContext.Request;

            // Allows using several time the stream in ASP.Net Core
            httpRequest.EnableBuffering();

            using (var readStream = new StreamReader(httpRequest.Body, Encoding.UTF8, true, 1024, true))
            {
                bodyString = await readStream.ReadToEndAsync();
            }

            // Rewind, so the core is not lost when it looks the body for the request
            httpRequest.Body.Position = 0;

            return JsonConvert.DeserializeObject<TValue>(bodyString);
        }

        public static string GetRequestPidUri(this IHttpContextAccessor httpContextAccessor)
        {
            string result = null;
            var httpContext = httpContextAccessor.HttpContext;

            if (httpContext != null)
            {
                var query = httpContext.Request.Query;

                if (query.ContainsKey("pidUri"))
                {
                    result = query["pidUri"].ToString();
                }
                else if (query.ContainsKey("currentPidUri"))
                {
                    result = query["currentPidUri"].ToString();
                }
            }

            return result;
        }

        public static string GetRequestQueryParam(this IHttpContextAccessor httpContextAccessor, string param)
        {
            var httpContext = httpContextAccessor.HttpContext;

            if (httpContext != null)
            {
                var query = httpContext.Request.Query;

                if (query.TryGetValue(param, out var result))
                {
                    return result.ToString();
                }
            }

            return null;
        }
    }
}
