using System;
using COLID.MessageQueue.Datamodel;

namespace COLID.MessageQueue.Services
{
    public interface IMessageQueuePublisher
    {
        Action<string, string, BasicProperty> PublishMessage { get; set; }
    }
}
