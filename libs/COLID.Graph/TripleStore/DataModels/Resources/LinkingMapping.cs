
using System.ComponentModel;

namespace COLID.RegistrationService.Common.DataModel.Resources
{
    public class LinkingMapping
    {
        public LinkType LinkType { get; set; }

        public string PidUri { get; set; }
        public string InboundLinkLabel { get; set; }
        public string InboundLinkComment { get; set; }

        public LinkingMapping(LinkType linkType,  string pidUri)
        {
            LinkType = linkType;
            PidUri = pidUri;
        }

        public void setLabel(string label)
        {
            this.InboundLinkLabel = label;
        }
        public void setComment(string comment)
        {
            this.InboundLinkComment = comment;
        }
    }

    public enum LinkType
    {
        [Description("INBOUND")]
        inbound,

        [Description("OUTBOUND")]
        outbound,
    }

}
