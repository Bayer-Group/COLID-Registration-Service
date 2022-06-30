using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AutoMapper;
using COLID.Cache.Services;
using COLID.Exception.Models.Business;
using COLID.Graph.Metadata.Constants;
using COLID.RegistrationService.Repositories.Interface;
using COLID.RegistrationService.Services.Implementation;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.Services.MappingProfiles;
using COLID.RegistrationService.Tests.Common.Builder;
using COLID.RegistrationService.Tests.Common.Utils;
using COLID.StatisticsLog.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using COLID.Graph.Metadata.Services;
using COLID.Graph.TripleStore.DataModels.Taxonomies;
using COLID.RegistrationService.Common.DataModel.PidUriTemplates;
using Microsoft.Extensions.Logging;
using Entity = COLID.Graph.TripleStore.DataModels.Base.Entity;

namespace COLID.RegistrationService.Tests.Unit.Services
{
    [ExcludeFromCodeCoverage]
    public class TaxonomyServiceTests
    {
        private readonly Mock<ITaxonomyRepository> _taxonomyRepoMock;
        private readonly ITaxonomyService _service;

        private readonly ISet<Uri> _metadataGraphs;

        public TaxonomyServiceTests()
        {
            _metadataGraphs = new HashSet<Uri>
            {
                new Uri("https://pid.bayer.com/pid_ontology_oss/5"), 
                new Uri("https://pid.bayer.com/pid_enterprise_core_ontology/1.0"),
                new Uri("https://pid.bayer.com/pid_ontology_oss/shacled/5.0"),
                new Uri("https://pid.bayer.com/pid_ontology_oss/technical/5.0")
            };

            var loggerMock = new Mock<ILogger<TaxonomyService>>();
            var validationServiceMock = new Mock<IValidationService>();
            var metadataServiceMock = new Mock<IMetadataService>();
            _taxonomyRepoMock = new Mock<ITaxonomyRepository>();
            var cacheServiceMock = new Mock<ICacheService>();


            cacheServiceMock.Setup(t => t.GetOrAdd(It.IsAny<string>(), It.IsAny<Func<IList<TaxonomyResultDTO>>>())).Returns(
                (string key, Func<IList<TaxonomyResultDTO>> function) => function());
            cacheServiceMock.Setup(t => t.GetOrAdd(It.IsAny<string>(), It.IsAny<Func<TaxonomyResultDTO>>())).Returns(
                (string key, Func<TaxonomyResultDTO> function) => function());

            var pidUriTemplateServiceMock = new Mock<IPidUriTemplateService>();
            pidUriTemplateServiceMock.Setup(t => t.GetFlatPidUriTemplateByPidUriTemplate(It.IsAny<Entity>()))
                .Returns(null as PidUriTemplateFlattened);
            pidUriTemplateServiceMock.Setup(t => t.FormatPidUriTemplateName(It.IsAny<PidUriTemplateFlattened>()))
                .Returns(string.Empty);

            metadataServiceMock.Setup(mock => mock.GetMultiInstanceGraph(It.IsAny<string>()))
                .Returns(_metadataGraphs);

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<TaxonomyNameResolver>(t => new TaxonomyNameResolver(pidUriTemplateServiceMock.Object));

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.ConstructServicesUsing(serviceProvider.GetService);
                cfg.AddProfile(new TaxonomyProfile());
            });

            var mapper = config.CreateMapper();

            _service = new TaxonomyService(mapper, metadataServiceMock.Object, validationServiceMock.Object, _taxonomyRepoMock.Object, loggerMock.Object, cacheServiceMock.Object);
        }

        [Fact]
        public void GetTaxonomy_Successful()
        {
            //Arrange
            var expectedTaxonomy = new TaxonomySchemeBuilder().GenerateSampleTaxonomy();
            var mockTaxonomy = new TaxonomySchemeBuilder()
                .GenerateSampleMathematicalTaxonomyList()
                .Build();
            _taxonomyRepoMock.Setup(mock => mock.GetTaxonomiesByIdentifier(It.IsAny<string>(), It.IsAny<ISet<Uri>>())).Returns(mockTaxonomy);

            // Act
            var taxonomy = _service.GetEntity("https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/0");

            // Assert
            TestUtils.AssertSameEntityContent(taxonomy, expectedTaxonomy);
        }

        [Fact]
        public void GetTaxonomy_NotFound()
        {
            //Arrange
            var identifier = "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/notfound";
            var mockTaxonomy = new TaxonomySchemeBuilder()
                .GenerateSampleMathematicalTaxonomyList()
                .Build();
            _taxonomyRepoMock.Setup(mock => mock.GetTaxonomiesByIdentifier(It.IsAny<string>(), It.IsAny<ISet<Uri>>())).Returns(mockTaxonomy);

            // Act
            var ex = Assert.Throws<EntityNotFoundException>(() => _service.GetEntity(identifier));

            // Assert
            Assert.Equal(RegistrationService.Common.Constants.Messages.Taxonomy.NotFound, ex.Message);
            Assert.Equal(identifier, ex.Id);
        }

        [Fact]
        public void GetTaxonomies_Successful()
        {
            //Arrange
            var expectedTaxonomies = new TaxonomySchemeBuilder().GenerateSampleTaxonomies();
            var mockTaxonomy = new TaxonomySchemeBuilder()
                .GenerateSampleMathematicalTaxonomyList()
                .Build();
            _taxonomyRepoMock.Setup(mock => mock.GetTaxonomies(It.IsAny<string>(), _metadataGraphs)).Returns(mockTaxonomy);

            // Act
            var taxonomies = _service.GetTaxonomies("https://pid.bayer.com/kos/19050/MathematicalModelCategory");

            // Assert
            TestUtils.AssertSameEntityContent(taxonomies, expectedTaxonomies);
        }

        [Fact]
        public void GetTaxonomies_Successful_WithInvalidBroader()
        {
            //Arrange
            var expectedTaxonomies = new TaxonomySchemeBuilder().GenerateSampleTaxonomies();
            expectedTaxonomies.FirstOrDefault(t => t.Id == "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/0")?.Children.Clear();

            var mockTaxonomy = new TaxonomySchemeBuilder()
                .GenerateSampleMathematicalTaxonomyList()
                .Build();

            var modifiedTaxonomy = mockTaxonomy.FirstOrDefault(t => t.Id == "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/8");
            modifiedTaxonomy.Properties[Graph.Metadata.Constants.SKOS.Broader] = new List<dynamic>() { "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/9" };

            _taxonomyRepoMock.Setup(mock => mock.GetTaxonomies(It.IsAny<string>(), _metadataGraphs)).Returns(mockTaxonomy);

            // Act
            var taxonomies = _service.GetTaxonomies("https://pid.bayer.com/kos/19050/MathematicalModelCategory");

            // Assert
            Assert.Equal(taxonomies, expectedTaxonomies);
        }

        [Fact]
        public void GetTaxonomies_NotFound()
        {
            //Arrange
            var expectedTaxonomies = new List<TaxonomyResultDTO>();
            var mockTaxonomy = new List<Taxonomy>();
            _taxonomyRepoMock.Setup(mock => mock.GetTaxonomies(It.IsAny<string>(), _metadataGraphs)).Returns(mockTaxonomy);

            // Act
            var taxonomies = _service.GetTaxonomies("https://pid.bayer.com/kos/19050/NotFound");

            // Assert
            Assert.Equal(taxonomies, expectedTaxonomies);
        }
    }
}
