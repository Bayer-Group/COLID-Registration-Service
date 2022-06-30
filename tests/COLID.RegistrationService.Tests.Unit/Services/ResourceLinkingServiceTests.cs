using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AutoMapper;
using COLID.Cache.Services.Lock;
using COLID.Exception.Models;
using COLID.RegistrationService.Common.Constants;
using COLID.Graph.Metadata.Constants;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.Metadata.Services;
using COLID.Graph.Tests.Builder;
using COLID.Graph.TripleStore.MappingProfiles;
using COLID.Graph.TripleStore.Transactions;
using COLID.RegistrationService.Common.Enums.ColidEntry;
using COLID.RegistrationService.Repositories.Interface;
using COLID.RegistrationService.Services.Implementation;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.Tests.Common.Builder;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Resource = COLID.Graph.Metadata.DataModels.Resources.Resource;

namespace COLID.RegistrationService.Tests.Unit.Services
{
    [ExcludeFromCodeCoverage]
    public class ResourceLinkingServiceTests
    {
        private readonly Mock<ILogger<ResourceLinkingService>> _mockLogger;
        private readonly Mock<IResourceRepository> _mockResourceRepo;
        private readonly Mock<IMetadataService> _mockMetadataService;
        private readonly Mock<IReindexingService> _mockReindexingService;

        private readonly Random _random = new Random();
        private readonly string _validRequester = "anonymous@anonymous.com";

        private readonly IResourceLinkingService _service;
        private readonly IList<MetadataProperty> _metadata;
        private IList<VersionOverviewCTO> _resourceVersionList;

        private readonly Uri _resourceGraph;
        private readonly Uri _draftResourceGraph;
        private readonly Uri _linkHistoryGraph;
        private readonly Uri _consumerGroupGraph;

        public ResourceLinkingServiceTests()
        {
            _resourceGraph = new Uri("https://pid.bayer.com/resource/2.0");
            _draftResourceGraph = new Uri("https://pid.bayer.com/resource/2.0/Draft");
            _linkHistoryGraph = new Uri("https://pid.bayer.com/resource/2.0/LinkHistory");
            _consumerGroupGraph = new Uri("https://pid.bayer.com/consumergroup/1.0");
            _resourceVersionList = new List<VersionOverviewCTO>();

            _mockLogger = new Mock<ILogger<ResourceLinkingService>>();
            _mockResourceRepo = new Mock<IResourceRepository>();
            _mockMetadataService = new Mock<IMetadataService>();
            _mockReindexingService = new Mock<IReindexingService>();

            var lockFactory = new InMemoryLockFactory();

            var configuration = new MapperConfiguration(cfg => cfg.AddProfile(new ResourceProfile()));
            var mapper = new Mapper(configuration);

            SetupMetadataService();
            SetupResourceRepo();

            _service = new ResourceLinkingService(
                _mockLogger.Object,
                _mockResourceRepo.Object,
                _mockReindexingService.Object,
                _mockMetadataService.Object
                );

            _metadata = new MetadataBuilder().GenerateSampleResourceData().Build();
        }

        #region Setup Services

        private void SetupMetadataService()
        {
            _mockMetadataService.Setup(mock => mock.GetInstanceGraph(PIDO.PidConcept)).Returns(_resourceGraph);
            _mockMetadataService.Setup(mock => mock.GetInstanceGraph("draft")).Returns(_draftResourceGraph);
            _mockMetadataService.Setup(mock => mock.GetInstanceGraph("linkHistory")).Returns(_linkHistoryGraph);
            _mockMetadataService.Setup(mock => mock.GetInstanceGraph(ConsumerGroup.Type)).Returns(_consumerGroupGraph);
            _mockMetadataService.Setup(mock => mock.GetInstanceGraph(ConsumerGroup.Type)).Returns(_consumerGroupGraph);
            _mockMetadataService.Setup(mock => mock.GetInstantiableEntityTypes(Graph.Metadata.Constants.Resource.Type.FirstResouceType)).Returns(new List<string>());
        }

