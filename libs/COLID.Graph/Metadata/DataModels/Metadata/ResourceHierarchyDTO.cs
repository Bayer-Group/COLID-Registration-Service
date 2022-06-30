using System.Collections.Generic;

namespace COLID.Graph.Metadata.DataModels.Metadata
{
    public class ResourceHierarchyDTO
    {

        public void addChild(ResourceHierarchyDTO child)
        {
            Children.Add(child);
        }

        public bool Instantiable { get; set; }

        public bool HasParent { get; set; }
        public bool IsCategory { get; set; }
        public bool HasChild { get; set; }
        public string Name { get; set; }
        public string ParentName { get; set; }
        public string Description { get; set; }
        public string Id { get; set; }
        public IList<ResourceHierarchyDTO> Children { get; set; } = new List<ResourceHierarchyDTO>();
        public int Level { get; set; }

    }
}
