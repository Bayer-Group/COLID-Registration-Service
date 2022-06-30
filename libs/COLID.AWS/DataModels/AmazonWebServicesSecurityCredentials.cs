using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.AWS.DataModels
{
    public class AmazonWebServicesSecurityCredentials
    {
        public string AccessKeyId { get; set; }
        public string Code { get; set; }
        public string Expiration { get; set; }
        public string LastUpdated { get; set; }
        public string SecretAccessKey { get; set; }
        public string Token { get; set; }
        public string Type { get; set; }

        public AmazonWebServicesSecurityCredentials()
        {
        }
    }
}
