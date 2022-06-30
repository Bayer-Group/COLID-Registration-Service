using System;
using System.Collections.Generic;
using System.Text;
using COLID.Graph.Metadata.DataModels.Resources;
namespace COLID.RegistrationService.Common.DataModels.Resources
{
    public class RRMResource : Resource
    {
        public IList<ResourceLinkDTO> CustomLinks { get; set; }
    }
}
