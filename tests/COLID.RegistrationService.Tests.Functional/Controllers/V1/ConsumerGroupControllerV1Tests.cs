using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using COLID.RegistrationService.Tests.Common.Builder;
using COLID.RegistrationService.Tests.Functional.DataModel.V1;
using Newtonsoft.Json;
using Xunit;

namespace COLID.RegistrationService.Tests.Functional.Controllers.V1
{
    public class ConsumerGroupControllerV1Tests : IClassFixture<FunctionTestsFixture>
    {
        private readonly HttpClient _client;
        private readonly FunctionTestsFixture _factory;

        private readonly string _apiPath = "api/v1/consumerGroup";
        private readonly string _deprecatedConsumerGroup = "https://pid.bayer.com/kos/19050#695533d3-391f-4249-81f9-f1674d1ea9dc";

        public ConsumerGroupControllerV1Tests(FunctionTestsFixture factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        /// <summary>
        /// Route GET api/v1/consumerGroupList
        /// </summary>
        [Fact]
        public async Task GetConsumerGroupList_Success()
        {
            // Act
            var result = await _client.GetAsync($"{_apiPath}List");

            // Assert
            result.EnsureSuccessStatusCode();
            var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
            var actualTestResults = JsonConvert.DeserializeObject<List<BaseEntityResultDtoV1>>(content);
            Assert.Equal(5, actualTestResults.Count);

            var indigoCG = actualTestResults.FirstOrDefault(cg => cg.Subject == "https://pid.bayer.com/kos/19050#bf2f8eeb-fdb9-4ee1-ad88-e8932fa8753c");
            Assert.Equal(5, indigoCG.Properties.Count());
            AssertResult(indigoCG.Properties, "INDIGO", "PID.Group01Data.ReadWrite");
            Assert.Contains("https://pid.bayer.com/kos/19050#5a9bc613-f948-4dd3-8cd7-9a4465319d24", indigoCG.Properties[Graph.Metadata.Constants.ConsumerGroup.HasPidUriTemplate]);
            Assert.Equal(5, indigoCG.Properties[Graph.Metadata.Constants.ConsumerGroup.HasPidUriTemplate].Count());

            var dinosCG = actualTestResults.FirstOrDefault(cg => cg.Subject == "https://pid.bayer.com/kos/19050#82fc2870-ca4e-407f-a197-bf3766ad785f");
            Assert.Equal(5, dinosCG.Properties.Count());
            AssertResult(dinosCG.Properties, "DINOS", "PID.Group02Data.ReadWrite");
            Assert.Single(dinosCG.Properties[Graph.Metadata.Constants.ConsumerGroup.HasPidUriTemplate]);
        }

        # region Testing GET api/v1/consumerGroup

        /// <summary>
        /// Route GET api/v1/consumerGroup
        /// </summary>
        [Fact]
        public async Task GetOneConsumerGroupById_Success()
        {
            // Arrange
            var pidUri = HttpUtility.UrlEncode("https://pid.bayer.com/kos/19050#2b3f0380-dd22-4666-a28b-7f1eeb82a5ff").ToString();
            var url = $"{_apiPath}?subject={pidUri}";

            // Act
            var result = await _client.GetAsync(url);

            // Assert
            result.EnsureSuccessStatusCode();
            var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
            var dataServicesCG = Newtonsoft.Json.JsonConvert.DeserializeObject<BaseEntityResultDtoV1>(content);

            Assert.Single(dataServicesCG.Properties[Graph.Metadata.Constants.ConsumerGroup.HasPidUriTemplate]);
            AssertResult(dataServicesCG.Properties, "Data Services", "PID.Group03Data.ReadWrite");
        }

        [Fact]
        public async Task GetOneConsumerGroupById_Error_BadRequest_EmptyUri()
        {
            var result = await _client.GetAsync($"{_apiPath}?subject=");
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task GetOneConsumerGroupById_Error_NotFound_InvalidUri()
        {
            var result = await _client.GetAsync($"{_apiPath}?subject=INVALID_SUBJECT");
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        #endregion

        #region Testing POST api/v1/consumerGroup

        /// <summary>
        /// Route POST api/v1/consumerGroup
        /// </summary>
        [Fact]
        public async Task CreateConsumerGroup_Success()
        {
            // Arrange
            var cg = new ConsumerGroupBuilder()
                .GenerateSampleData()
                .WithLabel("SUSHI")
                .WithAdRole("group4.read")
                .Build();
            HttpContent requestContent = new StringContent(cg.ToString(), Encoding.UTF8, MediaTypeNames.Application.Json);

            // Act
            var result = await _client.PostAsync(_apiPath, requestContent);
            result.EnsureSuccessStatusCode();

            // Assert
            var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
            var cgResult = JsonConvert.DeserializeObject<ConsumerGroupWriteResultCtoV1>(content).Entity;

            Assert.NotNull(cgResult);
            AssertResult(cgResult.Properties, "SUSHI", "group4.read");
            Assert.Single(cgResult.Properties[Graph.Metadata.Constants.ConsumerGroup.HasPidUriTemplate]);

            // Cleanup | TODO: Cleanup via Test SetUp and TearDown
            var deleteResult = await _client.DeleteAsync($"{_apiPath}?subject={HttpUtility.UrlEncode(cgResult.Subject).ToString()}");
            deleteResult.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Route POST api/v1/consumerGroup
        /// </summary>
        [Fact]
        public async Task CreateConsumerGroup_Success_WithDeprecatedStatus()
        {
            // Arrange
            var cg = new ConsumerGroupBuilder()
                .GenerateSampleData()
                .WithLabel("SUSHI")
                .WithAdRole("group4.read")
                .WithLifecycleStatus(Graph.Metadata.Constants.ConsumerGroup.LifecycleStatus.Deprecated)
                .Build();
            HttpContent requestContent = new StringContent(cg.ToString(), Encoding.UTF8, MediaTypeNames.Application.Json);

            // Act
            var result = await _client.PostAsync(_apiPath, requestContent);
            result.EnsureSuccessStatusCode();

            // Assert
            var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
            var cgResult = JsonConvert.DeserializeObject<ConsumerGroupWriteResultCtoV1>(content).Entity;

            Assert.NotNull(cgResult);
            AssertResult(cgResult.Properties, "SUSHI", "group4.read");
            Assert.Single(cgResult.Properties[Graph.Metadata.Constants.ConsumerGroup.HasPidUriTemplate]);

            // Cleanup | TODO: Cleanup via Test SetUp and TearDown
            var deleteResult = await _client.DeleteAsync($"{_apiPath}?subject={HttpUtility.UrlEncode(cgResult.Subject).ToString()}");
            deleteResult.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task CreateConsumerGroup_Error_BadRequest_WrongPidUriTemplate()
        {
            // TODO: Implement PidUriTemplateCheck during creation of consumer groups

            // Arrange
            var cg = new ConsumerGroupBuilder().WithPidUriTemplate("mlem").WithLabel("PIRATE").Build();
            HttpContent requestContent = new StringContent(cg.ToString(), Encoding.UTF8, MediaTypeNames.Application.Json);

            // Act
            var result = await _client.PostAsync(_apiPath, requestContent);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task CreateConsumerGroup_Error_BadRequest_DeprecatedPidUriTemplate()
        {
            // TODO: Implement PidUriTemplateCheck during creation of consumer groups

            // Arrange
            var cg = new ConsumerGroupBuilder()
                .WithPidUriTemplate("https://pid.bayer.com/kos/19050#bbcacfb7-f7c3-4b71-a509-d020ad703e83")
                .WithLabel("PIRATE")
                .Build();
            HttpContent requestContent = new StringContent(cg.ToString(), Encoding.UTF8, MediaTypeNames.Application.Json);

            // Act
            var result = await _client.PostAsync(_apiPath, requestContent);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task CreateConsumerGroup_Error_BadRequest_EmptyContent()
        {
            // Arrange
            HttpContent requestContent = new StringContent(string.Empty, Encoding.UTF8, MediaTypeNames.Application.Json);

            // Act
            var result = await _client.PostAsync(_apiPath, requestContent);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        #endregion

        #region Testing PUT api/v1/consumerGroup

        /// <summary>
        /// Route PUT api/v1/consumerGroup
        /// </summary>
        [Fact]
        public async Task EditConsumerGroup_Success()
        {
            // Create CG
            var createCg = new ConsumerGroupBuilder().GenerateSampleData().WithLabel("FROMAGE").WithAdRole("group4.read").Build();
            HttpContent createRequestContent = new StringContent(createCg.ToString(), Encoding.UTF8, MediaTypeNames.Application.Json);
            var createResult = await _client.PostAsync(_apiPath, createRequestContent);
            createResult.EnsureSuccessStatusCode();
            var createContent = await createResult.Content.ReadAsStringAsync().ConfigureAwait(false);
            var createEntity = JsonConvert.DeserializeObject<ConsumerGroupWriteResultCtoV1>(createContent).Entity;
            var cgIdUri = HttpUtility.UrlEncode(createEntity.Subject).ToString();
            var searchUrl = $"{_apiPath}?subject={cgIdUri}";

            // Search CG
            var searchResult = await _client.GetAsync(searchUrl);
            searchResult.EnsureSuccessStatusCode();

            // Edit CG
            var updateCg = new ConsumerGroupBuilder().GenerateSampleData().WithLabel("SPIDERMAN").WithAdRole("group3.read").Build();
            HttpContent updateRequestContent = new StringContent(updateCg.ToString(), Encoding.UTF8, MediaTypeNames.Application.Json);
            var editResult = await _client.PutAsync(searchUrl, updateRequestContent);
            editResult.EnsureSuccessStatusCode();

            // Search CG
            var searchResultAfterEdit = await _client.GetAsync(searchUrl);
            searchResultAfterEdit.EnsureSuccessStatusCode();
            var updateContentAfterEdit = await searchResultAfterEdit.Content.ReadAsStringAsync().ConfigureAwait(false);
            var updatedEntity = JsonConvert.DeserializeObject<BaseEntityResultDtoV1>(updateContentAfterEdit);

            AssertResult(updatedEntity.Properties, "SPIDERMAN", "group3.read");

            // Cleanup | TODO: Cleanup via Test SetUp and TearDown
            var deleteResult = await _client.DeleteAsync(searchUrl);
            deleteResult.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task EditConsumerGroup_Success_WithDeprecatedStatus()
        {
            // Create CG
            var createCg = new ConsumerGroupBuilder().GenerateSampleData().WithLabel("FROMAGE").WithAdRole("group4.read").Build();
            HttpContent createRequestContent = new StringContent(createCg.ToString(), Encoding.UTF8, MediaTypeNames.Application.Json);
            var createResult = await _client.PostAsync(_apiPath, createRequestContent);
            createResult.EnsureSuccessStatusCode();
            var createContent = await createResult.Content.ReadAsStringAsync().ConfigureAwait(false);
            var createEntity = JsonConvert.DeserializeObject<ConsumerGroupWriteResultCtoV1>(createContent).Entity;
            var cgIdUri = HttpUtility.UrlEncode(createEntity.Subject).ToString();
            var searchUrl = $"{_apiPath}?subject={cgIdUri}";

            // Search CG
            var searchResult = await _client.GetAsync(searchUrl);
            searchResult.EnsureSuccessStatusCode();

            // Edit CG
            var updateCg = new ConsumerGroupBuilder()
                .GenerateSampleData()
                .WithLabel("SPIDERMAN")
                .WithAdRole("group3.read")
                .WithLifecycleStatus(Graph.Metadata.Constants.ConsumerGroup.LifecycleStatus.Deprecated)
                .Build();
            HttpContent updateRequestContent = new StringContent(updateCg.ToString(), Encoding.UTF8, MediaTypeNames.Application.Json);
            var editResult = await _client.PutAsync(searchUrl, updateRequestContent);
            editResult.EnsureSuccessStatusCode();

            // Search CG
            var searchResultAfterEdit = await _client.GetAsync(searchUrl);
            searchResultAfterEdit.EnsureSuccessStatusCode();
            var updateContentAfterEdit = await searchResultAfterEdit.Content.ReadAsStringAsync().ConfigureAwait(false);
            var updatedEntity = JsonConvert.DeserializeObject<BaseEntityResultDtoV1>(updateContentAfterEdit);

            AssertResult(updatedEntity.Properties, "SPIDERMAN", "group3.read");

            // Cleanup | TODO: Cleanup via Test SetUp and TearDown
            var deleteResult = await _client.DeleteAsync(searchUrl);
            deleteResult.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task EditConsumerGroup_Error_BadRequest_UpdateExistingWithoutContent()
        {
            // Edit existing CG from ttl
            HttpContent updateRequestContent = new StringContent(string.Empty, Encoding.UTF8, MediaTypeNames.Application.Json);
            var cgIdUri = HttpUtility.UrlEncode("https://pid.bayer.com/kos/19050#3bb018e4-b006-4c9d-a85c-cd409fec89e5").ToString();
            var editResult = await _client.PutAsync($"{_apiPath}?subject={cgIdUri}", updateRequestContent);
            Assert.Equal(HttpStatusCode.BadRequest, editResult.StatusCode);
        }


        [Fact]
        public async Task EditConsumerGroup_Error_BadRequest_DeprecatedConsumerGroup()
        {
            var cgIdUri = HttpUtility.UrlEncode(_deprecatedConsumerGroup).ToString();
            var searchUrl = $"{_apiPath}?subject={cgIdUri}";

            // Edit CG
            var updateCg = new ConsumerGroupBuilder().GenerateSampleData().WithLabel("SPIDERMAN").WithAdRole("group3.read").Build();
            HttpContent updateRequestContent = new StringContent(updateCg.ToString(), Encoding.UTF8, MediaTypeNames.Application.Json);
            var editResult = await _client.PutAsync(searchUrl, updateRequestContent);
            
            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, editResult.StatusCode);
        }

        [Fact]
        public async Task EditConsumerGroup_Error_NotFound_EmptyUri()
        {
            var cg = new ConsumerGroupBuilder().GenerateSampleData().WithLabel("FROMAGE").WithAdRole("group4.read").Build();
            HttpContent updateRequestContent = new StringContent(cg.ToString(), Encoding.UTF8, MediaTypeNames.Application.Json);
            var editResult = await _client.PutAsync($"{_apiPath}?subject=", updateRequestContent);
            Assert.Equal(HttpStatusCode.BadRequest, editResult.StatusCode);
        }

        [Fact]
        public async Task EditConsumerGroup_Error_NotFound_InvalidUri()
        {
            var cg = new ConsumerGroupBuilder().GenerateSampleData().WithLabel("FROMAGE").WithAdRole("group4.read").Build();
            HttpContent updateRequestContent = new StringContent(cg.ToString(), Encoding.UTF8, MediaTypeNames.Application.Json);
            var editResult = await _client.PutAsync($"{_apiPath}?subject=INVALID_URI", updateRequestContent);
            Assert.Equal(HttpStatusCode.BadRequest, editResult.StatusCode);
        }

        #endregion

        #region Testing DELETE api/v1/consumerGroup

        /// <summary>
        /// Route DELETE api/v1/consumerGroup
        /// </summary>
        [Fact]
        public async Task DeleteConsumerGroup_Success()
        {
            // Create CG
            var createCg = new ConsumerGroupBuilder().GenerateSampleData().WithLabel("PIZZA").WithAdRole("group4.read").Build();
            HttpContent createRequestContent = new StringContent(createCg.ToString(), Encoding.UTF8, MediaTypeNames.Application.Json);
            var createResult = await _client.PostAsync(_apiPath, createRequestContent);
            createResult.EnsureSuccessStatusCode();
            var createContent = await createResult.Content.ReadAsStringAsync().ConfigureAwait(false);
            var createEntity = JsonConvert.DeserializeObject<ConsumerGroupWriteResultCtoV1>(createContent).Entity;
            var cgIdUri = HttpUtility.UrlEncode(createEntity.Subject).ToString();
            var searchUrl = $"{_apiPath}?subject={cgIdUri}";

            // Search CG
            var searchResult = await _client.GetAsync(searchUrl);
            searchResult.EnsureSuccessStatusCode();

            // Delete CG
            var deleteResult = await _client.DeleteAsync(searchUrl);
            deleteResult.EnsureSuccessStatusCode();

            // Search CG again
            var searchResultAfterDeletion = await _client.GetAsync(searchUrl);
            Assert.Equal(HttpStatusCode.NotFound, searchResultAfterDeletion.StatusCode);
        }

        [Fact]
        public async Task DeleteConsumerGroup_Error_AlreadyDeprecated()
        {
            // Create CG
            var cgIdUri = HttpUtility.UrlEncode(_deprecatedConsumerGroup).ToString();
            var searchUrl = $"{_apiPath}?subject={cgIdUri}";

            // Delete CG
            var deleteResult = await _client.DeleteAsync(searchUrl);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, deleteResult.StatusCode);
        }

        [Fact]
        public async Task DeleteConsumerGroup_Error_NotFound_InvalidUri()
        {
            var res = await _client.DeleteAsync($"{_apiPath}?subject=INVALID_Uri");
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        }

        [Fact]
        public async Task DeleteConsumerGroup_Error_NotFound_WrongUri()
        {
            var res = await _client.DeleteAsync($"{_apiPath}?subject=http://meh");
            Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        }

        [Fact]
        public async Task DeleteConsumerGroup_Error_BadRequest_EmptyUri()
        {
            var res = await _client.DeleteAsync($"{_apiPath}?subject=");
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        }

        #endregion

        // === HELPER ===
        private void AssertResult(IDictionary<string, List<dynamic>> properties, string label, string adrole, string lifecycleStatus = Graph.Metadata.Constants.ConsumerGroup.LifecycleStatus.Active)
        {
            Assert.NotNull(properties);
            Assert.Equal(new List<string> { Graph.Metadata.Constants.ConsumerGroup.Type }, properties[Graph.Metadata.Constants.RDF.Type]);
            Assert.Equal(new List<string> { label }, properties[Graph.Metadata.Constants.RDFS.Label]);
            Assert.Equal(new List<string> { adrole }, properties[Graph.Metadata.Constants.ConsumerGroup.AdRole]);
            Assert.Equal(new List<string> { lifecycleStatus }, properties[Graph.Metadata.Constants.ConsumerGroup.HasLifecycleStatus]);
        }
    }
}
