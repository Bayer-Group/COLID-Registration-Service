using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.Graph.Metadata.DataModels.Validation
{
    public class ResourceLinkingResult : ResourceLinkingInformation 
    {
        public string TimeTaken { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
    }
}
