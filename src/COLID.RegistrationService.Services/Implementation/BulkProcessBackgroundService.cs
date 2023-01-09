using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using COLID.AWS.Interface;
using COLID.Exception.Models;
using COLID.Graph.Metadata.Constants;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.Metadata.Services;
using COLID.Graph.TripleStore.DataModels.Resources;
using COLID.Graph.TripleStore.Extensions;
using COLID.Helper.SQS;
using COLID.RegistrationService.Repositories.Interface;
using COLID.RegistrationService.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static COLID.Graph.Metadata.Constants.Resource;

namespace COLID.RegistrationService.Services.Implementation
{
    public class BulkProcessBackgroundService : BackgroundService
    {
        private readonly ILogger<BulkProcessBackgroundService> _logger;
        private readonly IAWSSQSHelper _awsSQSHelper;
        private readonly IResourcePreprocessService _resourcePreprocessService;
        private readonly IResourceService _resourceService;
        private readonly IResourceRepository _resourceRepository;
        private readonly IRevisionService _revisionService;
        private readonly IMetadataService _metadataService;
        private readonly IValidationService _validationService;
        private readonly IIdentifierService _identifierService;
        private readonly IReindexingService _indexingService;
        private readonly IProxyConfigService _proxyConfigService;        
        private readonly IImportService _importService;
        private readonly IRemoteAppDataService _remoteAppDataService;

        /// <summary>
        /// Constructer to initialize
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="awsSQSHelper"></param>
        /// <param name="resourcePreprocessService"></param>
        /// <param name="resourceService"></param>
        /// <param name="resourceRepository"></param>
        public BulkProcessBackgroundService(ILogger<BulkProcessBackgroundService> logger, IAWSSQSHelper awsSQSHelper,
            IResourcePreprocessService resourcePreprocessService, IResourceService resourceService,
            IResourceRepository resourceRepository, IRevisionService revisionService, IMetadataService metadataService,
            IValidationService validationService, IIdentifierService identifierService, IReindexingService indexingService,
            IProxyConfigService proxyConfigService, IImportService importService, IRemoteAppDataService remoteAppDataService)
        {
            _logger = logger;
            _awsSQSHelper = awsSQSHelper;
            _resourcePreprocessService = resourcePreprocessService;
            _resourceService = resourceService;
            _resourceRepository = resourceRepository;
            _revisionService = revisionService;
            _metadataService = metadataService;
            _validationService = validationService;
            _identifierService = identifierService;
            _indexingService = indexingService;
            _proxyConfigService = proxyConfigService;            
            _importService = importService;
            _remoteAppDataService = remoteAppDataService;
        }

        /// <summary>
        /// Background service invoked automatically at startup, for bulk validation and linking resources
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BackgroundService: Started ");
            //await Task.Yield();
            while (!stoppingToken.IsCancellationRequested)
            {
                await AddUpdateResources();
                await LinkResources();
                await ImportExcel();
                await Task.Delay(10000, stoppingToken);
            }
        }

        /// <summary>
        /// Createa new resource ID
        /// </summary>
        /// <returns></returns>
        private string CreateNewResourceId()
        {
            return Graph.Metadata.Constants.Entity.IdPrefix + Guid.NewGuid();
        }

