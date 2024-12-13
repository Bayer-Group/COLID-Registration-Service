using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.Internal.Util;
using Amazon.S3;
using Amazon.S3.Model;
using COLID.AWS.DataModels;
using COLID.AWS.Interface;
using COLID.Common.Utilities;
using COLID.Exception.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace COLID.AWS.Implementation
{
    public class AmazonS3Service : IAmazonS3Service
    {
        private readonly IEc2InstanceMetadataConnector _ec2Connector;
        private readonly AmazonWebServicesOptions _awsConfig;
        private readonly ILogger<AmazonS3Service> _logger;

        public AmazonS3Service(IEc2InstanceMetadataConnector ec2Connector, IOptionsMonitor<AmazonWebServicesOptions> awsConfig, ILogger<AmazonS3Service> logger)
        {
            _ec2Connector = ec2Connector;
            _awsConfig = awsConfig.CurrentValue;
            _logger = logger;
        }

        protected virtual async Task<AmazonS3Client> GetAmazonS3Client()
        {
            var awsCredentials = await GetECSCredentials();
            if (!_awsConfig.UseLocalCredentials)
            {
                return new AmazonS3Client(awsCredentials.AccessKeyId, awsCredentials.SecretAccessKey, awsCredentials.Token);
            }
            return new AmazonS3Client(awsCredentials.AccessKeyId, awsCredentials.SecretAccessKey, RegionEndpoint.GetBySystemName(_awsConfig.S3Region));
        }

        private async Task<AmazonWebServicesSecurityCredentials> GetECSCredentials()
        {
            try
            {
                string uri = System.Environment.GetEnvironmentVariable(ECSTaskCredentials.ContainerCredentialsURIEnvVariable);
                if (!string.IsNullOrEmpty(uri))
                {
                    IWebProxy webProxy = System.Net.WebRequest.GetSystemWebProxy();
                    using var ecsTaskCredentials = new ECSTaskCredentials(webProxy);
                    var credentials = ecsTaskCredentials.GetCredentials();
                    return new AmazonWebServicesSecurityCredentials()
                    {
                        AccessKeyId= credentials.AccessKey,                        SecretAccessKey=credentials.SecretKey,
                        Token=credentials.Token 
                    };
                }
            }
            catch (SecurityException e)
            {
                Logger.GetLogger(typeof(ECSTaskCredentials)).Error(e, "Failed to access environment variable {0}", ECSTaskCredentials.ContainerCredentialsURIEnvVariable);
            }           
            return new AmazonWebServicesSecurityCredentials
            {
                Expiration = DateTime.Now.AddMonths(36).ToString(),
                AccessKeyId = _awsConfig.AccessKeyId,
                SecretAccessKey = _awsConfig.SecretAccessKey
            };
        }

        private async Task<AmazonWebServicesSecurityCredentials> GetCredentials()
        {
            if (!_awsConfig.UseLocalCredentials)
            {
                var credentials = await _ec2Connector.GetCredentialsIMDSv1();   // holt credentials von ec2 instanz --> policies    // das muss angepasst werden auf fargate task credentials
                return credentials;
            }

            return new AmazonWebServicesSecurityCredentials
            {
                Expiration = DateTime.Now.AddMonths(36).ToString(),
                AccessKeyId = _awsConfig.AccessKeyId,
                SecretAccessKey = _awsConfig.SecretAccessKey
            };
        }

        public async Task<AmazonS3FileDownloadDto> GetFileAsync(string bucketName, string key)
        {
            Guard.ArgumentNotNullOrWhiteSpace(bucketName, "bucket name can not be null");
            Guard.ArgumentNotNullOrWhiteSpace(key, "key can not be null");

            using var client = await GetAmazonS3Client();
            try
            {
                GetObjectRequest request = new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = NormalizeForAmazonS3(key)
                };

                using GetObjectResponse response = await client.GetObjectAsync(request);
                await using Stream responseStream = response.ResponseStream;

                var memoryStream = new MemoryStream();
                await responseStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0; // Reset stream position to read it again

                return new AmazonS3FileDownloadDto()
                {
                    Stream = memoryStream,
                    ContentType = response.Headers["Content-Type"]
                };
            }
            catch (AmazonS3Exception ex)
            {
                throw HandleAmazonServiceException(ex);
            }
        }

        public async Task<Dictionary<string, Stream>> GetAllFileAsync(string bucketName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(bucketName, "bucket name can not be null");
            Dictionary<string, Stream> fileContents = new Dictionary<string, Stream>();
            try
            {
                //Get S3 Client
                using var client = await GetAmazonS3Client();                
                //Get List of Files from S3
                ListObjectsResponse response = await client.ListObjectsAsync(new ListObjectsRequest
                {
                    BucketName = bucketName
                });                
                //Loop through each file
                foreach (S3Object obj in response.S3Objects)
                {                    
                    //Read files
                    GetObjectResponse objResponse = await client.GetObjectAsync(new GetObjectRequest
                    {
                        BucketName = bucketName,
                        Key = obj.Key
                    });                        
                    fileContents.Add(obj.Key, objResponse.ResponseStream);                    
                }
                return fileContents;
            }
            catch (AmazonS3Exception ex)
            {
                throw HandleAmazonServiceException(ex);
            }
        }

        public async Task<AmazonS3FileUploadInfoDto> UploadFileAsync(string bucketName, IFormFile file) => await UploadFileAsync(bucketName, string.Empty, file);

        public virtual async Task<AmazonS3FileUploadInfoDto> UploadFileAsync(string bucketName, string fileObjectPathPrefix, IFormFile file, bool isPresigned = false)
        {
            Guard.ArgumentNotNullOrWhiteSpace(bucketName, "bucket name can not be null");
            Guard.ArgumentNotNull(fileObjectPathPrefix, "fileObjectPathPrefix");
            Guard.IsGreaterThanZero(file.Length);

            var fileObjectPath = NormalizeForAmazonS3(file.FileName);
            if (!string.IsNullOrWhiteSpace(fileObjectPathPrefix))
            {
                fileObjectPath = NormalizeForAmazonS3($"{fileObjectPathPrefix}/{fileObjectPath}");
            }

            using var client = await GetAmazonS3Client();
            string url = string.Empty;
            string getUrl = string.Empty;

            try
            {
                var response = new PutObjectResponse();
                var filePath = Path.GetTempFileName();
                await using var stream = File.Create(filePath);
                await file.CopyToAsync(stream);

                var request = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = fileObjectPath,
                    InputStream = stream,
                    ContentType = file.ContentType
                };

                response = await client.PutObjectAsync(request);
                if ((int)response.HttpStatusCode >= 200 && (int)response.HttpStatusCode <= 299) // IsSuccessStatusCode
                {
                    return new AmazonS3FileUploadInfoDto
                    {
                        FileName = file.FileName,
                        FileSize = file.Length,
                        FileType = file.ContentType,
                        FileKey = fileObjectPath,
                        S3KeyName =  GenerateS3KeyName(bucketName, fileObjectPath),
                        S3ObjectUrl = GenerateS3ObjectUrl(bucketName, fileObjectPath)
                    };
                }
                    throw new TechnicalException($"AWS S3 doesn't return a success status code: {response.HttpStatusCode} for request with ID {response.ResponseMetadata.RequestId}");

            }
            catch (AmazonS3Exception ex)
            {
                throw HandleAmazonServiceException(ex);
            }
        }

        public async Task DeleteFileAsync(string bucketName, string key)
        {
            Guard.ArgumentNotNullOrWhiteSpace(bucketName, "bucket name can not be null");
            Guard.ArgumentNotNullOrWhiteSpace(key, "key can not be null");

            using var client = await GetAmazonS3Client();
            DeleteObjectRequest deleteRequest = new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = NormalizeForAmazonS3(key)
            };

            try
            {
                await client.DeleteObjectAsync(deleteRequest);
            }
            catch (AmazonS3Exception ex)
            {
                throw HandleAmazonServiceException(ex);
            }
        }

        public virtual string GenerateS3KeyName(string bucketName, string fileObjectPath)
        {
            return
                $"s3://{bucketName}/{HttpUtility.UrlEncode(fileObjectPath)}";
        }

        public virtual string GenerateS3ObjectUrl(string bucketName, string fileObjectPathPrefix, string fileName) => GenerateS3ObjectUrl(bucketName, $"{fileObjectPathPrefix}/{fileName}");
        
        public virtual string GenerateS3ObjectUrl(string bucketName, string fullKeyName)
        {
            return
                $"https://{bucketName}.s3.{_awsConfig.S3Region}.amazonaws.com/{HttpUtility.UrlEncode(NormalizeForAmazonS3(fullKeyName))}";
        }

        /// <summary>
        /// Normalization of key names, because AWS S3 will convert plus chars (+) into empty spaces ( )
        /// </summary>
        /// <param name="key">the key to normalize</param>
        /// <returns></returns>
        protected static string NormalizeForAmazonS3(string key)
        {
            Guard.ArgumentNotNullOrWhiteSpace(key, "key can not be null");
            return key
                .Replace("+", "_",StringComparison.Ordinal)
                .Replace(" ", "_",StringComparison.Ordinal);
        }

        private System.Exception HandleAmazonServiceException(AmazonServiceException ex)
        {
            _logger.LogError(ex, ex.Message, ex.StatusCode);

            throw ex.StatusCode switch
            {
                HttpStatusCode.NotFound => new FileNotFoundException(ex.Message, ex), // TODO: discuss with Team. not handled by exception Middleware
                HttpStatusCode.BadRequest => new ArgumentException(ex.Message, ex),
                _ => new TechnicalException($"An AWS S3 error occurred: {ex.Message}, status code: {ex.StatusCode}", ex)
            };
        }
        private static HttpWebResponse UploadObject(string url, IFormFile file)
        {
            //url = url.Replace("https", "http");
            HttpWebRequest httpRequest = WebRequest.Create(url) as HttpWebRequest;
            httpRequest.Method = "PUT";
            using (Stream dataStream = httpRequest.GetRequestStream())
            {
                var buffer = new byte[8000];
                using (var ms = new MemoryStream())
                {
                    file.CopyTo(ms);
                    ms.Position = 0;
                    ms.CopyTo(dataStream);
                }
            }
            HttpWebResponse response = httpRequest.GetResponse() as HttpWebResponse;
            return response;
        }
    }
}
