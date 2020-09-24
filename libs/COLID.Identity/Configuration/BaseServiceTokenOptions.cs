using System;

namespace COLID.Identity.Configuration
{
    public abstract class BaseServiceTokenOptions
    {
        public bool Enabled { get; set; }
        public string ServiceId { get; set; }
        public string ClientSecret { get; set; }
    }
}
