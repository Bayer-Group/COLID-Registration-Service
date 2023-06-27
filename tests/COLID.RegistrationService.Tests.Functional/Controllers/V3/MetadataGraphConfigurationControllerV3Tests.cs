using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using COLID.Graph.Metadata.DataModels.MetadataGraphConfiguration;
using Xunit;
using Xunit.Abstractions;

namespace COLID.RegistrationService.Tests.Functional.Controllers.V3
{
    public class MetadataGraphConfigurationControllerV3Tests : IClassFixture<FunctionTestsFixture>
    {
        private readonly ITestOutputHelper _output;
        private readonly HttpClient _client;
        private readonly FunctionTestsFixture _factory;
        private readonly string _apiPath = "api/v3/metadataGraphConfiguration";

        public MetadataGraphConfigurationControllerV3Tests(FunctionTestsFixture factory, ITestOutputHelper output)
        {
            _output = output;
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GetMetadataGraphConfiguration_Success()
        {
            var result = await _client.GetAsync(_apiPath + "/history");
            var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
            var metadataConfigHistory = Newtonsoft.Json.JsonConvert.DeserializeObject<List<MetadataGraphConfigurationResultDTO>>(content);
            Assert.NotNull(result);
            Assert.Equal(2, metadataConfigHistory.Count);
        }

        [Fact]
        public async Task GetLatestMetadataGraphConfiguration_Success()
        {
            var result = await _client.GetAsync(_apiPath);
            var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
            var metadataConfigHistory = Newtonsoft.Json.JsonConvert.DeserializeObject<MetadataGraphConfigurationResultDTO>(content);
            Assert.NotNull(result);
        }
    }
}
