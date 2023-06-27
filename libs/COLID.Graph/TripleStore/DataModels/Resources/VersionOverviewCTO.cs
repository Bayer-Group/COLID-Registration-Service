namespace COLID.Graph.Metadata.DataModels.Resources
{
    public class VersionOverviewCTO
    {
        public string Id { get; set; }
        public string Version { get; set; }
        public string PidUri { get; set; }
        public string BaseUri { get; set; }
        public string LifecycleStatus { get; set; }
        public string PublishedVersion { get; set; }
        public string LaterVersion { get; set; }
        public bool HasDraft { get; set; }
        public bool HasPublished { get; set; }
    }
}
