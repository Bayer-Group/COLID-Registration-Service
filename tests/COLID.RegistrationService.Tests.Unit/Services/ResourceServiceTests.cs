using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using AutoMapper;
using COLID.Cache.Services.Lock;
using COLID.Exception.Models;
using COLID.Exception.Models.Business;
using COLID.Graph.Metadata.Constants;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.Metadata.Services;
using COLID.Graph.Tests.Builder;
using COLID.Graph.TripleStore.DataModels.Resources;
using COLID.Graph.TripleStore.MappingProfiles;
using COLID.Graph.TripleStore.Transactions;
using COLID.MessageQueue.Configuration;
using COLID.RegistrationService.Common.DataModel.Resources;
using COLID.RegistrationService.Common.DataModels.LinkHistory;
using COLID.RegistrationService.Common.Enums.ColidEntry;
using COLID.RegistrationService.Repositories.Interface;
using COLID.RegistrationService.Services.Authorization.UserInfo;
using COLID.RegistrationService.Services.Implementation;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.Services.Validation.Exceptions;
using COLID.RegistrationService.Services.Validation.Models;
using COLID.RegistrationService.Tests.Common.Builder;
using COLID.StatisticsLog.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using static COLID.Graph.Metadata.Constants.Resource;
using Entity = COLID.Graph.TripleStore.DataModels.Base.Entity;
using Resource = COLID.Graph.Metadata.DataModels.Resources.Resource;

namespace COLID.RegistrationService.Tests.Unit.Services
{
    [ExcludeFromCodeCoverage]
    public class ResourceServiceTests
    {
        private readonly Mock<IOptionsMonitor<ColidMessageQueueOptions>> _messageQueuingOptionsAccessor;
        private readonly Mock<IAuditTrailLogService> _mockAuditTrailLogService;
        private readonly Mock<ILogger<ResourceService>> _mockLogger;
        private readonly Mock<IResourceRepository> _mockResourceRepo;
        private readonly Mock<IResourceLinkingService> _mockResourceLinkingService;
        private readonly Mock<IResourcePreprocessService> _mockPreProcessService;

        //private readonly Mock<IHistoricResourceService> _mockHistoryResourceService;
        private readonly Mock<IMetadataService> _mockMetadataService;

        private readonly Mock<IIdentifierService> _mockIdentifierService;
        private readonly Mock<IUserInfoService> _mockUserInfoService;
        private readonly Mock<IReindexingService> _mockReindexingService;
        private readonly Mock<IRemoteAppDataService> _mockRemoteAppDataService;
        private readonly Mock<IValidationService> _mockValidationService;
        private readonly Mock<IRevisionService> _mockRevisionService;
        private readonly Mock<IAttachmentService> _mockAttachmentService;
        private readonly Mock<IGraphManagementService> _mockGraphManagementService;
        private readonly LockServiceFactory _lockServiceFactory;

        private readonly Random _random = new Random();
        private readonly string _validRequester = "anonymous@anonymous.com";

        private readonly IResourceService _service;
        private readonly IList<MetadataProperty> _metadata;
        private IList<VersionOverviewCTO> _resourceVersionList;

        private readonly Uri _resourceGraph;
        private readonly Uri _draftResourceGraph;
        private readonly Uri _linkHistoryGraph;
        private readonly Uri _historicResourecGraph;
        private readonly Uri _consumerGroupGraph;

        public ResourceServiceTests()
        {
            _resourceGraph = new Uri("https://pid.bayer.com/resource/2.0");
            _draftResourceGraph = new Uri("https://pid.bayer.com/resource/2.0/Draft");
            _linkHistoryGraph = new Uri("https://pid.bayer.com/linkhistory");
            // _historicResourecGraph = new Uri("https://pid.bayer.com/resource/historic");
            _consumerGroupGraph = new Uri("https://pid.bayer.com/consumergroup/1.0");
            _resourceVersionList = new List<VersionOverviewCTO>();

            _messageQueuingOptionsAccessor = new Mock<IOptionsMonitor<ColidMessageQueueOptions>>();
            _mockAuditTrailLogService = new Mock<IAuditTrailLogService>();
            _mockLogger = new Mock<ILogger<ResourceService>>();
            _mockResourceRepo = new Mock<IResourceRepository>();
            _mockResourceLinkingService = new Mock<IResourceLinkingService>();
            _mockPreProcessService = new Mock<IResourcePreprocessService>();
            //_mockHistoryResourceService = new Mock<IHistoricResourceService>();
            _mockMetadataService = new Mock<IMetadataService>();
            _mockIdentifierService = new Mock<IIdentifierService>();
            _mockUserInfoService = new Mock<IUserInfoService>();
            _mockReindexingService = new Mock<IReindexingService>();
            _mockRemoteAppDataService = new Mock<IRemoteAppDataService>();
            _mockValidationService = new Mock<IValidationService>();
            _mockRevisionService = new Mock<IRevisionService>();
            _mockAttachmentService = new Mock<IAttachmentService>();
            _mockGraphManagementService = new Mock<IGraphManagementService>();

            var lockFactory = new InMemoryLockFactory();
            _lockServiceFactory = new LockServiceFactory(lockFactory);

            var configuration = new MapperConfiguration(cfg => cfg.AddProfile(new ResourceProfile()));
            var mapper = new Mapper(configuration);

            SetupMetadataService();
            SetUpRemoteAppDataService();
            SetupResourceRepo();
            SetUpUserInfoService();
            SetUpResourceLinkingService();

            _service = new ResourceService(
                _messageQueuingOptionsAccessor.Object,
                mapper,
                _mockAuditTrailLogService.Object,
                _mockLogger.Object,
                _mockResourceRepo.Object,
                _mockResourceLinkingService.Object,
                _mockPreProcessService.Object,
                //_mockHistoryResourceService.Object,
                _mockMetadataService.Object,
                _mockIdentifierService.Object,
                _mockUserInfoService.Object,
                _mockReindexingService.Object,
                _mockRemoteAppDataService.Object,
                _mockValidationService.Object,
                _lockServiceFactory,
                _mockRevisionService.Object,
                _mockAttachmentService.Object,
                _mockGraphManagementService.Object
                );

            _metadata = new MetadataBuilder().GenerateSampleResourceData().Build();
        }

        #region Setup Services

        private void SetupMetadataService()
        {
            _mockMetadataService.Setup(mock => mock.GetInstanceGraph(PIDO.PidConcept)).Returns(_resourceGraph);
            _mockMetadataService.Setup(mock => mock.GetInstanceGraph("draft")).Returns(_draftResourceGraph);
            _mockMetadataService.Setup(mock => mock.GetInstanceGraph("linkHistory")).Returns(_linkHistoryGraph);
            _mockMetadataService.Setup(mock => mock.GetHistoricInstanceGraph()).Returns(_historicResourecGraph);
            _mockMetadataService.Setup(mock => mock.GetInstanceGraph(ConsumerGroup.Type)).Returns(_consumerGroupGraph);
        }

