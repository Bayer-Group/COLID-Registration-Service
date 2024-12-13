using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.SQS.Model;

namespace COLID.AWS.Interface
{
    public interface IAmazonSQSExtendedService
    {
        /// <summary>
        /// Post resource message to queue
        /// </summary>
        /// <param name="queueUrl">url of the AWS SQS</param>
        /// <param name="s3Name">S3 bucket to store large msg</param>
        /// <param name="messageBody">message</param>         
        /// <returns></returns>
        Task<bool> SendMessageAsync(string queueUrl, string s3Name, object messageBody, Dictionary<string, MessageAttributeValue> messageAttributes = null, bool isFifoQueue = true);

        /// <summary>
        /// Receives resource message
        /// </summary>
        /// <param name="queueUrl">url of the AWS SQS</param>
        /// <param name="s3Name">S3 bucket to fetch large msg</param>
        /// <param name="maxMsg">max number of message to fetch at once</param>
        /// <param name="waitSeconds">seconds to wait if there is no message</param>
        /// <returns></returns>
        Task<List<Message>> ReceiveMessageAsync(string queueUrl, string s3Name, int maxMsg, int waitSeconds);


        /// <summary>
        /// Deletes the resource message from the resource queue 
        /// </summary>
        /// <param name="queueUrl">url of the AWS SQS</param>
        /// <param name="s3Name">S3 bucket to delete large msg</param>
        /// <param name="messageReceiptHandle"></param>
        /// <returns></returns>
        Task<bool> DeleteMessageAsync(string queueUrl, string s3Name, string messageReceiptHandle);

        /// <summary>
        /// Get approx message count in the queue
        /// </summary>
        /// <param name="queueUrl">url of the AWS SQS</param>
        /// <param name="s3Name">S3 bucket to store large msg</param>
        /// <returns></returns>
        Task<int> GetMessageCountAsync(string queueUrl, string s3Name);
    }
}
