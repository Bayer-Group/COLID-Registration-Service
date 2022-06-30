using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.Tests.Builder;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.DataModels.Resources;
using COLID.Graph.TripleStore.Extensions;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.Services.Validation.Models;
using COLID.RegistrationService.Services.Validation.Validators.Keys;
using COLID.RegistrationService.Tests.Common.Builder;
using Moq;
using Xunit;

namespace COLID.RegistrationService.Tests.Unit.Services.Validation.Validators.Keys
{
    [ExcludeFromCodeCoverage]
    public class LinkTypesValidatorTests
    {
        private readonly Mock<IEntityService> _entityServiceMock;
        private readonly ExtendableListValidator _validator;
        private readonly IList<MetadataProperty> _metadata;
        private readonly Uri _labelUri;
        public LinkTypesValidatorTests()
        {
            _entityServiceMock = new Mock<IEntityService>();
            _validator = new ExtendableListValidator(_entityServiceMock.Object);

            _metadata = new MetadataBuilder().GenerateSampleKeyword().Build();

            _labelUri = new Uri(Graph.Metadata.Constants.RDFS.Label);
        }

        [Fact]
        public void InternalHasValidationResult_SingleKeywordAlreadyInCorrectFormat()
        {
            const string keyword = "https://pid.bayer.com/kos/19050#11111111-1111-1111-1111-111111111111";
            const string type = COLID.Graph.Metadata.Constants.Keyword.Type;
            var entityResult = new BaseEntityResultCTO() { Entity = new BaseEntityResultDTO() { Id = keyword } };

            string createdKeywordId;

            // Arrange
            _entityServiceMock.Setup(m => m.CheckIfPropertyValueExists(_labelUri, It.IsAny<string>(),type, out createdKeywordId)).Returns(true).Verifiable();
            _entityServiceMock.Setup(m => m.CreateEntity(It.IsAny<BaseEntityRequestDTO>())).ReturnsAsync(entityResult).Verifiable();

            var resource = CreateResourceWithKeyword(keyword);
            var validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetKeywordProperty(resource));

            // Assert
            Assert.Contains(Graph.Metadata.Constants.Resource.Keyword, validationFacade.RequestResource.Properties);
            Assert.Single(validationFacade.RequestResource.Properties[Graph.Metadata.Constants.Resource.Keyword]);
            Assert.Equal(keyword, validationFacade.RequestResource.Properties[Graph.Metadata.Constants.Resource.Keyword].First());
            Assert.Equal(0, validationFacade.ValidationResults.Count);

            _entityServiceMock.Verify(m => m.CheckIfPropertyValueExists(_labelUri, keyword, type, out createdKeywordId), Times.Never);
            _entityServiceMock.Verify(m => m.CreateEntity(It.IsAny<BaseEntityRequestDTO>()), Times.Never);
        }

        [Fact]
        public void InternalHasValidationResult_Failed_SingleKeywordIsNull()
        {
            const string keyword = null;

            // Arrange
            var resource = CreateResourceWithKeyword(keyword);
            var validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);

