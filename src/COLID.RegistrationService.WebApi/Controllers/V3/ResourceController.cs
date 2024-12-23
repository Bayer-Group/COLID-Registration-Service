﻿using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using COLID.Common.DataModel.Attributes;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Identity.Requirements;
using COLID.RegistrationService.Common.DataModel.Resources;
using COLID.RegistrationService.Common.DataModel.Search;
using COLID.RegistrationService.Common.DataModels.LinkHistory;
using COLID.RegistrationService.Common.DataModels.Resources;
using COLID.RegistrationService.Services.Authorization.Requirements;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.WebApi.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace COLID.RegistrationService.WebApi.Controllers.V3
{
    /// <summary>
    /// API endpoint for resources.
    /// </summary>
    [ApiController]
    [Authorize]
    [ApiVersion(Constants.API.Version.V3)]
    [Route("api/v{version:apiVersion}/resource")]
    [Produces(MediaTypeNames.Application.Json)]
    public class ResourceController : Controller
    {
        private readonly IResourceService _resourceService;
        private readonly IResourceLinkingService _resourceLinkingService;
        //private readonly IHistoricResourceService _historicResourceService;
        private readonly IResourceComparisonService _resourceComparisonService;
        //private readonly IEndpointTestService _endpointTestService;

        /// <summary>
        /// API endpoint for resources.
        /// </summary>
        /// <param name="resourceService">The service for resources</param>
        /// <param name="resourceLinkingService">The service for linking two resources as versions together</param>        
        /// <param name="resourceComparisonService">The service for comparison of resources</param>
        /// <param name="attachmentService">The service to add attachments to resources</param>
        public ResourceController(IResourceService resourceService,
            IResourceLinkingService resourceLinkingService,
            //IHistoricResourceService historicResourceService,
            IResourceComparisonService resourceComparisonService,
            IAttachmentService attachmentService
            )
        {
            _resourceService = resourceService;
            _resourceLinkingService = resourceLinkingService;
            //_historicResourceService = historicResourceService;
            _resourceComparisonService = resourceComparisonService;
        }

        /// <summary>
        /// Get a resource, filtered by it's pid entry lifecycle status (draft, published or markedfordeletion)<br />
        /// Note: Historic is not allowed! Use historic endpoint instead.
        /// </summary>
        /// <param name="pidUri">The pid uri of the resource.</param>
        /// <param name="lifecycleStatus">The status to fetch</param>
        /// <returns>The filtered or COLID entry of the given resource Id</returns>
        /// <response code="200">Returns the COLID entry</response>
        /// <response code="404">If a COLID entry with the given entry lifecycle status does not exist</response>
        [HttpGet]
        [ValidateActionParameters]
        [ProducesResponseType(typeof(Resource), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public IActionResult GetByPidUriAndLifecycleStatus([FromQuery] Uri pidUri, [FromQuery, NotRequired] Uri lifecycleStatus)
        {
            if (string.IsNullOrWhiteSpace(lifecycleStatus?.ToString()))
            {
                var latestResource = _resourceService.GetByPidUri(pidUri);
                return Ok(latestResource);
            }

            var specificResource = _resourceService.GetByPidUriAndLifecycleStatus(pidUri, lifecycleStatus);
            return Ok(specificResource);
        }

        /// <summary>
        /// Returns a list containing an overview of all COLID entries with reduced information.
        /// If a draft of the COLID entry exists, the overview contains information of the draft. Otherwise, it contains information of the published version.
        /// </summary>
        /// <returns>A list containing an overview over the COLID entries</returns>
        /// <response code="200">Returns a list containing an overview over the COLID entries</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpGet]
        [Route("search")]
        [ValidateActionParameters]
        [ProducesResponseType(typeof(IList<ResourceOverviewDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult GetResourceOverview([FromQuery] ResourceSearchCriteriaDTO resourceSearchDTO)
        {
            var result = _resourceService.SearchByCriteria(resourceSearchDTO);

            return Ok(result);
        }

        /// <summary>
        /// Checks all due resources for review and notifies responsible people 
        /// </summary>
        /// <returns>Status of Notification process</returns>
        /// <response code="200">Status of Notification process</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPut]
        [Route("notifyDueReviews")]
        [ValidateActionParameters]
        [ProducesResponseType(typeof(Dictionary<string, List<string>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult GetResourceDueForReview()
        {
            var result = _resourceService.NotifyForDueReviews().Result;

            return Ok(result);
        }

        /// <summary>
        /// Returns a list of resources that are due for review latest on the specified date.
        /// </summary>
        /// <param name="consumerGroup">The consumer group of the resources to be retrieved</param>
        /// <param name="endDate">The end date specified in yyyy-mm-dd or mm.dd.yyyy  </param>
        /// <returns>A list containing resources that are due for review</returns>
        /// <response code="200">Returns a list containing resources that are due for review</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpGet]
        [Route("dueReviews")]
        [ValidateActionParameters]
        [ProducesResponseType(typeof(IList<Resource>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult GetResourceDueForReview([FromQuery, NotRequired] Uri consumerGroup, DateTime endDate)
        {
            var result = _resourceService.GetDueResources(consumerGroup, endDate);
            //TODO Adjust the SPARQL query to return the also brokenLinks property
            return Ok(result);
        }

        /// <summary>
        /// Confirms a review cycle for given resource without publishing it.
        /// </summary>
        /// <returns>A transfer object for the resource the review should be confirmed for</returns>
        /// <response code="200">A transfer object for the resource the review should be confirmed for</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPut]
        [Route("confirmReview")]
        [ValidateActionParameters]
        [ProducesResponseType(typeof(IList<ResourceOverviewDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult ConfirmReviewCycleForResource([FromQuery] Uri pidUri)
        {
            _resourceService.ConfirmReviewCycleForResource(pidUri);

            return Ok();
        }

        /// <summary> 
        /// Returns a list containing an overview of all COLID entries with reduced information. 
        /// If a draft of the COLID entry exists, the overview contains information of the draft. Otherwise, it contains information of the published version. 
        /// </summary> 
        /// <returns>A list containing an overview over the COLID entries</returns> 
        /// <response code="200">Returns a list containing an overview over the COLID entries</response> 
        /// <response code="500">If an unexpected error occurs</response> 
        [HttpPost]
        [Route("search")]
        [ValidateActionParameters]
        [ProducesResponseType(typeof(IList<ResourceOverviewDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult GetResourceOverviewForLongSearchTerm([FromBody] ResourceSearchCriteriaDTO resourceSearchDTO)
        {
            var result = _resourceService.SearchByCriteria(resourceSearchDTO);

            return Ok(result);
        }


        /// <summary>
        /// Adds a new semantic link to a specific resource
        /// </summary>
        /// <returns>The resource containing the new links</returns>
        /// <response code="200">Resource containing new created links</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPost]
        [Route("addLink")]
        [ValidateActionParameters]
        [Authorize(Policy = nameof(ResourceRequirement))]
        [ProducesResponseType(typeof(Resource), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateResourceLinkAsync([FromQuery] string pidUri, [FromQuery] string linkType, [FromQuery] string pidUriToLink, [FromQuery] string requester)
        {
            var result = await _resourceService.AddResourceLink(pidUri, linkType, pidUriToLink,requester);

            return Ok(result);
        }

        /// <summary>
        /// Remove a new semantic link from a specific resource
        /// </summary>
        /// <returns>The resource containing the new links</returns>
        /// <response code="200">Resource containing new created links</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPost]
        [Route("removeLink")]
        [ValidateActionParameters]
        [Authorize(Policy = nameof(ResourceRequirement))]
        [ProducesResponseType(typeof(Resource), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RemoveResourceLink([FromQuery] string pidUri, [FromQuery] string linkType, [FromQuery] string pidUriToUnLink, [FromQuery] bool returnTargetResource, [FromQuery] string requester)
        {
            var result = await _resourceService.RemoveResourceLink(pidUri, linkType, pidUriToUnLink, returnTargetResource, requester);

            return Ok(result);
        }

        /// <summary>
        /// Gets the revision history for a specific resource
        /// </summary>
        /// <returns>The resource revision history containing additionals and removals</returns>
        /// <response code="200">Resource containing new created links</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpGet]
        [ValidateActionParameters]
        [Route("resourcerevisionshistory")]
        [ProducesResponseType(typeof(List<ResourceRevision>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetResourceRevisionsHistory(Uri pidUri)
        {
            var result =  await _resourceService.GetResourceRevisionsHistory(pidUri);

            return Ok(result);
        }
        /// <summary>
        /// Returns all distribution endpoints related to a resource.
        /// </summary>
        /// <param name="pidUri">Pid uri of related resource</param>
        /// <returns>List of distribution endpoints</returns>
        [HttpGet]
        [Route("distributionEndpointList")]
        [ValidateActionParameters]
        [ProducesResponseType(typeof(IList<Entity>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult GetDistributionEndpointsOfResource([FromQuery] Uri pidUri)
        {
            var result = _resourceService.GetDistributionEndpoints(pidUri);

            return Ok(result);
        }

        /// <summary>
        /// Compares all fields of two resources.
        /// </summary>
        /// <param name="id">The IDs of the resources</param>
        /// <returns>Returns a comparison object containing the comparison result.</returns>
        /// <response code="200">Returns the comparison result of the two resources.</response>
        /// <response code="400">If the given request is invalid, e.g. if both IDs are equal</response>
        /// <response code="404">If one of the given IDs is not found</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPost]
        [ValidateActionParameters]
        [Route("comparison")]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 404)]
        public IActionResult CompareResources([FromQuery] string[] id)
        {
            var result = _resourceComparisonService.CompareResources(id);
            return Ok(result);
        }

        /// <summary>
        /// Creates and saves a given resource as a draft.
        /// </summary>
        /// <remarks>
        ///   <br>The following fields are critical and are required for each save operation.</br>
        ///   <ul>
        ///    <li>EntityType     - http://www.w3.org/1999/02/22-rdf-syntax-ns#type </li>
        ///    <li>DateCreated    - https://pid.bayer.com/kos/19050/dateCreatedhasLabel </li>
        ///    <li>Author         - https://pid.bayer.com/kos/19050/author </li>
        ///    <li>LastChangeUser - https://pid.bayer.com/kos/19050/lastChangeUser </li>
        ///    <li>ConsumerGroup  - https://pid.bayer.com/kos/19050#hasConsumerGroup </li>
        ///    <li>Label          - https://pid.bayer.com/kos/19050/hasLabel </li>
        ///    <li>PidUri         - http://pid.bayer.com/kos/19014/hasPID </li>
        ///    <li>Version        - https://pid.bayer.com/kos/19050/hasVersion </li>
        ///    <li>BaseUri        - https://pid.bayer.com/kos/19050/hasBaseURI (Ontology)</li>
        ///   </ul>
        /// </remarks>
        /// <param name="resource">The resource to save as a draft</param>
        /// <returns>The validation results of the newly created resource draft</returns>
        /// <response code="200">Returns the validation results of the newly created resource draft</response>
        /// <response code="400">If the given resource cannot be mapped to the resource object or has a duplicate PID URI</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPost]
        [ValidateActionParameters]
        [Authorize(Policy = nameof(ResourceRequirement))]
        [Produces(MediaTypeNames.Application.Json, Type = typeof(Task<ResourceWriteResultCTO>))]
        [ProducesResponseType(typeof(Task<ResourceWriteResultCTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateResource([FromBody] ResourceRequestDTO resource)
        {
            //Validate Resource
            var result = await _resourceService.CreateResource(resource);

            return Ok(result);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("batch")]
        [ValidateActionParameters]
        [Authorize(Policy = nameof(ResourceRequirement))]
        [Produces(MediaTypeNames.Application.Json, Type = typeof(Task<ResourceWriteResultCTO>))]
        [ProducesResponseType(typeof(Task<ResourceWriteResultCTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateResourceBatch([FromBody] ResourceRequestDTO resource)
        {
            //Validate Resource
            var result = _resourceService.QueueResourceCreation(resource);

            return Ok(result);
        }

        /// <summary>
        /// Edits and saves a given resource as a draft with the given Id.
        /// </summary>
        /// <remarks>
        ///   <br>The following fields are critical and are required for each save operation.</br>
        ///   <ul>
        ///    <li>Entitytype     - http://www.w3.org/1999/02/22-rdf-syntax-ns#type </li>
        ///    <li>DateCreated    - https://pid.bayer.com/kos/19050/dateCreatedhasLabel </li>
        ///    <li>Author         - https://pid.bayer.com/kos/19050/author </li>
        ///    <li>LastChangeUser - https://pid.bayer.com/kos/19050/lastChangeUser </li>
        ///    <li>ConsumerGroup  - https://pid.bayer.com/kos/19050#hasConsumerGroup </li>
        ///    <li>Label          - https://pid.bayer.com/kos/19050/hasLabel </li>
        ///    <li>PidUri         - http://pid.bayer.com/kos/19014/hasPID </li>
        ///    <li>Version        - https://pid.bayer.com/kos/19050/hasVersion </li>
        ///    <li>BaseUri        - https://pid.bayer.com/kos/19050/hasBaseURI (Ontology)</li>
        ///   </ul>
        /// </remarks>
        /// <param name="pidUri">The pidUri of the existing resource to save as a draft</param>
        /// <param name="resource">The resource to save as a draft</param>
        /// <returns>The validation results of the edited resource draft</returns>
        /// <response code="200">Returns the validation results of the edited resource draft</response>
        /// <response code="400">If the given resource cannot be mapped to the resource object or has a duplicate PID URI</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPut]
        [ValidateActionParameters]
        [Authorize(Policy = nameof(ResourceRequirement))]
        [ProducesResponseType(typeof(Task<ResourceWriteResultCTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EditResource([FromQuery] Uri pidUri, [FromBody] ResourceRequestDTO resource)
        {
            var result = await _resourceService.EditResource(pidUri, resource);

            return Ok(result);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pidUri"></param>
        /// <param name="resource"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("batch")]
        [ValidateActionParameters]
        [Authorize(Policy = nameof(ResourceRequirement))]
        [ProducesResponseType(typeof(Task<ResourceWriteResultCTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EditResourceBatch([FromQuery] Uri pidUri, [FromBody] ResourceRequestDTO resource)
        {
            var result = _resourceService.QueueResourceEdit(pidUri, resource);

            return Ok(result);
        }

        /// <summary> 
        /// Edits and saves a given resource also by changing its type as a draft with the given Id. 
        /// </summary> 
        /// <remarks> 
        ///   <br>The following fields are critical and are required for each save operation.</br> 
        ///   <ul> 
        ///    <li>Entitytype     - http://www.w3.org/1999/02/22-rdf-syntax-ns#type </li> 
        ///    <li>DateCreated    - https://pid.bayer.com/kos/19050/dateCreatedhasLabel </li> 
        ///    <li>Author         - https://pid.bayer.com/kos/19050/author </li> 
        ///    <li>LastChangeUser - https://pid.bayer.com/kos/19050/lastChangeUser </li> 
        ///    <li>ConsumerGroup  - https://pid.bayer.com/kos/19050#hasConsumerGroup </li> 
        ///    <li>Label          - https://pid.bayer.com/kos/19050/hasLabel </li> 
        ///    <li>PidUri         - http://pid.bayer.com/kos/19014/hasPID </li> 
        ///    <li>Version        - https://pid.bayer.com/kos/19050/hasVersion </li> 
        ///    <li>BaseUri        - https://pid.bayer.com/kos/19050/hasBaseURI (Ontology)</li> 
        ///   </ul> 
        /// </remarks> 
        /// <param name="pidUri">The pidUri of the existing resource to save as a draft</param> 
        /// <param name="resource">The resource to save as a draft</param> 
        /// <returns>The validation results of the edited resource draft</returns> 
        /// <response code="200">Returns the validation results of the edited resource draft</response> 
        /// <response code="400">If the given resource cannot be mapped to the resource object or has a duplicate PID URI</response> 
        /// <response code="500">If an unexpected error occurs</response> 
        [HttpPut]
        [ValidateActionParameters]
        [Route("type")]
        [Authorize(Policy = nameof(ResourceRequirement))]
        [ProducesResponseType(typeof(Task<ResourceWriteResultCTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EditResourceType([FromQuery] Uri pidUri, [FromBody] ResourceRequestDTO resource)
        {
            var result = await _resourceService.EditResource(pidUri, resource, true);

            return Ok(result);
        }

        /// <summary>
        /// Deletes a PID entry of a resource by its given pidUri.
        /// </summary>
        /// <remarks>
        ///   <br><b>Note:</b> To physically delete a resource from COLID Admin rights are required!</br>
        /// </remarks>
        /// <param name="pidUri">The pidUri of the PID entry to delete</param>
        /// <param name="requester"> email id of the requester</param>"
        /// <returns>Returns a status message according to the result</returns>
        /// <response code="200">Returns status code only</response>
        /// <response code="400">If the given pidUri is null or empty, or the COLID entry could not be deleted (e.g. because it was not present in COLID)</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpDelete]
        [ValidateActionParameters]
        [Authorize(Policy = nameof(ResourceRequirement))]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteResourceAsync([FromQuery] Uri pidUri, [FromQuery] string requester)
        {
            var result =await _resourceService.DeleteResource(pidUri, requester);

            return Ok(result);
        }

        /// <summary>
        /// Publish a given resource with the given Id.
        /// </summary>
        /// <param name="pidUri">The complete PID URI of the existing resource to publish</param>
        /// <returns>The validation results of the edited and published resource</returns>
        /// <response code="200">Returns the validation results of the edited and published resource</response>
        /// <response code="400">If the given resource cannot be mapped to the resource object, the validation results contain errors, or the COLID entry has a duplicate PID URI</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPut]
        [ValidateActionParameters]
        [Authorize(Policy = nameof(ResourceRequirement))]
        [Route("publish")]
        [ProducesResponseType(typeof(Task<ResourceWriteResultCTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PublishResource([FromQuery] Uri pidUri)
        {
            var result = await _resourceService.PublishResource(pidUri).ConfigureAwait(true);

            return Ok(result);
        }

        /// <summary>
        /// Mark a COLID entry of a resource by its given pid uri as deleted.
        /// </summary>
        /// <param name="pidUri">The pidUri of the COLID entry to delete</param>
        /// <param name="requester">The requester that marks the resource for deletion</param>
        /// <returns>A status code</returns>
        /// <response code="200">Returns status code only</response>
        /// <response code="400">If the given pid uri is null or empty, or the COLID entry could not be deleted (e.g. because it was not present in COLID)</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPut]
        [ValidateActionParameters]
        [Authorize(Policy = nameof(ResourceRequirement))]
        [Route("markForDeletion")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> MarkDeleteResourceAsync([FromQuery] Uri pidUri, [FromQuery] string requester)
        {
            var result = await _resourceService.MarkResourceAsDeletedAsync(pidUri, requester);

            return Ok(result);
        }
        
        /// <summary>
        /// Mark a COLID entry of a multiple resource by its given pid uri as deleted.
        /// </summary>
        /// <param name="pidUris">The List of pidUris of related PID entries to delete</param>
        /// <param name="requester">The requester that marks the resource for deletion</param>
        /// <returns>Returns a status message according to the result</returns>
        /// <response code="200">Returns status code only</response>
        /// <response code="400">If the given pid uri is null or empty, or the COLID entry could not be deleted (e.g. because it was not present in COLID)</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPut]
        [ValidateActionParameters]
        [Authorize(Policy = nameof(AdministratorRequirement))]
        [Route("markForDeletionList")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> MarkDeleteResourceListAsync([FromQuery] string requester, [FromBody] IList<Uri> pidUris)
        {
            //List<Uri> pidUris = null;
            var result = await _resourceService.MarkResourceAsDeletedListAsync(pidUris, requester);

            return Ok(result);
        }

        /// <summary>
        /// Unmark a COLID entry of a resource by its given pid uri as deleted.
        /// </summary>
        /// <param name="pidUri">The pidUri of the COLID entry to delete</param>
        /// <returns>A status code</returns>
        /// <response code="200">Returns status code only</response>
        /// <response code="400">If the given pid uri is null or empty, or the COLID entry could not be deleted (e.g. because it was not present in COLID)</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPut]
        [ValidateActionParameters]
        [Authorize(Policy = nameof(AdministratorRequirement))]
        [Route("unmarkFromDeletion")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult UnmarkDeleteResourceAsync([FromQuery] Uri pidUri)
        {
            var result = _resourceService.UnmarkResourceAsDeleted(pidUri);

            return Ok(result);
        }

        /// <summary>
        /// Link resource to another one
        /// </summary>
        /// <param name="currentPidUri">PID URI of the resource to be linked</param>
        /// <param name="linkToPidUri">PID URI of the resource to be linked to</param>
        /// <returns>Returns a status message according to the result.</returns>
        /// <response code ="200" > Returns status code only</response>
        /// <response code="400">If the given pid uri is null or empty</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPut]
        [ValidateActionParameters]
        [Authorize(Policy = nameof(ResourceRequirement))]
        [Route("version/link")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult LinkResourceIntoList([FromQuery] Uri currentPidUri, [FromQuery] Uri linkToPidUri)
        {
            var result = _resourceLinkingService.LinkResourceIntoList(currentPidUri, linkToPidUri);

            return Ok(result);
        }

        /// <summary>
        /// Unlinking of two resources
        /// </summary>
        /// <param name="pidUri">PID URI of the resource, , which should be unlinked</param>
        /// <returns>Returns a status message according to the result.</returns>
        /// <response code ="200">Returns status code only</response>
        /// <response code="400">If the given pid uri is null or empty</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPut]
        [ValidateActionParameters]
        [Authorize(Policy = nameof(ResourceRequirement))]
        [Route("version/unlink")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult UnlinkResourceFromList([FromQuery] Uri pidUri)
        {
            _resourceLinkingService.UnlinkResourceFromList(pidUri, false, out var result);

            return Ok(result);
        }

        #region Historic Resources

        ///// <summary>
        ///// Determine all historic entries, identified by the given pidUri, and returns overview information of them.
        ///// </summary>
        ///// <param name="pidUri">the resource to search for</param>
        ///// <returns>a list of resource-information related to the pidUri</returns>
        //[MapToApiVersion(Constants.API.Version.V3)]
        //[HttpGet]
        //[ValidateActionParameters]
        //[Route("historyList")]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        //public IActionResult GetHistoricOverviewList([FromQuery] string pidUri)
        //{
        //    var result = _historicResourceService.GetHistoricOverviewByPidUri(pidUri);

        //    return Ok(result);
        //}

        ///// <summary>
        ///// Determine a single historic entry, identified by the given unique id and pidUri.
        ///// </summary>
        ///// <param name="pidUri">the resource pidUri to search for</param>
        ///// <param name="id">the resource id to search for</param>
        ///// <returns>a single historized resource</returns>
        //[MapToApiVersion(Constants.API.Version.V3)]
        //[HttpGet]
        //[ValidateActionParameters]
        //[Route("history")]
        //[ProducesResponseType(200)]
        //[ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        //public IActionResult GetHistoricResource([FromQuery] string pidUri, [FromQuery] string id)
        //{
        //    var result = _historicResourceService.GetHistoricResource(pidUri, id);

        //    return Ok(result);
        //}

        #endregion Historic Resources

        /// <summary>
        /// Unmarks COLID entris of multiple resources by its given pid uris as deleted.
        /// </summary>
        /// <param name="pidUris">A list of pidUris of the COLID entries to delete</param>
        /// <returns>A status code</returns>
        /// <response code="200">Returns status code only</response>
        /// <response code="400">If the given pid uris is null or empts</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPut]
        [ValidateActionParameters]
        [Authorize(Policy = nameof(AdministratorRequirement))]
        [Route("resourceList/reject")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult UnmarkDeleteResourcesAsync([FromBody] IList<Uri> pidUris)
        {
            var result = _resourceService.UnmarkResourcesAsDeleted(pidUris);

            return Ok(result);
        }

        /// <summary>
        /// Deletes PID entries of multiple resource by its given pidUris.
        /// </summary>
        /// <remarks>
        ///   <br><b>Note:</b> To physically delete a resource from COLID Admin rights are required!</br>
        /// </remarks>
        /// <param name="pidUris">The List of pidUris of related PID entries to delete</param>
        /// <param name="requester">email of person requesting the operation</param>
        /// <returns>Returns a status message according to the result</returns>
        /// <response code="200">Returns status code only</response>
        /// <response code="400">If the given pidUris are null or empty</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPut]
        [ValidateActionParameters]
        [Authorize(Policy = nameof(AdministratorRequirement))]
        [Route("resourceList")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteMarkedForDeletionResourcesAsync([FromQuery] string requester, [FromBody] IList<Uri> pidUris)
        {
            var result = await _resourceService.DeleteMarkedForDeletionResources(pidUris, requester);

            return Ok(result);
        }

        /// <summary>
        /// Returns the links associated with the resources of pidUris
        /// </summary>
        /// <param name="pidUris"></param>
        /// <response code="200">Returns status code only</response>
        /// <response code="400">If the given pidUris are null or empty</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPost]
        [ValidateActionParameters]
        [Route("linkedResourceList")]
        [ProducesResponseType(typeof(IList<Resource>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IList<Resource>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(IList<Resource>), StatusCodes.Status500InternalServerError)]
        public IActionResult GetResourceAndLinkForGraph([FromBody] IList<Uri> pidUris)
        {
            var result = _resourceLinkingService.GetLinksOfPublishedResource(pidUris);
            return Ok(result);
        }

        /// <summary>
        /// Get Table and column detail.
        /// </summary>        
        /// <param name="pidUri">The pidUri of related PID entries </param>
        /// <returns>Return list of table and colum</returns>
        /// <response code="200">Returns status code only</response>
        /// <response code="400">If the given pidUris are null or empty</response>
        /// <response code="500">If an unexpected error occurs</response>
        [MapToApiVersion(Constants.API.Version.V3)]
        [HttpGet]
        [ValidateActionParameters]
        [Route("linkedTableAndColumnResource")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetTableAndColumnResource([FromQuery] Uri pidUri)
        {
            var result = await _resourceService.GetTableAndColumnById(pidUri);

            return Ok(result);
        }

        /// <summary>
        /// Get Table and column detail against multiple piduris.
        /// </summary>        
        /// <param name="pidUris">List of pidUri </param>
        /// <returns>Return list of table and colum against each piduri</returns>
        /// <response code="200">Returns status code only</response>
        /// <response code="400">If the given pidUris are null or empty</response>
        /// <response code="500">If an unexpected error occurs</response>
        [MapToApiVersion(Constants.API.Version.V3)]
        [HttpPost]
        [ValidateActionParameters]
        [Route("linkedTableAndColumnResourceByPidUris")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetTableAndColumnResourceByPidUris([FromBody] IList<Uri> pidUris)
        {
            var result = await _resourceService.GetTableAndColumnByPidUris(pidUris);

            return Ok(result);
        }

        /// <summary>
        ///Gets Resource Type Hierarchy.
        /// </summary>
        /// 
        /// <param name="resourceTypes">The base concept class for resource </param>
        /// <returns>Return resources hierarchy of the base resource</returns>
        /// <response code="200">Returns status code only</response>
        /// <response code="400">If the given resource type is null or empty</response>
        /// <response code="500">If an unexpected error occurs</response>
        [MapToApiVersion(Constants.API.Version.V3)]
        [HttpPut]
        [ValidateActionParameters]
        [Route("getResourceHierarchy")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetResourceHierarchy([FromBody] IList<string> resourceTypes)
        {
            var result = await _resourceService.GetResourceHierarchy(resourceTypes);

            return Ok(result);
        }

        /// <summary>
        /// Starts indexing process a new resource towards on indexing crawler service.
        /// </summary>
        /// <param name="pidUri">Pid Uri of resource to be indexed</param>
        /// <returns></returns>
        [HttpPost]
        [ValidateActionParameters]        
        [Route("indexNewResource")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult IndexNewResource(Uri pidUri)
        {
            _resourceService.IndexNewResource(pidUri);
            return Ok();
        }

        /// <summary>
        /// Starts indexing process of an updated resource towards on indexing crawler service.
        /// </summary>
        /// <param name="pidUri">Pid Uri of resource to be indexed</param>
        /// <returns></returns>
        [HttpPost]
        [ValidateActionParameters]
        [Route("indexUpdatedResource")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult IndexUpdatedResource(Uri pidUri)
        {
            _resourceService.IndexUpdatedResource(pidUri);
            return Ok();
        }

        /// <summary>
        /// link repair
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("linkFix")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult linkFix()
        {
            Thread background = new Thread(() => _resourceService.linkFix());
            background.Start();

            return NoContent();
        }

        /// <summary>
        /// Get Link History        
        /// </summary>
        /// <param name="startPidUri">The pid uri of the start resource.</param>   
        /// <param name="endPidUri">Optional pid uri of the end resource.</param>  
        /// <returns>List of link history</returns>
        /// <response code="200">Returns list of link history</response>
        /// <response code="404">If link history does not exist</response>
        [HttpGet]
        [Route("linkHistory")]
        [ValidateActionParameters]
        [ProducesResponseType(typeof(List<LinkHistoryDto>), StatusCodes.Status200OK)]        
        public IActionResult GetLinkHistory([FromQuery] Uri startPidUri, [FromQuery, NotRequired] Uri endPidUri)
        {
            var linkHistory = _resourceService.GetLinkHistory(startPidUri, endPidUri);
            return Ok(linkHistory);
        }

        /// <summary>
        /// Search Link History        
        /// </summary>
        /// <param name="searchParam">Parameters: StartDate, EndDate, Optional: email, linkType  OrderByColumn: CreatedDate, CreatedBy, DeletionDate, DeletedBy and Status </param>             
        /// <returns>List of link history</returns>
        /// <response code="200">Returns list of link history</response>
        /// <response code="404">If link history does not exist</response>
        [HttpPost]
        [Route("searchLinkHistory")]
        [ValidateActionParameters]
        [ProducesResponseType(typeof(List<LinkHistoryDto>), StatusCodes.Status200OK)]
        public IActionResult SearchLinkHistory([FromBody] LinkHistorySearchParamDto searchParam)
        {
            var linkHistory = _resourceService.SearchLinkHistory(searchParam);
            return Ok(linkHistory);
        }

        /// <summary>
        /// Eligible Collibra Data Types      
        /// </summary>
        /// <param> </param>             
        /// <returns>List of eligible Resource types</returns>
        /// <response code="200">Returns list of eligible resource types</response>
        [HttpGet]
        [Route("eligibleCollibraDataTypes")]
        [ValidateActionParameters]
        [ProducesResponseType(typeof(IList<string>), StatusCodes.Status200OK)]
        public IActionResult GetEligibleCollibraDataTypes()
        {
            var results = _resourceService.GetEligibleCollibraDataTypes();
            return Ok(results);
        }

        /// <summary>
        /// Fetch resource PID Uris for Collibra      
        /// </summary>
        /// <param> </param>             
        /// <returns>List of eligible pid URIs</returns>
        /// <response code="200">Returns list of eligible resource types</response>
        [HttpGet]
        [Route("PIDURIsForCollibra")]
        [ValidateActionParameters]
        [ProducesResponseType(typeof(IList<string>), StatusCodes.Status200OK)]
        public IActionResult GetPIDURIsForCollibra()
        {
            var results = _resourceService.GetPIDURIsForCollibra();
            return Ok(results);
        }

        /// <summary>
        /// Processes PID Uris for Collibra      
        /// </summary>
        /// <param> </param>             
        /// <returns>List of eligible pid URIs</returns>
        /// <response code="200">Returns list of eligible resource types</response>
        [HttpPost]
        [Route("PIDURIsForCollibra")]
        [ValidateActionParameters]
        [ProducesResponseType(typeof(IList<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> PostPIDURIsForCollibra()
        {
          await _resourceService.PostPIDURIsForCollibra();
          return Ok();

        }
    }
}

