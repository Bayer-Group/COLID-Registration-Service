using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Mime;
using System.Threading;
using COLID.RegistrationService.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using COLID.AWS.DataModels;
using COLID.RegistrationService.Common.DataModel.Search;
using Microsoft.AspNetCore.Http;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;
using Newtonsoft.Json.Linq;

namespace COLID.RegistrationService.WebApi.Controllers.V3
{
    /// <summary>
    /// API endpoint for exports.
    /// </summary>
    [Route("api/v{version:apiVersion}")]
    [ApiController]
    [Authorize]
    [ApiVersion(Constants.API.Version.V3)]
    public class ExportController : Controller
    {
        private readonly IExportService _exportService;

        /// <summary>
        /// API endpoint for exports.
        /// </summary>
        /// <param name="exportService">The service for exports</param>
        public ExportController(
            IExportService exportService
            )
        {
            _exportService = exportService;
        }

        /// <summary>
        /// Export result of search request
        /// </summary>
        /// <param name="exportRequest"></param>
        /// <returns>A status code. </returns>
        /// <response code="204">Successful request</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPost]
        [SwaggerResponse((int)HttpStatusCode.NoContent)]
        [SwaggerResponse((int)HttpStatusCode.InternalServerError)]
        [Route("export")]
        public IActionResult Export([FromBody] ExportRequestDto exportRequest)
        {
            Thread background = new Thread(() => _exportService.Export(exportRequest));
            background.Start();

            return NoContent();
        }        
    }
}
