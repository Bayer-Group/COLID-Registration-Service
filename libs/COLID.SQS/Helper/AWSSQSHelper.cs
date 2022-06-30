using Amazon.SQS;
using Amazon.SQS.Model;
using COLID.SQS.Model;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace COLID.Helper.SQS
{
    public class AWSSQSHelper: IAWSSQSHelper
    {
        private readonly IAmazonSQS _sqs;
        private readonly AWSSQSConfiguration _settings;

        /// <summary>
        /// Initialize AWS SQS 
        /// </summary>
        /// <param name="sqs"></param>
        /// <param name="settings"></param>
        public AWSSQSHelper(IAmazonSQS sqs, IOptions<AWSSQSConfiguration> settings)
        {
            this._sqs = sqs;
            this._settings = settings.Value;
        }

        /// <summary>
        /// Post resource message to queue
        /// </summary>
        /// <param name="userDetail"></param>
        /// <returns></returns>
        public async Task<bool> SendResourceMessageAsync(object messageBody)
        {
            try
            {
                string message = JsonSerializer.Serialize(messageBody);
                var sendRequest = new SendMessageRequest(_settings.ResourceOutputQueueUrl, message);
                sendRequest.MessageGroupId = Guid.NewGuid().ToString();
                sendRequest.MessageDeduplicationId = Guid.NewGuid().ToString();
                // Post message or payload to queue  
                var sendResult = await _sqs.SendMessageAsync(sendRequest);

                return sendResult.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Receives resource message
        /// </summary>
        /// <returns></returns>
        public async Task<List<Message>> ReceiveResourceMessageAsync()
        {
            try
            {
                //Create New instance  
                var request = new ReceiveMessageRequest
                {
                    QueueUrl = _settings.ResourceInputQueueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 10
                };
                //CheckIs there any new message available to process  
                var result = await _sqs.ReceiveMessageAsync(request);

                return result.Messages.Any() ? result.Messages : new List<Message>();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Deletes the resource message from the resource queue 
        /// </summary>
        /// <param name="messageReceiptHandle"></param>
        /// <returns></returns>
        public async Task<bool> DeleteResourceMessageAsync(string messageReceiptHandle)
        {
            try
            {                 
                var deleteResult = await _sqs.DeleteMessageAsync(_settings.ResourceInputQueueUrl, messageReceiptHandle);
                return deleteResult.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Post linking result message to queue
        /// </summary>
        /// <param name="userDetail"></param>
        /// <returns></returns>
        public async Task<bool> SendLinkingMessageAsync(object messageBody)
        {
            try
            {
                string message = JsonSerializer.Serialize(messageBody);
                var sendRequest = new SendMessageRequest(_settings.LinkingOutputQueueUrl, message);
                sendRequest.MessageGroupId = Guid.NewGuid().ToString();
                sendRequest.MessageDeduplicationId = Guid.NewGuid().ToString();
                // Post message or payload to queue  
                var sendResult = await _sqs.SendMessageAsync(sendRequest);

                return sendResult.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Receives linking message
        /// </summary>
        /// <returns></returns>
        public async Task<List<Message>> ReceiveLinkingMessageAsync()
        {
            try
            {
                //Create New instance  
                var request = new ReceiveMessageRequest
                {
                    QueueUrl = _settings.LinkingInputQueueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 5
                };
                //CheckIs there any new message available to process  
                var result = await _sqs.ReceiveMessageAsync(request);

                return result.Messages.Any() ? result.Messages : new List<Message>();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Deletes the linking message from the linking queue 
        /// </summary>
        /// <param name="messageReceiptHandle"></param>
        /// <returns></returns>
        public async Task<bool> DeleteLinkingMessageAsync(string messageReceiptHandle)
        {
            try
            {
                var deleteResult = await _sqs.DeleteMessageAsync(_settings.LinkingInputQueueUrl, messageReceiptHandle);
                return deleteResult.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
