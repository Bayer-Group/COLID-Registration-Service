using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using COLID.RegistrationService.Tests.Common.Builder;
using COLID.RegistrationService.WebApi;
using Microsoft.AspNetCore.WebUtilities;
using Xunit;
using Xunit.Abstractions;

namespace COLID.RegistrationService.Tests.Functional.Controllers.V1
{
    public class DistributionEndpointControllerV1Tests : IClassFixture<FunctionTestsFixture>
    {
        private readonly ITestOutputHelper _output;
        private readonly HttpClient _client;
        private readonly FunctionTestsFixture _factory;
        private readonly string _apiPath = "api/v1/distributionEndpoint";

        public DistributionEndpointControllerV1Tests(FunctionTestsFixture factory, ITestOutputHelper output)
        {
            _output = output;
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task CreateDistributionEndpoint_Error_BadRequest()
        {
            HttpContent content = new StringContent(string.Empty, Encoding.UTF8);
            var result = await _client.PostAsync(_apiPath, new StringContent(string.Empty, Encoding.UTF8, MediaTypeNames.Application.Json));
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task CreateDistributionEndpoint_Error_UnsupportedMediaType()
        {
            var deJson = new DistributionEndpointBuilder().GenerateSampleData().Build().ToString();
            HttpContent requestContent = new StringContent(deJson, Encoding.UTF8);
            var result = await _client.PostAsync(_apiPath, new StringContent(string.Empty));
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, result.StatusCode);
        }

        /// <summary>
        /// Route POST api/v3/consumerGroup
        /// </summary>
        public async Task CreateDistributionEndpoint_Success()
        {
            // Arrange
            var pidUriParam = "https://pid.bayer.com/URI1010";
            var queryParams = new Dictionary<string, string> { { "resourcePidUri", pidUriParam }, { "createAsMainDistributionEndpoint", "false" } };
            var requestUri = QueryHelpers.AddQueryString(_apiPath, queryParams);
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            var content = new DistributionEndpointBuilder().GenerateSampleData().Build().ToString();
            request.Content = new StringContent(content.ToString(), Encoding.UTF8, MediaTypeNames.Application.Json);

            // Act
            var result = await _client.SendAsync(request);

            _output.WriteLine(result.Content.ReadAsStringAsync().Result.ToString());
            // Assert
            result.EnsureSuccessStatusCode();
        }
    }
}
