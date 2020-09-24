using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.Graph.TripleStore.AWS
{
    public class NeptuneLoaderErrorResponse
    {
        public string DetailedMessage { get; set; }

        public string RequestId { get; set; }

        public string Code { get; set; }
    }
}
