using System;
using System.Net.Mime;
using System.Threading.Tasks;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.RegistrationService.Services.Authorization.Requirements;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.WebApi.Controllers.V2.Filter;
using COLID.RegistrationService.WebApi.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace COLID.RegistrationService.WebApi.Controllers.V2
{
    /// <summary>
    /// API endpoint for consumer groups.
    /// </summary>
    [ApiController]
    [Authorize]
    [ApiVersion(Constants.API.Version.V1)]
    [ApiVersion(Constants.API.Version.V2)]
    [Route("api/v{version:apiVersion}/distributionEndpoint")]
    [Produces(MediaTypeNames.Application.Json)]
    [TransformIdPropertyResponseFilter]
    [Obsolete("A new version of this endpoint is available")]
    public class DistributionEndpointController : Controller
    {
        private readonly IDistributionEndpointService _distributionEndpointService;

        /// <summary>
        /// API endpoint for distribution endpoints.
        /// </summary>
        /// <param name="distributionEndpointService">The service for distribution endpoints</param>
        public DistributionEndpointController(IDistributionEndpointService distributionEndpointService)
        {
            _distributionEndpointService = distributionEndpointService;
        }

        /// <summary>
        /// Creates a distribution endpoint and appends it to the resource of the given pid uri.
        /// </summary>
        /// <remarks>
        /// - If an endpoint is stored as a main distribution endpoint, the old main distribution endpoint becomes a normal endpoint.
        /// - If there is no draft resource, a new draft is created. This resource must be published separately.
        /// </remarks>
        /// <param name="resourcePidUri">Pid uri of the resource to which the distribution endpoint is to be attached.</param>
        /// <param name="createAsMainDistributionEndpoint">Specifies whether an endpoint is stored as a main distribution endpoint.</param>
        /// <param name="entity">Distribution endpoint to create.</param>
        [MapToApiVersion(Constants.API.Version.V1)]
        [MapToApiVersion(Constants.API.Version.V2)]
        [HttpPost]
        [Authorize(Policy = nameof(CreateDistributionEndpointRequirement))]
        [ValidateActionParameters]
        public async Task<IActionResult> Post([FromQuery] Uri resourcePidUri, [FromBody] BaseEntityRequestDTO entity, [FromQuery] bool createAsMainDistributionEndpoint = false)
        {
            return Ok(await _distributionEndpointService.CreateDistributionEndpoint(resourcePidUri, createAsMainDistributionEndpoint, entity).ConfigureAwait(false));
        }

        /// <summary>
        /// Edit a distribution endpoint and appends to given pid uri.
        /// </summary>
        /// <remarks>
        /// - If an endpoint is stored as a main distribution endpoint, the old main distribution endpoint becomes a normal endpoint.
        /// - If there is no draft resource, a new draft is created. This resource must be published separately.
        /// </remarks>
        /// <param name="distributionEndpointPidUri">Pid uri of the distribution endpoint to be edited</param>
        /// <param name="editAsMainDistributionEndpoint">Specifies whether an endpoint is stored as a main distribution endpoint.</param>
        /// <param name="requestDistributionEndpoint">Distribution endpoint to be edited</param>
        /// <returns></returns>
        [HttpPut]
        [Authorize(Policy = nameof(EditDistributionEndpointRequirement))]
        [ValidateActionParameters]
        public async Task<IActionResult> Put([FromQuery] Uri distributionEndpointPidUri, [FromBody] BaseEntityRequestDTO requestDistributionEndpoint, [FromQuery] bool editAsMainDistributionEndpoint = false)
        {
            return Ok(await _distributionEndpointService.EditDistributionEndpoint(distributionEndpointPidUri, editAsMainDistributionEndpoint, requestDistributionEndpoint).ConfigureAwait(false));
        }

        /// <summary>
        /// Deletes the endpoint distribution with the given pid uri
        /// </summary>
        /// <remarks>
        /// - If there is no draft resource, a new draft is created. The endpoint still exists at the published resource. This resource (draft) must be published separately.
        /// </remarks>
        /// <param name="distributionEndpointPidUri">Pid uri of the distribution endpoint to be deleted</param>
        /// <returns></returns>
        [HttpDelete]
        [Authorize(Policy = nameof(EditDistributionEndpointRequirement))]
        [ValidateActionParameters]
        public async Task<IActionResult> DeleteDistributionEndpoint(Uri distributionEndpointPidUri)
        {
            _distributionEndpointService.DeleteDistributionEndpoint(distributionEndpointPidUri);

            return Ok();
        }
    }
}
