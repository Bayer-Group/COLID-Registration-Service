using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.Tests.Builder;
using COLID.Graph.TripleStore.DataModels.ConsumerGroups;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.Services.Validation.Models;
using COLID.RegistrationService.Services.Validation.Validators.Keys;
using COLID.RegistrationService.Tests.Common.Builder;
using Moq;
using Xunit;

namespace COLID.RegistrationService.Tests.Unit.Services.Validation.Validators.Keys
{
    [ExcludeFromCodeCoverage]
    public class ConsumerGroupValidatorTests
    {
        private readonly Mock<IConsumerGroupService> _consumerGroupServiceMock;
        private readonly ConsumerGroupValidator _validator;
        private readonly IList<MetadataProperty> _metadata;

        private readonly ConsumerGroupResultDTO _activeConsumerGroup;

        public ConsumerGroupValidatorTests()
        {
            _consumerGroupServiceMock = new Mock<IConsumerGroupService>();

            _activeConsumerGroup = new ConsumerGroupBuilder()
                .GenerateSampleData()
                .WithId($"https://pid.bayer.com/kos/{Guid.NewGuid()}")
                .BuildResultDTO();

            _metadata = new MetadataBuilder().GenerateSampleConsumerGroup().Build();

            IList<ConsumerGroupResultDTO> activeConsumerGroups = new List<ConsumerGroupResultDTO> { _activeConsumerGroup };
            _consumerGroupServiceMock.Setup(m => m.GetActiveEntities()).Returns(activeConsumerGroups).Verifiable();

            _validator = new ConsumerGroupValidator(_consumerGroupServiceMock.Object);
        }

        [Fact]
        public void InternalHasValidationResult_Success()
        {
            // Arrange
            var resource = CreateResourceWithConsumerGroup(_activeConsumerGroup.Id);
            var validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);

            //// Act
            _validator.HasValidationResult(validationFacade, GetConsumerGroupProperty(resource));

            //// Assert
            Assert.Contains(Graph.Metadata.Constants.Resource.HasConsumerGroup, validationFacade.RequestResource.Properties);
            Assert.Single(validationFacade.RequestResource.Properties[Graph.Metadata.Constants.Resource.HasConsumerGroup]);
            Assert.Equal(_activeConsumerGroup.Id, validationFacade.RequestResource.Properties[Graph.Metadata.Constants.Resource.HasConsumerGroup].First());
            Assert.Equal(0, validationFacade.ValidationResults.Count);
        }

        [Fact]
        public void InternalHasValidationResult_CriticalValidatioResultExists()
        {
            // Arrange
            var resource = CreateResourceWithConsumerGroup(_activeConsumerGroup.Id);
            var criticalValidationResult = new ValidationResultProperty(resource.Id, Graph.Metadata.Constants.Resource.HasConsumerGroup, "Unique critical mock error", string.Empty, ValidationResultSeverity.Violation);
            var validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);
            validationFacade.ValidationResults.Add(criticalValidationResult);

            //// Act
            _validator.HasValidationResult(validationFacade, GetConsumerGroupProperty(resource));

            //// Assert
            Assert.Contains(Graph.Metadata.Constants.Resource.HasConsumerGroup, validationFacade.RequestResource.Properties);
            Assert.Single(validationFacade.RequestResource.Properties[Graph.Metadata.Constants.Resource.HasConsumerGroup]);
            Assert.Equal(_activeConsumerGroup.Id, validationFacade.RequestResource.Properties[Graph.Metadata.Constants.Resource.HasConsumerGroup].First());

            Assert.Equal(1, validationFacade.ValidationResults.Count);
            Assert.Contains(criticalValidationResult, validationFacade.ValidationResults);
        }

        [Fact]
        public void InternalHasValidationResult_ForbiddenConsumerGroup()
        {
            // Arrange
            var deprecatedCG = new ConsumerGroupBuilder()
                .GenerateSampleData()
                .WithId($"https://pid.bayer.com/kos/{Guid.NewGuid()}")
                .WithLifecycleStatus(Graph.Metadata.Constants.ConsumerGroup.LifecycleStatus.Deprecated)
                .BuildResultDTO();
            var activeCG = new ConsumerGroupBuilder()
                .GenerateSampleData()
                .WithId($"https://pid.bayer.com/kos/{Guid.NewGuid()}")
                .BuildResultDTO();

            IList<ConsumerGroupResultDTO> activeConsumerGroups = new List<ConsumerGroupResultDTO> { activeCG };
            _consumerGroupServiceMock.Setup(m => m.GetActiveEntities()).Returns(activeConsumerGroups).Verifiable();

            var resource = CreateResourceWithConsumerGroup(deprecatedCG.Id);
            var validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);

            //// Act
            _validator.HasValidationResult(validationFacade, GetConsumerGroupProperty(resource));

            //// Assert
            Assert.Contains(Graph.Metadata.Constants.Resource.HasConsumerGroup, validationFacade.RequestResource.Properties);
            Assert.Equal(1, validationFacade.RequestResource.Properties[Graph.Metadata.Constants.Resource.HasConsumerGroup].Count);
            Assert.Equal(deprecatedCG.Id, validationFacade.RequestResource.Properties[Graph.Metadata.Constants.Resource.HasConsumerGroup][0]);
            Assert.Equal(1, validationFacade.ValidationResults.Count);
        }


        private KeyValuePair<string, List<dynamic>> GetConsumerGroupProperty(Resource resource)
        {
            return resource.Properties.SingleOrDefault(p => p.Key == Graph.Metadata.Constants.Resource.HasConsumerGroup);
        }

        private Resource CreateResourceWithConsumerGroup(string consumerGroup)
        {
            var resource = new ResourceBuilder()
                .GenerateSampleData()
                .WithConsumerGroup(consumerGroup)
                .WithPidUri($"https://pid.bayer.com/kos/0308eeb4-ed33-43b8-abf7-599a57cbd718")
                .Build();

            return resource;
        }
    }
}
