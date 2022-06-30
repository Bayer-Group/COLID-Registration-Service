//using System.Collections.Generic;
//using System.Linq;
//using System.Net;
//using System.Net.Http;
//using System.Threading.Tasks;
//using System.Web;
//using COLID.Graph.TripleStore.DataModels.Base;
//using COLID.RegistrationService.Tests.Common.Builder;
//using COLID.RegistrationService.Tests.Common.Utils;
//using COLID.RegistrationService.Tests.Functional.DataModel.V1;
//using COLID.RegistrationService.Tests.Unit.Extensions;
//using Newtonsoft.Json;
//using Xunit;
//using Xunit.Abstractions;

//namespace COLID.RegistrationService.Tests.Functional.Controllers.V2
//{
//    public class HistoricResourceControllerV2Tests : IClassFixture<FunctionTestsFixture>
//    {
//        private readonly ITestOutputHelper _output;
//        private readonly HttpClient _client;
//        private readonly FunctionTestsFixture _factory;
//        private readonly string _apiPath = "api/v2/resource/history";

//        public HistoricResourceControllerV2Tests(FunctionTestsFixture factory, ITestOutputHelper output)
//        {
//            _output = output;
//            _factory = factory;
//            _client = _factory.CreateClient();
//        }

//        [Fact]
//        public async Task GetHistoricResourceOverview_Successful()
//        {
//            // Arrange
//            var pidUri = HttpUtility.UrlEncode("https://pid.bayer.com/URI1010");
//            var url = $"{_apiPath}List?pidUri={pidUri}";

//            // Act
//            var result = await _client.GetAsync(url);

//            // Assert
//            result.EnsureSuccessStatusCode();
//            var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
//            var historicResources = Newtonsoft.Json.JsonConvert.DeserializeObject<List<HistoricResourceOverviewDtoV1>>(content);

//            Assert.NotNull(historicResources);
//            Assert.Equal(1, historicResources.Count);

//            var historicResource = historicResources.FirstOrDefault();

//            Assert.Equal("https://pid.bayer.com/kos/19050#5216adec-a936-499c-a500-570c32fd7f9f", historicResource.Subject);
//            Assert.Equal("https://pid.bayer.com/URI1010", historicResource.PidUri);
//            Assert.Equal("christian.kaubisch.ext@bayer.com", historicResource.LastChangeUser);
//            Assert.Equal("2020-01-22T10:31:29.21Z", historicResource.LastChangeDateTime);
//        }

//        [Theory]
//        [InlineData("INVALID_Uri")]
//        [InlineData(null)]
//        [InlineData("")]
//        public async Task GetHistoricResourceOverview_Error_BadRequest_InvalidUri(string uri)
//        {
//            var res = await _client.GetAsync($"{_apiPath}List?pidUri={uri}");
//            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
//        }

//        [Fact]
//        public async Task GetHistoricResourceOverview_Error_NotFound_WrongUri()
//        {
//            var result = await _client.GetAsync($"{_apiPath}List?pidUri=http://meh");

//            var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
//            var historics = Newtonsoft.Json.JsonConvert.DeserializeObject<IList<HistoricResourceOverviewDtoV1>>(content);

//            result.EnsureSuccessStatusCode();
//            Assert.Empty(historics);
//        }

//        [Fact]
//        public async Task GetHistoricResource_Successful()
//        {
//            // Arrange
//            var id = HttpUtility.UrlEncode("https://pid.bayer.com/kos/19050#80221099-a8bd-4ed9-9a9f-db33fba99e7d").ToString();
//            var pidUri = HttpUtility.UrlEncode("https://pid.bayer.com/URI5010").ToString();
//            var url = $"{_apiPath}?pidUri={pidUri}&subject={id}";

//            // Act
//            var result = await _client.GetAsync(url);
//            var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);

//            var historic = JsonConvert.DeserializeObject<ResourceV1>(content);

//            // Assert
//            result.EnsureSuccessStatusCode();
//            Assert.NotNull(historic);

