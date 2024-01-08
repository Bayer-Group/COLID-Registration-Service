using System;
using System.Collections.Generic;
using System.Linq;
using ColidConstants = COLID.RegistrationService.Common.Constants;
using COLID.Exception.Models;
using COLID.RegistrationService.Common.Constants;
using COLID.RegistrationService.Common.DataModels.Contacts;
using System.Threading.Tasks;
using COLID.RegistrationService.Services.Interface;
using Microsoft.Extensions.Logging;
using COLID.MessageQueue.Datamodel;
using Newtonsoft.Json;
using COLID.Graph.Metadata.Services;
using COLID.RegistrationService.Services.Extensions;
using Newtonsoft.Json.Serialization;
using COLID.MessageQueue.Configuration;
using Microsoft.Extensions.Options;
using COLID.RegistrationService.Common.DataModel.DistributionEndpoints;
using System.Net.Http;
using System.Net;
using COLID.MessageQueue.Services;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.RegistrationService.Repositories.Interface;
using COLID.Graph.Metadata.Constants;
using COLID.Common.Utilities;

namespace COLID.RegistrationService.Services.Implementation
{
    public class ResourceDataValidityCheckService : IResourceDataValidityCheckService, IMessageQueueReceiver, IMessageQueuePublisher
    {
        private readonly IRemoteAppDataService _remoteAppDataService;
        private readonly IResourceService _resourceService;
        private readonly IResourceRepository _resourceRepository;
        private readonly IMetadataService _metadataService;
        private readonly IReindexingService _indexingService;
        private readonly HttpClient _client;
        private readonly ColidMessageQueueOptions _mqOptions;
        private readonly string _brokenEndpointTopic;
        private readonly string _brokenContactTopic;
        private readonly string _setBrokenFlagsTopic;
        private readonly JsonSerializerSettings _serializerSettings;
        private readonly ILogger<ResourceDataValidityCheckService> _logger;

        public Action<string, string, BasicProperty> PublishMessage { get; set; }
        public IDictionary<string, Action<string>> OnTopicReceivers => new Dictionary<string, Action<string>>()
        {
            { _mqOptions.Topics[_brokenEndpointTopic], TestEndpoints},
            { _mqOptions.Topics[_brokenContactTopic], TestContacts },
            { _mqOptions.Topics[_setBrokenFlagsTopic], SetInvalidContactsOrDistributionEndpointsFlagInElastic },
        };

        public ResourceDataValidityCheckService(
            IRemoteAppDataService remoteAppDataService,
            IResourceService resourceService,
            IResourceRepository resourceRepository,
            IMetadataService metadata,
            IReindexingService reindexingService,
            IHttpClientFactory clientFactory,
            IOptionsMonitor<ColidMessageQueueOptions> messageQueueOptionsAccessor,
            ILogger<ResourceDataValidityCheckService> logger)
        {
            _remoteAppDataService = remoteAppDataService;
            _resourceService = resourceService;
            _resourceRepository = resourceRepository;
            _metadataService = metadata;
            _indexingService = reindexingService;
            _client = clientFactory.CreateClient();
            _mqOptions = messageQueueOptionsAccessor.CurrentValue;
            _brokenEndpointTopic = "TargetURLChecking";
            _brokenContactTopic = "ContactValidityChecking";
            _setBrokenFlagsTopic = "SetBrokenFlags";
            _serializerSettings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            _logger = logger;
        }

        public void PushContactsToCheckInQueue()
        {
            var contactsToCheck = GetContactsToCheck();

            var queueItem = JsonConvert.SerializeObject(contactsToCheck, _serializerSettings);
            PublishMessage(_mqOptions.Topics[_brokenContactTopic], queueItem, new BasicProperty() { Priority = 0 });
        }

        public void PushEndpointsInQueue()
        {
            var resourceTypes = _metadataService.GetInstantiableEntityTypes(Graph.Metadata.Constants.Resource.Type.FirstResouceType);
            var endpoints = _resourceService.GetDistributionEndpoints(resourceTypes, null);
            _logger.LogInformation("Testing {Count} endpoints in chunks of 20", endpoints.Count);
            var endpointsBatch = endpoints.Batch(20);

            foreach (var batch in endpointsBatch)
            {
                var _queueItem = JsonConvert.SerializeObject(batch.ToList(), _serializerSettings);
                PublishMessage(_mqOptions.Topics[_brokenEndpointTopic], _queueItem, new BasicProperty() { Priority = 0 });
            }
        }

