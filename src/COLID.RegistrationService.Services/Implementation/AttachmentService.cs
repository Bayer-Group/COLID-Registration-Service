using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AutoMapper;
using COLID.AWS.DataModels;
using COLID.AWS.Interface;
using COLID.Common.Utilities;
using COLID.Exception.Models.Business;
using COLID.Graph.Metadata.Constants;
using COLID.Graph.Metadata.Services;
using COLID.Graph.TripleStore.DataModels.Attachments;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.Services;
using COLID.RegistrationService.Common.DataModels.Attachment;
using COLID.RegistrationService.Repositories.Interface;
using COLID.RegistrationService.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace COLID.RegistrationService.Services.Implementation
{
    public class AttachmentService : BaseEntityService<Attachment, BaseEntityRequestDTO, BaseEntityResultDTO, BaseEntityResultCTO, IAttachmentRepository>, IAttachmentService
    {
        private readonly IConfiguration _configuration;
        private readonly IAttachmentRepository _attachmentRepository;
        private readonly IAmazonS3Service _awsS3Service;
        private readonly AmazonWebServicesOptions _awsConfig;
        private readonly ILogger<AttachmentService> _logger;
        private readonly string _s3AccessLinkPrefix;

        public AttachmentService(
            IOptionsMonitor<AmazonWebServicesOptions> awsOptionsMonitor,
            ILogger<AttachmentService> logger,
            IMapper mapper,
            IConfiguration configuration,
            IMetadataService metadataService,
            IAttachmentRepository attachmentRepository,
            IAmazonS3Service awsS3Service) : base(mapper, metadataService, null, attachmentRepository, logger)
        {
            _configuration = configuration;
            _attachmentRepository = attachmentRepository;
            _awsS3Service = awsS3Service;
            _awsConfig = awsOptionsMonitor.CurrentValue;
            _logger = logger;
            _s3AccessLinkPrefix = _configuration.GetConnectionString("s3AccessLinkPrefix");
        }

        public bool Exists(string id)
        {
            Guard.ArgumentNotNullOrWhiteSpace(id, "ID is null or empty");
            Guard.IsValidUri(new Uri(id));

            return _attachmentRepository.CheckIfEntityExists(id, new List<string> { AttachmentConstants.Type }, new HashSet<Uri> { GetResourceInstanceGraph() });
        }

        public async Task<AmazonS3FileDownloadDto> GetAttachment(string id)
        {
            Guard.ArgumentNotNullOrWhiteSpace(id, "Id is null or empty");

            var (guid, fileName) = GetGuidAndFileNameFromId(id);
            return await GetAttachment(guid, fileName);
        }

        public async Task<AmazonS3FileDownloadDto> GetAttachment(Guid id, string fileName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(fileName, "FileName is empty");

            var key = $"{id}/{fileName}";
            var fileDto = await _awsS3Service.GetFileAsync(_awsConfig.S3BucketForFiles, key);

            return fileDto;
        }

        public async Task<AttachmentDto> UploadAttachment(IFormFile file, string comment) => await UploadAttachment(file, Guid.NewGuid(), comment);

        public async Task<AttachmentDto> UploadAttachment(IFormFile file, Guid id, string comment = "")
        {
            Guard.ArgumentNotNull(file, "File cannot be null");
            Guard.ArgumentNotNull(comment, "Comment can not be null but empty");
            Guard.IsGreaterThanZero(file.Length);

            var s3FileInfo = await _awsS3Service.UploadFileAsync(_awsConfig.S3BucketForFiles, id.ToString(), file);
            var attachmentId = CreateAttachmentProperties(s3FileInfo, comment);
            var attachmentDto = new AttachmentDto { Id = attachmentId, s3File = s3FileInfo };
            return attachmentDto;
        }

        private string CreateAttachmentProperties(AmazonS3FileUploadInfoDto s3FileInfo, string comment)
        {
            var attachment = new Attachment()
            {
                Id =  this._s3AccessLinkPrefix + s3FileInfo.FileKey,
                Properties = new Dictionary<string, List<dynamic>>
                {
                    { RDF.Type, new List<dynamic> { AttachmentConstants.Type } },
                    { RDFS.Label, new List<dynamic> { s3FileInfo.FileName } },
                    { AttachmentConstants.HasFileSize, new List<dynamic> { s3FileInfo.FileSize } },
                    { AttachmentConstants.HasFileType, new List<dynamic> { s3FileInfo.FileType } },
                    { RDFS.Comment, new List<dynamic> { comment } }
                }
            };

            var metadata = _metadataService.GetMetadataForEntityType(AttachmentConstants.Type);

            _attachmentRepository.CreateEntity(attachment, metadata, GetResourceInstanceGraph());
            _attachmentRepository.CreateEntity(attachment, metadata, GetDraftResourceInstanceGraph());

            return attachment.Id;
        }

        public async Task DeleteAttachment(string id)
        {
            Guard.ArgumentNotNullOrWhiteSpace(id, "ID is null or empty");
            Guard.IsValidUri(new Uri(id));

            if (_attachmentRepository.IsAttachmentAllowedToDelete(id, GetHistoricInstanceGraph(), GetResourceInstanceGraph(), GetDraftResourceInstanceGraph()))
            {
                var (guid, fileName) = GetGuidAndFileNameFromId(id);
                var key = $"{guid}/{fileName}";
                await _awsS3Service.DeleteFileAsync(_awsConfig.S3BucketForFiles, key);
                _attachmentRepository.DeleteEntity(id, GetResourceInstanceGraph());
                _attachmentRepository.DeleteEntity(id, GetDraftResourceInstanceGraph());
            }
            else
            {
                throw new ConflictException(Common.Constants.Messages.AttachmentMsg.Conflict);
            }
        }

        public async Task DeleteAttachment(Guid id, string fileName)
        {
            Guard.ArgumentNotNullOrWhiteSpace(fileName, "FileName is empty");

            var delId = GetIdFromGuidAndFileName(id, fileName);
            await DeleteAttachment(delId);
        }

        public async Task DeleteAttachments(ICollection<dynamic> ids)
        {
            if (ids == null || !ids.Any())
            {
                return;
            }

            foreach (var id in ids)
            {
                try
                {
                    await DeleteAttachment(id);
                }
                catch (FileNotFoundException ex)
                {
                    // ignore missing files to process further ids
                    _logger.LogError($"The attachment with id {id} could not be found: ", ex);
                }
            }
        }

        private string GetIdFromGuidAndFileName(Guid guid, string fileName)
        {
            return _awsS3Service.GenerateS3ObjectUrl(_awsConfig.S3BucketForFiles, guid.ToString(), fileName);
        }

        private static Tuple<Guid, string> GetGuidAndFileNameFromId(string id)
        {
            Guard.IsValidUri(new Uri(id));

            var filename = id.Split('/').Last();
            var guidString = Regex.Match(id, Common.Constants.Regex.Guid).Groups[0].ToString();
            var guid = Guid.Parse(guidString);

            return new Tuple<Guid, string>(guid, filename);
        }

        private Uri GetHistoricInstanceGraph()
        {
            return _metadataService.GetHistoricInstanceGraph();
        }

        private Uri GetResourceInstanceGraph()
        {
            return _metadataService.GetInstanceGraph(PIDO.PidConcept);
        }
        private Uri GetDraftResourceInstanceGraph()
        {
            return _metadataService.GetInstanceGraph("draft");
        }
    }
}
