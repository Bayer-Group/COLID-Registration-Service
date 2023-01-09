using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using COLID.AWS.DataModels;
using Microsoft.AspNetCore.Http;

namespace COLID.AWS.Interface
{
    /// <summary>
    /// This class contains all AWS S3 related operations for the configured bucket.
    /// </summary>
    public interface IAmazonS3Service
    {
        /// <summary>
        /// Download a file from the given bucket, identified by the given key.
        /// The file will be passed as a memory stream object within the returned Dto.
        /// </summary>
        /// <param name="bucketName">the name of the s3 bucket</param>
        /// <param name="key">the s3 key to use</param>
        Task<AmazonS3FileDownloadDto> GetFileAsync(string bucketName, string key);

        /// <summary>
        /// Download all file from the given bucket.
        /// The files will be passed as key value pair where key is the name of the file and the value being the memory stream object within the returned Dictionary.
        /// </summary>
        /// <param name="bucketName">the name of the s3 bucket</param>        
        Task<Dictionary<string, Stream>> GetAllFileAsync(string bucketName);

        /// <summary>
        /// Upload a file into the given bucket and returns the full s3 key path.
        /// The file will be passed as a memory stream object within the returned Dto.
        /// The s3 object key will be determined by the filename.
        /// </summary>
        /// <param name="bucketName">the name of the s3 bucket</param>
        /// <param name="file">the file object to upload</param>
        Task<AmazonS3FileUploadInfoDto> UploadFileAsync(string bucketName, IFormFile file);

        /// <summary>
        /// Download a file from the given bucket, identified by the given key.
        /// The file will be passed as a memory stream object within the returned Dto.
        /// </summary>
        /// <param name="bucketName">the name of the s3 bucket</param>
        /// <param name="fileObjectPathPrefix">the s3 key to use</param>
        /// <param name="file">the file object to upload</param>
        Task<AmazonS3FileUploadInfoDto> UploadFileAsync(string bucketName, string fileObjectPathPrefix, IFormFile file, bool isPresigned = false);

        /// <summary>
        /// Deletes a file from the given bucket, identified by the given key.
        /// </summary>
        /// <param name="bucketName">the name of the s3 bucket</param>
        /// <param name="key">the s3 key to use</param>
        Task DeleteFileAsync(string bucketName, string key);

        /// <summary>
        /// Creates a valid S3 object URL, based on given bucket, prefix for key and filename.
        /// <br />
        /// <b>Note:</b> The values will be escaped and the filename will be encoded.
        /// </summary>
        /// <param name="bucketName">bucket to use</param>
        /// <param name="fileObjectPathPrefix">key to use</param>
        /// <param name="fileName">filename to use</param>
        /// <returns></returns>
        string GenerateS3ObjectUrl(string bucketName, string fileObjectPathPrefix, string fileName);

        /// <summary>
        /// Creates a valid S3 object URL, based on given bucket and  the full key name
        /// <br />
        /// <b>Note:</b> The values will be escaped and the filename will be encoded.
        /// </summary>
        /// <param name="bucketName">bucket to use</param>
        /// <param name="fullKeyName">key to use</param>
        /// <returns></returns>
        string GenerateS3ObjectUrl(string bucketName, string fullKeyName);
    }
}
