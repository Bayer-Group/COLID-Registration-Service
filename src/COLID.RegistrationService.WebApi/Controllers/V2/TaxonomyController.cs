using System;
using System.Net.Mime;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.WebApi.Controllers.V2.Filter;
using COLID.RegistrationService.WebApi.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace COLID.RegistrationService.WebApi.Controllers.V2
{
    /// <summary>
    /// 
    /// </summary>
    [ApiController]
    //[Authorize]
    [ApiVersion(Constants.API.Version.V2)]
    [Route("api/v{version:apiVersion}")]
    [Produces(MediaTypeNames.Application.Json)]
    [TransformIdPropertyResponseFilter]
    [Obsolete("A new version of this endpoint is available")]
    public class TaxonomyController : Controller
    {
        private readonly ITaxonomyService _taxonomyService;

        /// <summary>
        /// API endpoint for entities.
        /// </summary>
        /// <param name="taxonomyService">The service for entities</param>
        public TaxonomyController(ITaxonomyService taxonomyService)
        {
            _taxonomyService = taxonomyService;
        }

        /// <summary>
        /// Returns a taxonomy list, with all narrower.
        /// </summary>
        /// <param name="taxonomyType">The type of taxonomy to search</param>
        /// <returns>The taxonomy as list with all narrower.</returns>
        /// <response code="200">Returns the taxonomy as list. If there are no taxonomy, an empty list is returned.</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpGet]
        [ValidateActionParameters]
        [Route("taxonomyList")]
        public IActionResult GetTaxonomies([FromQuery] string taxonomyType)
        {
            var taxonomies = _taxonomyService.GetTaxonomies(taxonomyType);

            return Ok(taxonomies);
        }

        /// <summary>
        /// Returns a specific taxonomy item with the given subject.
        /// </summary>
        /// <param name="subject">The subject of specific taxonomy item.</param>
        /// <returns>A taxonomy item with all narrower</returns>
        /// <response code="200">Returns taxonomy item with all narrower</response>
        /// <response code="404">If no taxonomy exists with the given subject</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpGet]
        [Authorize]
        [ValidateActionParameters]
        [Route("taxonomy")]
        public IActionResult GetTaxonomyById([FromQuery] string subject)
        {
            var taxonomy = _taxonomyService.GetEntity(subject);

            if (taxonomy == null)
            {
                return NotFound("No taxonomy for given subject: " + subject);
            };

            return Ok(taxonomy);
        }
    }
}
