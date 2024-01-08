using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using COLID.Identity.Extensions;
using COLID.Identity.Services;
using COLID.RegistrationService.Common.Constants;
using COLID.RegistrationService.Common.DataModels.RelationshipManager;
using COLID.RegistrationService.Services.Configuration;
using COLID.RegistrationService.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace COLID.RegistrationService.Services.Implementation
{
    internal class RemoteRRMService : IRemoteRRMService
    {
        private readonly CancellationToken _cancellationToken;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IConfiguration _configuration;
        private readonly ITokenService<ColidRRMServiceTokenOptions> _tokenService;
        private readonly ILogger<RemoteRRMService> _logger;
        private readonly bool _bypassProxy;
        private readonly string _rrmServiceGetAllMapsEndpoint;

        public RemoteRRMService(
            IHttpClientFactory clientFactory,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ITokenService<ColidRRMServiceTokenOptions> tokenService,
            ILogger<RemoteRRMService> logger
            )
        {
            _clientFactory = clientFactory;
            _configuration = configuration;
            _tokenService = tokenService;
            _cancellationToken = httpContextAccessor?.HttpContext?.RequestAborted ?? CancellationToken.None;
            _logger = logger;
            _bypassProxy = _configuration.GetValue<bool>("BypassProxy");
            var serverUrl = _configuration.GetConnectionString("rrmAPIUrl");
            _rrmServiceGetAllMapsEndpoint = $"{serverUrl}/api/GraphMap/All/pidURIs";


        }

        public async Task<List<MapProxyDTO>> GetAllRRMMaps()
        {
            using (var httpClient = (_bypassProxy ? _clientFactory.CreateClient("NoProxy") : _clientFactory.CreateClient()))
            {
                HttpResponseMessage response = null;
                List<MapProxyDTO> responseContent = null; 
                try
                {
                    var path = $"{_rrmServiceGetAllMapsEndpoint}";
                    var accessToken = await _tokenService.GetAccessTokenForWebApiAsync();
                    response = await httpClient.SendRequestWithBearerTokenAsync(HttpMethod.Get, path,
                        null, accessToken, _cancellationToken);
                    response.EnsureSuccessStatusCode();
                    if (response.Content != null)
                    {
                        responseContent = await response.Content.ReadAsAsync<List<MapProxyDTO>>();
                    }
                }
                catch (AuthenticationException ex)
                {
                    _logger.LogError("An Authentication error occured to connect to the remote rrm service", ex);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError("Couldn't connect to the remote rrm service.", ex);
                }

                return responseContent;
            }
        }
    }
}
