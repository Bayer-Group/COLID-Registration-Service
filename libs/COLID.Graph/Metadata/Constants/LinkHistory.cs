using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.Graph.Metadata.Constants
{
   public class LinkHistory
    {
        public const string HasLinkStart = "https://pid.bayer.com/kos/19050/hasLinkStart";
        public const string HasLinkEnd = "https://pid.bayer.com/kos/19050/hasLinkEnd";
        public const string HasLinkType = "https://pid.bayer.com/kos/19050/hasLinkType";
        public const string HasLinkStatus = "https://pid.bayer.com/kos/19050/hasLinkStatus";
        public const string DeletedBy = "https://pid.bayer.com/kos/19050/deletedBy";
        public const string DateDeleted = "https://pid.bayer.com/kos/19050/dateDeleted";

        public static class LinkStatus
        {
            public const string Deleted = "https://pid.bayer.com/kos/19050/Deleted";
            public const string Created = "https://pid.bayer.com/kos/19050/Created";
        }

    }
}
