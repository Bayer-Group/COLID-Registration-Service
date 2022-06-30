using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.RegistrationService.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace COLID.RegistrationService.WebApi.Controllers.V3
{
    /// <summary>
    /// API endpoint for import.
    /// </summary>
    [ApiVersion(Constants.API.Version.V3)]
    [Route("api/v{version:apiVersion}")]
    [Authorize]
    public class ImportController : Controller
    {
        private readonly IImportService _importService;

        /// <summary>
        /// API endpoint for resources.
        /// </summary>
        /// <param name="importService">The service for importing resources</param>        
        public ImportController(IImportService importService)
        {
            _importService = importService;
            
        }

        /// <summary>
        /// Validate resources for bulk upload
        /// </summary>
        /// <param name="resources"></param>
        /// <returns>Validation result and set of triples that can be uploaded to triple store. </returns>
        /// <response code="204">Successful request</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPost]
        [SwaggerResponse((int)HttpStatusCode.NoContent)]
        [SwaggerResponse((int)HttpStatusCode.InternalServerError)]
        [Route("validate")]
        public async Task<IActionResult> Validate([FromBody] List<ResourceRequestDTO> resources)
        {
            var result = await _importService.ValidateResource(resources);
            return Ok(result);
        }

        /// <summary>
        /// Clean up all messages from SQS-bulk
        /// </summary>
        /// <returns>count of messages deleted</returns>
        [HttpPut]
        [SwaggerResponse((int)HttpStatusCode.NoContent)]
        [SwaggerResponse((int)HttpStatusCode.InternalServerError)]
        [Route("cleanupSQS")]
        public async Task<IActionResult> CleanUpBulkUploadSQSQueue()
        {
            var result = await _importService.CleanUpBulkUploadSQSQueue();
            return Ok(result);
        }

        /// <summary>
        /// Links resources available is SQS-bulk and stores result in SQS-result
        /// </summary>   
        /// <returns>Linking result. </returns>
        /// <response code="204">Successful request</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPost]
        [SwaggerResponse((int)HttpStatusCode.NoContent)]
        [SwaggerResponse((int)HttpStatusCode.InternalServerError)]
        [Route("startlinking")]
        public async Task<IActionResult> StartLinking()
        {
            var result = await _importService.StartLinkingResources();
            return Ok(result);
        }
    }
}
