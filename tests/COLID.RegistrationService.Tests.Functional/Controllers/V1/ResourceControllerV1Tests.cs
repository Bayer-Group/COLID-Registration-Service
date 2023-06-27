using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.RegistrationService.Common.DataModel.Resources;
using Xunit;

namespace COLID.RegistrationService.Tests.Functional.Controllers.V1
{
    public class ResourceControllerV1Tests : IClassFixture<FunctionTestsFixture>
    {
        private readonly HttpClient _client;
        private readonly FunctionTestsFixture _factory;
        private readonly string _apiPathV1 = "api/v1/resource";

        public ResourceControllerV1Tests(FunctionTestsFixture factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Theory]
        [InlineData("INVALID_Uri")]
        [InlineData(null)]
        [InlineData("")]
        public async Task GetResourceByPidUri_Error_BadRequest_InvalidUri(string pidUri)
        {
            var res = await _client.GetAsync($"{_apiPathV1}?pidUri={pidUri}&main=false");
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

            var res2 = await _client.GetAsync($"{_apiPathV1}?pidUri={pidUri}");
            Assert.Equal(HttpStatusCode.BadRequest, res2.StatusCode);
        }

        [Fact]
        public async Task GetResourceByPidUri_Error_NotFound_WrongUri()
        {
            var result = await _client.GetAsync($"{_apiPathV1}?pidUri=http://meh&main=false");

            var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
            var historics = Newtonsoft.Json.JsonConvert.DeserializeObject<Resource>(content);

            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        }
    }
}
