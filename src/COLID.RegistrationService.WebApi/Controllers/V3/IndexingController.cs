using System.Net.Mime;
using COLID.RegistrationService.Services.Interface;
using COLID.StatisticsLog.Type;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using COLID.Identity.Requirements;

namespace COLID.RegistrationService.WebApi.Controllers.V3
{
    /// <summary>
    /// API endpoint for logging.
    /// </summary>
    [ApiController]
    [Authorize(Policy = nameof(SuperadministratorRequirement))]
    [ApiVersion(Constants.API.Version.V3)]
    [Route("api/v{version:apiVersion}/reindex")]
    [Produces(MediaTypeNames.Application.Json)]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class IndexingController : Controller
    {
        private readonly IReindexingService _indexingService;

        /// <summary>
        /// API endpoint for logging.
        /// </summary>
        public IndexingController(IReindexingService ReindexingService)
        {
            _indexingService = ReindexingService;
        }

        /// <summary>
        /// Start the reindexing and publish all resources to data marketplace
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <returns>A status code</returns>
        /// <response code="200">Returns true, if the reindexing as been started. Otherwise false</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPost("start")]
        [Log(LogType.AuditTrail)]
        public async Task<IActionResult> StartReindexing()
        {
            await _indexingService.Reindex();

            return Ok();
        }
    }
}
