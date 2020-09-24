using System.Net.Mime;
using COLID.RegistrationService.WebApi.Filters;
using COLID.StatisticsLog.DataModel;
using COLID.StatisticsLog.Services;
using Microsoft.AspNetCore.Mvc;
using System;

namespace COLID.RegistrationService.WebApi.Controllers.V2
{
    /// <summary>
    /// API endpoint for logging.
    /// </summary>
    [ApiController]
    [ApiVersion(Constants.API.Version.V1)]
    [ApiVersion(Constants.API.Version.V2)]
    [Route("api/v{version:apiVersion}/log")]
    [Produces(MediaTypeNames.Application.Json)]
    [Obsolete]
    public class LogController : Controller
    {
        private readonly IGeneralLogService _logService;

        /// <summary>
        /// API endpoint for logging.
        /// </summary>
        public LogController(IGeneralLogService logService)
        {
            _logService = logService;
        }

        /// <summary>
        /// Logs the given entry to the database used for logging.
        /// </summary>
        /// <returns>A status code</returns>
        /// <response code="200">Returns true, if the log as been processed. Otherwise false</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPost]
        [ValidateActionParameters]
        [Route("{logLevel}")]
        public IActionResult LogMessage([FromRoute] int logLevel, [FromBody] LogEntry logEntry)
        {
            _logService.Log(logEntry, (Serilog.Events.LogEventLevel)logLevel);

            return Ok();
        }
    }
}
