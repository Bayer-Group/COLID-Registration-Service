using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Mime;
using COLID.RegistrationService.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using COLID.AWS.DataModels;
using Microsoft.AspNetCore.Http;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace COLID.RegistrationService.WebApi.Controllers.V3
{
    /// <summary>
    /// API endpoint for attachments.
    /// </summary>
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = false)]
    [Authorize]
    [ApiVersion(Constants.API.Version.V3)]
    [Route("api/v{version:apiVersion}")]
    [Produces(MediaTypeNames.Application.Json)]
    public class AttachmentController : Controller
    {
        private readonly IAttachmentService _attachmentService;

        private const string _mimetypeOctetStream = "application/octet-stream";

        /// <summary>
        /// API endpoint for attachments.
        /// </summary>
        /// <param name="attachmentService">The service for attachments</param>
        public AttachmentController(IAttachmentService attachmentService)
        {
            _attachmentService = attachmentService;
        }

        /// <summary>
        /// Get a file, defined by it's filename and the unique guid.
        /// </summary>
        /// <param name="guid">unique identifier of the file</param>
        /// <param name="fileName">the filename to use</param>
        /// <returns></returns>
        [HttpGet("attachment")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(System.IO.File))]
        [SwaggerResponseExample((int)HttpStatusCode.OK, typeof(AmazonS3FileDownloadDto))]
        [SwaggerResponse((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> GetAttachment([FromQuery] Guid guid, [FromQuery] string fileName)
        {
            var s3FileDto = await _attachmentService.GetAttachment(guid, fileName);
            var file = File(s3FileDto.Stream, _mimetypeOctetStream, fileName);
            return file;
        }

        /// <summary>
        /// Upload a given file with a maximum size of 5MB.
        /// </summary>
        /// <param name="file">the file to upload</param>
        /// <param name="comment">(optional) the comment to add</param>
        /// <returns></returns>
        [HttpPost("attachment")]
        [RequestFormLimits(MultipartBodyLengthLimit = 5243680)]  // max 5 MB (5242880) + 800 Byte buffer for request header
        [RequestSizeLimit(5243680)]
        public async Task<IActionResult> UploadAttachment(IFormFile file, [FromQuery] [AllowNull] string comment = "")
        {
            var s3FileInfo = await _attachmentService.UploadAttachment(file, comment);
            return Ok(s3FileInfo);
        }

        /// <summary>
        /// Delete a file, defined by it's filename and the unique guid.
        /// </summary>
        /// <param name="guid">unique identifier of the file</param>
        /// <param name="fileName">the filename to use</param>
        /// <returns></returns>
        [HttpDelete("attachment")]
        public async Task<IActionResult> DeleteAttachment([FromQuery] Guid guid, [FromQuery] string fileName)
        {
            await _attachmentService.DeleteAttachment(guid, fileName);
            return NoContent();
        }
    }
}