        /// <summary>
        /// Read resources from SQS que and Validate 
        /// </summary>
        /// <returns></returns>
        private async Task AddUpdateResources()
        {
            //_logger.LogInformation("BackgroundService: Running.... ");
            Stopwatch stpWatch = new Stopwatch();
            stpWatch.Start();
            //Check for msgs in a loop           
            int msgcount, totalMsgCount = 0;
            try
            {
                do
                {
                    //Check msgs available in SQS  
                    var msgs = await _awsSQSHelper.ReceiveResourceMessageAsync();
                    msgcount = msgs.Count;
                    totalMsgCount += msgs.Count;

                    //get Instance graphs if there is msg to process

                    Uri resInstanceGraph = null;
                    Uri draftInstanceGraph = null;
                    if (msgs.Count > 0)
                    {
                        _logger.LogInformation("BackgroundService: Found {count} messages from resource queue ", msgs.Count);
                        if (resInstanceGraph == null)
                            resInstanceGraph = _metadataService.GetInstanceGraph(PIDO.PidConcept);

                        if (draftInstanceGraph == null)
                            draftInstanceGraph = _metadataService.GetInstanceGraph("draft");

                        //List to collect ValidationFacade of each resource
                        List<BulkUploadResult> totalValidationResult = new List<BulkUploadResult>();

                        //Iterate on each msg which will contain list of resource
                        foreach (var msg in msgs)
                        {
                            ResourceRequestDTO resource;
                            // Try Get reources from the msg
                            try
                            {
                                resource = JsonConvert.DeserializeObject<ResourceRequestDTO>(msg.Body);
                            }
                            catch (System.Exception ex)
                            {
                                //Collect result
                                totalValidationResult.Add(new BulkUploadResult
                                {
                                    ActionDone = "Error",
                                    ErrorMessage = "Unable to Deserialize the resource",
                                    TimeTaken = stpWatch.ElapsedMilliseconds.ToString(),
                                    pidUri = "",
                                    ResourceLabel = ex.Message,
                                    ResourceDefinition = ""
                                });
                                // Delete msg from input Queue
                                if (DeleteMessageFromSQS(msg.ReceiptHandle).Result == false)
                                {
                                    _logger.LogInformation("BackgroundService: Could not delete meessage");
                                }
                                continue;
                            }

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
                                // Delete msg from input Queue
                                if (DeleteMessageFromSQS(msg.ReceiptHandle).Result == false)
                                {
                                    _logger.LogInformation("BackgroundService: Could not delete meessage");
                                }
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
                                // Delete msg from input Queue
                                if (DeleteMessageFromSQS(msg.ReceiptHandle).Result == false)
                                {
                                    _logger.LogInformation("BackgroundService: Could not delete meessage for sourceId {sourceId} ", srcId);
                                }
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

                            if (pidUri != null)
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
                                    string newResourceId = CreateNewResourceId();
                                    _logger.LogInformation("BackgroundService: About to Validate New resource: {msg}", msg.Body);
                                    //Validate
                                    var (validationResult, failed, validationFacade) =
                                        await _resourcePreprocessService.ValidateAndPreProcessResource(newResourceId, resource, new ResourcesCTO(), ResourceCrudAction.Create, false, null, false, true);
                                    _logger.LogInformation("BackgroundService: Validation Complete for: {srcId} having status {stat}", srcId, failed.ToString());

                                    //Update pidUri in stateItem                            
                                    UpdateStateItemsWithPidUri(resource, validationFacade.RequestResource.PidUri.ToString());

                                    //Create result data
                                    BulkUploadResult result = new BulkUploadResult
                                    {
                                        ActionDone = failed ? "Error" : "Validated",
                                        ErrorMessage = failed ? "Validation Failed while Adding the resource." : string.Empty,
                                        Results = validationResult.Results,
                                        Triples = validationResult.Triples.Replace(ColidEntryLifecycleStatus.Draft, ColidEntryLifecycleStatus.Published),
                                        InstanceGraph = resInstanceGraph.ToString(),
                                        TimeTaken = stpWatch.ElapsedMilliseconds.ToString(),
                                        pidUri = validationFacade.RequestResource.PidUri == null ? "" : validationFacade.RequestResource.PidUri.ToString(),
                                        ResourceId = newResourceId,
                                        SourceId = srcId,
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

                                    // Delete msg from input Queue
                                    if (DeleteMessageFromSQS(msg.ReceiptHandle).Result == false)
                                    {
                                        _logger.LogInformation("BackgroundService: Could not delete meessage for sourceId {sourceId} ", srcId);
                                    }
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

                                    _logger.LogInformation("BackgroundService: About to Validate Existing resource: {msg}", msg.Body);
                                    var (validationResult, failed, validationFacade) =
                                        await _resourcePreprocessService.ValidateAndPreProcessResource(id, resource, resourcesCTO, ResourceCrudAction.Publish, false, null, false, true);
                                    _logger.LogInformation("BackgroundService: Validation Complete for: {srcId} having status {stat}", srcId, failed.ToString());

                                    //Update pidUri in stateItem                            
                                    UpdateStateItemsWithPidUri(resource, validationFacade.RequestResource.PidUri.ToString());

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
                                            SourceId = srcId,
                                            ResourceLabel = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasLabel, true),
                                            ResourceDefinition = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasResourceDefintion, true),
                                            StateItems = resource.StateItems
                                        });
                                        // Delete msg from input Queue
                                        if (DeleteMessageFromSQS(msg.ReceiptHandle).Result == false)
                                        {
                                            _logger.LogInformation("BackgroundService: Could not delete meessage for sourceId {sourceId} ", srcId);
                                        }
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
                                            SourceId = srcId,
                                            ResourceLabel = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasLabel, true),
                                            ResourceDefinition = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasResourceDefintion, true),
                                            StateItems = resource.StateItems
                                        });
                                        // Delete msg from input Queue
                                        if (DeleteMessageFromSQS(msg.ReceiptHandle).Result == false)
                                        {
                                            _logger.LogInformation("BackgroundService: Could not delete meessage for sourceId {sourceId} ", srcId);
                                        }
                                        continue;
                                    }

                                    var resourcetoCreate = _resourceService.SetHasLaterVersionResourceId(validationFacade.RequestResource);
                                    using (var transaction = _resourceRepository.CreateTransaction())
                                    {
                                        // try deleting draft version and all inbound edges are changed to the new entry.
                                        _resourceRepository.DeleteDraft(validationFacade.RequestResource.PidUri,
                                        new Uri(validationFacade.RequestResource.Id), draftInstanceGraph);

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
                                            
                                            // Check if Link already exists
                                            IList<MetadataProperty> metadataEntityType = _metadataService.GetMetadataForEntityType(resource.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.RDF.Type, true));
                                            var linkMetadata = metadataEntityType.Where(x => (x.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.Shacl.Group, true)).GetValue("key") == COLID.Graph.Metadata.Constants.Resource.Groups.LinkTypes).ToList().Select(y => y.Key).ToHashSet();

                                            //Add previous links to Properties.
                                            var outboundlinks = _resourceRepository.GetOutboundLinksOfPublishedResource(validationFacade.RequestResource.PidUri, resInstanceGraph, linkMetadata);
                                            resourcetoCreate.Links = outboundlinks;

                                            foreach (var lnk in outboundlinks)
                                            {
                                                var temp = new List<dynamic>();
                                                temp.AddRange(lnk.Value.Select(s => s.PidUri).ToList());
                                                resourcetoCreate.Properties.Add(lnk.Key, temp);
                                            }
                                        }

                                        //Add Published
                                        _resourceRepository.Create(resourcetoCreate, validationFacade.MetadataProperties, resInstanceGraph);

                                        //Update revision
                                        if (resourcesCTO.Published == null)
                                        {
                                            await _revisionService.InitializeResourceInAdditionalsGraph(resourcetoCreate, validationFacade.MetadataProperties);
                                        }
                                        else
                                        {
                                            Graph.Metadata.DataModels.Resources.Resource updatedResource = await _revisionService.AddAdditionalsAndRemovals(resourcesCTO.Published, validationFacade.RequestResource);
                                        }

                                        _logger.LogInformation("BackgroundService: Commit - {sparqlQuery}", transaction.GetSparqlString());

                                        transaction.Commit();
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
                                        SourceId = srcId,
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

                                    _logger.LogInformation("BackgroundService: Sending update notification for sourceId {sourceId} ", srcId);
                                    await _remoteAppDataService.NotifyResourcePublished(validationFacade.RequestResource);
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

                                    // Delete msg from input Queue
                                    if (DeleteMessageFromSQS(msg.ReceiptHandle).Result == false)
                                    {
                                        _logger.LogInformation("BackgroundService: Could not delete meessage for sourceId {sourceId} ", srcId);
                                    }
                                    continue;
                                }

                            }

                            // Delete msg from input Queue
                            if (DeleteMessageFromSQS(msg.ReceiptHandle).Result == false)
                            {                                
                                _logger.LogInformation("BackgroundService: Could not delete meessage for sourceId {sourceId} ", srcId);
                            }                            
                        }

                        //Send msg to output queue  
                        try
                        {                            
                            await _awsSQSHelper.SendResourceMessageAsync(totalValidationResult);                            
                        }
                        catch(System.Exception ex)
                        {
                            _logger.LogInformation("BackgroundService: Could not send meessage to output queue {msg} ", ex.Message);
                        }
                        
                    }

                } while (msgcount > 0);
            }
            catch (System.Exception ex)
            {
                _logger.LogError("BackgroundService: " + (ex.InnerException == null ? ex.Message : ex.InnerException.Message));
            }

            stpWatch.Stop();
            if (totalMsgCount > 0)
            {
                //_logger.LogInformation("BackgroundService: Processed {totMsgCount} messages in {milsec} Milliseconds.", totalMsgCount, stpWatch.ElapsedMilliseconds);
            }

        }

        private async Task LinkResources()
        {
            //_logger.LogInformation("BackgroundService: Checking linking messages from SQS Queue.... ");

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
            if (totalMsgCount > 0)
            {
                //_logger.LogInformation("BackgroundService: Linked {totMsgCount} resources in {milsec} Milliseconds.", totalMsgCount, stpWatch.ElapsedMilliseconds);
            }
        }

        private async Task<bool> DeleteMessageFromSQS(string msgReceiptHandle)
        {
            if (msgReceiptHandle == null || msgReceiptHandle == string.Empty)
                return false;
            try
            {
                // Delete msg from input Queue
                await _awsSQSHelper.DeleteResourceMessageAsync(msgReceiptHandle);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void UpdateStateItemsWithPidUri(ResourceRequestDTO resource, string assetPidUri)
        {
            foreach (var stateItem in resource.StateItems)
            {
                if (stateItem["entry_type"] == "asset")
                {
                    stateItem["pid_uri"] = assetPidUri;
                }
                else
                {
                    //Get PidUri from Distribution List
                    if (resource.Properties.ContainsKey(Graph.Metadata.Constants.Resource.Distribution))
                    {
                        List<dynamic> distList = resource.Properties[Graph.Metadata.Constants.Resource.Distribution];
                        foreach (dynamic dist in distList)
                        {
                            string EndPointPidUri = ((COLID.Graph.TripleStore.DataModels.Base.EntityBase)dist).Properties[Graph.Metadata.Constants.Resource.hasPID][0].Id;
                            string EndPointUrl = ((COLID.Graph.TripleStore.DataModels.Base.EntityBase)dist).Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.DistributionEndpoints.HasNetworkAddress, true);

                            if (stateItem["key_value"] == EndPointUrl)
                            {
                                stateItem["pid_uri"] = EndPointPidUri;
                                break;
                            }
                        }

                    }

                }
            }
        }

        private async Task ImportExcel()
        {
            try
            {
                await _importService.ImportExcel();
            }
            catch(System.Exception ex)
            {
                _logger.LogError("BackgroundService: " + (ex.InnerException == null ? ex.Message : ex.InnerException.Message));
            }
            
        }
    }
}
