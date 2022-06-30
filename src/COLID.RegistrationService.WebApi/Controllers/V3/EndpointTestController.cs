using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using COLID.RegistrationService.Services.Interface;

namespace COLID.RegistrationService.WebApi.Controllers.V3
{
    /// <summary>
    /// API endpoint for to test distribution endpoint content
    /// </summary>
    [ApiController]
    [Authorize]
    [ApiVersion(Constants.API.Version.V3)]
    [Route("api/v{version:apiVersion}/EndpointTest")]
    [Produces(MediaTypeNames.Application.Json)]
    public class EndpointTestController : Controller
    {
        private IEndpointTestService _endpointTestService;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="endpointTestService"></param>
        public EndpointTestController(IEndpointTestService endpointTestService)
        {
            _endpointTestService = endpointTestService;
        }

        /// <summary>
        /// Test the endpoints and notify the users
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult TestEndpoints()
        {
            _endpointTestService.PushEndpointsInQueue();
            return Ok();
        }
    }
}
