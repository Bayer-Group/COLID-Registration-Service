using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using COLID.Exception.Models.Business;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.Metadata.Services;
using COLID.Graph.Tests.Builder;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.Services.Validation.Models;
using COLID.RegistrationService.Services.Validation.Validators.Groups;
using COLID.RegistrationService.Tests.Common.Builder;
using Moq;
using Xunit;

namespace COLID.RegistrationService.Tests.Unit.Services.Validation.Validators.Groups
{
    [ExcludeFromCodeCoverage]
    public class LinkTypesValidatorTests
    {
        private readonly Mock<IResourceService> _resourceServiceMock;
        private readonly Mock<IMetadataService> _metadataServiceMock;
        private readonly IList<MetadataProperty> _metadata;

        private readonly LinkTypesValidator _validator;

        public LinkTypesValidatorTests()
        {
            _resourceServiceMock = new Mock<IResourceService>();
            _metadataServiceMock = new Mock<IMetadataService>();
            _metadata = new MetadataBuilder().GenerateSampleIsCopyOfDataset().Build();

            var nonRdfDatasets = new List<string>() { Graph.Metadata.Constants.Resource.Type.GenericDataset, Graph.Metadata.Constants.Resource.Type.RDFDatasetWithInstances };
            _metadataServiceMock.Setup(t => t.GetInstantiableEntityTypes(Graph.Metadata.Constants.PIDO.NonRDFDataset)).Returns(nonRdfDatasets);

            _validator = new LinkTypesValidator(_resourceServiceMock.Object, _metadataServiceMock.Object);
            
        }

        [Fact]
        public void InternalHasValidationResult_Success()
        {
            // Arrange
            var linkedResource = CreateResource(Graph.Metadata.Constants.Resource.Type.GenericDataset);
            var linkedPidUriString = linkedResource.PidUri.ToString();
            var resource = CreateResourceWithIsCopyOfDataset(linkedPidUriString);

            _resourceServiceMock.Setup(t => t.GetByPidUri(linkedResource.PidUri)).Returns(linkedResource);

            var validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetCopyOfDatasetProperty(resource));

            // Assert
            Assert.Contains(Graph.Metadata.Constants.Resource.IsCopyOfDataset, validationFacade.RequestResource.Properties);
            Assert.Single(validationFacade.RequestResource.Properties[Graph.Metadata.Constants.Resource.IsCopyOfDataset]);
            Assert.Equal(linkedPidUriString, validationFacade.RequestResource.Properties[Graph.Metadata.Constants.Resource.IsCopyOfDataset].First());
            Assert.Equal(0, validationFacade.ValidationResults.Count);
        }

        [Fact]
        public void InternalHasValidationResult_CreateValidationError_LinkedResourceIsSameAsCurrent()
        {
            // Arrange
            var pidUri = $"https://pid.bayer.com/kos/{Guid.NewGuid()}";
            var resource = CreateResourceWithIsCopyOfDataset(new Uri(pidUri), pidUri);

            var validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetCopyOfDatasetProperty(resource));

            // Assert
            Assert.Contains(Graph.Metadata.Constants.Resource.IsCopyOfDataset, validationFacade.RequestResource.Properties);
            Assert.Single(validationFacade.RequestResource.Properties[Graph.Metadata.Constants.Resource.IsCopyOfDataset]);

