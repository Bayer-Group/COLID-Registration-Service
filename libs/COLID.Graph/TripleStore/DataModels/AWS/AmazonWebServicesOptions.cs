using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.Graph.TripleStore.DataModels.AWS
{
    public class AmazonWebServicesOptions
    {
        public string S3Region { get; set; }

        public string S3BucketName { get; set; }

        public string S3AccessIamRoleArn { get; set; }

        public bool UseLocalCredentials { get; set; }

        public string AccessKeyId { get; set; }

        public string SecretAccessKey { get; set; }
    }
}
