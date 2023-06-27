using System;
using System.Collections.Generic;
using System.Text;
using COLID.Graph.Utils;
using Microsoft.Extensions.Configuration;

namespace COLID.Graph.Metadata.Constants
{
    public static class FilterGroupAndProperties
    {
        public static readonly string ServiceUrl = GraphUtils.CurrentRootConfiguration.GetValue<string>("ServiceUrl");

        public static readonly string FilterCategoryGroups = ServiceUrl + "kos/19050/FilterGroup";
        public static readonly string FilterGroupOrder = ServiceUrl + "kos/19050/GroupOrder";
        public static readonly string FilterProperties = ServiceUrl + "kos/19050/GroupProperty";
        public static readonly string FilterPropertyUri = ServiceUrl + "kos/19050/PropertyUrl";
        public static readonly string FilterPropertyOrder = ServiceUrl + "kos/19050/PropertyOrder";
    }
}
