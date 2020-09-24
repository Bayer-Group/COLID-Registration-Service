using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading.Tasks;
using COLID.Common.DataModel.Attributes;
using COLID.Exception.Models.Business;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Identity.Requirements;
using COLID.RegistrationService.Common.DataModel.Resources;
using COLID.RegistrationService.Common.DataModel.Search;
using COLID.RegistrationService.Services.Authorization.Requirements;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.WebApi.Controllers.V2.Filter;
using COLID.RegistrationService.WebApi.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace COLID.RegistrationService.WebApi.Controllers.V2
{
    /// <summary>
    /// API endpoint for resources.
    /// </summary>
    [ApiController]
    [Authorize]
    [ApiVersion(Constants.API.Version.V1)]
    [ApiVersion(Constants.API.Version.V2)]
    [Route("api/v{version:apiVersion}/resource")]
    [Produces(MediaTypeNames.Application.Json)]
    [TransformIdPropertyResponseFilter]
    [Obsolete]
    public class ResourceController : Controller
    {
        private readonly IResourceService _resourceService;
        private readonly IResourceLinkingService _resourceLinkingService;
        private readonly IHistoricResourceService _historicResourceService;

        /// <summary>
        /// API endpoint for resources.
        /// </summary>
        /// <param name="resourceService">The service for resources</param>
        /// <param name="resourceLinkingService">The service for linking two resources as versions together</param>
        public ResourceController(IResourceService resourceService, IResourceLinkingService resourceLinkingService, IHistoricResourceService historicResourceService)
        {
            _resourceService = resourceService;
            _resourceLinkingService = resourceLinkingService;
            _historicResourceService = historicResourceService;
        }

        /// <summary>
        /// If parameter main is false:
        ///    - Returns the newest COLID entry of the given resource subject.
        ///    - If a draft of the resource exists, the draft is returned. Otherwise, the published resource is returned.
        /// If parameter main is true:
        ///    - Returns the main COLID entry of the given resource subject.
        ///    - If a published of the resource exists, the published is returned. Otherwise, the draft resource is returned.
        /// </summary>
        /// <param name="pidUri">The pid uri of the resource, which is the same in the draft as well as in the published versions.</param>
        /// <param name="main">A boolean to get the main or the newest resource</param>
        /// <returns>The newest or main COLID entry of the given resource subject</returns>
        /// <response code="200">Returns the newest COLID entry</response>
        /// <response code="400">Returns if the COLID entry does not exists</response>
        /// <response code="404">If no COLID entry of the resource has been found with the pid uri</response>
        /// <response code="500">If an unexpected error occurs</response>
        [MapToApiVersion(Constants.API.Version.V1)]
        [HttpGet]
        [ValidateActionParameters]
        [ProducesResponseType(typeof(Resource), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public IActionResult GetResourceByPidUri([FromQuery]Uri pidUri, [FromQuery, NotRequired] bool main)
        {
            return Ok(main ? _resourceService.GetMainResourceByPidUri(pidUri) : _resourceService.GetByPidUri(pidUri));
        }

        /// <summary>
        /// Get a resource, filtered by it's pid entry lifecycle status (draft, published or markedfordeletion)<br />
        /// Note: Historic is not allowed! Use historic endpoint instead.
        /// </summary>
        /// <param name="pidUri">The pid uri of the resource.</param>
        /// <param name="pidEntryLifecycleStatus">The status to fetch</param>
        /// <returns>The filtered or COLID entry of the given resource subject</returns>
        /// <response code="200">Returns the COLID entry</response>
        /// <response code="404">If a COLID entry with the given entry lifecycle status does not exist</response>
        [MapToApiVersion(Constants.API.Version.V2)]
        [HttpGet]
        [ValidateActionParameters]
        [ProducesResponseType(typeof(Resource), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public IActionResult GetByPidUriAndPidEntryLifecycleStatus([FromQuery]Uri pidUri, [FromQuery, NotRequired] Uri pidEntryLifecycleStatus)
        {
            if (string.IsNullOrWhiteSpace(pidEntryLifecycleStatus?.ToString()))
            {
                var latestResource = _resourceService.GetByPidUri(pidUri);
                return Ok(latestResource);
            }

            var specificResource = _resourceService.GetByPidUriAndLifecycleStatus(pidUri, pidEntryLifecycleStatus);
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
        /// Edits and saves a given resource as a draft with the given subject.
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
        public async Task<IActionResult> EditResource([FromQuery] Uri pidUri, [FromBody]ResourceRequestDTO resource)
        {
            var result = await _resourceService.EditResource(pidUri, resource);

            return Ok(result);
        }

        /// <summary>
        /// Deletes a PID entry of a resource by its given pidUri.
        /// </summary>
        /// <remarks>
        ///   <br><b>Note:</b> To physically delete a resource from COLID Admin rights are required!</br>
        /// </remarks>
        /// <param name="pidUri">The pidUri of the PID entry to delete</param>
        /// <returns>Returns a status message according to the result</returns>
        /// <response code="200">Returns status code only</response>
        /// <response code="400">If the given pidUri is null or empty, or the COLID entry could not be deleted (e.g. because it was not present in COLID)</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpDelete]
        [ValidateActionParameters]
        [Authorize(Policy = nameof(ResourceRequirement))]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public IActionResult DeleteResourceAsync([FromQuery] Uri pidUri)
        {
            var result = _resourceService.DeleteResource(pidUri);

            return Ok(result);
        }

        /// <summary>
        /// Publish a given resource with the given subject.
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
        /// <returns>A status code</returns>
        /// <response code="200">Returns status code only</response>
        /// <response code="400">If the given pid uri is null or empty, or the COLID entry could not be deleted (e.g. because it was not present in COLID)</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPut]
        [Obsolete]
        [ValidateActionParameters]
        [Authorize(Policy = nameof(ResourceRequirement))]
        [Route("markForDeletion")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> MarkDeleteResourceAsync([FromQuery] Uri pidUri)
        {
            throw new DeprecatedVersionException(Constants.API.DeprecatedVersion);
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

        /// <summary>
        /// Determine all historic entries, identified by the given pidUri, and returns overview information of them.
        /// </summary>
        /// <param name="pidUri">the resource to search for</param>
        /// <returns>a list of resource-information related to the pidUri</returns>
        [MapToApiVersion(Constants.API.Version.V2)]
        [HttpGet]
        [ValidateActionParameters]
        [Route("historyList")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public IActionResult GetHistoricOverviewList([FromQuery] string pidUri)
        {
            var result = _historicResourceService.GetHistoricOverviewByPidUri(pidUri);

            return Ok(result);
        }

        /// <summary>
        /// Determine a single historic entry, identified by the given unique subject and pidUri.
        /// </summary>
        /// <param name="pidUri">the resource pidUri to search for</param>
        /// <param name="subject">the resource subject to search for</param>
        /// <returns>a single historized resource</returns>
        [MapToApiVersion(Constants.API.Version.V2)]
        [HttpGet]
        [ValidateActionParameters]
        [Route("history")]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public IActionResult GetHistoricResource([FromQuery] string pidUri, [FromQuery] string subject)
        {
            var result = _historicResourceService.GetHistoricResource(pidUri, subject);

            return Ok(result);
        }

        #endregion Historic Resources
    }
}
