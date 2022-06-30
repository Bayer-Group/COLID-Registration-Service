using Amazon.SQS.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace COLID.Helper.SQS
{
    public interface IAWSSQSHelper
    {
        Task<bool> SendResourceMessageAsync(object messageBody);
        Task<List<Message>> ReceiveResourceMessageAsync();
        Task<bool> DeleteResourceMessageAsync(string messageReceiptHandle);

        Task<bool> SendLinkingMessageAsync(object messageBody);
        Task<List<Message>> ReceiveLinkingMessageAsync();
        Task<bool> DeleteLinkingMessageAsync(string messageReceiptHandle);
    }
}
