using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using COLID.RegistrationService.Repositories.Interface;
using Microsoft.Extensions.Logging;
using COLID.RegistrationService.Common.DataModels.IronMountain;
using Newtonsoft.Json;
using COLID.IronMountainService.Common.Models;
using Microsoft.Extensions.Configuration;
using System.Threading;
using CorrelationId.Abstractions;
using Microsoft.AspNetCore.Http;
using System.Linq;
using COLID.Identity.Extensions;
using System.Security.Authentication;

namespace COLID.RegistrationService.Repositories.Implementation
{
    public class IronMountainRepository : IIronMountainRepository
    {

        private readonly ILogger<IronMountainRepository> _logger;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IConfiguration _configuration;
        private readonly CancellationToken _cancellationToken;
        private readonly ICorrelationContextAccessor _correlationContext;
        private readonly string _retentionScheduleEndpoint;
        public IronMountainRepository(ILogger<IronMountainRepository> logger, IHttpClientFactory clientFactory,
                                      IConfiguration configuration, IHttpContextAccessor httpContextAccessor,
                                      ICorrelationContextAccessor correlationContext)
        {
            _logger = logger;
            _clientFactory = clientFactory;
            _configuration = configuration;
            _cancellationToken = httpContextAccessor?.HttpContext?.RequestAborted ?? CancellationToken.None;
            _correlationContext = correlationContext;
            var serverUrl = _configuration.GetConnectionString("ironMountainConnectionUrl");
            _retentionScheduleEndpoint = $"{serverUrl}/api/5.4/retention-schedule";
        }

        public async Task<IronMountainRentionScheduleDto> GetIronMountainData()
        {
            using var httpClient = _clientFactory.CreateClient();
            Dictionary<string, string> authenticationCredentials = _configuration.GetSection("IronMountainAuthentication").GetChildren().
                    Select(x => new KeyValuePair<string, string>(x.Key, x.Value)).ToDictionary(x => x.Key, x => x.Value);

            IronMountainAuthenticationToken token = GetToken(new Uri(_configuration["ConnectionStrings:ironMountainAccessTokenUrl"]), authenticationCredentials).Result;

            _logger.LogInformation("token recieved {token}", token.AccessToken);
            HttpResponseMessage response = null;
            try
            {
                var path = $"{_retentionScheduleEndpoint}/62/rs_5fb5024870735";
                response = await httpClient.SendRequestWithOptionsAsync(HttpMethod.Get, path, null, token.AccessToken, _cancellationToken, _correlationContext.CorrelationContext);
                response.EnsureSuccessStatusCode();
            }
            catch (AuthenticationException ex)
            {
                _logger.LogError("An error occured to authenticate to iron mountain", ex);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError("Couldn't connect to iron mountain.", ex);
            }
            catch (SystemException ex)
            {
                _logger.LogError("Error while connecting to Iron Mountain.", ex);
            }
            if (response?.Content != null)
            {
                _logger.LogInformation("response {0}", response);
                var responseContent = await response.Content.ReadAsStringAsync();
                var recordClassesObject = JsonConvert.DeserializeObject<IronMountainRentionScheduleDto>(responseContent);
                return recordClassesObject;
            }
            return new IronMountainRentionScheduleDto();
        }
        private async Task<IronMountainAuthenticationToken> GetToken(Uri authenticationUrl, Dictionary<string, string> authenticationCredentials)
        {
            IronMountainAuthenticationToken token = null;
            using var httpClient = _clientFactory.CreateClient();

            FormUrlEncodedContent content = new FormUrlEncodedContent(authenticationCredentials);
            try
            {
                HttpResponseMessage response = await httpClient.PostAsync(authenticationUrl, content);

                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    string message = string.Format("Request failed while fetching IronMountain token. Received HTTP {0}", response.StatusCode);
                    _logger.LogError(message);
                    throw new ApplicationException(message);
                }

                string responseString = await response.Content.ReadAsStringAsync();

                token = JsonConvert.DeserializeObject<IronMountainAuthenticationToken>(responseString);
            }
            catch (System.Exception ex)
            {
                _logger.LogError("An error occured while calling Iron Mountain Token Url", ex);
            }
            return token;
        }
    }
}
