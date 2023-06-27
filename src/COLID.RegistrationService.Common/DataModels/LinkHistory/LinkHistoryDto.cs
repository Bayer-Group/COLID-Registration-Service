using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.RegistrationService.Common.DataModels.LinkHistory
{
    public class LinkHistoryDto
    {
        public Uri LinkHistoryId { get; set; }
        public bool InBound { get; set; }
        public Uri LinkStatus { get; set; }
        public Uri LinkType { get; set; }
        public string LinkTypeLabel { get; set; }
        public string DateCreated { get; set; }
        public string DateDeleted { get; set; }
        public string Author { get; set; }
        public string DeletedBy { get; set; }
        public Uri LinkStartResourcetId { get; set; }
        public Uri LinkStartResourcePidUri { get; set; }
        public string LinkStartResourceLabel { get; set; }
        public Uri LinkStartResourceType { get; set; }
        public string LinkStartResourceTypeLabel { get; set; }
        public Uri LinkEndResourcetId { get; set; }
        public Uri LinkEndResourcePidUri { get; set; }
        public string LinkEndResourceLabel { get; set; }
        public Uri LinkEndResourceType { get; set; }
        public string LinkEndResourceTypeLabel { get; set; }
        public string LastModifiedOn { get; set; }
    }
}
