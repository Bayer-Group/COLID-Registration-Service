using System.Net.Mime;
using COLID.RegistrationService.Services.Authorization.Requirements;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.WebApi.Filters;
using COLID.StatisticsLog.Type;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using COLID.Identity.Requirements;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.RegistrationService.Common.DataModel.ResourceTemplates;

namespace COLID.RegistrationService.WebApi.Controllers.V3
{
    /// <summary>
    /// API endpoint for resource templates.
    /// </summary>
    [ApiController]
    [Authorize]
    [ApiVersion(Constants.API.Version.V3)]
    [Route("api/v{version:apiVersion}")]
    [Produces(MediaTypeNames.Application.Json)]
    public class ResourceTemplateController : Controller
    {
        private readonly IResourceTemplateService _resourceTemplateService;

        /// <summary>
        /// API endpoint for resource templates.
        /// </summary>
        /// <param name="resourceTemplateService">The service for resource templates</param>
        public ResourceTemplateController(IResourceTemplateService resourceTemplateService)
        {
            _resourceTemplateService = resourceTemplateService;
        }

        /// <summary>
        /// Returns a list of all created Resource Templates.
        /// </summary>
        /// <returns>A list of all created Resource Templates</returns>
        /// <response code="200">Returns the list of Resource Templates</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpGet]
        [Route("resourceTemplateList")]
        public IActionResult GetResourceTemplates()
        {
            var resourceTemplates = _resourceTemplateService.GetEntities(null);

            return Ok(resourceTemplates);
        }

        /// <summary>
        /// Returns the Resource Template of the given Id.
        /// </summary>
        /// <param name="id">The Id of a resource Template.</param>
        /// <returns>A ResourceTemplate</returns>
        /// <response code="200">Returns the Resource Template of the given Id</response>
        /// <response code="404">If no Resource Template exists with the given Id</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpGet]
        [ValidateActionParameters]
        [Route("resourceTemplateById")]
        public IActionResult GetResourceTemplateById([FromQuery] string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest("No valid id is given.");
            }

            var resourceTemplate = _resourceTemplateService.GetEntity(id);

            return Ok(resourceTemplate);
        }

        /// <summary>
        /// Creates a resource Template.
        /// </summary>
        /// <param name="resourceTemplate">The new resource Template to create</param>
        /// <returns>A newly created resource Template</returns>
        /// <response code="201">Returns the newly created resource Template</response>
        /// <response code="400">If the resource Template is invalid</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPost]
        [ValidateActionParameters]
        [Route("resourceTemplate")]
        [Authorize(Policy = nameof(AdministratorRequirement))]
        [Log(LogType.AuditTrail)]
        public async Task<IActionResult> CreateResourceTemplate([FromBody] ResourceTemplateRequestDTO resourceTemplate)
        {
            var newResourceTemplate = await _resourceTemplateService.CreateEntity(resourceTemplate);

            if (!newResourceTemplate.ValidationResult.Conforms && newResourceTemplate.ValidationResult.Severity != ValidationResultSeverity.Info)
            {
                return BadRequest(newResourceTemplate);
            }

            return Created("/api/resourceTemplate/" + newResourceTemplate.Entity.Id, newResourceTemplate);
        }

        /// <summary>
        /// Edits the Resource Template with the given Id and sets the given values.
        /// </summary>
        /// <param name="id">The Id of the Resource Template to edit</param>
        /// <param name="resourceTemplate">The new values for the existing Resource Template</param>
        /// <returns>A status code</returns>
        /// <response code="200">Returns status code only</response>
        /// <response code="400">If the given Id or Resource Template information is invalid and do not match</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPut]
        [ValidateActionParameters]
        [Route("resourceTemplate")]
        [Authorize(Policy = nameof(AdministratorRequirement))]
        [Log(LogType.AuditTrail)]
        public IActionResult EditresourceTemplate([FromQuery] string id, [FromBody] ResourceTemplateRequestDTO resourceTemplate)
        {
            var newResourceTemplate = _resourceTemplateService.EditEntity(id, resourceTemplate);

            if (!newResourceTemplate.ValidationResult.Conforms && newResourceTemplate.ValidationResult.Severity != ValidationResultSeverity.Info)
            {
                return BadRequest(newResourceTemplate);
            }

            return Ok();
        }

        /// <summary>
        /// By a given id, the resource template will be deleted or set as deprecated.
        /// If a consumer group references the resource template, it can't be deleted,
        /// </summary>
        /// <param name="id">The Id of the resource Template to be deleted.</param>
        /// <returns>A status code</returns>
        /// <response code="200">Returns a status code with a corresponding error message</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpDelete]
        [ValidateActionParameters]
        [Route("resourceTemplate")]
        [Authorize(Policy = nameof(AdministratorRequirement))]
        [Log(LogType.AuditTrail)]
        public IActionResult DeleteResourceTemplate([FromQuery] string id)
        {
            _resourceTemplateService.DeleteResourceTemplate(id);

            return Ok();
        }
    }
}
