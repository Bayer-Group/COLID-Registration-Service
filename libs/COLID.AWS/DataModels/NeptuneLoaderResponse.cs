using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.AWS.DataModels
{
    public class NeptuneLoaderResponse
    {
        public string status { get; set; }

        public IDictionary<string, string> payload { get; set; }
    }
}
