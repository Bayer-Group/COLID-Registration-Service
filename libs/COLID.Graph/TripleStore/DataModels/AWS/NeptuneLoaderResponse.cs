using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.Graph.TripleStore.AWS
{
    public class NeptuneLoaderResponse
    {
        public string status { get; set; }

        public IDictionary<string, string> payload { get; set; }
    }
}
