using System;
using System.Collections.Generic;
using System.Text;
using COLID.Graph.TripleStore.DataModels.Base;
namespace COLID.RegistrationService.Common.DataModels.Resources
{
    public class ResourceRevisionsDTO
    {
        public IList<ResourceRevision> Revisions { get; set; }
    }
    public class ResourceRevision
    {
        public string Name { get; set; }
        public Dictionary<string, List<dynamic>> Additionals { get; set; }
        public Dictionary<string, List<dynamic>> Removals { get; set; }
    }
}
