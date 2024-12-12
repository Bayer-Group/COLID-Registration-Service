using System.Net.Mime;
using System.Web;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.WebApi.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace COLID.RegistrationService.WebApi.Controllers.V3
{
    /// <summary>
    /// 
    /// </summary>
    [ApiController]
    [Authorize]
    [ApiVersion(Constants.API.Version.V3)]
    [Route("api/v{version:apiVersion}")]
    [Produces(MediaTypeNames.Application.Json)]
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
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, VaryByHeader = "taxonomyType")]
        public IActionResult GetTaxonomies([FromQuery] string taxonomyType)
        {
            var taxonomies = _taxonomyService.GetTaxonomies(taxonomyType);

            return Ok(taxonomies);
        }

        /// <summary>
        /// Returns a specific taxonomy item with the given Id.
        /// </summary>
        /// <param name="id">The Id of specific taxonomy item.</param>
        /// <returns>A taxonomy item with all narrower</returns>
        /// <response code="200">Returns taxonomy item with all narrower</response>
        /// <response code="404">If no taxonomy exists with the given Id</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpGet]
        [Authorize]
        [ValidateActionParameters]
        [Route("taxonomy")]
        public IActionResult GetTaxonomyById([FromQuery] string id)
        {
            var taxonomy = _taxonomyService.GetEntity(id);

            if (taxonomy == null)
            {
                return NotFound("No taxonomy for given Id: " + id);
            };

            return Ok(taxonomy);
        }

        [HttpGet]
        [Authorize]
        [ValidateActionParameters]
        [Route("searchTaxonomy/{taxonomyType}")]
        public IActionResult SearchTaxonomies(string taxonomyType, [FromQuery] string searchTerm)
        {
            var searchHits = _taxonomyService.GetTaxonomySearchHits(HttpUtility.UrlDecode(taxonomyType), searchTerm);

            return Ok(searchHits);
        }
    }
}
