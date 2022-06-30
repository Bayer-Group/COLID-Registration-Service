using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using COLID.Common.Extensions;
using COLID.RegistrationService.Common.Enums.PidUriTemplate;
using COLID.RegistrationService.Common.Extensions;
using COLID.RegistrationService.Tests.Common.Builder;
using COLID.RegistrationService.Tests.Common.Utils;
using COLID.RegistrationService.Tests.Functional.DataModel.V1;
using Newtonsoft.Json;
using Xunit;

namespace COLID.RegistrationService.Tests.Functional.Controllers.V1
{
    public class PidUriTemplateControllerV1Tests : IClassFixture<FunctionTestsFixture>
    {
        private readonly HttpClient _client;
        private readonly FunctionTestsFixture _factory;

        private static readonly string _apiPath = $"api/v1/pidUriTemplate";

        private const string _validId = "https://pid.bayer.com/kos/19050#13cd004a-a410-4af5-a8fc-eecf9436b58b";
        private const string _deprecatedId = "https://pid.bayer.com/kos/19050#bbcacfb7-f7c3-4b71-a509-d020ad703e83";
        private const string _notFoundId = "https://pid.bayer.com/kos/19050#not-found-uri";
        private const string _invalidId = "INVALID_Uri";
        private const string _emptyId = "";
        
        // Pid uri template that has no connection to a consumer group, but was used by an identifier Entry ID6000 
        private const string _validIdWithoutCgReference = "https://pid.bayer.com/kos/19050#14d9eeb8-d85d-446d-9703-3a0f43482f5a";

