using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using COLID.AWS.DataModels;
using COLID.AWS.Interface;
using COLID.Exception.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace COLID.AWS.Implementation
{
    public class Ec2InstanceMetadataConnector : IEc2InstanceMetadataConnector
    {
        private readonly HttpClient _client;
        private readonly ILogger<Ec2InstanceMetadataConnector> _logger;

        private const string SECURITY_CREDENTIALS_URL = "http://169.254.169.254/latest/meta-data/iam/security-credentials";

        private string _cachedIamRole = string.Empty;
        private AmazonWebServicesSecurityCredentials _cachedCredentials;

        public Ec2InstanceMetadataConnector(IHttpClientFactory clientFactory, ILogger<Ec2InstanceMetadataConnector> logger)
        {
            _client = clientFactory.CreateClient();
            _client.Timeout = TimeSpan.FromSeconds(5);
            _logger = logger;
        }

        public async Task<string> GetIAMRoleIMDSv1()
        {
            if (!string.IsNullOrEmpty(_cachedIamRole))
            {
                _logger.LogInformation("Use cached AWS IAM role");
                // TODO: refactor later, because caching is not very useful with transient instantiation
                return _cachedIamRole;
            }

            _logger.LogInformation("Fetching AWS IAM Role");
            using var cancellationToken = new CancellationTokenSource();

            try
            {
                var response = await _client.GetAsync(SECURITY_CREDENTIALS_URL, cancellationToken.Token);
                var awsIamRole = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(awsIamRole))
                {
                    throw new TechnicalException("No matching EC2-metadata found! It is necessary to attach the annotation 'iam.amazonaws.com/role' to the executing the kubernetes pod");
                }
                _logger.LogInformation("Found AWS IAM role: " + awsIamRole);
                _cachedIamRole = awsIamRole;

                return awsIamRole;
            }
            catch (SocketException ex)
            {
                throw new TechnicalException(ex.Message, ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new TechnicalException("The HTTP request has been aborted due to timeout.", ex);
            }
        }

        public async Task<AmazonWebServicesSecurityCredentials> GetCredentialsIMDSv1()
        {
            if (_cachedCredentials != null &&
                DateTime.Now.AddSeconds(10) < DateTime.Parse(_cachedCredentials.Expiration))
            {
                _logger.LogInformation("Use cached AWS credentials, cached token expires on: ", _cachedCredentials.Expiration);
                return _cachedCredentials;
            }

            var awsIamRole = await GetIAMRoleIMDSv1();
            using var cancellationToken = new CancellationTokenSource();

            _logger.LogInformation("Fetching AWS credentials");
            try
            {
                var response = await _client.GetAsync($"{SECURITY_CREDENTIALS_URL}/{awsIamRole}", cancellationToken.Token);
                var credentialString = await response.Content.ReadAsStringAsync();
                _logger.LogInformation(credentialString);
                var credentials = JsonConvert.DeserializeObject<AmazonWebServicesSecurityCredentials>(credentialString);
                _cachedCredentials = credentials;

                return credentials;
            }
            catch (SocketException ex)
            {
                throw new TechnicalException(ex.Message, ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new TechnicalException("The HTTP request has been aborted due to timeout.", ex);
            }
        }
    }
}
