using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using COLID.Common.Extensions;
using COLID.RegistrationService.Common.DataModel.Resources;
using COLID.RegistrationService.Common.Enums.ColidEntry;
using COLID.RegistrationService.Common.Extensions;
using COLID.RegistrationService.Tests.Common.Builder;
using COLID.RegistrationService.Tests.Common.Extensions;
using COLID.RegistrationService.Tests.Common.Utils;
using COLID.RegistrationService.Tests.Functional.Authorization;
using COLID.RegistrationService.Tests.Functional.Extensions;
using COLID.Graph.TripleStore.DataModels.Base;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RedLockNet;
using Xunit;
using Xunit.Abstractions;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.RegistrationService.Common.DataModel.Resources.Comparison;

namespace COLID.RegistrationService.Tests.Functional.Controllers.V3
{
    public class ResourceControllerV3Tests : IClassFixture<FunctionTestsFixture>
    {
        private readonly HttpClient _client;
        private readonly HttpClient _adminClient;
        private readonly HttpClient _api2ApiClient;
        private readonly FunctionTestsFixture _factory;
        private readonly IDistributedLockFactory _distributedLockFactory;
        private readonly ITestOutputHelper _output;
        private readonly string _apiPathV3 = "api/v3/resource";
        private readonly Random _random = new Random();

        private const string PIDURI_TEMPLATE = "https://pid.bayer.com/kos/19050#cb849b80-6d00-4c3a-8810-6e4f91ee0cd1";

        public ResourceControllerV3Tests(FunctionTestsFixture factory, ITestOutputHelper output)
        {
            _factory = factory;
            _client = _factory.CreateClient();
            _adminClient = _factory.WithAuthentication<AdminTestAuthenticationHandler>().CreateClient();
            _api2ApiClient = _factory.WithAuthentication<ApiToApiTestAuthenticationHandler>().CreateClient();
            _distributedLockFactory = _factory.Services.GetService<IDistributedLockFactory>();
            _output = output;
        }


        #region GET Resource
        [Fact]
        public async Task Get_ResourceByPidUriAndColidEntryLifecycleStatus_Success()
        {
            // Arrange
            var pidUri = "https://pid.bayer.com/URI1010";
            var entryLifecycleStatus = Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Published;
            var resourceResponse = await GetResource(pidUri, entryLifecycleStatus);
            var resource = resourceResponse.Resource;

            // == Header
            Assert.Equal("https://pid.bayer.com/kos/19050#d8d8f921-cf1d-43d8-af0e-581a32a43def", resource.Id);
            Assert.Equal("https://pid.bayer.com/URI1010", resource.PidUri.ToString());
            Assert.Null(resource.BaseUri);
            Assert.Null(resource.PreviousVersion);
            Assert.Null(resource.LaterVersion);
            Assert.Null(resource.PublishedVersion);

            // == Properties
            var property = resource.Properties;

            var expectedDe = new DistributionEndpointBuilder()
                .WithId("https://pid.bayer.com/kos/19050#7a492679-e2a4-4d43-a330-ab34b6131a42")
                .WithType(RegistrationService.Common.Enums.DistributionEndpoint.Type.BrowsableResource)
                .WithNetworkedResourceLabel("ID1012").WithNetworkAddress("http://ID1012")
                .WithDistributionEndpointLifecycleStatus(RegistrationService.Common.Enums.DistributionEndpoint.LifecycleStatus.Active)
                .WithPidUri("https://pid.bayer.com/URI1020", null).Build();
            TestUtils.AssertSameEntityContent(expectedDe, property[Graph.Metadata.Constants.Resource.Distribution].First());

            Assert.True(property.ContainsResourceDefinition("ID1002"));
            Assert.True(property.ContainsLabel("ID1002"));
            Assert.True(property.ContainsLifecycleStatus(EnumExtension.GetDescription(LifecycleStatus.Released)));
            Assert.True(property.ContainsEntryLifecycleStatus(ColidEntryLifecycleStatus.Published.GetDescription()));
            Assert.True(property.ContainsInformationClassification("https://pid.bayer.com/kos/19050/Open"));
            Assert.True(property.ContainsLastChangeUser("simon.lansing.ext@bayer.com"));
            Assert.True(property.ContainsAuthor("tim.odenthal.ext@bayer.com"));
            Assert.True(property.ContainsLicensedData("false"));
            Assert.True(property.ContainsVersion("2"));
            Assert.True(property.ContainsType(EnumExtension.GetDescription(RegistrationService.Common.Enums.ColidEntry.Type.GenericDataset)));
            Assert.True(property.ContainsIsDerivedFromDataset("https://pid.bayer.com/URI5002"));
            Assert.True(property.ContainsIsPersonalData("false"));
            Assert.True(property.ContainsHasConsumerGroup("https://pid.bayer.com/kos/19050#bf2f8eeb-fdb9-4ee1-ad88-e8932fa8753c"));
            Assert.True(property.ContainsHasHistoricVersion("https://pid.bayer.com/kos/19050#b2461db8-7280-4224-b7e5-678ef67e0c6a"));

            Assert.Equal("2019-12-19T14:24:09Z", property[Graph.Metadata.Constants.Resource.DateCreated].First().ToString("s") + "Z");
            Assert.Equal("2019-12-19T14:24:31Z", property[Graph.Metadata.Constants.Resource.DateModified].First().ToString("s") + "Z");
        }

        [Fact]
        public async Task Get_ResourceByPidUriAndLifecycleStatus_Error_NotFound()
        {
            // Arrange
            var pidUri = "https://pid.bayer.com/URI1010";
            var entryLifecycleStatus = Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Draft;
            var fullPath = $"{_apiPathV3}?pidUri={pidUri}&lifecycleStatus={entryLifecycleStatus}";

            // Act
            var result = await _client.GetAsync(fullPath);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        }

        [Fact]
        public async Task Get_ResourceByPidUriAndLifecycleStatus_Error_Unallowed()
        {
            // Arrange
            var pidUri = "https://pid.bayer.com/URI1010";
            var pidEntryLifecycleStatus = Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Historic;
            var fullPath = $"{_apiPathV3}?pidUri={pidUri}&lifecycleStatus={pidEntryLifecycleStatus}";

            // Act
            var result = await _client.GetAsync(fullPath);
            var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }
        #endregion 

        #region Create Resource