        public void PushSingleEndpointInQueue(Uri distributionPidUri)
        {
            var resourceTypes = _metadataService.GetInstantiableEntityTypes(Graph.Metadata.Constants.Resource.Type.FirstResouceType);
            var endpoints = _resourceService.GetDistributionEndpoints(resourceTypes, distributionPidUri);
            var queueItem = JsonConvert.SerializeObject(endpoints, _serializerSettings);
            PublishMessage(_mqOptions.Topics[_brokenEndpointTopic], queueItem, new BasicProperty() { Priority = 0 });
        }

        public void PushDataFlaggingInQueue()
        {
            var brokenResourcePidUris = GetPidUrisForInvalidDataResources();
            var queueItem = JsonConvert.SerializeObject(brokenResourcePidUris, _serializerSettings);
            PublishMessage(_mqOptions.Topics[_setBrokenFlagsTopic], queueItem, new BasicProperty() { Priority = 0 });
        }

        public void TestContacts(string mqValue)
        {
            var contactsToCheck = JsonConvert.DeserializeObject<ContactsToCheckDto>(mqValue, _serializerSettings);
            var invalidUserEmails = new HashSet<string>();

            var uniqueUsersToCheck = contactsToCheck.DataStewards.Union(contactsToCheck.DistributionEndpointContactPersons).ToHashSet();

            foreach (var email in uniqueUsersToCheck)
            {
                try
                {
                    Guard.IsValidEmail(email);
                }
                catch
                {
                    invalidUserEmails.Add(email);
                }
            }

            uniqueUsersToCheck.RemoveWhere(x => invalidUserEmails.Contains(x));
            var userCheckResult = _remoteAppDataService.CheckUsersValidity(uniqueUsersToCheck).Result;

            invalidUserEmails.UnionWith(userCheckResult.Where(u => !u.AccountEnabled).Select(x => x.Mail).ToHashSet());
            _logger.LogInformation("Found following invalid users {Users}", JsonConvert.SerializeObject(invalidUserEmails));

            if (invalidUserEmails.Count > 0)
            {
                Dictionary<string, ColidEntryContactInvalidUsersDto> notificationsToSend = new Dictionary<string, ColidEntryContactInvalidUsersDto>();
                var invalidDataStewards = contactsToCheck.DataStewards.Where(email => invalidUserEmails.Contains(email)).ToList();
                if (invalidDataStewards.Count > 0)
                {
                    var resourcesWithInvalidDataStewards = GetResourcesForInvalidDataStewards(invalidDataStewards);
                    foreach ((string key, InvalidDataStewardResourceInformation value) in resourcesWithInvalidDataStewards)
                    {
                        // flag resource with invalid data stewards
                        foreach (var invalidDataSteward in value.InvalidDataStewards)
                        {
                            _resourceService.CreateProperty(new Uri(key), new Uri(ContactValidityCheck.BrokenDataStewards), invalidDataSteward, _resourceService.GetResourceInstanceGraph());
                        }

                        // notification order depending on validity and availability
                        // -> other valid data stewards
                        // -> last reviewer
                        // -> last change user
                        // -> author
                        // -> consumer group contact person
                        if (value.ValidDataStewards.Count > 0)
                        {
                            foreach (var dataSteward in value.ValidDataStewards)
                            {
                                AddEntryToNotifications(notificationsToSend, dataSteward, value);
                            }
                        }
                        else if (value.LastReviewer != null && _remoteAppDataService.CheckPerson(value.LastReviewer))
                        {
                            AddEntryToNotifications(notificationsToSend, value.LastReviewer, value);
                        }
                        else if (value.LastChangeUser != null && _remoteAppDataService.CheckPerson(value.LastChangeUser))
                        {
                            AddEntryToNotifications(notificationsToSend, value.LastChangeUser, value);
                        }
                        else if (_remoteAppDataService.CheckPerson(value.Author))
                        {
                            AddEntryToNotifications(notificationsToSend, value.Author, value);
                        }
                        else
                        {
                            AddEntryToNotifications(notificationsToSend, value.ConsumerGroupContactPerson, value);
                        }
                    }
                }
                var invalidDistributionEndpointContacts = contactsToCheck.DistributionEndpointContactPersons.Where(email => invalidUserEmails.Contains(email)).ToList();
                if (invalidDistributionEndpointContacts.Count > 0)
                {
                    var resourcesWithInvalidDistributionEndpointContacts = GetResourcesForInvalidDistributionEndpointContacts(invalidDistributionEndpointContacts);

                    foreach (var resource in resourcesWithInvalidDistributionEndpointContacts)
                    {
                        // flag resource with invalid distribution endpoint contacts
                        _resourceService.CreateProperty(new Uri(resource.DistributionId), new Uri(ContactValidityCheck.BrokenEndpointContacts), resource.InvalidDistributionEndpointContact, _resourceService.GetResourceInstanceGraph());

                        // notification order depending on validity and availability
                        // -> last reviewer
                        // -> last change user
                        // -> author
                        // -> consumer group contact person
                        if (resource.LastReviewer != null && _remoteAppDataService.CheckPerson(resource.LastReviewer))
                        {
                            AddEntryToNotifications(notificationsToSend, resource.LastReviewer, resource);
                        }
                        else if (resource.LastChangeUser != null && _remoteAppDataService.CheckPerson(resource.LastChangeUser))
                        {
                            AddEntryToNotifications(notificationsToSend, resource.LastChangeUser, resource);
                        }
                        else if (_remoteAppDataService.CheckPerson(resource.Author))
                        {
                            AddEntryToNotifications(notificationsToSend, resource.Author, resource);
                        }
                        else
                        {
                            AddEntryToNotifications(notificationsToSend, resource.ConsumerGroupContactPerson, resource);
                        }
                    }
                }

                _logger.LogInformation("Prepared {NotificationsCount} notifications which should be sent next", notificationsToSend.Count);
                foreach (var notification in notificationsToSend.Values)
                {
                    try
                    {
                        _remoteAppDataService.NotifyInvalidContact(notification).Wait();
                    }
                    catch (GeneralException e)
                    {
                        _logger.LogError("Unable to send notification to user with email {Email}", notification.ContactMail);
                    }
                }
            }
        }

