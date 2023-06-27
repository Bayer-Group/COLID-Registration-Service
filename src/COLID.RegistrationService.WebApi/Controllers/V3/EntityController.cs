using System.Net.Mime;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.WebApi.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    public class EntityController : Controller
    {
        private readonly IEntityService _entityService;

        /// <summary>
        /// API endpoint for entities.
        /// </summary>
        /// <param name="entityService">The service for entities</param>
        public EntityController(IEntityService entityService)
        {
            _entityService = entityService;
        }

        /// <summary>
        /// Returns a list of all created entities.
        /// </summary>
        /// <returns>A list of all created entities</returns>
        /// <response code="200">Returns the list of entities. If there are no entities, an empty list is returned.</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpGet]
        [ValidateActionParameters]
        [Route("entityList")]
        public IActionResult GetEntities([FromQuery] EntitySearch entitySearch)
        {
            var entities = _entityService.GetEntities(entitySearch);

            return Ok(entities);
        }

        /// <summary>
        /// Returns the entity of the given Id.
        /// </summary>
        /// <param name="id">The Id of a entity.</param>
        /// <returns>A entity</returns>
        /// <response code="200">Returns the entity of the given Id</response>
        /// <response code="404">If no entity exists with the given Id</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpGet]
        [ValidateActionParameters]
        [Route("entity")]
        public IActionResult GetEntityById([FromQuery] string id)
        {
            var entity = _entityService.GetEntity(id);

            if (entity == null)
            {
                return NotFound("No entity for given Id: " + id);
            };

            return Ok(entity);
        }
    }
}
