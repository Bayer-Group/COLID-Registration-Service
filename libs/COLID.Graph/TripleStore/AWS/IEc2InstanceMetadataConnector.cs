using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using COLID.Graph.TripleStore.DataModels.AWS;

namespace COLID.Graph.TripleStore.AWS
{
    public interface IEc2InstanceMetadataConnector
    {
        /// <summary>
        /// Fetches the IAM role via IMDS v1 from the executing AWS EC2-instance within the kubernetes cluster.
        /// </summary>
        Task<string> GetIAMRoleIMDSv1();

        /// <summary>
        /// Fetches the AWS Credentialsfrom the executing AWS EC2-instance within the kubernetes cluster via the IAM role.
        /// </summary>
        Task<AmazonWebServicesSecurityCredentials> GetCredentialsIMDSv1();
    }
}
