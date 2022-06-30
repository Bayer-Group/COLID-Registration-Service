using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.Tests.Builder;
using COLID.Graph.TripleStore.DataModels.Resources;
using COLID.RegistrationService.Common.Enums.DistributionEndpoint;
using COLID.RegistrationService.Services.Validation.Models;
using COLID.RegistrationService.Services.Validation.Validators.Keys;
using COLID.RegistrationService.Tests.Common.Builder;
using Xunit;

namespace COLID.RegistrationService.Tests.Unit.Services.Validation.Validators.Keys
{
    [ExcludeFromCodeCoverage]
    public class MainDistributionEndpointValidatorTests
    {
        private readonly MainDistributionEndpointValidator _validator;
        private readonly IList<MetadataProperty> _metadata;

        public MainDistributionEndpointValidatorTests()
        {
            _validator = new MainDistributionEndpointValidator();
            _metadata = new MetadataBuilder().GenerateSampleMainDistributionEndpoint().Build();
        }

        [Fact]
        public void InternalHasValidationResult_Success()
        {
            // Arrange
            Resource resource = CreateResourceWithEndpointLifecycleStatus(LifecycleStatus.Active);
            EntityValidationFacade validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetMainDistributionEndpointProperty(resource));

            // Assert
            Assert.Contains(Graph.Metadata.Constants.Resource.MainDistribution, validationFacade.RequestResource.Properties);
            Assert.Equal(0, validationFacade.ValidationResults.Count);
        }

        [Fact]
        public void InternalHasValidationResult_CreateValidationResult_DeprecatedpLifecycleStatus()
        {
            // Arrange
            Resource resource = CreateResourceWithEndpointLifecycleStatus(LifecycleStatus.Deprecated);
            EntityValidationFacade validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetMainDistributionEndpointProperty(resource));

            // Assert
            Assert.Contains(Graph.Metadata.Constants.Resource.MainDistribution, validationFacade.RequestResource.Properties);
            Assert.Equal(1, validationFacade.ValidationResults.Count);
        }

        private KeyValuePair<string, List<dynamic>> GetMainDistributionEndpointProperty(Resource resource)
        {
            return resource.Properties.SingleOrDefault(p => p.Key == Graph.Metadata.Constants.Resource.MainDistribution);
        }

        private Resource CreateResourceWithEndpointLifecycleStatus(LifecycleStatus endpointStatus)
        {
            var firstEndpoint = new DistributionEndpointBuilder()
               .GenerateSampleData()
               .WithDistributionEndpointLifecycleStatus(endpointStatus)
               .Build();

            var resource = new ResourceBuilder()
                .GenerateSampleData()
                .WithMainDistributionEndpoint(firstEndpoint)
                .Build();

            return resource;
        }
    }
}
