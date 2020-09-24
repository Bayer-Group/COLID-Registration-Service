using System;
using System.Collections.Generic;

namespace COLID.StatisticsLog.DataModel
{
    public class LogEntry
    {
        public DateTime Timestamp { get; private set; }
        public string Message { get; set; }
        public string LogType { get; internal set; }

        public string Location { get; set; }
        public string Layer { get; set; }
        public string Product { get; set; }
        public string HostName { get; set; }

        public string UserId { get; set; }
        public string AppId { get; set; }

        public long? ElapsedMilliseconds { get; set; }

        public Dictionary<string, object> AdditionalInfo { get; set; } = new Dictionary<string, object>();

        public LogEntry()
        {
            Timestamp = DateTime.Now;
        }
    }
}
