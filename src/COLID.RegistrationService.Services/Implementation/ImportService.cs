using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using COLID.Graph.TripleStore.DataModels.Attributes;
using COLID.Graph.TripleStore.Extensions;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.Metadata.Services;
using COLID.RegistrationService.Common.DataModel.Resources;
using COLID.RegistrationService.Services.Interface;
using COLID.Common.Extensions;
using AutoMapper;
using COLID.RegistrationService.Services.Validation.Models;
using COLID.Graph.TripleStore.DataModels.Resources;
using COLID.RegistrationService.Services.Validation.Validators;
using System.Timers;
using System.Diagnostics;
using COLID.RegistrationService.Services.Validation;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.RegistrationService.Services.Validation.Exceptions;
using System.Threading;
using COLID.Graph.Metadata.Extensions;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Helper.SQS;
using System.Text.Json;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using COLID.Exception.Models;
using static COLID.Graph.Metadata.Constants.Resource;
using COLID.RegistrationService.Repositories.Interface;
using COLID.Graph.Metadata.Constants;

namespace COLID.RegistrationService.Services.Implementation
{
    /// <summary>
    /// Service to handle bulk import related operations.
    /// </summary>
    public class ImportService : IImportService
    {
        private readonly IMetadataService _metadataService;
        private readonly IResourcePreprocessService _resourcePreprocessService;
        private readonly IAWSSQSHelper _awsSQSHelper;
        private readonly ILogger<ImportService> _logger;
        private readonly IResourceService _resourceService;
        private readonly IResourceRepository _resourceRepository;
        private readonly IRevisionService _revisionService;
        private readonly IValidationService _validationService;
        private readonly IIdentifierService _identifierService;
        private readonly IReindexingService _indexingService;
        private readonly IRemoteAppDataService _remoteAppDataService;
        private readonly IProxyConfigService _proxyConfigService;

        public ImportService(
            IMetadataService metadataService,
            IMapper mapper,
            IAWSSQSHelper awsSQSHelper,
            IResourcePreprocessService resourcePreprocessService,
            IResourceService resourceService,
            ILogger<ImportService> logger,
            IResourceRepository resourceRepository,
            IRevisionService revisionService,
            IValidationService validationService,
            IIdentifierService identifierService,
            IReindexingService indexingService, 
            IRemoteAppDataService remoteAppDataService,
            IProxyConfigService proxyConfigService)
        {
            _metadataService = metadataService;
            _resourcePreprocessService = resourcePreprocessService;
            _awsSQSHelper = awsSQSHelper;
            _logger = logger;
            _resourceService = resourceService;
            _resourceRepository = resourceRepository;
            _revisionService = revisionService;
            _validationService = validationService;
            _identifierService = identifierService;
            _indexingService = indexingService;
            _remoteAppDataService = remoteAppDataService;
            _proxyConfigService = proxyConfigService;
        }

        /// <summary>
        /// Delete all messages from SQS-Queue
        /// </summary>        
        /// <returns></returns>
        public async Task<string> CleanUpBulkUploadSQSQueue()
        {
            try
            {
                var msgs = await _awsSQSHelper.ReceiveResourceMessageAsync();
                if (msgs.Count > 0)
                {
                    foreach (var msg in msgs)
                    {
                        // Delete msg from input Queue
                        await _awsSQSHelper.DeleteResourceMessageAsync(msg.ReceiptHandle);
                    }
                    return "Deleted " + msgs.Count.ToString() + " messages";
                }                
            }
            catch (System.Exception ex)
            {
                return ex.Message;
            }

            return "No Message to delete";
        }


