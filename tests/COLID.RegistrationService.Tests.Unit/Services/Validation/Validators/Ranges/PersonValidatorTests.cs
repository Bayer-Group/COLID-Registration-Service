using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.Tests.Builder;
using COLID.Graph.TripleStore.DataModels.Resources;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.Services.Validation.Models;
using COLID.RegistrationService.Services.Validation.Validators.Ranges;
using COLID.RegistrationService.Tests.Common.Builder;
using Moq;
using Xunit;

namespace COLID.RegistrationService.Tests.Unit.Services.Validation.Validators.Ranges
{
    [ExcludeFromCodeCoverage]
    public class PersonValidatorTests
    {
        private readonly Mock<IRemoteAppDataService> _remoteAppDataService;
        private readonly PersonValidator _validator;

        private readonly IList<MetadataProperty> _metadata;

        public PersonValidatorTests()
        {
            _remoteAppDataService = new Mock<IRemoteAppDataService>();
            _validator = new PersonValidator(_remoteAppDataService.Object);
            _metadata = new MetadataBuilder().GenerateSampleAuthor().Build();
        }


        private void SetupRemoteAppDataService(bool exists)
        {
            _remoteAppDataService.Setup(ex => ex.CheckPerson(It.IsAny<string>())).Returns(It.IsAny<bool>());
        }

        [Theory]
        [InlineData("")]
        [InlineData("invalid-text")]
        [InlineData("invalid-text-with-!§$%&")]
        [InlineData("peter.russel@bayer.com")]
        public void InternalHasValidationResult_CustomIdentifier_Successfully(string author)
        {
            // Arrange
            var resource = CreateResource(author);

            SetupRemoteAppDataService(true);

            EntityValidationFacade validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetAuthorProperty(resource));

            // Assert
            Assert.Contains(Graph.Metadata.Constants.Resource.Author, validationFacade.RequestResource.Properties);
            Assert.Equal(1, validationFacade.ValidationResults.Count);

            string currentAuthor = validationFacade.RequestResource.Properties.SingleOrDefault(p => p.Key == Graph.Metadata.Constants.Resource.Author).Value[0];

            Assert.Equal(author, currentAuthor);
        }

        [Theory]
        [InlineData(null)]
        public void InternalHasValidationResult_Null_Successfully(string author)
        {
            // Arrange
            var resource = CreateResource(author);

            SetupRemoteAppDataService(true);

            EntityValidationFacade validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetAuthorProperty(resource));

            // Assert
            Assert.Contains(Graph.Metadata.Constants.Resource.Author, validationFacade.RequestResource.Properties);
            Assert.Equal(0, validationFacade.ValidationResults.Count);

            string currentAuthor = validationFacade.RequestResource.Properties.SingleOrDefault(p => p.Key == Graph.Metadata.Constants.Resource.Author).Value[0];

            Assert.Equal(author, currentAuthor);
        }

        [Theory]
        [InlineData("peter.russel@bayer.com")]
        [InlineData("adam.johnson@bayer.com")]
        public void InternalHasValidationResult_CreateValidationResult_PersonNotExists(string author)
        {
            // Arrange
            var resource = CreateResource(author);
            var expectedMessage = string.Format(RegistrationService.Common.Constants.Messages.Person.PersonNotFound, author);

            SetupRemoteAppDataService(false);

            EntityValidationFacade validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetAuthorProperty(resource));

            // Assert
            Assert.Contains(Graph.Metadata.Constants.Resource.Author, validationFacade.RequestResource.Properties);
            Assert.Equal(1, validationFacade.ValidationResults.Count);

            string currentAuthor = validationFacade.RequestResource.Properties.SingleOrDefault(p => p.Key == Graph.Metadata.Constants.Resource.Author).Value[0];
            Assert.Equal(author, currentAuthor);

            var validationResult = validationFacade.ValidationResults.FirstOrDefault();
            Assert.Equal(expectedMessage, validationResult.Message);
            Assert.Equal(ValidationResultSeverity.Violation, validationResult.ResultSeverity);
        }

        private KeyValuePair<string, List<dynamic>> GetAuthorProperty(Resource resource)
        {
            return resource.Properties.SingleOrDefault(p => p.Key == Graph.Metadata.Constants.Resource.Author);
        }

        private KeyValuePair<string, List<dynamic>> GetLastChangeUserProperty(Resource resource)
        {
            return resource.Properties.SingleOrDefault(p => p.Key == Graph.Metadata.Constants.Resource.Author);
        }

        private Resource CreateResource(string author)
        {
            return new ResourceBuilder()
                .GenerateSampleData()
                .WithAuthor(author)
                .Build();
        }
    }
}
