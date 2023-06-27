using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.Tests.Builder;
using COLID.Graph.TripleStore.DataModels.Resources;
using COLID.RegistrationService.Services.Validation.Models;
using COLID.RegistrationService.Services.Validation.Validators.Keys;
using COLID.RegistrationService.Tests.Common.Builder;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace COLID.RegistrationService.Tests.Unit.Services.Validation.Validators.Keys
{
    [ExcludeFromCodeCoverage]
    public class PidUriValidatorTests
    {
        private readonly PidUriValidator _validator;
        private readonly IList<MetadataProperty> _metadata;

        public PidUriValidatorTests()
        {
            var testingConfig = new Dictionary<string, string>
            {
                {"ConnectionStrings:colidDomain", "pid.bayer.com"},
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(testingConfig)
                .Build();

            _validator = new PidUriValidator(configuration);
            _metadata = new MetadataBuilder().GenerateSamplePidUri().Build();
        }

        [Fact]
        public void InternalHasValidationResult_Success()
        {
            // Arrange
            Resource resource = CreateResource($"https://pid.bayer.com/kos/0308eeb4-ed33-43b8-abf7-599a57cbd718");
            EntityValidationFacade validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetPidUriProperty(resource));

            // Assert
            Assert.Contains(Graph.Metadata.Constants.EnterpriseCore.PidUri, validationFacade.RequestResource.Properties);
            Assert.Equal(0, validationFacade.ValidationResults.Count);
        }

        [Theory]
        [InlineData("https://dmp.bayer.com/kos/19014", "URI has to start with the prefix: pid.bayer.com.")] // Starts with different prefix
        [InlineData("https://pid.bayer.com", "The URI only contains the prefix: pid.bayer.com.")] // Has no absolute path
        [InlineData("https://pid.bayer.com/kos/pid.bayer.com", "The URI contains several times the prefix: pid.bayer.com.")] // Has prefix for several times
        public void InternalHasValidationResult_CreateValidationResults(string pidUri, string expectedMessage)
        {
            // Arrange
            Resource resource = CreateResource(pidUri);
            EntityValidationFacade validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetPidUriProperty(resource));

            // Assert
            Assert.Contains(Graph.Metadata.Constants.EnterpriseCore.PidUri, validationFacade.RequestResource.Properties);
            Assert.Equal(1, validationFacade.ValidationResults.Count);

            var validationResult = validationFacade.ValidationResults.FirstOrDefault();

            Assert.Equal(expectedMessage, validationResult.Message);
            Assert.Equal(ValidationResultSeverity.Violation,validationResult.ResultSeverity);
        }

        private KeyValuePair<string, List<dynamic>> GetPidUriProperty(Resource resource)
        {
            return resource.Properties.SingleOrDefault(p => p.Key == Graph.Metadata.Constants.EnterpriseCore.PidUri);
        }

        private Resource CreateResource(string optionalPidUri = null)
        {
            Resource resource = new ResourceBuilder()
                .GenerateSampleData()
                .WithPidUri(optionalPidUri, null)
                .Build();

            return resource;
        }
    }
}
