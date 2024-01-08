using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.SQS.Model;

namespace COLID.AWS.Interface
{
    public interface IAmazonSQSService
    {
        /// <summary>
        /// Post resource message to queue
        /// </summary>
        /// <param name="queueUrl">url of the AWS SQS</param>
        /// <param name="messageBody">message</param>         
        /// <returns></returns>
        Task<bool> SendMessageAsync(string queueUrl, object messageBody);

        /// <summary>
        /// Receives resource message
        /// </summary>
        /// <param name="queueUrl">url of the AWS SQS</param>
        /// <param name="maxMsg">max number of message to fetch at once</param>
        /// <param name="waitSeconds">seconds to wait if there is no message</param>
        /// <returns></returns>
        Task<List<Message>> ReceiveMessageAsync(string queueUrl, int maxMsg, int waitSeconds);


        /// <summary>
        /// Deletes the resource message from the resource queue 
        /// </summary>
        /// <param name="queueUrl">url of the AWS SQS</param>
        /// <param name="messageReceiptHandle"></param>
        /// <returns></returns>
        Task<bool> DeleteMessageAsync(string queueUrl, string messageReceiptHandle);

        /// <summary>
        /// Get approx message count in the queue
        /// </summary>
        /// <param name="queueUrl">url of the AWS SQS</param>
        /// <returns></returns>
        Task<int> GetMessageCountAsync(string queueUrl);
    }
}
