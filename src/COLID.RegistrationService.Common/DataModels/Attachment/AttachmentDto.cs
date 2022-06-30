using System;
using System.Collections.Generic;
using System.Text;
using COLID.AWS.DataModels;

namespace COLID.RegistrationService.Common.DataModels.Attachment
{
    public class AttachmentDto
    {
        public string Id { get; set; }

        public AmazonS3FileUploadInfoDto s3File { get; set; }
    }
}
