using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.Graph.TripleStore.AWS
{
    public class NeptuneLoaderStatusResponse
    {
        public string Status { get; set; }
        public string LoadStatus { get; set; }
        public string StartTime { get; set; }
    }
}
