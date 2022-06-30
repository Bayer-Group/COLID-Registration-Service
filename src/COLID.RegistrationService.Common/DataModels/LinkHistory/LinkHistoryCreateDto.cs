using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.RegistrationService.Common.DataModels.LinkHistory
{
    public class LinkHistoryCreateDto
    {
        public Uri Id { get; set; }
        public Uri HasLinkStart { get; set; }
        public Uri HasLinkEnd { get; set; }
        public Uri HasLinkType { get; set; }
        public Uri HasLinkStatus { get; set; }
        public string Author { get; set; }
        public DateTime DateCreated { get; set; }

        public LinkHistoryCreateDto(Uri id, Uri hasLinkStart, Uri hasLinkEnd, Uri hasLinkType, Uri hasStatus, string author, DateTime dateCreated)
        {
            Id = id;
            HasLinkStart = hasLinkStart;
            HasLinkEnd = hasLinkEnd;
            HasLinkType = hasLinkType;
            HasLinkStatus = hasStatus;
            Author = author;
            DateCreated = dateCreated;
        }

        public LinkHistoryCreateDto()
        {
        }
    }
}
