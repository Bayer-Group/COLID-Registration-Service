using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.Tests.Builder;
using COLID.Graph.TripleStore.DataModels.Resources;
using COLID.RegistrationService.Services.Validation.Models;
using COLID.RegistrationService.Services.Validation.Validators.Keys;
using COLID.RegistrationService.Tests.Common.Builder;
using Xunit;

namespace COLID.RegistrationService.Tests.Unit.Services.Validation.Validators.Keys
{
    [ExcludeFromCodeCoverage]
    public class DateCreatedValidatorTests
    {
        private readonly DateCreatedValidator _validator;
        private readonly IList<MetadataProperty> _metadata;

        public DateCreatedValidatorTests()
        {
            _validator = new DateCreatedValidator();
            _metadata = new MetadataBuilder().GenerateSampleDateCreated().Build();
        }

        [Fact]
        public void InternalHasValidationResult_OverwritePropertyWithNewDateTime()
        {
            // Arrange
            var resourceDateCreated = DateTime.UtcNow.AddDays(-1);
            var resource = CreateResource(resourceDateCreated);
            var resourceCto = new ResourcesCTO(resource, null, new List<VersionOverviewCTO>());
            var new_resource = CreateResource(DateTime.UtcNow);

            EntityValidationFacade validationFacade = new EntityValidationFacade(ResourceCrudAction.Update, new_resource, resourceCto, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetDateTimeProperty(new_resource));

            // Assert
            Assert.Contains(Graph.Metadata.Constants.Resource.DateCreated, validationFacade.RequestResource.Properties);
            var dateCreatedList = GetDateTimeProperty(validationFacade.RequestResource).Value;

            Assert.All(dateCreatedList, t =>
            {
                DateTime dateCreated = Convert.ToDateTime(t);
                Assert.Equal(1, dateCreated.CompareTo(resourceDateCreated));
            });
        }

        [Fact]
        public void InternalHasValidationResult_OverwritePropertyWithRepoDateTime()
        {
            // Arrange
            var repoDate = DateTime.UtcNow.AddDays(-1);
            var repoResource = CreateResource(repoDate);
            var resourceCto = new ResourcesCTO(repoResource, null, new List<VersionOverviewCTO>());
            var resource = CreateResource(DateTime.UtcNow);

            EntityValidationFacade validationFacade = new EntityValidationFacade(ResourceCrudAction.Update, resource, resourceCto, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetDateTimeProperty(resource));

            // Assert
            Assert.Contains(Graph.Metadata.Constants.Resource.DateCreated, validationFacade.RequestResource.Properties);
            var dateCreatedList = GetDateTimeProperty(validationFacade.RequestResource).Value;

            Assert.All(dateCreatedList, t =>
            {
                Assert.Equal(repoDate.ToString("o"), t);
            });
        }

        private KeyValuePair<string, List<dynamic>> GetDateTimeProperty(Resource resource)
        {
            return resource.Properties.SingleOrDefault(p => p.Key == Graph.Metadata.Constants.Resource.DateCreated);
        }

        private Resource CreateResource(DateTime dateCreated)
        {
            var resource = new ResourceBuilder()
                .GenerateSampleData()
                .WithDateCreated(dateCreated.ToString("o"))
                .Build();

            return resource;
        }
    }
}
