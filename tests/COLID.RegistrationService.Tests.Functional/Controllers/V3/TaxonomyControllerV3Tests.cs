using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using COLID.Graph.TripleStore.DataModels.Taxonomies;
using COLID.RegistrationService.Tests.Common.Builder;
using COLID.RegistrationService.Tests.Common.Utils;
using COLID.RegistrationService.WebApi;
using Xunit;
using Xunit.Abstractions;

namespace COLID.RegistrationService.Tests.Functional.Controllers.V3
{
    public class TaxonomyControllerV3Tests : IClassFixture<FunctionTestsFixture>
    {
        private readonly ITestOutputHelper _output;
        private readonly HttpClient _client;
        private readonly FunctionTestsFixture _factory;
        private readonly string _apiPath = "api/v3";

        public TaxonomyControllerV3Tests(FunctionTestsFixture factory, ITestOutputHelper output)
        {
            _output = output;
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GetTaxonomies_Success()
        {
            var taxonomyType = HttpUtility.UrlEncode("https://pid.bayer.com/kos/19050/MathematicalModelCategory");

            var expectedTaxonomies = new TaxonomySchemeBuilder().GenerateSampleTaxonomies();

            var result = await _client.GetAsync(_apiPath + $"/taxonomyList?taxonomyType={taxonomyType}");
            var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
            var taxonomies = Newtonsoft.Json.JsonConvert.DeserializeObject<List<TaxonomyResultDTO>>(content);
            Assert.NotNull(taxonomies);

            // Sort the expected and actual results, so that the taxonomies and their properties are all in same order
            // The graph database returns different orders sometimes
            expectedTaxonomies = OrderTaxonomyList(expectedTaxonomies);
            taxonomies = OrderTaxonomyList(taxonomies);

            TestUtils.AssertSameEntityContent(expectedTaxonomies, taxonomies);
        }

        [Fact]
        public async Task GetTaxonomies_Error_NotFound_WrongUri()
        {
            var taxonomyType = HttpUtility.UrlEncode("https://pid.bayer.com/kos/19050/MathematicalModelCategory_Wrong_Uri");

            var expectedTaxonomies = new TaxonomySchemeBuilder().GenerateSampleTaxonomies();

            var result = await _client.GetAsync(_apiPath + $"/taxonomyList?taxonomyType={taxonomyType}");
            var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
            var taxonomies = Newtonsoft.Json.JsonConvert.DeserializeObject<List<TaxonomyResultDTO>>(content);
            result.EnsureSuccessStatusCode();
            Assert.Empty(taxonomies);
        }

        [Theory]
        [InlineData("INVALID_Uri")]
        [InlineData(null)]
        [InlineData("")]
        public async Task GetTaxonomies_Error_BadRequest_InvalidUriType(string uri)
        {
            var result = await _client.GetAsync(_apiPath + $"/taxonomyList?taxonomyType={uri}");
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task GetTaxonomy_Success()
        {
            var identifier = HttpUtility.UrlEncode("https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/0");

            var result = await _client.GetAsync(_apiPath + $"/taxonomy?id={identifier}");
            var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
            var taxonomy = Newtonsoft.Json.JsonConvert.DeserializeObject<TaxonomyResultDTO>(content);

            Assert.NotNull(taxonomy);
            Assert.Equal("https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/0", taxonomy.Id);
            Assert.Equal("Classification Model", taxonomy.Name);
            Assert.False(taxonomy.HasParent);
            Assert.True(taxonomy.HasChild);
            Assert.Equal(3, taxonomy.Children.Count);

            var dlmTaxo = taxonomy.Children.First(t => t.Name == "Deep Learning Model");

            Assert.Equal("Deep Learning Model", dlmTaxo.Name);
            Assert.True(dlmTaxo.HasParent);
            Assert.True(dlmTaxo.HasChild);
            Assert.Equal("https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/8", dlmTaxo.Id);
            Assert.Equal(4, dlmTaxo.Children.Count);
        }

        [Fact]
        public async Task GetTaxonomy_Error_NotFound_WrongUri()
        {
            var identifier = HttpUtility.UrlEncode("https://pid.bayer.com/kos/19050/MathematicalModelCategory_Wrong_Uri");
            var result = await _client.GetAsync(_apiPath + $"/taxonomy?id={identifier}");
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        }

        [Theory]
        [InlineData("INVALID_Uri")]
        [InlineData(null)]
        [InlineData("")]
        public async Task GetTaxonomy_Error_BadRequest_InvalidIdentifier(string identifier)
        {
            var result = await _client.GetAsync(_apiPath + $"/taxonomy?id={identifier}");
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        private List<TaxonomyResultDTO> OrderTaxonomyList(IList<TaxonomyResultDTO> taxonomies)
        {
            var orderedTaxonomies = taxonomies.OrderBy(t => t.Id);
            foreach (var taxo in orderedTaxonomies)
            {
                taxo.Properties = taxo.Properties.OrderBy(p => p.Key).ToDictionary(x => x.Key, x => x.Value);
                taxo.Children = OrderTaxonomyList(taxo.Children);
            }

            return orderedTaxonomies.ToList();
        }
    }
}
