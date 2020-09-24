using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using COLID.Exception.Models;
using COLID.Exception.Models.Business;
using COLID.RegistrationService.Common.DataModel.Graph;
using Xunit;
using Xunit.Abstractions;

namespace COLID.RegistrationService.Tests.Functional.Controllers.V3
{
    public class GraphManagementControllerV3Tests : IClassFixture<FunctionTestsFixture>
    {
        private readonly ITestOutputHelper _output;
        private readonly HttpClient _client;
        private readonly FunctionTestsFixture _factory;
        private readonly string _apiPath = "api/v3/graph";

        public GraphManagementControllerV3Tests(FunctionTestsFixture factory, ITestOutputHelper output)
        {
            _output = output;
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task Get_Success()
        {
            // Act
            var result = await _client.GetAsync(_apiPath);
            var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
            var graphs = Newtonsoft.Json.JsonConvert.DeserializeObject<List<GraphDto>>(content);

            // Assert
            Assert.NotNull(result);

            var activeGraphs = graphs.Where(g => g.Status == RegistrationService.Common.Enums.Graph.GraphStatus.Active && !string.IsNullOrWhiteSpace(g.StartTime));
            var unreferencedGraphs = graphs.Where(g => g.Status == RegistrationService.Common.Enums.Graph.GraphStatus.Unreferenced && string.IsNullOrWhiteSpace(g.StartTime));
            var historicGraphs = graphs.Where(g => g.Status == RegistrationService.Common.Enums.Graph.GraphStatus.Historic && !string.IsNullOrWhiteSpace(g.StartTime));

            Assert.Equal(12, activeGraphs.Count());
            Assert.Equal(2, unreferencedGraphs.Count());
            Assert.Single(historicGraphs);
        }

        [Fact]
        public async Task DeleteGraph_Success()
        {
            var graph = HttpUtility.UrlEncode("https://pid.bayer.com/colid/delete/graph");
            var result = await _client.DeleteAsync($"{_apiPath}?graph={graph}");

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("invalid_url")]
        public async Task DeleteGraph_Should_ThrowError_BusinessException(string graphName)
        {
            var exceptionResponse = await DeleteGraph<BusinessException>(graphName);

            AssertException(exceptionResponse, HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task DeleteGraph_Should_ThrowError_GraphNotFoundException()
        {
            var exceptionResponse = await DeleteGraph<GraphNotFoundException>("https://pid.bayer.com/not/exists");

            AssertException(exceptionResponse, HttpStatusCode.NotFound);
        }

        [Theory]
        [InlineData("https://pid.bayer.com/resource/1.0")]
        [InlineData("https://pid.bayer.com/resource/2.0")]
        [InlineData("https://pid.bayer.com/kos/19050/367403")]
        public async Task DeleteGraph_Should_ThrowError_ReferenceException(string graphName)
        {
            var exceptionResponse = await DeleteGraph<ReferenceException>(graphName);

            AssertException(exceptionResponse, HttpStatusCode.Conflict);
        }

        #region Helper methods to call api

        private async Task<ColidResponse<T>> DeleteGraph<T>(string graphName) where T : GeneralException
        {
            // Arrange
            var graph = HttpUtility.UrlEncode(graphName);

            // Act
            var result = await _client.DeleteAsync($"{_apiPath}?graph={graph}");
            var content = await result.Content.ReadAsStringAsync();
            var exception = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(content);

            return new ColidResponse<T>(result, content, exception);
        }

        #endregion Helper methods to call api

        #region Helper methods

        private void AssertException<T>(ColidResponse<T> result, HttpStatusCode expectedStatusCode) where T : GeneralException
        {
            Assert.Equal(expectedStatusCode, result.Response.StatusCode);
            Assert.Equal(typeof(T).Name, result.ResultObject.Type);
        }

        #endregion Helper methods

        private class ColidResponse<T>
        {
            public HttpResponseMessage Response { get; set; }
            public string Content { get; set; }
            public T ResultObject { get; set; }

            public ColidResponse(HttpResponseMessage rsp, string cnt)
            {
                Response = rsp;
                Content = cnt;
            }

            public ColidResponse(HttpResponseMessage rsp, string cnt, T resultObject)
            {
                Response = rsp;
                Content = cnt;
                ResultObject = resultObject;
            }
        }
    }
}
