using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.Tests.Builder;
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
        private readonly Mock<IKeywordService> _keywordServiceMock;
        private readonly KeywordValidator _validator;
        private readonly IList<MetadataProperty> _metadata;

        public LinkTypesValidatorTests()
        {
            _keywordServiceMock = new Mock<IKeywordService>();
            _validator = new KeywordValidator(_keywordServiceMock.Object);

            _metadata = new MetadataBuilder().GenerateSampleKeyword().Build();
        }

        [Fact]
        public void InternalHasValidationResult_SingleKeywordAlreadyInCorrectFormat()
        {
            const string keyword = "https://pid.bayer.com/kos/19050#11111111-1111-1111-1111-111111111111";
            string createdKeywordId;

            // Arrange
            _keywordServiceMock.Setup(m => m.CheckIfKeywordExists(It.IsAny<string>(), out createdKeywordId)).Returns(true).Verifiable();
            _keywordServiceMock.Setup(m => m.CreateKeyword(It.IsAny<string>())).Returns(It.IsAny<string>()).Verifiable();

            var resource = CreateResourceWithKeyword(keyword);
            var validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetKeywordProperty(resource));

            // Assert
            Assert.Contains(Graph.Metadata.Constants.Resource.Keyword, validationFacade.RequestResource.Properties);
            Assert.Single(validationFacade.RequestResource.Properties[Graph.Metadata.Constants.Resource.Keyword]);
            Assert.Equal(keyword, validationFacade.RequestResource.Properties[Graph.Metadata.Constants.Resource.Keyword].First());
            Assert.Equal(0, validationFacade.ValidationResults.Count);

            _keywordServiceMock.Verify(m => m.CheckIfKeywordExists(keyword, out createdKeywordId), Times.Never);
            _keywordServiceMock.Verify(m => m.CreateKeyword(keyword), Times.Never);
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
            // Arrange
            string createdKeywordId;
            _keywordServiceMock.Setup(m => m.CheckIfKeywordExists(It.IsAny<string>(), out createdKeywordId)).Returns(true).Verifiable();
            _keywordServiceMock.Setup(m => m.CreateKeyword(It.IsAny<string>())).Returns(It.IsAny<string>()).Verifiable();

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

            _keywordServiceMock.Verify(m => m.CheckIfKeywordExists(It.IsAny<string>(), out createdKeywordId), Times.Never);
            _keywordServiceMock.Verify(m => m.CreateKeyword(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void InternalHasValidationResult_MultipleKeywordsAlreadyInCorrectFormat()
        {
            var keywordList = new List<dynamic>()
            {
                "https://pid.bayer.com/kos/19050#11111111-1111-1111-1111-111111111111",
                "https://pid.bayer.com/kos/19050#22222222-2222-2222-2222-222222222222",
            };
            string createdKeywordId;

            // Arrange
            _keywordServiceMock.Setup(m => m.CheckIfKeywordExists(It.IsAny<string>(), out createdKeywordId)).Returns(true).Verifiable();
            _keywordServiceMock.Setup(m => m.CreateKeyword(It.IsAny<string>())).Returns(It.IsAny<string>()).Verifiable();

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

            _keywordServiceMock.Verify(m => m.CheckIfKeywordExists(It.IsAny<string>(), out createdKeywordId), Times.Never);
            _keywordServiceMock.Verify(m => m.CreateKeyword(It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData("https://pid.bayer.com/kos/19050#11111111-1111-1111-1111-111111111111", "https://pid.bayer.com/kos/19050#22222222")]
        [InlineData("https://pid.bayer.com/kos/19050#11111111-1111-1111-1111-111111111111", "invalid_uri")]
        public void InternalHasValidationResult_OneKeywordNotUrlFormatAndExists(string validKeywordId, string validKeyword)
        {
            var keywordList = new List<dynamic>() { validKeywordId, validKeyword };
            string existingKeywordId = "https://pid.bayer.com/kos/19050#33333333-3333-3333-3333-333333333333";
            const bool keywordExists = true;

            // Arrange
            _keywordServiceMock.Setup(m => m.CheckIfKeywordExists(It.IsAny<string>(), out existingKeywordId)).Returns(keywordExists).Verifiable();
            _keywordServiceMock.Setup(m => m.CreateKeyword(It.IsAny<string>())).Returns(It.IsAny<string>()).Verifiable();

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
            _keywordServiceMock.Verify(m => m.CheckIfKeywordExists(validKeywordId, out val), Times.Never);
            _keywordServiceMock.Verify(m => m.CheckIfKeywordExists(validKeyword, out existingKeywordId), Times.Once);
            _keywordServiceMock.Verify(m => m.CreateKeyword(It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData("https://pid.bayer.com/kos/19050#11111111-1111-1111-1111-111111111111", "https://pid.bayer.com/kos/19050#22222222")]
        [InlineData("https://pid.bayer.com/kos/19050#11111111-1111-1111-1111-111111111111", "invalid_uri")]
        public void InternalHasValidationResult_OneKeywordNotUrlFormatAndNotExists(string validKeywordId, string invalidKeywordId)
        {
            var keywordList = new List<dynamic>() { validKeywordId, invalidKeywordId };
            string existingKeywordId = string.Empty;
            const bool keywordExists = false;
            const string createdKeywordId = "https://pid.bayer.com/kos/19050#44444444-4444-4444-4444-444444444444";

            // Arrange
            _keywordServiceMock.Setup(m => m.CheckIfKeywordExists(It.IsAny<string>(), out existingKeywordId)).Returns(keywordExists).Verifiable();
            _keywordServiceMock.Setup(m => m.CreateKeyword(It.IsAny<string>())).Returns(createdKeywordId).Verifiable();

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
            _keywordServiceMock.Verify(m => m.CheckIfKeywordExists(validKeywordId, out val), Times.Never);
            _keywordServiceMock.Verify(m => m.CheckIfKeywordExists(invalidKeywordId, out existingKeywordId), Times.Once);
            _keywordServiceMock.Verify(m => m.CreateKeyword(validKeywordId), Times.Never);
            _keywordServiceMock.Verify(m => m.CreateKeyword(invalidKeywordId), Times.Once);
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
