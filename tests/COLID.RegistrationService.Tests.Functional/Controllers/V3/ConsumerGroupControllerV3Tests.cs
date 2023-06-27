using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.DataModels.ConsumerGroups;
using COLID.RegistrationService.Tests.Common.Builder;
using Newtonsoft.Json;
using Xunit;

namespace COLID.RegistrationService.Tests.Functional.Controllers.V3
{
    public class ConsumerGroupControllerV3Tests : IClassFixture<FunctionTestsFixture>
    {
        private readonly HttpClient _client;
        private readonly FunctionTestsFixture _factory;

        private readonly string _apiPath = "api/v3/consumerGroup";

        private readonly string _deprecatedConsumerGroup =
            "https://pid.bayer.com/kos/19050#695533d3-391f-4249-81f9-f1674d1ea9dc";

        public ConsumerGroupControllerV3Tests(FunctionTestsFixture factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        /// <summary>
        /// Route GET api/v3/consumerGroupList
        /// </summary>
        [Fact]
        public async Task GetConsumerGroupList_Success()
        {
            // Act
            var result = await _client.GetAsync($"{_apiPath}List");

            // Assert
            result.EnsureSuccessStatusCode();
            var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
            var actualTestResults = Newtonsoft.Json.JsonConvert.DeserializeObject<List<BaseEntityResultDTO>>(content);
            Assert.Equal(6, actualTestResults.Count);

            var indigoCG = actualTestResults.FirstOrDefault(cg => cg.Id == "https://pid.bayer.com/kos/19050#bf2f8eeb-fdb9-4ee1-ad88-e8932fa8753c");
            Assert.Equal(5, indigoCG.Properties.Count());
            AssertResult(indigoCG.Properties, "INDIGO", "PID.Group01Data.ReadWrite");
            Assert.Contains("https://pid.bayer.com/kos/19050#5a9bc613-f948-4dd3-8cd7-9a4465319d24", indigoCG.Properties[Graph.Metadata.Constants.ConsumerGroup.HasPidUriTemplate]);
            Assert.Equal(5, indigoCG.Properties[Graph.Metadata.Constants.ConsumerGroup.HasPidUriTemplate].Count());

            var dinosCG = actualTestResults.FirstOrDefault(cg => cg.Id == "https://pid.bayer.com/kos/19050#82fc2870-ca4e-407f-a197-bf3766ad785f");
            Assert.Equal(5, dinosCG.Properties.Count());
            AssertResult(dinosCG.Properties, "DINOS", "PID.Group02Data.ReadWrite");
            Assert.Single(dinosCG.Properties[Graph.Metadata.Constants.ConsumerGroup.HasPidUriTemplate]);
        }

        # region Testing GET api/v3/consumerGroup

        /// <summary>
        /// Route GET api/v3/consumerGroup
        /// </summary>
        [Fact]
        public async Task GetOneConsumerGroupById_Success()
        {
            // Arrange
            var pidUri = HttpUtility.UrlEncode("https://pid.bayer.com/kos/19050#2b3f0380-dd22-4666-a28b-7f1eeb82a5ff").ToString();
            var url = $"{_apiPath}?id={pidUri}";

            // Act
            var result = await _client.GetAsync(url);

            // Assert
            result.EnsureSuccessStatusCode();
            var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
            var dataServicesCG = Newtonsoft.Json.JsonConvert.DeserializeObject<ConsumerGroupResultDTO>(content);

            Assert.Single(dataServicesCG.Properties[Graph.Metadata.Constants.ConsumerGroup.HasPidUriTemplate]);
            AssertResult(dataServicesCG.Properties, "Data Services", "PID.Group03Data.ReadWrite");
        }

        [Fact]
        public async Task GetOneConsumerGroupById_Error_BadRequest_EmptyUri()
        {
            var result = await _client.GetAsync($"{_apiPath}?id=");
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task GetOneConsumerGroupById_Error_NotFound_InvalidUri()
        {
            var result = await _client.GetAsync($"{_apiPath}?id=INVALID_SUBJECT");
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        #endregion

        #region Testing POST api/v3/consumerGroup

        /// <summary>
        /// Route POST api/v3/consumerGroup
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
            var cgResult = JsonConvert.DeserializeObject<ConsumerGroupWriteResultCTO>(content);

            Assert.NotNull(cgResult);
            Assert.NotNull(cgResult.Entity);
            AssertResult(cgResult.Entity.Properties, "SUSHI", "group4.read");
            Assert.Single(cgResult.Entity.Properties[Graph.Metadata.Constants.ConsumerGroup.HasPidUriTemplate]);
            Assert.True(cgResult.ValidationResult.Conforms);

            // Cleanup | TODO: Cleanup via Test SetUp and TearDown
            var deleteResult = await _client.DeleteAsync($"{_apiPath}?id={HttpUtility.UrlEncode(cgResult.Entity.Id).ToString()}");
            deleteResult.EnsureSuccessStatusCode();
        }

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
            var cgResult = JsonConvert.DeserializeObject<ConsumerGroupWriteResultCTO>(content).Entity;

            Assert.NotNull(cgResult);
            AssertResult(cgResult.Properties, "SUSHI", "group4.read");
            Assert.Single(cgResult.Properties[Graph.Metadata.Constants.ConsumerGroup.HasPidUriTemplate]);

            // Cleanup | TODO: Cleanup via Test SetUp and TearDown
            var deleteResult = await _client.DeleteAsync($"{_apiPath}?id={HttpUtility.UrlEncode(cgResult.Id).ToString()}");
            deleteResult.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task CreateConsumerGroup_Success_WithDefaultTemplateIsMissing()
        {
            // Arrange
            var cg = new ConsumerGroupBuilder()
                .GenerateSampleData()
                .WithLabel("SUSHI")
                .WithAdRole("group4.read")
                .WithDefaultPidUriTemplate("https://pid.bayer.com/kos/19050#10168d5b-9eb9-4767-90cb-d7e99a1660ac")
                .Build();
            HttpContent requestContent = new StringContent(cg.ToString(), Encoding.UTF8, MediaTypeNames.Application.Json);

            // Act
            var result = await _client.PostAsync(_apiPath, requestContent);
            result.EnsureSuccessStatusCode();

            // Assert
            var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
            var cgResult = JsonConvert.DeserializeObject<ConsumerGroupWriteResultCTO>(content);

            Assert.NotNull(cgResult);
            Assert.NotNull(cgResult.Entity);
            AssertResult(cgResult.Entity.Properties, "SUSHI", "group4.read");
            Assert.Equal(2, cgResult.Entity.Properties[Graph.Metadata.Constants.ConsumerGroup.HasPidUriTemplate].Count);
            Assert.False(cgResult.ValidationResult.Conforms);
            Assert.Equal(ValidationResultSeverity.Info, cgResult.ValidationResult.Severity);


            // Cleanup | TODO: Cleanup via Test SetUp and TearDown
            var deleteResult = await _client.DeleteAsync($"{_apiPath}?id={HttpUtility.UrlEncode(cgResult.Entity.Id).ToString()}");
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
        public async Task CreateConsumerGroup_Error_BadRequest_InvalidContactPerson()
        {
            // TODO: Implement PidUriTemplateCheck during creation of consumer groups

            // Arrange
            var cg = new ConsumerGroupBuilder()
                .WithPidUriTemplate("https://pid.bayer.com/kos/19050#bbcacfb7-f7c3-4b71-a509-d020ad703e83")
                .WithContactPerson("invalid.person@bayer.com")
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

        #region Testing PUT api/v3/consumerGroup

        /// <summary>
        /// Route PUT api/v3/consumerGroup
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
            var createEntity = JsonConvert.DeserializeObject<ConsumerGroupWriteResultCTO>(createContent).Entity;
            var cgIdUri = HttpUtility.UrlEncode(createEntity.Id).ToString();
            var searchUrl = $"{_apiPath}?id={cgIdUri}";

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
            var updatedEntity = JsonConvert.DeserializeObject<ConsumerGroupResultDTO>(updateContentAfterEdit);

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
            var createEntity = JsonConvert.DeserializeObject<ConsumerGroupWriteResultCTO>(createContent).Entity;
            var cgIdUri = HttpUtility.UrlEncode(createEntity.Id).ToString();
            var searchUrl = $"{_apiPath}?id={cgIdUri}";

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
            var updatedEntity = JsonConvert.DeserializeObject<ConsumerGroupResultDTO>(updateContentAfterEdit);

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
            var editResult = await _client.PutAsync($"{_apiPath}?id={cgIdUri}", updateRequestContent);
            Assert.Equal(HttpStatusCode.BadRequest, editResult.StatusCode);
        }


        [Fact]
        public async Task EditConsumerGroup_Error_BadRequest_DeprecatedConsumerGroup()
        {
            var cgIdUri = HttpUtility.UrlEncode(_deprecatedConsumerGroup).ToString();
            var searchUrl = $"{_apiPath}?id={cgIdUri}";

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
            var editResult = await _client.PutAsync($"{_apiPath}?id=", updateRequestContent);
            Assert.Equal(HttpStatusCode.BadRequest, editResult.StatusCode);
        }

        [Fact]
        public async Task EditConsumerGroup_Error_NotFound_InvalidUri()
        {
            var cg = new ConsumerGroupBuilder().GenerateSampleData().WithLabel("FROMAGE").WithAdRole("group4.read").Build();
            HttpContent updateRequestContent = new StringContent(cg.ToString(), Encoding.UTF8, MediaTypeNames.Application.Json);
            var editResult = await _client.PutAsync($"{_apiPath}?id=INVALID_URI", updateRequestContent);
            Assert.Equal(HttpStatusCode.BadRequest, editResult.StatusCode);
        }

        #endregion

        #region Testing DELETE api/v3/consumerGroup

        /// <summary>
        /// Route DELETE api/v3/consumerGroup
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
            var createEntity = JsonConvert.DeserializeObject<ConsumerGroupWriteResultCTO>(createContent).Entity;
            var cgIdUri = HttpUtility.UrlEncode(createEntity.Id).ToString();
            var searchUrl = $"{_apiPath}?id={cgIdUri}";

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
        public async Task DeleteConsumerGroup_Deprecated_Success()
        {
            var cgIdUri = HttpUtility.UrlEncode("https://pid.bayer.com/kos/19050#bf2f8eeb-fdb9-4ee1-ad88-e8932fa8753c").ToString();
            var searchUrl = $"{_apiPath}?id={cgIdUri}";

            // Search CG
            var searchResult = await _client.GetAsync(searchUrl);
            searchResult.EnsureSuccessStatusCode();

            // Delete CG
            var deleteResult = await _client.DeleteAsync(searchUrl);
            deleteResult.EnsureSuccessStatusCode();

            // Search CG again
            var searchResultAfterDeletion = await _client.GetAsync(searchUrl);
            searchResultAfterDeletion.EnsureSuccessStatusCode();
            var updateContentAfterDeletion = await searchResultAfterDeletion.Content.ReadAsStringAsync().ConfigureAwait(false);
            var updatedEntity = JsonConvert.DeserializeObject<ConsumerGroupResultDTO>(updateContentAfterDeletion);

            AssertResult(updatedEntity.Properties, "INDIGO", "PID.Group01Data.ReadWrite", Graph.Metadata.Constants.ConsumerGroup.LifecycleStatus.Deprecated);

            // Clean up
            var reactivationUrl = $"{_apiPath}/reactivate?id={cgIdUri}";
            var reactivationResult = await _client.PostAsync(reactivationUrl, null);
            reactivationResult.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task DeleteConsumerGroup_Error_AlreadyDeprecated()
        {
            // Create CG
            var cgIdUri = HttpUtility.UrlEncode(_deprecatedConsumerGroup).ToString();
            var searchUrl = $"{_apiPath}?id={cgIdUri}";

            // Delete CG
            var deleteResult = await _client.DeleteAsync(searchUrl);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, deleteResult.StatusCode);
        }

        [Fact]
        public async Task DeleteConsumerGroup_Error_NotFound_InvalidUri()
        {
            var res = await _client.DeleteAsync($"{_apiPath}?id=INVALID_Uri");
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        }

        [Fact]
        public async Task DeleteConsumerGroup_Error_NotFound_WrongUri()
        {
            var res = await _client.DeleteAsync($"{_apiPath}?id=http://meh");
            Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        }

        [Fact]
        public async Task DeleteConsumerGroup_Error_BadRequest_EmptyUri()
        {
            var res = await _client.DeleteAsync($"{_apiPath}?id=");
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        }

        #endregion

        #region  Testing Reactivation api/v3/consumerGroup/reactivate

        [Fact]
        public async Task ReactivateConsumerGroup_Success()
        {
            var cgIdUri = HttpUtility.UrlEncode(_deprecatedConsumerGroup).ToString();
            var searchUrl = $"{_apiPath}?id={cgIdUri}";

            // Reactivate CG
            var reactivationUrl = $"{_apiPath}/reactivate?id={cgIdUri}";
            var reactivationResult = await _client.PostAsync(reactivationUrl, null);
            reactivationResult.EnsureSuccessStatusCode();

            // Search CG
            var searchResultAfterReactivation = await _client.GetAsync(searchUrl);
            searchResultAfterReactivation.EnsureSuccessStatusCode();
            var updateContentAfterReactivation = await searchResultAfterReactivation.Content.ReadAsStringAsync().ConfigureAwait(false);
            var updatedEntity = JsonConvert.DeserializeObject<ConsumerGroupResultDTO>(updateContentAfterReactivation);

            AssertResult(updatedEntity.Properties, "Deprecated", "PID.Group06Data.ReadWrite");

            // Delete CG
            var deleteResult = await _client.DeleteAsync(searchUrl);
            deleteResult.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task ReactivateConsumerGroup_Error_AlreadyActive()
        {
            // Create CG
            var createCg = new ConsumerGroupBuilder().GenerateSampleData().WithLabel("TONNO").WithAdRole("group7.read").Build();
            HttpContent createRequestContent = new StringContent(createCg.ToString(), Encoding.UTF8, MediaTypeNames.Application.Json);
            var createResult = await _client.PostAsync(_apiPath, createRequestContent);
            createResult.EnsureSuccessStatusCode();
            var createContent = await createResult.Content.ReadAsStringAsync().ConfigureAwait(false);
            var createEntity = JsonConvert.DeserializeObject<ConsumerGroupWriteResultCTO>(createContent).Entity;
            
            var cgIdUri = HttpUtility.UrlEncode(createEntity.Id).ToString();
            var searchUrl = $"{_apiPath}?id={cgIdUri}";

            // Search CG
            var searchResult = await _client.GetAsync(searchUrl);
            searchResult.EnsureSuccessStatusCode();

            // Reactivate CG
            var reactivationUrl = $"{_apiPath}/reactivate?id={cgIdUri}";
            var reactivationResult = await _client.PostAsync(reactivationUrl, null);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, reactivationResult.StatusCode);
        }

        [Fact]
        public async Task ReactivateConsumerGroup_Error_NotFound_InvalidUri()
        {
            var res = await _client.PostAsync($"{_apiPath}/reactivate?id=INVALID_Uri", null);
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        }

        [Fact]
        public async Task ReactivateConsumerGroup_Error_NotFound_WrongUri()
        {
            var res = await _client.PostAsync($"{_apiPath}/reactivate?id=http://meh", null);
            Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        }

        [Fact]
        public async Task ReactivateConsumerGroup_Error_BadRequest_EmptyUri()
        {
            var res = await _client.PostAsync($"{_apiPath}/reactivate?id=", null);
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
