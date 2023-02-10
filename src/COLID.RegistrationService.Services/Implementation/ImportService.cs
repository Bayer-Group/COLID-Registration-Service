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
using Microsoft.AspNetCore.Http;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.IO;
using System.Dynamic;
using COLID.AWS.Interface;
using COLID.RegistrationService.Common.DataModels.TransferObjects;
using COLID.AWS.DataModels;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using COLID.Identity.Services;
using COLID.RegistrationService.Services.Configuration;
using Microsoft.Extensions.Options;
using COLID.RegistrationService.Services.Authorization.UserInfo;

namespace COLID.RegistrationService.Services.Implementation
{
    /// <summary>
    /// Service to handle bulk import related operations.
    /// </summary>
    public class ImportService : IImportService
    {
        private readonly IMetadataService _metadataService;
        private readonly IResourcePreprocessService _resourcePreprocessService;
        private readonly ILogger<ImportService> _logger;
        private readonly IResourceService _resourceService;
        private readonly IResourceRepository _resourceRepository;
        private readonly IRevisionService _revisionService;
        private readonly IValidationService _validationService;
        private readonly IIdentifierService _identifierService;
        private readonly IReindexingService _indexingService;
        private readonly IRemoteAppDataService _remoteAppDataService;
        private readonly IProxyConfigService _proxyConfigService;
        private readonly IAmazonS3Service _awsS3Service;
        private readonly IConfiguration _configuration;
        private readonly ITokenService<ColidAppDataServiceTokenOptions> _adsTokenService;
        private readonly string _appDataServiceEndpoint;
        private readonly AmazonWebServicesOptions _awsConfig;
        private readonly IUserInfoService _userInfoService;

        private readonly Func<string, string> _messageBody =
            (url) => string.Format("The import excel file status is available at <a href=\"{0}\">{0}</a href>", url);
        private readonly string _s3AccessLinkPrefix;

        public ImportService(
            IMetadataService metadataService,
            IMapper mapper,
            IResourcePreprocessService resourcePreprocessService,
            IResourceService resourceService,
            ILogger<ImportService> logger,
            IResourceRepository resourceRepository,
            IRevisionService revisionService,
            IValidationService validationService,
            IIdentifierService identifierService,
            IReindexingService indexingService,
            IRemoteAppDataService remoteAppDataService,
            IProxyConfigService proxyConfigService,
            IAmazonS3Service awsS3Service,
            IConfiguration configuration,
            ITokenService<ColidAppDataServiceTokenOptions> adsTokenService,
            IOptionsMonitor<AmazonWebServicesOptions> awsOptionsMonitor,
            IUserInfoService userInfoService)
        {
            _metadataService = metadataService;
            _resourcePreprocessService = resourcePreprocessService;
            _logger = logger;
            _resourceService = resourceService;
            _resourceRepository = resourceRepository;
            _revisionService = revisionService;
            _validationService = validationService;
            _identifierService = identifierService;
            _indexingService = indexingService;
            _remoteAppDataService = remoteAppDataService;
            _proxyConfigService = proxyConfigService;
            _awsS3Service = awsS3Service;
            _configuration = configuration;
            _adsTokenService = adsTokenService;
            _s3AccessLinkPrefix = _configuration.GetConnectionString("s3AccessLinkPrefix");
            _appDataServiceEndpoint = _configuration.GetConnectionString("appDataServiceUrl") + "/api/Messages/sendGenericMessage";
            _awsConfig = awsOptionsMonitor.CurrentValue;
            _userInfoService = userInfoService;
        }

        /// <summary>
        /// Validate a resource
        /// </summary>
        /// <param name="resources"></param>
        /// <returns></returns>
        public async Task<List<BulkUploadResult>> ValidateResource(List<ResourceRequestDTO> resources, bool ignoreNonMandatory = false)
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
                    //if (srcId == null)
                    //{
                    //    //Collect result
                    //    totalValidationResult.Add(new BulkUploadResult
                    //    {
                    //        ActionDone = "Error",
                    //        ErrorMessage = "SourceId not found.",
                    //        TimeTaken = stpWatch.ElapsedMilliseconds.ToString(),
                    //        pidUri = pidUri == null ? "" : pidUri.ToString(),
                    //        ResourceLabel = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasLabel, true),
                    //        ResourceDefinition = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasResourceDefintion, true),
                    //        StateItems = resource.StateItems
                    //    });
                    //    //// Delete msg from input Queue
                    //    //if (DeleteMessageFromSQS(msg.ReceiptHandle).Result == false)
                    //    //{
                    //    //    _logger.LogInformation("BackgroundService: Could not delete meessage");
                    //    //}
                    //    continue;
                    //}

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
                            string newResourceId = Graph.Metadata.Constants.Entity.IdPrefix + Guid.NewGuid();
                            //_logger.LogInformation("BackgroundService: About to Validate New resource: {msg}", msg.Body);
                            //Validate
                            var (validationResult, failed, validationFacade) =
                                await _resourcePreprocessService.ValidateAndPreProcessResource(newResourceId, resource, new ResourcesCTO(), ResourceCrudAction.Create,false,null,false, ignoreNonMandatory);
                            _logger.LogInformation("BackgroundService: Validation Complete for: {srcId} having status {stat}", srcId, failed.ToString());

