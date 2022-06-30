using System;
using System.IO.Enumeration;
using COLID.AWS.DataModels;
using COLID.AWS.Implementation;
using COLID.AWS.Interface;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace COLID.AWS.Tests
{
    public class AmazonS3ServiceTests
    {
        private readonly Mock<IEc2InstanceMetadataConnector> _mockEc2Connector;
        private readonly AmazonWebServicesOptions _awsConfig; 
        private readonly ILogger<AmazonS3Service> _logger;

        private readonly AmazonS3Service _service;

        public AmazonS3ServiceTests()
        {
            _mockEc2Connector = new Mock<IEc2InstanceMetadataConnector>();
            _awsConfig = new AmazonWebServicesOptions {S3Region = "eu-cologne-1"};
            var awsOptionsMonitor = Mock.Of<IOptionsMonitor<AmazonWebServicesOptions>>(_ => _.CurrentValue == _awsConfig);
            _service = new AmazonS3Service(_mockEc2Connector.Object, awsOptionsMonitor, _logger);
        }

        [Fact]
        public void GenerateS3ObjectUrl_Should_ReturnObjectUrl()
        {
            const string bucket = "magic-bucket";
            const string key = "special-key";
            const string fileName = "kewl-filename.pkg";
            const string encodedFileName = "%2fkewl-filename.pkg";

            var objectUrl = _service.GenerateS3ObjectUrl(bucket, key, fileName);
            var expectedUrl = $"https://{bucket}.s3.{_awsConfig.S3Region}.amazonaws.com/{key}{encodedFileName}";

            Assert.Equal(expectedUrl, objectUrl);
        }
    }
}
