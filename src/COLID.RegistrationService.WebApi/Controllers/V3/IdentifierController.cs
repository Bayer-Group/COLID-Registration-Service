﻿using System;
using System.Net.Mime;
using COLID.Common.DataModel.Attributes;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.WebApi.Filters;
using COLID.StatisticsLog.Type;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using COLID.RegistrationService.Services.Validation;
using COLID.Identity.Requirements;
using System.Collections.Generic;
using System.Threading.Tasks;
using COLID.RegistrationService.Common.DataModels.Search;
using COLID.RegistrationService.Common.DataModels.RelationshipManager;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace COLID.RegistrationService.WebApi.Controllers.V3
{
    /// <summary>
    /// API endpoint for identifiers.
    /// </summary>
    [ApiController]
    [Authorize]
    [ApiVersion(Constants.API.Version.V3)]
    [Route("api/v{version:apiVersion}/identifier")]
    [Produces(MediaTypeNames.Application.Json)]
    public class IdentifierController : Controller
    {
        private readonly IIdentifierValidationService _identifierValidationService;
        private readonly IIdentifierService _identifierService;

        /// <summary>
        /// API endpoint for distribtuion endpoints.
        /// </summary>
        /// <param name="identifierValidationService">The service for identifier validation</param>
        /// <param name="identifierService"></param>
        public IdentifierController(IIdentifierValidationService identifierValidationService, IIdentifierService identifierService)
        {
            _identifierValidationService = identifierValidationService;
            _identifierService = identifierService;
        }

        /// <summary>
        /// Checks if one of the identifiers of the entry is a duplicate
        /// </summary>
        /// <param name="request">The DuplicateRequest containing the pid entry. Note that the URI to be checked is in the id-field!</param>
        /// <param name="previousVersion">If you create an entry with a previous version</param>
        /// <returns>Returns a validation result object containing the used resource and validation errors</returns>
        /// <response code="200">Returns the validation result, indicating if any identifier or target uri is a duplicate</response>
        /// <response code="400">If the given request is invalid</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPost]
        [ValidateActionParameters]
        [Route("checkForDuplicate")]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(string), 400)]
        public IActionResult CheckDuplicate([FromBody] Entity request, [FromQuery, NotRequired] Uri previousVersion)
        {
            var result = _identifierValidationService.CheckDuplicates(request, request.Id, previousVersion?.ToString());

            return Ok(result);
        }

        /// <summary>
        /// Determine all oprhaned identifiers and returns them in a list. An Identifier is an orphaned one,
        /// if it doesn't have any relation to a pid-uri or base-uri.
        /// </summary>
        /// <returns>Returns a list containing the uris as strings</returns>
        /// <response code="200">Returns the identifier list</response>
        /// <response code="500">An unexpected error occurs</response>
        [HttpGet]
        [ValidateActionParameters]
        [Route("orphanedList")]
        [Authorize(Policy = nameof(SuperadministratorRequirement))]
        [ProducesResponseType(200)]
        public IActionResult GetOrphanedIdentifierList()
        {
            var result = _identifierService.GetOrphanedIdentifiersList();
            return Ok(result);
        }

        /// <summary>
        /// Delete an orphaned identifier which matches the given URI.
        /// </summary>
        /// <remarks>
        /// This URI will be checked for any relations, before the identifier will be deleted.
        /// <para><b>Caution:</b> If the uri doesn't match to an orphaned one, it won't be deleted but status 200 returns!</para>
        /// </remarks>
        /// <param name="uri">The URI of the extended uri template to delete.</param>
        /// <response code="200">Ok, and the orphaned identifier was deleted.</response>
        /// <response code="404">There is no resource for the given uri</response>
        /// <response code="500">The uri is in the wrong format or no uri given</response>
        [HttpDelete]
        [ValidateActionParameters]
        [Route("orphaned")]
        [ProducesResponseType(200)]
        [Authorize(Policy = nameof(SuperadministratorRequirement))]
        [ProducesResponseType(typeof(string), 404)]
        [Log(LogType.AuditTrail)]
        public IActionResult DeleteOrphanedIdentifier([FromQuery] string uri)
        {
            _identifierService.DeleteOrphanedIdentifier(uri);
            return Ok();
        }

        /// <summary>
        /// Delete an orphaned multiple identifier which matches the given URI.
        /// </summary>
        /// <remarks>
        /// This URI will be checked for any relations, before the identifier will be deleted.
        /// <para><b>Caution:</b> If the uri doesn't match to an orphaned one, it won't be deleted but status 200 returns!</para>
        /// </remarks>
        /// <param name="uris">The URI of the extended uri template to delete.</param>
        /// <response code="200">Ok, and the orphaned identifier was deleted.</response>
        /// <response code="404">There is no resource for the given uri</response>
        /// <response code="500">The uri is in the wrong format or no uri given</response>
        [HttpDelete]
        [ValidateActionParameters]
        [Route("orphanedList")]
        [ProducesResponseType(200)]
        [Authorize(Policy = nameof(SuperadministratorRequirement))]
        [ProducesResponseType(typeof(string), 404)]
        [Log(LogType.AuditTrail)]
        public async Task<IActionResult> DeleteOrphanedIdentifierList([FromBody] IList<string> uris)
        {
            var result= await _identifierService.DeleteOrphanedIdentifierList(uris);
            return Ok(result);
        }

        /// <summary>
        /// Register the Saved Search as a PID URI
        /// </summary>
        /// <param name="searchFilterProxyDTORequest">The request from MarketPlace UI with user saved search</param>
        /// <returns>Returns the DTO back with the filled PIDURI generated by this endpoint</returns>
        /// <response code="200">Returns the DTO with newly generated PIDURI</response>
        /// <response code="400">If the given request is invalid</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPost]
        [ValidateActionParameters]
        [Route("savedsearch")]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(string), 400)]
        public async Task<IActionResult> RegisterSavedSearchAsIdentifier([FromBody] SearchFilterProxyDTO searchFilterProxyDTORequest)
        {
            var result = await _identifierService.RegisterSavedSearchAsIdentifier(searchFilterProxyDTORequest);

            return Ok(result);
        }

        /// <summary>
        /// Register the Saved Search as a PID URI
        /// </summary>
        /// <param name="mapProxyDTO">The mapProxyDTO which has mapID from RRM</param>
        /// <returns>Returns the created and registered URI in the DTO</returns>
        /// <response code="200">Returns newly generated PIDURI</response>
        /// <response code="400">If the given request is invalid</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPost]
        [ValidateActionParameters]
        [Route("rrmMaps")]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(string), 400)]
        public async Task<IActionResult> RegisterRRMMapAsIdentifier([FromBody] MapProxyDTO mapProxyDTO)
        {
            var result = await _identifierService.RegisterRRMMapAsIdentifier(mapProxyDTO);

            return Ok(result);
        }
    }
}
