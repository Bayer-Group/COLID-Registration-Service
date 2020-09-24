using System.Net.Mime;
using COLID.Common.DataModel.Attributes;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.WebApi.Filters;
using COLID.StatisticsLog.Type;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using COLID.Identity.Requirements;
using COLID.Graph.Metadata.Services;
using COLID.Graph.Metadata.DataModels.MetadataGraphConfiguration;

namespace COLID.RegistrationService.WebApi.Controllers.V3
{
    [ApiController]
    [Authorize(Policy = nameof(SuperadministratorRequirement))]
    [ApiVersion(Constants.API.Version.V3)]
    [Route("api/v{version:apiVersion}/metadataGraphConfiguration")]
    [Produces(MediaTypeNames.Application.Json)]
    public class MetadataGraphConfigurationController : Controller
    {
        private readonly IMetadataGraphConfigurationService _MetadataGraphConfigurationService;
        private readonly IReindexingService _indexingService;

        /// <summary>
        /// API endpoint for metadata graph configurations.
        /// </summary>
        /// <param name="MetadataGraphConfigurationService">The service for metadata graph configurations</param>
        public MetadataGraphConfigurationController(
            IMetadataGraphConfigurationService MetadataGraphConfigurationService,
            IReindexingService indexingService)
        {
            _MetadataGraphConfigurationService = MetadataGraphConfigurationService;
            _indexingService = indexingService;
        }

        /// <summary>
        /// Creates a new metadata graph configuration and sets the currently active as a historic version.
        /// </summary>
        /// <param name="metadataGraphConfiguration">The new metadata graph configuration to create</param>
        /// <returns>A newly created metadata graph configuration</returns>
        /// <response code="201">Returns the newly created metadata graph configuration</response>
        /// <response code="400">If the metadata graph configuration is invalid</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPost]
        [ValidateActionParameters]
        [Log(LogType.AuditTrail)]
        public async Task<IActionResult> CreateMetadataGraphConfiguration([FromBody] MetadataGraphConfigurationRequestDTO metadataGraphConfiguration)
        {
            var result = await _MetadataGraphConfigurationService.CreateEntity(metadataGraphConfiguration);
            await _indexingService.Reindex();

            return Created(new System.Uri($"/api/metadataGraphConfiguration"), result);
        }

        /// <summary>
        /// Returns the latest metadata graph configuration, which is currently in use.
        /// </summary>
        /// <returns>The latest metadata graph configuration</returns>
        /// <response code="200">Returns the latest metadata graph configuration</response>
        /// <response code="404">If no metadata graph configuration exists</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpGet]
        [ValidateActionParameters]
        public IActionResult GetLatestMetadataGraphConfiguration([FromQuery, NotRequired] string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return Ok(_MetadataGraphConfigurationService.GetLatestConfiguration());
            }

            return Ok(_MetadataGraphConfigurationService.GetEntity(id));
        }

        /// <summary>
        /// Determine all historic metdata graph configurations and returns overview information of them.
        /// </summary>
        /// <returns>An overview list of historic metadata graph configurations</returns>
        [HttpGet]
        [ValidateActionParameters]
        [Route("history")]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(string), 400)]
        public IActionResult GetHistoricOverviewList()
        {
            var result = _MetadataGraphConfigurationService.GetConfigurationOverview();
            return Ok(result);
        }
    }
}
