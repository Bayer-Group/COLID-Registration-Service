using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.Tests.Builder;
using COLID.Graph.TripleStore.DataModels.Resources;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.Services.Validation.Models;
using COLID.RegistrationService.Services.Validation.Validators;
using COLID.RegistrationService.Tests.Common.Builder;
using Moq;
using Xunit;

namespace COLID.RegistrationService.Tests.Unit.Services.Validation.Validators
{
    [ExcludeFromCodeCoverage]
    public class EntityPropertyValidatorTests
    {
        private readonly Mock<IServiceProvider> _serviceProviderMock;
        private readonly Mock<IEntityService> _entityServiceMock;

        private readonly EntityPropertyValidator _validator;

        private readonly IList<MetadataProperty> _metadata;

        public EntityPropertyValidatorTests()
        {
            _serviceProviderMock = new Mock<IServiceProvider>();
            _entityServiceMock = new Mock<IEntityService>();

            _validator = new EntityPropertyValidator(_entityServiceMock.Object, _serviceProviderMock.Object);

            _metadata = new MetadataBuilder().GenerateSampleResourceData().Build();
        }

        [Fact]
        public void HandleOverwriteProperties_Success()
        {
            // Arrange
            var propertyKey = Graph.Metadata.Constants.RDF.Type;

            var requestResource = CreateResourceWithType(Graph.Metadata.Constants.Resource.Type.GenericDataset);
            var repoResource = CreateResourceWithType(Graph.Metadata.Constants.Resource.Type.Ontology);
            var resourcesCTO = new ResourcesCTO(repoResource, repoResource, new List<VersionOverviewCTO>());

            var entityValidationFacade = new EntityValidationFacade(ResourceCrudAction.Update, requestResource, resourcesCTO, string.Empty, _metadata, string.Empty);

            // Act
            _validator.Validate(propertyKey, entityValidationFacade);

            // Assert
            var resourceType = entityValidationFacade.RequestResource.Properties[Graph.Metadata.Constants.RDF.Type];
            Assert.All(resourceType, t => Assert.Equal(Graph.Metadata.Constants.Resource.Type.Ontology, t));
        }

        private Resource CreateResourceWithType(string resourceType)
        {
            var resource = new ResourceBuilder()
                .GenerateSampleData()
                .WithType(resourceType)
                .Build();

            return resource;
        }
    }
}
