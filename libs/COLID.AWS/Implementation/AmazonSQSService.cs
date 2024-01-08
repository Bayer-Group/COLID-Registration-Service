using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using COLID.AWS.Interface;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace COLID.AWS.Implementation
{
    public class AmazonSQSService : IAmazonSQSService
    {
        private readonly IAmazonSQS _sqs;        

        /// <summary>
        /// Initialize AWS SQS 
        /// </summary>
        /// <param name="sqs"></param>
        /// <param name="settings"></param>
        public AmazonSQSService(IAmazonSQS sqs)
        {
            this._sqs = sqs;            
        }

        /// <summary>
        /// Post resource message to queue
        /// </summary>
        /// <param name="queueUrl">url of the AWS SQS</param>
        /// <param name="messageBody">message</param>         
        /// <returns></returns>
        public async Task<bool> SendMessageAsync(string queueUrl, object messageBody)
        {
            string message = JsonSerializer.Serialize(messageBody);
            var sendRequest = new SendMessageRequest(queueUrl, message);
            sendRequest.MessageGroupId = Guid.NewGuid().ToString();
            sendRequest.MessageDeduplicationId = Guid.NewGuid().ToString();
            // Post message or payload to queue  
            var sendResult = await _sqs.SendMessageAsync(sendRequest);

            return sendResult.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }

        /// <summary>
        /// Receives resource message
        /// </summary>
        /// <param name="queueUrl">url of the AWS SQS</param>
        /// <param name="maxMsg">max number of message to fetch at once</param>
        /// <param name="waitSeconds">seconds to wait if there is no message</param>
        /// <returns></returns>
        public async Task<List<Message>> ReceiveMessageAsync(string queueUrl, int maxMsg, int waitSeconds)
        {
            //Create New instance  
            var request = new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                MaxNumberOfMessages = maxMsg,
                WaitTimeSeconds = waitSeconds
            };
            //CheckIs there any new message available to process  
            var result = await _sqs.ReceiveMessageAsync(request);

            return result.Messages.Any() ? result.Messages : new List<Message>();
        }

        /// <summary>
        /// Deletes the resource message from the resource queue 
        /// </summary>
        /// <param name="queueUrl">url of the AWS SQS</param>
        /// <param name="messageReceiptHandle"></param>
        /// <returns></returns>
        public async Task<bool> DeleteMessageAsync(string queueUrl, string messageReceiptHandle)
        {
            var deleteResult = await _sqs.DeleteMessageAsync(queueUrl, messageReceiptHandle);
            return deleteResult.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }

        /// <summary>
        /// Get approx message count in the queue
        /// </summary>
        /// <param name="queueUrl">url of the AWS SQS</param>
        /// <returns></returns>
        public async Task<int> GetMessageCountAsync(string queueUrl)
        {
            GetQueueAttributesRequest attReq = new GetQueueAttributesRequest();
            attReq.QueueUrl = queueUrl;
            attReq.AttributeNames.Add("ApproximateNumberOfMessages");
            GetQueueAttributesResponse response = await _sqs.GetQueueAttributesAsync(attReq);          

            return response.ApproximateNumberOfMessages;
        }
    }
}