        public void TestEndpoints(string mqValue)
        {
            List<DistributionEndpointsTest> endPoints = JsonConvert.DeserializeObject<List<DistributionEndpointsTest>>(mqValue, _serializerSettings);
            List<(DistributionEndpointsTest endPoint, bool result)> endpointTestResult = new List<(DistributionEndpointsTest, bool)>();
            foreach (var endpoint in endPoints)
            {
                _logger.LogInformation("Current endpoint being tested: {Endpoint}", endpoint.NetworkAddress);
                endpointTestResult.Add(TestSingleEndpoint(endpoint));
            }
            var checkSuccessful = endpointTestResult.FindAll(x => x.result);
            var checkUnsuccessful = endpointTestResult.FindAll(x => !x.result);

            _logger.LogInformation("There were {Count} invalid links", checkUnsuccessful.Count);
            _logger.LogInformation("Following links were unsucessful {Links}", JsonConvert.SerializeObject(checkUnsuccessful.ToArray()));

            SetBrokenEndpointFlag(checkUnsuccessful);

            // remove notifations for valid distribution endpoints if there is one
            _remoteAppDataService.DeleteByAdditionalInfoAsync(checkSuccessful.Select(x => x.endPoint.DistributionEndpointSubject).ToList())
                    .Wait();


            var distributionEndpointPidURIs = _remoteAppDataService.GetByAdditionalInfoAsync(
                                                                checkUnsuccessful.Select(x => x.endPoint.DistributionEndpointSubject).ToList()
                                                                ).Result;
            foreach (var result in checkUnsuccessful)
            {
                if (distributionEndpointPidURIs.Any(x => x.pidUri.Equals(result.endPoint.DistributionEndpointSubject, StringComparison.Ordinal)))
                {
                    var endpointFromAds = distributionEndpointPidURIs.FirstOrDefault(x => x.pidUri.Equals(result.endPoint.DistributionEndpointSubject, StringComparison.Ordinal));
                    if (endpointFromAds.createdAt.AddDays(21) < DateTime.UtcNow) //check for 3 week period
                    {
                        //here the marking should be changed into persisting in a list
                        _resourceService.MarkDistributionEndpointAsDeprecated(new Uri(result.endPoint.PidUri));
                    }
                }
                else
                {
                    var invalidDistributionEndpointMessage = new InvalidDistributionEndpointMessage()
                    {
                        UserEmail = result.endPoint.LastChangeUser,
                        ResourceLabel = result.endPoint.ResourceLabel,
                        DistributionEndpoint = new Uri(result.endPoint.NetworkAddress),
                        ColidEntryPidUri = result.endPoint.PidUri,
                        DistributionEndpointPidUri = result.endPoint.DistributionEndpointSubject
                    };

                    if (_remoteAppDataService.CheckPerson(invalidDistributionEndpointMessage.UserEmail))
                    {
                        _remoteAppDataService.NotifyInvalidDistributionEndpoint(invalidDistributionEndpointMessage).Wait();
                    }
                    else if (_remoteAppDataService.CheckPerson(result.endPoint.Author))
                    {
                        invalidDistributionEndpointMessage.UserEmail = result.endPoint.Author;
                        _remoteAppDataService.NotifyInvalidDistributionEndpoint(invalidDistributionEndpointMessage).Wait();
                    }
                    else
                    {
                        invalidDistributionEndpointMessage.UserEmail = result.endPoint.ConsumerGroupContactPerson;
                        _remoteAppDataService.NotifyInvalidDistributionEndpoint(invalidDistributionEndpointMessage).Wait();
                    }
                }
            }
        }

