using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.AWS.DataModels
{
    public class NeptuneLoaderRequest
    {
        public string Source { get; set; }

        public string Format { get; set; }

        public string IamRoleArn { get; set; }

        public string Region { get; set; }

        public string FailOnError { get; set; }

        public string Parallelism { get; set; }

        public Dictionary<string, string> ParserConfiguration { get; set; }

        public NeptuneLoaderRequest()
        {
            ParserConfiguration = new Dictionary<string, string>();
        }
    }
}
