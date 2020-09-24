using COLID.MessageQueue.Datamodel;

namespace COLID.MessageQueue.Services
{
    public interface IMessageQueueService
    {
        void Register();

        void Unregister();

        void PublishMessage(string topic, string message, BasicProperty basicProperty);
    }
}
