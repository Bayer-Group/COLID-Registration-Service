using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AutoMapper;
using COLID.Cache.Services.Lock;
using COLID.Exception.Models;
using COLID.Graph.Metadata.Constants;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.Metadata.Services;
using COLID.Graph.Tests.Builder;
using COLID.Graph.TripleStore.MappingProfiles;
using COLID.Graph.TripleStore.Transactions;
using COLID.RegistrationService.Common.Enums.ColidEntry;
using COLID.RegistrationService.Repositories.Interface;
using COLID.RegistrationService.Services.Authorization.UserInfo;
using COLID.RegistrationService.Services.Implementation;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.Services.Validation.Models;
using COLID.RegistrationService.Tests.Common.Builder;
using COLID.StatisticsLog.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Resource = COLID.Graph.Metadata.DataModels.Resources.Resource;

namespace COLID.RegistrationService.Tests.Unit.Services
{
    [ExcludeFromCodeCoverage]
    public class RevisionServiceTests
    {
        private readonly Mock<ILogger<RevisionService>> _mockLogger;
        private readonly Mock<IResourceRepository> _mockResourceRepo;
        private readonly Mock<IMetadataService> _mockMetadataService;
 

        private readonly Random _random = new Random();
        private readonly string _validRequester = "anonymous@anonymous.com";

        private readonly IRevisionService _service;
        private readonly IList<MetadataProperty> _metadata;
        private IList<VersionOverviewCTO> _resourceVersionList;

        private readonly Uri _resourceGraph;
        private readonly Uri _draftResourceGraph;
        private readonly Uri _linkHistoryGraph;
        private readonly Uri _consumerGroupGraph;

