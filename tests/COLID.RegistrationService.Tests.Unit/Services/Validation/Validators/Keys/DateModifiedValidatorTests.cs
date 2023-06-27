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
    public class DateModifiedValidatorTests
    {
        private readonly DateModifiedValidator _validator;
        private readonly IList<MetadataProperty> _metadata;

        public DateModifiedValidatorTests()
        {
            _validator = new DateModifiedValidator();
            _metadata = new MetadataBuilder().GenerateSampleDateModified().Build();
        }

        [Theory]
        [InlineData(ResourceCrudAction.Create)]
        [InlineData(ResourceCrudAction.Update)]
        public void InternalHasValidationResult_OverwritePropertyWithNewDateTime(ResourceCrudAction crudAction)
        {
            // Arrange
            var expectedDateModified = DateTime.UtcNow;
            var resource = new ResourceBuilder()
                .GenerateSampleData()
                .WithLastChangeDateTime(DateTime.UtcNow.AddDays(-1).ToString("o"))
                .Build();

            EntityValidationFacade validationFacade = new EntityValidationFacade(crudAction, resource, null, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetDateModifiedProperty(resource));

            // Assert
            Assert.Contains(Graph.Metadata.Constants.Resource.DateModified, validationFacade.RequestResource.Properties);
            var dateCreatedList = GetDateModifiedProperty(validationFacade.RequestResource).Value;

            Assert.All(dateCreatedList, t =>
            {
                var dateModified = Convert.ToDateTime(t);
                Assert.Equal(1, dateModified.CompareTo(expectedDateModified));
            });
        }

        private KeyValuePair<string, List<dynamic>> GetDateModifiedProperty(Resource resource)
        {
            return resource.Properties.SingleOrDefault(p => p.Key == Graph.Metadata.Constants.Resource.DateModified);
        }
    }
}
