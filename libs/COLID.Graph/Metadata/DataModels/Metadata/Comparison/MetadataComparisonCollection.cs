using System.Collections.Generic;

namespace COLID.Graph.Metadata.DataModels.Metadata.Comparison
{
    /// <summary>
    /// Useful class for deserialization from JSON responses especially in COLID.SearchService
    /// </summary>
    public class MetadataComparisonCollection : Dictionary<string, MetadataComparisonProperty>
    { }
}
