using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.Tests.Builder;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.DataModels.Resources;
using COLID.RegistrationService.Common.DataModel.PidUriTemplates;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.Services.Validation.Models;
using COLID.RegistrationService.Services.Validation.Validators.Ranges;
using COLID.RegistrationService.Tests.Common.Builder;
using Moq;
using Xunit;

namespace COLID.RegistrationService.Tests.Unit.Services.Validation.Validators.Ranges
{
    [ExcludeFromCodeCoverage]
    public class IdentifierValidatorTests
    {
        private readonly Mock<IPidUriTemplateService> _pidUriTemplateService;
        private readonly Mock<IPidUriGenerationService> _pidUriGenerationService;
        private readonly Mock<IConsumerGroupService> _consumerGroupService;
        private readonly IdentifierValidator _validator;

        private readonly IList<MetadataProperty> _metadata;

        public IdentifierValidatorTests()
        {
            _pidUriTemplateService = new Mock<IPidUriTemplateService>();
            _pidUriGenerationService = new Mock<IPidUriGenerationService>();
            _consumerGroupService = new Mock<IConsumerGroupService>();
            _validator = new IdentifierValidator(_pidUriTemplateService.Object, _pidUriGenerationService.Object, _consumerGroupService.Object);
            _metadata = new MetadataBuilder().GenerateSamplePidUri().Build();
        }

        private void SetupPidUriTemplateService(IList<PidUriTemplateFlattened> pidUriTemplateFlattened = null)
        {
            _pidUriTemplateService.Setup(mock => mock.GetFlatPidUriTemplates(It.IsAny<EntitySearch>())).Returns(pidUriTemplateFlattened ?? new List<PidUriTemplateFlattened>());
        }

        private void SetupPidUriGenerationService()
        {
            _pidUriGenerationService.SetupGet(ex => ex.GeneratedIdentifier).Returns(new List<string>());
        }

        [Fact]
        public void InternalHasValidationResult_CustomIdentifier_Successfully()
        {
            // Arrange
            var resource = CreateResource("https://pid.bayer.com/custom-valid-identifer");
            var pidUriTemplateFlattened = new PidUriTemplateFlattenedBuilder().GenerateSampleData().Build();

            SetupPidUriTemplateService(new List<PidUriTemplateFlattened>() { pidUriTemplateFlattened });
            SetupPidUriGenerationService();

            EntityValidationFacade validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetPidUriProperty(resource));

            // Assert
            Assert.Contains(Graph.Metadata.Constants.EnterpriseCore.PidUri, validationFacade.RequestResource.Properties);
            Assert.Equal(0, validationFacade.ValidationResults.Count);

            Entity pidUri = validationFacade.RequestResource.Properties.SingleOrDefault(p => p.Key == Graph.Metadata.Constants.EnterpriseCore.PidUri).Value[0];

            Assert.Equal("https://pid.bayer.com/custom-valid-identifer", pidUri.Id);
            Assert.DoesNotContain(Graph.Metadata.Constants.Identifier.HasUriTemplate, pidUri.Properties);
        }

        [Fact]
        public void InternalHasValidationResult_GenerateIdentifier()
        {
            // Arrange
            var pidUriTemplateFlattened = new PidUriTemplateFlattenedBuilder().GenerateSampleData().Build();
            var resource = CreateResource(null, pidUriTemplateFlattened.Id);
            var expectedidentifier = "https://pid.bayer.com/DINOS/4113458a-12a2-4f26-9ff7-1d8fb18b25ec";

            var consumerGroup = new ConsumerGroupBuilder()
               .GenerateSampleData()
               .WithId($"{Graph.Metadata.Constants.Entity.IdPrefix}{new Guid()}")
               .WithPidUriTemplate(pidUriTemplateFlattened.Id)
               .BuildResultDTO();

            _consumerGroupService.Setup(s => s.GetEntity(It.IsAny<string>())).Returns(consumerGroup);
            _pidUriTemplateService.Setup(t => t.GetFlatIdentifierTemplateById(It.IsAny<string>())).Returns(pidUriTemplateFlattened);
            _pidUriGenerationService.Setup(g => g.GenerateIdentifierFromTemplate(It.IsAny<PidUriTemplateFlattened>(), It.IsAny<Entity>())).Returns(expectedidentifier);

            SetupPidUriTemplateService();
            SetupPidUriGenerationService();

            EntityValidationFacade validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, consumerGroup.Id);

            // Act
            _validator.HasValidationResult(validationFacade, GetPidUriProperty(resource));

            // Assert
            Entity pidUri = validationFacade.RequestResource.Properties.SingleOrDefault(p => p.Key == Graph.Metadata.Constants.EnterpriseCore.PidUri).Value[0];

