using System;
using System.Net.Mime;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.WebApi.Controllers.V2.Filter;
using COLID.RegistrationService.WebApi.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace COLID.RegistrationService.WebApi.Controllers.V2
{
    [ApiController]
    [Authorize]
    [ApiVersion(Constants.API.Version.V1)]
    [ApiVersion(Constants.API.Version.V2)]
    [Route("api/v{version:apiVersion}")]
    [Produces(MediaTypeNames.Application.Json)]
    [TransformIdPropertyResponseFilter]
    [Obsolete]
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
        /// Returns the entity of the given subject.
        /// </summary>
        /// <param name="subject">The subject of a entity.</param>
        /// <returns>A entity</returns>
        /// <response code="200">Returns the entity of the given subject</response>
        /// <response code="404">If no entity exists with the given subject</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpGet]
        [ValidateActionParameters]
        [Route("entity")]
        public IActionResult GetEntityById([FromQuery] string subject)
        {
            var entity = _entityService.GetEntity(subject);

            if (entity == null)
            {
                return NotFound("No entity for given subject: " + subject);
            };

            return Ok(entity);
        }
    }
}
