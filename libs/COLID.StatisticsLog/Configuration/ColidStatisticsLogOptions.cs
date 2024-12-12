using System;

namespace COLID.StatisticsLog.Configuration
{
    public class ColidStatisticsLogOptions
    {
        public bool Enabled { get; set; }

        public string ProductName { get; set; }

        public string LayerName { get; set; }

        public Uri BaseUri { get; set; }

        public string DefaultIndex { get; set; }

        public string AwsRegion { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string AnonymizerKey { get; set; }
    }
}
