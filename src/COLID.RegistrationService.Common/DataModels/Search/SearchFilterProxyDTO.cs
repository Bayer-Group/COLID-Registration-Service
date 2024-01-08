using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace COLID.RegistrationService.Common.DataModels.Search
{
    public class SearchFilterProxyDTO
    {
        public string Name { get; set; }
        public JObject FilterJson { get; set; }
        public string SearchTerm { get; set; }
        public string PidUri { get; set; }
    }
}
