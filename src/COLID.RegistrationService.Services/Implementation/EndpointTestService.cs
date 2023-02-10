using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using COLID.Graph.Metadata.Services;
using COLID.MessageQueue.Configuration;
using COLID.MessageQueue.Datamodel;
using COLID.MessageQueue.Services;
using COLID.RegistrationService.Common.DataModel.DistributionEndpoints;
using COLID.RegistrationService.Services.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace COLID.RegistrationService.Services.Implementation
{
    public class EndpointTestService : IEndpointTestService, IMessageQueueReceiver, IMessageQueuePublisher
    {
        private readonly IRemoteAppDataService _remoteAppDataService;
        private readonly ColidMessageQueueOptions _mqOptions;
        private readonly ILogger<EndpointTestService> _logger;
        private readonly JsonSerializerSettings _serializerSettings;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly HttpClient _client;
        private readonly string _topicName;
        public Action<string, string, BasicProperty> PublishMessage { get; set; }

        public IDictionary<string, Action<string>> OnTopicReceivers => new Dictionary<string, Action<string>>()
        {
            { _mqOptions.Topics[_topicName], TestEndpoints}
        };

        public EndpointTestService(
            IOptionsMonitor<ColidMessageQueueOptions> messageQueueOptionsAccessor,
            ILogger<EndpointTestService> logger,
            IHttpClientFactory clientFactory,
            IRemoteAppDataService remoteAppDataService,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _client = clientFactory.CreateClient();
            _remoteAppDataService = remoteAppDataService;
            _mqOptions = messageQueueOptionsAccessor.CurrentValue;
            _topicName = "TargetURLChecking";
            _serviceScopeFactory = serviceScopeFactory;

            _serializerSettings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
        }

        public void PushEndpointsInQueue()
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var _resourceService = scope.ServiceProvider.GetService<IResourceService>();
                var _metadataService = scope.ServiceProvider.GetService<IMetadataService>();
                var resourceTypes = _metadataService.GetInstantiableEntityTypes(Graph.Metadata.Constants.Resource.Type.FirstResouceType);
                foreach (var resourceType in resourceTypes)
                {
                    int index = 0;
                    var endPoints = _resourceService.GetDistributionEndpoints(resourceType);
                    //var batchList = endPoints.GroupBy(x => (index++ / 20));
                    var batchList = endPoints.Select((x, i) => new { Index = i, Value = x })
                                    .GroupBy(x => x.Index / 20)
                                    .Select(x => x.Select(v => v.Value));

                    foreach (var batch in batchList)
                    {
                        var _queueItem = JsonConvert.SerializeObject(batch?.ToList(), _serializerSettings);
                        PublishMessage(_mqOptions.Topics[_topicName], _queueItem, new BasicProperty() { Priority = 0 });
                    }
                }
            }

        }

        public void TestEndpoints(string mqValue)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                //var _resourceService = scope.ServiceProvider.GetService<IResourceService>();
                IResourceService _resourceService = null;
                List<DistributionEndpointsTest> endPoints = JsonConvert.DeserializeObject<List<DistributionEndpointsTest>>(mqValue, _serializerSettings);

                List<(DistributionEndpointsTest endPoint, bool result)> endpoint_test_result = new List<(DistributionEndpointsTest, bool)>();

                foreach (var endpoint in endPoints)
                {
                    endpoint_test_result.Add(TestSingleEndpoint(endpoint));
                }

                var checkSuccessful = endpoint_test_result.FindAll(x => x.result);
                var checkUnsuccessful = endpoint_test_result.FindAll(x => !x.result);

                _remoteAppDataService.DeleteByAdditionalInfoAsync(checkSuccessful.Select(x => x.endPoint.DistributionEndpointPidUri).ToList())
                    .Wait();

                var distributionEndpointPidURIs = _remoteAppDataService.GetByAdditionalInfoAsync(
                                                                checkUnsuccessful.Select(x => x.endPoint.DistributionEndpointPidUri).ToList()
                                                                ).Result;

                foreach (var result in checkUnsuccessful)
                {
                    if (distributionEndpointPidURIs.Any(x => x.pidUri.Equals(result.endPoint.DistributionEndpointPidUri)))
                    {
                        var endpointFromAds = distributionEndpointPidURIs.Single(x => x.pidUri.Equals(result.endPoint.DistributionEndpointPidUri));
                        if (endpointFromAds.createdAt.AddDays(21) < DateTime.UtcNow) //check for 3 week period
                        {
                            _resourceService.MarkDistributionEndpointAsDeprecated(new Uri(result.endPoint.PidUri));
                        }
                    }
                    else
                    {
                        var invalidDistributionEndpointMessage = new InvalidDistributionEndpointMessage()
                        {
                            ColidEntryPidUri = result.endPoint.PidUri,
                            UserEmail = result.endPoint.Author,
                            ResourceLabel = result.endPoint.ResourceLabel,
                            DistributionEndpoint = new Uri(result.endPoint.NetworkAddress),
                            DistributionEndpointPidUri = result.endPoint.DistributionEndpointPidUri
                        };
                        _remoteAppDataService.NotifyInvalidDistributionEndpoint(invalidDistributionEndpointMessage).Wait();
                    }
                }
            }
        }
        private (DistributionEndpointsTest, bool) TestSingleEndpoint(DistributionEndpointsTest endpoint)
        {
            bool result = false;
            try
            {
                var distributionEndpoint = endpoint.NetworkAddress;
                HttpResponseMessage responseMessage = _client.GetAsync(distributionEndpoint).Result;
                var responseString = responseMessage.Content.ReadAsStringAsync().Result;

                switch (responseMessage.StatusCode)
                {
                    /*204, 205*/
                    case HttpStatusCode.NoContent:
                    case HttpStatusCode.ResetContent:
                        result = false;
                        break;

                    /*304*/
                    case HttpStatusCode.NotModified:
                        result = true;
                        //result = validate_content(string.Empty, string.Empty);
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
                                result = false;
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
            catch (System.Exception exception)
            {
                _logger.LogDebug(exception.Message);
                result = false;
            }
            return (endpoint, result);
        }


        private bool validate_content(string cmd, string args)
        {
            /*
            * Pass response to the python script using command line argument
            * There will be call to the ml-content-validator in future to validate the response received
            * For time being this function always return true
            */
            return true;
        }
    }
}