            Assert.Equal(expectedidentifier, pidUri.Id);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void InternalHasValidationResult_CreateValidationResult_IsNotOfTypeEntity(dynamic pidUriEntity)
        {
            // Arrange
            var resource = CreateResource();
            var expectedMessage = "The identifier was not specified in the correct format.";
            resource.Properties[Graph.Metadata.Constants.EnterpriseCore.PidUri] = new List<dynamic>() { pidUriEntity };

            EntityValidationFacade validationFacade = new EntityValidationFacade(ResourceCrudAction.Update, resource, null, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetPidUriProperty(resource));

            // Assert
            AssertValidationResult(validationFacade, expectedMessage);
        }

        [Theory]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData(null)]
        public void InternalHasValidationResult_CreateValidationResult_ContainsEmptyPidUri(string pidUriString)
        {
            // Arrange
            var resource = CreateResource(pidUriString);

            EntityValidationFacade validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetPidUriProperty(resource));

            // Assert
            Assert.DoesNotContain(Graph.Metadata.Constants.EnterpriseCore.PidUri, validationFacade.RequestResource.Properties);
        }

        [Theory]
        [InlineData("invalid uri", "No valid URI.")] // Invalid Uri
        [InlineData("ftp://dmp.bayer.com/kos/19014", "URI must start with http(s).")] // Wrong uri scheme
        [InlineData("https://dmp.bayer.com/kos/19014#pound", "URI must not contain pounds like '#' or '%23'.")] // Include pounds
        [InlineData("https://dmp.bayer.com/kos/19014%23pound", "URI must not contain pounds like '#' or '%23'.")] // Include pounds
        public void InternalHasValidationResult_CreateValidationResult_InvalidUri(string pidUri, string expectedMessage)
        {
            // Arrange
            var resource = CreateResource(pidUri);

            SetupPidUriTemplateService();
            SetupPidUriGenerationService();

            EntityValidationFacade validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetPidUriProperty(resource));

            // Assert
            AssertValidationResult(validationFacade, expectedMessage);
        }

        [Fact]
        public void InternalHasValidationResult_CreateValidationResult_SpacesInIdentifier()
        {
            // Arrange
            var expectedMessage = RegistrationService.Common.Constants.Messages.String.TruncateSpaces;
            var resource = CreateResource(" https://dmp.bayer.com/kos/19014 pound ");

            SetupPidUriTemplateService();
            SetupPidUriGenerationService();

            EntityValidationFacade validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetPidUriProperty(resource));

            // Assert
            AssertValidationResult(validationFacade, expectedMessage);
            Assert.Equal("https://dmp.bayer.com/kos/19014pound", validationFacade.RequestResource.Properties[Graph.Metadata.Constants.EnterpriseCore.PidUri].FirstOrDefault().Id);
        }

        [Fact]
        public void InternalHasValidationResult_CreateValidationResult_IdentifierMatchValidTemplate()
        {
            // Arrange
            var resource = CreateResource("https://pid.bayer.com/DINOS/4113458a-12a2-4f26-9ff7-1d8fb18b25ec", "https://pid.bayer.com/kos/19050#14d9eeb8-d85d-446d-9703-3a0f43482f5a");

            var pidUriTemplateFlattened = new PidUriTemplateFlattenedBuilder().GenerateSampleData().Build();

            var consumerGroup = new ConsumerGroupBuilder()
                .GenerateSampleData()
                .WithId($"{Graph.Metadata.Constants.Entity.IdPrefix}{new Guid()}")
                .BuildResultDTO();

            _consumerGroupService.Setup(s => s.GetEntity(It.IsAny<string>())).Returns(consumerGroup);

            SetupPidUriTemplateService(new List<PidUriTemplateFlattened>() { pidUriTemplateFlattened });
            SetupPidUriGenerationService();

            EntityValidationFacade validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, consumerGroup.Id);

            // Act
            _validator.HasValidationResult(validationFacade, GetPidUriProperty(resource));

            // Assert
            Entity identifier = validationFacade.RequestResource.Properties[Graph.Metadata.Constants.EnterpriseCore.PidUri].FirstOrDefault();
            Assert.Contains(Graph.Metadata.Constants.Identifier.HasUriTemplate, identifier.Properties);
            var template = identifier.Properties[Graph.Metadata.Constants.Identifier.HasUriTemplate];
            Assert.Contains("https://pid.bayer.com/kos/19050#14d9eeb8-d85d-446d-9703-3a0f43482f5a", template);
        }

        [Fact]
        public void InternalHasValidationResult_CreateValidationResult_IdentifierMatchForbiddenTemplate()
        {
            // Arrange
            var resource = CreateResource("https://pid.bayer.com/DINOS/4113458a-12a2-4f26-9ff7-1d8fb18b25ec");
            var expectedMessage = Graph.Metadata.Constants.Messages.Identifier.MatchForbiddenTemplate;
            var pidUriTemplateFlattened = new PidUriTemplateFlattenedBuilder().GenerateSampleData().Build();

            var consumerGroup = new ConsumerGroupBuilder()
                .GenerateSampleData()
                .WithPidUriTemplate("https://pid.bayer.com/kos/19050#9e8fb007-9cfd-4cf9-9084-2412cd999999")
                .BuildResultDTO();

            _consumerGroupService.Setup(s => s.GetEntity(It.IsAny<string>())).Returns(consumerGroup);

            SetupPidUriTemplateService(new List<PidUriTemplateFlattened>() { pidUriTemplateFlattened });
            SetupPidUriGenerationService();

            EntityValidationFacade validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetPidUriProperty(resource));

            // Assert
            AssertValidationResult(validationFacade, expectedMessage);
        }

        [Fact]
        public void InternalHasValidationResult_CreateValidationResult_InvalidTemplate()
        {
            // Arrange
            var resource = CreateResource(null, "https://pid.bayer.com/kos/19050#9e8fb007-9cfd-4cf9-9084-2412cd999999");
            var expectedMessage = RegistrationService.Common.Constants.Messages.PidUriTemplate.NotExists;
            var pidUriTemplateFlattened = new PidUriTemplateFlattenedBuilder().GenerateSampleData().Build();

            var consumerGroup = new ConsumerGroupBuilder()
                .GenerateSampleData()
                .WithId($"{Graph.Metadata.Constants.Entity.IdPrefix}{new Guid()}")
                .BuildResultDTO();

            _consumerGroupService.Setup(s => s.GetEntity(It.IsAny<string>())).Returns(consumerGroup);
            _pidUriTemplateService.Setup(t => t.GetFlatIdentifierTemplateById(It.IsAny<string>())).Returns((PidUriTemplateFlattened)null);

            SetupPidUriTemplateService();
            SetupPidUriGenerationService();

            EntityValidationFacade validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, consumerGroup.Id);

            // Act
            _validator.HasValidationResult(validationFacade, GetPidUriProperty(resource));

            // Assert
            AssertValidationResult(validationFacade, expectedMessage);
        }

        [Fact]
        public void InternalHasValidationResult_CreateValidationResult_ForbiddenTemplate()
        {
            // Arrange
            var pidUriTemplateFlattened = new PidUriTemplateFlattenedBuilder().GenerateSampleData().Build();
            var resource = CreateResource(null, pidUriTemplateFlattened.Id);
            var expectedMessage = RegistrationService.Common.Constants.Messages.PidUriTemplate.ForbiddenTemplate;

            var consumerGroup = new ConsumerGroupBuilder()
                .GenerateSampleData()
                .WithPidUriTemplate("https://pid.bayer.com/kos/19050#9e8fb007-9cfd-4cf9-9084-2412cd999999")
                .BuildResultDTO();

            _consumerGroupService.Setup(s => s.GetEntity(It.IsAny<string>())).Returns(consumerGroup);
            _pidUriTemplateService.Setup(t => t.GetFlatIdentifierTemplateById(It.IsAny<string>())).Returns(pidUriTemplateFlattened);

            SetupPidUriTemplateService();
            SetupPidUriGenerationService();

            EntityValidationFacade validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetPidUriProperty(resource));

            // Assert
            AssertValidationResult(validationFacade, expectedMessage);
        }

        [Fact]
        public void InternalHasValidationResult_CreateValidationResult_MatchTemplateFailed()
        {
            // Arrange
            var pidUriTemplateFlattened = new PidUriTemplateFlattenedBuilder().GenerateSampleData().Build();
            var resource = CreateResource("https://pid.bayer.com/Indigo/4113458a-12a2-4f26-9ff7-1d8fb18b25ec", pidUriTemplateFlattened.Id);
            var expectedMessage = RegistrationService.Common.Constants.Messages.PidUriTemplate.MatchedFailed;

            var consumerGroup = new ConsumerGroupBuilder()
                .GenerateSampleData()
                .BuildResultDTO();

            _consumerGroupService.Setup(s => s.GetEntity(It.IsAny<string>())).Returns(consumerGroup);
            _pidUriTemplateService.Setup(t => t.GetFlatIdentifierTemplateById(It.IsAny<string>())).Returns(pidUriTemplateFlattened);

            SetupPidUriTemplateService();
            SetupPidUriGenerationService();

            EntityValidationFacade validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetPidUriProperty(resource));

            // Assert
            AssertValidationResult(validationFacade, expectedMessage);
        }

        private void AssertValidationResult(EntityValidationFacade validationFacade, string expectedMessage)
        {
            Assert.Contains(Graph.Metadata.Constants.EnterpriseCore.PidUri, validationFacade.RequestResource.Properties);
            Assert.Equal(1, validationFacade.ValidationResults.Count);

            var validationResult = validationFacade.ValidationResults.FirstOrDefault();

            Assert.Equal(expectedMessage, validationResult.Message);
            Assert.Equal(ValidationResultSeverity.Violation, validationResult.ResultSeverity);
        }

        private KeyValuePair<string, List<dynamic>> GetPidUriProperty(Resource resource)
        {
            return resource.Properties.SingleOrDefault(p => p.Key == Graph.Metadata.Constants.EnterpriseCore.PidUri);
        }

        private Resource CreateResource(string optionalPidUri = null, string uriTemplate = null)
        {
            return new ResourceBuilder()
                .GenerateSampleData()
                .WithPidUri(optionalPidUri, uriTemplate)
                .Build();
        }
    }
}
