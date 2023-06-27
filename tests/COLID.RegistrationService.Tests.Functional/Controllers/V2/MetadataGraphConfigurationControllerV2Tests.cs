using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using COLID.Graph.Metadata.DataModels.MetadataGraphConfiguration;
using COLID.RegistrationService.WebApi;
using Xunit;
using Xunit.Abstractions;

namespace COLID.RegistrationService.Tests.Functional.Controllers.V1
{
    public class MetadataGraphConfigurationControllerV2Tests : IClassFixture<FunctionTestsFixture>
    {
        private readonly HttpClient _client;
        private readonly FunctionTestsFixture _factory;
        private readonly string _apiPath = "api/v2/metadataGraphConfiguration";

        public MetadataGraphConfigurationControllerV2Tests(FunctionTestsFixture factory)
        {
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
