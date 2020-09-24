using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using AutoMapper;
using COLID.Cache.Services.Lock;
using COLID.Exception.Models;
using COLID.Exception.Models.Business;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.Metadata.Services;
using COLID.Graph.Tests.Builder;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.MappingProfiles;
using COLID.Graph.TripleStore.Transactions;
using COLID.RegistrationService.Common.DataModel.Resources;
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
using Moq;
using Xunit;

namespace COLID.RegistrationService.Tests.Unit.Services
{
    [ExcludeFromCodeCoverage]
    public class ResourceServiceTests
    {
        private readonly Mock<IAuditTrailLogService> _mockAuditTrailLogService;
        private readonly Mock<ILogger<ResourceService>> _mockLogger;
        private readonly Mock<IResourceRepository> _mockResourceRepo;
        private readonly Mock<IResourceLinkingService> _mockResourceLinkingService;
        private readonly Mock<IResourcePreprocessService> _mockPreProcessService;
        private readonly Mock<IHistoricResourceService> _mockHistoryResourceService;
        private readonly Mock<IMetadataService> _mockMetadataService;
        private readonly Mock<IIdentifierService> _mockIdentifierService;
        private readonly Mock<IUserInfoService> _mockUserInfoService;
        private readonly Mock<IReindexingService> _mockReindexingService;
        private readonly Mock<IRemoteAppDataService> _mockRemoteAppDataService;
        private readonly Mock<IValidationService> _mockValidationService;
        private readonly LockServiceFactory _lockServiceFactory;

        private readonly Random _random = new Random();
        private readonly string _validRequester = "anonymous@anonymous.com";

        private readonly IResourceService _service;
        private readonly IList<MetadataProperty> _metadata;

        public ResourceServiceTests()
        {
            _mockAuditTrailLogService = new Mock<IAuditTrailLogService>();
            _mockLogger = new Mock<ILogger<ResourceService>>();
            _mockResourceRepo = new Mock<IResourceRepository>();
            _mockResourceLinkingService = new Mock<IResourceLinkingService>();
            _mockPreProcessService = new Mock<IResourcePreprocessService>();
            _mockHistoryResourceService = new Mock<IHistoricResourceService>();
            _mockMetadataService = new Mock<IMetadataService>();
            _mockIdentifierService = new Mock<IIdentifierService>();
            _mockUserInfoService = new Mock<IUserInfoService>();
            _mockReindexingService = new Mock<IReindexingService>();
            _mockRemoteAppDataService = new Mock<IRemoteAppDataService>();
            _mockValidationService = new Mock<IValidationService>();

            var lockFactory = new LockFactory();
            _lockServiceFactory = new LockServiceFactory(lockFactory);

            var configuration = new MapperConfiguration(cfg => cfg.AddProfile(new ResourceProfile()));
            var mapper = new Mapper(configuration);

            SetUpRemoteAppDataService();
            SetupResourceRepo();
            SetUpUserInfoService();

            _service = new ResourceService(
                mapper,
                _mockAuditTrailLogService.Object,
                _mockLogger.Object,
                _mockResourceRepo.Object,
                _mockResourceLinkingService.Object,
                _mockPreProcessService.Object,
                _mockHistoryResourceService.Object,
                _mockMetadataService.Object,
                _mockIdentifierService.Object,
                _mockUserInfoService.Object,
                _mockReindexingService.Object,
                _mockRemoteAppDataService.Object,
                _mockValidationService.Object,
                _lockServiceFactory
                );

            _metadata = new MetadataBuilder().GenerateSampleResourceData().Build();
        }

        #region Setup Services
        private void SetupResourceRepo()
        {
            _mockResourceRepo.Setup(mock => mock.CheckIfExist(It.IsAny<Uri>(), It.IsAny<IList<string>>())).Returns(true);

            var mockTransaction = new Mock<ITripleStoreTransaction>();
            _mockResourceRepo.Setup(mock => mock.CreateTransaction()).Returns(mockTransaction.Object);
        }

