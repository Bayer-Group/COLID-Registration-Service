using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace COLID.AWS.DataModels
{
    public class AmazonS3FileDownloadDto
    {
        public Stream Stream { get; set; }

        public string ContentType { get; set; }
    }
}
