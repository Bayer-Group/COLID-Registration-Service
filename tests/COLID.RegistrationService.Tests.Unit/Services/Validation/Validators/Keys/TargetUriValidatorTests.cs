using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.Tests.Builder;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.DataModels.Resources;
using COLID.RegistrationService.Services.Validation.Models;
using COLID.RegistrationService.Services.Validation.Validators.Keys;
using COLID.RegistrationService.Tests.Common.Builder;
using Xunit;

namespace COLID.RegistrationService.Tests.Unit.Services.Validation.Validators.Keys
{
    [ExcludeFromCodeCoverage]
    public class TargetUriValidatorTests
    {
        private readonly TargetUriValidator _validator;
        private readonly IList<MetadataProperty> _metadata;

        public TargetUriValidatorTests()
        {
            _validator = new TargetUriValidator();
            _metadata = new MetadataBuilder().GenerateSampleHasNetworkAdress().Build();
        }

        [Fact]
        public void InternalHasValidationResult_Success()
        {
            // Arrange
            Entity entity = CreateEntity("https://www.google.com");
            Resource resource = CreateResourceWithEndpoint(entity);
            EntityValidationFacade validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetNetworkAdressProperty(entity));

            // Assert
            Assert.Equal(0, validationFacade.ValidationResults.Count);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("https://www.google.com/ with spaces")]
        public void InternalHasValidationResult_CreateValidationResults_NullOrEmptyPidUri(string networkAdress)
        {
            // Arrange
            Entity entity = CreateEntity(networkAdress);
            Resource resource = CreateResourceWithEndpoint(entity);
            EntityValidationFacade validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetNetworkAdressProperty(entity));

            // Assert
            Assert.Equal(1, validationFacade.ValidationResults.Count);
        }

        private KeyValuePair<string, List<dynamic>> GetNetworkAdressProperty(Entity resource)
        {
            return resource.Properties.SingleOrDefault(p => p.Key == Graph.Metadata.Constants.Resource.DistributionEndpoints.HasNetworkAddress);
        }

        private Entity CreateEntity(string networkAdress)
        {
            Entity entity = new DistributionEndpointBuilder()
                .GenerateSampleData()
                .WithNetworkAddress(networkAdress)
                .Build();

            return entity;
        }

        private Resource CreateResourceWithEndpoint(Entity endpoint)
        {
            var resource = new ResourceBuilder()
                .GenerateSampleData()
                .WithDistributionEndpoint(endpoint)
                .Build();

            return resource;
        }
    }
}
