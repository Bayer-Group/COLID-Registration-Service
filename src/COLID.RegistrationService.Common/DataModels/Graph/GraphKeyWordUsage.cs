using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COLID.RegistrationService.Common.DataModels.Graph
{
    public class GraphKeyWordUsage
    {
        public Uri KeyId { get; set; }
        public string Label { get; set; }
        public int Usage { get; set; }
    }
}