        public IList<DistributionEndpointsTest> GetBrokenEndpoints()
        {
            var resourceTypes = _metadataService.GetInstantiableEntityTypes(Graph.Metadata.Constants.Resource.Type.FirstResouceType);

            return _resourceService.GetBrokenEndpoint(resourceTypes);
        }

        public void SetInvalidContactsOrDistributionEndpointsFlagInElastic(string mqValue)
        {
            var brokenResourcePidUris = JsonConvert.DeserializeObject<List<Uri>>(mqValue, _serializerSettings);

            foreach (var pidUri in brokenResourcePidUris)
            {
                try
                {
                    var resource = _resourceService.GetByPidUri(pidUri);
                    ResourcesCTO resourceCTO = _resourceService.GetResourcesByPidUri(pidUri);
                    _indexingService.IndexPublishedResource(pidUri, resource, resourceCTO);
                }
                catch
                {
                    _logger.LogInformation("Failed setting flags for resource with PID-URI {PID}", pidUri.ToString());
                }
                
            }
        }

        public IList<Uri> GetPidUrisForInvalidDataResources()
        {
            Uri instanceGraphUri = _resourceService.GetResourceInstanceGraph();
            return _resourceRepository.GetResourcePidUrisWithBrokenEndpointsOrBrokenContacts(instanceGraphUri);
        }

        private ContactsToCheckDto GetContactsToCheck()
        {
            Uri instanceGraphUri = _resourceService.GetResourceInstanceGraph();
            return _resourceRepository.GetContactsToCheck(instanceGraphUri);
        }

        private Dictionary<string, InvalidDataStewardResourceInformation> GetResourcesForInvalidDataStewards(IList<string> invalidContacts)
        {
            Uri instanceGraphUri = _resourceService.GetResourceInstanceGraph();
            Uri consumerGroupGraph = _metadataService.GetInstanceGraph(ConsumerGroup.Type);

            return _resourceRepository.GetResourcesForInvalidDataStewards(invalidContacts, instanceGraphUri, consumerGroupGraph);

        }

        private IList<InvalidDistributionEndpointContactResourceInformation> GetResourcesForInvalidDistributionEndpointContacts(IList<string> invalidContacts)
        {
            Uri instanceGraphUri = _resourceService.GetResourceInstanceGraph();
            Uri consumerGroupGraph = _metadataService.GetInstanceGraph(ConsumerGroup.Type);

            return _resourceRepository.GetResourcesForInvalidDistributionEndpointContacts(invalidContacts, instanceGraphUri, consumerGroupGraph);
        }

        private static void AddEntryToNotifications(Dictionary<string, ColidEntryContactInvalidUsersDto> notificationsToSend, string email, InvalidDataStewardResourceInformation value)
        {
            if (!notificationsToSend.ContainsKey(email))
            {
                notificationsToSend.Add(email, new ColidEntryContactInvalidUsersDto(email, new ColidEntryInvalidUsersDto(value.PidUri, value.Label, value.InvalidDataStewards)));
            }
            else
            {
                notificationsToSend.TryGetValue(email, out var entry);
                entry.ColidEntries.Add(new ColidEntryInvalidUsersDto(value.PidUri, value.Label, value.InvalidDataStewards));
            }
        }

