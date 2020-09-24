using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using COLID.RegistrationService.Tests.Common.Builder;
using COLID.RegistrationService.Tests.Functional.DataModel.V2;
using Xunit;

namespace COLID.RegistrationService.Tests.Functional.Controllers.V2
{
    public class TaxonomyControllerV2Tests : IClassFixture<FunctionTestsFixture>
    {
        private readonly HttpClient _client;
        private readonly FunctionTestsFixture _factory;
        private readonly string _apiPath = "api/v2/taxonomy";

        public TaxonomyControllerV2Tests(FunctionTestsFixture factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GetTaxonomies_Success()
        {
            var taxonomyType = HttpUtility.UrlEncode("https://pid.bayer.com/kos/19050/MathematicalModelCategory");

            var result = await _client.GetAsync($"{_apiPath}List?taxonomyType={taxonomyType}");
            var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
            var taxonomies = Newtonsoft.Json.JsonConvert.DeserializeObject<List<TaxonomyResultDtoV2>>(content);

            var classificationModelTaxonomy = taxonomies.First(t => t.Name == "Classification Model");

            Assert.NotNull(taxonomies);
            Assert.Equal(4, taxonomies.Count);
            Assert.Equal("https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/0", classificationModelTaxonomy.Subject);
            Assert.Equal("Classification Model", classificationModelTaxonomy.Name);
            Assert.False(classificationModelTaxonomy.HasParent);
            Assert.True(classificationModelTaxonomy.HasChild);
            Assert.Equal(3, classificationModelTaxonomy.Children.Count);

            var dlmTaxo = classificationModelTaxonomy.Children.First(t => t.Name == "Deep Learning Model");

            Assert.Equal("Deep Learning Model", dlmTaxo.Name);
            Assert.True(dlmTaxo.HasParent);
            Assert.True(dlmTaxo.HasChild);
            Assert.Equal("https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/8", dlmTaxo.Subject);
            Assert.Equal(4, dlmTaxo.Children.Count);
        }

        [Fact]
        public async Task GetTaxonomies_Error_NotFound_WrongUri()
        {
            var taxonomyType = HttpUtility.UrlEncode("https://pid.bayer.com/kos/19050/MathematicalModelCategory_Wrong_Uri");

            var expectedTaxonomies = new TaxonomySchemeBuilder().GenerateSampleTaxonomies();

            var result = await _client.GetAsync($"{_apiPath}List?taxonomyType={taxonomyType}");
            var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
            var taxonomies = Newtonsoft.Json.JsonConvert.DeserializeObject<List<TaxonomyResultDtoV2>>(content);
            result.EnsureSuccessStatusCode();
            Assert.Empty(taxonomies);
        }

        [Theory]
        [InlineData("INVALID_Uri")]
        [InlineData(null)]
        [InlineData("")]
        public async Task GetTaxonomies_Error_BadRequest_InvalidUriType(string uri)
        {
            var result = await _client.GetAsync($"{_apiPath}List?taxonomyType={uri}");
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task GetTaxonomy_Success()
        {
            var identifier = HttpUtility.UrlEncode("https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/0");

            var expectedTaxonomy = new TaxonomySchemeBuilder().GenerateSampleTaxonomy();

            var result = await _client.GetAsync($"{_apiPath}?subject={identifier}");
            var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
            var taxonomy = Newtonsoft.Json.JsonConvert.DeserializeObject<TaxonomyResultDtoV2>(content);

            Assert.NotNull(taxonomy);
            Assert.Equal("https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/0", taxonomy.Subject);
            Assert.Equal("Classification Model", taxonomy.Name);
            Assert.False(taxonomy.HasParent);
            Assert.True(taxonomy.HasChild);
            Assert.Equal(3, taxonomy.Children.Count);

            var dlmTaxo = taxonomy.Children.First(t => t.Name == "Deep Learning Model");

            Assert.Equal("Deep Learning Model", dlmTaxo.Name);
            Assert.True(dlmTaxo.HasParent);
            Assert.True(dlmTaxo.HasChild);
            Assert.Equal("https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/8", dlmTaxo.Subject);
            Assert.Equal(4, dlmTaxo.Children.Count);
        }

        [Fact]
        public async Task GetTaxonomy_Error_NotFound_WrongUri()
        {
            var identifier = HttpUtility.UrlEncode("https://pid.bayer.com/kos/19050/MathematicalModelCategory_Wrong_Uri");
            var result = await _client.GetAsync($"{_apiPath}?subject={identifier}");
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        }

        [Theory]
        [InlineData("INVALID_Uri")]
        [InlineData(null)]
        [InlineData("")]
        public async Task GetTaxonomy_Error_BadRequest_InvalidIdentifier(string identifier)
        {
            var result = await _client.GetAsync($"{_apiPath}?id={identifier}");
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }
    }
}
