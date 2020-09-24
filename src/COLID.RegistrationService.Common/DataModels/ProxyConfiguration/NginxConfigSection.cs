using System.Collections.Generic;

namespace COLID.RegistrationService.Common.DataModel.ProxyConfiguration
{
    public class NginxConfigSection
    {
        public IList<NginxConfigSection> Subsections { get; set; } = new List<NginxConfigSection>();
        public string Name { get; set; }
        public string Parameters { get; set; }
        public string Version { get; set; }
        public string Order { get; set; }
        public int? OrderInt => Order != null ? int.Parse(Order) : (int?)null;
        public IList<NginxAttribute> Attributes { get; set; } = new List<NginxAttribute>();
    }
}