        private static void AddEntryToNotifications(Dictionary<string, ColidEntryContactInvalidUsersDto> notificationsToSend, string email, InvalidDistributionEndpointContactResourceInformation value)
        {
            if (!notificationsToSend.ContainsKey(email))
            {
                notificationsToSend.Add(email, new ColidEntryContactInvalidUsersDto(email, new ColidEntryInvalidUsersDto(value.PidUri, $"{value.Label} with distribution endpoint {System.Text.RegularExpressions.Regex.Replace(value.DistributionLabel, "<.*?>", String.Empty)}", new List<string>(1) { value.InvalidDistributionEndpointContact })));
            }
            else
            {
                notificationsToSend.TryGetValue(email, out var entry);
                entry.ColidEntries.Add(new ColidEntryInvalidUsersDto(value.PidUri, $"{value.Label} with distribution endpoint {System.Text.RegularExpressions.Regex.Replace(value.DistributionLabel, "<.*?>", String.Empty)}", new List<string>(1) { value.InvalidDistributionEndpointContact }));
            }
        }

        private (DistributionEndpointsTest, bool) TestSingleEndpoint(DistributionEndpointsTest endpoint)
        {
            bool result = false;
            try
            {
                var distributionEndpoint = endpoint.NetworkAddress;
                var responseMessage = _client.GetAsync(distributionEndpoint).Result;
                var responseString = responseMessage.Content.ReadAsStringAsync().Result;

                _logger.LogInformation("Status code for {request} is {statusCode}", endpoint.NetworkAddress, responseMessage.StatusCode);
                switch (responseMessage.StatusCode)
                {
                    /*204, 205*/
                    case HttpStatusCode.NoContent:
                    case HttpStatusCode.ResetContent:
                        result = false;
                        break;

                    /*401, 402, 403, 406, 409, 411, 413, 415, 416, 417, [418], 422, 423, 424, [425], 426, 429, 431 */
                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.PaymentRequired:
                    case HttpStatusCode.Forbidden:
                    case HttpStatusCode.NotAcceptable:
                    case HttpStatusCode.UpgradeRequired:
                    case HttpStatusCode.TooManyRequests:
                    case HttpStatusCode.Conflict:
                    case HttpStatusCode.LengthRequired:
                    case HttpStatusCode.RequestEntityTooLarge:
                    case HttpStatusCode.UnsupportedMediaType:
                    case HttpStatusCode.RequestedRangeNotSatisfiable:
                    case HttpStatusCode.ExpectationFailed:
                    case HttpStatusCode.RequestHeaderFieldsTooLarge:
                    case HttpStatusCode.UnprocessableEntity:
                    case HttpStatusCode.Locked:
                    case HttpStatusCode.FailedDependency:
                        result = true;
                        break;

                    default:
                        var codeInitial = Math.Floor((double)responseMessage.StatusCode / 100); //e.g take 2 out of 200 and 3 out of 300 and so on
                        switch (codeInitial)
                        {
                            case 1: /*100+*/
                                result = true;
                                break;
                            case 2:/*200+*/
                                result = true;
                                //result = validate_content(string.Empty, string.Empty);
                                break;
                            case 3:/*300+*/
                                result = true;
                                break;
                            case 4:/*400+*/
                                result = false;
                                break;
                            case 5:/*500+*/
                                result = true;
                                break;
                        }
                        break;
                }
            }
            //if not 200 it will fail 
            catch (System.Exception exception)
            {
                // in case there is a timeout we cant check the validity of the endpoint and therefore set it to true
                _logger.LogError(exception.Message);
                result = true;
            }
            endpoint.CheckedFlag = !result; //if the checkedflag is true that means the link is broken
            return (endpoint, result); //the tested endpoint and the test result
        }

        private void SetBrokenEndpointFlag(IList<(DistributionEndpointsTest endPoint, bool result)> checkUnsuccessful)
        {
            foreach ((DistributionEndpointsTest endPoint, bool result) item in checkUnsuccessful)
            {
                _resourceService.CreateProperty(new Uri(item.endPoint.DistributionEndpointSubject),
                    new Uri(ColidConstants.DistributionEndpoint.DistributionEndpointsTest.EndpointLifecycleStatus), ColidConstants.DistributionEndpoint.DistributionEndpointsTest.Broken,                    // to update in db (DistributionEndpointSubject,"https://pid.bayer.com/kos/19050/hasEndpointURLStatus", "broken")
                    _resourceService.GetResourceInstanceGraph());

            }
        }
    }
}
