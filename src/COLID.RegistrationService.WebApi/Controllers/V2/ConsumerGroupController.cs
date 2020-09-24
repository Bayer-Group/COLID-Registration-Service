using System;
using System.Net.Mime;
using System.Threading.Tasks;
using COLID.StatisticsLog.Type;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.WebApi.Controllers.V2.Filter;
using COLID.RegistrationService.WebApi.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using COLID.Identity.Requirements;
using COLID.Graph.TripleStore.DataModels.ConsumerGroups;

namespace COLID.RegistrationService.WebApi.Controllers.V2
{
    /// <summary>
    /// API endpoint for consumer groups.
    /// </summary>
    [ApiController]
    [Authorize]
    [ApiVersion(Constants.API.Version.V1)]
    [ApiVersion(Constants.API.Version.V2)]
    [Route("api/v{version:apiVersion}")]
    [Produces(MediaTypeNames.Application.Json)]
    [TransformIdPropertyResponseFilter]
    [Obsolete]
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
        [Route("consumerGroupList")]
        public IActionResult GetConsumerGroups()
        {
            var consumerGroups = _consumerGroupService.GetEntities(null);

            return Ok(consumerGroups);
        }

        /// <summary>
        /// Returns the consumer group of the given subject.
        /// </summary>
        /// <param name="subject">The subject of a consumer group.</param>
        /// <returns>A consumer group</returns>
        /// <response code="200">Returns the consumer group of the given subject</response>
        /// <response code="404">If no consumer group exists with the given subject</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpGet]
        [ValidateActionParameters]
        [Route("consumerGroup")]
        public IActionResult GetConsumerGroupById([FromQuery] string subject)
        {
            var consumerGroup = _consumerGroupService.GetEntity(subject);

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

            return Created("/api/consumerGroup?subject=" + result.Entity.Id, result);
        }

        /// <summary>
        /// Edits the consumer group with the given subject and sets the given values.
        /// </summary>
        /// <param name="subject">The subject of the consumer group to edit.</param>
        /// <param name="consumerGroup">All values for the existing consumer group</param>
        /// <returns>A status code</returns>
        /// <response code="200">Returns status code only</response>
        /// <response code="400">If the given subject or consumer group information is invalid and do not match</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPut]
        [ValidateActionParameters]
        [Route("consumerGroup")]
        [Authorize(Policy = nameof(AdministratorRequirement))]
        [Log(LogType.AuditTrail)]
        public IActionResult EditConsumerGroup([FromQuery] string subject, [FromBody] ConsumerGroupRequestDTO consumerGroup)
        {
            var newConsumerGroup = _consumerGroupService.EditEntity(subject, consumerGroup);

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
        public IActionResult DeleteConsumerGroup([FromQuery] string subject)
        {
            _consumerGroupService.DeleteOrDeprecateConsumerGroup(subject);

            return Ok();
        }
    }
}