        public RevisionServiceTests()
        {
            _resourceGraph = new Uri("https://pid.bayer.com/resource/2.0");
            _draftResourceGraph = new Uri("https://pid.bayer.com/resource/2.0/Draft");
            _linkHistoryGraph = new Uri("https://pid.bayer.com/resource/2.0/LinkHistory");
            _consumerGroupGraph = new Uri("https://pid.bayer.com/consumergroup/1.0");
            _resourceVersionList = new List<VersionOverviewCTO>();

            _mockLogger = new Mock<ILogger<RevisionService>>();
            _mockResourceRepo = new Mock<IResourceRepository>();
            _mockMetadataService = new Mock<IMetadataService>();

            var lockFactory = new InMemoryLockFactory();

            var configuration = new MapperConfiguration(cfg => cfg.AddProfile(new ResourceProfile()));
            var mapper = new Mapper(configuration);

            SetupMetadataService();
            SetupResourceRepo();

            _service = new RevisionService(
                    mapper,
                _mockLogger.Object,
                _mockResourceRepo.Object,
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
        }

        private void SetupResourceRepo()
        {
            _mockResourceRepo.Setup(mock => mock.CheckIfExist(It.IsAny<Uri>(), It.IsAny<IList<string>>(), _resourceGraph)).Returns(true);

            var mockTransaction = new Mock<ITripleStoreTransaction>();
            _mockResourceRepo.Setup(mock => mock.CreateTransaction()).Returns(mockTransaction.Object);
        }

        #endregion Setup Services

        [Fact]
        public async void CreateInitialResourceRevision_Success()
        {
            // Arrange
            var resourceBuilder = new ResourceBuilder()
                .GenerateSampleData();
            var resource = resourceBuilder.Build();

            string revisionGraphPrefix = resource.Id + "Rev" + 1;
            string additionalGraphName = revisionGraphPrefix + "_added";
            // Act
            var result = _service.InitializeResourceInAdditionalsGraph(resource, _metadata);
            
            // Assert
            Assert.NotNull(result);
            _mockResourceRepo.Verify(t => t.CreateProperty(new Uri(resource.Id), new Uri(COLID.Graph.Metadata.Constants.Resource.HasRevision), revisionGraphPrefix,_resourceGraph), Times.Once);
            _mockResourceRepo.Verify(t => t.Create(resource, _metadata, new Uri(additionalGraphName)), Times.Once);

        }

        [Fact]
        public async void AddAdditionalsAndRemovals_AddNewProperty_Success()
        {
            var authorChange = "ChangedAuthor@bayer.com";

            // Arrange
            var resourceBuilder = new ResourceBuilder().GenerateSampleData()
                .WithEntryLifecycleStatus(ColidEntryLifecycleStatus.Published);
            var publishedResource = resourceBuilder.Build();

            var resourceBuilder2 = new ResourceBuilder().GenerateSampleData()
               .WithEntryLifecycleStatus(ColidEntryLifecycleStatus.Draft)
               .WithAuthor(authorChange)
               .WithPidUri(publishedResource.PidUri.ToString());

            var draftResource = resourceBuilder2.Build();
            draftResource.Properties.Remove(Graph.Metadata.Constants.Resource.Keyword);

            _mockMetadataService.Setup(mock => mock.GetMetadataForEntityTypeInConfig(It.IsAny<string>(), It.IsAny<string>())).Returns(_metadata);

            var revisionGraphPrefix = publishedResource.Id + "Rev" + 1;
            // Act
            var result = await _service.AddAdditionalsAndRemovals(publishedResource,draftResource);

            // Assert
            Assert.NotNull(result);

            Assert.True(result.Properties.ContainsKey(Graph.Metadata.Constants.Resource.Author));
            Assert.Equal(result.Properties[Graph.Metadata.Constants.Resource.Author][0], authorChange);
        }

        [Fact]
        public async void AddAdditionalsAndRemovals_DeleteProperty_Success()
        {
            // Arrange
            var resourceBuilder = new ResourceBuilder().GenerateSampleData()
                .WithEntryLifecycleStatus(ColidEntryLifecycleStatus.Published);
            var publishedResource = resourceBuilder.Build();

            var resourceBuilder2 = new ResourceBuilder().GenerateSampleData()
               .WithEntryLifecycleStatus(ColidEntryLifecycleStatus.Draft)
               .WithPidUri(publishedResource.PidUri.ToString());

            var draftResource = resourceBuilder2.Build();
            draftResource.Properties.Remove(Graph.Metadata.Constants.Resource.Keyword);
            draftResource.Properties.Remove(Graph.Metadata.Constants.Resource.Author);

            _mockMetadataService.Setup(mock => mock.GetMetadataForEntityTypeInConfig(It.IsAny<string>(), It.IsAny<string>())).Returns(_metadata);
            var revisionGraphPrefix = publishedResource.Id + "Rev" + 1;


            Assert.True(publishedResource.Properties.ContainsKey(Graph.Metadata.Constants.Resource.Keyword));
            Assert.True(publishedResource.Properties.ContainsKey(Graph.Metadata.Constants.Resource.Author));
            // Act
            var result = await _service.AddAdditionalsAndRemovals(publishedResource, draftResource);

            // Assert
            Assert.NotNull(result);
           
            Assert.False(result.Properties.ContainsKey(Graph.Metadata.Constants.Resource.Author));
            Assert.False(result.Properties.ContainsKey(Graph.Metadata.Constants.Resource.Keyword));
        }

        [Fact]
        public async void AddAdditionalsAndRemovals_AddNewProperty_And_DeleteExistingProperty_Success()
        {
            var authorChange = "ChangedAuthor@bayer.com";
            var randomKeyword = "RandomKeyword";
            
            var resourceBuilder = new ResourceBuilder().GenerateSampleData()
                .WithEntryLifecycleStatus(ColidEntryLifecycleStatus.Published);
            var publishedResource = resourceBuilder.Build();
            publishedResource.Properties.Remove(Graph.Metadata.Constants.Resource.Keyword);

            var resourceBuilder2 = new ResourceBuilder().GenerateSampleData()
               .WithEntryLifecycleStatus(ColidEntryLifecycleStatus.Draft)
               .WithKeyword(randomKeyword)
               .WithPidUri(publishedResource.PidUri.ToString());

            var draftResource = resourceBuilder2.Build();
            draftResource.Properties.Remove(Graph.Metadata.Constants.Resource.Author);

            _mockMetadataService.Setup(mock => mock.GetMetadataForEntityTypeInConfig(It.IsAny<string>(), It.IsAny<string>())).Returns(_metadata);
            
            var revisionGraphPrefix = publishedResource.Id + "Rev" + 1;


            Assert.False(publishedResource.Properties.ContainsKey(Graph.Metadata.Constants.Resource.Keyword));
            Assert.True(publishedResource.Properties.ContainsKey(Graph.Metadata.Constants.Resource.Author));
            // Act
            var result = await _service.AddAdditionalsAndRemovals(publishedResource, draftResource);

            // Assert
            Assert.NotNull(result);

            Assert.False(result.Properties.ContainsKey(Graph.Metadata.Constants.Resource.Author));
            Assert.True(result.Properties.ContainsKey(Graph.Metadata.Constants.Resource.Keyword));
            Assert.Equal(result.Properties[Graph.Metadata.Constants.Resource.Keyword][0] , randomKeyword);
        }

        [Fact]
        public async void AddAdditionalsAndRemovals_Resources_With_Different_PidUris_ThrowError()
        {
             
            // Arrange
            var resourceBuilder = new ResourceBuilder().GenerateSampleData()
                .WithEntryLifecycleStatus(ColidEntryLifecycleStatus.Published);
            var publishedResource = resourceBuilder.Build();

            var resourceBuilder2 = new ResourceBuilder().GenerateSampleData()
               .WithEntryLifecycleStatus(ColidEntryLifecycleStatus.Draft);
            var draftResource = resourceBuilder2.Build();

            _mockMetadataService.Setup(mock => mock.GetMetadataForEntityType(It.IsAny<string>())).Returns(_metadata);

            var revisionGraphPrefix = publishedResource.Id + "Rev" + 1;
            // Act
            await Assert.ThrowsAsync<BusinessException>(() =>  _service.AddAdditionalsAndRemovals(publishedResource, draftResource));

             
        }

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
