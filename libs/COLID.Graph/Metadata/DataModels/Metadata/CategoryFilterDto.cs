 
using System;
using System.Collections.Generic;

namespace COLID.Graph.Metadata.DataModels.Metadata
{
    public class CategoryFilterDTO
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string LastChangeUser { get; set; }
        public IList<string> ResourceTypes { get; set; } = new List<string>();

    }
}
