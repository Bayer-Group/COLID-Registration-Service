using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.Graph.Metadata.DataModels.FilterGroup
{
    public class FilterGroup
    {
        public string GroupName { get; set; }
        public int Order { get; set; }
        public IList<FilterProperty> Filters { get; set; } = new List<FilterProperty>();
    }
}