        [Fact]
        public async Task Create_Returns_CreatedResource()
        {
            var pidUri = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";

            var resourceRequestDto = new ResourceBuilder()
                .WithLabel("this is a very fine resource")
                .WithEntryLifecycleStatus(ColidEntryLifecycleStatus.Draft)
                .WithLifecycleStatus(LifecycleStatus.Released)
                .WithAuthor("christian.kaubisch.ext@bayer.com")
                .WithVersion("4")
                .WithInformationClassification("https://pid.bayer.com/kos/19050/Restricted")
                .WithLastChangeUser("christian.kaubisch.ext@bayer.com")
                .WithConsumerGroup($"{Graph.Metadata.Constants.Entity.IdPrefix}bf2f8eeb-fdb9-4ee1-ad88-e8932fa8753c")
                .WithLastChangeDateTime(DateTime.UtcNow.ToString("o"))
                .WithDateCreated(DateTime.UtcNow.ToString("o"))
                .WithType("https://pid.bayer.com/kos/19050/GenericDataset")
                .WithResourceDefinition("version 4")
                .HasPersonalData(true)
                .HasLicensedData(false)
                .WithPidUri(pidUri, "https://pid.bayer.com/kos/19050#cb849b80-6d00-4c3a-8810-6e4f91ee0cd1")
                .BuildRequestDto();

            var resourceWriteResult = await CreateResource(resourceRequestDto);

            // Assert
            ValidateValidColidResourceCreationResponse(resourceWriteResult, pidUri);

            AssertResourceRequestWithEntity(resourceRequestDto, resourceWriteResult.Resource);

            var draftResourceResult = await GetResource(pidUri);

            ValidateGetColidResourceResponse(draftResourceResult, pidUri);

            AssertResourceRequestWithEntity(resourceRequestDto, draftResourceResult.Resource);
        }

        [Fact]
        public async Task Create_Returns_CreatedResource_RemoveProperties()
        {
            var pidUri = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";

            var expectedResource = new ResourceBuilder()
                .GenerateSampleData(pidUri, PIDURI_TEMPLATE)
                .WithMetadataGraphConfiguration("https://pid.bayer.com/kos/19050#0a4a27fa-7217-410d-8826-454830b62d6f")
                .BuildRequestDto();

            var actualResource = new ResourceBuilder()
                .GenerateSampleData(pidUri, PIDURI_TEMPLATE)
                .WithHistoricVersion(pidUri)
                .WithLaterVersion(pidUri)
                .BuildRequestDto();

            var resourceWriteResult = await CreateResource(actualResource);

            // Assert
            ValidateValidColidResourceCreationResponse(resourceWriteResult, pidUri);
            AssertResourceRequestWithEntity(expectedResource, resourceWriteResult.Resource);

            var draftResourceResult = await GetResource(pidUri);
            ValidateGetColidResourceResponse(draftResourceResult, pidUri);
            AssertResourceRequestWithEntity(expectedResource, draftResourceResult.Resource);
        }

        [Fact]
        public async Task Create_Returns_CreatedResource_OverwriteProperties()
        {
            var pidUri = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";

            var expectedResource = GetExpectedCreatedResource(pidUri);

            var actualResource = new ResourceBuilder()
                .GenerateSampleData(pidUri, PIDURI_TEMPLATE)
                .WithKeyword("PID")
                .WithDateCreated("some-invalid-datetime")
                .WithLastChangeDateTime("some-invalid-datetime")
                .WithEntryLifecycleStatus(ColidEntryLifecycleStatus.Published)
                .WithMetadataGraphConfiguration("https://pid.bayer.com/kos/19050#invalid-config")
                .BuildRequestDto();

            Entity pidUriEntity = actualResource.Properties[Graph.Metadata.Constants.EnterpriseCore.PidUri].First();
            pidUriEntity.Properties[Graph.Metadata.Constants.RDF.Type] = new List<dynamic> { Graph.Metadata.Constants.Resource.Type.Ontology };

            WriteLine("actualResource", actualResource);
            var resourceWriteResult = await CreateResource(_adminClient, actualResource);

            // Assert
            ValidateValidColidResourceCreationResponse(resourceWriteResult, pidUri);
            AssertResourceRequestWithEntity(expectedResource, resourceWriteResult.Resource);

            var draftResourceResult = await GetResource(_adminClient, pidUri);
            ValidateGetColidResourceResponse(draftResourceResult, pidUri);
            AssertResourceRequestWithEntity(expectedResource, draftResourceResult.Resource);
        }

        [Fact]
        // Check if person fields will be overwritten
        public async Task Create_Returns_CreatedResource_Admin_OverwriteProperties()
        {
            var pidUri = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";

            var expectedResource = GetExpectedCreatedResource(pidUri);

            var actualResource = new ResourceBuilder()
                .GenerateSampleData(pidUri, PIDURI_TEMPLATE)
                .BuildRequestDto();

            var resourceWriteResult = await CreateResource(_adminClient, actualResource);

            // Assert
            ValidateValidColidResourceCreationResponse(resourceWriteResult, pidUri);
            AssertResourceRequestWithEntity(expectedResource, resourceWriteResult.Resource);

            var draftResourceResult = await GetResource(_adminClient, pidUri);
            ValidateGetColidResourceResponse(draftResourceResult, pidUri);
            AssertResourceRequestWithEntity(expectedResource, draftResourceResult.Resource);
        }

        [Fact]
        // Check if person fields wont be overwritten
        public async Task Create_Returns_CreatedResource_Api2Api_OverwriteProperties()
        {
            var pidUri = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";

            var expectedResource = new ResourceBuilder()
                .GenerateSampleData(pidUri, PIDURI_TEMPLATE)
                .BuildRequestDto();

            var actualResource = new ResourceBuilder()
                .GenerateSampleData(pidUri, PIDURI_TEMPLATE)
                .BuildRequestDto();

            var resourceWriteResult = await CreateResource(_api2ApiClient, actualResource);

            // Assert
            ValidateValidColidResourceCreationResponse(resourceWriteResult, pidUri);
            AssertResourceRequestWithEntity(expectedResource, resourceWriteResult.Resource);

            var draftResourceResult = await GetResource(_api2ApiClient, pidUri);
            ValidateGetColidResourceResponse(draftResourceResult, pidUri);
            AssertResourceRequestWithEntity(expectedResource, draftResourceResult.Resource);
        }

        [Fact]
        public async Task Create_Returns_CreatedResource_Api2Api_InvalidAuthor()
        {
            var pidUri = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999)}";

            var actualResource = new ResourceBuilder()
                .GenerateSampleData(pidUri, PIDURI_TEMPLATE)
                .WithAuthor("invalid_author@bayer.com")
                .BuildRequestDto();

            var resourceWriteResult = await CreateResource(_api2ApiClient, actualResource);

            // Assert 
            ValidateCriticalColidResourceCreationResponse(resourceWriteResult, pidUri);

