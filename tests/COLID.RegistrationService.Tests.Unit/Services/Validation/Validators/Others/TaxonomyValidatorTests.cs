using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AutoMapper;
using COLID.Cache.Services;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.Metadata.Services;
using COLID.Graph.Tests.Builder;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.DataModels.Taxonomies;
using COLID.Graph.TripleStore.Extensions;
using COLID.RegistrationService.Common.DataModel.PidUriTemplates;
using COLID.RegistrationService.Repositories.Interface;
using COLID.RegistrationService.Services.Implementation;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.Services.MappingProfiles;
using COLID.RegistrationService.Services.Validation.Models;
using COLID.RegistrationService.Services.Validation.Validators.Others;
using COLID.RegistrationService.Tests.Common.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace COLID.RegistrationService.Tests.Unit.Services.Validation.Validators.Others
{
    [ExcludeFromCodeCoverage]
    public class TaxonomyValidatorTests
    {
        private readonly TaxonomyValidator _validator;
        private readonly IList<MetadataProperty> _metadata;

        public TaxonomyValidatorTests()
        {
            var loggerMock = new Mock<ILogger<TaxonomyService>>();
            var validationServiceMock = new Mock<IValidationService>();
            var metadataServiceMock = new Mock<IMetadataService>();
            var cacheServiceMock = new Mock<ICacheService>();

            var mathematicalTaxonomy = new TaxonomySchemeBuilder().GenerateSampleMathematicalTaxonomyList().Build();
            var informationClassificationTaxonomy = new TaxonomySchemeBuilder().GenerateSampleInformationClassificationTaxonomyList().Build();

            var taxonomyRepoMock = new Mock<ITaxonomyRepository>();
            taxonomyRepoMock.Setup(t => t.GetTaxonomies(Graph.Metadata.Constants.Resource.Type.MathematicalModelCategory)).Returns(mathematicalTaxonomy);
            taxonomyRepoMock.Setup(t => t.GetTaxonomies(Graph.Metadata.Constants.Resource.Type.InformationClassification)).Returns(informationClassificationTaxonomy);

            cacheServiceMock.Setup(t => t.GetOrAdd(It.IsAny<string>(), It.IsAny<Func<IList<Taxonomy>>>())).Returns(
                (string key, Func<IList<Taxonomy>> function) => function());
            cacheServiceMock.Setup(t => t.GetOrAdd(It.IsAny<string>(), It.IsAny<Func<IList<TaxonomyResultDTO>>>())).Returns(
                (string key, Func<IList<TaxonomyResultDTO>> function) => function());
            var pidUriTemplateServiceMock = new Mock<IPidUriTemplateService>();


            pidUriTemplateServiceMock.Setup(t => t.GetFlatPidUriTemplateByPidUriTemplate(It.IsAny<Entity>()))
                .Returns(null as PidUriTemplateFlattened);
            pidUriTemplateServiceMock.Setup(t => t.GetFlatPidUriTemplateByPidUriTemplate(It.IsAny<Entity>()))
                .Returns(null as PidUriTemplateFlattened);
            pidUriTemplateServiceMock.Setup(t => t.FormatPidUriTemplateName(It.IsAny<PidUriTemplateFlattened>()))
                .Returns(string.Empty);

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<TaxonomyNameResolver>(t => new TaxonomyNameResolver(pidUriTemplateServiceMock.Object));

            var serviceProvider = serviceCollection.BuildServiceProvider();


            var config = new MapperConfiguration(cfg =>
            {
                cfg.ConstructServicesUsing(serviceProvider.GetService);
                cfg.AddProfile(new TaxonomyProfile());
            });

            var mapper = config.CreateMapper();

            var taxonomyService = new TaxonomyService(mapper, metadataServiceMock.Object, validationServiceMock.Object, taxonomyRepoMock.Object, loggerMock.Object, cacheServiceMock.Object);

            _validator = new TaxonomyValidator(taxonomyService);

            _metadata = new MetadataBuilder()
                .GenerateSampleHasInformationClassifikation()
                .GenerateSampleMathematicalCategory()
                .Build();
        }

        [Theory]
        [ClassData(typeof(MathematicalModelTaxonomyTestData))]
        public void InternalHasValidationResult_MathematicalModelCategory_Taxonomy_CreateValidationResults(List<string> taxonomyList, List<string> expectedList)
        {
            var propertyKey = Graph.Metadata.Constants.Resource.MathematicalModelCategory;
            InternalHasValidationResult_Taxonomy_CreateValidationResults(propertyKey, taxonomyList, expectedList);
        }

        [Theory]
        [ClassData(typeof(InformationClassificationTestData))]
        public void InternalHasValidationResult_InformationClassification_Taxonomy_CreateValidationResults(List<string> taxonomyList, List<string> expectedList)
        {
            var propertyKey = Graph.Metadata.Constants.Resource.HasInformationClassification;
            InternalHasValidationResult_Taxonomy_CreateValidationResults(propertyKey, taxonomyList, expectedList);
        }

        private void InternalHasValidationResult_Taxonomy_CreateValidationResults(string propertyKey, List<string> taxonomyList, List<string> expectedList)
        {
            // Arrange
            Resource resource = CreateResourceWithTaxonomyProperty(propertyKey, taxonomyList.Cast<dynamic>().ToList(), out var property);
            var validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, property);

            // Assert
            var resultValue = resource.Properties.FirstOrDefault(p => p.Key == propertyKey).Value;

            Assert.Equal(expectedList.Cast<dynamic>().OrderBy(t => t), resultValue.OrderBy(t => t));
            Assert.Empty(validationFacade.ValidationResults);
        }

        [Theory]
        [InlineData(Graph.Metadata.Constants.Resource.MathematicalModelCategory, "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/notfound")]
        [InlineData(Graph.Metadata.Constants.Resource.HasInformationClassification, "https://pid.bayer.com/secret/notfound")]

        public void InternalHasValidationResult_Taxonomy_InvalidValues(string propertyKey, string taxonomyId)
        {
            // Arrange
            Resource resource = CreateResourceWithTaxonomyProperty(propertyKey, new List<dynamic>() { taxonomyId }, out var property);
            EntityValidationFacade validationFacade = new EntityValidationFacade(ResourceCrudAction.Create, resource, null, null, _metadata, null);

            // Act
            _validator.HasValidationResult(validationFacade, property);

            // Assert
            Assert.Single(validationFacade.ValidationResults);
            Assert.Collection(validationFacade.ValidationResults, validationResult => AssertValidationResult(validationResult, taxonomyId));
        }

        private void AssertValidationResult(ValidationResultProperty validationResult, string taxonomyId)
        {
            Assert.Equal(string.Format(RegistrationService.Common.Constants.Messages.Taxonomy.InvalidSelection, taxonomyId), validationResult.Message);
            Assert.Equal(ValidationResultSeverity.Violation, validationResult.ResultSeverity);
        }

        private Resource CreateResourceWithTaxonomyProperty(string propertyKey, List<dynamic> taxonomyValue, out KeyValuePair<string, List<dynamic>> property)
        {
            Resource resource = new ResourceBuilder()
                .GenerateSampleData()
                .Build();

            resource.Properties.AddOrUpdate(propertyKey, taxonomyValue);

            property = resource.Properties.First(t => t.Key == propertyKey);

            return resource;
        }

        private class MathematicalModelTaxonomyTestData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { GetList(4), GetList(4) };
                yield return new object[] { GetList(4, 5, 6), GetList(4, 5, 6) };
                yield return new object[] { GetList(4, 5, 6, 7), GetList(8) };
                yield return new object[] { GetList(4, 5, 6, 7, 1), GetList(8, 1) };
                yield return new object[] { GetList(4, 5, 6, 7, 11, 12, 13), GetList(8, 11, 12, 13) };
                yield return new object[] { GetList(4, 5, 6, 7, 11, 12, 13, 14), GetList(8, 9) };
                yield return new object[] { GetList(4, 5, 6, 7, 11, 12, 13, 14, 10), GetList(0) };
                yield return new object[] { GetList(8, 9), GetList(8, 9) };
                yield return new object[] { GetList(8, 9, 10), GetList(0) };
                yield return new object[] { GetList(11, 12, 13, 14, 10), GetList(9, 10) };
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private class InformationClassificationTestData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[]
                {
                    GetList(Graph.Metadata.Constants.Resource.InformationClassification.Internal),
                    GetList(Graph.Metadata.Constants.Resource.InformationClassification.Internal)
                };
                yield return new object[]
                {
                    GetList(Graph.Metadata.Constants.Resource.InformationClassification.Internal, Graph.Metadata.Constants.Resource.InformationClassification.Secret),
                    GetList(Graph.Metadata.Constants.Resource.InformationClassification.Internal, Graph.Metadata.Constants.Resource.InformationClassification.Secret)
                };

            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private static List<string> GetList(params int[] list)
        {
            return list.Select(t => $"https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/{t}").ToList();
        }

        private static List<string> GetList(params string[] list)
        {
            return list.ToList();
        }
    }
}