        /// <summary>
        /// Start linking resources from SQS-Queue
        /// </summary>        
        /// <returns></returns>
        public async Task<string> StartLinkingResources()
        {
            _logger.LogInformation("ImportService: Starting to receive linking messages from SQS Queue.... ");

            Stopwatch stpWatch = new Stopwatch();
            stpWatch.Start();
            //Check for msgs in a loop           
            int msgcount, totalMsgCount = 0;
            
            do
            {
                //Check msgs available in SQS  
                var msgs = await _awsSQSHelper.ReceiveLinkingMessageAsync();
                msgcount = msgs.Count;
                totalMsgCount += msgs.Count;

                //Iterate on each msg which will contain list of resource
                foreach (var msg in msgs)
                {
                    //Get reources from the msg
                    List<ResourceLinkingInformation> resourceLinkingInfos = JsonConvert.DeserializeObject<List<ResourceLinkingInformation>>(msg.Body);

                    //List to collect ValidationFacade of each resource
                    List<ResourceLinkingResult> totalLinkingResult = new List<ResourceLinkingResult>();

                    var tasks = resourceLinkingInfos.Select(async linkInfo =>
                    //foreach (ResourceRequestDTO resource in resources)
                    {
                        ResourceLinkingResult curlinkingResult = new ResourceLinkingResult
                        {
                            PidUri = linkInfo.PidUri,
                            LinkType = linkInfo.LinkType,
                            PidUriToLink = linkInfo.PidUriToLink,
                            Requester = linkInfo.Requester                          
                        };

                        try
                        {
                            //Add link                            
                            await _resourceService.AddResourceLink(linkInfo.PidUri, linkInfo.LinkType, linkInfo.PidUriToLink, linkInfo.Requester);

                            //Collect result
                            curlinkingResult.Status = "Linked";
                            curlinkingResult.TimeTaken = stpWatch.ElapsedMilliseconds.ToString();
                            curlinkingResult.Message = "Linked Successfully";                            
                        }
                        catch (System.Exception ex)
                        {
                            //Collect result
                            curlinkingResult.Status = "Error";
                            curlinkingResult.TimeTaken = stpWatch.ElapsedMilliseconds.ToString();
                            curlinkingResult.Message = ex.Message;                            
                        }

                        totalLinkingResult.Add(curlinkingResult);
                    });
                    await Task.WhenAll(tasks);

                    //Send msg to output queue and delete message from input queue 
                    if (await _awsSQSHelper.SendLinkingMessageAsync(totalLinkingResult))
                    {
                        await _awsSQSHelper.DeleteLinkingMessageAsync(msg.ReceiptHandle);
                    }
                }

            } while (msgcount > 0);
  
            stpWatch.Stop();
            _logger.LogInformation("ImportService: Linked {totMsgCount} resources in {milsec} Milliseconds.", totalMsgCount, stpWatch.ElapsedMilliseconds);
            return "Linking Complete in " + stpWatch.ElapsedMilliseconds.ToString() + " Milliseconds";
        }

        private string CreateNewResourceId()
        {
            return Graph.Metadata.Constants.Entity.IdPrefix + Guid.NewGuid();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="resources"></param>
        /// <returns></returns>
        public async Task<List<BulkUploadResult>> ValidateResource(List<ResourceRequestDTO> resources)
        {
            _logger.LogInformation("ImportService: Received {count} resources. Validation Started...", resources.Count);
            Stopwatch stpWatch = new Stopwatch();
            stpWatch.Start();
            //List to collect ValidationFacade of each resource
            List<BulkUploadResult> totalValidationResult = new List<BulkUploadResult>();

            try
            {
                Uri resInstanceGraph = null;
                Uri draftInstanceGraph = null;

                if (resInstanceGraph == null)
                    resInstanceGraph = _metadataService.GetInstanceGraph(PIDO.PidConcept);

                if (draftInstanceGraph == null)
                    draftInstanceGraph = _metadataService.GetInstanceGraph("draft");
               
                foreach (ResourceRequestDTO resource in resources)
                {
                    
                    
                    //Extract pidUri
                    var hasPid = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.hasPID, true);
                    Uri pidUri = null;
                    try
                    {
                        if (hasPid != null && ((COLID.Graph.TripleStore.DataModels.Base.Entity)hasPid).Id != string.Empty)
                            pidUri = new Uri(((COLID.Graph.TripleStore.DataModels.Base.Entity)hasPid).Id);
                    }
                    catch
                    {
                        pidUri = null;
                    }
                    //Check SourceId
                    string srcId = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasSourceID, true);
                    if (srcId == null)
                    {
                        //Collect result
                        totalValidationResult.Add(new BulkUploadResult
                        {
                            ActionDone = "Error",
                            ErrorMessage = "SourceId not found.",
                            TimeTaken = stpWatch.ElapsedMilliseconds.ToString(),
                            pidUri = pidUri == null ? "" : pidUri.ToString(),
                            ResourceLabel = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasLabel, true),
                            ResourceDefinition = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasResourceDefintion, true),
                            StateItems = resource.StateItems
                        });
                        //// Delete msg from input Queue
                        //if (DeleteMessageFromSQS(msg.ReceiptHandle).Result == false)
                        //{
                        //    _logger.LogInformation("BackgroundService: Could not delete meessage");
                        //}
                        continue;
                    }

