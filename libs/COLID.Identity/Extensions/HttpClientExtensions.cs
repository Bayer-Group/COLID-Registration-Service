using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CorrelationId;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace COLID.Identity.Extensions
{
    public static class HttpClientExtensions
    {
        /// <summary>
        /// This extension method for <see cref="HttpClient"/> provides a convenient overload that accepts
        /// a <see cref="string"/> accessToken to be used as Bearer authentication.
        /// </summary>
        /// <param name="httpClient">The <see cref="HttpClient"/> instance</param>
        /// <param name="method">The <see cref="HttpMethod"/></param>
        /// <param name="path">The path to the requested target</param>
        /// <param name="requestBody">The body of the request</param>
        /// <param name="accessToken">The access token to be used as Bearer authentication</param>
        /// <param name="ct">A <see cref="CancellationToken"/></param>
        /// <param name="serializerSettings">A <see cref="JsonSerializerSettings"/> to serialize the requestBody.</param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> SendRequestWithBearerTokenAsync(this HttpClient httpClient, HttpMethod method, string path, object requestBody, string accessToken, CancellationToken ct, JsonSerializerSettings serializerSettings = null)
        {
            var request = createHttpRequest(method, path, requestBody, accessToken, ref serializerSettings);

            var response = await httpClient.SendAsync(request, ct);
            return response;
        }

        /// <summary>
        /// This extension method for <see cref="HttpClient"/> provides a convenient overload that accepts
        /// a <see cref="string"/> accessToken to be used as Bearer authentication.
        /// </summary>
        /// <param name="httpClient">The <see cref="HttpClient"/> instance</param>
        /// <param name="method">The <see cref="HttpMethod"/></param>
        /// <param name="path">The path to the requested target</param>
        /// <param name="requestBody">The body of the request</param>
        /// <param name="accessToken">The access token to be used as Bearer authentication</param>
        /// <param name="ct">A <see cref="CancellationToken"/></param>
        /// <param name="correlationContext">A <see cref="CorrelationContext"/></param>
        /// <param name="serializerSettings">A <see cref="JsonSerializerSettings"/> to serialize the requestBody.</param>
        /// 
        /// <returns></returns>
        public static async Task<HttpResponseMessage> SendRequestWithOptionsAsync(this HttpClient httpClient, HttpMethod method, string path, object requestBody, string accessToken, CancellationToken ct, CorrelationContext correlationContext, JsonSerializerSettings serializerSettings = null)
        {
            var request = createHttpRequest(method, path, requestBody, accessToken, ref serializerSettings);

            if (correlationContext != null && !request.Headers.Contains(correlationContext.Header))
            {
                request.Headers.Add(correlationContext.Header, correlationContext.CorrelationId);
            }
            
            var response = await httpClient.SendAsync(request, ct);
            return response;
        }

        /// <summary>
        /// Creates a <see cref="HttpRequestMessage"/> object to send.
        /// </summary>
        /// <param name="method">The <see cref="HttpMethod"/></param>
        /// <param name="path">The path to the requested target</param>
        /// <param name="requestBody">The body of the request</param>
        /// <param name="accessToken">The access token to be used as Bearer authentication</param>
        /// <param name="serializerSettings">A <see cref="JsonSerializerSettings"/> to serialize the requestBody.</param>
        /// <returns></returns>
        private static HttpRequestMessage createHttpRequest(HttpMethod method, string path, object requestBody, string accessToken, ref JsonSerializerSettings serializerSettings)
        {
            serializerSettings ??= new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            var request = new HttpRequestMessage(method, path);

            if (requestBody != null)
            {
                var json = JsonConvert.SerializeObject(requestBody, serializerSettings);
                var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
                request.Content = content;
            }

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, accessToken);
            }

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));

            return request;
        }
    }
}
