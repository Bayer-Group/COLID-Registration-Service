using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.RegistrationService.Common.DataModel.Resources;
using COLID.RegistrationService.Tests.Common.Builder;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace COLID.RegistrationService.Tests.Functional.Controllers.V3
{
    public class IdentifierControllerV3Tests : IClassFixture<FunctionTestsFixture>
    {
        private readonly HttpClient _client;
        private readonly FunctionTestsFixture _factory;
        private readonly ITestOutputHelper _output;
        private readonly string _apiPathV3 = "api/v3/identifier";
        private readonly Random _random = new Random();

        private const string PIDURI_TEMPLATE = "https://pid.bayer.com/kos/19050#cb849b80-6d00-4c3a-8810-6e4f91ee0cd1";

        public IdentifierControllerV3Tests(FunctionTestsFixture factory, ITestOutputHelper output)
        {
            _factory = factory;
            _client = _factory.CreateClient();
            _output = output;
        }


        #region Check Duplicate - OK

        [Fact]
        public async Task Post_CheckDuplicate_ShouldReturn_Ok()
        {
            var permanentIdentifier = "https://pid.bayer.com/kos/nice-permanent-identifier";
            var endpointPermanentIdentifier = "https://pid.bayer.com/kos/nice-endpoint-permanent-identifier";

            var endpoint = new DistributionEndpointBuilder()
                .GenerateSampleData()
                .WithPidUri(endpointPermanentIdentifier, string.Empty)
                .Build();

            var resourceRequestDto = new ResourceBuilder()
                .GenerateSampleData()
                .WithPidUri(permanentIdentifier, string.Empty)
                .WithDistributionEndpoint(endpoint)
                .BuildRequestDto();

            var resourceWriteResult = await CheckDuplicate(resourceRequestDto);

            // Assert
            _output.WriteLine(resourceWriteResult.Content);
            Assert.Equal(HttpStatusCode.OK, resourceWriteResult.Response.StatusCode);
            Assert.Empty(resourceWriteResult.ValidationResults);
        }

        [Fact]
        public async Task Post_CheckDuplicate_ShouldReturn_Ok_OnlyTemplate()
        {
            var resourceRequestDto = new ResourceBuilder()
                .GenerateSampleData()
                .BuildRequestDto();

            var resourceWriteResult = await CheckDuplicate(resourceRequestDto);

            // Assert
            _output.WriteLine(resourceWriteResult.Content);
            Assert.Equal(HttpStatusCode.OK, resourceWriteResult.Response.StatusCode);
            Assert.Empty(resourceWriteResult.ValidationResults);
        }

        [Fact]
        public async Task Post_CheckDuplicate_ShouldReturn_Ok_BaseUri()
        {
            var resourceRequestDto = new ResourceBuilder()
                .GenerateSampleData()
                .WithType(Graph.Metadata.Constants.Resource.Type.Ontology)
                .WithBaseUri(string.Empty)
                .BuildRequestDto();

            var resourceWriteResult = await CheckDuplicate(resourceRequestDto);

            // Assert
            _output.WriteLine(resourceWriteResult.Content);
            Assert.Equal(HttpStatusCode.OK, resourceWriteResult.Response.StatusCode);
            Assert.Empty(resourceWriteResult.ValidationResults);
        }

        [Fact]
        public async Task Post_CheckDuplicate_ShouldReturn_Ok_BaseUri_OnlyTemplate()
        {
            var permanentIdentifier = "https://pid.bayer.com/kos/nice-permanent-identifier";

            var resourceRequestDto = new ResourceBuilder()
                .GenerateSampleData()
                .WithType(Graph.Metadata.Constants.Resource.Type.Ontology)
                .WithBaseUri(permanentIdentifier, string.Empty)
                .BuildRequestDto();

            var resourceWriteResult = await CheckDuplicate(resourceRequestDto);

            // Assert
            _output.WriteLine(resourceWriteResult.Content);
            Assert.Equal(HttpStatusCode.OK, resourceWriteResult.Response.StatusCode);
            Assert.Empty(resourceWriteResult.ValidationResults);
        }

        [Fact]
        public async Task Post_CheckDuplicate_ShouldReturn_Ok_WithPreviousVersion()
        {
            var permanentIdentifier = "https://pid.bayer.com/kos/nice-permanent-identifier";
            var previousVersion = "https://pid.bayer.com/no-cg-reference-used-by-identifier/8c2bd5ca-b784-46f7-a964-46399902918f";

            var resourceRequestDto = new ResourceBuilder()
                .GenerateSampleData()
                .WithPidUri(permanentIdentifier, string.Empty)
                .BuildRequestDto();

            var resourceWriteResult = await CheckDuplicate(resourceRequestDto, previousVersion);

            // Assert
            _output.WriteLine(resourceWriteResult.Content);
            Assert.Equal(HttpStatusCode.OK, resourceWriteResult.Response.StatusCode);
            Assert.Equal(0, resourceWriteResult.ValidationResults.Count);
        }

        [Fact]
        public async Task Post_CheckDuplicate_ShouldReturn_Ok_BaseUriSameAsRepoPreviousVersionBaseUri()
        {
            var permanentIdentifier = "https://pid.bayer.com/deprecated/8c2bd5ca-b784-46f7-a964-46399902918f";
            var previousVersion = "https://pid.bayer.com/no-cg-reference-used-by-identifier/8c2bd5ca-b784-46f7-a964-46399902918f";

            var resourceRequestDto = new ResourceBuilder()
                .GenerateSampleData()
                .WithType(Graph.Metadata.Constants.Resource.Type.Ontology)
                .WithBaseUri(permanentIdentifier)
                .BuildRequestDto();

            var resourceWriteResult = await CheckDuplicate(resourceRequestDto, previousVersion);

            // Assert
            _output.WriteLine(resourceWriteResult.Content);
            Assert.Equal(HttpStatusCode.OK, resourceWriteResult.Response.StatusCode);
            Assert.Equal(0, resourceWriteResult.ValidationResults.Count);
        }

        #endregion

        #region Check Duplicate - Permanent Identifier - Same uris in entity

        [Fact]
        public async Task Post_CheckDuplicate_ShouldReturn_Error_PidUriSameAsBaseUri()
        {
            var permanentIdentifier = "https://pid.bayer.com/kos/nice-permanent-identifier";
            var resourceRequestDto = new ResourceBuilder()
                .GenerateSampleData()
                .WithType(Graph.Metadata.Constants.Resource.Type.Ontology)
                .WithPidUri(permanentIdentifier, string.Empty)
                .WithBaseUri(permanentIdentifier, string.Empty)
                .BuildRequestDto();

            var resourceWriteResult = await CheckDuplicate(resourceRequestDto);

            // Assert
            _output.WriteLine(resourceWriteResult.Content);
            Assert.Equal(HttpStatusCode.OK, resourceWriteResult.Response.StatusCode);
            Assert.Equal(2, resourceWriteResult.ValidationResults.Count);
            Assert.Contains(resourceWriteResult.ValidationResults,
                t => t.Path == Graph.Metadata.Constants.Resource.BaseUri && t.ResultSeverity == ValidationResultSeverity.Violation);
            Assert.Contains(resourceWriteResult.ValidationResults,
                t => t.Path == Graph.Metadata.Constants.EnterpriseCore.PidUri && t.ResultSeverity == ValidationResultSeverity.Violation);
        }

        [Fact]
        public async Task Post_CheckDuplicate_ShouldReturn_Error_PidUriSameAsEndpointUri()
        {
            var permanentIdentifier = "https://pid.bayer.com/kos/nice-permanent-identifier";

            var endpoint = new DistributionEndpointBuilder()
                .GenerateSampleData()
                .WithPidUri(permanentIdentifier, string.Empty)
                .Build();

            var resourceRequestDto = new ResourceBuilder()
                .GenerateSampleData()
                .WithPidUri(permanentIdentifier, string.Empty)
                .WithDistributionEndpoint(endpoint)
                .BuildRequestDto();

            var resourceWriteResult = await CheckDuplicate(resourceRequestDto);

            // Assert
            _output.WriteLine(resourceWriteResult.Content);
            Assert.Equal(HttpStatusCode.OK, resourceWriteResult.Response.StatusCode);
            Assert.Equal(2, resourceWriteResult.ValidationResults.Count);
            Assert.All(resourceWriteResult.ValidationResults, property => Assert.Equal(ValidationResultSeverity.Violation, property.ResultSeverity));

            var distinctIds = resourceWriteResult.ValidationResults.Select(t => t.Node).Distinct();
            Assert.Equal(2, distinctIds.Count());
        }

        [Fact]
        public async Task Post_CheckDuplicate_ShouldReturn_Error_BaseUriSameAsEndpointUri()
        {
            var permanentIdentifier = "https://pid.bayer.com/kos/nice-permanent-identifier";

            var endpoint = new DistributionEndpointBuilder()
                .GenerateSampleData()
                .WithPidUri(permanentIdentifier, string.Empty)
                .Build();

            var resourceRequestDto = new ResourceBuilder()
                .GenerateSampleData()
                .WithBaseUri(permanentIdentifier, string.Empty)
                .WithDistributionEndpoint(endpoint)
                .BuildRequestDto();

            var resourceWriteResult = await CheckDuplicate(resourceRequestDto);

            // Assert
            _output.WriteLine(resourceWriteResult.Content);
            Assert.Equal(HttpStatusCode.OK, resourceWriteResult.Response.StatusCode);
            Assert.Equal(2, resourceWriteResult.ValidationResults.Count);
            Assert.All(resourceWriteResult.ValidationResults, property => Assert.Equal(ValidationResultSeverity.Violation, property.ResultSeverity));

            var distinctIds = resourceWriteResult.ValidationResults.Select(t => t.Node).Distinct();
            Assert.Equal(2, distinctIds.Count());
        }

        [Fact]
        public async Task Post_CheckDuplicate_ShouldReturn_Error_PidUriAndBaseUriSameAsEndpointUri()
        {
            var permanentIdentifier = "https://pid.bayer.com/kos/nice-permanent-identifier";

            var endpoint = new DistributionEndpointBuilder()
                .GenerateSampleData()
                .WithPidUri(permanentIdentifier, string.Empty)
                .Build();

            var resourceRequestDto = new ResourceBuilder()
                .GenerateSampleData()
                .WithPidUri(permanentIdentifier, string.Empty)
                .WithBaseUri(permanentIdentifier, string.Empty)
                .WithDistributionEndpoint(endpoint)
                .BuildRequestDto();

            var resourceWriteResult = await CheckDuplicate(resourceRequestDto);

            // Assert
            _output.WriteLine(resourceWriteResult.Content);
            Assert.Equal(HttpStatusCode.OK, resourceWriteResult.Response.StatusCode);
            Assert.Equal(3, resourceWriteResult.ValidationResults.Count);
            Assert.All(resourceWriteResult.ValidationResults, property => Assert.Equal(ValidationResultSeverity.Violation, property.ResultSeverity));

            var distinctIds = resourceWriteResult.ValidationResults.Select(t => t.Node).Distinct();
            Assert.Equal(2, distinctIds.Count());

            Assert.Contains(resourceWriteResult.ValidationResults,
                t => t.Path == Graph.Metadata.Constants.EnterpriseCore.PidUri && t.Node == endpoint.Id);
        }

        [Fact]
        public async Task Post_CheckDuplicate_ShouldReturn_Error_SameEndpointUris()
        {
            var endpointPermanentIdentifier = "https://pid.bayer.com/kos/nice-permanent-identifier";

            var firstEndpoint = new DistributionEndpointBuilder()
                .GenerateSampleData()
                .WithPidUri(endpointPermanentIdentifier, string.Empty)
                .Build();

            var secondEndpointEndpoint = new DistributionEndpointBuilder()
                .GenerateSampleData()
                .WithNetworkAddress("https://www.second-network-adress.de")
                .WithPidUri(endpointPermanentIdentifier, string.Empty)
                .Build();

            var endpoints = new List<Entity>() { firstEndpoint, secondEndpointEndpoint };

            var resourceRequestDto = new ResourceBuilder()
                .GenerateSampleData()
                .WithDistributionEndpoint(endpoints)
                .BuildRequestDto();

            var resourceWriteResult = await CheckDuplicate(resourceRequestDto);

            // Assert
            _output.WriteLine(resourceWriteResult.Content);
            Assert.Equal(HttpStatusCode.OK, resourceWriteResult.Response.StatusCode);
            Assert.Equal(2, resourceWriteResult.ValidationResults.Count);
            Assert.All(resourceWriteResult.ValidationResults, property =>
            {
                Assert.Equal(Graph.Metadata.Constants.EnterpriseCore.PidUri, property.Path);
                Assert.Equal(ValidationResultSeverity.Violation, property.ResultSeverity);
            });
            Assert.Contains(resourceWriteResult.ValidationResults, property => property.Node == firstEndpoint.Id);
            Assert.Contains(resourceWriteResult.ValidationResults, property => property.Node == secondEndpointEndpoint.Id);
        }

        #endregion

        #region Check Duplicate - Permanent Identifier - With Repo

        [Fact]
        public async Task Post_CheckDuplicate_ShouldReturn_Error_Duplicate_PidUriSameAsRepoPidUri()
        {
            var duplicateUri = "https://pid.bayer.com/URI1010";

            var resourceRequestDto = new ResourceBuilder()
                .GenerateSampleData()
                .WithPidUri(duplicateUri, string.Empty)
                .BuildRequestDto();

            var resourceWriteResult = await CheckDuplicate(resourceRequestDto);

            // Assert
            _output.WriteLine(resourceWriteResult.Content);
            Assert.Equal(HttpStatusCode.OK, resourceWriteResult.Response.StatusCode);
            Assert.Equal(1, resourceWriteResult.ValidationResults.Count);

            Assert.Contains(resourceWriteResult.ValidationResults,
                t => t.Path == Graph.Metadata.Constants.EnterpriseCore.PidUri && t.ResultSeverity == ValidationResultSeverity.Violation);
        }

        [Fact]
        public async Task Post_CheckDuplicate_ShouldReturn_Error_Duplicate_PidUriSameAsRepoPreviousVersionPidUri()
        {
            var previousVersion = "https://pid.bayer.com/no-cg-reference-used-by-identifier/8c2bd5ca-b784-46f7-a964-46399902918f";

            var resourceRequestDto = new ResourceBuilder()
                .GenerateSampleData()
                .WithPidUri(previousVersion, string.Empty)
                .BuildRequestDto();

            var resourceWriteResult = await CheckDuplicate(resourceRequestDto, previousVersion);

            // Assert
            _output.WriteLine(resourceWriteResult.Content);
            Assert.Equal(HttpStatusCode.OK, resourceWriteResult.Response.StatusCode);
            Assert.Equal(1, resourceWriteResult.ValidationResults.Count);

            Assert.Contains(resourceWriteResult.ValidationResults,
                t => t.Path == Graph.Metadata.Constants.EnterpriseCore.PidUri && t.ResultSeverity == ValidationResultSeverity.Violation);
        }

        [Fact]
        public async Task Post_CheckDuplicate_ShouldReturn_Error_Duplicate_PidUriSameAsRepoBaseUri()
        {
            var duplicateUri = "https://pid.bayer.com/URI5005";

            var resourceRequestDto = new ResourceBuilder()
                .GenerateSampleData()
                .WithPidUri(duplicateUri, string.Empty)
                .BuildRequestDto();

            var resourceWriteResult = await CheckDuplicate(resourceRequestDto);

            // Assert
            _output.WriteLine(resourceWriteResult.Content);
            Assert.Equal(HttpStatusCode.OK, resourceWriteResult.Response.StatusCode);
            Assert.Equal(1, resourceWriteResult.ValidationResults.Count);

            Assert.Contains(resourceWriteResult.ValidationResults,
                t => t.Path == Graph.Metadata.Constants.EnterpriseCore.PidUri && t.ResultSeverity == ValidationResultSeverity.Violation);
        }

        [Fact]
        public async Task Post_CheckDuplicate_ShouldReturn_Error_Duplicate_BaseUriSameAsRepoBaseUri()
        {
            var duplicateUri = "https://pid.bayer.com/URI5005";

            var resourceRequestDto = new ResourceBuilder()
                .GenerateSampleData()
                .WithBaseUri(duplicateUri, string.Empty)
                .BuildRequestDto();

            var resourceWriteResult = await CheckDuplicate(resourceRequestDto);

            // Assert
            _output.WriteLine(resourceWriteResult.Content);
            Assert.Equal(HttpStatusCode.OK, resourceWriteResult.Response.StatusCode);
            Assert.Equal(1, resourceWriteResult.ValidationResults.Count);

            Assert.Contains(resourceWriteResult.ValidationResults,
                t => t.Path == Graph.Metadata.Constants.Resource.BaseUri && t.ResultSeverity == ValidationResultSeverity.Violation);
        }

        [Fact]
        public async Task Post_CheckDuplicate_ShouldReturn_Error_Duplicate_BaseUriSameAsRepoPidUri()
        {
            var duplicateUri = "https://pid.bayer.com/URI1010";

            var resourceRequestDto = new ResourceBuilder()
                .GenerateSampleData()
                .WithPidUri(duplicateUri, string.Empty)
                .BuildRequestDto();

            var resourceWriteResult = await CheckDuplicate(resourceRequestDto);

            // Assert
            _output.WriteLine(resourceWriteResult.Content);
            Assert.Equal(HttpStatusCode.OK, resourceWriteResult.Response.StatusCode);
            Assert.Equal(1, resourceWriteResult.ValidationResults.Count);

            Assert.Contains(resourceWriteResult.ValidationResults,
                t => t.Path == Graph.Metadata.Constants.EnterpriseCore.PidUri && t.ResultSeverity == ValidationResultSeverity.Violation);
        }

        [Fact]
        public async Task Post_CheckDuplicate_ShouldReturn_Error_Duplicate_BaseAndPidUriSameAsRepoBaseAndPidUri()
        {
            var duplicatePidUri = "https://pid.bayer.com/URI5004";
            var duplicateBaseUri = "https://pid.bayer.com/URI5005";

            var resourceRequestDto = new ResourceBuilder()
                .GenerateSampleData()
                .WithPidUri(duplicatePidUri, string.Empty)
                .WithBaseUri(duplicateBaseUri, string.Empty)
                .BuildRequestDto();

            var resourceWriteResult = await CheckDuplicate(resourceRequestDto);

            // Assert
            _output.WriteLine(resourceWriteResult.Content);
            Assert.Equal(HttpStatusCode.OK, resourceWriteResult.Response.StatusCode);
            Assert.Equal(2, resourceWriteResult.ValidationResults.Count);

            Assert.Contains(resourceWriteResult.ValidationResults,
                t => t.Path == Graph.Metadata.Constants.EnterpriseCore.PidUri && t.ResultSeverity == ValidationResultSeverity.Violation);
            Assert.Contains(resourceWriteResult.ValidationResults,
                t => t.Path == Graph.Metadata.Constants.Resource.BaseUri && t.ResultSeverity == ValidationResultSeverity.Violation);
        }

        [Fact]
        public async Task Post_CheckDuplicate_ShouldReturn_Error_Duplicate_PidUriSameAsRepoEndpointPidUri()
        {
            var permanentIdentifier = "https://pid.bayer.com/URI1020";

            var resourceRequestDto = new ResourceBuilder()
                .GenerateSampleData()
                .WithPidUri(permanentIdentifier, string.Empty)
                .BuildRequestDto();

            var resourceWriteResult = await CheckDuplicate(resourceRequestDto);

            // Assert
            _output.WriteLine(resourceWriteResult.Content);
            Assert.Equal(HttpStatusCode.OK, resourceWriteResult.Response.StatusCode);
            Assert.Equal(1, resourceWriteResult.ValidationResults.Count);
            Assert.All(resourceWriteResult.ValidationResults, property =>
            {
                Assert.Equal(Graph.Metadata.Constants.EnterpriseCore.PidUri, property.Path);
                Assert.Equal(ValidationResultSeverity.Violation, property.ResultSeverity);
            });
        }

        [Fact]
        public async Task Post_CheckDuplicate_ShouldReturn_Error_Duplicate_BaseUriSameAsRepoEndpointPidUri()
        {
            var permanentIdentifier = "https://pid.bayer.com/URI1020";

            var resourceRequestDto = new ResourceBuilder()
                .GenerateSampleData()
                .WithBaseUri(permanentIdentifier, string.Empty)
                .BuildRequestDto();

            var resourceWriteResult = await CheckDuplicate(resourceRequestDto);

            // Assert
            _output.WriteLine(resourceWriteResult.Content);
            Assert.Equal(HttpStatusCode.OK, resourceWriteResult.Response.StatusCode);
            Assert.Equal(1, resourceWriteResult.ValidationResults.Count);
            Assert.All(resourceWriteResult.ValidationResults, property =>
            {
                Assert.Equal(Graph.Metadata.Constants.Resource.BaseUri, property.Path);
                Assert.Equal(ValidationResultSeverity.Violation, property.ResultSeverity);
            });
        }

        [Fact]
        public async Task Post_CheckDuplicate_ShouldReturn_Error_Duplicate_EndpointPidUriSameAsRepoEndpointPidUri()
        {
            var endpointPermanentIdentifier = "https://pid.bayer.com/URI1020";

            var firstEndpoint = new DistributionEndpointBuilder()
                .GenerateSampleData()
                .WithPidUri(endpointPermanentIdentifier, string.Empty)
                .Build();

            var resourceRequestDto = new ResourceBuilder()
                .GenerateSampleData()
                .WithDistributionEndpoint(firstEndpoint)
                .BuildRequestDto();

            var resourceWriteResult = await CheckDuplicate(resourceRequestDto);

            // Assert
            _output.WriteLine(resourceWriteResult.Content);
            Assert.Equal(HttpStatusCode.OK, resourceWriteResult.Response.StatusCode);
            Assert.Equal(1, resourceWriteResult.ValidationResults.Count);
            Assert.All(resourceWriteResult.ValidationResults, property =>
            {
                Assert.Equal(firstEndpoint.Id, property.Node);
                Assert.Equal(Graph.Metadata.Constants.EnterpriseCore.PidUri, property.Path);
                Assert.Equal(ValidationResultSeverity.Violation, property.ResultSeverity);
            });
        }

        [Fact]
        public async Task Post_CheckDuplicate_ShouldReturn_Error_Duplicate_EndpointPidUriSameAsRepoPidUri()
        {
            var endpointPermanentIdentifier = "https://pid.bayer.com/URI1010";

            var firstEndpoint = new DistributionEndpointBuilder()
                .GenerateSampleData()
                .WithPidUri(endpointPermanentIdentifier, string.Empty)
                .Build();

            var resourceRequestDto = new ResourceBuilder()
                .GenerateSampleData()
                .WithDistributionEndpoint(firstEndpoint)
                .BuildRequestDto();

            var resourceWriteResult = await CheckDuplicate(resourceRequestDto);

            // Assert
            _output.WriteLine(resourceWriteResult.Content);
            Assert.Equal(HttpStatusCode.OK, resourceWriteResult.Response.StatusCode);
            Assert.Equal(1, resourceWriteResult.ValidationResults.Count);
            Assert.All(resourceWriteResult.ValidationResults, property =>
            {
                Assert.Equal(firstEndpoint.Id, property.Node);
                Assert.Equal(Graph.Metadata.Constants.EnterpriseCore.PidUri, property.Path);
                Assert.Equal(ValidationResultSeverity.Violation, property.ResultSeverity);
            });
        }

        [Fact]
        public async Task Post_CheckDuplicate_ShouldReturn_Error_Duplicate_EndpointPidUriSameAsRepoBaseUri()
        {
            var endpointPermanentIdentifier = "https://pid.bayer.com/URI5005";

            var firstEndpoint = new DistributionEndpointBuilder()
                .GenerateSampleData()
                .WithPidUri(endpointPermanentIdentifier, string.Empty)
                .Build();

            var resourceRequestDto = new ResourceBuilder()
                .GenerateSampleData()
                .WithDistributionEndpoint(firstEndpoint)
                .BuildRequestDto();

            var resourceWriteResult = await CheckDuplicate(resourceRequestDto);

            // Assert
            _output.WriteLine(resourceWriteResult.Content);
            Assert.Equal(HttpStatusCode.OK, resourceWriteResult.Response.StatusCode);
            Assert.Equal(1, resourceWriteResult.ValidationResults.Count);
            Assert.All(resourceWriteResult.ValidationResults, property =>
            {
                Assert.Equal(firstEndpoint.Id, property.Node);
                Assert.Equal(Graph.Metadata.Constants.EnterpriseCore.PidUri, property.Path);
                Assert.Equal(ValidationResultSeverity.Violation, property.ResultSeverity);
            });
        }

        #endregion

        #region Check Duplicate - Target Uri

        [Fact]
        public async Task Post_CheckDuplicate_ShouldReturn_Error_Duplicate_EndpointTargetUriSameAsTargetUri()
        {
            var network_address = "https://www.network-adress.de";

            var firstEndpoint = new DistributionEndpointBuilder()
                .GenerateSampleData()
                .WithNetworkAddress(network_address)
                .Build();

            var secondEndpointEndpoint = new DistributionEndpointBuilder()
                .GenerateSampleData()
                .WithNetworkAddress(network_address)
                .Build();

            var endpoints = new List<Entity>() { firstEndpoint, secondEndpointEndpoint };

            var resourceRequestDto = new ResourceBuilder()
                .GenerateSampleData()
                .WithDistributionEndpoint(endpoints)
                .BuildRequestDto();

            var resourceWriteResult = await CheckDuplicate(resourceRequestDto);

            // Assert
            _output.WriteLine(resourceWriteResult.Content);
            Assert.Equal(HttpStatusCode.OK, resourceWriteResult.Response.StatusCode);
            Assert.Equal(2, resourceWriteResult.ValidationResults.Count);
            Assert.All(resourceWriteResult.ValidationResults, property =>
            {
                Assert.Equal(Graph.Metadata.Constants.Resource.DistributionEndpoints.HasNetworkAddress, property.Path);
                Assert.Equal(ValidationResultSeverity.Info, property.ResultSeverity);
            });
            Assert.Contains(resourceWriteResult.ValidationResults, property => property.Node == firstEndpoint.Id);
            Assert.Contains(resourceWriteResult.ValidationResults, property => property.Node == secondEndpointEndpoint.Id);
        }

        [Fact]
        public async Task Post_CheckDuplicate_ShouldReturn_Error_Duplicate_EndpointTargetUriSameAsRepoTargetUri()
        {
            var firstEndpoint = new DistributionEndpointBuilder()
                .GenerateSampleData()
                .WithNetworkAddress("http://ID1012")
                .Build();

            var endpoints = new List<Entity>() { firstEndpoint };

            var resourceRequestDto = new ResourceBuilder()
                .GenerateSampleData()
                .WithDistributionEndpoint(endpoints)
                .BuildRequestDto();

            var resourceWriteResult = await CheckDuplicate(resourceRequestDto);

            // Assert
            _output.WriteLine(resourceWriteResult.Content);
            Assert.Equal(HttpStatusCode.OK, resourceWriteResult.Response.StatusCode);
            Assert.Equal(1, resourceWriteResult.ValidationResults.Count);
            
            Assert.All(resourceWriteResult.ValidationResults, property =>
            {
                Assert.Equal(Graph.Metadata.Constants.Resource.DistributionEndpoints.HasNetworkAddress, property.Path);
                Assert.Equal(ValidationResultSeverity.Info, property.ResultSeverity);
            });
            Assert.Contains(resourceWriteResult.ValidationResults, property => property.Node == firstEndpoint.Id);
        }

        [Fact]
        public async Task Post_CheckDuplicate_ShouldReturn_Error_Duplicate_MultipleEndpointTargetUrisSameAsRepoTargetUris()
        {
            var firstEndpoint = new DistributionEndpointBuilder()
                .GenerateSampleData()
                .WithNetworkAddress("http://ID1012")
                .Build();

            var secondEndpointEndpoint = new DistributionEndpointBuilder()
                .GenerateSampleData()
                .WithNetworkAddress("http://ID5021")
                .Build();

            var endpoints = new List<Entity>() { firstEndpoint, secondEndpointEndpoint };

            var resourceRequestDto = new ResourceBuilder()
                .GenerateSampleData()
                .WithDistributionEndpoint(endpoints)
                .BuildRequestDto();

            var resourceWriteResult = await CheckDuplicate(resourceRequestDto);

            // Assert
            _output.WriteLine(resourceWriteResult.Content);
            Assert.Equal(HttpStatusCode.OK, resourceWriteResult.Response.StatusCode);
            Assert.Equal(2, resourceWriteResult.ValidationResults.Count);
            Assert.All(resourceWriteResult.ValidationResults, property =>
            {
                Assert.Equal(Graph.Metadata.Constants.Resource.DistributionEndpoints.HasNetworkAddress, property.Path);
                Assert.Equal(ValidationResultSeverity.Info, property.ResultSeverity);
            });
            Assert.Contains(resourceWriteResult.ValidationResults, property => property.Node == firstEndpoint.Id);
            Assert.Contains(resourceWriteResult.ValidationResults, property => property.Node == secondEndpointEndpoint.Id);
        }

        #endregion

        #region Helper methods to call api
        private async Task<ColidIdentifierResponse> CheckDuplicate(ResourceRequestDTO resourceRequestDto, string previousVersion = "")
        {
            var url = $"{_apiPathV3}/checkForDuplicate";
            if (!string.IsNullOrWhiteSpace(previousVersion))
            {
                url += $"?previousVersion={previousVersion}";
            }

            var response = await _client.PostAsync(url, BuildJsonHttpContent(resourceRequestDto));
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            _output.WriteLine(content);
            Assert.NotNull(content);
            var result = JsonConvert.DeserializeObject<List<ValidationResultProperty>>(content);
            Assert.NotNull(result);

            if (!response.IsSuccessStatusCode)
            {
                _output.WriteLine(result.ToString());
            }

            return new ColidIdentifierResponse(response, content, result);
        }

        #endregion


        private class ColidIdentifierResponse
        {
            public HttpResponseMessage Response { get; set; }
            public string Content { get; set; }
            public IList<ValidationResultProperty> ValidationResults { get; set; }

            public ColidIdentifierResponse(HttpResponseMessage rsp, string cnt)
            {
                Response = rsp;
                Content = cnt;
            }

            public ColidIdentifierResponse(HttpResponseMessage rsp, string cnt, IList<ValidationResultProperty> res)
            {
                Response = rsp;
                Content = cnt;
                ValidationResults = res;
            }

        }


        private static HttpContent BuildJsonHttpContent<T>(T obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            HttpContent requestContent = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
            return requestContent;
        }

        private static HttpContent BuildJsonHttpContent<T>(params T[] obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            HttpContent requestContent = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
            return requestContent;
        }


    }
}
