using System.Net.Mime;
using COLID.RegistrationService.Services.Authorization.Requirements;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.WebApi.Filters;
using COLID.StatisticsLog.Type;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using COLID.Identity.Requirements;
using COLID.RegistrationService.Common.DataModel.PidUriTemplates;
using COLID.Graph.Metadata.DataModels.Validation;

namespace COLID.RegistrationService.WebApi.Controllers.V3
{
    /// <summary>
    /// API endpoint for pid uri templates.
    /// </summary>
    [ApiController]
    [Authorize]
    [ApiVersion(Constants.API.Version.V3)]
    [Route("api/v{version:apiVersion}")]
    [Produces(MediaTypeNames.Application.Json)]
    public class PidUriTemplateController : Controller
    {
        private readonly IPidUriTemplateService _pidUriTemplateService;

        /// <summary>
        /// API endpoint for pid uri templates.
        /// </summary>
        /// <param name="pidUriTemplateService">The service for pid uri templates</param>
        public PidUriTemplateController(IPidUriTemplateService pidUriTemplateService)
        {
            _pidUriTemplateService = pidUriTemplateService;
        }

        /// <summary>
        /// Returns a list of all created PidUri Templates.
        /// </summary>
        /// <returns>A list of all created pidUri Templates</returns>
        /// <response code="200">Returns the list of PidUri Templates</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpGet]
        [Route("pidUriTemplateList")]
        public IActionResult GetPidUriTemplates()
        {
            var pidUriTemplates = _pidUriTemplateService.GetEntities(null);

            return Ok(pidUriTemplates);
        }

        /// <summary>
        /// Returns the pidUri Template of the given Id.
        /// </summary>
        /// <param name="id">The Id of a pidUri Template.</param>
        /// <returns>A PidUriTemplate</returns>
        /// <response code="200">Returns the pidUri Template of the given Id</response>
        /// <response code="404">If no pidUri Template exists with the given Id</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpGet]
        [ValidateActionParameters]
        [Route("pidUriTemplate")]
        public IActionResult GetPidUriTemplateById([FromQuery] string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest("No valid id is given.");
            }

            var pidUriTemplate = _pidUriTemplateService.GetEntity(id);

            return Ok(pidUriTemplate);
        }

        /// <summary>
        /// Creates a pidUri Template.
        /// </summary>
        /// <param name="pidUriTemplate">The new pidUri Template to create</param>
        /// <returns>A newly created pidUri Template</returns>
        /// <response code="201">Returns the newly created pidUri Template</response>
        /// <response code="400">If the pidUri Template is invalid</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPost]
        [ValidateActionParameters]
        [Route("pidUriTemplate")]
        [Authorize(Policy = nameof(AdministratorRequirement))]
        [Log(LogType.AuditTrail)]
        public async Task<IActionResult> CreatePidUriTemplate([FromBody] PidUriTemplateRequestDTO pidUriTemplate)
        {
            var newPidUriTemplate = await _pidUriTemplateService.CreateEntity(pidUriTemplate);

            if (!newPidUriTemplate.ValidationResult.Conforms && newPidUriTemplate.ValidationResult.Severity != ValidationResultSeverity.Info)
            {
                return BadRequest(newPidUriTemplate);
            }

            return Created("/api/pidUriTemplate/" + newPidUriTemplate.Entity.Id, newPidUriTemplate);
        }

        /// <summary>
        /// Edits the pidUri Template with the given Id and sets the given values.
        /// </summary>
        /// <param name="id">The Id of the pidUri Template to edit</param>
        /// <param name="pidUriTemplate">The new values for the existing pidUri Template</param>
        /// <returns>A status code</returns>
        /// <response code="200">Returns status code only</response>
        /// <response code="400">If the given Id or pidUri Template information is invalid and do not match</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPut]
        [ValidateActionParameters]
        [Route("pidUriTemplate")]
        [Authorize(Policy = nameof(AdministratorRequirement))]
        [Log(LogType.AuditTrail)]
        public IActionResult EditPidUriTemplate([FromQuery] string id, [FromBody] PidUriTemplateRequestDTO pidUriTemplate)
        {
            var newPidUriTemplate = _pidUriTemplateService.EditEntity(id, pidUriTemplate);

            if (!newPidUriTemplate.ValidationResult.Conforms && newPidUriTemplate.ValidationResult.Severity != ValidationResultSeverity.Info)
            {
                return BadRequest(newPidUriTemplate);
            }

            return Ok();
        }

        /// <summary>
        /// By a given id, the pid uri template will be deleted or set as deprecated.
        /// If a permanent identifier references the pid uri template, the status is set to deprecated,
        /// otherwise the pid uri template will be deleted.
        /// </summary>
        /// <param name="id">The Id of the pidUri Template to set as deprecated or delete.</param>
        /// <returns>A status code</returns>
        /// <response code="200">Returns a status code with a corresponding error message</response>
        /// <response code="409">If a template has a reference to a consumer group</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpDelete]
        [ValidateActionParameters]
        [Route("pidUriTemplate")]
        [Authorize(Policy = nameof(AdministratorRequirement))]
        [Log(LogType.AuditTrail)]
        public IActionResult DeleteOrDeprecatePidUriTemplate([FromQuery] string id)
        {
            _pidUriTemplateService.DeleteOrDeprecatePidUriTemplate(id);

            return Ok();
        }

        /// <summary>
        /// Reactivate a pidUri Template.
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="id">The Id of the pidUri Template to reactivate.</param>
        /// <returns>A status code</returns>
        /// <response code="200">Returns a status code with a corresponding error message</response>
        /// <response code="400">If the pid uri template cannot be reactivated</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPost]
        [ValidateActionParameters]
        [Route("pidUriTemplate/reactivate")]
        [Authorize(Policy = nameof(AdministratorRequirement))]
        [Log(LogType.AuditTrail)]
        public IActionResult ReactivatePidUriTemplate([FromQuery] string id)
        {
            _pidUriTemplateService.ReactivateTemplate(id);

            return Ok();
        }
    }
}
