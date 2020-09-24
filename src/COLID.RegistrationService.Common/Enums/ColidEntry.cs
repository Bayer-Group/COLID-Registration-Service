using System.ComponentModel;

namespace COLID.RegistrationService.Common.Enums.ColidEntry
{
    public enum ColidEntryLifecycleStatus
    {
        [Description("https://pid.bayer.com/kos/19050/draft")]
        Draft,

        [Description("https://pid.bayer.com/kos/19050/published")]
        Published,

        [Description("https://pid.bayer.com/kos/19050/historic")]
        Historic,

        [Description("https://pid.bayer.com/kos/19050/markedForDeletion")]
        MarkedForDeletion
    }

    public enum LifecycleStatus
    {
        [Description("https://pid.bayer.com/kos/19050/released")]
        Released,

        [Description("https://pid.bayer.com/kos/19050/deprecated")]
        Deprecated,

        [Description("https://pid.bayer.com/kos/19050/underDevelopment")]
        UnderDevelopment
    }

    public enum Type
    {
        [Description("https://pid.bayer.com/kos/19050/GenericDataset")]
        GenericDataset
    }
}