//            Assert.Equal("https://pid.bayer.com/kos/19050#80221099-a8bd-4ed9-9a9f-db33fba99e7d", historic.Subject);
//            Assert.Equal("https://pid.bayer.com/URI5010", historic.PidUri.ToString());

//            var property = historic.Properties;

//            Assert.True(property.ContainsLabel("ID5001"));
//            Assert.True(property.ContainsResourceDefinition("ID5001"));
//            Assert.True(property.ContainsLifecycleStatus("https://pid.bayer.com/kos/19050/released"));
//            Assert.True(property.ContainsInformationClassification("https://pid.bayer.com/kos/19050/Open"));
//            Assert.True(property.ContainsLastChangeUser("christian.kaubisch.ext@bayer.com"));
//            Assert.True(property.ContainsAuthor("christian.kaubisch.ext@bayer.com"));
//            Assert.True(property.ContainsIsPersonalData("false"));
//            Assert.True(property.ContainsLicensedData("false"));
//            Assert.True(property.ContainsVersion("1"));
//            Assert.True(property.ContainsEntryLifecycleStatus("https://pid.bayer.com/kos/19050/historic"));
//            Assert.True(property.ContainsHasConsumerGroup("https://pid.bayer.com/kos/19050#bf2f8eeb-fdb9-4ee1-ad88-e8932fa8753c"));
//            Assert.Equal("2020-01-22T10:31:41Z", property[Graph.Metadata.Constants.Resource.DateModified].First().ToString("s") + "Z");
//            Assert.Equal("2020-01-22T10:29:21Z", property[Graph.Metadata.Constants.Resource.DateCreated].First().ToString("s") + "Z");

//            var expectedPidUriEntity = new PidUriBuilder().WithId("https://pid.bayer.com/URI5010").WithType().Build();
//            var expectedPidUriEntityV1 = new EntityV1(expectedPidUriEntity);

//            EntityV1 actualPidUriEntityV1 = JsonConvert.DeserializeObject<EntityV1>(property[Graph.Metadata.Constants.EnterpriseCore.PidUri].FirstOrDefault().ToString());
//            TestUtils.AssertSameEntityContent<EntityV1>(expectedPidUriEntityV1, actualPidUriEntityV1);

//            var expectedDeEntity = new DistributionEndpointBuilder()
//                .WithId("https://pid.bayer.com/kos/19050#f3a1757e-f7eb-409a-81f4-0d9f30a25342")
//                .WithType(RegistrationService.Common.Enums.DistributionEndpoint.Type.BrowsableResource)
//                .WithNetworkedResourceLabel("ID5011").WithNetworkAddress("http://ID5011")
//                .WithDistributionEndpointLifecycleStatus(RegistrationService.Common.Enums.DistributionEndpoint.LifecycleStatus.Active)
//                .WithPidUri("https://pid.bayer.com/URI5020", null).Build();
//            var expectedDeEntityV1 = new EntityV1(expectedDeEntity);

//            EntityV1 actualDeEntityV1 = JsonConvert.DeserializeObject<EntityV1>(property[Graph.Metadata.Constants.Resource.Distribution].FirstOrDefault().ToString());
//            TestUtils.AssertSameEntityContent(expectedDeEntityV1, actualDeEntityV1);
//        }

//        [Theory]
//        [InlineData("INVALID_Uri", "INVALID_Uri")]
//        [InlineData(null, null)]
//        [InlineData("", "")]
//        public async Task GetHistoricResource_Error_BadRequest_InvalidUri(string pidUri, string id)
//        {
//            var res = await _client.GetAsync($"{_apiPath}?pidUri={pidUri}&subject={id}");
//            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
//        }

//        [Fact]
//        public async Task GetHistoricResource_Error_NotFound_WrongUri()
//        {
//            var pidUri = "https://meh/pidUri";
//            var id = "https://meh/subject";
//            var res = await _client.GetAsync($"{_apiPath}?pidUri={pidUri}&subject={id}");

//            Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
//        }

//        private void AssertDistributionEndpoint(Entity actual, Entity expected)
//        {
//        }
//    }
//}
