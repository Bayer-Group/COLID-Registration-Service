using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using COLID.RegistrationService.WebApi;
using Xunit;
using Xunit.Abstractions;

namespace COLID.RegistrationService.Tests.Functional.Controllers.V1
{
    public class ProxyConfigControllerV1Tests : IClassFixture<FunctionTestsFixture>
    {
        private readonly HttpClient _client;
        private readonly FunctionTestsFixture _factory;
        private readonly ITestOutputHelper _output;
        private readonly string _apiPath = "api/v1/proxyConfig";

        public ProxyConfigControllerV1Tests(FunctionTestsFixture factory, ITestOutputHelper output)
        {
            _factory = factory;
            _client = _factory.CreateClient();
            _output = output;
        }

        [Fact]
        public async Task GetProxyConfig_Success()
        {
            // Arrange
            var expectedConfig = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "Setup/Results/proxy-config.conf");
            // Act
            var result = await _client.GetAsync($"{_apiPath}");
            var configString = await result.Content.ReadAsStringAsync().ConfigureAwait(false);

            // Assert
            result.EnsureSuccessStatusCode();
            Assert.NotNull(configString);
            Assert.NotEmpty(configString);
            _output.WriteLine("expectedConfig =" + expectedConfig);
            _output.WriteLine("configString   =" + configString);
            Assert.Equal(expectedConfig.Replace("\r\n", "\n"), configString.Replace("\r\n", "\n"));
        }
    }
}