        public PidUriTemplateControllerV1Tests(FunctionTestsFixture factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        /// <summary>
        /// Route GET pidUriTemplateList
        /// </summary>
        [Fact]
        public async Task GetPidUriTemplateList_Success()
        {
            // Act
            var result = await _client.GetAsync($"{_apiPath}List");

            // Assert
            result.EnsureSuccessStatusCode();
            var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
            var actualTestResults = JsonConvert.DeserializeObject<List<BaseEntityResultDtoV1>>(content);
            Assert.Equal(10, actualTestResults.Count);

            var GUID_0 = actualTestResults.FirstOrDefault(cg => cg.Subject == _validId);
            AssertResult(GUID_0, "https://pid.bayer.com/{GUID:0}/",
                _validId,
                "https://pid.bayer.com/",
                "0",
                IdType.Guid.GetDescription(),
                Suffix.Slash.GetDescription());
        }

        #region Testing GET /api/{version}/pidUriTemplate

        /// <summary>
        /// Route GET pidUriTemplateList
        /// </summary>
        [Fact]
        public async Task GetOnePidUriTemplateById_Success()
        {
            // Arrange
            var id = HttpUtility.UrlEncode("https://pid.bayer.com/kos/19050#c890bb5a-10cd-4c9e-a96a-43c62eb55375");
            var url = $"{_apiPath}?subject={id}";

            // Act
            var result = await _client.GetAsync(url);

            // Assert
            result.EnsureSuccessStatusCode();
            var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
            var dinos = Newtonsoft.Json.JsonConvert.DeserializeObject<BaseEntityResultDtoV1>(content);

            AssertResult(dinos, "https://pid.bayer.com/DINOS/{GUID:0}",
                "https://pid.bayer.com/kos/19050#c890bb5a-10cd-4c9e-a96a-43c62eb55375",
                "https://pid.bayer.com/",
                "0",
                IdType.Guid.GetDescription(),
                Suffix.Empty.GetDescription(),
                "DINOS/");
        }

        [Fact]
        public async Task GetOnePidUriTemplateById_Error_BadRequest_EmptyUri()
        {
            var result = await _client.GetAsync($"{_apiPath}?subject=");
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task GetOnePidUriTemplateById_Error_NotFound_InvalidUri()
        {
            var result = await _client.GetAsync($"{_apiPath}?subject=INVALID_ID");
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        #endregion Testing GET /api/{version}/pidUriTemplate

        #region Testing POST /api/{version}/pidUriTemplate

        /// <summary>
        /// Route POST api/v3/pidUriTemplate
        /// </summary>
        [Fact]
        public async Task CreatePidUriTemplate_Success()
        {
            var baseUrl = Graph.Metadata.Constants.Resource.PidUrlPrefix;
            var pidUriTemplateIdType = TestUtils.GetRandomEnumValue<IdType>();
            var pidUriTemplateSuffix = TestUtils.GetRandomEnumValue<Suffix>();

            // Arrange
            var pidUriTpl = new PidUriTemplateBuilder().GenerateSampleData()
                .WithBaseUrl(baseUrl)
                .WithPidUriTemplateIdType(pidUriTemplateIdType)
                .WithPidUriTemplateSuffix(pidUriTemplateSuffix)
                .Build();

            HttpContent requestContent = new StringContent(pidUriTpl.ToString(), Encoding.UTF8, MediaTypeNames.Application.Json);

            // Act
            var result = await _client.PostAsync(_apiPath, requestContent);
            result.EnsureSuccessStatusCode();

            // Assert
            var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
            var pidUriTplResult = JsonConvert.DeserializeObject<PidUriTemplateWriteResultCtoV1>(content).Entity;

            Assert.NotNull(pidUriTplResult);
            AssertResultProperties(pidUriTplResult.Properties, baseUrl, "1", pidUriTemplateIdType, pidUriTemplateSuffix);

            // Cleanup
            var deleteResult = await _client.DeleteAsync($"{_apiPath}?subject={HttpUtility.UrlEncode(pidUriTplResult.Subject)}");
            deleteResult.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task CreatePidUriTemplate_Error_BadRequest_EmptyContent()
        {
            // Arrange
            HttpContent requestContent = new StringContent(string.Empty, Encoding.UTF8, MediaTypeNames.Application.Json);

            // Act
            var result = await _client.PostAsync(_apiPath, requestContent);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task CreatePidUriTemplate_Error_BadRequest_SameTemplateExists()
        {
            var baseUrl = Graph.Metadata.Constants.Resource.PidUrlPrefix;
            var pidUriTemplateIdType = TestUtils.GetRandomEnumValue<IdType>();
            var pidUriTemplateSuffix = TestUtils.GetRandomEnumValue<Suffix>();

            // Arrange
            var pidUriTpl = new PidUriTemplateBuilder().GenerateSampleData()
                .WithBaseUrl(baseUrl)
                .WithPidUriTemplateIdType(pidUriTemplateIdType)
                .WithPidUriTemplateSuffix(pidUriTemplateSuffix)
                .Build();

            HttpContent requestContent = new StringContent(pidUriTpl.ToString(), Encoding.UTF8, MediaTypeNames.Application.Json);

            // Act for creation
            var result = await _client.PostAsync(_apiPath, requestContent);
            result.EnsureSuccessStatusCode();

            // Assert for creation
            var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
            var pidUriTplResult = JsonConvert.DeserializeObject<PidUriTemplateWriteResultCtoV1>(content).Entity;

            Assert.NotNull(pidUriTplResult);
            AssertResultProperties(pidUriTplResult.Properties, baseUrl, "1", pidUriTemplateIdType, pidUriTemplateSuffix);

            // Act
            var resultBadRequest = await _client.PostAsync(_apiPath, requestContent);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, resultBadRequest.StatusCode);

            // Cleanup
            var deleteResult = await _client.DeleteAsync($"{_apiPath}?subject={HttpUtility.UrlEncode(pidUriTplResult.Subject)}");
            deleteResult.EnsureSuccessStatusCode();
        }

        #endregion Testing POST /api/{version}/pidUriTemplate

        #region Testing PUT api/{version}/pidUriTemplate

        /// <summary>
        /// Route PUT pidUriTemplate
        /// </summary>
        [Fact]
        public async Task EditPidUriTemplate_Success()
        {
            var baseUrl = Graph.Metadata.Constants.Resource.PidUrlPrefix;
            var pidUriTemplateIdType = TestUtils.GetRandomEnumValue<IdType>();
            var pidUriTemplateSuffix = TestUtils.GetRandomEnumValue<Suffix>();

            // Create TPL
            var pidUriTpl = new PidUriTemplateBuilder().GenerateSampleData().WithBaseUrl(baseUrl).WithPidUriTemplateIdType(pidUriTemplateIdType).WithPidUriTemplateSuffix(pidUriTemplateSuffix).Build();
            HttpContent createRequestContent = new StringContent(pidUriTpl.ToString(), Encoding.UTF8, MediaTypeNames.Application.Json);
            var createResult = await _client.PostAsync(_apiPath, createRequestContent);
            createResult.EnsureSuccessStatusCode();
            var createdContent = await createResult.Content.ReadAsStringAsync().ConfigureAwait(false);
            var createdEntity = JsonConvert.DeserializeObject<PidUriTemplateWriteResultCtoV1>(createdContent);
            var searchUrl = $"{_apiPath}?subject={TestUtils.EncodeIfNecessary(createdEntity.Entity.Subject)}";

            // Search TPL
            var searchResult = await _client.GetAsync(searchUrl);
            searchResult.EnsureSuccessStatusCode();

            // Edit TPL
            var newBaseUrl = baseUrl.Replace("pid.", "pid-dev.");
            var updateTpl = new PidUriTemplateBuilder().GenerateSampleData().WithBaseUrl(newBaseUrl).WithPidUriTemplateIdType(pidUriTemplateIdType).WithPidUriTemplateSuffix(pidUriTemplateSuffix).Build();
            HttpContent updateRequestContent = new StringContent(updateTpl.ToString(), Encoding.UTF8, MediaTypeNames.Application.Json);
            var editResult = await _client.PutAsync(searchUrl, updateRequestContent);
            editResult.EnsureSuccessStatusCode();

            // Search TPL
            var searchResultAfterEdit = await _client.GetAsync(searchUrl);
            searchResultAfterEdit.EnsureSuccessStatusCode();
            var updateContentAfterEdit = await searchResultAfterEdit.Content.ReadAsStringAsync().ConfigureAwait(false);
            var updatedEntity = JsonConvert.DeserializeObject<BaseEntityResultDtoV1>(updateContentAfterEdit);

            Assert.Equal(new List<string> { newBaseUrl }, updatedEntity.Properties[COLID.Graph.Metadata.Constants.PidUriTemplate.HasBaseUrl]);

            // Cleanup TPL
            var deleteResult = await _client.DeleteAsync(searchUrl);
            deleteResult.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task EditPidUriTemplate_Error_BadRequest_SameTemplateExists()
        {
            var baseUrl = Graph.Metadata.Constants.Resource.PidUrlPrefix;
            var pidUriTemplateIdType = IdType.Guid.GetDescription();
            var pidUriTemplateSuffix = Suffix.Empty.GetDescription();

            // Arrange

            // Create TPL
            var pidUriTpl = new PidUriTemplateBuilder().GenerateSampleData().WithBaseUrl(baseUrl).WithPidUriTemplateIdType(pidUriTemplateIdType).WithPidUriTemplateSuffix(pidUriTemplateSuffix).Build();
            HttpContent createRequestContent = new StringContent(pidUriTpl.ToString(), Encoding.UTF8, MediaTypeNames.Application.Json);
            var createResult = await _client.PostAsync(_apiPath, createRequestContent);
            createResult.EnsureSuccessStatusCode();
            var createdContent = await createResult.Content.ReadAsStringAsync().ConfigureAwait(false);
            var createdEntity = JsonConvert.DeserializeObject<PidUriTemplateWriteResultCtoV1>(createdContent);
            var searchUrl = $"{_apiPath}?subject={TestUtils.EncodeIfNecessary(createdEntity.Entity.Subject)}";

            // Search TPL
            var searchResult = await _client.GetAsync(searchUrl);
            searchResult.EnsureSuccessStatusCode();

            // Edit TPL
            var newSearchUrl = $"{_apiPath}?subject={TestUtils.EncodeIfNecessary(_validId)}";
            HttpContent updateRequestContent = new StringContent(pidUriTpl.ToString(), Encoding.UTF8, MediaTypeNames.Application.Json);
            var editResult = await _client.PutAsync(newSearchUrl, updateRequestContent);

            Assert.Equal(HttpStatusCode.BadRequest, editResult.StatusCode);

            // Cleanup
            var deleteResult = await _client.DeleteAsync(searchUrl);
            deleteResult.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task EditPidUriTemplate_Error_BadRequest_UpdateExistingWithoutContent()
        {
            HttpContent updateRequestContent = new StringContent(string.Empty, Encoding.UTF8, MediaTypeNames.Application.Json);
            var idUri = HttpUtility.UrlEncode(_validId);
            var editResult = await _client.PutAsync($"{_apiPath}?subject={idUri}", updateRequestContent);
            Assert.Equal(HttpStatusCode.BadRequest, editResult.StatusCode);
        }

        [Fact]
        public async Task EditPidUriTemplate_Error_BadRequest_UpdateDeprecatedTemplate()
        {
            HttpContent updateRequestContent = new StringContent(string.Empty, Encoding.UTF8, MediaTypeNames.Application.Json);
            var idUri = HttpUtility.UrlEncode(_deprecatedId);
            var editResult = await _client.PutAsync($"{_apiPath}?subject={idUri}", updateRequestContent);
            Assert.Equal(HttpStatusCode.BadRequest, editResult.StatusCode);
        }

        [Fact]
        public async Task EditPidUriTemplate_Error_NotFound_EmptyUri()
        {
            var pidUriTpl = new PidUriTemplateBuilder().GenerateSampleData().Build();
            HttpContent updateRequestContent = new StringContent(pidUriTpl.ToString(), Encoding.UTF8, MediaTypeNames.Application.Json);
            var editResult = await _client.PutAsync($"{_apiPath}?subject=", updateRequestContent);
            Assert.Equal(HttpStatusCode.BadRequest, editResult.StatusCode);
        }

        [Fact]
        public async Task EditPidUriTemplate_Error_NotFound_InvalidUri()
        {
            var pidUriTpl = new PidUriTemplateBuilder().GenerateSampleData().Build();
            HttpContent updateRequestContent = new StringContent(pidUriTpl.ToString(), Encoding.UTF8, MediaTypeNames.Application.Json);
            var editResult = await _client.PutAsync($"{_apiPath}?subject=INVALID_URI", updateRequestContent);
            Assert.Equal(HttpStatusCode.BadRequest, editResult.StatusCode);
        }

        #endregion Testing PUT api/{version}/pidUriTemplate

        #region Testing DELETE api/{version}/pidUriTemplate

        /// <summary>
        /// Route DELETE pidUriTemplate
        /// </summary>
        [Fact]
        public async Task DeletePidUriTemplate_Purge_Success()
        {
            // Create Tpl
            var baseUrl = Graph.Metadata.Constants.Resource.PidUrlPrefix;
            var pidUriTemplateIdType = TestUtils.GetRandomEnumValue<IdType>();
            var pidUriTemplateSuffix = TestUtils.GetRandomEnumValue<Suffix>();

            var pidUriTpl = new PidUriTemplateBuilder().GenerateSampleData()
                .WithBaseUrl(baseUrl)
                .WithPidUriTemplateIdType(pidUriTemplateIdType)
                .WithPidUriTemplateSuffix(pidUriTemplateSuffix)
                .Build();

            HttpContent requestContent = new StringContent(pidUriTpl.ToString(), Encoding.UTF8, MediaTypeNames.Application.Json);
            var result = await _client.PostAsync(_apiPath, requestContent);
            result.EnsureSuccessStatusCode();

            var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
            var pidUriTplResult = JsonConvert.DeserializeObject<PidUriTemplateWriteResultCtoV1>(content).Entity;
            var searchUrl = $"{_apiPath}?subject={TestUtils.EncodeIfNecessary(pidUriTplResult.Subject)}";

            // Search Template
            var searchResult = await _client.GetAsync(searchUrl);
            searchResult.EnsureSuccessStatusCode();

            // Delete Template
            var deleteResult = await _client.DeleteAsync(searchUrl);
            deleteResult.EnsureSuccessStatusCode();

            // Search Template again
            var searchResultAfterDeletion = await _client.GetAsync(searchUrl);
            Assert.Equal(HttpStatusCode.NotFound, searchResultAfterDeletion.StatusCode);
        }

        /// <summary>
        /// Route DELETE pidUriTemplate, but set template as deprecated
        /// </summary>
        [Fact]
        public async Task DeletePidUriTemplate_Deprecated_Success()
        {
            var searchUrl = $"{_apiPath}?subject={TestUtils.EncodeIfNecessary(_validIdWithoutCgReference)}";

            // Search Template
            var searchResult = await _client.GetAsync(searchUrl);
            searchResult.EnsureSuccessStatusCode();

            // Delete Template
            var deleteResult = await _client.DeleteAsync(searchUrl);
            deleteResult.EnsureSuccessStatusCode();

            // Search Template again
            var searchResultAfterDeletion = await _client.GetAsync(searchUrl);
            searchResultAfterDeletion.EnsureSuccessStatusCode();
            var updateContentAfterDeletion = await searchResultAfterDeletion.Content.ReadAsStringAsync().ConfigureAwait(false);
            var updatedEntity = JsonConvert.DeserializeObject<BaseEntityResultDtoV1>(updateContentAfterDeletion);

            Assert.Equal(new List<string> { COLID.Graph.Metadata.Constants.PidUriTemplate.LifecycleStatus.Deprecated }, updatedEntity.Properties[COLID.Graph.Metadata.Constants.PidUriTemplate.HasLifecycleStatus]);
        }

        /// <summary>
        /// Route DELETE pidUriTemplate, but set template as deprecated
        /// </summary>
        [Fact]
        public async Task DeletePidUriTemplate_ERROR_AlreadyDeprecated()
        {
            var searchUrl = $"{_apiPath}?subject={TestUtils.EncodeIfNecessary(_deprecatedId)}";

            // Search Template
            var searchResult = await _client.GetAsync(searchUrl);
            searchResult.EnsureSuccessStatusCode();

            // Delete Template
            var deleteResult = await _client.DeleteAsync(searchUrl);

            // Search Template again
            Assert.Equal(HttpStatusCode.BadRequest, deleteResult.StatusCode);
        }


        [Fact]
        public async Task DeletePidUriTemplate_Error_UsedByConsumerGroup()
        {
            var tplIdUri = TestUtils.EncodeIfNecessary(_validId);
            var searchUrl = $"{_apiPath}?subject={tplIdUri}";

            // Search CG
            var searchResult = await _client.GetAsync(searchUrl);
            searchResult.EnsureSuccessStatusCode();

            // Delete CG
            var deleteResult = await _client.DeleteAsync(searchUrl);

            Assert.Equal(HttpStatusCode.Conflict, deleteResult.StatusCode);
        }

        [Fact]
        public async Task DeleteDeletePidUriTemplate_Error_NotFound_InvalidUri()
        {
            var res = await _client.DeleteAsync($"{_apiPath}?subject=INVALID_Uri");
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        }

        public async Task DeleteDeletePidUriTemplate_Error_NotFound_WrongUri()
        {
            // TODO: Implement check for deletion of templates, that are not present.
            var res = await _client.DeleteAsync($"{_apiPath}?subject=http://meh");
            Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        }

        [Fact]
        public async Task DeleteDeletePidUriTemplate_Error_BadRequest_EmptyUri()
        {
            var res = await _client.DeleteAsync($"{_apiPath}?subject=");
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        }

        #endregion Testing DELETE api/{version}/pidUriTemplate

        // === HELPER ===
        private void AssertResult(BaseEntityResultDtoV1 resultToCheck, string name, string id, string baseUrl, string idLength, string idType, string suffix, string route = "")
        {
            Assert.NotNull(resultToCheck);
            Assert.Equal(name, resultToCheck.Name);
            Assert.Equal(id, resultToCheck.Subject);

            // properties
            Assert.NotNull(resultToCheck.Properties);
            var properties = resultToCheck.Properties;
            AssertResultProperties(properties, baseUrl, idLength, idType, suffix, route);
        }

        private void AssertResultProperties(IDictionary<string, List<dynamic>> properties, string baseUrl, string idLength, string idType, string suffix, string route = "")
        {
            Assert.NotNull(properties);
            Assert.Equal(new List<string> { COLID.Graph.Metadata.Constants.PidUriTemplate.Type }, properties[Graph.Metadata.Constants.RDF.Type]);
            Assert.Equal(new List<string> { baseUrl }, properties[COLID.Graph.Metadata.Constants.PidUriTemplate.HasBaseUrl]);
            Assert.Equal(new List<string> { idLength }, properties[COLID.Graph.Metadata.Constants.PidUriTemplate.HasIdLength]);
            Assert.Equal(new List<string> { idType }, properties[COLID.Graph.Metadata.Constants.PidUriTemplate.HasPidUriTemplateIdType]);
            Assert.Equal(new List<string> { suffix }, properties[COLID.Graph.Metadata.Constants.PidUriTemplate.HasPidUriTemplateSuffix]);

            if (!string.IsNullOrWhiteSpace(route))
            {
                Assert.Equal(new List<string> { route }, properties[COLID.Graph.Metadata.Constants.PidUriTemplate.HasRoute]);
            }
        }
    }
}