            Assert.Single(resourceWriteResult.ValidationResult.Results);
            Assert.False(resourceWriteResult.ValidationResult.Conforms);
            Assert.Equal(ValidationResultSeverity.Violation, resourceWriteResult.ValidationResult.Severity);
            Assert.All(resourceWriteResult.ValidationResult.Results, r =>
            {
                Assert.Equal(ValidationResultPropertyType.CUSTOM, r.Type);
                Assert.Equal(Graph.Metadata.Constants.Resource.Author, r.Path);
                Assert.Equal(ValidationResultSeverity.Violation, r.ResultSeverity);

            });
        }


        [Fact]
        public async Task Create_Returns_Error_Critical_ShaclValidationResults()
        {
            var pidUri = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";

            var resourceRequestDto = new ResourceBuilder()
                .WithLabel("this is a very fine resource", "this is a very fine resource with a second label")
                .WithEntryLifecycleStatus(ColidEntryLifecycleStatus.Draft)
                .WithLifecycleStatus(LifecycleStatus.Released)
                .WithAuthor("christian.kaubisch.ext@bayer.com")
                .WithVersion("4", "5")
                .WithInformationClassification("https://pid.bayer.com/kos/19050/Restricted")
                .WithLastChangeUser("christian.kaubisch.ext@bayer.com")
                .WithConsumerGroup($"{Graph.Metadata.Constants.Entity.IdPrefix}bf2f8eeb-fdb9-4ee1-ad88-e8932fa8753c")
                .WithLastChangeDateTime(DateTime.UtcNow.ToString("o"))
                .WithDateCreated(DateTime.UtcNow.ToString("o"))
                .WithType("https://pid.bayer.com/kos/19050/GenericDataset")
                .WithResourceDefinition("version 4")
                .HasPersonalData(true)
                .HasLicensedData(false)
                .WithPidUri(pidUri, "https://pid.bayer.com/kos/19050#cb849b80-6d00-4c3a-8810-6e4f91ee0cd1")
                .BuildRequestDto();


            var resourceWriteResult = await CreateResource(resourceRequestDto);

            // Assert
            ValidateCriticalColidResourceCreationResponse(resourceWriteResult, pidUri);

            Assert.Equal(2, resourceWriteResult.ValidationResult.Results.Count());
            Assert.False(resourceWriteResult.ValidationResult.Conforms);
            Assert.Equal(ValidationResultSeverity.Violation, resourceWriteResult.ValidationResult.Severity);
            Assert.All(resourceWriteResult.ValidationResult.Results, r =>
            {
                Assert.Equal(ValidationResultPropertyType.SHACL, r.Type);
                Assert.Equal(ValidationResultSeverity.Violation, r.ResultSeverity);
            });
        }

        [Fact]
        public async Task Create_Returns_Error_Warning_ShaclValidationResults()
        {
            var pidUri = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";

            var resourceRequestDto = new ResourceBuilder()
                .WithLabel("this is a very fine resource")
                .WithEntryLifecycleStatus(ColidEntryLifecycleStatus.Draft)
                .WithAuthor("christian.kaubisch.ext@bayer.com")
                .WithVersion("4")
                .WithInformationClassification("https://pid.bayer.com/kos/19050/Restricted")
                .WithLastChangeUser("christian.kaubisch.ext@bayer.com")
                .WithConsumerGroup($"{Graph.Metadata.Constants.Entity.IdPrefix}bf2f8eeb-fdb9-4ee1-ad88-e8932fa8753c")
                .WithLastChangeDateTime(DateTime.UtcNow.ToString("o"))
                .WithDateCreated(DateTime.UtcNow.ToString("o"))
                .WithType("https://pid.bayer.com/kos/19050/GenericDataset")
                .WithResourceDefinition("version 4", "version 4 - v2")
                .HasPersonalData(true)
                .HasLicensedData(false)
                .WithPidUri(pidUri, "https://pid.bayer.com/kos/19050#cb849b80-6d00-4c3a-8810-6e4f91ee0cd1")
                .BuildRequestDto();


            var resourceWriteResult = await CreateResource(resourceRequestDto);

            // Assert
            ValidateWarningColidResourceCreationResponse(resourceWriteResult, pidUri);

            Assert.Equal(2, resourceWriteResult.ValidationResult.Results.Count());
            Assert.False(resourceWriteResult.ValidationResult.Conforms);
            Assert.Equal(ValidationResultSeverity.Warning, resourceWriteResult.ValidationResult.Severity);
            Assert.All(resourceWriteResult.ValidationResult.Results, r =>
            {
                Assert.Equal(ValidationResultPropertyType.SHACL, r.Type);
                Assert.Equal(ValidationResultSeverity.Warning, r.ResultSeverity);
            });
        }

        // TODO: Check all errors that can occur through validators
        [Fact]
        public async Task Create_Returns_Error_InvalidProperties()
        {
            var pidUri = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";

            var actualResource = new ResourceBuilder()
                .GenerateSampleData(pidUri, PIDURI_TEMPLATE)
                .BuildRequestDto();

            var resourceWriteResult = await CreateResource(_api2ApiClient, actualResource);

            // Assert
            // ValidateCriticalColidResourceCreationResponse(resourceWriteResult, pidUri);
        }

        // TODO:
        // Validator invalid datetime format
        // Validator linking

        [Fact]
        public async Task Create_Returns_CreatedResource_ConvertSingleValuesToArray()
        {
            var pidUri = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999)}";

            var resourceLabel = "this is a very fine resource";
            var endpoint = new DistributionEndpointBuilder().GenerateSampleData().Build();

            var resourceRequestDto = new ResourceBuilder()
                .WithLabel(resourceLabel)
                .WithEntryLifecycleStatus(ColidEntryLifecycleStatus.Draft)
                .WithLifecycleStatus(LifecycleStatus.Released)
                .WithAuthor("christian.kaubisch.ext@bayer.com")
                .WithVersion("4")
                .WithInformationClassification("https://pid.bayer.com/kos/19050/Restricted")
                .WithLastChangeUser("christian.kaubisch.ext@bayer.com")
                .WithConsumerGroup($"{Graph.Metadata.Constants.Entity.IdPrefix}bf2f8eeb-fdb9-4ee1-ad88-e8932fa8753c")
                .WithLastChangeDateTime(DateTime.UtcNow.ToString("o"))
                .WithDateCreated(DateTime.UtcNow.ToString("o"))
                .WithType("https://pid.bayer.com/kos/19050/GenericDataset")
                .WithResourceDefinition("version 4")
                .HasPersonalData(true)
                .HasLicensedData(false)
                .WithDistributionEndpoint(endpoint)
                .WithPidUri(pidUri, "https://pid.bayer.com/kos/19050#cb849b80-6d00-4c3a-8810-6e4f91ee0cd1")
                .BuildRequestDto();

            var json = JObject.FromObject(resourceRequestDto);
            json["Properties"][Graph.Metadata.Constants.Resource.HasLabel] = resourceLabel;
            json["Properties"][Graph.Metadata.Constants.Resource.Distribution] = JToken.FromObject(endpoint);

            HttpContent requestContent = new StringContent(json.ToString(), Encoding.UTF8, MediaTypeNames.Application.Json);

            var resourceWriteResult = await CreateResource(_client, requestContent);

            // Assert
            ValidateValidColidResourceCreationResponse(resourceWriteResult, pidUri);
            AssertResourceRequestWithEntity(resourceRequestDto, resourceWriteResult.Resource);

            var draftResourceResult = await GetResource(pidUri);

            ValidateGetColidResourceResponse(draftResourceResult, pidUri);
            AssertResourceRequestWithEntity(resourceRequestDto, draftResourceResult.Resource);
        }

        private ResourceRequestDTO GetExpectedCreatedResource(string pidUri)
        {
            return new ResourceBuilder().GenerateSampleData()
                .WithPidUri(pidUri, PIDURI_TEMPLATE)
                .WithEntryLifecycleStatus(ColidEntryLifecycleStatus.Draft)
                .WithAuthor("johnny.rocket@bayer.com")
                .WithLastChangeUser("johnny.rocket@bayer.com")
                .WithMetadataGraphConfiguration("https://pid.bayer.com/kos/19050#0a4a27fa-7217-410d-8826-454830b62d6f")
                .BuildRequestDto();
        }

        #endregion

        #region Edit Resource

        [Fact]
        public async Task Edit_Returns_EditedResource()
        {
            var pidUri1 = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            var resourceCreateWriteResult = await CreateResource(pidUri1);

            var editedResourceRequest = new ResourceRequestDTO() { Properties = resourceCreateWriteResult.Resource.Properties };
            editedResourceRequest.Properties[Graph.Metadata.Constants.Resource.HasLabel] = new List<dynamic> { "this is a very fine edited resource" };

            var resourceWriteResult = await EditResource(resourceCreateWriteResult.Resource.PidUri, editedResourceRequest);

            // Assert
            ValidateValidColidResourceCreationResponse(resourceWriteResult, pidUri1);

            WriteLine("Expected: ", editedResourceRequest);
            WriteLine("Actual: ", resourceWriteResult.Resource);

            AssertResourceRequestWithEntity(editedResourceRequest, resourceWriteResult.Resource);
        }

        [Fact]
        public async Task Edit_Returns_Error_ResourceLocked()
        {
            var pidUri1 = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            var resourceCreateWriteResult = await CreateResource(pidUri1);
            await CreateLocks(pidUri1);

            var editedResourceRequest = new ResourceRequestDTO() { Properties = resourceCreateWriteResult.Resource.Properties };
            editedResourceRequest.Properties[Graph.Metadata.Constants.Resource.HasLabel] = new List<dynamic> { "this is a very fine edited resource" };

            var resourceWriteResult = await EditResource(resourceCreateWriteResult.Resource.PidUri, editedResourceRequest);

            // Assert
            Assert.Equal(HttpStatusCode.Locked, resourceWriteResult.Response.StatusCode);
        }

        #endregion

        #region PUT - MarkForDeletion 
        [Fact]
        public async Task Put_MarkForDeletion_Returns_MarkedForDeletionMessage()
        {
            var pidUri = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            var resourceDto = new ResourceBuilder().GenerateSampleData().WithPidUri(pidUri, "https://pid.bayer.com/kos/19050#cb849b80-6d00-4c3a-8810-6e4f91ee0cd1").BuildRequestDto();

            await CreateResource(resourceDto);
            await PublishResource(pidUri);
            var markResult = await MarkForDeletionResource(pidUri);
            Assert.Contains("The resource has been marked as deleted.", markResult.Content);

            var resourceResult = await GetResource(pidUri);
            Assert.True(resourceResult.Resource.Properties.ContainsEntryLifecycleStatus(ColidEntryLifecycleStatus.MarkedForDeletion));
            Assert.True(resourceResult.Resource.Properties.ContainsChangeRequester("anonymous@anonymous.com"));
        }

        [Theory]
        [InlineData("superadmin@bayer.com")]
        [InlineData("anonymous@anonymous.com")]
        public async Task Put_MarkForDeletion_Api2Api_Returns_MarkedForDeletionMessage(string requester)
        {
            var pidUri = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            var resourceDto = new ResourceBuilder().GenerateSampleData().WithPidUri(pidUri, "https://pid.bayer.com/kos/19050#cb849b80-6d00-4c3a-8810-6e4f91ee0cd1").BuildRequestDto();

            await CreateResource(_api2ApiClient, resourceDto);
            await PublishResource(_api2ApiClient, pidUri);
            var markResult = await MarkForDeletionResource(_api2ApiClient, pidUri, requester);
            Assert.Contains("The resource has been marked as deleted.", markResult.Content);

            var resourceResult = await GetResource(_api2ApiClient, pidUri);
            Assert.True(resourceResult.Resource.Properties.ContainsEntryLifecycleStatus(ColidEntryLifecycleStatus.MarkedForDeletion));
            Assert.True(resourceResult.Resource.Properties.ContainsChangeRequester(requester));
        }

        [Fact]
        public async Task Put_MarkForDeletion_Error_ResourceIsDraft()
        {
            var pidUri = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            var resourceDto = new ResourceBuilder().GenerateSampleData().WithPidUri(pidUri, "https://pid.bayer.com/kos/19050#cb849b80-6d00-4c3a-8810-6e4f91ee0cd1").BuildRequestDto();

            await CreateResource(resourceDto);
            var markResult = await MarkForDeletionResource(pidUri);
            _output.WriteLine(markResult.Content);
            Assert.Equal(HttpStatusCode.BadRequest, markResult.Response.StatusCode);
        }

        [Fact]
        public async Task Put_MarkForDeletion_Error_ResourceIsDoesNotExist()
        {
            var markResult = await MarkForDeletionResource("http://pid.bayer.com/somegloriousnotexistingpiduri");
            _output.WriteLine(markResult.Content);
            Assert.Equal(HttpStatusCode.NotFound, markResult.Response.StatusCode);
        }

        [Fact]
        public async Task Put_MarkForDeletion_Error_ResourceIsAlreadyMarked()
        {
            var pidUri = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            var resourceDto = new ResourceBuilder().GenerateSampleData().WithPidUri(pidUri, "https://pid.bayer.com/kos/19050#cb849b80-6d00-4c3a-8810-6e4f91ee0cd1").BuildRequestDto();

            await CreateResource(resourceDto);
            await PublishResource(pidUri);
            await MarkForDeletionResource(pidUri);

            var markResultAgain = await MarkForDeletionResource(pidUri);
            _output.WriteLine(markResultAgain.Content);
            Assert.Equal(HttpStatusCode.BadRequest, markResultAgain.Response.StatusCode);
        }

        [Fact]
        public async Task Put_MarkForDeletion_Error_ResourceLocked()
        {
            var pidUri = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            var resourceDto = new ResourceBuilder().GenerateSampleData().WithPidUri(pidUri, "https://pid.bayer.com/kos/19050#cb849b80-6d00-4c3a-8810-6e4f91ee0cd1").BuildRequestDto();

            await CreateResource(resourceDto);
            await PublishResource(pidUri);
            await CreateLocks(pidUri);

            var markResultAgain = await MarkForDeletionResource(pidUri);

            _output.WriteLine(markResultAgain.Content);
            Assert.Equal(HttpStatusCode.Locked, markResultAgain.Response.StatusCode);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("mehh")]
        [InlineData("super.admin@bayer.com")]
        public async Task Put_MarkForDeletion_Error_InvalidRequester(string requester)
        {
            var pidUri = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            var resourceDto = new ResourceBuilder().GenerateSampleData().WithPidUri(pidUri, "https://pid.bayer.com/kos/19050#cb849b80-6d00-4c3a-8810-6e4f91ee0cd1").BuildRequestDto();

            await CreateResource(resourceDto);
            await PublishResource(pidUri);

            var markResultAgain = await MarkForDeletionResource(pidUri, requester);

            _output.WriteLine(markResultAgain.Content);
            Assert.Equal(HttpStatusCode.BadRequest, markResultAgain.Response.StatusCode);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("mehh")]
        [InlineData("brian.pitt@bayer.com")]
        public async Task Put_MarkForDeletion_Api2Api_Error_InvalidRequester(string requester)
        {
            var pidUri = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            var resourceDto = new ResourceBuilder().GenerateSampleData().WithPidUri(pidUri, "https://pid.bayer.com/kos/19050#cb849b80-6d00-4c3a-8810-6e4f91ee0cd1").BuildRequestDto();

            await CreateResource(_api2ApiClient, resourceDto);
            await PublishResource(_api2ApiClient, pidUri);

            var markResultAgain = await MarkForDeletionResource(_api2ApiClient, pidUri, requester);

            _output.WriteLine(markResultAgain.Content);
            Assert.Equal(HttpStatusCode.BadRequest, markResultAgain.Response.StatusCode);
        }

        #endregion

        #region PUT - UnmarkForDeletion 
        [Fact]
        public async Task Put_UnmarkFromDeletion_Returns_MarkedForDeletionMessage()
        {
            var pidUri = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            var resourceDto = new ResourceBuilder().GenerateSampleData().WithPidUri(pidUri, "https://pid.bayer.com/kos/19050#cb849b80-6d00-4c3a-8810-6e4f91ee0cd1").BuildRequestDto();

            await CreateResource(resourceDto);
            await PublishResource(pidUri);
            await MarkForDeletionResource(pidUri);
            var unmarkResult = await UnmarkFromDeletionResource(pidUri);
            Assert.Contains("The resource has been unmarked as deleted.", unmarkResult.Content);

            var resourceResult = await GetResource(pidUri);
            Assert.True(resourceResult.Resource.Properties.ContainsEntryLifecycleStatus(ColidEntryLifecycleStatus.Published));
            Assert.False(resourceResult.Resource.Properties.ContainsKey(Graph.Metadata.Constants.Resource.ChangeRequester));
        }

        [Fact]
        public async Task Put_UnmarkFromDeletion_Error_ResourceIsNotMarketForDeletion()
        {
            var pidUri = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            var resourceDto = new ResourceBuilder().GenerateSampleData().WithPidUri(pidUri, "https://pid.bayer.com/kos/19050#cb849b80-6d00-4c3a-8810-6e4f91ee0cd1").BuildRequestDto();

            await CreateResource(resourceDto);
            await PublishResource(pidUri);
            var unmarkResult = await UnmarkFromDeletionResource(pidUri);
            Assert.Equal(HttpStatusCode.BadRequest, unmarkResult.Response.StatusCode);
        }

        [Fact]
        public async Task Put_UnmarkFromDeletion_Error_ResourceIsDoesNotExist()
        {
            var markResult = await UnmarkFromDeletionResource("http://pid.bayer.com/somegloriousnotexistingpiduri");
            _output.WriteLine(markResult.Content);
            Assert.Equal(HttpStatusCode.NotFound, markResult.Response.StatusCode);
        }

        [Fact]
        public async Task Put_UnmarkFromDeletion_Error_ResourceLocked()
        {
            var pidUri = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            var resourceDto = new ResourceBuilder().GenerateSampleData().WithPidUri(pidUri, "https://pid.bayer.com/kos/19050#cb849b80-6d00-4c3a-8810-6e4f91ee0cd1").BuildRequestDto();

            await CreateResource(resourceDto);
            await PublishResource(pidUri);
            await MarkForDeletionResource(pidUri);
            await CreateLocks(pidUri);

            var unmarkResult = await UnmarkFromDeletionResource(pidUri);

            Assert.Equal(HttpStatusCode.Locked, unmarkResult.Response.StatusCode);
        }
        #endregion

        #region PUT - Multiple UnmarkForDeletion
        [Fact]
        public async Task Put_MultipleUnmarkFromDeletion_Returns_Ok()
        {
            var pidUri1 = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            var pidUri2 = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            await CreateResource(pidUri1);
            await CreateResource(pidUri2);
            await PublishResource(pidUri1);
            await PublishResource(pidUri2);
            await MarkForDeletionResource(pidUri1);
            await MarkForDeletionResource(pidUri2);

            var pidUris = new string[] { pidUri1, pidUri2 };
            var result = await UnmarkFromDeletionResources(pidUris);

            _output.WriteLine(result.Content);
            Assert.Equal(HttpStatusCode.OK, result.Response.StatusCode);

            var resourceResult1 = await GetResource(pidUri1);
            Assert.True(resourceResult1.Resource.Properties.ContainsEntryLifecycleStatus(ColidEntryLifecycleStatus.Published));
            Assert.False(resourceResult1.Resource.Properties.ContainsKey(Graph.Metadata.Constants.Resource.ChangeRequester));

            var resourceResult2 = await GetResource(pidUri2);
            Assert.True(resourceResult2.Resource.Properties.ContainsEntryLifecycleStatus(ColidEntryLifecycleStatus.Published));
            Assert.False(resourceResult2.Resource.Properties.ContainsKey(Graph.Metadata.Constants.Resource.ChangeRequester));
        }

        [Fact]
        public async Task Put_MultipleUnmarkFromDeletion_Error_ResourceLocked()
        {
            var pidUri1 = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            var pidUri2 = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            await CreateResource(pidUri1);
            await CreateResource(pidUri2);
            await PublishResource(pidUri1);
            await PublishResource(pidUri2);
            await MarkForDeletionResource(pidUri1);
            await MarkForDeletionResource(pidUri2);
            await CreateLocks(pidUri1, pidUri2);

            var pidUris = new string[] { pidUri1, pidUri2 };
            var result = await UnmarkFromDeletionResources(pidUris);
            var unmarkFromDeletionResourcesResult = JsonConvert.DeserializeObject<IList<ResourceMarkedOrDeletedResult>>(result.Content);

            _output.WriteLine(result.Content);
            Assert.Equal(HttpStatusCode.OK, result.Response.StatusCode);
            Assert.All(unmarkFromDeletionResourcesResult, deletedResult =>
            {
                Assert.False(deletedResult.Success);
            });
        }

        [Fact]
        public async Task Put_MultipleUnmarkFromDeletion_Error_NotMarkedForDeletion()
        {
            var pidUri1 = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            var pidUri2 = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            await CreateResource(pidUri1);
            await CreateResource(pidUri2);
            await PublishResource(pidUri1);
            await PublishResource(pidUri2);

            var pidUris = new string[] { pidUri1, pidUri2 };
            var result = await UnmarkFromDeletionResources(pidUris);

            _output.WriteLine(result.Content);
            Assert.Equal(HttpStatusCode.OK, result.Response.StatusCode);

            var resultObject = JsonConvert.DeserializeObject<List<ResourceMarkedOrDeletedResult>>(result.Content);
            Assert.Equal(2, resultObject.Count);
            Assert.NotNull(resultObject.FirstOrDefault(e => e.PidUri.AbsoluteUri == pidUri1));
            Assert.NotNull(resultObject.FirstOrDefault(e => e.PidUri.AbsoluteUri == pidUri2));
        }

        [Fact]
        public async Task Put_MultipleUnmarkFromDeletion_Return_TwoFailureForDelete()
        {
            var notExists = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            var draft = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            await CreateResource(draft);
            var published = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            await CreateResource(published);
            await PublishResource(published);
            var publishedAndMarked = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            await CreateResource(publishedAndMarked);
            await PublishResource(publishedAndMarked);
            await MarkForDeletionResource(publishedAndMarked);

            var pidUris = new string[] { notExists, draft, published, publishedAndMarked };
            var result = await UnmarkFromDeletionResources(pidUris);

            _output.WriteLine(result.Content);
            Assert.Equal(HttpStatusCode.OK, result.Response.StatusCode);

            var resultObject = JsonConvert.DeserializeObject<List<ResourceMarkedOrDeletedResult>>(result.Content);
            Assert.Equal(3, resultObject.Count);
            Assert.NotNull(resultObject.FirstOrDefault(e => e.PidUri.AbsoluteUri == notExists));
            Assert.NotNull(resultObject.FirstOrDefault(e => e.PidUri.AbsoluteUri == draft));
            Assert.NotNull(resultObject.FirstOrDefault(e => e.PidUri.AbsoluteUri == published));

            var resourceResult = await GetResource(publishedAndMarked);
            Assert.True(resourceResult.Resource.Properties.ContainsEntryLifecycleStatus(ColidEntryLifecycleStatus.Published));
        }

        #endregion

        #region POST - Comparison
        [Fact]
        public async Task Comparison_CompareTwoResources()
        {
            var idA = "https://pid.bayer.com/kos/19050#d8d8f921-cf1d-43d8-af0e-581a32a43def";
            var idB = "https://pid.bayer.com/kos/19050#fff23414-1b9c-446a-9bbd-28271ba33899";

            var url = $"{_apiPathV3}/comparison?id={WebUtility.UrlEncode(idA)}&id={WebUtility.UrlEncode(idB)}";

            var response = await _client.PostAsync(url, new StringContent(string.Empty));
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            Assert.NotNull(content);
            var result = JsonConvert.DeserializeObject<ResourcesComparisonDto>(content);
            Assert.NotNull(result);
        }
        #endregion

        #region DELETE - Single Resources
        [Fact]
        public async Task Delete_MarkedForDeletionResource_Returns_DeletedMessage()
        {
            var pidUri = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            var resourceDto = new ResourceBuilder().GenerateSampleData().WithPidUri(pidUri, "https://pid.bayer.com/kos/19050#cb849b80-6d00-4c3a-8810-6e4f91ee0cd1").BuildRequestDto();

            await CreateResource(resourceDto);
            await PublishResource(pidUri);
            await MarkForDeletionResource(pidUri);

            var result = await DeleteResource(pidUri);
            Assert.Equal(HttpStatusCode.OK, result.Response.StatusCode);

            var deletedResource = await GetResource(pidUri);
            Assert.Equal(HttpStatusCode.NotFound, deletedResource.Response.StatusCode);
        }

        [Fact]
        public async Task Delete_DraftResource_Returns_DeletedMessage()
        {
            var pidUri = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            var resourceDto = new ResourceBuilder().GenerateSampleData().WithPidUri(pidUri, "https://pid.bayer.com/kos/19050#cb849b80-6d00-4c3a-8810-6e4f91ee0cd1").BuildRequestDto();

            var createResult = await CreateResource(resourceDto);

            var result = await DeleteResource(createResult.Resource.PidUri.ToString());
            Assert.Equal(HttpStatusCode.OK, result.Response.StatusCode);

            var deletedResource = await GetResource(pidUri);
            Assert.Equal(HttpStatusCode.NotFound, deletedResource.Response.StatusCode);
        }

        [Fact]
        public async Task Delete_PublishedResource_Error_ResourceHasToBeMarkedForDeletionFirst()
        {
            var pidUri = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            var resourceDto = new ResourceBuilder().GenerateSampleData().WithPidUri(pidUri, "https://pid.bayer.com/kos/19050#cb849b80-6d00-4c3a-8810-6e4f91ee0cd1").BuildRequestDto();

            await CreateResource(resourceDto);
            await PublishResource(pidUri);

            var result = await DeleteResource(pidUri);
            Assert.Equal(HttpStatusCode.BadRequest, result.Response.StatusCode);
        }

        [Fact]
        public async Task Delete_Error_ResourceIsDoesNotExist()
        {
            var markResult = await DeleteResource("http://pid.bayer.com/somegloriousnotexistingpiduri");
            _output.WriteLine(markResult.Content);
            Assert.Equal(HttpStatusCode.NotFound, markResult.Response.StatusCode);
        }

        [Fact]
        public async Task Delete_PublishedResource_Error_ResourceLocked()
        {
            var pidUri = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            var resourceDto = new ResourceBuilder().GenerateSampleData().WithPidUri(pidUri, "https://pid.bayer.com/kos/19050#cb849b80-6d00-4c3a-8810-6e4f91ee0cd1").BuildRequestDto();

            await CreateResource(resourceDto);
            await PublishResource(pidUri);
            await CreateLocks(pidUri);

            var result = await DeleteResource(pidUri);
            Assert.Equal(HttpStatusCode.Locked, result.Response.StatusCode);
        }

        #endregion

        #region DELETE - Multiple Resources
        [Fact]
        public async Task Delete_MutlipleDraftResources_Returns_Ok()
        {
            var pidUri1 = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            var pidUri2 = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            var pidUri3 = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            await CreateResource(pidUri1);
            await CreateResource(pidUri2);
            await CreateResource(pidUri3);

            var pidUris = new string[] { pidUri1, pidUri2, pidUri3 };
            var result = await DeleteMarkedForDeletionResources(pidUris);
            var resultObject = JsonConvert.DeserializeObject<IList<ResourceMarkedOrDeletedResult>>(result.Content);

            _output.WriteLine(result.Content);
            Assert.Equal(HttpStatusCode.OK, result.Response.StatusCode);
            Assert.All(resultObject, deletedResult =>
            {
                Assert.True(deletedResult.Success);
            });

            var deletedResource1 = await GetResource(pidUri1);
            Assert.Equal(HttpStatusCode.NotFound, deletedResource1.Response.StatusCode);
            var deletedResource2 = await GetResource(pidUri2);
            Assert.Equal(HttpStatusCode.NotFound, deletedResource2.Response.StatusCode);
            var deletedResource3 = await GetResource(pidUri3);
            Assert.Equal(HttpStatusCode.NotFound, deletedResource3.Response.StatusCode);
        }

        [Fact]
        public async Task Delete_MutlipleDraftResources_Errors_ResourceLocked()
        {
            var pidUri1 = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            var pidUri2 = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            var pidUri3 = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            await CreateResource(pidUri1);
            await CreateResource(pidUri2);
            await CreateResource(pidUri3);
            await CreateLocks(pidUri1, pidUri2, pidUri3);

            var pidUris = new string[] { pidUri1, pidUri2, pidUri3 };
            var result = await DeleteMarkedForDeletionResources(pidUris);
            var deleteMarkedForDeletionResourcesResult = JsonConvert.DeserializeObject<IList<ResourceMarkedOrDeletedResult>>(result.Content);

            _output.WriteLine(result.Content);
            Assert.Equal(HttpStatusCode.OK, result.Response.StatusCode);
            Assert.All(deleteMarkedForDeletionResourcesResult, deletedResult =>
            {
                Assert.False(deletedResult.Success);
            });
        }

        [Fact]
        public async Task Delete_MutliplePublishedResources_Returns_Ok()
        {
            var pidUri1 = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            var pidUri2 = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            await CreateResource(pidUri1);
            await CreateResource(pidUri2);
            await PublishResource(pidUri1);
            await PublishResource(pidUri2);
            await MarkForDeletionResource(pidUri1);
            await MarkForDeletionResource(pidUri2);

            var pidUris = new string[] { pidUri1, pidUri2 };
            var result = await DeleteMarkedForDeletionResources(pidUris);
            var deleteMarkedForDeletionResourcesResult = JsonConvert.DeserializeObject<IList<ResourceMarkedOrDeletedResult>>(result.Content);

            _output.WriteLine(result.Content);
            Assert.Equal(HttpStatusCode.OK, result.Response.StatusCode);
            Assert.All(deleteMarkedForDeletionResourcesResult, deletedResult =>
            {
                Assert.True(deletedResult.Success);
            });

            var deletedResource1 = await GetResource(pidUri1);
            Assert.Equal(HttpStatusCode.NotFound, deletedResource1.Response.StatusCode);
            var deletedResource2 = await GetResource(pidUri2);
            Assert.Equal(HttpStatusCode.NotFound, deletedResource1.Response.StatusCode);
        }

        [Fact]
        public async Task Delete_MutliplePublishedResources_Errors_ResourceLocked()
        {
            var pidUri1 = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            var pidUri2 = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            await CreateResource(pidUri1);
            await CreateResource(pidUri2);
            await PublishResource(pidUri1);
            await PublishResource(pidUri2);
            await MarkForDeletionResource(pidUri1);
            await MarkForDeletionResource(pidUri2);
            await CreateLocks(pidUri1, pidUri2);

            var pidUris = new string[] { pidUri1, pidUri2 };
            var result = await DeleteMarkedForDeletionResources(pidUris);
            var deleteMarkedForDeletionResourcesResults = JsonConvert.DeserializeObject<IList<ResourceMarkedOrDeletedResult>>(result.Content);

            _output.WriteLine(result.Content);
            Assert.Equal(HttpStatusCode.OK, result.Response.StatusCode);
            Assert.All(deleteMarkedForDeletionResourcesResults, deletedResult =>
            {
                Assert.False(deletedResult.Success);
            });
        }

        [Fact]
        public async Task Delete_MutliplePublishedResources_Error_NotMarkedForDeletion()
        {
            var pidUri1 = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            var pidUri2 = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            await CreateResource(pidUri1);
            await CreateResource(pidUri2);
            await PublishResource(pidUri1);
            await PublishResource(pidUri2);
            await CreateLocks(pidUri1, pidUri2);

            var pidUris = new string[] { pidUri1, pidUri2 };
            var result = await DeleteMarkedForDeletionResources(pidUris);
            var resourceMarkedOrDeletedResults = JsonConvert.DeserializeObject<IList<ResourceMarkedOrDeletedResult>>(result.Content);

            _output.WriteLine(result.Content);

            Assert.Equal(HttpStatusCode.OK, result.Response.StatusCode);
            Assert.All(resourceMarkedOrDeletedResults, deletedResult =>
            {
                Assert.False(deletedResult.Success);
            });

            var deletedResource1 = await GetResource(pidUri1);
            Assert.Equal(HttpStatusCode.OK, deletedResource1.Response.StatusCode);
            var deletedResource2 = await GetResource(pidUri2);
            Assert.Equal(HttpStatusCode.OK, deletedResource2.Response.StatusCode);
        }

        [Fact]
        public async Task Delete_MutliplePublishedResources_Return_TwoFailureForDelete()
        {
            var notExists = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            var draft = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            await CreateResource(draft);
            var published = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            await CreateResource(published);
            await PublishResource(published);
            var publishedAndMarked = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            await CreateResource(publishedAndMarked);
            await PublishResource(publishedAndMarked);
            await MarkForDeletionResource(publishedAndMarked);

            var pidUris = new string[] { notExists, draft, published, publishedAndMarked };
            var result = await DeleteMarkedForDeletionResources(pidUris);


            _output.WriteLine(result.Content);
            Assert.Equal(HttpStatusCode.OK, result.Response.StatusCode);

            var resultObject = JsonConvert.DeserializeObject<List<ResourceMarkedOrDeletedResult>>(result.Content);
            Assert.Equal(2, resultObject.Count);
            Assert.NotNull(resultObject.FirstOrDefault(e => e.PidUri.AbsoluteUri == notExists));
            Assert.NotNull(resultObject.FirstOrDefault(e => e.PidUri.AbsoluteUri == published));
            Assert.All(resultObject, deletedResult =>
            {
                Assert.False(deletedResult.Success);
            });

            var draftRes = await GetResource(draft);
            Assert.Equal(HttpStatusCode.NotFound, draftRes.Response.StatusCode);
            var publishedAndMarkedRes = await GetResource(publishedAndMarked);
            Assert.Equal(HttpStatusCode.NotFound, publishedAndMarkedRes.Response.StatusCode);
        }
        #endregion

        #region Helper methods to call api

        private Task<ColidResourceResponse> GetResource(string pidUri, string entryLifecycleStatus = "")
        {
            return GetResource(_client, pidUri, entryLifecycleStatus);
        }

        private async Task<ColidResourceResponse> GetResource(HttpClient client, string pidUri, string entryLifecycleStatus = "")
        {
            var url = $"{_apiPathV3}?pidUri={pidUri}";
            if (string.IsNullOrWhiteSpace(entryLifecycleStatus))
            {
                url += $"&lifecycleStatus={entryLifecycleStatus}";
            }

            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            Assert.NotNull(content);
            var result = JsonConvert.DeserializeObject<Resource>(content);
            Assert.NotNull(result);

            if (!response.IsSuccessStatusCode)
            {
                _output.WriteLine(result.ToString());
            }

            return new ColidResourceResponse(response, content, result);
        }

        private async Task<ColidResourceResponse> CreateResource(string pidUri, ColidEntryLifecycleStatus entryLifecycleStatus = ColidEntryLifecycleStatus.Draft)
        {
            var dto = new ResourceBuilder().GenerateSampleData().WithPidUri(pidUri, PIDURI_TEMPLATE)
                .WithEntryLifecycleStatus(entryLifecycleStatus).BuildRequestDto();
            return await CreateResource(dto);
        }

        private Task<ColidResourceResponse> CreateResource(ResourceRequestDTO resourceDto)
        {
            return CreateResource(_client, resourceDto);
        }

        private Task<ColidResourceResponse> CreateResource(HttpClient client, ResourceRequestDTO resourceDto)
        {
            return CreateResource(client, BuildJsonHttpContent(resourceDto));
        }

        private async Task<ColidResourceResponse> CreateResource(HttpClient client, HttpContent httpContent)
        {
            var response = await client.PostAsync(_apiPathV3, httpContent);
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
            var result = JsonConvert.DeserializeObject<ResourceWriteResultCTO>(content);

            _output.WriteLine(JsonConvert.SerializeObject(result));

            return new ColidResourceResponse(response, content, result.Resource, result.ValidationResult);
        }

        private async Task<ColidResourceResponse> EditResource(Uri pidUri, ResourceRequestDTO resourceDto)
        {
            var response = await _client.PutAsync($"{_apiPathV3}?pidUri={pidUri}", BuildJsonHttpContent(resourceDto));
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
            var result = JsonConvert.DeserializeObject<ResourceWriteResultCTO>(content);

            if (!response.IsSuccessStatusCode && result.ValidationResult != null)
            {
                _output.WriteLine(result.ValidationResult.ToString());
            }

            return new ColidResourceResponse(response, content, result.Resource, result.ValidationResult);
        }

        private Task<ColidResourceResponse> PublishResource(string pidUri)
        {
            return PublishResource(_client, pidUri);
        }

        private async Task<ColidResourceResponse> PublishResource(HttpClient client, string pidUri)
        {
            var response = await client.PutAsync($"{_apiPathV3}/publish?pidUri={pidUri}", null);
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
            var result = JsonConvert.DeserializeObject<ResourceWriteResultCTO>(content);
            Assert.True(result.Resource.Properties.ContainsEntryLifecycleStatus(ColidEntryLifecycleStatus.Published));

            return new ColidResourceResponse(response, content, result.Resource, result.ValidationResult);
        }

        private Task<ColidResourceResponse> MarkForDeletionResource(string pidUri, string requester = "anonymous@anonymous.com")
        {
            return MarkForDeletionResource(_client, pidUri, requester);
        }

        private async Task<ColidResourceResponse> MarkForDeletionResource(HttpClient client, string pidUri, string requester = "anonymous@anonymous.com")
        {
            var response = await client.PutAsync($"{_apiPathV3}/markForDeletion?pidUri={pidUri}&requester={requester}", null);
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
            return new ColidResourceResponse(response, content);
        }

        private async Task<ColidResourceResponse> UnmarkFromDeletionResource(string pidUri)
        {
            var response = await _client.PutAsync($"{_apiPathV3}/unmarkFromDeletion?pidUri={pidUri}", null);
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
            return new ColidResourceResponse(response, content);
        }

        private async Task<ColidResourceResponse> UnmarkFromDeletionResources(params string[] pidUri)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, $"{_apiPathV3}/resourceList/reject")
            {
                Content = new StringContent(JsonConvert.SerializeObject(pidUri), Encoding.UTF8, MediaTypeNames.Application.Json),
            };

            var response = await _client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(true);

            return new ColidResourceResponse(response, content);
        }

        private async Task<ColidResourceResponse> DeleteResource(string pidUri)
        {
            var response = await _client.DeleteAsync($"{_apiPathV3}?pidUri={pidUri}");
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
            return new ColidResourceResponse(response, content);

        }

        private async Task<ColidResourceResponse> DeleteMarkedForDeletionResources(params string[] pidUri)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, $"{_apiPathV3}/resourceList")
            {
                Content = new StringContent(JsonConvert.SerializeObject(pidUri), Encoding.UTF8, MediaTypeNames.Application.Json),
            };

            var response = await _client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(true);

            return new ColidResourceResponse(response, content);
        }

        #endregion

        #region Helper methods

        private async Task CreateLocks(params string[] resources)
        {
            foreach (var resource in resources)
            {
                await _distributedLockFactory.CreateLockAsync(resource, TimeSpan.MaxValue);
            }
        }

        private void ValidateGetColidResourceResponse(ColidResourceResponse colidResourceResponse, string pidUri)
        {
            Assert.NotNull(colidResourceResponse);
            Assert.Equal(HttpStatusCode.OK, colidResourceResponse.Response.StatusCode);
            Assert.Null(colidResourceResponse.ValidationResult);
            Assert.NotNull(colidResourceResponse.Resource);
            Assert.Equal(new Uri(pidUri), colidResourceResponse.Resource.PidUri);
        }

        private void ValidateValidColidResourceCreationResponse(ColidResourceResponse colidResourceResponse, string pidUri)
        {
            var validationResults = colidResourceResponse.ValidationResult;
            Assert.NotNull(colidResourceResponse.Resource);
            Assert.NotNull(validationResults);
            Assert.True(validationResults.Conforms);
            Assert.Empty(validationResults.Results);
            Assert.Equal(new Uri(pidUri), colidResourceResponse.Resource.PidUri);
        }

        private void ValidateWarningColidResourceCreationResponse(ColidResourceResponse colidResourceResponse, string pidUri)
        {
            var validationResults = colidResourceResponse.ValidationResult;
            Assert.NotNull(colidResourceResponse.Resource);
            Assert.NotNull(validationResults);
            Assert.False(validationResults.Conforms);
            Assert.Equal(ValidationResultSeverity.Warning, validationResults.Severity);
            Assert.NotEmpty(validationResults.Results);
            Assert.Equal(new Uri(pidUri), colidResourceResponse.Resource.PidUri);
        }

        private void ValidateCriticalColidResourceCreationResponse(ColidResourceResponse colidResourceResponse, string pidUri)
        {
            var validationResults = colidResourceResponse.ValidationResult;
            Assert.Equal(HttpStatusCode.BadRequest, colidResourceResponse.Response.StatusCode);
            Assert.False(validationResults.Conforms);
            Assert.Equal(ValidationResultSeverity.Violation, validationResults.Severity);
            Assert.NotNull(validationResults);
            Assert.NotEmpty(validationResults.Results);
            Assert.NotNull(colidResourceResponse.Resource);
            Assert.Equal(new Uri(pidUri), colidResourceResponse.Resource.PidUri);
        }

        private void AssertResourceRequestWithEntity(EntityBase expectedResource, EntityBase actualResource)
        {
            var actualProperties = actualResource.Properties;
            var expectedProperties = expectedResource.Properties;

            WriteLine("Actual:", actualProperties);
            WriteLine("Expected: ", expectedProperties);

            Assert.NotNull(actualProperties);
            Assert.NotEmpty(actualProperties);

            if (expectedProperties.ContainsKey(Graph.Metadata.Constants.Resource.MetadataGraphConfiguration))
            {
                Assert.Equal(expectedProperties.Count, actualProperties.Count);
            }
            else
            {
                Assert.Equal(expectedProperties.Count + 1, actualProperties.Count);
            }

            var containsActions = actualProperties.GetContainsActions();

            foreach (var property in expectedProperties)
            {
                var key = property.Key;
                if (containsActions.TryGetValue(key, out var action))
                {
                    WriteLine("Key", key);
                    Assert.True(action.Invoke(property.Value.FirstOrDefault()?.ToString()));
                }
            }
        }

        private void WriteLine(string tag, object obj)
        {
            _output.WriteLine($"{tag}: " + JsonConvert.SerializeObject(obj));
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
        #endregion

        private class ColidResourceResponse
        {
            public HttpResponseMessage Response { get; set; }
            public string Content { get; set; }
            public Resource Resource { get; set; }
            public ValidationResult ValidationResult { get; set; }

            public ColidResourceResponse(HttpResponseMessage rsp, string cnt)
            {
                Response = rsp;
                Content = cnt;
            }

            public ColidResourceResponse(HttpResponseMessage rsp, string cnt, Resource res)
            {
                Response = rsp;
                Content = cnt;
                Resource = res;
            }

            public ColidResourceResponse(HttpResponseMessage rsp, string cnt, Resource res, ValidationResult valRes)
            {
                Response = rsp;
                Content = cnt;
                Resource = res;
                ValidationResult = valRes;
            }

        }
    }
}