                            //Update pidUri in stateItem                            
                            //UpdateStateItemsWithPidUri(resource, validationFacade.RequestResource.PidUri.ToString());

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
                            //if (!failed)
                            //    _proxyConfigService.AddUpdateNginxConfigRepository(resource);
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
                                await _resourcePreprocessService.ValidateAndPreProcessResource(id, resource, resourcesCTO, ResourceCrudAction.Publish, false, null,false, ignoreNonMandatory);
                            _logger.LogInformation("BackgroundService: Validation Complete for: {srcId} having status {stat}", srcId, failed.ToString());

                            //Update pidUri in stateItem                            
                            // UpdateStateItemsWithPidUri(resource, validationFacade.RequestResource.PidUri.ToString());

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
                                if (resourcesCTO.Published == null)
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
                                StateItems = resource.StateItems,
                                Triples = validationResult.Triples
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
                            //_proxyConfigService.AddUpdateNginxConfigRepository(resource);
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

        public async Task UploadImportExcelToS3(IFormFile file)
        {
            string userEmail = _userInfoService.GetEmail();
            MemoryStream inputStream = new MemoryStream();
            file.CopyTo(inputStream);
            try
            {
                using (SpreadsheetDocument doc = (SpreadsheetDocument)SpreadsheetDocument.Open(inputStream, true))
                {
                    //extract the important inner parts of the worksheet
                    WorkbookPart workbookPart = doc.WorkbookPart;
                    Sheets sheets = workbookPart.Workbook.GetFirstChild<Sheets>();

                    //Get worksheet of "TechnicalInfo"
                    Sheet techinicalInfoDataSheet = sheets.Elements<Sheet>().Where(s => s.Name == "TechnicalInfo").FirstOrDefault(); //Get data sheet for Technical Info (requested User)
                    Worksheet technicalInfoWorksheet = ((WorksheetPart)workbookPart.GetPartById(techinicalInfoDataSheet.Id)).Worksheet;
                    SheetData technicalInfoRowContainer = (SheetData)technicalInfoWorksheet.GetFirstChild<SheetData>();

                    //Validate User email address
                    List<string> emailRowValues = getRowValues(0, doc, technicalInfoRowContainer);
                    if (emailRowValues[1] == string.Empty)
                    {
                        SendNotification(null, "Unable to upload Excel file - No user email mentioned in TechnicalInfo Tab", userEmail);
                        return;
                    }
                    var validRequester = _remoteAppDataService.CheckPerson(emailRowValues[1]);
                    if (!validRequester)
                    {
                        SendNotification(null, "Unable to upload Excel file - Invalid user email mentioned in TechnicalInfo Tab", userEmail);
                        return;
                    }                                       

                    //Update Current Date
                    Row docUploadDate = technicalInfoRowContainer.Elements<Row>().ElementAt(1);
                    Cell docUploadDateCell = docUploadDate.Elements<Cell>().Where(c => string.Compare
                   (c.CellReference.Value, "B" + docUploadDate.RowIndex.Value, true) == 0).First();

                    docUploadDateCell.CellValue = new CellValue(DateTime.UtcNow.ToString());
                    docUploadDateCell.DataType = CellValues.String;

                    //Update Uploaded User
                    Row docUploadByUser = technicalInfoRowContainer.Elements<Row>().ElementAt(2);
                    Cell docUploadByUserCell = docUploadByUser.Elements<Cell>().Where(c => string.Compare
                   (c.CellReference.Value, "B" + docUploadByUser.RowIndex.Value, true) == 0).First();

                    docUploadByUserCell.CellValue = new CellValue(_userInfoService.GetEmail());
                    docUploadByUserCell.DataType = CellValues.String;


                    workbookPart.Workbook.Save();
                    doc.Close();
                    // Reset pointer in stream
                    inputStream.Seek(0, SeekOrigin.Begin);


                    var formFile = new FormFile(inputStream, 0, inputStream.Length, "file", userEmail.Split("@")[0] + ".xlsx")
                    {
                        Headers = new HeaderDictionary(),
                        ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                    };

                    await _awsS3Service.UploadFileAsync(_awsConfig.S3BucketForImportExcelInput, Guid.NewGuid().ToString(), formFile);
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogInformation("ImportService: " + (ex.InnerException == null ? ex.Message : ex.InnerException.Message));
                SendNotification(null, "Unable to upload Excel file - " + (ex.InnerException == null ? ex.Message : ex.InnerException.Message), userEmail);
            }
           
        }

        public async Task ImportExcel()
        {
            try
            {                
                //Get all Excel files to Import                
                Dictionary<string, Stream> fileContents = await _awsS3Service.GetAllFileAsync(_awsConfig.S3BucketForImportExcelInput);

                foreach (KeyValuePair<string, Stream> fileContent in fileContents)
                {
                    try
                    {
                        _logger.LogInformation("ImportService: Found Excel File - " + fileContent.Key);

                        //Copy stream to MemoryStream
                        var inputstream = new MemoryStream();
                        fileContent.Value.CopyTo(inputstream);

                        //Delete excel file from Input Bucket
                        await _awsS3Service.DeleteFileAsync(_awsConfig.S3BucketForImportExcelInput, fileContent.Key);

                        //Process Excel file
                        MemoryStream processedFile = await ExecuteImportExcel(inputstream);
                    }
                    catch (System.Exception ex)
                    {
                        _logger.LogInformation("ImportService: " + (ex.InnerException == null ? ex.Message : ex.InnerException.Message));
                    }
                }                
            }
            catch (System.Exception ex)
            {
                _logger.LogWarning("ImportService: " + (ex.InnerException == null ? ex.Message : ex.InnerException.Message));
            }
        }


        public async Task<MemoryStream> ExecuteImportExcel(MemoryStream inputstream)
        {   //Constant Column Number
            const int ColPidUri = 9;
            const int ColPublishedDraft = 5;
            const int ColTypeREDE = 6;
            const int ColLinkType = 5;
            const int ColLinkTargetPidUri = 4;
            const int ColLinkSourcePidUri = 3;
            const string x = "x";
            const string CREATE = "CREATE";
            const string UPDATE = "UPDATE";
            const string DELETE = "DELETE";
            const string CHANGETYPE = "CHANGETYPE";
            const string PUBLISHED = "PUBLISHED";

            List<string> ignorePoroperties = new List<string> {
                Graph.Metadata.Constants.Identifier.HasUriTemplate                
            };

            string userEmail = "";
            string respondToUserEmail = "";
            try
            {                
                using (SpreadsheetDocument doc = (SpreadsheetDocument)SpreadsheetDocument.Open(inputstream, true))
                {                    
                    //extract the important inner parts of the worksheet
                    WorkbookPart workbookPart = doc.WorkbookPart;
                    Sheets sheets = workbookPart.Workbook.GetFirstChild<Sheets>();

                    //Get worksheet of "TechnicalInfo"
                    Sheet techinicalInfoDataSheet = sheets.Elements<Sheet>().Where(s => s.Name == "TechnicalInfo").FirstOrDefault(); //Get data sheet for Technical Info (requested User)
                    Worksheet technicalInfoWorksheet = ((WorksheetPart)workbookPart.GetPartById(techinicalInfoDataSheet.Id)).Worksheet;
                    SheetData technicalInfoRowContainer = (SheetData)technicalInfoWorksheet.GetFirstChild<SheetData>();
                    userEmail = this.getRowValues(0, doc, technicalInfoRowContainer)[1];
                    respondToUserEmail = this.getRowValues(2, doc, technicalInfoRowContainer)[1];

                    Sheet dataSheet = sheets.Elements<Sheet>().Where(s => s.Name == "Data").FirstOrDefault(); //Get data sheet
                    Sheet linkDataSheet = sheets.Elements<Sheet>().Where(s => s.Name == "Links").FirstOrDefault(); //Get data sheet for Links


                    //Get worksheet of "Data" 
                    Worksheet worksheet = ((WorksheetPart)workbookPart.GetPartById(dataSheet.Id)).Worksheet;
                    SheetData rowContainer = (SheetData)worksheet.GetFirstChild<SheetData>();

                    //Get worksheet of "Links" 
                    Worksheet linksWorksheet = ((WorksheetPart)workbookPart.GetPartById(linkDataSheet.Id)).Worksheet;
                    SheetData linksRowContainer = (SheetData)linksWorksheet.GetFirstChild<SheetData>();


                    // get the third row of the template(which should include the PID URIs)                             
                    List<string> uriHeader = this.getRowValues(1, doc, rowContainer);
                    List<string> pidUriTypes = this.getRowValues(2, doc, rowContainer);
                    List<string> pidUris = this.getRowValues(3, doc, rowContainer);


                    //Collect resources that needs to be created, updated or deleted
                    var markedResources = new List<ExcelRowResource>();

                    //Loop throug all the Data rows to find rows that needs to be Created, Updated or Deleted                
                    for (int rowCtr = 4; rowCtr < worksheet.GetFirstChild<SheetData>().Descendants<Row>().Count(); rowCtr++)
                    {
                        List<string> curRowData = this.getRowValues(rowCtr, doc, rowContainer);

                        //Create
                        if (curRowData.ElementAt(1) != null && curRowData.ElementAt(1).ToUpper() == x.ToUpper() && curRowData.ElementAt(ColTypeREDE).ToUpper() == "RE")
                        {
                            markedResources.Add(new ExcelRowResource
                            {
                                Action = CREATE,
                                PublishOrDraft = curRowData.ElementAt(ColPublishedDraft).ToUpper(),
                                ExcelRow = rowContainer.Elements<Row>().ElementAt(rowCtr),
                                Resource = ConvertToResource(pidUris, pidUriTypes, curRowData, ignorePoroperties, doc, rowContainer, userEmail)
                            });
                        }
                        //Update
                        else if (curRowData.ElementAt(2) != null && curRowData.ElementAt(2).ToUpper() == x.ToUpper() && curRowData.ElementAt(ColTypeREDE).ToUpper() == "RE")
                        {
                            markedResources.Add(new ExcelRowResource
                            {
                                Action = UPDATE,
                                PublishOrDraft = curRowData.ElementAt(ColPublishedDraft).ToUpper(),
                                pidUri = curRowData.ElementAt(ColPidUri),
                                ExcelRow = rowContainer.Elements<Row>().ElementAt(rowCtr),
                                Resource = ConvertToResource(pidUris, pidUriTypes, curRowData, ignorePoroperties, doc, rowContainer, userEmail)
                            });
                        }
                        //Delete
                        else if (curRowData.ElementAt(3) != null && curRowData.ElementAt(3).ToUpper() == x.ToUpper() && curRowData.ElementAt(ColTypeREDE).ToUpper() == "RE")
                        {
                            markedResources.Add(new ExcelRowResource
                            {
                                Action = DELETE,
                                PublishOrDraft = curRowData.ElementAt(ColPublishedDraft).ToUpper(),
                                pidUri = curRowData.ElementAt(ColPidUri),
                                ExcelRow = rowContainer.Elements<Row>().ElementAt(rowCtr)
                            });
                        }
                        //Change Type
                        else if (curRowData.ElementAt(4) != null && curRowData.ElementAt(4).ToUpper() == x.ToUpper() && curRowData.ElementAt(ColTypeREDE).ToUpper() == "RE")
                        {
                            markedResources.Add(new ExcelRowResource
                            {
                                Action = CHANGETYPE,
                                PublishOrDraft = curRowData.ElementAt(ColPublishedDraft).ToUpper(),
                                pidUri = curRowData.ElementAt(ColPidUri),
                                ExcelRow = rowContainer.Elements<Row>().ElementAt(rowCtr),
                                Resource = ConvertToResource(pidUris, pidUriTypes, curRowData, ignorePoroperties, doc, rowContainer, userEmail)
                            });
                        }
                    }
                    
                    //Collect Links that needs to be Created, Updated or Deleted
                    var markedLinks = new List<ExcelRowLinks>();

                    //Loop throug all the Link rows to find Links that needs to be Created, Updated or Deleted                
                    for (int rowCtr = 2; rowCtr < linksWorksheet.GetFirstChild<SheetData>().Descendants<Row>().Count(); rowCtr++)
                    {
                        List<string> curRowLink = this.getRowValues(rowCtr, doc, linksRowContainer);

                        //Add
                        if (curRowLink.ElementAt(1).ToUpper() == x.ToUpper())
                        {
                            markedLinks.Add(new ExcelRowLinks
                            {
                                Action = CREATE,
                                SourcePidUri = curRowLink.ElementAt(ColLinkSourcePidUri),
                                TargetPidUri = curRowLink.ElementAt(ColLinkTargetPidUri),
                                LinkType = curRowLink.ElementAt(ColLinkType),
                                ExcelRow = linksRowContainer.Elements<Row>().ElementAt(rowCtr)
                            });
                        }
                        //Remove
                        else if (curRowLink.ElementAt(2).ToUpper() == x.ToUpper())
                        {
                            markedLinks.Add(new ExcelRowLinks
                            {
                                Action = DELETE,
                                SourcePidUri = curRowLink.ElementAt(ColLinkSourcePidUri),
                                TargetPidUri = curRowLink.ElementAt(ColLinkTargetPidUri),
                                LinkType = curRowLink.ElementAt(ColLinkType),
                                ExcelRow = linksRowContainer.Elements<Row>().ElementAt(rowCtr)
                            });
                        }
                    }

                    //var result = ValidateResource(markedResources.Where(f => f.Action != DELETE).Select(s => s.Resource).ToList());
                    foreach (ExcelRowResource markedResource in markedResources)
                    {
                        try
                        {
                            switch (markedResource.Action)
                            {
                                case CREATE:
                                    //If Published then do validation first or else it will leave a draft record in the database
                                    if (markedResource.PublishOrDraft == PUBLISHED)
                                    {
                                        var (validationResultForCrt, failedForCrt, validationFacadeForCrt) =
                                        _resourcePreprocessService.ValidateAndPreProcessResource(Graph.Metadata.Constants.Entity.IdPrefix + Guid.NewGuid(), markedResource.Resource, new ResourcesCTO(), ResourceCrudAction.Create).Result;                                        
                                        if (failedForCrt)
                                        {
                                            markedResource.ActionResponseMessage = "Validation Failed : " + JsonConvert.SerializeObject(validationResultForCrt.Results);
                                            break;
                                        }
                                    }

                                    //Create draft record
                                    var createResult = _resourceService.CreateResource(markedResource.Resource).Result;
                                    if (createResult.ValidationResult.Severity == ValidationResultSeverity.Violation)
                                    {
                                        markedResource.ActionResponseMessage = "Something went wrong while creating draft Resource: " + JsonConvert.SerializeObject(createResult.Resource) + " Error:" + JsonConvert.SerializeObject(createResult.ValidationResult);
                                    }
                                    else
                                    {
                                        markedResource.ActionResponseMessage = "Created Draft" + createResult.Resource.PidUri;
                                    }
                                    
                                    //publish record if marked as piblished
                                    if (markedResource.PublishOrDraft == PUBLISHED)
                                    {
                                        var publishCreatedResult = _resourceService.PublishResource(createResult.Resource.PidUri).Result;
                                        if (publishCreatedResult.ValidationResult.Severity == ValidationResultSeverity.Violation || publishCreatedResult.ValidationResult.Severity == ValidationResultSeverity.Warning)
                                        {
                                            markedResource.ActionResponseMessage = "Something went wrong while publishing Resource: " + JsonConvert.SerializeObject(publishCreatedResult.Resource) + " Error:" + JsonConvert.SerializeObject(publishCreatedResult.ValidationResult);
                                        }
                                        else
                                        {
                                            markedResource.ActionResponseMessage = "Created Published" + publishCreatedResult.Resource.PidUri;
                                        }                                        
                                    }
                                    break;
                                case UPDATE:
                                case CHANGETYPE:
                                    //If Published then do validation first or else it will leave a draft record in the database
                                    if (markedResource.PublishOrDraft == PUBLISHED)
                                    {
                                        var resourcesCTO = _resourceService.GetResourcesByPidUri(new Uri(markedResource.pidUri));
                                        var id = resourcesCTO.GetDraftOrPublishedVersion().Id;
                                        var (validationResultForUpd, failedForUpd, validationFacadeForUpd) =
                                            _resourcePreprocessService.ValidateAndPreProcessResource(id, markedResource.Resource, resourcesCTO, ResourceCrudAction.Publish, markedResource.Action == CHANGETYPE, null).Result;

                                        if (failedForUpd)
                                        {
                                            markedResource.ActionResponseMessage = "Validation Failed : " + JsonConvert.SerializeObject(validationResultForUpd.Results);
                                            break;
                                        }
                                    }

                                    //Update draft record
                                    var UpdateResult = _resourceService.EditResource(new Uri(markedResource.pidUri), markedResource.Resource, markedResource.Action == CHANGETYPE).Result;
                                    if (UpdateResult.ValidationResult.Severity == ValidationResultSeverity.Violation)
                                    {
                                        markedResource.ActionResponseMessage = "Something went wrong while Editing Resource: " + JsonConvert.SerializeObject(UpdateResult.Resource) + " Error:" + JsonConvert.SerializeObject(UpdateResult.ValidationResult);
                                    }
                                    else
                                    {
                                        markedResource.ActionResponseMessage = "Update Resource" + UpdateResult.Resource.PidUri;
                                    }
                                    
                                    //publish record if marked as piblished
                                    if (markedResource.PublishOrDraft == PUBLISHED)
                                    {
                                        var publishCreatedResult = _resourceService.PublishResource(UpdateResult.Resource.PidUri).Result;
                                        if (publishCreatedResult.ValidationResult.Severity == ValidationResultSeverity.Violation || publishCreatedResult.ValidationResult.Severity == ValidationResultSeverity.Warning)
                                        {
                                            markedResource.ActionResponseMessage = "Something went wrong while publishing Resource: " + JsonConvert.SerializeObject(publishCreatedResult.Resource) + " Error:" + JsonConvert.SerializeObject(publishCreatedResult.ValidationResult);
                                        }
                                        else
                                        {
                                            markedResource.ActionResponseMessage = "Update Published" + publishCreatedResult.Resource.PidUri;
                                        }                                        
                                    }
                                    break;
                                case DELETE:
                                    markedResource.ActionResponseMessage = _resourceService.MarkResourceAsDeletedAsync(new Uri(markedResource.pidUri), userEmail).Result;
                                    break;
                            }
                        }
                        catch (System.Exception ex)
                        {
                            if (ex.InnerException is ResourceValidationException)
                            {
                                markedResource.ActionResponseMessage = ex.InnerException.Message
                                    + " Resource : " + JsonConvert.SerializeObject(((ResourceValidationException)ex.InnerException).Resource)
                                    + " Validation Resilt :" + JsonConvert.SerializeObject(((ResourceValidationException)ex.InnerException).ValidationResult);
                            }
                            else
                            {
                                markedResource.ActionResponseMessage = (ex.InnerException == null ? ex.Message : ex.InnerException.Message);
                            }
                        }

                        //Update excel Row first column with result status
                        var cellRef = "A" + markedResource.ExcelRow.RowIndex.Value;
                        Cell curCell = new Cell() { CellReference = cellRef };
                        try
                        {
                            //try to get the existing cell..but it may not be there as sometimes Excel does not save blank cells
                            curCell = markedResource.ExcelRow.Elements<Cell>().Where(c => string.Compare(c.CellReference.Value, "A" + markedResource.ExcelRow.RowIndex.Value, true) == 0).First();
                        }
                        catch
                        {
                            markedResource.ExcelRow.InsertAt(curCell, 0);
                        }
                        
                        curCell.CellValue = new CellValue(markedResource.ActionResponseMessage);
                        curCell.DataType = CellValues.String;
                    }

                    foreach (ExcelRowLinks markedLink in markedLinks)
                    {
                        try
                        {
                            switch (markedLink.Action)
                            {
                                case CREATE:
                                    var creatResult = _resourceService.AddResourceLink(markedLink.SourcePidUri, markedLink.LinkType, markedLink.TargetPidUri, userEmail).Result;
                                    markedLink.ActionResponseMessage = "Link Added";
                                    break;

                                case DELETE:
                                    var deleteResult = _resourceService.RemoveResourceLink(markedLink.SourcePidUri, markedLink.LinkType, markedLink.TargetPidUri, false, userEmail).Result;
                                    markedLink.ActionResponseMessage = "Link Removed";
                                    break;

                            }
                        }
                        catch (System.Exception ex)
                        {
                            markedLink.ActionResponseMessage = (ex.InnerException == null ? ex.Message : ex.InnerException.Message);
                        }
                        
                        //Update excel Row first column with result status
                        var cellRef = "A" + markedLink.ExcelRow.RowIndex.Value;
                        Cell curCell = new Cell() { CellReference = cellRef };
                        try
                        {
                            //try to get the existing cell..but it may not be there as sometimes Excel does not save blank cells
                            curCell = markedLink.ExcelRow.Elements<Cell>().Where(c => string.Compare(c.CellReference.Value, "A" + markedLink.ExcelRow.RowIndex.Value, true) == 0).First();
                        }
                        catch
                        {
                            markedLink.ExcelRow.InsertAt(curCell, 0);
                        }

                        curCell.CellValue = new CellValue(markedLink.ActionResponseMessage);
                        curCell.DataType = CellValues.String;
                    }
                    
                    workbookPart.Workbook.Save();
                    doc.Close();
                    
                    // Reset pointer in stream
                    inputstream.Seek(0, SeekOrigin.Begin);

                    //Upload to S3
                    // Create formfile from stream
                    var formFile = new FormFile(inputstream, 0, inputstream.Length, "file", respondToUserEmail.Split("@")[0] + ".xlsx")
                    {
                        Headers = new HeaderDictionary(),
                        ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                    };
                   
                    // Upload the formfile using awsS3Service
                    var s3FileInfo = await _awsS3Service.UploadFileAsync(_awsConfig.S3BucketForFiles, Guid.NewGuid().ToString(), formFile, true);

                    // Close the stream
                    inputstream.Close();
                    
                    //Notify User
                    SendNotification(s3FileInfo, "", respondToUserEmail);                   
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogWarning("ImportService: " + (ex.InnerException == null ? ex.Message : ex.InnerException.Message));
                if (respondToUserEmail != string.Empty)
                    SendNotification(null, (ex.InnerException == null ? ex.Message : ex.InnerException.Message), respondToUserEmail);
            }

            return inputstream;
        }

        private List<string> getRowValues(int rowNumber, SpreadsheetDocument doc, SheetData rowContainer)
        {
            Row valueRow = rowContainer.Elements<Row>().ElementAt(rowNumber);

            List<string> rowValues = new List<string>();
            foreach (Cell cell in GetRowCells(valueRow))
            {
                //Loop through the cells and extract the PID URIs.
                //The following complicated looking snipped is used to get a proper string from the cells and unfortunately required
                if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
                {
                    SharedStringTablePart sharedStringTablePart = doc.WorkbookPart.GetPartsOfType<SharedStringTablePart>().First();
                    SharedStringItem[] items = sharedStringTablePart.SharedStringTable.Elements<SharedStringItem>().ToArray();
                    rowValues.Add(items[int.Parse(cell.CellValue.Text)].InnerText);
                }
                else
                {
                    rowValues.Add(cell.InnerText);

                }
            }
            return rowValues;
        }

        /// <summary>
        /// Conver Excel rows to ResourceRequestDTO
        /// </summary>
        /// <param name="pidUris"></param>
        /// <param name="rowData"></param>
        /// <param name="ignoreProperties"></param>
        /// <returns>ResourceRequestDTO</returns>
        private ResourceRequestDTO ConvertToResource(List<string> pidUris, List<string> pidUriTypes, List<string> rowData,
            List<string> ignoreProperties, SpreadsheetDocument doc, SheetData rowContainer, string userEmail)
        {
            //Constant Column Number
            int ColPidUri = 9;
            int ColPidUriTemplate = 10;
            int ColBaseUri = 13;
            int ColBaseUriTemplate = 14;

            //Loop through each properties
            ResourceRequestDTO curResource = new ResourceRequestDTO();
            for (int colCtr = 0; colCtr < pidUris.Count; colCtr++)
            {
                if (rowData.Count <= colCtr)// sometimes rowdata is less than header row (i.e. piduris count)
                    break;

                if (pidUris.ElementAt(colCtr) == Graph.Metadata.Constants.Resource.hasPID ||
                    (pidUris.ElementAt(colCtr) == Graph.Metadata.Constants.Resource.BaseUri && !String.IsNullOrEmpty(rowData.ElementAt(ColBaseUri))) ||
                    (!String.IsNullOrEmpty(pidUris.ElementAt(colCtr)) && !String.IsNullOrEmpty(rowData.ElementAt(colCtr))))
                {
                    //Ignore Properties
                    if (ignoreProperties.Contains(pidUris.ElementAt(colCtr)))
                        continue;
                    
                    if (pidUris.ElementAt(colCtr) == Graph.Metadata.Constants.Resource.hasPID)
                    {
                        // Treat hasPid differently
                        curResource.Properties.Add(pidUris.ElementAt(colCtr), new List<dynamic> {
                                new COLID.Graph.TripleStore.DataModels.Base.Entity(rowData.ElementAt(ColPidUri), new Dictionary<string,List<dynamic>>() {
                                    { Graph.Metadata.Constants.RDF.Type, new List<dynamic> { Graph.Metadata.Constants.Identifier.Type } },
                                    { Graph.Metadata.Constants.Identifier.HasUriTemplate,  new List<dynamic> {rowData.ElementAt(ColPidUriTemplate) } }
                                })
                            });
                    }
                    else if(pidUris.ElementAt(colCtr) == Graph.Metadata.Constants.Resource.BaseUri) 
                    {
                        // Treat hasBaseUri differently
                        curResource.Properties.Add(pidUris.ElementAt(colCtr), new List<dynamic> {
                                new COLID.Graph.TripleStore.DataModels.Base.Entity(rowData.ElementAt(ColBaseUri), new Dictionary<string,List<dynamic>>() {
                                    { Graph.Metadata.Constants.RDF.Type, new List<dynamic> { Graph.Metadata.Constants.Identifier.Type } },
                                    { Graph.Metadata.Constants.Identifier.HasUriTemplate,  new List<dynamic> {rowData.ElementAt(ColBaseUriTemplate) == string.Empty? rowData.ElementAt(ColPidUriTemplate) : rowData.ElementAt(ColBaseUriTemplate) } }
                                })
                            });
                    }
                    else if (pidUris.ElementAt(colCtr) == Graph.Metadata.Constants.Resource.Distribution || pidUris.ElementAt(colCtr) == Graph.Metadata.Constants.Resource.MainDistribution)
                    {
                        // Collect distributionendpoint information
                        string[] endpoints = rowData.ElementAt(colCtr).Split(",");
                        List<dynamic> curDistributionList = new List<dynamic>();
                        for (int ctr = 0; ctr < endpoints.Count(); ctr++)
                        {
                            if (int.TryParse(endpoints[ctr], out int result))
                            {
                                List<string> curcurDistEndPointRowData = this.getRowValues(result + 3, doc, rowContainer);
                                var curDistEndPoint = ConvertToResource(pidUris, pidUriTypes, curcurDistEndPointRowData, ignoreProperties, doc, rowContainer, userEmail);
                                curDistributionList.Add(new COLID.Graph.TripleStore.DataModels.Base.Entity("", curDistEndPoint.Properties));
                            }
                        }
                        curResource.Properties.Add(pidUris.ElementAt(colCtr), curDistributionList);
                    }
                    else if (pidUris.ElementAt(colCtr) == Graph.Metadata.Constants.Resource.DateCreated || pidUris.ElementAt(colCtr) == Graph.Metadata.Constants.Resource.DateModified)
                    {
                        curResource.Properties.Add(pidUris.ElementAt(colCtr), new List<dynamic> { DateTime.UtcNow });
                    }
                    else if (pidUris.ElementAt(colCtr) == Graph.Metadata.Constants.Resource.Author || pidUris.ElementAt(colCtr) == Graph.Metadata.Constants.Resource.LastChangeUser)
                    {
                        curResource.Properties.Add(pidUris.ElementAt(colCtr), new List<dynamic> { userEmail });
                    }
                    else
                    {
                        if (pidUriTypes.ElementAt(colCtr).Contains("comma separated"))
                        {
                            string[] values = rowData.ElementAt(colCtr).Split(",");
                            List<dynamic> curValues = new List<dynamic>();
                            for (int ctr = 0; ctr < values.Count(); ctr++)
                            {
                                if(values.ElementAt(ctr) != null || values.ElementAt(ctr) != string.Empty)
                                    curValues.Add(values.ElementAt(ctr).Trim());
                            }
                            curResource.Properties.Add(pidUris.ElementAt(colCtr), curValues);
                        }
                        else
                        {
                            curResource.Properties.Add(pidUris.ElementAt(colCtr), new List<dynamic> { rowData.ElementAt(colCtr) });
                        }
                    }                    
                }
            }

            ////Check for Links in case of Update
            //if (linksRowContainer != null && curResource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.hasPID, true).Id != string.Empty)
            //{
            //    //FIlter on PidUri
            //    Dictionary<string, List<dynamic>> curProp = new Dictionary<string, List<dynamic>>();               
            //    for (int rowCtr = 2; rowCtr < linksRowContainer.ChildElements.Count; rowCtr++)
            //    {
            //        List<string> curRowLink = this.getRowValues(rowCtr, doc, linksRowContainer);

            //        if (curRowLink.ElementAt(ColLinkTargetPidUri) == curResource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.hasPID, true).Id)
            //        {
            //            if (curProp.ContainsKey(curRowLink.ElementAt(ColLinkType)))
            //            {
            //                if (!curProp[curRowLink.ElementAt(ColLinkType)].Contains(curRowLink.ElementAt(ColLinkSourcePidUri)))                            
            //                    curProp[curRowLink.ElementAt(ColLinkType)].Add(curRowLink.ElementAt(ColLinkSourcePidUri));

            //            }
            //            else
            //            {
            //                curProp.Add(curRowLink.ElementAt(ColLinkType), new List<dynamic> { curRowLink.ElementAt(ColLinkSourcePidUri) });
            //            }                        
            //        }
            //    }
            //    //Add Links to resource
            //    curResource.Properties.AddRange(curProp);                
            //}

            return curResource;
        }

        ///<summary>returns an empty cell when a blank cell is encountered
        ///</summary>
        private IEnumerable<Cell> GetRowCells(Row row)
        {
            int currentCount = 0;

            foreach (DocumentFormat.OpenXml.Spreadsheet.Cell cell in
                row.Descendants<DocumentFormat.OpenXml.Spreadsheet.Cell>())
            {
                string columnName = GetColumnName(cell.CellReference);

                int currentColumnIndex = ConvertColumnNameToNumber(columnName);

                for (; currentCount < currentColumnIndex; currentCount++)
                {
                    yield return new DocumentFormat.OpenXml.Spreadsheet.Cell();
                }

                yield return cell;
                currentCount++;
            }
        }

        /// <summary>
        /// Given a cell name, parses the specified cell to get the column name.
        /// </summary>
        /// <param name="cellReference">Address of the cell (ie. B2)</param>
        /// <returns>Column Name (ie. B)</returns>
        private string GetColumnName(string cellReference)
        {
            // Match the column name portion of the cell name.
            var regex = new System.Text.RegularExpressions.Regex("[A-Za-z]+");
            var match = regex.Match(cellReference);

            return match.Value;
        }

        /// <summary>
        /// Given just the column name (no row index),
        /// it will return the zero based column index.
        /// </summary>
        /// <param name="columnName">Column Name (ie. A or AB)</param>
        /// <returns>Zero based index if the conversion was successful</returns>
        /// <exception cref="ArgumentException">thrown if the given string
        /// contains characters other than uppercase letters</exception>
        private int ConvertColumnNameToNumber(string columnName)
        {
            var alpha = new System.Text.RegularExpressions.Regex("^[A-Z]+$");
            if (!alpha.IsMatch(columnName)) throw new ArgumentException();

            char[] colLetters = columnName.ToCharArray();
            Array.Reverse(colLetters);

            int convertedValue = 0;
            for (int i = 0; i < colLetters.Length; i++)
            {
                char letter = colLetters[i];
                int current = i == 0 ? letter - 65 : letter - 64; // ASCII 'A' = 65
                convertedValue += current * (int)Math.Pow(26, i);
            }

            return convertedValue;
        }

        /// <summary>
        /// Send a notification via AppDataService to the user informing 
        /// about successful export and download link
        /// </summary>
        /// <param name="uploadInfoDto"></param>
        private async void SendNotification(AmazonS3FileUploadInfoDto uploadInfoDto, string errorMsg, string userEmail)
        {
            HttpClient client = new HttpClient();
            try
            {
                var fileLink = this._s3AccessLinkPrefix + (uploadInfoDto == null ? "" : uploadInfoDto.FileKey);
                // Generate generic message
                var message = new MessageUserDto()
                {
                    Subject = "Excel import status",
                    Body = errorMsg == string.Empty ? _messageBody(fileLink) : errorMsg,
                    UserEmail = userEmail
                };

                // Convert Message to JSON-Object
                string jsonobject = JsonConvert.SerializeObject(message);
                StringContent content = new StringContent(jsonobject, Encoding.UTF8, "application/json");

                //Set AAD token
                var accessToken = await _adsTokenService.GetAccessTokenForWebApiAsync();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                // Send JSON-Object to AppDataService endpoint
                HttpResponseMessage notification_response = await client.PutAsync(_appDataServiceEndpoint, content);
                notification_response.EnsureSuccessStatusCode();
                return;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning("ExcelImport: An error occurred while passing the notification to the AppData service.", ex.Message);
                throw ex;
            }
        }


        public class ExcelRowResource
        {
            public string Action { get; set; }
            public string pidUri { get; set; }
            public string PublishOrDraft { get; set; }
            public Row ExcelRow { get; set; }
            public ResourceRequestDTO Resource { get; set; }
            public string ActionResponseMessage { get; set; }
        }

        public class ExcelRowLinks
        {
            public string Action { get; set; }
            public string SourcePidUri { get; set; }
            public string TargetPidUri { get; set; }
            public string LinkType { get; set; }
            public Row ExcelRow { get; set; }
            public string ActionResponseMessage { get; set; }
        }
    }
}
