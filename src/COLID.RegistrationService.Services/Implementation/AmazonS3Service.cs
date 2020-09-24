using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using AngleSharp.Io;
using COLID.Exception.Models;
using COLID.Graph.TripleStore.AWS;
using COLID.Graph.TripleStore.DataModels.AWS;
using COLID.RegistrationService.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace COLID.RegistrationService.Services.Implementation
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

        public async Task<string> UploadFile(IFormFile file)
        {
            using var client = await GetAmazonS3Client();
            try
            {
                if (file.Length > 0)
                {
                    var filePath = Path.GetTempFileName();
                    await using var stream = File.Create(filePath);
                    await file.CopyToAsync(stream);

                    var request = new PutObjectRequest
                    {
                        BucketName = _awsConfig.S3BucketName,
                        Key = file.FileName.Replace("+", "%2B"), // s3 interprets + as space
                        InputStream = stream,
                        ContentType = MimeTypeNames.Plain
                    };

                    var response = await client.PutObjectAsync(request);
                    if ((int)response.HttpStatusCode >= 200 && (int)response.HttpStatusCode <= 299) // IsSuccessStatusCode
                    {
                        return $"s3://{_awsConfig.S3BucketName}/{request.Key}";
                    }
                    throw new TechnicalException($"AWS S3 doesn't return a success status code: {response.HttpStatusCode}");
                }
            }
            catch (AmazonS3Exception ex)
            {
                throw new TechnicalException($"An AWS S3 error occured: {ex.Message}", ex);
            }

            return string.Empty;
        }

        private async Task<AmazonS3Client> GetAmazonS3Client()
        {
            var awsCredentials = await GetCredentials();
            if (!_awsConfig.UseLocalCredentials)
            {
                return new AmazonS3Client(awsCredentials.AccessKeyId, awsCredentials.SecretAccessKey, awsCredentials.Token);
            }
            return new AmazonS3Client(awsCredentials.AccessKeyId, awsCredentials.SecretAccessKey, RegionEndpoint.GetBySystemName(_awsConfig.S3Region));
        }

        private async Task<AmazonWebServicesSecurityCredentials> GetCredentials()
        {
            if (!_awsConfig.UseLocalCredentials)
            {
                // TODO: try catch exception if not available and use access key fallback. LOG EVERYTHING !!
                return await _ec2Connector.GetCredentialsIMDSv1();
            }

            return new AmazonWebServicesSecurityCredentials
            {
                Expiration = DateTime.Now.AddMonths(36).ToString(),
                AccessKeyId = _awsConfig.AccessKeyId,
                SecretAccessKey = _awsConfig.SecretAccessKey
            };
        }
    }
}
