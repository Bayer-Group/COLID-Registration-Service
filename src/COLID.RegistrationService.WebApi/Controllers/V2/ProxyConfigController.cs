using System;
using System.Net.Mime;
using COLID.RegistrationService.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace COLID.RegistrationService.WebApi.Controllers.V2
{
    /// <summary>
    /// API endpoint for proxy configuration.
    /// </summary>
    [ApiController]
    [ApiVersion(Constants.API.Version.V1)]
    [ApiVersion(Constants.API.Version.V2)]
    [Route("api/v{version:apiVersion}/proxyConfig")]
    [Produces(MediaTypeNames.Text.Plain)]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Obsolete("A new version of this endpoint is available")]
    public class ProxyConfigController : Controller
    {
        private readonly IProxyConfigService _proxyConfigService;

        /// <summary>
        /// API endpoint for proxy configuration.
        /// </summary>
        /// <param name="proxyConfigService">The service for proxy configuration</param>
        public ProxyConfigController(IProxyConfigService proxyConfigService)
        {
            _proxyConfigService = proxyConfigService;
        }

        /// <summary>
        /// Returns the NGINX proxy configuration for all published COLID entries.
        /// </summary>
        /// <returns>The NGINX proxy configuration for all published COLID entries</returns>
        /// <response code="200">Returns the NGINX proxy configuration for all published COLID entries</response>
        /// <response code="404">If the NGINX proxy configuration can not be generated</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpGet]
        public IActionResult GetProxyConfiguration()
        {
            var proxyConfig = _proxyConfigService.GetCurrentProxyConfiguration();

            if (string.IsNullOrWhiteSpace(proxyConfig))
            {
                return NotFound("No proxy config found.");
            }

            return Ok(proxyConfig);
        }
    }
}
