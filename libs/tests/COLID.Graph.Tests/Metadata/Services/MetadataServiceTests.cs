using System;
using System.Collections.Generic;
using COLID.Cache.Services;
using COLID.Graph.Metadata.Repositories;
using COLID.Graph.Metadata.Services;
using COLID.Graph.Metadata.Constants;
using Moq;
using Xunit;
using System.Linq;
using COLID.Graph.Tests.Builder;
using COLID.Graph.Metadata.DataModels.Metadata.Comparison;

namespace COLID.Graph.Tests.Metadata.Services
{
    public class MetadataServiceTests
    {
        private readonly Mock<IMetadataRepository> _metadataRepo;
        private readonly Mock<IMetadataGraphConfigurationRepository> _metadataGraphConfigRepoMock;
        private readonly ICacheService _cacheService;

        private readonly IMetadataService _metadataService;

        public MetadataServiceTests()
        {
            _metadataRepo = new Mock<IMetadataRepository>();
            _cacheService = new NoCacheService();
            _metadataGraphConfigRepoMock = new Mock<IMetadataGraphConfigurationRepository>();

            _metadataService = new MetadataService(_metadataRepo.Object, _cacheService, _metadataGraphConfigRepoMock.Object);
        }

        [Fact]
        public void GetComparisonMetadata_Success()
        {
            // Arrange
            var entityTypes = new List<string>() { Resource.Type.MathematicalModel, Resource.Type.Ontology };
            var metadataComparisonConfigTypes = new List<MetadataComparisonConfigTypesDto>();
            metadataComparisonConfigTypes.Add(new MetadataComparisonConfigTypesDto(null, entityTypes));

            var browsableResource = new MetadataBuilder().GenerateSampleEndpointData().BuildBrowsableResource();
            var mathematicalModelMetadata = new MetadataBuilder()
                .GenerateSampleResourceData(Resource.Type.MathematicalModel)
                .GenerateSampleDistributionEndpoint(browsableResource)
                .Build();

            var queryEndpoint = new MetadataBuilder().GenerateSampleEndpointData().BuildQueryEndpoint();
            var ontologyMetadata = new MetadataBuilder()
                .GenerateSampleDistributionEndpoint(queryEndpoint)
                .GenerateSampleResourceData(Resource.Type.Ontology).Build();

            _metadataRepo.Setup(s => s.GetMetadataForEntityTypeInConfig(Resource.Type.MathematicalModel, null)).Returns(mathematicalModelMetadata);
            _metadataRepo.Setup(s => s.GetMetadataForEntityTypeInConfig(Resource.Type.Ontology, null)).Returns(ontologyMetadata);

            // Act
            var metadata = _metadataService.GetComparisonMetadata(metadataComparisonConfigTypes);

            // Assert
            var distributionEndpoint = metadata.FirstOrDefault(t => t.Key == Resource.Distribution);
            Assert.NotNull(distributionEndpoint);
            Assert.Equal(14, metadata.Count);
            Assert.Equal(2, distributionEndpoint.NestedMetadata.Count);
            Assert.Contains(distributionEndpoint.NestedMetadata, t => t.Key == queryEndpoint.Key);
            Assert.Contains(distributionEndpoint.NestedMetadata, t => t.Key == browsableResource.Key);
        }

        [Fact]
        public void GetComparisonMetadata_Should_ThrowError_EmptyEntityTypes()
        {
            var entityTypes = new List<string>();
            var metadataComparisonConfigTypes = new List<MetadataComparisonConfigTypesDto>();
            metadataComparisonConfigTypes.Add(new MetadataComparisonConfigTypesDto(null, entityTypes));

            Assert.Throws<ArgumentNullException>(() => _metadataService.GetComparisonMetadata(metadataComparisonConfigTypes));
        }

        [Fact]
        public void GetComparisonMetadata_Should_ThrowError_EntityTypesAreNull()
        {
            Assert.Throws<ArgumentNullException>(() => _metadataService.GetComparisonMetadata(null));
        }

        [Fact]
        public void GetMergedMetadata_Success()
        {
            // Arrange
            var entityTypes = new List<string>() { Resource.Type.MathematicalModel, Resource.Type.Ontology };

            var browsableResource = new MetadataBuilder().GenerateSampleEndpointData().BuildBrowsableResource();
            var mathematicalModelMetadata = new MetadataBuilder()
                .GenerateSampleResourceData(Resource.Type.MathematicalModel)
                .GenerateSampleDistributionEndpoint(browsableResource)
                .Build();

            var queryEndpoint = new MetadataBuilder().GenerateSampleEndpointData().BuildQueryEndpoint();
            var ontologyMetadata = new MetadataBuilder()
                .GenerateSampleDistributionEndpoint(queryEndpoint)
                .GenerateSampleResourceData(Resource.Type.Ontology).Build();

            _metadataRepo.Setup(s => s.GetMetadataForEntityTypeInConfig(Resource.Type.MathematicalModel, null)).Returns(mathematicalModelMetadata);
            _metadataRepo.Setup(s => s.GetMetadataForEntityTypeInConfig(Resource.Type.Ontology, null)).Returns(ontologyMetadata);

            // Act
            var metadata = _metadataService.GetMergedMetadata(entityTypes);

            // Assert
            var distributionEndpoint = metadata.FirstOrDefault(t => t.Key == Resource.Distribution);
            Assert.NotNull(distributionEndpoint);
            Assert.Equal(14, metadata.Count);
            Assert.Equal(2, distributionEndpoint.NestedMetadata.Count);
            Assert.Contains(distributionEndpoint.NestedMetadata, t => t.Key == queryEndpoint.Key);
            Assert.Contains(distributionEndpoint.NestedMetadata, t => t.Key == browsableResource.Key);
        }

        [Fact]
        public void GetMergedMetadata_Should_ThrowError_EmptyEntityTypes()
        {
            Assert.Throws<ArgumentNullException>(() => _metadataService.GetMergedMetadata(new List<string>()));
        }

        [Fact]
        public void GetMergedMetadata_Should_ThrowError_EntityTypesAreNull()
        {
            Assert.Throws<ArgumentNullException>(() => _metadataService.GetMergedMetadata(null));
        }

    }
}