        private void SetUpRemoteAppDataService()
        {
            var validPersons = new List<string> { "superadmin@bayer.com", "christian.kaubisch.ext@bayer.com", _validRequester };
            _mockRemoteAppDataService
            .Setup(mock => mock.CheckPerson(It.IsIn<string>(validPersons)))
              .Returns(Task.FromResult(true));
            _mockRemoteAppDataService
            .Setup(mock => mock.CheckPerson(It.IsNotIn<string>(validPersons)))
              .Returns(Task.FromResult(false));
        }

        private void SetUpUserInfoService()
        {
            _mockUserInfoService.Setup(mock => mock.GetEmail()).Returns("anonymous@anonymous.com");
            _mockUserInfoService.Setup(mock => mock.HasApiToApiPrivileges()).Returns(false);
        }

        #endregion

        #region Create Resource

        [Fact]
        public async void CreateResource_Success_WithoutPreviousVersion()
        {
            // Arrange
            var requestBuilder = new ResourceBuilder()
                .GenerateSampleData();

            var request = requestBuilder.BuildRequestDto();
            var resource = requestBuilder.Build();

            var resourceCto = new ResourcesCTO(null, null);
            var validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, resourceCto, request.HasPreviousVersion, _metadata, null);

            var result = Task.FromResult(new Tuple<ValidationResult, bool, EntityValidationFacade>(null, false, validationFacade));
            _mockPreProcessService.Setup(t => t.ValidateAndPreProcessResource(It.IsAny<string>(), request, It.IsAny<ResourcesCTO>(), ResourceCrudAction.Create, It.IsAny<bool>(), It.IsAny<string>())).Returns(result);

            // Act
            await _service.CreateResource(request);

            // Assert
            _mockResourceRepo.Verify(t => t.Create(validationFacade.RequestResource, It.IsAny<IList<MetadataProperty>>()), Times.Once);
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

            var resourceCto = new ResourcesCTO(null, null);
            var validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, resourceCto, request.HasPreviousVersion, _metadata, null);

            var preProcessServiceResult = Task.FromResult(new Tuple<ValidationResult, bool, EntityValidationFacade>(null, false, validationFacade));
            _mockPreProcessService.Setup(t => t.ValidateAndPreProcessResource(It.IsAny<string>(), request, It.IsAny<ResourcesCTO>(), ResourceCrudAction.Create, It.IsAny<bool>(), It.IsAny<string>())).Returns(preProcessServiceResult);

            // Act
            var result = await _service.CreateResource(request);

            // Assert
            Assert.NotNull(result);
            _mockResourceRepo.Verify(t => t.Create(validationFacade.RequestResource, It.IsAny<IList<MetadataProperty>>()), Times.Once);
            _mockResourceLinkingService.Verify(t => t.LinkResourceIntoList(validationFacade.RequestResource.PidUri, It.IsAny<Uri>()), Times.Once);
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
            _mockPreProcessService.Setup(t => t.ValidateAndPreProcessResource(It.IsAny<string>(), request, It.IsAny<ResourcesCTO>(), ResourceCrudAction.Create, It.IsAny<bool>(), It.IsAny<string>())).Returns(result);

