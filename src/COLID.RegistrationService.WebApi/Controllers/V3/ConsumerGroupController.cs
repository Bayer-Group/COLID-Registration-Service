using System.Net.Mime;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.WebApi.Filters;
using COLID.StatisticsLog.Type;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using COLID.Identity.Requirements;
using COLID.Graph.TripleStore.DataModels.ConsumerGroups;

namespace COLID.RegistrationService.WebApi.Controllers.V3
{
    /// <summary>
    /// API endpoint for consumer groups.
    /// </summary>
    [ApiController]
    [Authorize]
    [ApiVersion(Constants.API.Version.V3)]
    [Route("api/v{version:apiVersion}")]
    [Produces(MediaTypeNames.Application.Json)]
    public class ConsumerGroupController : Controller
    {
        private readonly IConsumerGroupService _consumerGroupService;

        /// <summary>
        /// API endpoint for consumer groups.
        /// </summary>
        /// <param name="consumerGroupService">The service for consumer groups</param>
        public ConsumerGroupController(IConsumerGroupService consumerGroupService)
        {
            _consumerGroupService = consumerGroupService;
        }

        /// <summary>
        /// Returns a list of all created consumer groups.
        /// </summary>
        /// <returns>A list of all created consumer groups</returns>
        /// <response code="200">Returns the list of consumer groups. If there are no consumer groups, an empty list is returned.</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpGet]
        [Authorize(Policy = nameof(AdministratorRequirement))]
        [Route("consumerGroupList")]
        public IActionResult GetConsumerGroups()
        {
            var consumerGroups = _consumerGroupService.GetEntities(null);
            return Ok(consumerGroups);
        }

        /// <summary>
        /// Returns a list of all active created consumer groups.
        /// </summary>
        /// <returns>A list of all active created consumer groups</returns>
        /// <response code="200">Returns the list of active consumer groups. If there are no consumer groups, an empty list is returned.</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpGet]
        [Route("consumerGroupList/active")]
        public IActionResult GetActiveConsumerGroups()
        {
            var consumerGroups = _consumerGroupService.GetActiveEntities();
            return Ok(consumerGroups);
        }

        /// <summary>
        /// Returns the consumer group of the given Id.
        /// </summary>
        /// <param name="id">The Id of a consumer group.</param>
        /// <returns>A consumer group</returns>
        /// <response code="200">Returns the consumer group of the given Id</response>
        /// <response code="404">If no consumer group exists with the given Id</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpGet]
        [ValidateActionParameters]
        [Route("consumerGroup")]
        public IActionResult GetConsumerGroupById([FromQuery] string id)
        {
            var consumerGroup = _consumerGroupService.GetEntity(id);
            return Ok(consumerGroup);
        }

        /// <summary>
        /// Creates a consumer group.
        /// </summary>
        /// <param name="consumerGroup">The new consumer group to create</param>
        /// <returns>A newly created consumer group</returns>
        /// <response code="201">Returns the newly created consumer group</response>
        /// <response code="400">If the consumerGroup is invalid</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPost]
        [ValidateActionParameters]
        [Route("consumerGroup")]
        [Authorize(Policy = nameof(AdministratorRequirement))]
        [Log(LogType.AuditTrail)]
        public async Task<IActionResult> CreateConsumerGroup([FromBody] ConsumerGroupRequestDTO consumerGroup)
        {
            // Create consumer group
            var result = await _consumerGroupService.CreateEntity(consumerGroup);

            return Created("/api/consumerGroup?id=" + result.Entity.Id, result);
        }

        /// <summary>
        /// Edits the consumer group with the given Id and sets the given values.
        /// </summary>
        /// <param name="id">The Id of the consumer group to edit.</param>
        /// <param name="consumerGroup">All values for the existing consumer group</param>
        /// <returns>A status code</returns>
        /// <response code="200">Returns status code only</response>
        /// <response code="400">If the given Id or consumer group information is invalid and do not match</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPut]
        [ValidateActionParameters]
        [Route("consumerGroup")]
        [Authorize(Policy = nameof(AdministratorRequirement))]
        [Log(LogType.AuditTrail)]
        public IActionResult EditConsumerGroup([FromQuery] string id, [FromBody] ConsumerGroupRequestDTO consumerGroup)
        {
            var newConsumerGroup = _consumerGroupService.EditEntity(id, consumerGroup);
            return Ok(newConsumerGroup);
        }

        /// <summary>
        /// By a given id, the consumer group will be deleted or set as deprecated.
        /// If a colid entry references the consumer group, the status is set to deprecated,
        /// otherwise the consumer group will be deleted.
        /// </summary>
        /// <param name="id">The Id of the consumer group to delete or deactivate.</param>
        /// <returns>A status code</returns>
        /// <response code="200">Returns status code only</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpDelete]
        [ValidateActionParameters]
        [Route("consumerGroup")]
        [Authorize(Policy = nameof(AdministratorRequirement))]
        [Log(LogType.AuditTrail)]
        public IActionResult DeleteOrDeprecateConsumerGroup([FromQuery] string id)
        {
            _consumerGroupService.DeleteOrDeprecateConsumerGroup(id);
            return Ok();
        }

        /// <summary>
        /// Reactivate a consumer group.
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="id">The Id of the consumer group to reactivate.</param>
        /// <returns>A status code</returns>
        /// <response code="200">Returns a status code with a corresponding error message</response>
        /// <response code="400">If the consumer group cannot be reactivated</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPost]
        [ValidateActionParameters]
        [Route("consumerGroup/reactivate")]
        [Authorize(Policy = nameof(AdministratorRequirement))]
        [Log(LogType.AuditTrail)]
        public IActionResult ReactivateConsumerGroup([FromQuery] string id)
        {
            _consumerGroupService.ReactivateConsumerGroup(id);
            return Ok();
        }
    }
}
