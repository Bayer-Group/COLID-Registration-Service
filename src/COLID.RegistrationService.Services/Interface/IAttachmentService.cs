using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using COLID.AWS.DataModels;
using COLID.RegistrationService.Common.DataModels.Attachment;
using Microsoft.AspNetCore.Http;

namespace COLID.RegistrationService.Services.Interface
{
    /// <summary>
    /// This service is responsible for all file attachment operations, that are related or assigned to
    /// a single resource.
    /// </summary>
    public interface IAttachmentService
    {
        /// <summary>
        /// Download the attachment from the configured file storage and returns the file as
        /// a stream with additional information.
        /// </summary>
        /// <param name="guid">the unique identifier for the file</param>
        /// <param name="fileName">the filename to use</param>
        /// <returns></returns>
        public Task<AmazonS3FileDownloadDto> GetAttachment(Guid id, string fileName);

        /// <summary>
        /// Download the attachment from the configured file storage and returns the file as
        /// a stream with additional information.
        /// </summary>
        /// <param name="id">the id (absolute url) to the file</param>
        public Task<AmazonS3FileDownloadDto> GetAttachment(string id);

        /// <summary>
        /// <inheritdoc cref="UploadAttachment(IFormFile,Guid,string)"/>
        /// </summary>
        /// <param name="file">the file to upload</param>
        /// <param name="comment">(optional) comment for the attachment</param>
        /// <returns></returns>
        public Task<AttachmentDto> UploadAttachment(IFormFile file, string comment);

        /// <summary>
        /// Adds an attachment to the configured file storage (file add!).
        /// The created key will also use a unique guid in order to create separate
        /// folders for each attachment. Each filename will be encoded.
        /// </summary>
        /// <example>
        /// The following example will lighten up this behaviour:
        /// - Filename: Test file+123.jpg
        /// - Guid: 749A4D55-9651-4FB2-96A7-0A9F24180455
        ///
        /// The result key will be <code>749A4D55-9651-4FB2-96A7-0A9F24180455/Test_file_123.jpg</code>
        /// </example>
        /// <param name="file">the file to upload</param>
        /// <param name="guid">the unique identifier for the file</param>
        /// <param name="comment">(optional) comment for the attachment</param>
        /// <returns></returns>
        public Task<AttachmentDto> UploadAttachment(IFormFile file, Guid id, string comment);

        /// <summary>
        /// Deletes an attachment from the configured file storage (file deletion!). In
        /// addition to that, the stored meta information within the graph will be removed too.
        /// </summary>
        /// <param name="guid">the unique identifier for the file</param>
        /// <param name="fileName">the filename to use</param>
        public Task DeleteAttachment(Guid id, string fileName);

        /// <summary>
        /// Deletes an attachment from the configured file storage (file deletion!). In
        /// addition to that, the stored meta information within the graph will be removed too.
        /// </summary>
        /// <param name="id">the id (absolute url) to the file</param>
        public Task DeleteAttachment(string id);

        public Task DeleteAttachments(ICollection<dynamic> ids);

        /// <summary>
        /// Check if a given id (uri to file) exists and returns the result.
        /// </summary>
        /// <param name="id">the id to check</param>
        bool Exists(string id);
    }
}
