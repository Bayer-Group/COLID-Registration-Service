using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using COLID.Common.Utilities;
using COLID.Exception.Models;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.TripleStore.Extensions;
using COLID.Identity.Extensions;
using COLID.Identity.Services;
using COLID.RegistrationService.Services.Configuration;
using COLID.RegistrationService.Services.DataModel;
using COLID.RegistrationService.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace COLID.RegistrationService.Services.Implementation
{
    internal class RemoteAppDataService : IRemoteAppDataService
    {
        private readonly CancellationToken _cancellationToken;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RemoteAppDataService> _logger;
        private readonly ITokenService<ColidAppDataServiceTokenOptions> _tokenService;

        private readonly string AppDataServiceConsumerGroupApi;
        private readonly string AppDataServiceColidEntryApi;
        private readonly string AppDataServiceGraphApi;

        public RemoteAppDataService(
            IHttpClientFactory clientFactory,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger<RemoteAppDataService> logger,
            ITokenService<ColidAppDataServiceTokenOptions> tokenService)
        {
            _clientFactory = clientFactory;
            _configuration = configuration;
            _tokenService = tokenService;
            _logger = logger;
            _cancellationToken = httpContextAccessor?.HttpContext?.RequestAborted ?? CancellationToken.None;

            var serverUrl = _configuration.GetConnectionString("appDataServiceUrl");
            AppDataServiceConsumerGroupApi = $"{serverUrl}/api/consumerGroups";
            AppDataServiceColidEntryApi = $"{serverUrl}/api/colidEntries";
            AppDataServiceGraphApi = $"{serverUrl}/api/activeDirectory";
        }

        public async Task CreateConsumerGroup(Uri consumerGroupId)
        {
            using (var httpClient = _clientFactory.CreateClient())
            {
                var consumerGroupDto = new ConsumerGroupDto() { Uri = consumerGroupId };
                var response = await AquireTokenAndSendToAppDataService(httpClient, HttpMethod.Post, AppDataServiceConsumerGroupApi, consumerGroupDto);

                CheckResponseStatus(response, "Something went wrong while creating consumer group in AppDataService");
            }
        }

        public async Task DeleteConsumerGroup(Uri consumerGroupId)
        {
            using (var httpClient = _clientFactory.CreateClient())
            {
                var consumerGroupDto = new ConsumerGroupDto() { Uri = consumerGroupId };
                var response = await AquireTokenAndSendToAppDataService(httpClient, HttpMethod.Delete, AppDataServiceConsumerGroupApi, consumerGroupDto);

                CheckResponseStatus(response, "Something went wrong while deleting consumer group in AppDataService");
            }
        }

        public async Task NotifyResourcePublished(Resource resource)
        {
            using (var httpClient = _clientFactory.CreateClient())
            {
                var label = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasLabel, true);
                var colidEntrySubscriptionDto = new ColidEntryCto() { Label = label, ColidPidUri = resource.PidUri };
                var response = await AquireTokenAndSendToAppDataService(httpClient, HttpMethod.Put, AppDataServiceColidEntryApi, colidEntrySubscriptionDto);

                CheckResponseStatus(response, "Something went wrong while publishing colid entry in AppDataService");
            }
        }

        public async Task NotifyResourceDeleted(Resource resource)
        {
            using (var httpClient = _clientFactory.CreateClient())
            {
                var label = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasLabel, true);
                var colidEntrySubscriptionDto = new ColidEntryCto() { Label = label, ColidPidUri = resource.PidUri };
                var response = await AquireTokenAndSendToAppDataService(httpClient, HttpMethod.Delete, AppDataServiceColidEntryApi, colidEntrySubscriptionDto);

                CheckResponseStatus(response, "Something went wrong while publishing colid entry in AppDataService");
            }
        }

        public async Task<bool> CheckPerson(string id)
        {
            Guard.IsValidEmail(id);

            var appDataServiceGraphUserAndGroupApi = $"{AppDataServiceGraphApi}/usersAndGroups/{id}";

            using (var httpClient = _clientFactory.CreateClient())
            {
                var response = await AquireTokenAndSendToAppDataService(httpClient, HttpMethod.Get, appDataServiceGraphUserAndGroupApi, null);

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        return false;
                    }

                    var result = await response.Content.ReadAsStringAsync();
                    var message = "Something went wrong while validating person fields through active directory, id={id}\ncontent={content}";

                    _logger.LogError(message, id, result);
                    throw new TechnicalException(message);
                }

                return true;
            }
        }

        private async Task<HttpResponseMessage> AquireTokenAndSendToAppDataService(HttpClient httpClient, HttpMethod httpMethod, string endpointUrl, object requestBody)
        {
            var accessToken = await _tokenService.GetAccessTokenForWebApiAsync();
            var response = await httpClient.SendRequestWithBearerTokenAsync(httpMethod, endpointUrl,
                requestBody, accessToken, _cancellationToken);
            return response;
        }

        private static void CheckResponseStatus(HttpResponseMessage response, string errorMessage)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new GeneralException(errorMessage);
            }
        }
    }
}