        private void SetupResourceRepo()
        {
            _mockResourceRepo.Setup(mock => mock.CheckIfExist(It.IsAny<Uri>(), It.IsAny<IList<string>>(), _resourceGraph)).Returns(true);

            var mockTransaction = new Mock<ITripleStoreTransaction>();
            _mockResourceRepo.Setup(mock => mock.CreateTransaction()).Returns(mockTransaction.Object);
        }

        private void SetUpRemoteAppDataService()
        {
            var validPersons = new List<string> { "superadmin@bayer.com", "christian.kaubisch.ext@bayer.com", _validRequester };
            _mockRemoteAppDataService
            .Setup(mock => mock.CheckPerson(It.IsIn<string>(validPersons)))
              .Returns(It.IsAny<bool>());
            _mockRemoteAppDataService
            .Setup(mock => mock.CheckPerson(It.IsNotIn<string>(validPersons)))
              .Returns(It.IsAny<bool>());
        }

        private void SetUpUserInfoService()
        {
            _mockUserInfoService.Setup(mock => mock.GetEmail()).Returns("anonymous@anonymous.com");
            _mockUserInfoService.Setup(mock => mock.HasApiToApiPrivileges()).Returns(false);
        }

        private void SetUpResourceLinkingService()
        {
            _mockResourceLinkingService.Setup(mock => mock.LinkResourceIntoList(It.IsAny<Uri>(), It.IsAny<Uri>(), out _resourceVersionList)).Returns(string.Empty);
        }

        #endregion Setup Services

        #region Create Resource

        [Fact]
        public async void CreateResource_Success_WithoutPreviousVersion()
        {
            // Arrange
            var requestBuilder = new ResourceBuilder()
                .GenerateSampleData();

            var request = requestBuilder.BuildRequestDto();
            var resource = requestBuilder.Build();

            var resourceCto = new ResourcesCTO(null, null, new List<VersionOverviewCTO>());
            var validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, resourceCto, request.HasPreviousVersion, _metadata, null);

