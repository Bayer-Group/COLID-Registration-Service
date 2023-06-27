using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Amazon;
using Amazon.S3;
using COLID.AWS.DataModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace COLID.AWS.Implementation
{
    public class MinIoService : AmazonS3Service
    {
        private readonly AmazonWebServicesOptions _awsConfig;

        public MinIoService(IOptionsMonitor<AmazonWebServicesOptions> awsConfig, ILogger<MinIoService> logger)
            : base(null, awsConfig, logger)
        {
            _awsConfig = awsConfig.CurrentValue;
        }

        protected override async Task<AmazonS3Client> GetAmazonS3Client()
        {
            var s3Config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(_awsConfig.S3Region), // MUST set this before setting ServiceURL and it should match the `MINIO_REGION` environment variable.
                ServiceURL = _awsConfig.S3ServiceUrl,
                ForcePathStyle = true // MUST be true to work correctly with MinIO server
            };

            var client = new AmazonS3Client(_awsConfig.AccessKeyId, _awsConfig.SecretAccessKey, s3Config);
            return client;
        }

        public override string GenerateS3ObjectUrl(string bucketName, string fileObjectPathPrefix, string fileName) => GenerateS3ObjectUrl(bucketName, $"{fileObjectPathPrefix}/{fileName}");

        public override string GenerateS3ObjectUrl(string bucketName, string fullKeyName)
        {
            UriBuilder builder = new UriBuilder(_awsConfig.S3ServiceUrl);
            builder.Host = "localhost";

            return $"{builder}{bucketName}/{HttpUtility.UrlEncode(fullKeyName)}";
        }
    }
}
