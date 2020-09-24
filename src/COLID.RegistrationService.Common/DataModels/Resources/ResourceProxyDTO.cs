using System.Collections.Generic;

namespace COLID.RegistrationService.Common.DataModel.Resources
{
    public class ResourceProxyDTO
    {
        public string PidUrl { get; set; }
        public string TargetUrl { get; set; }
        public string ResourceVersion { get; set; }
        public IList<ResourceProxyDTO> NestedProxies { get; set; }
    }
}