            var result = Task.FromResult(new Tuple<ValidationResult, bool, EntityValidationFacade>(null, false, validationFacade));
            _mockPreProcessService.Setup(t => t.ValidateAndPreProcessResource(It.IsAny<string>(), request, It.IsAny<ResourcesCTO>(), ResourceCrudAction.Create, It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(result);

            // Act
            await _service.CreateResource(request);

            // Assert
            _mockResourceRepo.Verify(t => t.Create(validationFacade.RequestResource, It.IsAny<IList<MetadataProperty>>(), _draftResourceGraph), Times.Once);
            _mockResourceLinkingService.Verify(t => t.LinkResourceIntoList(validationFacade.RequestResource.PidUri, It.IsAny<Uri>()), Times.Never);
        }

        [Fact]
        public async void CreateResource_Success_WithPreviousVersion()
        {
            // Arrange
            var requestBuilder = new ResourceBuilder()
                .GenerateSampleData();

            var request = requestBuilder.BuildRequestDto();
            var resource = requestBuilder.Build();

            request.HasPreviousVersion = Graph.Metadata.Constants.Entity.IdPrefix + Guid.NewGuid();

            var resourceCto = new ResourcesCTO(null, null, new List<VersionOverviewCTO>());
            var validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, resourceCto, request.HasPreviousVersion, _metadata, null);

            var preProcessServiceResult = Task.FromResult(new Tuple<ValidationResult, bool, EntityValidationFacade>(null, false, validationFacade));
            _mockPreProcessService.Setup(t => t.ValidateAndPreProcessResource(It.IsAny<string>(), request, It.IsAny<ResourcesCTO>(), ResourceCrudAction.Create, It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(preProcessServiceResult);

            // Act
            var result = await _service.CreateResource(request);

            // Assert
            Assert.NotNull(result);
            _mockResourceRepo.Verify(t => t.Create(validationFacade.RequestResource, It.IsAny<IList<MetadataProperty>>(), _draftResourceGraph), Times.Once);
            _mockResourceLinkingService.Verify(t => t.LinkResourceIntoList(validationFacade.RequestResource.PidUri, It.IsAny<Uri>(), out _resourceVersionList), Times.Once);
        }

        [Fact]
        public async void CreateResource_Should_ThrowError_ValidationFailed()
        {
            // Arrange
            var requestBuilder = new ResourceBuilder()
                .GenerateSampleData();

            var request = requestBuilder.BuildRequestDto();
            var resource = requestBuilder.Build();

            var entityValidationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);
            var result = Task.FromResult(new Tuple<ValidationResult, bool, EntityValidationFacade>(null, true, entityValidationFacade));
            _mockPreProcessService.Setup(t => t.ValidateAndPreProcessResource(It.IsAny<string>(), request, It.IsAny<ResourcesCTO>(), ResourceCrudAction.Create, It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(result);

            // Act
            await Assert.ThrowsAsync<ResourceValidationException>(() => _service.CreateResource(request));
        }

        #endregion Create Resource

        #region Edit Resource

        [Fact]
        public async void EditResource_Success()
        {
            // Arrange
            var requestBuilder = new ResourceBuilder()
                .GenerateSampleData();
            var requestBuilder2 = new ResourceBuilder()
                .GenerateSampleData();

            var request = requestBuilder.BuildRequestDto();
            var resource = requestBuilder2.Build();
            var publishedId = Graph.Metadata.Constants.Entity.IdPrefix + Guid.NewGuid();
            resource.Id = publishedId;

            request.Properties.Remove(Graph.Metadata.Constants.EnterpriseCore.PidUri);
            resource.Properties.TryGetValue(Graph.Metadata.Constants.EnterpriseCore.PidUri, out List<dynamic> value2);
            request.Properties.Add(Graph.Metadata.Constants.EnterpriseCore.PidUri, value2);
            request.Properties.TryGetValue(Graph.Metadata.Constants.Resource.Keyword, out List<dynamic> label);
            label[0] = "this is a change keyword";

            var resourceCto = new ResourcesCTO(null, resource, new List<VersionOverviewCTO>());
            _mockResourceRepo.Setup(s => s.GetResourcesByPidUri(resource.PidUri, It.IsAny<IList<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(resourceCto);

            var validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, resourceCto, request.HasPreviousVersion, _metadata, null);
            var validationResult = new ValidationResult();
            var preProcessServiceResult = new Tuple<ValidationResult, bool, EntityValidationFacade>(validationResult, false, validationFacade);

            var newResourceId = string.Empty;
            _mockMetadataService.Setup(mock => mock.GetMetadataForEntityType(It.IsAny<string>())).Returns(_metadata);
            _mockPreProcessService.Setup(t => t.ValidateAndPreProcessResource(It.IsAny<string>(), request, It.IsAny<ResourcesCTO>(), ResourceCrudAction.Update, It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(preProcessServiceResult)
                .Callback<string, ResourceRequestDTO, ResourcesCTO, ResourceCrudAction, bool, string>((a, b, c, d, e, f) =>
                {
                    newResourceId = a;
                    validationResult.Results.Add(new ValidationResultProperty(a, "some path", "some value", "some message", ValidationResultSeverity.Warning));
                });

            // Act
            var result = await _service.EditResource(resource.PidUri, request);

            // Assert
            Assert.All(result.ValidationResult.Results, t =>
            {
                Assert.Equal(t.Node, newResourceId);
            });

            _mockResourceRepo.Verify(s => s.DeleteDraft(result.Resource.PidUri, new Uri(result.Resource.Id), It.IsAny<Uri>()), Times.Once);
            _mockResourceRepo.Verify(s => s.Create(It.IsAny<Resource>(), _metadata, It.IsAny<Uri>()), Times.Once);

            _mockIdentifierService.Verify(s => s.DeleteAllUnpublishedIdentifiers(It.IsAny<Entity>()), Times.Never);

            Assert.Equal(result.Resource.Id, resource.Id);
        }

        [Fact]
        public async void EditResource__Should_ThrowError_ResourceValidation()
        {
            // Arrange
            var requestBuilder = new ResourceBuilder()
                .GenerateSampleData();
            var requestBuilder2 = new ResourceBuilder()
                .GenerateSampleData();

            var request = requestBuilder.BuildRequestDto();
            var resource = requestBuilder2.Build();
            var publishedId = Graph.Metadata.Constants.Entity.IdPrefix + Guid.NewGuid();
            resource.Id = publishedId;

            request.Properties.Remove(Graph.Metadata.Constants.EnterpriseCore.PidUri);
            resource.Properties.TryGetValue(Graph.Metadata.Constants.EnterpriseCore.PidUri, out List<dynamic> value2);
            request.Properties.Add(Graph.Metadata.Constants.EnterpriseCore.PidUri, value2);
            request.Properties.TryGetValue(Graph.Metadata.Constants.Resource.Keyword, out List<dynamic> lable);
            lable[0] = "this is a change keyword";

            var resourceCto = new ResourcesCTO(null, resource, new List<VersionOverviewCTO>());

            _mockMetadataService.Setup(mock => mock.GetMetadataForEntityType(It.IsAny<string>())).Returns(_metadata);
            _mockResourceRepo.Setup(s => s.GetResourcesByPidUri(resource.PidUri, It.IsAny<IList<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(resourceCto);

            var validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, resourceCto, request.HasPreviousVersion, _metadata, null);
            var validationResult = new ValidationResult();
            var preProcessServiceResult = new Tuple<ValidationResult, bool, EntityValidationFacade>(validationResult, true, validationFacade);

            var newResourceId = string.Empty;
            _mockPreProcessService.Setup(t => t.ValidateAndPreProcessResource(It.IsAny<string>(), request, It.IsAny<ResourcesCTO>(), ResourceCrudAction.Update, It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(preProcessServiceResult)
                .Callback<string, ResourceRequestDTO, ResourcesCTO, ResourceCrudAction, bool, string>((a, b, c, d, e, f) =>
                {
                    newResourceId = a;
                    validationResult.Results.Add(new ValidationResultProperty(a, "some path", "some value", "some message", ValidationResultSeverity.Warning));
                });

            // Act
            var result = await Assert.ThrowsAsync<ResourceValidationException>(() => _service.EditResource(resource.PidUri, request));

            // Assert
            _mockResourceRepo.Verify(s => s.CreateTransaction(), Times.Never);
            Assert.All(result.ValidationResult.Results, t => Assert.Equal(t.Node, publishedId));
        }

        [Fact]
        public async void EditResource__Should_ThrowError_ResourceNotChangedException()
        {
            // Arrange
            var requestBuilder = new ResourceBuilder()
                .GenerateSampleData();

            var request = requestBuilder.BuildRequestDto();
            var resource = requestBuilder.Build();
            var publishedId = Graph.Metadata.Constants.Entity.IdPrefix + Guid.NewGuid();
            resource.Id = publishedId;

            var resourceCto = new ResourcesCTO(null, resource, new List<VersionOverviewCTO>());

            _mockMetadataService.Setup(mock => mock.GetMetadataForEntityType(It.IsAny<string>())).Returns(_metadata);
            _mockResourceRepo.Setup(s => s.GetResourcesByPidUri(resource.PidUri, It.IsAny<IList<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(resourceCto);

            var validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, resourceCto, request.HasPreviousVersion, _metadata, null);
            var validationResult = new ValidationResult();
            var preProcessServiceResult = new Tuple<ValidationResult, bool, EntityValidationFacade>(validationResult, true, validationFacade);

            var newResourceId = string.Empty;
            _mockPreProcessService.Setup(t => t.ValidateAndPreProcessResource(It.IsAny<string>(), request, It.IsAny<ResourcesCTO>(), ResourceCrudAction.Update, It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(preProcessServiceResult)
                .Callback<string, ResourceRequestDTO, ResourcesCTO, ResourceCrudAction, bool, string>((a, b, c, d, e, f) =>
                {
                    newResourceId = a;
                    validationResult.Results.Add(new ValidationResultProperty(a, "some path", "some value", "some message", ValidationResultSeverity.Warning));
                });

            // Act
            var result = await Assert.ThrowsAsync<BusinessException>(() => _service.EditResource(resource.PidUri, request));

            // Assert
            _mockResourceRepo.Verify(s => s.CreateTransaction(), Times.Never);
        }

        #endregion Edit Resource

        #region Publish Resource

        [Fact]
        public async void PublishResource_FirstPublish_Success()
        {
            // Arrange
            var resource = new ResourceBuilder().GenerateSampleData().Build();
            var resourcerequest = new ResourceBuilder().GenerateSampleData().BuildRequestDto();
            var resourceCto = new ResourcesCTO(resource, null, new List<VersionOverviewCTO>());

            _mockMetadataService.Setup(mock => mock.GetMetadataForEntityType(It.IsAny<string>())).Returns(_metadata);
            _mockResourceRepo.Setup(s => s.GetResourcesByPidUri(resource.PidUri, It.IsAny<IList<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(resourceCto);
            _mockResourceRepo.Setup(s => s.GetOutboundLinksOfPublishedResource(resource.PidUri, It.IsAny<Uri>(), It.IsAny<ISet<string>>())).Returns(new Dictionary<string, List<LinkingMapping>>());
            
            var validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, resourceCto, null, _metadata, null);
            var validationResult = new ValidationResult();
            var preProcessServiceResult = new Tuple<ValidationResult, bool, EntityValidationFacade>(validationResult, false, validationFacade);

            var newResourceId = string.Empty;
            _mockMetadataService.Setup(mock => mock.GetMetadataForEntityType(It.IsAny<string>())).Returns(_metadata);
            _mockPreProcessService.Setup(t => t.ValidateAndPreProcessResource(resource.Id, It.IsAny<ResourceRequestDTO>(), It.IsAny<ResourcesCTO>(), ResourceCrudAction.Publish, It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(preProcessServiceResult)
                .Callback<string, ResourceRequestDTO, ResourcesCTO, ResourceCrudAction, bool, string>((a, b, c, d, e, f) =>
                {
                    newResourceId = a;
                    validationResult.Results.Add(new ValidationResultProperty(a, "some path", "some value", "some message", ValidationResultSeverity.Warning));
                });

            // Act
            _service.PublishResource(resource.PidUri);

            //Assert
            _mockResourceRepo.Verify(s => s.DeleteDraft(validationFacade.RequestResource.PidUri, new Uri(validationFacade.RequestResource.Id), _draftResourceGraph), Times.Once);
            _mockResourceRepo.Verify(s => s.DeletePublished(validationFacade.RequestResource.PidUri, new Uri(validationFacade.RequestResource.Id), _resourceGraph), Times.Once);
            _mockIdentifierService.Verify(s => s.DeleteAllUnpublishedIdentifiers(resource), Times.Once);

            //Called Methods if resource is published for the first time
            _mockRevisionService.Verify(s => s.AddAdditionalsAndRemovals(It.IsAny<Resource>(), resource), Times.Never);

            _mockRevisionService.Verify(s => s.InitializeResourceInAdditionalsGraph(validationFacade.RequestResource, _metadata), Times.Once);
            _mockResourceRepo.Verify(s => s.Create(It.IsAny<Resource>(), _metadata, _resourceGraph), Times.Once);
        }

        [Fact]
        public async void PublishResource_SecondPublish_Success()
        {
            // Arrange
            var resource = new ResourceBuilder().GenerateSampleData().Build();
            var publishedResource = CreateResource(ColidEntryLifecycleStatus.Published);
            var resourcerequest = new ResourceBuilder().GenerateSampleData().BuildRequestDto();
            var resourceCto = new ResourcesCTO(resource, publishedResource, new List<VersionOverviewCTO>());

            _mockMetadataService.Setup(mock => mock.GetMetadataForEntityType(It.IsAny<string>())).Returns(_metadata);
            _mockResourceRepo.Setup(s => s.GetResourcesByPidUri(resource.PidUri, It.IsAny<IList<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(resourceCto);
            var validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, resourceCto, null, _metadata, null);
            var validationResult = new ValidationResult();
            var preProcessServiceResult = new Tuple<ValidationResult, bool, EntityValidationFacade>(validationResult, false, validationFacade);

            var newResourceId = string.Empty;
            _mockMetadataService.Setup(mock => mock.GetMetadataForEntityType(It.IsAny<string>())).Returns(_metadata);
            _mockRevisionService.Setup(mock => mock.AddAdditionalsAndRemovals(It.IsAny<Resource>(), It.IsAny<Resource>())).Returns(Task.FromResult(publishedResource));
            _mockPreProcessService.Setup(t => t.ValidateAndPreProcessResource(resource.Id, It.IsAny<ResourceRequestDTO>(), It.IsAny<ResourcesCTO>(), ResourceCrudAction.Publish, It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(preProcessServiceResult)
                .Callback<string, ResourceRequestDTO, ResourcesCTO, ResourceCrudAction, bool, string>((a, b, c, d, e, f) =>
                {
                    newResourceId = a;
                    validationResult.Results.Add(new ValidationResultProperty(a, "some path", "some value", "some message", ValidationResultSeverity.Warning));
                });
            _mockResourceRepo.Setup(s => s.GetOutboundLinksOfPublishedResource(It.IsAny<Uri>(), It.IsAny<Uri>(), It.IsAny<HashSet<string>>())).Returns(new Dictionary<string, List<LinkingMapping>>());

            // Act
            _service.PublishResource(resource.PidUri);

            //Assert
            _mockResourceRepo.Verify(s => s.DeleteDraft(validationFacade.RequestResource.PidUri, new Uri(validationFacade.RequestResource.Id), _draftResourceGraph), Times.Once);
            _mockResourceRepo.Verify(s => s.DeletePublished(validationFacade.RequestResource.PidUri, new Uri(validationFacade.RequestResource.Id), _resourceGraph), Times.Once);
            _mockIdentifierService.Verify(s => s.DeleteAllUnpublishedIdentifiers(resource), Times.Once);

            //Called Methods if resource is published for the first time
            _mockRevisionService.Verify(s => s.AddAdditionalsAndRemovals(publishedResource, resource), Times.Once);
            _mockResourceRepo.Verify(s => s.Create(It.IsAny<Resource>(), _metadata, _resourceGraph), Times.Once);

            _mockRevisionService.Verify(s => s.InitializeResourceInAdditionalsGraph(validationFacade.RequestResource, _metadata), Times.Never);
            _mockResourceRepo.Verify(s => s.Create(validationFacade.RequestResource, _metadata, _resourceGraph), Times.Never);
        }

        [Fact]
        public async void PublishResource_Should_ThrowError_Exception_AlreadyPublished()
        {
            // Arrange
            var resource = new ResourceBuilder().GenerateSampleData().Build();
            var resourceCto = new ResourcesCTO(null, resource, new List<VersionOverviewCTO>());

            _mockResourceRepo.Setup(s => s.GetResourcesByPidUri(resource.PidUri, It.IsAny<IList<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(resourceCto);
            _mockMetadataService.Setup(mock => mock.GetMetadataForEntityType(It.IsAny<string>())).Returns(_metadata);

            // Act
            await Assert.ThrowsAsync<BusinessException>(() => _service.PublishResource(resource.PidUri));
        }

        #endregion Publish Resource

        #region Mark resource as deleted

        [Fact]
        public async void MarkResourceAsDeleted_Success()
        {
            //Arrange
            var pidUri = GetRandomPidUri();
            var resource = CreateResource(ColidEntryLifecycleStatus.Published);
            var resources = new ResourcesCTO(null, resource, new List<VersionOverviewCTO>());

            _mockResourceRepo.Setup(mock => mock.GetResourcesByPidUri(pidUri, It.IsAny<IList<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(resources);
            _mockResourceRepo.Setup(mock => mock.GetByPidUri(pidUri, It.IsAny<IList<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(resource);

            _mockRemoteAppDataService.Setup(mock => mock.CheckPerson(It.IsAny<string>())).Returns(true);

            // Act
            var result = await _service.MarkResourceAsDeletedAsync(pidUri, _validRequester);

            // Assert
            _mockResourceRepo.Verify(mock => mock.CreateTransaction(), Times.Once);
            _mockResourceRepo.Verify(mock => mock.DeleteAllProperties(new Uri(resource.Id), new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus), _resourceGraph), Times.Once);
            _mockResourceRepo.Verify(mock => mock.CreateProperty(new Uri(resource.Id), new Uri(Graph.Metadata.Constants.Resource.ChangeRequester), _validRequester, _resourceGraph), Times.Once);
            _mockResourceRepo.Verify(mock => mock.CreateProperty(new Uri(resource.Id),
                                new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus),
                                new Uri(Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.MarkedForDeletion), _resourceGraph), Times.Once);

            Assert.Equal(RegistrationService.Common.Constants.Messages.Resource.Delete.MarkedDeletedSuccessful, result);
        }

        [Fact]
        public async Task MarkResourceAsDeleted_Should_ThrowException_IfEntryIsDraft()
        {
            //Arrange
            var pidUri = GetRandomPidUri();
            var resource = CreateResource(ColidEntryLifecycleStatus.Draft);

            _mockResourceRepo.Setup(mock => mock.GetByPidUri(pidUri, It.IsAny<IList<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(resource);

            // Act && Assert
            await Assert.ThrowsAsync<BusinessException>(() => _service.MarkResourceAsDeletedAsync(pidUri, _validRequester));
        }

        [Fact]
        public async Task MarkResourceAsDeleted_Should_ThrowException_IfEntryIsMarkedForDeletion()
        {
            //Arrange
            var pidUri = GetRandomPidUri();
            var resource = CreateResource(ColidEntryLifecycleStatus.MarkedForDeletion);

            _mockResourceRepo.Setup(mock => mock.GetByPidUri(pidUri, It.IsAny<IList<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(resource);

            // Act && Assert
            await Assert.ThrowsAsync<BusinessException>(() => _service.MarkResourceAsDeletedAsync(pidUri, _validRequester));
        }

        [Fact]
        public void MarkResourceAsDeleted_Should_ThrowException_IfUriDoesNotExists()
        {
            //Arrange
            var pidUri = GetRandomPidUri();

            _mockResourceRepo.Setup(mock => mock.CheckIfExist(pidUri, It.IsAny<IList<string>>(), _resourceGraph)).Returns(false);

            // Act && Assert
            Assert.ThrowsAsync<EntityNotFoundException>(() => _service.MarkResourceAsDeletedAsync(pidUri, _validRequester));
        }

        [Fact]
        public async Task MarkResourceAsDeleted_Should_ThrowException_IfEntityIsAlreadyLocked()
        {
            //Arrange
            var pidUri = GetRandomPidUri();

            var lockService = _lockServiceFactory.CreateLockService();
            lockService.CreateLock(pidUri.ToString());

            // Act && Assert
            await Assert.ThrowsAsync<Cache.Exceptions.ResourceLockedException>(() => _service.MarkResourceAsDeletedAsync(pidUri, _validRequester));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task MarkResourceAsDeleted_Should_ThrowException_IfEmailIsNull(string requester)
        {
            //Arrange
            var pidUri = GetRandomPidUri();

            // Act && Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.MarkResourceAsDeletedAsync(pidUri, requester));
        }

        [Theory]
        [InlineData("meehhh")]
        [InlineData("peter@fox@bayer.com")]
        public async Task MarkResourceAsDeleted_Should_ThrowException_IfEmailIsInvalid(string requester)
        {
            //Arrange
            var pidUri = GetRandomPidUri();

            // Act && Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.MarkResourceAsDeletedAsync(pidUri, requester));
        }

        [Theory]
        [InlineData("superadmin@bayer.com", false)]
        [InlineData("peter@bayer.com", false)]
        [InlineData("peter@bayer.com", true)]
        public async Task MarkResourceAsDeleted_Should_ThrowException_IfRequesterIsInvalid(string requester, bool api2ApiUser)
        {
            //Arrange
            var pidUri = GetRandomPidUri();
            _mockUserInfoService.Setup(mock => mock.HasApiToApiPrivileges()).Returns(api2ApiUser);

            // Act && Assert
            await Assert.ThrowsAsync<BusinessException>(() => _service.MarkResourceAsDeletedAsync(pidUri, requester));
        }

        #endregion Mark resource as deleted

        #region Unmark resource as deleted

        [Fact]
        public void UnmarkResourceAsDeleted_Success()
        {
            //Arrange
            var pidUri = GetRandomPidUri();
            var resource = CreateResource(ColidEntryLifecycleStatus.MarkedForDeletion);
            var resources = new ResourcesCTO(null, resource, new List<VersionOverviewCTO>());

            _mockResourceRepo.Setup(mock => mock.GetResourcesByPidUri(pidUri, It.IsAny<IList<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(resources);
            _mockResourceRepo.Setup(mock => mock.GetByPidUri(pidUri, It.IsAny<IList<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(resource);
            _mockMetadataService.Setup(mock => mock.GetMetadataForEntityType(It.IsAny<string>())).Returns(_metadata);

            // Act
            var result = _service.UnmarkResourceAsDeleted(pidUri);

            // Assert
            _mockResourceRepo.Verify(mock => mock.CreateTransaction(), Times.Once);

            _mockResourceRepo.Verify(mock => mock.DeleteAllProperties(new Uri(resource.Id), new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus), _resourceGraph), Times.Once);
            _mockResourceRepo.Verify(mock => mock.DeleteAllProperties(new Uri(resource.Id), new Uri(Graph.Metadata.Constants.Resource.ChangeRequester), _resourceGraph), Times.Once);
            _mockResourceRepo.Verify(mock => mock.CreateProperty(new Uri(resource.Id), new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus), new Uri(Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Published), _resourceGraph), Times.Once);

            Assert.Equal(RegistrationService.Common.Constants.Messages.Resource.Delete.UnmarkDeletedSuccessful, result);
        }

        [Fact]
        public void UnmarkResourceAsDeleted_Should_ThrowException_IfEntryIsDraft()
        {
            //Arrange
            var pidUri = GetRandomPidUri();
            var resource = CreateResource(ColidEntryLifecycleStatus.Draft);

            _mockResourceRepo.Setup(mock => mock.GetByPidUri(pidUri, It.IsAny<IList<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(resource);
            _mockMetadataService.Setup(mock => mock.GetMetadataForEntityType(It.IsAny<string>())).Returns(_metadata);

            // Act && Assert
            Assert.Throws<BusinessException>(() => _service.UnmarkResourceAsDeleted(pidUri));
        }

        [Fact]
        public void UnmarkResourceAsDeleted_Should_ThrowException_IfEntryIsPublished()
        {
            //Arrange
            var pidUri = GetRandomPidUri();
            var resource = CreateResource(ColidEntryLifecycleStatus.Published);

            _mockResourceRepo.Setup(mock => mock.GetByPidUri(pidUri, It.IsAny<IList<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(resource);
            _mockMetadataService.Setup(mock => mock.GetMetadataForEntityType(It.IsAny<string>())).Returns(_metadata);

            // Act && Assert
            Assert.Throws<BusinessException>(() => _service.UnmarkResourceAsDeleted(pidUri));
        }

        [Fact]
        public void UnmarkResourceAsDeleted_Should_ThrowException_IfUriDoesNotExists()
        {
            //Arrange
            var pidUri = GetRandomPidUri();

            _mockResourceRepo.Setup(mock => mock.CheckIfExist(pidUri, It.IsAny<IList<string>>(), _resourceGraph)).Returns(false);

            // Act && Assert
            Assert.Throws<EntityNotFoundException>(() => _service.UnmarkResourceAsDeleted(pidUri));
        }

        [Fact]
        public void UnmarkResourceAsDeleted_Should_ThrowException_IfEntityIsAlreadyLocked()
        {
            //Arrange
            var pidUri = GetRandomPidUri();

            var lockService = _lockServiceFactory.CreateLockService();
            lockService.CreateLock(pidUri.ToString());

            // Act && Assert
            Assert.Throws<Cache.Exceptions.ResourceLockedException>(() => _service.UnmarkResourceAsDeleted(pidUri));
        }

        #endregion Unmark resource as deleted

        #region Link Methods

        [Fact]
        public async void AddResourceLink_Success()
        {
            // Arrange
            var linkType = COLID.Graph.Metadata.Constants.Resource.LinkTypes.IsCopyOfDataset;
            //ParentResource
            var ParentResource = CreateResource(ColidEntryLifecycleStatus.Published);
            var ParentResourceCto = new ResourcesCTO(null, ParentResource, new List<VersionOverviewCTO>());
            _mockResourceRepo.Setup(s => s.GetResourcesByPidUri(ParentResource.PidUri, It.IsAny<IList<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(ParentResourceCto);
            //LinkedResource
            var LinkResource = CreateResource(ColidEntryLifecycleStatus.Published);
            var LinkResourceCto = new ResourcesCTO(null, LinkResource, new List<VersionOverviewCTO>());
            _mockResourceRepo.Setup(s => s.GetResourcesByPidUri(LinkResource.PidUri, It.IsAny<IList<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(LinkResourceCto);
            //helper mockups
            _mockMetadataService.Setup(mock => mock.GetMetadataForEntityType(It.IsAny<string>())).Returns(_metadata);
            _mockResourceRepo.Setup(mock => mock.GetOutboundLinksOfPublishedResource(ParentResource.PidUri, _resourceGraph, It.IsAny<ISet<string>>())).Returns(new Dictionary<string, List<LinkingMapping>>());

            _mockRemoteAppDataService.Setup(mock => mock.CheckPerson(It.IsAny<string>())).Returns(true);

            // Act
            var resultParent = await _service.AddResourceLink(ParentResource.PidUri.ToString(), linkType, LinkResource.PidUri.ToString(), _validRequester);
            // Assert
            _mockResourceRepo.Verify(s => s.CreateTransaction(), Times.Once);
            _mockResourceRepo.Verify(t => t.GetOutboundLinksOfPublishedResource(ParentResource.PidUri, _resourceGraph, It.IsAny<ISet<string>>()), Times.Once);
            _mockResourceRepo.Verify(t => t.CreateLinkPropertyWithGivenPid(new Uri(ParentResource.Id), new Uri(linkType), LinkResource.PidUri.ToString(), _resourceGraph), Times.Once);
            _mockResourceRepo.Verify(t => t.CreateLinkHistoryEntry(It.IsAny<LinkHistoryCreateDto>(), _linkHistoryGraph, _resourceGraph), Times.Once);
            Assert.Equal(resultParent.PidUri, ParentResource.PidUri);
            Assert.True(resultParent.Links.ContainsKey(linkType));
            Assert.Equal(resultParent.Links[linkType].Count, 1);
            Assert.Equal(resultParent.Links[linkType][0].LinkType, LinkType.outbound);
        }

        [Fact]
        public async void AddResourceLink_Adding_Not_Allowed_Link_Throws_BusinessException()
        {
            // Arrange
            var linkType = "https://pid.bayer.com/kos/19050/someFancyLink";
            //ParentResource
            var ParentResource = CreateResource(ColidEntryLifecycleStatus.Published);
            var ParentResourceCto = new ResourcesCTO(null, ParentResource, new List<VersionOverviewCTO>());
            _mockResourceRepo.Setup(s => s.GetResourcesByPidUri(ParentResource.PidUri, It.IsAny<IList<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(ParentResourceCto);
            //LinkedResource
            var LinkResource = CreateResource(ColidEntryLifecycleStatus.Published);
            var LinkResourceCto = new ResourcesCTO(null, LinkResource, new List<VersionOverviewCTO>());
            _mockResourceRepo.Setup(s => s.GetResourcesByPidUri(LinkResource.PidUri, It.IsAny<IList<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(LinkResourceCto);
            //helper mockups
            _mockMetadataService.Setup(mock => mock.GetMetadataForEntityType(It.IsAny<string>())).Returns(_metadata);
            _mockResourceRepo.Setup(mock => mock.GetOutboundLinksOfPublishedResource(ParentResource.PidUri, _resourceGraph, It.IsAny<ISet<string>>())).Returns(new Dictionary<string, List<LinkingMapping>>());

            // Act
            Assert.ThrowsAsync<BusinessException>(() => _service.AddResourceLink(ParentResource.PidUri.ToString(), linkType, LinkResource.PidUri.ToString(), _validRequester));
        }

        [Fact]
        public async void AddResourceLink_Linking_Same_Resource_Throws_BusinessException()
        {
            // Arrange
            var linkType = COLID.Graph.Metadata.Constants.Resource.LinkTypes.IsCopyOfDataset;
            //ParentResource
            var ParentResource = CreateResource(ColidEntryLifecycleStatus.Published);
            var ParentResourceCto = new ResourcesCTO(null, ParentResource, new List<VersionOverviewCTO>());
            _mockResourceRepo.Setup(s => s.GetResourcesByPidUri(ParentResource.PidUri, It.IsAny<IList<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(ParentResourceCto);

            //helper mockups
            _mockMetadataService.Setup(mock => mock.GetMetadataForEntityType(It.IsAny<string>())).Returns(_metadata);
            _mockResourceRepo.Setup(mock => mock.GetOutboundLinksOfPublishedResource(ParentResource.PidUri, _resourceGraph, It.IsAny<ISet<string>>())).Returns(new Dictionary<string, List<LinkingMapping>>());

            // Act
            Assert.ThrowsAsync<BusinessException>(() => _service.AddResourceLink(ParentResource.PidUri.ToString(), linkType, ParentResource.PidUri.ToString(), _validRequester));
        }

        [Fact]
        public async void AddResourceLink_InvalidRequester_ThrowsBusinessException()
        {
            // Arrange
            var linkType = COLID.Graph.Metadata.Constants.Resource.LinkTypes.IsCopyOfDataset;
            //ParentResource
            var ParentResource = CreateResource(ColidEntryLifecycleStatus.Published);
            var ParentResourceCto = new ResourcesCTO(null, ParentResource, new List<VersionOverviewCTO>());
            _mockResourceRepo.Setup(s => s.GetResourcesByPidUri(ParentResource.PidUri, It.IsAny<IList<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(ParentResourceCto);
            //LinkedResource
            var LinkResource = CreateResource(ColidEntryLifecycleStatus.Published);
            var LinkResourceCto = new ResourcesCTO(null, LinkResource, new List<VersionOverviewCTO>());
            _mockResourceRepo.Setup(s => s.GetResourcesByPidUri(LinkResource.PidUri, It.IsAny<IList<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(LinkResourceCto);
            //helper mockups
            _mockMetadataService.Setup(mock => mock.GetMetadataForEntityType(It.IsAny<string>())).Returns(_metadata);

            // Act
            Assert.ThrowsAsync<BusinessException>(() => _service.AddResourceLink(ParentResource.PidUri.ToString(), linkType, LinkResource.PidUri.ToString(), "invalidUser@bayer.com"));
        }

        [Fact]
        public async void AddResourceLink_For_Not_Existing_Resources_ThrowsEntityNotFoundException()
        {
            // Arrange
            var linkType = COLID.Graph.Metadata.Constants.Resource.LinkTypes.DerivedFromDataset;
            var ParentResourcePidUri = "https://pid.bayer.com/fancyxxx-xxxx-xxxx-xxxx-resourcexxxx/";
            var LinkResource = CreateResource(ColidEntryLifecycleStatus.Published);

            // Act
            Assert.ThrowsAsync<EntityNotFoundException>(() => _service.AddResourceLink(ParentResourcePidUri, linkType, LinkResource.PidUri.ToString(), _validRequester));
        }

        [Fact]
        public async void RemoveResourceLink_Success()
        {
            // Arrange
            var linkType = COLID.Graph.Metadata.Constants.Resource.LinkTypes.IsCopyOfDataset;
            //LinkedResource
            var linkResource = CreateResource(ColidEntryLifecycleStatus.Published);
            var linkResourceCto = new ResourcesCTO(null, linkResource, new List<VersionOverviewCTO>());
            _mockResourceRepo.Setup(s => s.GetResourcesByPidUri(linkResource.PidUri, It.IsAny<IList<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(linkResourceCto);

            //ParentResource
            var parentResource = new ResourceBuilder()
                .GenerateSampleData()
                .WithEntryLifecycleStatus(ColidEntryLifecycleStatus.Published)
                .WithCopyOfDataset(linkResource.PidUri.ToString())
                .Build();
            var parentResourceCto = new ResourcesCTO(null, parentResource, new List<VersionOverviewCTO>());
            _mockResourceRepo.Setup(s => s.GetResourcesByPidUri(parentResource.PidUri, It.IsAny<IList<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(parentResourceCto);

            var linkList = new Dictionary<string, List<LinkingMapping>>();
            var linkingMappingList = new List<LinkingMapping>();
            linkingMappingList.Add(new LinkingMapping(LinkType.outbound, linkResource.PidUri.ToString()));
            linkList.Add(linkType, linkingMappingList);

            //helper mockups
            _mockMetadataService.Setup(mock => mock.GetMetadataForEntityType(It.IsAny<string>())).Returns(_metadata);
            _mockResourceRepo.Setup(mock => mock.GetOutboundLinksOfPublishedResource(parentResource.PidUri, _resourceGraph, It.IsAny<ISet<string>>())).Returns(linkList);
            
            _mockRemoteAppDataService.Setup(mock => mock.CheckPerson(It.IsAny<string>())).Returns(true);

            // Act
            var resultParent = await _service.RemoveResourceLink(parentResource.PidUri.ToString(), linkType, linkResource.PidUri.ToString(), false, _validRequester);
            // Assert
            _mockResourceRepo.Verify(s => s.CreateTransaction(), Times.Once);
            _mockResourceRepo.Verify(t => t.GetOutboundLinksOfPublishedResource(parentResource.PidUri, _resourceGraph, It.IsAny<ISet<string>>()), Times.Once);
            _mockResourceRepo.Verify(t => t.DeleteLinkPropertyWithGivenPid(new Uri(parentResource.Id), new Uri(linkType), linkResource.PidUri.ToString(), _resourceGraph), Times.Once);
            // link history tests
            _mockResourceRepo.Verify(t => t.GetLinkHistoryRecord(new Uri(parentResource.Id), new Uri(linkType), linkResource.PidUri, _linkHistoryGraph, _resourceGraph), Times.Once);
            _mockResourceRepo.Verify(t => t.DeleteAllProperties(It.IsAny<Uri>(), new Uri(LinkHistory.HasLinkStatus), _linkHistoryGraph), Times.Once);
            _mockResourceRepo.Verify(t => t.CreateProperty(It.IsAny<Uri>(), new Uri(LinkHistory.HasLinkStatus), new Uri(LinkHistory.LinkStatus.Deleted), _linkHistoryGraph), Times.Exactly(1));
            _mockResourceRepo.Verify(t => t.CreateProperty(It.IsAny<Uri>(), new Uri(LinkHistory.DeletedBy), It.IsAny<string>(), _linkHistoryGraph), Times.Exactly(1));
            _mockResourceRepo.Verify(t => t.CreateProperty(It.IsAny<Uri>(), new Uri(LinkHistory.DateDeleted), It.IsAny<string>(), _linkHistoryGraph), Times.Exactly(1));

            Assert.Equal(resultParent.PidUri, parentResource.PidUri);
            Assert.False(resultParent.Links.ContainsKey(linkType));
        }

        [Fact]
        public async void RemoveResourceLink_GetInboundResourceBack_Success()
        {
            // Arrange
            var linkType = COLID.Graph.Metadata.Constants.Resource.LinkTypes.IsCopyOfDataset;
            //LinkedResource
            var linkResource = CreateResource(ColidEntryLifecycleStatus.Published);
            var linkResourceCto = new ResourcesCTO(null, linkResource, new List<VersionOverviewCTO>());
            _mockResourceRepo.Setup(s => s.GetResourcesByPidUri(linkResource.PidUri, It.IsAny<IList<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(linkResourceCto);
            _mockResourceRepo.Setup(s => s.GetByPidUri(linkResource.PidUri, It.IsAny<IList<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(linkResource);

            //ParentResource
            var parentResource = new ResourceBuilder()
                .GenerateSampleData()
                .WithEntryLifecycleStatus(ColidEntryLifecycleStatus.Published)
                .WithCopyOfDataset(linkResource.PidUri.ToString())
                .Build();
            var parentResourceCto = new ResourcesCTO(null, parentResource, new List<VersionOverviewCTO>());
            _mockResourceRepo.Setup(s => s.GetResourcesByPidUri(parentResource.PidUri, It.IsAny<IList<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(parentResourceCto);

            var linkList = new Dictionary<string, List<LinkingMapping>>();
            var linkingMappingList = new List<LinkingMapping>();
            linkingMappingList.Add(new LinkingMapping(LinkType.outbound, linkResource.PidUri.ToString()));
            linkList.Add(linkType, linkingMappingList);

            //helper mockups
            _mockMetadataService.Setup(mock => mock.GetMetadataForEntityType(It.IsAny<string>())).Returns(_metadata);
            _mockResourceRepo.Setup(mock => mock.GetOutboundLinksOfPublishedResource(parentResource.PidUri, _resourceGraph, It.IsAny<ISet<string>>())).Returns(linkList);

            _mockRemoteAppDataService.Setup(mock => mock.CheckPerson(It.IsAny<string>())).Returns(true);

            // Act
            var resultParent = await _service.RemoveResourceLink(parentResource.PidUri.ToString(), linkType, linkResource.PidUri.ToString(), true, _validRequester);
            // Assert
            _mockResourceRepo.Verify(s => s.CreateTransaction(), Times.Once);
            _mockResourceRepo.Verify(t => t.GetOutboundLinksOfPublishedResource(parentResource.PidUri, _resourceGraph, It.IsAny<ISet<string>>()), Times.Once);
            _mockResourceRepo.Verify(t => t.DeleteLinkPropertyWithGivenPid(new Uri(parentResource.Id), new Uri(linkType), linkResource.PidUri.ToString(), _resourceGraph), Times.Once);
            // link history tests
            _mockResourceRepo.Verify(t => t.GetLinkHistoryRecord(new Uri(parentResource.Id), new Uri(linkType), linkResource.PidUri, _linkHistoryGraph, _resourceGraph), Times.Once);
            _mockResourceRepo.Verify(t => t.DeleteAllProperties(It.IsAny<Uri>(), new Uri(LinkHistory.HasLinkStatus), _linkHistoryGraph), Times.Once);
            _mockResourceRepo.Verify(t => t.CreateProperty(It.IsAny<Uri>(), new Uri(LinkHistory.HasLinkStatus), new Uri(LinkHistory.LinkStatus.Deleted), _linkHistoryGraph), Times.Exactly(1));
            _mockResourceRepo.Verify(t => t.CreateProperty(It.IsAny<Uri>(), new Uri(LinkHistory.DeletedBy), It.IsAny<string>(), _linkHistoryGraph), Times.Exactly(1));
            _mockResourceRepo.Verify(t => t.CreateProperty(It.IsAny<Uri>(), new Uri(LinkHistory.DateDeleted), It.IsAny<string>(), _linkHistoryGraph), Times.Exactly(1));

            Assert.Equal(resultParent.PidUri, linkResource.PidUri);
        }

        [Fact]
        public async void RemoveResourceLink_Adding_Not_Allowed_Link_Throws_BusinessException()
        {
            // Arrange
            var linkType = "https://pid.bayer.com/kos/19050/someFancyLink";
            //ParentResource
            var parentResource = CreateResource(ColidEntryLifecycleStatus.Published);
            var parentResourceCto = new ResourcesCTO(null, parentResource, new List<VersionOverviewCTO>());
            _mockResourceRepo.Setup(s => s.GetResourcesByPidUri(parentResource.PidUri, It.IsAny<IList<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(parentResourceCto);
            //LinkedResource
            var linkResource = CreateResource(ColidEntryLifecycleStatus.Published);
            var linkResourceCto = new ResourcesCTO(null, linkResource, new List<VersionOverviewCTO>());
            _mockResourceRepo.Setup(s => s.GetResourcesByPidUri(linkResource.PidUri, It.IsAny<IList<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(linkResourceCto);
            //helper mockups
            _mockMetadataService.Setup(mock => mock.GetMetadataForEntityType(It.IsAny<string>())).Returns(_metadata);
            _mockResourceRepo.Setup(mock => mock.GetOutboundLinksOfPublishedResource(parentResource.PidUri, _resourceGraph, It.IsAny<ISet<string>>())).Returns(new Dictionary<string, List<LinkingMapping>>());

            // Act
            Assert.ThrowsAsync<BusinessException>(() => _service.RemoveResourceLink(parentResource.PidUri.ToString(), linkType, linkResource.PidUri.ToString(), false, _validRequester));
        }

        [Fact]
        public async void RemoveResourceLink_Linking_Same_Resource_Throws_BusinessException()
        {
            // Arrange
            var linkType = COLID.Graph.Metadata.Constants.Resource.LinkTypes.IsCopyOfDataset;
            //ParentResource
            var parentResource = CreateResource(ColidEntryLifecycleStatus.Published);
            var parentResourceCto = new ResourcesCTO(null, parentResource, new List<VersionOverviewCTO>());
            _mockResourceRepo.Setup(s => s.GetResourcesByPidUri(parentResource.PidUri, It.IsAny<IList<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(parentResourceCto);

            //helper mockups
            _mockMetadataService.Setup(mock => mock.GetMetadataForEntityType(It.IsAny<string>())).Returns(_metadata);
            _mockResourceRepo.Setup(mock => mock.GetOutboundLinksOfPublishedResource(parentResource.PidUri, _resourceGraph, It.IsAny<ISet<string>>())).Returns(new Dictionary<string, List<LinkingMapping>>());

            // Act
            Assert.ThrowsAsync<BusinessException>(() => _service.RemoveResourceLink(parentResource.PidUri.ToString(), linkType, parentResource.PidUri.ToString(), false, _validRequester));
        }

        [Fact]
        public async void RemoveResourceLink_InvalidRequester_ThrowsBusinessException()
        {
            // Arrange
            var linkType = COLID.Graph.Metadata.Constants.Resource.LinkTypes.IsCopyOfDataset;
            //ParentResource
            var parentResource = CreateResource(ColidEntryLifecycleStatus.Published);
            var parentResourceCto = new ResourcesCTO(null, parentResource, new List<VersionOverviewCTO>());
            _mockResourceRepo.Setup(s => s.GetResourcesByPidUri(parentResource.PidUri, It.IsAny<IList<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(parentResourceCto);
            //LinkedResource
            var linkResource = CreateResource(ColidEntryLifecycleStatus.Published);
            var linkResourceCto = new ResourcesCTO(null, linkResource, new List<VersionOverviewCTO>());
            _mockResourceRepo.Setup(s => s.GetResourcesByPidUri(linkResource.PidUri, It.IsAny<IList<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(linkResourceCto);
            //helper mockups
            _mockMetadataService.Setup(mock => mock.GetMetadataForEntityType(It.IsAny<string>())).Returns(_metadata);

            // Act
            Assert.ThrowsAsync<BusinessException>(() => _service.RemoveResourceLink(parentResource.PidUri.ToString(), linkType, linkResource.PidUri.ToString(), false, _validRequester));
        }

        [Fact]
        public async void RemoveResourceLink_For_Not_Existing_Resources_ThrowsEntityNotFoundException()
        {
            // Arrange
            var linkType = COLID.Graph.Metadata.Constants.Resource.LinkTypes.DerivedFromDataset;
            var parentResourcePidUri = "https://pid.bayer.com/fancyxxx-xxxx-xxxx-xxxx-resourcexxxx/";
            var linkResource = CreateResource(ColidEntryLifecycleStatus.Published);

            // Act
            Assert.ThrowsAsync<BusinessException>(() => _service.RemoveResourceLink(parentResourcePidUri, linkType, linkResource.PidUri.ToString(), false, _validRequester));
        }

        #endregion Link Methods

        #region Helper

        private Resource CreateResource(ColidEntryLifecycleStatus colidEntryLifecycleStatus)
        {
            return new ResourceBuilder()
                .GenerateSampleData()
                .WithEntryLifecycleStatus(colidEntryLifecycleStatus)
                .Build();
        }

        private Uri GetRandomPidUri()
        {
            var pidUri = $"https://pid.bayer.com/constraint/c{_random.Next(0, 9999999).ToString("D7")}";
            return new Uri(pidUri);
        }

        #endregion Helper
    }
}
