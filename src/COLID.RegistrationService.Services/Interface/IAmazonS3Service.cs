using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;

namespace COLID.RegistrationService.Services.Interface
{
    /// <summary>
    /// This class contains all AWS S3 related operations.
    /// </summary>
    public interface IAmazonS3Service
    {
        /// <summary>
        /// Upload a turtle-file (ttl) into a specified S3 bucket, hosted on AWS.
        /// </summary>
        /// <param name="file">the file to upload</param>
        Task<string> UploadFile(IFormFile file);

    }
}
