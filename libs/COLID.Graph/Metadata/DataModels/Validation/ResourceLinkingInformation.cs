using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.Graph.Metadata.DataModels.Validation
{
    public class ResourceLinkingInformation
    {
        public string PidUri { get; set; }
        public string LinkType  { get; set; }
        public string PidUriToLink { get; set; }
        public string Requester { get; set; }        
    }
}