        private void SetupResourceRepo()
        {
            _mockResourceRepo.Setup(mock => mock.CheckIfExist(It.IsAny<Uri>(), It.IsAny<IList<string>>(), _resourceGraph)).Returns(true);

            var mockTransaction = new Mock<ITripleStoreTransaction>();
            _mockResourceRepo.Setup(mock => mock.CreateTransaction()).Returns(mockTransaction.Object);
        }

        #endregion Setup Services

        [Fact]
        public async void Link_Upper_Version_To_Smaller_Version_Success()
        {
            var resource_ver1 = CreateResourceWithVersion(ColidEntryLifecycleStatus.Published,"1");
            var versionList = new List<VersionOverviewCTO>();
            versionList.Add(new VersionOverviewCTO() { Id = resource_ver1.Id, Version = "1",PidUri = resource_ver1.PidUri.ToString(), PublishedVersion = resource_ver1.PublishedVersion });
            resource_ver1.Versions = versionList;
            var resource_ver2 = CreateResourceWithVersion(ColidEntryLifecycleStatus.Published,"2");
            
            
            _mockResourceRepo.Setup(t => t.GetMainResourceByPidUri(resource_ver1.PidUri, It.IsAny<List<string>>(), It.IsAny<Dictionary<Uri,bool>>())).Returns(resource_ver1);
            _mockResourceRepo.Setup(t => t.CheckIfExist(resource_ver1.PidUri, It.IsAny<List<string>>(), _resourceGraph)).Returns(true);
            _mockResourceRepo.Setup(t => t.CheckIfExist(resource_ver1.PidUri, It.IsAny<List<string>>(), _draftResourceGraph)).Returns(false);
            _mockResourceRepo.Setup(t => t.GetIdByPidUri(resource_ver1.PidUri, It.IsAny<ISet<Uri>>())).Returns(new Uri(resource_ver1.Id));

            _mockResourceRepo.Setup(t => t.GetMainResourceByPidUri(resource_ver2.PidUri, It.IsAny<List<string>>(), It.IsAny<Dictionary<Uri,bool>>())).Returns(resource_ver2);
            _mockResourceRepo.Setup(t => t.GetResourcesByPidUri(resource_ver2.PidUri, It.IsAny<List<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(new ResourcesCTO(null, resource_ver1, new List<VersionOverviewCTO>()));
            _mockResourceRepo.Setup(t => t.CheckIfExist(resource_ver2.PidUri, It.IsAny<List<string>>(), _resourceGraph)).Returns(true);
            _mockResourceRepo.Setup(t => t.CheckIfExist(resource_ver2.PidUri, It.IsAny<List<string>>(), _draftResourceGraph)).Returns(false);
            _mockResourceRepo.Setup(t => t.GetIdByPidUri(resource_ver2.PidUri, It.IsAny<ISet<Uri>>())).Returns(new Uri(resource_ver2.Id));

            // Act
             var result = _service.LinkResourceIntoList(resource_ver2.PidUri, resource_ver1.PidUri, out IList<VersionOverviewCTO> versions);

            // Assert
            _mockResourceRepo.Verify(t => t.CreateLinkingProperty(resource_ver1.PidUri, new Uri(Graph.Metadata.Constants.Resource.HasLaterVersion), new Uri(resource_ver2.Id), _draftResourceGraph), Times.Once);
            _mockResourceRepo.Verify(t => t.CreateLinkingProperty(resource_ver1.PidUri, new Uri(Graph.Metadata.Constants.Resource.HasLaterVersion), new Uri(resource_ver2.Id), _resourceGraph), Times.Once);
            Assert.Equal(result, COLID.RegistrationService.Common.Constants.Messages.Resource.Linking.LinkSuccessful);
        }

        [Fact]
        public async void Link_Smaller_Version_To_Upper_Resource_Success()
        {
            var resourceA = CreateResourceWithVersion(ColidEntryLifecycleStatus.Published, "2");
            var versionList = new List<VersionOverviewCTO>();
            versionList.Add(new VersionOverviewCTO() { Id = resourceA.Id, Version = "2", PidUri = resourceA.PidUri.ToString(), PublishedVersion = resourceA.PublishedVersion });
            resourceA.Versions = versionList;
            var resourceB = CreateResourceWithVersion(ColidEntryLifecycleStatus.Published, "1");


            _mockResourceRepo.Setup(t => t.GetMainResourceByPidUri(resourceA.PidUri, It.IsAny<List<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(resourceA);
            _mockResourceRepo.Setup(t => t.CheckIfExist(resourceA.PidUri, It.IsAny<List<string>>(), _resourceGraph)).Returns(true);
            _mockResourceRepo.Setup(t => t.CheckIfExist(resourceA.PidUri, It.IsAny<List<string>>(), _draftResourceGraph)).Returns(false);
            _mockResourceRepo.Setup(t => t.GetIdByPidUri(resourceA.PidUri, It.IsAny<ISet<Uri>>())).Returns(new Uri(resourceA.Id));

            _mockResourceRepo.Setup(t => t.GetMainResourceByPidUri(resourceB.PidUri, It.IsAny<List<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(resourceB);
            _mockResourceRepo.Setup(t => t.GetResourcesByPidUri(resourceB.PidUri, It.IsAny<List<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(new ResourcesCTO(null, resourceA, new List<VersionOverviewCTO>()));
            _mockResourceRepo.Setup(t => t.CheckIfExist(resourceB.PidUri, It.IsAny<List<string>>(), _resourceGraph)).Returns(true);
            _mockResourceRepo.Setup(t => t.CheckIfExist(resourceB.PidUri, It.IsAny<List<string>>(), _draftResourceGraph)).Returns(false);
            _mockResourceRepo.Setup(t => t.GetIdByPidUri(resourceB.PidUri, It.IsAny<ISet<Uri>>())).Returns(new Uri(resourceB.Id));

            // Act
            var result = _service.LinkResourceIntoList(resourceB.PidUri, resourceA.PidUri, out IList<VersionOverviewCTO> versions);

            // Assert
            _mockResourceRepo.Verify(t => t.CreateLinkingProperty(resourceB.PidUri, new Uri(Graph.Metadata.Constants.Resource.HasLaterVersion), new Uri(resourceA.Id), _draftResourceGraph), Times.Once);
            _mockResourceRepo.Verify(t => t.CreateLinkingProperty(resourceB.PidUri, new Uri(Graph.Metadata.Constants.Resource.HasLaterVersion), new Uri(resourceA.Id), _resourceGraph), Times.Once);
            Assert.Equal(result, COLID.RegistrationService.Common.Constants.Messages.Resource.Linking.LinkSuccessful);
        }

        [Fact]
        public async void Link_New_Version_Between_Upper_And_Smaller_Resource_Success()
        {
            var resourceA = CreateResourceWithVersion(ColidEntryLifecycleStatus.Published, "1");
            var resourceC = CreateResourceWithVersion(ColidEntryLifecycleStatus.Published, "3");
            var versionList = new List<VersionOverviewCTO>();
            versionList.Add(new VersionOverviewCTO() { Id = resourceA.Id, Version = "1", PidUri = resourceA.PidUri.ToString(), PublishedVersion = resourceA.PublishedVersion });
            versionList.Add(new VersionOverviewCTO() { Id = resourceC.Id, Version = "3", PidUri = resourceC.PidUri.ToString(), PublishedVersion = resourceC.PublishedVersion });
            resourceA.Versions = versionList;
            var resourceB = CreateResourceWithVersion(ColidEntryLifecycleStatus.Published, "2");


            _mockResourceRepo.Setup(t => t.GetMainResourceByPidUri(resourceA.PidUri, It.IsAny<List<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(resourceA);
            _mockResourceRepo.Setup(t => t.CheckIfExist(resourceA.PidUri, It.IsAny<List<string>>(), _resourceGraph)).Returns(true);
            _mockResourceRepo.Setup(t => t.CheckIfExist(resourceA.PidUri, It.IsAny<List<string>>(), _draftResourceGraph)).Returns(false);
            _mockResourceRepo.Setup(t => t.GetIdByPidUri(resourceA.PidUri, It.IsAny<ISet<Uri>>())).Returns(new Uri(resourceA.Id));

            _mockResourceRepo.Setup(t => t.GetIdByPidUri(resourceC.PidUri, It.IsAny<ISet<Uri>>())).Returns(new Uri(resourceC.Id));

            _mockResourceRepo.Setup(t => t.GetMainResourceByPidUri(resourceB.PidUri, It.IsAny<List<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(resourceB);
            _mockResourceRepo.Setup(t => t.GetResourcesByPidUri(resourceB.PidUri, It.IsAny<List<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(new ResourcesCTO(null, resourceA, new List<VersionOverviewCTO>()));
            _mockResourceRepo.Setup(t => t.CheckIfExist(resourceB.PidUri, It.IsAny<List<string>>(), _resourceGraph)).Returns(true);
            _mockResourceRepo.Setup(t => t.CheckIfExist(resourceB.PidUri, It.IsAny<List<string>>(), _draftResourceGraph)).Returns(false);
            _mockResourceRepo.Setup(t => t.GetIdByPidUri(resourceB.PidUri, It.IsAny<ISet<Uri>>())).Returns(new Uri(resourceB.Id));

            // Act
            var result = _service.LinkResourceIntoList(resourceB.PidUri, resourceA.PidUri, out IList<VersionOverviewCTO> versions);

            // Assert
            _mockResourceRepo.Verify(t => t.DeleteLinkingProperty(resourceA.PidUri, new Uri(Graph.Metadata.Constants.Resource.HasLaterVersion), new Uri(resourceC.Id), _draftResourceGraph), Times.Once);
            _mockResourceRepo.Verify(t => t.DeleteLinkingProperty(resourceA.PidUri, new Uri(Graph.Metadata.Constants.Resource.HasLaterVersion), new Uri(resourceC.Id), _resourceGraph), Times.Once);
            
            _mockResourceRepo.Verify(t => t.CreateLinkingProperty(resourceA.PidUri, new Uri(Graph.Metadata.Constants.Resource.HasLaterVersion), new Uri(resourceB.Id), _draftResourceGraph), Times.Once);
            _mockResourceRepo.Verify(t => t.CreateLinkingProperty(resourceA.PidUri, new Uri(Graph.Metadata.Constants.Resource.HasLaterVersion), new Uri(resourceB.Id), _resourceGraph), Times.Once);
            
            _mockResourceRepo.Verify(t => t.CreateLinkingProperty(resourceB.PidUri, new Uri(Graph.Metadata.Constants.Resource.HasLaterVersion), new Uri(resourceC.Id), _draftResourceGraph), Times.Once);
            _mockResourceRepo.Verify(t => t.CreateLinkingProperty(resourceB.PidUri, new Uri(Graph.Metadata.Constants.Resource.HasLaterVersion), new Uri(resourceC.Id), _resourceGraph), Times.Once);
            Assert.Equal(result, COLID.RegistrationService.Common.Constants.Messages.Resource.Linking.LinkSuccessful);
        }

        [Fact]
        public async void Link_Resources_Without_VersionList_Throws_BusinessException()
        {
            var resource_ver1 = CreateResourceWithVersion(ColidEntryLifecycleStatus.Published, "1");
            var resource_ver2 = CreateResourceWithVersion(ColidEntryLifecycleStatus.Published, "2");


            _mockResourceRepo.Setup(t => t.GetMainResourceByPidUri(resource_ver1.PidUri, It.IsAny<List<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(resource_ver1);
            _mockResourceRepo.Setup(t => t.CheckIfExist(resource_ver1.PidUri, It.IsAny<List<string>>(), _resourceGraph)).Returns(true);
            _mockResourceRepo.Setup(t => t.CheckIfExist(resource_ver1.PidUri, It.IsAny<List<string>>(), _draftResourceGraph)).Returns(false);
            _mockResourceRepo.Setup(t => t.GetMainResourceByPidUri(resource_ver2.PidUri, It.IsAny<List<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(resource_ver2);
            _mockResourceRepo.Setup(t => t.CheckIfExist(resource_ver2.PidUri, It.IsAny<List<string>>(), _resourceGraph)).Returns(true);
            _mockResourceRepo.Setup(t => t.CheckIfExist(resource_ver2.PidUri, It.IsAny<List<string>>(), _draftResourceGraph)).Returns(false);

            // Act
            var result = Assert.Throws<BusinessException>(() => _service.LinkResourceIntoList(resource_ver2.PidUri, resource_ver1.PidUri, out IList<VersionOverviewCTO> versions));
        }

        [Fact]
        public async void Link_Resources_With_Same_Version_Throws_BusinessException()
        {
            var resource_ver1 = CreateResourceWithVersion(ColidEntryLifecycleStatus.Published, "1");
            var versionList = new List<VersionOverviewCTO>();
            versionList.Add(new VersionOverviewCTO() { Id = resource_ver1.Id, Version = "1", PidUri = resource_ver1.PidUri.ToString(), PublishedVersion = resource_ver1.PublishedVersion });
            resource_ver1.Versions = versionList;
            var resource_ver2 = CreateResourceWithVersion(ColidEntryLifecycleStatus.Published, "1");


            _mockResourceRepo.Setup(t => t.GetMainResourceByPidUri(resource_ver1.PidUri, It.IsAny<List<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(resource_ver1);
            _mockResourceRepo.Setup(t => t.CheckIfExist(resource_ver1.PidUri, It.IsAny<List<string>>(), _resourceGraph)).Returns(true);
            _mockResourceRepo.Setup(t => t.CheckIfExist(resource_ver1.PidUri, It.IsAny<List<string>>(), _draftResourceGraph)).Returns(false);
            _mockResourceRepo.Setup(t => t.GetMainResourceByPidUri(resource_ver2.PidUri, It.IsAny<List<string>>(), It.IsAny<Dictionary<Uri, bool>>())).Returns(resource_ver2);
            _mockResourceRepo.Setup(t => t.CheckIfExist(resource_ver2.PidUri, It.IsAny<List<string>>(), _resourceGraph)).Returns(true);
            _mockResourceRepo.Setup(t => t.CheckIfExist(resource_ver2.PidUri, It.IsAny<List<string>>(), _draftResourceGraph)).Returns(false);

            // Act
            var result =  Assert.Throws<BusinessException>(() => _service.LinkResourceIntoList(resource_ver2.PidUri, resource_ver1.PidUri, out IList<VersionOverviewCTO> versions));
        }


        #region Helper

        private Resource CreateResource(ColidEntryLifecycleStatus colidEntryLifecycleStatus)
        {
            return new ResourceBuilder()
                .GenerateSampleData()
                .WithEntryLifecycleStatus(colidEntryLifecycleStatus)
                .Build();
        }
        private Resource CreateResourceWithVersion(ColidEntryLifecycleStatus colidEntryLifecycleStatus, string version)
        {
            return new ResourceBuilder()
                .GenerateSampleData()
                .WithEntryLifecycleStatus(colidEntryLifecycleStatus)
                .WithVersion(version)
                .Build();
        }
        private Resource CreateResourceWithVersionAndLaterVersion(ColidEntryLifecycleStatus colidEntryLifecycleStatus, string version)
        {
            return new ResourceBuilder()
                .GenerateSampleData()
                .WithEntryLifecycleStatus(colidEntryLifecycleStatus)
                .WithVersion(version)
                .WithLaterVersion(GetRandomPidUri().ToString())
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
