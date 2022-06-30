using System;

namespace COLID.AWS.DataModels
{
    public class ColidTripleStoreOptions
    {
        public Uri ReadUrl { get; set;  }

        public Uri UpdateUrl { get; set; }

        public Uri LoaderUrl { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }
    }
}
