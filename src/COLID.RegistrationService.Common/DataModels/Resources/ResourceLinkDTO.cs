
namespace COLID.RegistrationService.Common.DataModels.Resources
{
    public class ResourceLinkDTO
    {
        public string startNodeId { get; set; }
        public string endNodeId { get; set; }
        public string startNodeType { get; set; }
        public string endNodeType { get; set; }
        public LinkTypeDTO type { get; set; }
        public string status { get; set; }
        public string startNodeName { get; set; }
        public string endNodeName { get; set; }
        public ResourceLinkDTO(string startNodeId,  string endNodeId, string startNodeName, string endNodeName, LinkTypeDTO type = null, string status = null, string startNodeType = null, string endNodeType = null)
        {
            this.startNodeId = startNodeId;
            this.endNodeId = endNodeId;
            this.type = type;
            this.status = status;
            this.startNodeName = startNodeName;
            this.endNodeName = endNodeName;
            this.startNodeType = startNodeType;
            this.endNodeType = endNodeType;
        }
    }
}
