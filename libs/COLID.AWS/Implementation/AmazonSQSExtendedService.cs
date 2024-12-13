using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.SQS.Model;
using Amazon.SQS;
using COLID.AWS.DataModels;
using COLID.AWS.Interface;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Amazon.SQS.ExtendedClient;
using Amazon.S3;
using Amazon.Runtime.Internal.Util;
using System.Text.Json;
using System.Xml.Linq;

namespace COLID.AWS.Implementation
{
    public class AmazonSQSExtendedService : IAmazonSQSExtendedService
    {

        private readonly AmazonWebServicesOptions _awsConfig;
        private readonly ILogger<AmazonSQSService> _logger;

        /// <summary>
        /// Initialize AWS SQS 
        /// </summary>
        /// <param name="sqs"></param>
        /// <param name="settings"></param>
        public AmazonSQSExtendedService(IOptionsMonitor<AmazonWebServicesOptions> awsConfig, ILogger<AmazonSQSService> logger)
        {
            _awsConfig = awsConfig.CurrentValue;
            _logger = logger;
        }

        private async Task<AmazonWebServicesSecurityCredentials> GetECSCredentials()
        {
            try
            {
                string uri = System.Environment.GetEnvironmentVariable(ECSTaskCredentials.ContainerCredentialsURIEnvVariable);
                if (!string.IsNullOrEmpty(uri))
                {
                    IWebProxy webProxy = System.Net.WebRequest.GetSystemWebProxy();
                    using var ecsTaskCredentials = new ECSTaskCredentials(webProxy);
                    var credentials = ecsTaskCredentials.GetCredentials();
                    return new AmazonWebServicesSecurityCredentials()
                    {
                        AccessKeyId = credentials.AccessKey,
                        SecretAccessKey = credentials.SecretKey,
                        Token = credentials.Token
                    };
                }
            }
            catch (SecurityException e)
            {
                Logger.GetLogger(typeof(ECSTaskCredentials)).Error(e, "Failed to access environment variable {0}", ECSTaskCredentials.ContainerCredentialsURIEnvVariable);
            }

            return new AmazonWebServicesSecurityCredentials
            {
                Expiration = DateTime.Now.AddMonths(36).ToString(),
                AccessKeyId = _awsConfig.AccessKeyId,
                SecretAccessKey = _awsConfig.SecretAccessKey
            };
        }

        protected virtual async Task<AmazonS3Client> GetAmazonS3Client()
        {
            var awsCredentials = await GetECSCredentials();
            if (!_awsConfig.UseLocalCredentials)
            {

                return new AmazonS3Client(awsCredentials.AccessKeyId, awsCredentials.SecretAccessKey, awsCredentials.Token);
            }
            return new AmazonS3Client(awsCredentials.AccessKeyId, awsCredentials.SecretAccessKey, RegionEndpoint.GetBySystemName(_awsConfig.S3Region));
        }

        protected virtual async Task<AmazonSQSClient> GetAmazonSQSClient()
        {
            if (_awsConfig.UseLocalCredentials)
            {
                if (String.IsNullOrEmpty(_awsConfig.AccessKeyId))
                {
                    //For Local Stack
                    AmazonSQSConfig config = new AmazonSQSConfig
                    {
                        ServiceURL = _awsConfig.S3ServiceUrl,
                        UseHttp = true,
                        AuthenticationRegion = _awsConfig.S3Region,
                    };
                    AWSCredentials creds = new AnonymousAWSCredentials();
                    return new AmazonSQSClient(creds, config);
                }
                else
                {
                    //TO connect with AWS from Dev machine
                    return new AmazonSQSClient(_awsConfig.AccessKeyId, _awsConfig.SecretAccessKey);
                }
            }
            else
            {
                //To connect from ECS to sqs
                var awsCredentials = await GetECSCredentials();
                return new AmazonSQSClient(awsCredentials.AccessKeyId, awsCredentials.SecretAccessKey, awsCredentials.Token);
            }
        }

        protected virtual async Task<AmazonSQSExtendedClient> GetAmazonSQSExtendedClient(string S3BucketName)
        {
            var sqsClient = await GetAmazonSQSClient();
            var s3Client = await GetAmazonS3Client();
         
            return new AmazonSQSExtendedClient(sqsClient, new ExtendedClientConfiguration().WithLargePayloadSupportEnabled(s3Client, S3BucketName));
        }

