using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using COLID.RegistrationService.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace COLID.RegistrationService.WebApi.Controllers.V3
{
    /// <summary>
    /// Cache Manager
    /// </summary>
    [ApiController]
    [Authorize]
    [ApiVersion(Constants.API.Version.V3)]
    [Route("api/v{version:apiVersion}/cacheManager")]
    [Produces(MediaTypeNames.Application.Json)]
    public class CacheManagerController : Controller
    {
        private readonly ICacheManagerService _cacheManagerService;

        /// <summary>
        /// API endpoint to manage cache.
        /// </summary>
        /// <param name="cacheManagerService">The service to manage cache</param>
        public CacheManagerController(ICacheManagerService cacheManagerService)
        {
            _cacheManagerService = cacheManagerService;
        }

        /// <summary>
        /// Flush all cache.
        /// </summary>        
        [HttpDelete("deleteAll")]
        public IActionResult ClearCache()
        {
            _cacheManagerService.ClearCache();
            return Ok("Cache cleared");
        }
    }
}
