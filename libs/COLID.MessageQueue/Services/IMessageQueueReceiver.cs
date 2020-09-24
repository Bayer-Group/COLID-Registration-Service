using System;
using System.Collections.Generic;

namespace COLID.MessageQueue.Services
{
    public interface IMessageQueueReceiver
    {
        IDictionary<string, Action<string>> OnTopicReceivers { get; }
    }
}
