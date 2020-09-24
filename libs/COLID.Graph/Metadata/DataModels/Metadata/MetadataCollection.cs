using System.Collections.Generic;

namespace COLID.Graph.Metadata.DataModels.Metadata
{
    /// <summary>
    /// Useful class for deserialization from JSON responses especially in COLID.SearchService
    /// </summary>
    public class MetadataCollection : Dictionary<string, MetadataProperty>
    { }
}
