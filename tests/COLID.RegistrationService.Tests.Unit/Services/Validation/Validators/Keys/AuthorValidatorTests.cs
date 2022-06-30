using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.Tests.Builder;
using COLID.Graph.TripleStore.DataModels.Resources;
using COLID.RegistrationService.Services.Authorization.UserInfo;
using COLID.RegistrationService.Services.Validation.Models;
using COLID.RegistrationService.Services.Validation.Validators.Keys;
using COLID.RegistrationService.Tests.Common.Builder;
using Moq;
using Xunit;

namespace COLID.RegistrationService.Tests.Unit.Services.Validation.Validators.Keys
{
    [ExcludeFromCodeCoverage]
    public class AuthorValidatorTests
    {
        private readonly Mock<IUserInfoService> _mockUserInfoService;
        private readonly AuthorValidator _validator;
        private readonly IList<MetadataProperty> _metadata;

        public AuthorValidatorTests()
        {
            _mockUserInfoService = new Mock<IUserInfoService>();
            _validator = new AuthorValidator(_mockUserInfoService.Object);

            _metadata = new MetadataBuilder().GenerateSampleAuthor().Build();
        }

        [Fact]
        public void InternalHasValidationResult_Success_NoReplacement()
        {
            // Arrange
            _mockUserInfoService.Setup(mock => mock.HasApiToApiPrivileges()).Returns(true);

            var author = "jake.peralta@nypd.com";
            Resource resource = CreateResourceWithAuthor(author);
            EntityValidationFacade validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetAuthorProperty(resource));

            // Assert
            AssertValidationFacade(validationFacade, author);
        }

        [Fact]
        public void InternalHasValidationResult_Success_ReplaceAuthorWithEmailOnCreate()
        {
            // Arrange
            var author = "nameWithoutMail";
            var mail = author + "@bayer.com";
            _mockUserInfoService.Setup(mock => mock.GetRoles()).Returns(new List<string>());
            _mockUserInfoService.Setup(mock => mock.GetEmail()).Returns(mail);
            Resource resource = CreateResourceWithAuthor(author);
            EntityValidationFacade validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetAuthorProperty(resource));

            // Assert
            AssertValidationFacade(validationFacade, mail);
        }

        [Fact]
        public void InternalHasValidationResult_Success_ReplaceAuthorWithEmailOnUpdate()
        {
            // Arrange
            _mockUserInfoService.Setup(mock => mock.GetRoles()).Returns(new List<string>());
            Resource newResource = CreateResourceWithAuthor("peter.griffin@bayer.com");
            Resource repoResource = CreateResourceWithAuthor("jack.sparrow@bayer.com");
            ResourcesCTO repoResourceCTO = new ResourcesCTO() { Draft = repoResource, Published = repoResource };

            EntityValidationFacade validationFacade = new EntityValidationFacade(ResourceCrudAction.Update, newResource, repoResourceCTO, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetAuthorProperty(newResource));

            // Assert
            AssertValidationFacade(validationFacade, "jack.sparrow@bayer.com");
        }

        private void AssertValidationFacade(EntityValidationFacade validationFacade, string email)
        {
            Assert.Contains(Graph.Metadata.Constants.Resource.Author, validationFacade.RequestResource.Properties);
            Assert.Equal(0, validationFacade.ValidationResults.Count);

            var authorValue = GetAuthorProperty(validationFacade.RequestResource).Value;

            Assert.Single(authorValue);
            Assert.Contains(authorValue, u => u == email);
        }

        private KeyValuePair<string, List<dynamic>> GetAuthorProperty(Resource resource)
        {
            return resource.Properties.SingleOrDefault(p => p.Key == Graph.Metadata.Constants.Resource.Author);
        }

        private Resource CreateResourceWithAuthor(string author = null)
        {
            Resource resource = new ResourceBuilder()
                .GenerateSampleData()
                .WithAuthor(author)
                .WithPidUri($"https://pid.bayer.com/kos/0308eeb4-ed33-43b8-abf7-599a57cbd718")
                .Build();

            return resource;
        }
    }
}
