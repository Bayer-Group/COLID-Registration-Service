namespace COLID.RegistrationService.Common.DataModel.Resources
{
    //Transfer object for Sidebar
    public class ResourceOverviewDTO
    {
        public string Id { get; set; }
        public string PidUri { get; set; }
        public string Name { get; set; }
        public string Definition { get; set; }
        public string ResourceType { get; set; }
        public string LifeCycleStatus { get; set; }
        public string PublishedVersion { get; set; }
        public string ChangeRequester { get; set; }
    }
}
