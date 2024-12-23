﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using COLID.Cache.Services;
using COLID.Common.Utilities;
using COLID.Exception.Models;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.Extensions;
using COLID.Identity.Extensions;
using COLID.Identity.Services;
using COLID.RegistrationService.Common.DataModel.DistributionEndpoints;
using COLID.RegistrationService.Common.DataModels.Contacts;
using COLID.RegistrationService.Common.DataModels.Search;
using COLID.RegistrationService.Common.DataModels.TransferObjects;
using COLID.RegistrationService.Services.Configuration;
using COLID.RegistrationService.Services.DataModel;
using COLID.RegistrationService.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace COLID.RegistrationService.Services.Implementation
{
    internal class RemoteAppDataService : IRemoteAppDataService
    {
        private readonly CancellationToken _cancellationToken;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RemoteAppDataService> _logger;
        private readonly ITokenService<ColidAppDataServiceTokenOptions> _tokenService;
        private readonly ICacheService _cacheService;
        private readonly bool _bypassProxy;
        private readonly string AppDataServiceConsumerGroupApi;
        private readonly string AppDataServiceColidEntryApi;
        private readonly string AppDataServiceGraphApi;
        private readonly string AppDataServiceUserApi;
        private readonly string AppDataServiceMessagesApi;
        private readonly string AppDataServiceMessageTemplatesApi;
        private readonly string AppDataServiceNotifyInvalidDistributionEndpointApi;
        private readonly string AppDataServiceNotifyInvalidContacts;
        private readonly string AppDataServiceCheckUsersAreValid;
        private readonly string AppDataServiceDeleteByAdditionalInfoApi;
        private readonly string AppDataServiceGetByAdditionalInfoApi;
        private readonly string AppDataServiceGetAllSavedSearchFiltersApi;

        public RemoteAppDataService(
            IHttpClientFactory clientFactory,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger<RemoteAppDataService> logger,
            ITokenService<ColidAppDataServiceTokenOptions> tokenService,
            ICacheService cacheService)
        {
            _clientFactory = clientFactory;
            _configuration = configuration;
            _tokenService = tokenService;
            _logger = logger;
            _cancellationToken = httpContextAccessor?.HttpContext?.RequestAborted ?? CancellationToken.None;
            _cacheService = cacheService;
            _bypassProxy = configuration.GetValue<bool>("BypassProxy");

            var serverUrl = _configuration.GetConnectionString("appDataServiceUrl");
            AppDataServiceConsumerGroupApi = $"{serverUrl}/api/consumerGroups";
            AppDataServiceColidEntryApi = $"{serverUrl}/api/colidEntries";
            AppDataServiceGraphApi = $"{serverUrl}/api/activeDirectory";
            AppDataServiceUserApi = $"{serverUrl}/api/Users";
            AppDataServiceMessagesApi = $"{serverUrl}/api/Messages";
            AppDataServiceMessageTemplatesApi = $"{serverUrl}/api/MessageTemplates";
            AppDataServiceNotifyInvalidDistributionEndpointApi = $"{serverUrl}/api/Messages/notifyUserAboutInvalidDistributionEndpoint";
            AppDataServiceNotifyInvalidContacts = $"{serverUrl}/api/Messages/notifyUserAboutInvalidContacts";
            AppDataServiceCheckUsersAreValid = $"{serverUrl}/api/ActiveDirectory/users/status";
            AppDataServiceDeleteByAdditionalInfoApi = $"{serverUrl}/api/Messages/deleteByAdditionalInfo";
            AppDataServiceGetByAdditionalInfoApi = $"{serverUrl}/api/Messages/getByAdditionalInfo";
            AppDataServiceGetAllSavedSearchFiltersApi = $"{serverUrl}/api/Users/searchAllFiltersDataMarketplace";
        }

        public async Task CreateConsumerGroup(Uri consumerGroupId)
        {
            using (var httpClient = (_bypassProxy ? _clientFactory.CreateClient("NoProxy") : _clientFactory.CreateClient()))
            {
                var consumerGroupDto = new ConsumerGroupDto() { Uri = consumerGroupId };
                var response = await AquireTokenAndSendToAppDataService(httpClient, HttpMethod.Post, AppDataServiceConsumerGroupApi, consumerGroupDto);

                CheckResponseStatus(response, "Something went wrong while creating consumer group in AppDataService");
            }
        }

        public async Task DeleteConsumerGroup(Uri consumerGroupId)
        {
            using (var httpClient = (_bypassProxy ? _clientFactory.CreateClient("NoProxy") : _clientFactory.CreateClient()))
            {
                var consumerGroupDto = new ConsumerGroupDto() { Uri = consumerGroupId };
                var response = await AquireTokenAndSendToAppDataService(httpClient, HttpMethod.Delete, AppDataServiceConsumerGroupApi, consumerGroupDto);

                CheckResponseStatus(response, "Something went wrong while deleting consumer group in AppDataService");
            }
        }

        public async Task NotifyResourcePublished(Resource resource)
        {
            using (var httpClient = (_bypassProxy ? _clientFactory.CreateClient("NoProxy") : _clientFactory.CreateClient()))
            {
                var label = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasLabel, true);
                var colidEntrySubscriptionDto = new ColidEntryCto() { Label = label, ColidPidUri = resource.PidUri };
                var response = await AquireTokenAndSendToAppDataService(httpClient, HttpMethod.Put, AppDataServiceColidEntryApi, colidEntrySubscriptionDto);

                CheckResponseStatus(response, "Something went wrong while publishing colid entry in AppDataService");
            }
        }

        public async Task NotifyResourceDeleted(Uri pidUri, Entity resource)
        {
            using (var httpClient = (_bypassProxy ? _clientFactory.CreateClient("NoProxy") : _clientFactory.CreateClient()))
            {
                var label = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasLabel, true);
                var colidEntrySubscriptionDto = new ColidEntryCto() { Label = label, ColidPidUri = pidUri };
                var response = await AquireTokenAndSendToAppDataService(httpClient, HttpMethod.Delete, AppDataServiceColidEntryApi, colidEntrySubscriptionDto);

                CheckResponseStatus(response, "Something went wrong while publishing colid entry in AppDataService");
            }
        }

        public async Task NotifyInvalidDistributionEndpoint(InvalidDistributionEndpointMessage message)
        {
            //_logger.LogInformation("NotifyInvalidDistributionEndpoint: we entered the task with the message: ", message);
            using (var httpClient = (_bypassProxy ? _clientFactory.CreateClient("NoProxy") : _clientFactory.CreateClient()))
            {
                //_logger.LogWarning($"TargetURL:Sending request to {AppDataServiceNotifyInvalidDistributionEndpointApi}");
                var response = await AquireTokenAndSendToAppDataService(
                    httpClient, 
                    HttpMethod.Post, 
                    AppDataServiceNotifyInvalidDistributionEndpointApi,
                    message);

                CheckResponseStatus(response, "Something went wrong while notifying user about invalid distribution endpoint(s)");
            }
        }

        public async Task NotifyInvalidContact(ColidEntryContactInvalidUsersDto message)
        {
            using (var httpClient = (_bypassProxy ? _clientFactory.CreateClient("NoProxy") : _clientFactory.CreateClient()))
            {
                var response = await AquireTokenAndSendToAppDataService(
                    httpClient,
                    HttpMethod.Post,
                    AppDataServiceNotifyInvalidContacts,
                    message);

                CheckResponseStatus(response, "Something went wrong while notifying user about invalid contacts");
            }
        }


        public async Task DeleteByAdditionalInfoAsync(IList<string> checkSuccessful)
        {
            using (var httpClient = (_bypassProxy ? _clientFactory.CreateClient("NoProxy") : _clientFactory.CreateClient()))
            {
                _logger.LogWarning($"TargetURL: Sending request to {AppDataServiceDeleteByAdditionalInfoApi}");
                var response = await AquireTokenAndSendToAppDataService(
                    httpClient,
                    HttpMethod.Delete,
                    AppDataServiceDeleteByAdditionalInfoApi,
                    checkSuccessful);

                CheckResponseStatus(response, "Something went wrong while deleting invalid-distribution-endpoint messages from app data service");
            }
        }
        public async Task<List<(string pidUri, DateTime createdAt)>>GetByAdditionalInfoAsync(IList<string> checkUnsuccessful)
        {
            using (var httpClient = (_bypassProxy ? _clientFactory.CreateClient("NoProxy") : _clientFactory.CreateClient()))
            {
                _logger.LogWarning($"TargetURL: Sending request to {AppDataServiceGetByAdditionalInfoApi}");
                var response = await AquireTokenAndSendToAppDataService(
                    httpClient,
                    HttpMethod.Post,
                    AppDataServiceGetByAdditionalInfoApi,
                    checkUnsuccessful);

                CheckResponseStatus(response, "Something went wrong while fetching notify-distribution-endpoint messages from appdata service)");

                var jsonResponse = await response.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<List<(string pidUri, DateTime createdAt)>>(jsonResponse);
            }
        }

        public bool CheckPerson(string id) 
        {
            Guard.IsValidEmail(id);
            var cachePersonExistsCheck = $"personexists:{id}";            
            string personExists = _cacheService.GetOrAdd(cachePersonExistsCheck,
                () =>
                {                    
                    return PersonExists(id).GetAwaiter().GetResult().ToString();
                });
            
            if (personExists.ToLower() == true.ToString().ToLower())
                return true;
            return false;
        }

        private async Task<bool> PersonExists(string id)
        {
            Guard.IsValidEmail(id); return true;
            var appDataServiceGraphUserAndGroupApi = $"{AppDataServiceGraphApi}/usersAndGroups/{id}";

            using (var httpClient = (_bypassProxy ? _clientFactory.CreateClient("NoProxy") : _clientFactory.CreateClient()))
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

        public async Task<List<ColidUserDto>> GetAllColidUser()
        {
            var appDataServiceGraphUserAndGroupApi = $"{AppDataServiceUserApi}";

            using (var httpClient = (_bypassProxy ? _clientFactory.CreateClient("NoProxy") : _clientFactory.CreateClient()))
            {
                var response = await AquireTokenAndSendToAppDataService(httpClient, HttpMethod.Get, appDataServiceGraphUserAndGroupApi, null);

                if (!response.IsSuccessStatusCode )
                {
                    var message = "Something went wrong while getting all persons from colid";
                    var result = await response.Content.ReadAsStringAsync();
                    _logger.LogError(message, result);
                    throw new TechnicalException(message);
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();

                List<ColidUserDto> userList = JsonConvert.DeserializeObject<List<ColidUserDto>>(jsonResponse);
                return userList;
            }
        }

        public async Task<List<MessageTemplateDto>> GetAllMessageTemplates()
        {

            using (var httpClient = (_bypassProxy ? _clientFactory.CreateClient("NoProxy") : _clientFactory.CreateClient()))
            {
                var response = await AquireTokenAndSendToAppDataService(httpClient, HttpMethod.Get, this.AppDataServiceMessageTemplatesApi, null);

                if (!response.IsSuccessStatusCode)
                {
                    var message = "Something went wrong while getting all message templates";
                    var result = await response.Content.ReadAsStringAsync();
                    _logger.LogError(message, result);
                    throw new TechnicalException(message);
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();

                List<MessageTemplateDto> MessageTemplateList = JsonConvert.DeserializeObject<List<MessageTemplateDto>>(jsonResponse);
                return MessageTemplateList;
            }
        }

        public async Task<IList<AdUserDto>> CheckUsersValidity(ISet<string> userEmails)
        {
            //_logger.LogInformation("Following emails will be checked {Emails}", JsonConvert.SerializeObject(userEmails));
            using (var httpClient = (_bypassProxy ? _clientFactory.CreateClient("NoProxy") : _clientFactory.CreateClient()))
            {
                var response = await AquireTokenAndSendToAppDataService(httpClient, HttpMethod.Post, AppDataServiceCheckUsersAreValid, userEmails);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();

                IList<AdUserDto> adUserDtos = JsonConvert.DeserializeObject<IList<AdUserDto>>(jsonResponse);
                return adUserDtos;
            }
        }

        private async Task<HttpResponseMessage> AquireTokenAndSendToAppDataService(HttpClient httpClient, HttpMethod httpMethod, string endpointUrl, object requestBody)
        {
            var accessToken = await _tokenService.GetAccessTokenForWebApiAsync();
            var response = await httpClient.SendRequestWithOptionsAsync(httpMethod, endpointUrl,
                requestBody, accessToken, _cancellationToken);
            //_logger.LogInformation("AquireTokenAndSendToAppDataService: with the response: ", response);
            //Console.WriteLine("in remotAppDataServ/AquireTokenAnd... response: "+response);
            return response;
        }

        private static void CheckResponseStatus(HttpResponseMessage response, string errorMessage)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new GeneralException(errorMessage);
            }
        }

        public async Task SendGenericMessage(string subject, string body, string email)
        {
            var AppDataServiceMessagesApi = $"{this.AppDataServiceMessagesApi}/sendGenericMessage";

            using (var httpClient = (_bypassProxy ? _clientFactory.CreateClient("NoProxy") : _clientFactory.CreateClient()))
            {
                var colidEntrySubscriptionDto = new MessageUserDto() { Subject = subject, Body = body, UserEmail = email };
                var response = await AquireTokenAndSendToAppDataService(httpClient, HttpMethod.Put, AppDataServiceMessagesApi, colidEntrySubscriptionDto);

                if (!response.IsSuccessStatusCode)
                {
                    var message = "Something went wrong while sending message to user";
                    throw new TechnicalException(message);
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();

                  
            }
        }

        public async Task<List<SearchFilterProxyDTO>> GetAllSavedSearchFilters()
        {

            using (var httpClient = (_bypassProxy ? _clientFactory.CreateClient("NoProxy") : _clientFactory.CreateClient()))
            {
                var response = await AquireTokenAndSendToAppDataService(httpClient, HttpMethod.Get, AppDataServiceGetAllSavedSearchFiltersApi, null);

                if (!response.IsSuccessStatusCode)
                {
                    var message = "Something went wrong while getting all saved search filters";
                    var result = await response.Content.ReadAsStringAsync();
                    _logger.LogError(message, result);
                    throw new TechnicalException(message);
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();

                List<SearchFilterProxyDTO> allSearchFiltersList = JsonConvert.DeserializeObject<List<SearchFilterProxyDTO>>(jsonResponse);
                return allSearchFiltersList.Where(x=>x.PidUri!=null).ToList();
            }
        }
    }
}