        /// <summary>
        /// Post resource message to queue
        /// </summary>
        /// <param name="queueUrl">url of the AWS SQS</param>
        /// <param name="s3Name">S3 bucket to store large msg</param>
        /// <param name="messageBody">message</param>         
        /// <returns></returns>
        public async Task<bool> SendMessageAsync(string queueUrl, string s3Name, object messageBody, Dictionary<string, MessageAttributeValue> messageAttributes = null, bool isFifoQueue = true)
        {
            string message = JsonSerializer.Serialize(messageBody);
            var sendRequest = new SendMessageRequest(queueUrl, message);
            if (isFifoQueue) //we don't need the values for standard queues in agent squirrel
            {
                sendRequest.MessageGroupId = Guid.NewGuid().ToString();
                sendRequest.MessageDeduplicationId = Guid.NewGuid().ToString();
            }
            var msgAttributes = new Dictionary<string, MessageAttributeValue>();
            msgAttributes.Add("MessageAttribute1", new MessageAttributeValue
            {
                DataType = "String",
                StringValue = "String"
            });
            sendRequest.MessageAttributes = msgAttributes;

            // Post message or payload to queue  
            using (AmazonSQSExtendedClient sqsExtClient = await GetAmazonSQSExtendedClient(s3Name))
            {
                var sendResult = new SendMessageResponse();
                try
                {
                    sendResult = await sqsExtClient.SendMessageAsync(sendRequest);
                    return sendResult.HttpStatusCode == System.Net.HttpStatusCode.OK;
                }
                catch (AmazonSQSException ex)
                {
                    _logger.LogError($"AmazonSQSException: {ex.Message}. RequestId: {ex.RequestId}. StatusCode: {ex.StatusCode}. ErrorCode: {ex.ErrorCode}.", ex);
                }
                catch (AmazonServiceException ex)
                {
                    _logger.LogError($"AmazonServiceException: {ex.Message}. RequestId: {ex.RequestId}. StatusCode: {ex.StatusCode}.", ex);
                }
                catch (System.Exception ex)
                {
                    _logger.LogError($"Exception: {ex.Message}.", ex);
                }
                return false;
            }
        }

        /// <summary>
        /// Receives resource message
        /// </summary>
        /// <param name="queueUrl">url of the AWS SQS</param>
        /// <param name="s3Name">S3 bucket to store large msg</param>
        /// <param name="maxMsg">max number of message to fetch at once</param>
        /// <param name="waitSeconds">seconds to wait if there is no message</param>
        /// <returns></returns>
        public async Task<List<Message>> ReceiveMessageAsync(string queueUrl, string s3Name, int maxMsg, int waitSeconds)
        {
            //Create New instance  
            var request = new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                MaxNumberOfMessages = maxMsg,
                WaitTimeSeconds = waitSeconds
            };
            //CheckIs there any new message available to process  
            using (AmazonSQSExtendedClient sqsExtClient = await GetAmazonSQSExtendedClient(s3Name))
            {
                var result = await sqsExtClient.ReceiveMessageAsync(request);

                return result.Messages.Any() ? result.Messages : new List<Message>();
            }
        }

        /// <summary>
        /// Deletes the resource message from the resource queue 
        /// </summary>
        /// <param name="queueUrl">url of the AWS SQS</param>
        /// <param name="s3Name">S3 bucket to store large msg</param>
        /// <param name="messageReceiptHandle"></param>
        /// <returns></returns>
        public async Task<bool> DeleteMessageAsync(string queueUrl, string s3Name, string messageReceiptHandle)
        {
            using (AmazonSQSExtendedClient sqsExtClient = await GetAmazonSQSExtendedClient(s3Name))
            {
                var deleteResult = await sqsExtClient.DeleteMessageAsync(queueUrl, messageReceiptHandle);
                return deleteResult.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
        }

        /// <summary>
        /// Get approx message count in the queue
        /// </summary>
        /// <param name="queueUrl">url of the AWS SQS</param>
        /// <param name="s3Name">S3 bucket to store large msg</param>
        /// <returns></returns>
        public async Task<int> GetMessageCountAsync(string queueUrl, string s3Name)
        {
            GetQueueAttributesRequest attReq = new GetQueueAttributesRequest();
            attReq.QueueUrl = queueUrl;
            attReq.AttributeNames.Add("ApproximateNumberOfMessages");
            using (AmazonSQSExtendedClient sqsExtClient = await GetAmazonSQSExtendedClient(s3Name))
            {
                GetQueueAttributesResponse response = await sqsExtClient.GetQueueAttributesAsync(attReq);

                return response.ApproximateNumberOfMessages;
            }
        }
    }
}
