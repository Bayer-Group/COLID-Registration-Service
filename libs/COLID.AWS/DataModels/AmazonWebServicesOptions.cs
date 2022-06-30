using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.AWS.DataModels
{
    public class AmazonWebServicesOptions
    {
        /// <summary>
        /// The AWS S3 region to use.
        /// </summary>
        public string S3Region { get; set; }

        /// <summary>
        /// Bucket to store file attachments for resources.
        /// </summary>
        public string S3BucketForFiles { get; set; }

        /// <summary>
        /// Bucket to store graphs to import them to AWS Neptune.
        /// </summary>
        public string S3BucketForGraphs { get; set; }

        /// <summary>
        /// ARN of the AWS IAM role to access the S3 bucket.
        /// </summary>
        public string S3AccessIamRoleArn { get; set; }

        /// <summary>
        /// Use MinIO as a local alternative as S3.
        /// </summary>
        public bool S3UseMinIo { get; set; }

        /// <summary>
        /// S3 Service URL will only be used if S3UseMinIo is true.
        /// </summary>
        public string S3ServiceUrl { get; set; }

        /// <summary>
        /// Flat to use AccessKeyId and SecretAccessKey from the config or
        /// determine the credentials from AWS EC2 Metadata API via IMDSv1.
        /// https://docs.aws.amazon.com/AWSEC2/latest/UserGuide/instancedata-data-retrieval.html
        /// </summary>
        public bool UseLocalCredentials { get; set; }
        
        /// <summary>
        /// Prefix of S3 File Access Links 
        /// </summary>
        public bool S3AccessLinkPrefix { get; set; }

        /// <summary>
        /// The AWS/MinIO Access Key Id.
        /// </summary>
        public string AccessKeyId { get; set; }

        /// <summary>
        /// The AWS/MinIO Secret Access Id.
        /// </summary>
        public string SecretAccessKey { get; set; }
    }
}
