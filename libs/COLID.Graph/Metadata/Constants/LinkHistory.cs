using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using COLID.Graph.Utils;
using Microsoft.Extensions.Configuration;

namespace COLID.Graph.Metadata.Constants
{
   public static class LinkHistory
    {
        public static readonly string ServiceUrl = GraphUtils.CurrentRootConfiguration.GetValue<string>("ServiceUrl");
        public static readonly string HasLinkStart = ServiceUrl + "kos/19050/hasLinkStart";
        public static readonly string HasLinkEnd = ServiceUrl + "kos/19050/hasLinkEnd";
        public static readonly string HasLinkType = ServiceUrl + "kos/19050/hasLinkType";
        public static readonly string HasLinkStatus = ServiceUrl + "kos/19050/hasLinkStatus";
        public static readonly string DeletedBy = ServiceUrl + "kos/19050/deletedBy";
        public static readonly string DateDeleted = ServiceUrl + "kos/19050/dateDeleted";

        public static class LinkStatus
        {
            public static readonly string Deleted = ServiceUrl + "kos/19050/Deleted";
            public static readonly string Created = ServiceUrl + "kos/19050/Created";
        }

    }
}
