using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.Tests.Builder;
using COLID.RegistrationService.Services.Authorization.UserInfo;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.Services.Validation.Models;
using COLID.RegistrationService.Services.Validation.Validators.Datatypes;
using COLID.RegistrationService.Tests.Common.Builder;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace COLID.RegistrationService.Tests.Unit.Services.Validation.Validators.Datatypes
{
    [ExcludeFromCodeCoverage]
    public class DatetimeValidatorTests
    {
        private readonly Mock<IUserInfoService> _mockUserInfoService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly DatetimeValidator _validator;

        public DatetimeValidatorTests()
        {
            _validator = new DatetimeValidator();

            _mockUserInfoService = new Mock<IUserInfoService>();
            _mockConfiguration = new Mock<IConfiguration>();

            _mockConfiguration.Setup(mock => mock.GetSection(It.IsAny<string>())).Returns(new Mock<IConfigurationSection>().Object);
        }

        [Theory]
        [InlineData("2020-03-19T12:06:09.6509493Z", "2020-03-19T13:06:09.6509493+01:00")]
        [InlineData("Thu, 19 Mar 2020 12:09:04 GMT", "2020-03-19T13:09:04.0000000+01:00")]
        [InlineData("2020-03-19T12:09:17", "2020-03-19T12:09:17.0000000")]
        [InlineData("2020-03-19 12:09:40Z", "2020-03-19T13:09:40.0000000+01:00")]
        public void InternalHasValidationResult_Success(string actualDateTime, string expectedDateTime)
        {
            // Arrange
            Resource resource = CreateResourceWithDateTimeProperty(actualDateTime);
            IList<MetadataProperty> metadata = new MetadataBuilder().GenerateSampleDateCreated().Build();
            EntityValidationFacade validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetDateTimeProperty(resource));

            // Assert
            Assert.Contains(Graph.Metadata.Constants.Resource.Author, validationFacade.RequestResource.Properties);

            AssertDateTimteStrings(expectedDateTime, GetDateTimeProperty(validationFacade.RequestResource).Value.FirstOrDefault().ToString());
            Assert.Equal(0, validationFacade.ValidationResults.Count);
        }

        private void AssertDateTimteStrings(string expectedDateTime, string actualDateTime)
        {
            var expectedDateTimeSeconds = Convert.ToDateTime(expectedDateTime);
            var actualDateTimeSeconds = Convert.ToDateTime(actualDateTime);

            Assert.Equal(expectedDateTimeSeconds, actualDateTimeSeconds);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void InternalHasValidationResult_EmptyOrNullDateTime(string actualDateTime)
        {
            // Arrange
            Resource resource = CreateResourceWithDateTimeProperty(actualDateTime);
            IList<MetadataProperty> metadata = new MetadataBuilder().GenerateSampleDateCreated().Build();
            EntityValidationFacade validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetDateTimeProperty(resource));

            // Assert
            Assert.Contains(Graph.Metadata.Constants.Resource.DateCreated, validationFacade.RequestResource.Properties);

            var dateTimeValue = validationFacade.RequestResource.Properties[Graph.Metadata.Constants.Resource.DateCreated];
            Assert.Single(dateTimeValue);
            Assert.Contains(dateTimeValue, t => t == actualDateTime);
        }

        [Theory]
        [InlineData("someinvalidstring")]
        public void InternalHasValidationResult_CreateValidationResult_InvalidFormat(string actualDateTime)
        {
            // Arrange
            Resource resource = CreateResourceWithDateTimeProperty(actualDateTime);
            IList<MetadataProperty> metadata = new MetadataBuilder().GenerateSampleDateCreated().Build();
            EntityValidationFacade validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, GetDateTimeProperty(resource));

            // Assert
            Assert.Contains(Graph.Metadata.Constants.Resource.DateCreated, validationFacade.RequestResource.Properties);
            Assert.Equal(1, validationFacade.ValidationResults.Count);
            Assert.Contains(validationFacade.ValidationResults, t => t.ResultValue == actualDateTime);
        }

        private KeyValuePair<string, List<dynamic>> GetDateTimeProperty(Resource resource)
        {
            return resource.Properties.SingleOrDefault(p => p.Key == Graph.Metadata.Constants.Resource.DateCreated);
        }

        private Resource CreateResourceWithDateTimeProperty(string dateTime)
        {
            Resource resource = new ResourceBuilder()
                .GenerateSampleData()
                .WithDateCreated(dateTime)
                .WithPidUri($"https://pid.bayer.com/kos/0308eeb4-ed33-43b8-abf7-599a57cbd718")
                .Build();

            return resource;
        }
    }
}