            Assert.Equal(pidUri, validationFacade.RequestResource.Properties[Graph.Metadata.Constants.Resource.IsCopyOfDataset].First());
            Assert.Equal(1, validationFacade.ValidationResults.Count);
            Assert.Contains(validationFacade.ValidationResults, v => v.ResultValue == pidUri && v.Path == Graph.Metadata.Constants.Resource.IsCopyOfDataset);
        }

        [Fact]
        public void InternalHasValidationResult_CreateValidationError_IsWrongResourceType()
        {
            // Arrange
            var linkedResource = CreateResource(Graph.Metadata.Constants.Resource.Type.Ontology);
            var linkedPidUriString = linkedResource.PidUri.ToString();
            var resource = CreateResourceWithIsCopyOfDataset(linkedPidUriString);

            _resourceServiceMock.Setup(t => t.GetByPidUri(linkedResource.PidUri)).Returns(linkedResource);

            var validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetCopyOfDatasetProperty(resource));

            // Assert
            Assert.Contains(Graph.Metadata.Constants.Resource.IsCopyOfDataset, validationFacade.RequestResource.Properties);
            Assert.Single(validationFacade.RequestResource.Properties[Graph.Metadata.Constants.Resource.IsCopyOfDataset]);

            Assert.Equal(linkedPidUriString, validationFacade.RequestResource.Properties[Graph.Metadata.Constants.Resource.IsCopyOfDataset].First());
            Assert.Equal(1, validationFacade.ValidationResults.Count);
            Assert.Contains(validationFacade.ValidationResults, v => v.ResultValue == linkedPidUriString && v.Path == Graph.Metadata.Constants.Resource.IsCopyOfDataset);
        }

        [Fact]
        public void InternalHasValidationResult_CreateValidationError_EntityNotFound()
        {
            // Arrange
            var linkedPidUriString = $"https://pid.bayer.com/kos/{Guid.NewGuid()}";
            var resource = CreateResourceWithIsCopyOfDataset(linkedPidUriString);

            _resourceServiceMock.Setup(t => t.GetByPidUri(new Uri(linkedPidUriString))).Throws(new EntityNotFoundException(string.Empty, linkedPidUriString));

            var validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetCopyOfDatasetProperty(resource));

            // Assert
            Assert.Contains(Graph.Metadata.Constants.Resource.IsCopyOfDataset, validationFacade.RequestResource.Properties);
            Assert.Single(validationFacade.RequestResource.Properties[Graph.Metadata.Constants.Resource.IsCopyOfDataset]);

            Assert.Equal(linkedPidUriString, validationFacade.RequestResource.Properties[Graph.Metadata.Constants.Resource.IsCopyOfDataset].First());
            Assert.Equal(1, validationFacade.ValidationResults.Count);
            Assert.Contains(validationFacade.ValidationResults, v => v.ResultValue == linkedPidUriString && v.Path == Graph.Metadata.Constants.Resource.IsCopyOfDataset);
        }

        [Fact]
        public void InternalHasValidationResult_CreateValidationError_WrongUriFormat()
        {
            // Arrange
            var linkedPidUriString = $"https://pid.bayer.com/kos/{Guid.NewGuid()}";
            var resource = CreateResourceWithIsCopyOfDataset(linkedPidUriString);

            _resourceServiceMock.Setup(t => t.GetByPidUri(new Uri(linkedPidUriString))).Throws(new UriFormatException());

            var validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetCopyOfDatasetProperty(resource));

            // Assert
            Assert.Contains(Graph.Metadata.Constants.Resource.IsCopyOfDataset, validationFacade.RequestResource.Properties);
            Assert.Single(validationFacade.RequestResource.Properties[Graph.Metadata.Constants.Resource.IsCopyOfDataset]);

            Assert.Equal(linkedPidUriString, validationFacade.RequestResource.Properties[Graph.Metadata.Constants.Resource.IsCopyOfDataset].First());
            Assert.Equal(1, validationFacade.ValidationResults.Count);
            Assert.Contains(validationFacade.ValidationResults, v => v.ResultValue == linkedPidUriString && v.Path == Graph.Metadata.Constants.Resource.IsCopyOfDataset);
        }

        private Resource CreateResourceWithIsCopyOfDataset(params string[] linkedPidUris)
        {
            var pidUri = new Uri($"https://pid.bayer.com/kos/{Guid.NewGuid()}");
            return CreateResourceWithIsCopyOfDataset(pidUri, linkedPidUris);
        }


        private KeyValuePair<string, List<dynamic>> GetCopyOfDatasetProperty(Resource resource)
        {
            return resource.Properties.SingleOrDefault(p => p.Key == Graph.Metadata.Constants.Resource.IsCopyOfDataset);
        }


        private Resource CreateResource(string resourceType)
        {
            var resourceBuilder = new ResourceBuilder()
                .GenerateSampleData()
                .WithType(resourceType);

            var resource = resourceBuilder.Build();

            return resource;
        }

        private Resource CreateResourceWithIsCopyOfDataset(Uri resourcePidUri, params string[] linkedPidUris)
        {
            var resourceBuilder = new ResourceBuilder()
                .GenerateSampleData()
                .WithPidUri(resourcePidUri.ToString())
                .WithCopyOfDataset(linkedPidUris);

            var resource = resourceBuilder.Build();

            return resource;
        }
    }
}
