using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.RegistrationService.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Annotations;

namespace COLID.RegistrationService.WebApi.Controllers.V3
{
    /// <summary>
    /// API endpoint for import.
    /// </summary>
    [ApiController]
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
        /// <param name="ignoreNonMandatory"></param>
        /// <returns>Validation result and set of triples that can be uploaded to triple store. </returns>
        /// <response code="204">Successful request</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPost]
        [SwaggerResponse((int)HttpStatusCode.NoContent)]
        [SwaggerResponse((int)HttpStatusCode.InternalServerError)]
        [Route("validate")]
        public async Task<IActionResult> Validate([FromBody] List<ResourceRequestDTO> resources, bool ignoreNonMandatory = false)
        {
            var result = await _importService.ValidateResource(resources, ignoreNonMandatory);
            return Ok(result);
        }
        
        /// <summary>
        /// Upload Excel template for import
        /// </summary>   
        /// <returns> </returns>
        /// <response code="204">Successful request</response>
        /// <response code="500">If an unexpected error occurs</response>
        [HttpPost("importExcel")]
        [RequestFormLimits(MultipartBodyLengthLimit = 5243680)]  // max 5 MB (5242880) + 800 Byte buffer for request header
        [RequestSizeLimit(5243680)]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            //var inputstream = new MemoryStream();
            //file.CopyTo(inputstream);            
            //var outputStream = await _importService.ExecuteImportExcel(inputstream);
            //return File(fileContents: outputStream.ToArray(), contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ExcelFile.xlsx");
            await _importService.UploadImportExcelToS3(file);
            return NoContent();
        }
    }
}
