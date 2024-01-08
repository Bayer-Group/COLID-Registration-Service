using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COLID.RegistrationService.Common.DataModels.Graph
{    
    public class UpdateKeyWordGraph
    {
        public Uri Graph { get; set; }
        public Uri SaveAsGraph { get; set; }
        public Uri SaveAsType { get; set; }
        public IList<Addition> Additions { get; set; }
        public IList<Deletetion> Deletions { get; set; }
        public IList<Updation> Updations { get; set; }
    }

    public class Addition
    {
        public string Label { get; set; }
    }

    public class Deletetion
    {
        public Uri KeyId { get; set; }
    }

    public class Updation
    {
        public Uri KeyId { get; set; }
        public string Label { get; set; }
    }    
}
