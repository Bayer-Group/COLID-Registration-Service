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
    public class LastChangeUserValidatorTests
    {
        private readonly Mock<IUserInfoService> _mockUserInfoService;
        private readonly LastChangeUserValidator _validator;

        private readonly IList<MetadataProperty> _metadata;

        public LastChangeUserValidatorTests()
        {
            _mockUserInfoService = new Mock<IUserInfoService>();
            _validator = new LastChangeUserValidator(_mockUserInfoService.Object);

            _metadata = new MetadataBuilder().GenerateSampleLastChangeUser().Build();
        }

        [Fact]
        public void InternalHasValidationResult_Success_NoReplacement()
        {
            // Arrange
            _mockUserInfoService.Setup(mock => mock.HasApiToApiPrivileges()).Returns(true);

            var lastChangeUser = "jake.peralta@nypd.com";
            Resource resource = CreateResourceWithLastChangeUser(lastChangeUser);
            EntityValidationFacade validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetLastChangeUserProperty(resource));

            // Assert
            AssertValidationFacade(validationFacade, lastChangeUser);
        }

        [Fact]
        public void InternalHasValidationResult_Success_ReplaceAuthorWithEmail()
        {
            // Arrange
            var mail = "peter.griffin@bayer.com";
            _mockUserInfoService.Setup(mock => mock.GetRoles()).Returns(new List<string>());
            _mockUserInfoService.Setup(mock => mock.GetEmail()).Returns(mail);

            var lastChangeUser = "jake.peralta@nypd.com";
            Resource resource = CreateResourceWithLastChangeUser(lastChangeUser);
            EntityValidationFacade validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetLastChangeUserProperty(resource));

            // Assert
            AssertValidationFacade(validationFacade, mail);
        }

        private void AssertValidationFacade(EntityValidationFacade validationFacade, string email)
        {
            Assert.Contains(Graph.Metadata.Constants.Resource.Author, validationFacade.RequestResource.Properties);
            Assert.Equal(0, validationFacade.ValidationResults.Count);

            var lastChangeUserValue = GetLastChangeUserProperty(validationFacade.RequestResource).Value;

            Assert.Single(lastChangeUserValue);
            Assert.Contains(lastChangeUserValue, u => u == email);
        }

        private KeyValuePair<string, List<dynamic>> GetLastChangeUserProperty(Resource resource)
        {
            return resource.Properties.SingleOrDefault(p => p.Key == Graph.Metadata.Constants.Resource.LastChangeUser);
        }

        private Resource CreateResourceWithLastChangeUser(string lastChangeUser = null)
        {
            Resource resource = new ResourceBuilder()
                .GenerateSampleData()
                .WithLastChangeUser(lastChangeUser)
                .WithPidUri($"https://pid.bayer.com/kos/0308eeb4-ed33-43b8-abf7-599a57cbd718")
                .Build();

            return resource;
        }
    }
}