            // Act
            await Assert.ThrowsAsync<ResourceValidationException>(() => _service.CreateResource(request));
        }

        #endregion

        #region Edit Resource

        [Fact]
        public async void EditResource_Success()
        {
            // Arrange
            var requestBuilder = new ResourceBuilder()
                .GenerateSampleData();

            var request = requestBuilder.BuildRequestDto();

            var resource = requestBuilder.Build();
            var publishedId = Graph.Metadata.Constants.Entity.IdPrefix + Guid.NewGuid();
            resource.Id = publishedId;

            var resourceCto = new ResourcesCTO(null, resource);
            _mockResourceRepo.Setup(s => s.GetResourcesByPidUri(resource.PidUri, It.IsAny<IList<string>>())).Returns(resourceCto);

            var validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, resourceCto, request.HasPreviousVersion, _metadata, null);
            var validationResult = new ValidationResult();
            var preProcessServiceResult = new Tuple<ValidationResult, bool, EntityValidationFacade>(validationResult, false, validationFacade);

            var newResourceId = string.Empty;
            _mockPreProcessService.Setup(t => t.ValidateAndPreProcessResource(It.IsAny<string>(), request, It.IsAny<ResourcesCTO>(), ResourceCrudAction.Update, It.IsAny<bool>(), It.IsAny<string>()))
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

            _mockResourceRepo.Verify(s => s.DeleteDraft(result.Resource.PidUri, new Uri(result.Resource.Id)), Times.Once);
            _mockResourceRepo.Verify(s => s.Create(resource, _metadata), Times.Once);
            _mockResourceRepo.Verify(s => s.CreateLinkingProperty(resource.PidUri, new Uri(
                Graph.Metadata.Constants.Resource.HasPidEntryDraft),
                Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Published,
                Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Draft), Times.Once);
            _mockIdentifierService.Verify(s => s.DeleteAllUnpublishedIdentifiers(It.IsAny<Entity>()), Times.Never);

        }

        [Fact]
        public async void EditResource__Should_ThrowError_ResourceValidation()
        {
            // Arrange
            var requestBuilder = new ResourceBuilder()
                .GenerateSampleData();

            var request = requestBuilder.BuildRequestDto();

            var resource = requestBuilder.Build();
            var publishedId = Graph.Metadata.Constants.Entity.IdPrefix + Guid.NewGuid();
            resource.Id = publishedId;

            var resourceCto = new ResourcesCTO(null, resource);
            _mockResourceRepo.Setup(s => s.GetResourcesByPidUri(resource.PidUri, It.IsAny<IList<string>>())).Returns(resourceCto);

            var validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, resourceCto, request.HasPreviousVersion, _metadata, null);
            var validationResult = new ValidationResult();
            var preProcessServiceResult = new Tuple<ValidationResult, bool, EntityValidationFacade>(validationResult, true, validationFacade);

            var newResourceId = string.Empty;
            _mockPreProcessService.Setup(t => t.ValidateAndPreProcessResource(It.IsAny<string>(), request, It.IsAny<ResourcesCTO>(), ResourceCrudAction.Update, It.IsAny<bool>(), It.IsAny<string>()))
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

        #endregion

        #region Publish Resource

        [Fact]
        public async void PublishResource_Should_ThrowError_Exception_AlreadyPublished()
        {
            // Arrange
            var resource = new ResourceBuilder().GenerateSampleData().Build();
            var resourceCto = new ResourcesCTO(null, resource);

            _mockResourceRepo.Setup(s => s.GetResourcesByPidUri(resource.PidUri, It.IsAny<IList<string>>())).Returns(resourceCto);

            // Act
            await Assert.ThrowsAsync<BusinessException>(() => _service.PublishResource(resource.PidUri));
        }

        #endregion

        #region Mark resource as deleted
        [Fact]
        public async void MarkResourceAsDeleted_Success()
        {
            //Arrange
            var pidUri = GetRandomPidUri();
            var resource = CreateResource(ColidEntryLifecycleStatus.Published);

            _mockResourceRepo.Setup(mock => mock.GetByPidUri(pidUri, It.IsAny<IList<string>>())).Returns(resource);

            // Act
            var result = await _service.MarkResourceAsDeletedAsync(pidUri, _validRequester);

            // Assert
            _mockResourceRepo.Verify(mock => mock.CreateTransaction(), Times.Once);
            _mockResourceRepo.Verify(mock => mock.DeleteAllProperties(new Uri(resource.Id), new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus)), Times.Once);
            _mockResourceRepo.Verify(mock => mock.CreateProperty(new Uri(resource.Id), new Uri(Graph.Metadata.Constants.Resource.ChangeRequester), _validRequester), Times.Once);
            _mockResourceRepo.Verify(mock => mock.CreateProperty(new Uri(resource.Id),
                                new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus),
                                new Uri(Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.MarkedForDeletion)), Times.Once);

            Assert.Equal(RegistrationService.Common.Constants.Messages.Resource.Delete.MarkedDeletedSuccessful, result);
        }

        [Fact]
        public async Task MarkResourceAsDeleted_Should_ThrowException_IfEntryIsDraft()
        {
            //Arrange
            var pidUri = GetRandomPidUri();
            var resource = CreateResource(ColidEntryLifecycleStatus.Draft);

            _mockResourceRepo.Setup(mock => mock.GetByPidUri(pidUri, It.IsAny<IList<string>>())).Returns(resource);

            // Act && Assert
            await Assert.ThrowsAsync<BusinessException>(() => _service.MarkResourceAsDeletedAsync(pidUri, _validRequester));
        }

        [Fact]
        public async Task MarkResourceAsDeleted_Should_ThrowException_IfEntryIsMarkedForDeletion()
        {
            //Arrange
            var pidUri = GetRandomPidUri();
            var resource = CreateResource(ColidEntryLifecycleStatus.MarkedForDeletion);

            _mockResourceRepo.Setup(mock => mock.GetByPidUri(pidUri, It.IsAny<IList<string>>())).Returns(resource);

            // Act && Assert
            await Assert.ThrowsAsync<BusinessException>(() => _service.MarkResourceAsDeletedAsync(pidUri, _validRequester));
        }

        [Fact]
        public void MarkResourceAsDeleted_Should_ThrowException_IfUriDoesNotExists()
        {
            //Arrange
            var pidUri = GetRandomPidUri();

            _mockResourceRepo.Setup(mock => mock.CheckIfExist(pidUri, It.IsAny<IList<string>>())).Returns(false);

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

        #endregion

        #region Unmark resource as deleted
        [Fact]
        public void UnmarkResourceAsDeleted_Success()
        {
            //Arrange
            var pidUri = GetRandomPidUri();
            var resource = CreateResource(ColidEntryLifecycleStatus.MarkedForDeletion);

            _mockResourceRepo.Setup(mock => mock.GetByPidUri(pidUri, It.IsAny<IList<string>>())).Returns(resource);

            // Act
            var result = _service.UnmarkResourceAsDeleted(pidUri);

            // Assert
            _mockResourceRepo.Verify(mock => mock.CreateTransaction(), Times.Once);

            _mockResourceRepo.Verify(mock => mock.DeleteAllProperties(new Uri(resource.Id), new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus)), Times.Once);
            _mockResourceRepo.Verify(mock => mock.DeleteAllProperties(new Uri(resource.Id), new Uri(Graph.Metadata.Constants.Resource.ChangeRequester)), Times.Once);
            _mockResourceRepo.Verify(mock => mock.CreateProperty(new Uri(resource.Id), new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus), new Uri(Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Published)), Times.Once);

            Assert.Equal(RegistrationService.Common.Constants.Messages.Resource.Delete.UnmarkDeletedSuccessful, result);
        }

        [Fact]
        public void UnmarkResourceAsDeleted_Should_ThrowException_IfEntryIsDraft()
        {
            //Arrange
            var pidUri = GetRandomPidUri();
            var resource = CreateResource(ColidEntryLifecycleStatus.Draft);

            _mockResourceRepo.Setup(mock => mock.GetByPidUri(pidUri, It.IsAny<IList<string>>())).Returns(resource);

            // Act && Assert
            Assert.Throws<BusinessException>(() => _service.UnmarkResourceAsDeleted(pidUri));
        }

        [Fact]
        public void UnmarkResourceAsDeleted_Should_ThrowException_IfEntryIsPublished()
        {
            //Arrange
            var pidUri = GetRandomPidUri();
            var resource = CreateResource(ColidEntryLifecycleStatus.Published);

            _mockResourceRepo.Setup(mock => mock.GetByPidUri(pidUri, It.IsAny<IList<string>>())).Returns(resource);

            // Act && Assert
            Assert.Throws<BusinessException>(() => _service.UnmarkResourceAsDeleted(pidUri));
        }

        [Fact]
        public void UnmarkResourceAsDeleted_Should_ThrowException_IfUriDoesNotExists()
        {
            //Arrange
            var pidUri = GetRandomPidUri();

            _mockResourceRepo.Setup(mock => mock.CheckIfExist(pidUri, It.IsAny<IList<string>>())).Returns(false);

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

        #endregion

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
        #endregion
    }
}
