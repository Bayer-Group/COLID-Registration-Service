﻿/*using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using COLID.Common.Extensions;
using COLID.Graph.Metadata.Constants;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.Services;
using COLID.RegistrationService.Common.DataModel.Resources;
using COLID.RegistrationService.Common.Enums.ColidEntry;
using COLID.RegistrationService.Common.Extensions;
using COLID.RegistrationService.Repositories.Interface;
using COLID.RegistrationService.Services.Implementation;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.Tests.Common.Builder;
using COLID.StatisticsLog.Services;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using Entity = COLID.Graph.TripleStore.DataModels.Base.Entity;
using Resource = COLID.Graph.Metadata.DataModels.Resources.Resource;

namespace COLID.RegistrationService.Tests.Unit.Services
{
    [ExcludeFromCodeCoverage]
    public class HistoricResourceServiceTests
    {
        private readonly Mock<ILogger<HistoricResourceService>> _logger;
        private readonly Mock<IHistoricResourceRepository> _historicRepo;
        private readonly Mock<IResourceRepository> _resourceRepo;
        private readonly Mock<IMetadataService> _metadataService;
        private readonly IHistoricResourceService _service;

        private readonly Uri _resourecGraph;
        private readonly Uri _historicResourecGraph;

        public HistoricResourceServiceTests()
        {
            _resourecGraph = new Uri("https://pid.bayer.com/resource/2.0");
            _historicResourecGraph = new Uri("https://pid.bayer.com/resource/historic");

            _logger = new Mock<ILogger<HistoricResourceService>>();
            _historicRepo = new Mock<IHistoricResourceRepository>();
            _resourceRepo = new Mock<IResourceRepository>();
            _metadataService = new Mock<IMetadataService>();

            SetupMetadataService();

            _service = new HistoricResourceService(_logger.Object, _historicRepo.Object, _resourceRepo.Object, _metadataService.Object);
        }

        private void SetupMetadataService()
        {
            _metadataService.Setup(mock => mock.GetInstanceGraph(PIDO.PidConcept)).Returns(_resourecGraph);
            _metadataService.Setup(mock => mock.GetHistoricInstanceGraph()).Returns(_historicResourecGraph);
        }


        [Fact]
        public void CreateHistoricTest_Successful()
        {
            Resource resourceAfterMethod = null;

            // Arrange
            _historicRepo.Setup(mock => mock.CreateHistoricResource(It.IsAny<Resource>(), It.IsAny<IList<MetadataProperty>>(), _historicResourecGraph))
                .Callback<Resource, IList<MetadataProperty>, Uri>((r, mp, ng) => resourceAfterMethod = r);

            Entity de = new DistributionEndpointBuilder().GenerateSampleData().Build();
            Entity mainDe = new DistributionEndpointBuilder().GenerateSampleData().WithNetworkAddress("www.google2").Build();
            Resource res = new ResourceBuilder().GenerateSampleData()
                .WithEntryLifecycleStatus(ColidEntryLifecycleStatus.Published)
                .WithDistributionEndpoint(de)
                .WithMainDistributionEndpoint(mainDe)
                .Build();

            // Act
            _service.CreateHistoricResource(res, new List<MetadataProperty>());

            // Assert
            _historicRepo.Verify(mock => mock.CreateHistoricResource(It.IsAny<Resource>(), It.IsAny<IList<MetadataProperty>>(), _historicResourecGraph), Times.Once());

            res.Properties.TryGetValue(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus, out var entryLifecycleStatusValue);
            Assert.False(entryLifecycleStatusValue.Contains(ColidEntryLifecycleStatus.Published.GetDescription()));
            Assert.True(entryLifecycleStatusValue.Contains(ColidEntryLifecycleStatus.Historic.GetDescription()));

            // Assert that the resource was cleaned up by PrepareResourceForHistorization
            res.Properties.TryGetValue(Graph.Metadata.Constants.Resource.HasPidEntryDraft, out var pidEntryDraftValue);
            Assert.True(pidEntryDraftValue.IsNullOrEmpty<dynamic>());

            res.Properties.TryGetValue(Graph.Metadata.Constants.EnterpriseCore.PidUri, out var pidUriValue);
            Assert.Empty(pidUriValue.FirstOrDefault().Properties);

            AssertNestedProperties(res, Graph.Metadata.Constants.Resource.Distribution);
            AssertNestedProperties(res, Graph.Metadata.Constants.Resource.MainDistribution);
        }

        [Fact]
        public void CreateHistoricTest_IgnoreDraft()
        {
            // Arrange
            Resource resourceAfterMethod = null;
            _historicRepo.Setup(mock => mock.CreateHistoricResource(It.IsAny<Resource>(), It.IsAny<IList<MetadataProperty>>(), _historicResourecGraph))
                .Callback<Resource, IList<MetadataProperty>, Uri>((r, mp, ng) => resourceAfterMethod = r);

            Resource res = new ResourceBuilder().GenerateSampleData()
                .WithEntryLifecycleStatus(ColidEntryLifecycleStatus.Draft)
                .Build();

            // Act
            _service.CreateHistoricResource(res, new List<MetadataProperty>());

            // Assert
            _historicRepo.Verify(mock => mock.CreateHistoricResource(It.IsAny<Resource>(), It.IsAny<IList<MetadataProperty>>(),_historicResourecGraph), Times.Never());
        }

        [Fact]
        public void CreateHistoricTest_IgnoreNonPublishedResources()
        {
            // Arrange
            _historicRepo.Setup(mock => mock.CreateHistoricResource(It.IsAny<Resource>(), It.IsAny<IList<MetadataProperty>>(), _historicResourecGraph));
            Resource res = new ResourceBuilder().GenerateSampleData().WithEntryLifecycleStatus(ColidEntryLifecycleStatus.Draft)
                .WithHistoricVersion("http://somehistoricuri").Build();

            // Act
            _service.CreateHistoricResource(res, new List<MetadataProperty>());

            // Assert
            _historicRepo.Verify(mock => mock.CreateHistoricResource(It.IsAny<Resource>(), It.IsAny<IList<MetadataProperty>>(), _historicResourecGraph), Times.Never());
        }

        [Theory]
        [InlineData("https://test.uri.1", null)]
        [InlineData(null, "https://test.uri.2")]
        [InlineData("https://test.uri.1", "https://test.uri.1")]
        [InlineData("", "")]
        [InlineData("", "https://test.uri.1")]
        public void CreateHasHistoricRelationTest_ThrowsArgumentException(string firstUri, string secondUri)
        {
            // Arrange
            _resourceRepo.Setup(mock => mock.CreateProperty(It.IsAny<Uri>(), It.IsAny<Uri>(), It.IsAny<Uri>(), _resourecGraph));

            // Act
            //Action act = () => _service.CreateHasHistoricRelation(firstUri, secondUri);
            _resourceRepo.Verify(mock => mock.CreateProperty(It.IsAny<Uri>(), It.IsAny<Uri>(), It.IsAny<Uri>(), _resourecGraph), Times.Never());

            // Assert
            //var exception = Assert.Throws<ArgumentException>(act);
            //Assert.Equal("Passed parameters are null or equal", exception.Message);
        }

        private void AssertNestedProperties(Resource res, string key)
        {
            res.Properties.TryGetValue(key, out var deValues);
            foreach (Entity deVal in deValues)
            {
                foreach (Entity v in deVal.Properties[Graph.Metadata.Constants.EnterpriseCore.PidUri])
                {
                    Assert.True(v.Properties.IsNullOrEmpty());
                }
            }
        }
    }
}
*/
