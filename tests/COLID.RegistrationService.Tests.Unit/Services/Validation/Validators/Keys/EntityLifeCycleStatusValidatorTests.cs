using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.Tests.Builder;
using COLID.Graph.TripleStore.DataModels.Resources;
using COLID.RegistrationService.Common.Enums.ColidEntry;
using COLID.RegistrationService.Services.Validation.Models;
using COLID.RegistrationService.Services.Validation.Validators.Keys;
using COLID.RegistrationService.Tests.Common.Builder;
using Xunit;

namespace COLID.RegistrationService.Tests.Unit.Services.Validation.Validators.Keys
{
    [ExcludeFromCodeCoverage]
    public class EntityLifeCycleStatusValidatorTests
    {
        private readonly EntityLifeCycleStatusValidator _validator;
        private readonly IList<MetadataProperty> _metadata;

        public EntityLifeCycleStatusValidatorTests()
        {
            _validator = new EntityLifeCycleStatusValidator();
            _metadata = new MetadataBuilder().GenerateSampleEntryLifceCycleStatus().Build();
        }

        [Theory]
        [InlineData(ResourceCrudAction.Create, ColidEntryLifecycleStatus.Published, Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Draft)]
        [InlineData(ResourceCrudAction.Update, ColidEntryLifecycleStatus.Published, Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Draft)]
        [InlineData(ResourceCrudAction.Publish, ColidEntryLifecycleStatus.Draft, Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Published)]
        public void InternalHasValidationResult_OverwritePropertyWithNewDateTime(ResourceCrudAction crudAction, ColidEntryLifecycleStatus currentEntryLifecycleStatus, string expectedEntryLifecycleStatus)
        {
            // Arrange
            var resource = new ResourceBuilder()
                .GenerateSampleData()
                .WithEntryLifecycleStatus(currentEntryLifecycleStatus)
                .Build();

            EntityValidationFacade validationFacade = new EntityValidationFacade(crudAction, resource, null, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetEntryLifecycleStatusProperty(resource));

            // Assert
            Assert.Contains(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus, validationFacade.RequestResource.Properties);
            var entryLifecycleStatus = GetEntryLifecycleStatusProperty(validationFacade.RequestResource).Value;

            Assert.Single(entryLifecycleStatus);
            Assert.All(entryLifecycleStatus, value =>
            {
                Assert.Equal(expectedEntryLifecycleStatus, value);
            });
        }

        private KeyValuePair<string, List<dynamic>> GetEntryLifecycleStatusProperty(Resource resource)
        {
            return resource.Properties.SingleOrDefault(p => p.Key == Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus);
        }
    }
}
