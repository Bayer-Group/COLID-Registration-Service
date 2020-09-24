using System.Collections.Generic;

namespace COLID.MessageQueue.Configuration
{
    public class ColidMessageQueueOptions
    {
        public bool Enabled { get; set; }
        public string HostName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string ExchangeName { get; set; }
        public IDictionary<string, string> Topics { get; set; }

        public ColidMessageQueueOptions()
        {
        }
    }
}
