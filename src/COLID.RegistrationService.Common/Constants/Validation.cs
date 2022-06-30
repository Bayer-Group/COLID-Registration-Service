using System.Collections.Generic;
using COLID.Graph.Metadata.Constants;

namespace COLID.RegistrationService.Common.Constants
{
    public static class Validation
    {
        public static readonly IList<string> CriticalProperties = new List<string> { RDF.Type, Resource.DateCreated, Resource.Author, Resource.LastChangeUser, Resource.HasConsumerGroup, Resource.HasLabel, EnterpriseCore.PidUri, Resource.HasVersion, Resource.BaseUri, Resource.HasEntryLifecycleStatus };
        public static readonly IList<string> OverwriteProperties = new List<string> { RDF.Type, EnterpriseCore.PidUri, Resource.BaseUri, Resource.DateCreated, Resource.Author };
            
        public static readonly string DuplicateField = "This field is a duplicate.";
        public static readonly string DuplicateFieldOrphaned = "This identifier has been used by another resource in the past. It cannot be reused to avoid mistaking the resources.";
    }
}