                    //Check Entity Type
                    try
                    {
                        _validationService.CheckInstantiableEntityType(resource);
                    }
                    catch (System.Exception ex)
                    {
                        //Collect result
                        totalValidationResult.Add(new BulkUploadResult
                        {
                            ActionDone = "Error",
                            ErrorMessage = ex.Message,
                            TimeTaken = stpWatch.ElapsedMilliseconds.ToString(),
                            pidUri = pidUri == null ? "" : pidUri.ToString(),
                            SourceId = srcId,
                            ResourceLabel = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasLabel, true),
                            ResourceDefinition = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasResourceDefintion, true),
                            StateItems = resource.StateItems
                        });
                        //// Delete msg from input Queue
                        //if (DeleteMessageFromSQS(msg.ReceiptHandle).Result == false)
                        //{
                        //    _logger.LogInformation("BackgroundService: Could not delete meessage for sourceId {sourceId} ", srcId);
                        //}
                        continue;
                    }

                    ResourcesCTO resourcesCTO = new ResourcesCTO();
                    bool pidUriExistsinTripleStore = false;

                    if (pidUri == null)
                    {
                        //Check Whether resource is already present in Neptune (using SourceId)
                        ISet<Uri> resourceInstanceGraphs = new HashSet<Uri>();
                        resourceInstanceGraphs.Add(resInstanceGraph);
                        resourceInstanceGraphs.Add(draftInstanceGraph);
                        pidUri = _resourceRepository.GetPidUriBySourceId(srcId, resourceInstanceGraphs);
                    }
                    else
                    {
                        try
                        {
                            resourcesCTO = _resourceService.GetResourcesByPidUri(pidUri);
                            pidUriExistsinTripleStore = true;
                        }
                        catch
                        {
                            pidUriExistsinTripleStore = false;
                        }

                    }

                    //if Pid Uri is null then Add
                    if (pidUri == null || (pidUri != null && pidUriExistsinTripleStore == false))
                    {
                        try
                        {
                            string newResourceId =  CreateNewResourceId();
                            //_logger.LogInformation("BackgroundService: About to Validate New resource: {msg}", msg.Body);
                            //Validate
                            var (validationResult, failed, validationFacade) =
                                await _resourcePreprocessService.ValidateAndPreProcessResource(newResourceId, resource, new ResourcesCTO(), ResourceCrudAction.Create);
                            _logger.LogInformation("BackgroundService: Validation Complete for: {srcId} having status {stat}", srcId, failed.ToString());

                            //Update pidUri in stateItem
                            if (failed == false && resource.StateItems[0].ContainsKey("pid_uri"))
                            {
                                resource.StateItems[0]["pid_uri"] = validationFacade.RequestResource.PidUri.ToString();
                            }

                            //Create result data
                            BulkUploadResult result = new BulkUploadResult
                            {
                                ActionDone = failed ? "Error" : "Validated",
                                ErrorMessage = failed ? "Validation Failed while Adding the resource." : string.Empty,
                                Results = validationResult.Results,
                                Triples = validationResult.Triples.Replace(ColidEntryLifecycleStatus.Draft, ColidEntryLifecycleStatus.Published),
                                InstanceGraph = resInstanceGraph.ToString(),
                                TimeTaken = stpWatch.ElapsedMilliseconds.ToString(),
                                pidUri = validationFacade.RequestResource.PidUri.ToString(),
                                ResourceId = newResourceId,
                                SourceId = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasSourceID, true),
                                ResourceLabel = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasLabel, true),
                                ResourceDefinition = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasResourceDefintion, true),
                                StateItems = resource.StateItems
                            };

                            //Get distribution
                            if (resource.Properties.ContainsKey(Graph.Metadata.Constants.Resource.Distribution))
                            {
                                List<dynamic> distList = resource.Properties[Graph.Metadata.Constants.Resource.Distribution];
                                foreach (dynamic dist in distList)
                                {
                                    string EndPointPidUri = ((COLID.Graph.TripleStore.DataModels.Base.EntityBase)dist).Properties[Graph.Metadata.Constants.Resource.hasPID][0].Id;
                                    string EndPointUrl = ((COLID.Graph.TripleStore.DataModels.Base.EntityBase)dist).Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.DistributionEndpoints.HasNetworkAddress, true);
                                    result.DistributionEndPoint.Add(EndPointPidUri, EndPointUrl);
                                }

                            }

                            //Collect Result
                            totalValidationResult.Add(result);

                            //if validation passed then Update Nginx Proxy info for the resource DynamoDB
                            if (!failed)
                                _proxyConfigService.AddUpdateNginxConfigRepository(resource);
                        }
                        catch (System.Exception ex)
                        {
                            //Collect result
                            totalValidationResult.Add(new BulkUploadResult
                            {
                                ActionDone = "Error",
                                ErrorMessage = ex.Message,
                                TimeTaken = stpWatch.ElapsedMilliseconds.ToString(),
                                pidUri = pidUri == null ? "" : pidUri.ToString(),
                                SourceId = srcId,
                                ResourceLabel = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasLabel, true),
                                ResourceDefinition = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasResourceDefintion, true)
                            });

                            //// Delete msg from input Queue
                            //if (DeleteMessageFromSQS(msg.ReceiptHandle).Result == false)
                            //{
                            //    _logger.LogInformation("BackgroundService: Could not delete meessage for sourceId {sourceId} ", srcId);
                            //}
                            continue;
                        }

                    }
                    else //Update Resource
                    {
                        try
                        {
                            //var resourcesCTO = _resourceService.GetResourcesByPidUri(pidUri);   // Draft und Published resource getrennt behandeln.
                            var id = resourcesCTO.GetDraftOrPublishedVersion().Id; // Draft und Published resource getrennt behandeln.

                            //Update resource with PidUri
                            if (resource.Properties.ContainsKey(Graph.Metadata.Constants.Resource.hasPID))
                            {
                                ((COLID.Graph.TripleStore.DataModels.Base.Entity)resource.Properties[Graph.Metadata.Constants.Resource.hasPID][0]).Id = pidUri.ToString();
                            }

                            //_logger.LogInformation("BackgroundService: About to Validate Existing resource: {msg}", msg.Body);
                            var (validationResult, failed, validationFacade) =
                                await _resourcePreprocessService.ValidateAndPreProcessResource(id, resource, resourcesCTO, ResourceCrudAction.Publish, false, null);
                            _logger.LogInformation("BackgroundService: Validation Complete for: {srcId} having status {stat}", srcId, failed.ToString());

                            // The validation failed, if the results are cricital errors.
                            if (failed)
                            {
                                //Collect result
                                totalValidationResult.Add(new BulkUploadResult
                                {
                                    ActionDone = "Error",
                                    ErrorMessage = "Validation Failed while updating the resource.",
                                    Results = validationResult.Results,
                                    TimeTaken = stpWatch.ElapsedMilliseconds.ToString(),
                                    pidUri = validationFacade.RequestResource.PidUri.ToString(),
                                    ResourceId = id,
                                    SourceId = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasSourceID, true),
                                    ResourceLabel = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasLabel, true),
                                    ResourceDefinition = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasResourceDefintion, true),
                                    StateItems = resource.StateItems
                                });
                                //// Delete msg from input Queue
                                //if (DeleteMessageFromSQS(msg.ReceiptHandle).Result == false)
                                //{
                                //    _logger.LogInformation("BackgroundService: Could not delete meessage for sourceId {sourceId} ", srcId);
                                //}
                                continue;
                            }

                            if (resourcesCTO.HasPublished && (!_resourceService.ResourceHasChanged(resourcesCTO.Published, validationFacade.RequestResource)))
                            {
                                //Collect result
                                totalValidationResult.Add(new BulkUploadResult
                                {
                                    ActionDone = "Error",
                                    ErrorMessage = "No changes found in this resource.",
                                    TimeTaken = stpWatch.ElapsedMilliseconds.ToString(),
                                    pidUri = validationFacade.RequestResource.PidUri.ToString(),
                                    ResourceId = id,
                                    SourceId = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasSourceID, true),
                                    ResourceLabel = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasLabel, true),
                                    ResourceDefinition = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasResourceDefintion, true),
                                    StateItems = resource.StateItems
                                });
                                //// Delete msg from input Queue
                                //if (DeleteMessageFromSQS(msg.ReceiptHandle).Result == false)
                                //{
                                //    _logger.LogInformation("BackgroundService: Could not delete meessage for sourceId {sourceId} ", srcId);
                                //}
                                continue;
                            }

                            var resourcetoCreate = _resourceService.SetHasLaterVersionResourceId(validationFacade.RequestResource);
                            using (var transaction = _resourceRepository.CreateTransaction())
                            {                                
                                // try deleting draft version and all inbound edges are changed to the new entry.
                                _resourceRepository.DeleteDraft(validationFacade.RequestResource.PidUri, new Uri(validationFacade.RequestResource.Id), draftInstanceGraph);
                               
                                if (resourcesCTO.HasDraft)
                                    _identifierService.DeleteAllUnpublishedIdentifiers(resourcesCTO.Draft);

                                if (resourcesCTO.HasPublished)
                                    // Try to delete published and all inbound edges are changed to the new entry.
                                    _resourceRepository.DeletePublished(validationFacade.RequestResource.PidUri,
                                    new Uri(validationFacade.RequestResource.Id), resInstanceGraph);

                                //Add existing revision to the new resource
                                if (resourcesCTO.Published != null)
                                {
                                    var existingRevisions = resourcesCTO.Published.Properties.TryGetValue(COLID.Graph.Metadata.Constants.Resource.HasRevision, out List<dynamic> revisionValues) ? revisionValues : null;
                                    if (existingRevisions != null)
                                        resourcetoCreate.Properties.Add(COLID.Graph.Metadata.Constants.Resource.HasRevision, existingRevisions);
                                }
                                
                                //Add Published
                                _resourceRepository.Create(resourcetoCreate, validationFacade.MetadataProperties, resInstanceGraph);

                                //Update revision
                                if(resourcesCTO.Published == null)
                                {
                                    await _revisionService.InitializeResourceInAdditionalsGraph(resourcetoCreate, validationFacade.MetadataProperties);
                                }
                                else
                                {
                                    Graph.Metadata.DataModels.Resources.Resource updatedResource = await _revisionService.AddAdditionalsAndRemovals(resourcesCTO.Published, validationFacade.RequestResource);
                                }
                                

                                _logger.LogInformation("BackgroundService: {sparqlQuery}", transaction.GetSparqlString());
                                
                                //transaction.Commit();
                                //Index resource
                                //_indexingService.IndexPublishedResource(pidUri, updatedResource, validationFacade.ResourcesCTO);
                            }
                            _logger.LogInformation("BackgroundService: Resource updated having sourceId {sourceId} ", srcId);
                            //Create result data
                            BulkUploadResult result = new BulkUploadResult
                            {
                                ActionDone = "Updated",
                                TimeTaken = stpWatch.ElapsedMilliseconds.ToString(),
                                pidUri = pidUri == null ? "" : pidUri.ToString(),
                                ResourceId = id,
                                SourceId = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasSourceID, true),
                                ResourceLabel = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasLabel, true),
                                ResourceDefinition = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasResourceDefintion, true),
                                StateItems = resource.StateItems
                            };

                            //Get distribution
                            if (resource.Properties.ContainsKey(Graph.Metadata.Constants.Resource.Distribution))
                            {
                                List<dynamic> distList = resource.Properties[Graph.Metadata.Constants.Resource.Distribution];
                                foreach (dynamic dist in distList)
                                {
                                    string EndPointPidUri = ((COLID.Graph.TripleStore.DataModels.Base.EntityBase)dist).Properties[Graph.Metadata.Constants.Resource.hasPID][0].Id;
                                    string EndPointUrl = ((COLID.Graph.TripleStore.DataModels.Base.EntityBase)dist).Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.DistributionEndpoints.HasNetworkAddress, true);
                                    result.DistributionEndPoint.Add(EndPointPidUri, EndPointUrl);
                                }
                            }

                            //Collect Result
                            totalValidationResult.Add(result);

                            //Update Nginx Proxy info for the resource DynamoDB
                            _proxyConfigService.AddUpdateNginxConfigRepository(resource);
                        }
                        catch (System.Exception ex)
                        {
                            _logger.LogInformation("BackgroundService:  Error while updating - {msg} ", ex.Message);
                            //Collect result
                            totalValidationResult.Add(new BulkUploadResult
                            {
                                ActionDone = "Error",
                                ErrorMessage = ex.Message,
                                TimeTaken = stpWatch.ElapsedMilliseconds.ToString(),
                                pidUri = pidUri == null ? "" : pidUri.ToString(),
                                SourceId = srcId,
                                ResourceLabel = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasLabel, true),
                                ResourceDefinition = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasResourceDefintion, true),
                                StateItems = resource.StateItems
                            });

                            //// Delete msg from input Queue
                            //if (DeleteMessageFromSQS(msg.ReceiptHandle).Result == false)
                            //{
                            //    _logger.LogInformation("BackgroundService: Could not delete meessage for sourceId {sourceId} ", srcId);
                            //}
                            continue;
                        }

                    }

                    //// Delete msg from input Queue
                    //if (DeleteMessageFromSQS(msg.ReceiptHandle).Result == false)
                    //{
                    //    _logger.LogInformation("BackgroundService: Could not delete meessage for sourceId {sourceId} ", srcId);
                    //}
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogInformation(ex.Message);
                throw new TechnicalException(ex.Message);
            }

            stpWatch.Stop();
            _logger.LogInformation("ImportService: Validated {totMsgCount} messages in {milsec} Milliseconds.", resources.Count, stpWatch.ElapsedMilliseconds);
            

            return totalValidationResult;
        }

        private ResourceProxyDTO ConvertResourceToProxyDto(ResourceRequestDTO resource)
        {
            var hasPid = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.hasPID, true);
            string pidUri = hasPid != null ? hasPid.Id : "";

            var hasBaseUri = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.BaseUri, true);
            string baseUri = hasBaseUri != null ? hasBaseUri.Id : null;

            ResourceProxyDTO resourceProxyDto = new ResourceProxyDTO
            {
                PidUrl = pidUri,
                TargetUrl = null,
                ResourceVersion = null,
                NestedProxies = new List<ResourceProxyDTO>()
            };

            // Get distribution
            if (resource.Properties.ContainsKey(Graph.Metadata.Constants.Resource.Distribution))
            {
                List<dynamic> distList = resource.Properties[Graph.Metadata.Constants.Resource.Distribution];
                foreach (dynamic dist in distList)
                {
                    string distributionPidUri = ((COLID.Graph.TripleStore.DataModels.Base.EntityBase)dist).Properties[Graph.Metadata.Constants.Resource.hasPID][0].Id;
                    string distributionNetworkAddress = ((COLID.Graph.TripleStore.DataModels.Base.EntityBase)dist).Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.DistributionEndpoints.HasNetworkAddress, true);
                    bool isDistributionEndpointDeprecated = ((COLID.Graph.TripleStore.DataModels.Base.EntityBase)dist).Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.DistributionEndpoints.DistributionEndpointLifecycleStatus, true) == Common.Constants.DistributionEndpoint.LifeCycleStatus.Deprecated;
                    //string distBaseUri = ((COLID.Graph.TripleStore.DataModels.Base.EntityBase)dist).Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.DistributionEndpoints.HasNetworkAddress, true);
                    

                    resourceProxyDto.NestedProxies.Add(
                        new ResourceProxyDTO
                        {
                            PidUrl = distributionPidUri,
                            TargetUrl = isDistributionEndpointDeprecated ? pidUri : distributionNetworkAddress,
                            ResourceVersion = null,
                            BaseUrl = baseUri
                        });
                }
            }

            //Get Main distribution
            string baseUriDistTargetUrl = "";
            if (resource.Properties.ContainsKey(Graph.Metadata.Constants.Resource.MainDistribution))
            {
                List<dynamic> distList = resource.Properties[Graph.Metadata.Constants.Resource.MainDistribution];
                foreach (dynamic dist in distList)
                {
                    string mainDistributionPidUri = ((COLID.Graph.TripleStore.DataModels.Base.EntityBase)dist).Properties[Graph.Metadata.Constants.Resource.hasPID][0].Id;
                    string mainDistributionNetworkAddress = ((COLID.Graph.TripleStore.DataModels.Base.EntityBase)dist).Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.DistributionEndpoints.HasNetworkAddress, true);
                    bool isDistributionEndpointDeprecated = ((COLID.Graph.TripleStore.DataModels.Base.EntityBase)dist).Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.DistributionEndpoints.DistributionEndpointLifecycleStatus, true) == Common.Constants.DistributionEndpoint.LifeCycleStatus.Deprecated;
                    //string distBaseUri = ((COLID.Graph.TripleStore.DataModels.Base.EntityBase)dist).Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.DistributionEndpoints.HasNetworkAddress, true);
                    baseUriDistTargetUrl = isDistributionEndpointDeprecated ? pidUri : mainDistributionNetworkAddress;
                    
                    resourceProxyDto.NestedProxies.Add(
                        new ResourceProxyDTO
                        {
                            PidUrl = mainDistributionPidUri,
                            TargetUrl = baseUriDistTargetUrl,
                            ResourceVersion = null,
                            BaseUrl = baseUri
                        });
                }
            }

            // Proxy for base URI                        
            if (!string.IsNullOrWhiteSpace(baseUri))
            {
                string resourceVersion = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasVersion, true);

                // distribution target uri is null -> base uri have to redirect to resourcePidUri
                if (!string.IsNullOrWhiteSpace(baseUriDistTargetUrl))
                {
                    if (Uri.TryCreate(baseUriDistTargetUrl, UriKind.Absolute, out _))
                    {
                        resourceProxyDto.NestedProxies.Add(new ResourceProxyDTO { PidUrl = baseUri, TargetUrl = string.IsNullOrWhiteSpace(baseUriDistTargetUrl) ? pidUri : baseUriDistTargetUrl, ResourceVersion = resourceVersion, BaseUrl = baseUri });
                    }
                    else
                    {
                        resourceProxyDto.NestedProxies.Add(new ResourceProxyDTO { PidUrl = baseUri, TargetUrl = pidUri, ResourceVersion = resourceVersion, BaseUrl = baseUri });                        
                    }
                }
                else
                {
                    resourceProxyDto.NestedProxies.Add(new ResourceProxyDTO { PidUrl = baseUri, TargetUrl = pidUri, ResourceVersion = resourceVersion });
                }
            }
            
            return resourceProxyDto;
        }
    }
}