            // Act AND ASSERT
            Assert.Throws<ArgumentNullException>(() => _validator.HasValidationResult(validationFacade, GetKeywordProperty(resource)));
        }

        [Fact]
        public void InternalHasValidationResult_NoKeywordInResource()
        {
            const string type = COLID.Graph.Metadata.Constants.Keyword.Type;

            // Arrange
            string createdKeywordId;
            _entityServiceMock.Setup(m => m.CheckIfPropertyValueExists(_labelUri, It.IsAny<string>(), type, out createdKeywordId)).Returns(true).Verifiable();
            _entityServiceMock.Setup(m => m.CreateEntity(It.IsAny<BaseEntityRequestDTO>())).ReturnsAsync(It.IsAny<BaseEntityResultCTO>()).Verifiable();

            var resource = new ResourceBuilder()
                .GenerateSampleData()
                .WithPidUri($"https://pid.bayer.com/kos/0308eeb4-ed33-43b8-abf7-599a57cbd718")
                .Build();
            resource.Properties.Remove(Graph.Metadata.Constants.Resource.Keyword);

            var validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetKeywordProperty(resource));

            // Assert
            Assert.DoesNotContain(Graph.Metadata.Constants.Resource.Keyword, validationFacade.RequestResource.Properties);
            Assert.Equal(0, validationFacade.ValidationResults.Count);

            _entityServiceMock.Verify(m => m.CheckIfPropertyValueExists(_labelUri, It.IsAny<string>(), type, out createdKeywordId), Times.Never);
            _entityServiceMock.Verify(m => m.CreateEntity(It.IsAny<BaseEntityRequestDTO>()), Times.Never);
        }

        [Fact]
        public void InternalHasValidationResult_MultipleKeywordsAlreadyInCorrectFormat()
        {
            const string type = COLID.Graph.Metadata.Constants.Keyword.Type;

            var keywordList = new List<dynamic>()
            {
                "https://pid.bayer.com/kos/19050#11111111-1111-1111-1111-111111111111",
                "https://pid.bayer.com/kos/19050#22222222-2222-2222-2222-222222222222",
            };
            string createdKeywordId;

            // Arrange
            _entityServiceMock.Setup(m => m.CheckIfPropertyValueExists(_labelUri, It.IsAny<string>(), type, out createdKeywordId)).Returns(true).Verifiable();
            _entityServiceMock.Setup(m => m.CreateEntity(It.IsAny<BaseEntityRequestDTO>())).ReturnsAsync(It.IsAny<BaseEntityResultCTO>()).Verifiable();

            var resource = CreateResourceWithKeyword(keywordList);
            var validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetKeywordProperty(resource));

            // Assert
            Assert.Contains(Graph.Metadata.Constants.Resource.Keyword, validationFacade.RequestResource.Properties);
            Assert.Equal(2, validationFacade.RequestResource.Properties[Graph.Metadata.Constants.Resource.Keyword].Count);
            Assert.Equal(keywordList[0], validationFacade.RequestResource.Properties[Graph.Metadata.Constants.Resource.Keyword][0]);
            Assert.Equal(keywordList[1], validationFacade.RequestResource.Properties[Graph.Metadata.Constants.Resource.Keyword][1]);
            Assert.Equal(0, validationFacade.ValidationResults.Count);

            _entityServiceMock.Verify(m => m.CheckIfPropertyValueExists(_labelUri, It.IsAny<string>(), type, out createdKeywordId), Times.Never);
            _entityServiceMock.Verify(m => m.CreateEntity(It.IsAny<BaseEntityRequestDTO>()), Times.Never);
        }

        [Theory]
        [InlineData("https://pid.bayer.com/kos/19050#11111111-1111-1111-1111-111111111111", "https://pid.bayer.com/kos/19050#22222222")]
        [InlineData("https://pid.bayer.com/kos/19050#11111111-1111-1111-1111-111111111111", "invalid_uri")]
        public void InternalHasValidationResult_OneKeywordNotUrlFormatAndExists(string validKeywordId, string validKeyword)
        {
            const string type = COLID.Graph.Metadata.Constants.Keyword.Type;

            var keywordList = new List<dynamic>() { validKeywordId, validKeyword };
            string existingKeywordId = "https://pid.bayer.com/kos/19050#33333333-3333-3333-3333-333333333333";
            const bool keywordExists = true;

            // Arrange
            _entityServiceMock.Setup(m => m.CheckIfPropertyValueExists(_labelUri, It.IsAny<string>(), type, out existingKeywordId)).Returns(keywordExists).Verifiable();
            _entityServiceMock.Setup(m => m.CreateEntity(It.IsAny<BaseEntityRequestDTO>())).ReturnsAsync(It.IsAny<BaseEntityResultCTO>()).Verifiable();

            var resource = CreateResourceWithKeyword(keywordList);
            var validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetKeywordProperty(resource));

            // Assert
            Assert.Contains(Graph.Metadata.Constants.Resource.Keyword, validationFacade.RequestResource.Properties);
            Assert.Equal(2, validationFacade.RequestResource.Properties[Graph.Metadata.Constants.Resource.Keyword].Count);
            Assert.Contains(validKeywordId, validationFacade.RequestResource.Properties[Graph.Metadata.Constants.Resource.Keyword]);
            Assert.Contains(existingKeywordId, validationFacade.RequestResource.Properties[Graph.Metadata.Constants.Resource.Keyword]);
            Assert.DoesNotContain(validKeyword, validationFacade.RequestResource.Properties[Graph.Metadata.Constants.Resource.Keyword]);
            Assert.Equal(0, validationFacade.ValidationResults.Count);

            string val;
            _entityServiceMock.Verify(m => m.CheckIfPropertyValueExists(_labelUri, validKeywordId, type, out val), Times.Never);
            _entityServiceMock.Verify(m => m.CheckIfPropertyValueExists(_labelUri, validKeyword, type, out existingKeywordId), Times.Once);
            _entityServiceMock.Verify(m => m.CreateEntity(It.IsAny<BaseEntityRequestDTO>()), Times.Never);
        }

        [Theory]
        [InlineData("https://pid.bayer.com/kos/19050#11111111-1111-1111-1111-111111111111", "https://pid.bayer.com/kos/19050#22222222")]
        [InlineData("https://pid.bayer.com/kos/19050#11111111-1111-1111-1111-111111111111", "invalid_uri")]
        public void InternalHasValidationResult_OneKeywordNotUrlFormatAndNotExists(string validKeywordId, string invalidKeywordId)
        {
            const string type = COLID.Graph.Metadata.Constants.Keyword.Type;

            var keywordList = new List<dynamic>() { validKeywordId, invalidKeywordId };
            string existingKeywordId = string.Empty;
            const bool keywordExists = false;
            const string createdKeywordId = "https://pid.bayer.com/kos/19050#44444444-4444-4444-4444-444444444444";
            var entityResult = new BaseEntityResultCTO() { Entity = new BaseEntityResultDTO() { Id = createdKeywordId } };

            // Arrange
            _entityServiceMock.Setup(m => m.CheckIfPropertyValueExists(_labelUri, It.IsAny<string>(), type, out existingKeywordId)).Returns(keywordExists).Verifiable();
            _entityServiceMock.Setup(m => m.CreateEntity(It.IsAny<BaseEntityRequestDTO>())).ReturnsAsync(entityResult).Verifiable();

            var resource = CreateResourceWithKeyword(keywordList);
            var validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetKeywordProperty(resource));

            // Assert
            Assert.Contains(Graph.Metadata.Constants.Resource.Keyword, validationFacade.RequestResource.Properties);
            Assert.Equal(2, validationFacade.RequestResource.Properties[Graph.Metadata.Constants.Resource.Keyword].Count);
            Assert.Contains(validKeywordId, validationFacade.RequestResource.Properties[Graph.Metadata.Constants.Resource.Keyword]);
            Assert.Contains(createdKeywordId, validationFacade.RequestResource.Properties[Graph.Metadata.Constants.Resource.Keyword]);
            Assert.DoesNotContain(invalidKeywordId, validationFacade.RequestResource.Properties[Graph.Metadata.Constants.Resource.Keyword]);
            Assert.Equal(0, validationFacade.ValidationResults.Count);

            string val;
            _entityServiceMock.Verify(m => m.CheckIfPropertyValueExists(_labelUri, validKeywordId, type, out val), Times.Never);
            _entityServiceMock.Verify(m => m.CheckIfPropertyValueExists(_labelUri, invalidKeywordId, type, out existingKeywordId), Times.Once);
            _entityServiceMock.Verify(m => m.CreateEntity(It.IsAny<BaseEntityRequestDTO>()), Times.Once);
        }

        private KeyValuePair<string, List<dynamic>> GetKeywordProperty(Resource resource)
        {
            return resource.Properties.SingleOrDefault(p => p.Key == Graph.Metadata.Constants.Resource.Keyword);
        }

        private Resource CreateResourceWithKeyword(string keyword = null)
        {
            var resource = new ResourceBuilder()
                .GenerateSampleData()
                .WithKeyword(keyword)
                .WithPidUri($"https://pid.bayer.com/kos/0308eeb4-ed33-43b8-abf7-599a57cbd718")
                .Build();

            return resource;
        }

        private Resource CreateResourceWithKeyword(List<dynamic> keywords)
        {
            var resourceBuilder = new ResourceBuilder()
                .GenerateSampleData()
                .WithPidUri($"https://pid.bayer.com/kos/0308eeb4-ed33-43b8-abf7-599a57cbd718");

            var resource = resourceBuilder.Build();

            // The method WithKeyword does not allow multiple values, so set them after resource has been built
            resource.Properties.AddOrUpdate(Graph.Metadata.Constants.Resource.Keyword, keywords);

            return resource;
        }
    }
}
