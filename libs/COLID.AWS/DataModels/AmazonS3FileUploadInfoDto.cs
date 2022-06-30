using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.AWS.DataModels
{
    public class AmazonS3FileUploadInfoDto
    {
        public string FileName { get; set; }

        public string FileType { get; set; }

        public long FileSize { get; set; }

        public string S3KeyName { get; set; }

        public string S3ObjectUrl { get; set; }

        public string FileKey { get; internal set; }
    }
}
