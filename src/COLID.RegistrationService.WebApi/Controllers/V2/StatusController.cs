using System;
using System.Net.Mime;
using COLID.RegistrationService.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace COLID.RegistrationService.WebApi.Controllers.V2
{
    /// <summary>
    /// 
    /// </summary>
    [ApiController]
    [Authorize]
    [ApiVersion(Constants.API.Version.V1)]
    [ApiVersion(Constants.API.Version.V2)]
    [Route("api/v{version:apiVersion}/status")]
    [Produces(MediaTypeNames.Application.Json)]
    [Obsolete("A new version of this endpoint is available")]
    public class StatusController : Controller
    {
        private readonly IStatusService _statusService;

        /// <summary>
        /// API endpoint for status information.
        /// </summary>
        /// <param name="statusService">The service for status information</param>
        public StatusController(IStatusService statusService)
        {
            _statusService = statusService;
        }

        /// <summary>
        /// Returns the status with build information of the running web api.
        /// </summary>
        /// <response code="200">Returns the status of the build</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpGet]
        public IActionResult GetBuildInformation()
        {
            return Ok(_statusService.GetBuildInformation());
        }

    }
}
