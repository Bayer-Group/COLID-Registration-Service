using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        /// <returns></returns>
        public static async Task<HttpResponseMessage> SendRequestWithBearerTokenAsync(this HttpClient httpClient, HttpMethod method, string path, object requestBody, string accessToken, CancellationToken ct, JsonSerializerSettings serializerSettings = null)
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

            var response = await httpClient.SendAsync(request, ct);
            return response;
        }
    }
}
