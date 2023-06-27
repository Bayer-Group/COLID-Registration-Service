using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using COLID.IronMountainService.Common.Models;
using COLID.RegistrationService.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace COLID.RegistrationService.WebApi.Controllers.V3
{
    /// <summary>
    /// API endpoint for Iron Mountain Requests.
    /// </summary>
    [ApiController]
    [ApiVersion(Constants.API.Version.V3)]
    [Authorize]
    [Route("api/v{version:apiVersion}")]
    public class IronMountainController : ControllerBase
    {
        private readonly IIronMountainApiService _ironMountainApiService;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ironMountainApiService"></param>
        public IronMountainController(IIronMountainApiService ironMountainApiService)
        {
            _ironMountainApiService = ironMountainApiService;
        }

        /// <summary>
        /// Returns a list containing all record classes referenced in Iron Mountain
        /// </summary>
        /// <response code="200">Returns a list of IronMountain Retention Schedule Snapshot</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpGet("recordclasses")]
        public async Task<IActionResult> GetIronMountainRecordClasses()
        {
            var recordClasses = await _ironMountainApiService.GetAllRecordClasses();
            return Ok(recordClasses);
        }

        /// <summary>
        /// Retrieves PID Uris and their data categories
        /// and returns the list of relevant policies from Iron Mountain 
        /// </summary>
        /// <response code="200">List of Resources PID URIs and its Iron Mountain Policies</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPost("resourcepolicies")]
        public async Task<IActionResult> GetResourcePolicies([FromBody] ISet<IronMountainRequestDto> policyRequestValues)
        {
            var resourcePolicies = await _ironMountainApiService.GetResourcePolicies(policyRequestValues);
            return Ok(resourcePolicies);
        }
    }
}
