namespace COLID.RegistrationService.Common.DataModel.Resources
{
    public class ResourceMarkedDeletedDTO
    {
        public string Id { get; set; }

        public string PidUri { get; set; }
        public string ResourceType { get; set; }
        public string Label { get; set; }
        public string Definition { get; set; }
    }
}
