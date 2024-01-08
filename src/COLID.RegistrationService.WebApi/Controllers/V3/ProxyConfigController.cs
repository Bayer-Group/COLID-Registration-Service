using System.Net.Mime;
using COLID.RegistrationService.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using COLID.RegistrationService.Common.DataModels.Search;
using System;

namespace COLID.RegistrationService.WebApi.Controllers.V3
{
    /// <summary>
    /// API endpoint for proxy configuration.
    /// </summary>
    [ApiController]
    [ApiVersion(Constants.API.Version.V3)]
    [Route("api/v{version:apiVersion}/proxyConfig")]
    [Produces(MediaTypeNames.Text.Plain, MediaTypeNames.Application.Json)]
    //[ApiExplorerSettings(IgnoreApi = true)]
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
        /// <summary>
        /// Rebuild Proxy Config DynamoDb.
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> RebuildProxyConfiguration()
        {
            await _proxyConfigService.proxyConfigRebuild();

            return Ok();
        }
        /// <summary>
        /// Returns the NGINX proxy configuration for all published COLID entries.
        /// </summary>
        /// <returns>The NGINX proxy configuration for all published COLID entries</returns>
        /// <response code="200">Returns the NGINX proxy configuration for all published COLID entries</response>
        /// <response code="404">If the NGINX proxy configuration can not be generated</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpGet("v2")]
        public IActionResult GetProxyConfigurationV2()
        {
            var proxyConfig = _proxyConfigService.GetProxyConfigForNewEnvironment();

            if (string.IsNullOrWhiteSpace(proxyConfig))
            {
                return NotFound("No proxy config found.");
            }
            return Ok(proxyConfig);
        }

        /// <summary>
        /// Creates and insert proxy config in dynamodb for a search filter.
        /// </summary>
        [HttpPost("addSearchFilterProxy")]
        public IActionResult AddUpdateNginxConfigRepositoryForSearchFilter([FromBody] SearchFilterProxyDTO searchFilterProxyDTO)
        {
            _proxyConfigService.AddUpdateNginxConfigRepositoryForSearchFilter(searchFilterProxyDTO);

            return Ok();
        }

        /// <summary>
        /// Deletes the proxy config in dynamodb for a search filter
        /// </summary>
        [HttpDelete("removeSearchFilterProxy")]
        public IActionResult RemoveSearchFilterUriFromNginxConfigRepository([FromBody] string pidUri)
        {
            _proxyConfigService.RemoveSearchFilterUriFromNginxConfigRepository(pidUri, Common.Constants.PidUriTemplateSuffix.SavedSearchesParentNode);

            return Ok();
        }

        /// <summary>
        /// Deletes the proxy config in dynamodb for rrm maps.
        /// </summary>
        [HttpDelete("removeMapsProxy")]
        public IActionResult RemoveMapsUriFromNginxConfigRepository([FromBody] string pidUri)
        {
            _proxyConfigService.RemoveSearchFilterUriFromNginxConfigRepository(pidUri, Common.Constants.PidUriTemplateSuffix.RRMMapsParentNode);

            return Ok();
        }

        /// <summary>
        /// Finds pidUris that are not configured for Proxy
        /// </summary>
        [HttpGet("pidUrisWithNoProxyConfig")]
        public IActionResult FindPidUrisNotConfiguredForProxy()
        {
            return Ok(_proxyConfigService.FindPidUrisNotConfiguredForProxy());
        }

        /// <summary>
        /// Rebuild Proxy Configuration for a given PidUri
        /// </summary>
        /// <param name="pidUri"></param>
        /// <returns>true if configured successfully </returns>
        [HttpPut("RebuildProxyConfig")]
        public IActionResult RebuildProxyConfigurationByPidUri(string pidUri)
        {
            return Ok(_proxyConfigService.AddUpdateNginxConfigRepository(pidUri));
        }
    }
}
