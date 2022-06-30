using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using COLID.AWS.DataModels;
using COLID.AWS.Interface;
using COLID.Exception.Models.Business;
using COLID.Graph.Metadata.Constants;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.Services;
using COLID.Graph.TripleStore.DataModels.Attachments;
using COLID.RegistrationService.Repositories.Interface;
using COLID.RegistrationService.Services.Implementation;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.Tests.Unit.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace COLID.RegistrationService.Tests.Unit.Services
{
    [ExcludeFromCodeCoverage]
    public class AttachmentServiceTests
    {
        private readonly Mock<IConfiguration> _configuration;
        private readonly Mock<IAttachmentRepository> _mockAttachmentRepository;
        private readonly Mock<IAmazonS3Service> _mockAwsS3Service;
        private readonly Mock<IMetadataService> _mockMetadataService;
        private readonly Mock<ILogger<AttachmentService>> _mockLogger;
        private readonly Mock<IMapper> _mockMapper;

        private readonly IAttachmentService _service;

        private readonly ITestOutputHelper _output;

        public AttachmentServiceTests(ITestOutputHelper output)
        {
            _mockAttachmentRepository = new Mock<IAttachmentRepository>();
            _mockAwsS3Service = new Mock<IAmazonS3Service>();
            _mockMetadataService = new Mock<IMetadataService>();
            _mockLogger = new Mock<ILogger<AttachmentService>>();
            _mockMapper = new Mock<IMapper>();
            _configuration = new Mock<IConfiguration>();

            _output = output;

            AmazonWebServicesOptions awsOptions = new AmazonWebServicesOptions();
            var awsOptionsMonitor = Mock.Of<IOptionsMonitor<AmazonWebServicesOptions>>(_ => _.CurrentValue == awsOptions);

            _service = new AttachmentService(awsOptionsMonitor, _mockLogger.Object, _mockMapper.Object, _configuration.Object,
                _mockMetadataService.Object, _mockAttachmentRepository.Object, _mockAwsS3Service.Object);
        }

        #region Exists

        [Theory]
        [InlineData("http://file.s3.aws.com/test123")]
        [InlineData("s3://maybethis?")]
        [InlineData("s3://also.this.should/be")]
        public void Exists_Should_Call_CheckIfEntityExists(string id)
        {
            _output.WriteLine($"Testing id: `{id}`");
            _service.Exists(id);
            _mockAttachmentRepository.Verify(x => x.CheckIfEntityExists(id, It.IsAny<IList<string>>(), It.IsAny<HashSet<Uri>>()), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Exists_Throws_ArgumentException_IfIdIsEmptyOrNull(string id)
        {
            _output.WriteLine($"Testing id: `{id}`");
            Assert.Throws<ArgumentNullException>(() => _service.Exists(id));
        }

        [Theory]
        [InlineData("this is not an uri")]
        [InlineData("http:/thisneither")]
        public void Exists_Throws_UriFormatException_IfIdIsIsNotAnUri(string id)
        {
            _output.WriteLine($"Testing id: `{id}`");
            Assert.Throws<UriFormatException>(() => _service.Exists(id));
        }

        #endregion Exists

        #region GetAttachment

        [Fact]
        public async Task GetAttachment_ById_Should_ReturnFileDto()
        {
            // ARRANGE
            var guid = Guid.NewGuid();
            var filename = "fancy_filename.meh";
            var id = $"http://{guid}/{filename}";
            var keyUsedForCall = string.Empty;
            await using var expectedStream = new MemoryStream(Encoding.UTF8.GetBytes("owen_wilson_says_wow"));
            var expectedS3File = new AmazonS3FileDownloadDto { ContentType = "application/text", Stream = expectedStream };
            _mockAwsS3Service.Setup(x => x.GetFileAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(expectedS3File)
                .Callback<string, string>((bucket, key) => keyUsedForCall = key);

            // ACT
            var resultS3File = await _service.GetAttachment(id);
            _output.WriteLine(resultS3File.ToString());

            // ASSERT
            Assert.NotNull(resultS3File);
            Assert.Equal(expectedS3File.ContentType, resultS3File.ContentType);
            Assert.Equal(expectedStream, resultS3File.Stream);
            Assert.Equal($"{guid}/{filename}", keyUsedForCall);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void GetAttachment_ById_ThrowsException_IfArgumentIsNullOrWhitespace(string id)
        {
            _output.WriteLine($"Testing id: `{id}`");
            Assert.ThrowsAsync<ArgumentException>(() => _service.GetAttachment(id));
        }

        [Fact]
        public async Task GetAttachment_ByGuidAndFilename_Should_ReturnFileDto()
        {
            // ARRANGE
            var guid = Guid.NewGuid();
            var filename = "fancy_filename.meh";
            var expectedKey = $"{guid}/{filename}";
            var keyUsedForCall = string.Empty;
            await using var expectedStream = new MemoryStream(Encoding.UTF8.GetBytes("owen_wilson_says_wow"));
            var expectedS3File = new AmazonS3FileDownloadDto { ContentType = "application/text", Stream = expectedStream };
            _mockAwsS3Service.Setup(x => x.GetFileAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(expectedS3File)
                .Callback<string, string>((bucket, key) => keyUsedForCall = key);

            // ACT
            var resultS3File = await _service.GetAttachment(guid, filename);

            // ASSERT
            Assert.NotNull(resultS3File);
            Assert.Equal(expectedS3File.ContentType, resultS3File.ContentType);
            Assert.Equal(expectedStream, resultS3File.Stream);
            Assert.Equal(expectedKey, keyUsedForCall);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void GetAttachment_ByGuidAndFilename_ThrowsException_IfArgumentIsNullOrWhitespace(string filename)
        {
            _output.WriteLine($"Testing filename: `{filename}`");
            Assert.ThrowsAsync<ArgumentException>(() => _service.GetAttachment(Guid.NewGuid(), filename));
        }

        #endregion GetAttachment

        #region UploadAttachment

        [Fact]
        public async Task UploadAttachment_ByFileAndGuid_Should_Return_AttachmentDto()
        {
            // ARRANGE
            var comment = "no comment";
            var s3ObjectMock = new AmazonS3FileUploadInfoDto
            {
                FileName = "testfile.jpg",
                FileSize = 20000,
                FileType = "image/jpg",
                S3KeyName = "7FB3719C-EE44-4425-AD6A-BCD800725529/testfile.jpg",
                S3ObjectUrl = "http://s3.aws.com/7FB3719C-EE44-4425-AD6A-BCD800725529/testfile.jpg"
            };
            using var contentStream = new MemoryStream();
            contentStream.Write(new ASCIIEncoding().GetBytes(s3ObjectMock.FileName));
            IFormFile file = new FormFile(contentStream, 0, s3ObjectMock.FileSize, s3ObjectMock.FileName, s3ObjectMock.FileName);

            _mockAwsS3Service.Setup(x => x.UploadFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IFormFile>(),false))
                .ReturnsAsync(s3ObjectMock);

            // Check the attachment object to store the correct values within the graph
            Attachment interceptedAttachment = null;
            _mockAttachmentRepository.Setup(x =>
                    x.CreateEntity(It.IsAny<Attachment>(), It.IsAny<IList<MetadataProperty>>(), It.IsAny<Uri>()))
                .Callback<Attachment, IList<MetadataProperty>, Uri>((attachment, metadata, graph) => interceptedAttachment = attachment);

            // ACT
            var response = await _service.UploadAttachment(file, "no comment");

            // ASSERT
            Assert.NotNull(response);
            //Assert.Equal(s3ObjectMock.S3ObjectUrl, response.Id);
            Assert.Equal(s3ObjectMock.FileName, response.s3File.FileName);
            Assert.Equal(s3ObjectMock.FileType, response.s3File.FileType);
            Assert.Equal(s3ObjectMock.FileSize, response.s3File.FileSize);
            Assert.Equal(s3ObjectMock.S3KeyName, response.s3File.S3KeyName);
            Assert.Equal(s3ObjectMock.S3ObjectUrl, response.s3File.S3ObjectUrl);

            //Assert.Equal(s3ObjectMock.S3ObjectUrl, interceptedAttachment.Id);
            var props = interceptedAttachment.Properties;
            Assert.True(props.ContainsType(AttachmentConstants.Type));
            Assert.True(props.ContainsRdfsLabel(s3ObjectMock.FileName));
            Assert.True(props.ContainsAttachmentFileSize(s3ObjectMock.FileSize.ToString()));
            Assert.True(props.ContainsAttachmentFileType(s3ObjectMock.FileType));
            Assert.True(props.ContainsRdfsComment(comment));
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData(null, "")]
        [InlineData(null, " ")]
        public void UploadAttachment_ByFileAndGuid_ThrowsException_IfArgumentIsNullOrWhitespace(IFormFile file, string comment)
        {
            Assert.ThrowsAsync<ArgumentException>(() => _service.UploadAttachment(file, comment));
        }

        [Fact]
        public void UploadAttachment_ByFileAndGuid_ThrowsException_IfFileHasNoContent()
        {
            using var emptyStream = new MemoryStream();
            IFormFile file = new FormFile(emptyStream, 0, 0, "name", "filename");
            Assert.ThrowsAsync<ArgumentException>(() => _service.UploadAttachment(file, ""));
        }

        [Fact]
        public void UploadAttachment_ByFileAndGuid_ThrowsException_IfCommentIsNull()
        {
            using var emptyStream = new MemoryStream();
            IFormFile file = new FormFile(emptyStream, 0, 20, "name", "filename");
            Assert.ThrowsAsync<ArgumentException>(() => _service.UploadAttachment(file, null));
        }

        #endregion UploadAttachment

        #region DeleteAttachment

        [Fact]
        public void DeleteAttachment_ByGuidAndKey_Should_DeleteAttachment()
        {
            // ARRANGE
            _mockAttachmentRepository.Setup(x =>
                    x.IsAttachmentAllowedToDelete(It.IsAny<string>(), It.IsAny<Uri>(), It.IsAny<Uri>(), It.IsAny<Uri>()))
                .Returns(true);

            var guid = Guid.NewGuid();
            var fileName = "total+cool filename.jpg";
            var expectedObjectUrl = $"http://{guid}/{fileName}";
            _mockAwsS3Service.Setup(x => x.GenerateS3ObjectUrl(It.IsAny<string>(), guid.ToString(), fileName))
                .Returns(expectedObjectUrl);

            var interceptedKeyName = string.Empty;
            _mockAwsS3Service.Setup(x => x.DeleteFileAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((bucket, key) => interceptedKeyName = key);

            // ACT
            _service.DeleteAttachment(guid, fileName);

            // ASSERT
            _mockAwsS3Service.Verify(x => x.DeleteFileAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _mockAttachmentRepository.Verify(x => x.DeleteEntity(It.IsAny<string>(), It.IsAny<Uri>()), Times.Exactly(2));
            Assert.Equal(expectedObjectUrl.Replace("http://", ""), interceptedKeyName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void DeleteAttachment_ByGuidAndKey_ThrowsException_IfKeyIsNullOrWhitespace(string key)
        {
            _output.WriteLine($"Testing key: `{key}`");
            Assert.ThrowsAsync<ArgumentException>(() => _service.DeleteAttachment(Guid.NewGuid(), key));
        }

        [Fact]
        public void DeleteAttachment_ById_Should_DeleteAttachment()
        {
            // ARRANGE
            _mockAttachmentRepository.Setup(x =>
                    x.IsAttachmentAllowedToDelete(It.IsAny<string>(), It.IsAny<Uri>(), It.IsAny<Uri>(), It.IsAny<Uri>()))
                .Returns(true);

            var guid = Guid.NewGuid();
            var fileName = "total+cool filename.jpg";
            var expectedKeyName = $"{guid}/{fileName}";
            var id = $"http://{expectedKeyName}";
            _mockAwsS3Service.Setup(x => x.GenerateS3ObjectUrl(It.IsAny<string>(), guid.ToString(), fileName))
                .Returns(expectedKeyName);

            var interceptedKeyName = string.Empty;
            _mockAwsS3Service.Setup(x => x.DeleteFileAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((bucket, key) => interceptedKeyName = key);

            // ACT
            _service.DeleteAttachment(id);

            // ASSERT
            _mockAwsS3Service.Verify(x => x.DeleteFileAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _mockAttachmentRepository.Verify(x => x.DeleteEntity(It.IsAny<string>(), It.IsAny<Uri>()), Times.Exactly(2));
            Assert.Equal(expectedKeyName, interceptedKeyName);
        }

        [Fact]
        public void DeleteAttachment_ById_ThrowsException_IfDeleteFileIsNotAllowed()
        {
            _mockAttachmentRepository.Setup(x =>
                    x.IsAttachmentAllowedToDelete(It.IsAny<string>(), It.IsAny<Uri>(), It.IsAny<Uri>(), It.IsAny<Uri>()))
                .Returns(false);
            var id = $"http://{Guid.NewGuid()}/testfile.jpg";
            Assert.ThrowsAsync<ConflictException>(() => _service.DeleteAttachment(id));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void DeleteAttachment_ById_ThrowsException_IfIdIsNullOrWhitespace(string id)
        {
            _output.WriteLine($"Testing id: `{id}`");
            Assert.ThrowsAsync<ArgumentException>(() => _service.DeleteAttachment(id));
        }

        [Theory]
        [InlineData("well no uri")]
        [InlineData("htp:/alsoNoUri")]
        [InlineData("ugh://meh")]
        public void DeleteAttachment_ById_ThrowsException_IfUriIsNotValid(string id)
        {
            _output.WriteLine($"Testing id: `{id}`");
            Assert.ThrowsAsync<UriFormatException>(() => _service.DeleteAttachment(id));
        }

        [Fact]
        public void DeleteAttachments_ByIds_Should_DeleteAttachments()
        {
            // ARRANGE
            _mockAttachmentRepository.Setup(x =>
                    x.IsAttachmentAllowedToDelete(It.IsAny<string>(), It.IsAny<Uri>(), It.IsAny<Uri>(), It.IsAny<Uri>()))
                .Returns(true);

            // ACT
            ICollection<dynamic> ids = new List<dynamic> {
                "http://00000000-0000-0000-0000-000000000001/meh",
                "http://00000000-0000-0000-0000-000000000002/meh",
                "http://00000000-0000-0000-0000-000000000003/meh" };
            _service.DeleteAttachments(ids);

            // ASSERT
            _mockAwsS3Service.Verify(x => x.DeleteFileAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(3));
            _mockAttachmentRepository.Verify(x => x.DeleteEntity(It.IsAny<string>(), It.IsAny<Uri>()), Times.Exactly(6));
        }

        [Fact]
        public void DeleteAttachments_ByIds_Should_DoNothingIfIdsAreInvalid()
        {
            // ARRANGE
            _mockAttachmentRepository.Setup(x =>
                    x.IsAttachmentAllowedToDelete(It.IsAny<string>(), It.IsAny<Uri>(), It.IsAny<Uri>(), It.IsAny<Uri>()))
                .Returns(true);

            // ACT
            ICollection<dynamic> ids = new List<dynamic> {
                "invalid_uri",
                "http://[00000000-0000-0000-0000-000000000002]meh",
                "" };
            _service.DeleteAttachments(ids);
            _service.DeleteAttachments(null);

            // ASSERT
            _mockAwsS3Service.Verify(x => x.DeleteFileAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(0));
            _mockAttachmentRepository.Verify(x => x.DeleteEntity(It.IsAny<string>(), It.IsAny<Uri>()), Times.Exactly(0));
        }

        #endregion DeleteAttachment
    }
}
