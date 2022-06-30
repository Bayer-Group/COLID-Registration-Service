using System.Collections.Generic;

namespace COLID.Cache.Configuration
{
    public class ColidCacheOptions
    {
        public bool Enabled { get; set; }

        public bool UseInMemory { get; set; }

        public IList<string> EndpointUrls { get; set; }

        public string Password { get; set; }

        public double AbsoluteExpirationRelativeToNow { get; set; }

        public int SyncTimeout { get; set; }
        public int KeepAlive { get; set; }
        public int ReconnectRetryPolicy { get; set; }
        public bool AbortOnConnectFail { get; set; }

        public bool AllowAdmin { get; set; }

        public bool Ssl { get; set; }

        public ColidCacheOptions()
        {
            EndpointUrls = new List<string>();
        }
    }
}
