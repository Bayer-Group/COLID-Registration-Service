using System.ComponentModel;
using System.Runtime.Serialization;

namespace COLID.RegistrationService.Common.Enums.PidUriTemplate
{
    public enum IdType
    {
        [Description("https://pid.bayer.com/kos/19050/taxonomies#Guid")]
        [EnumMember(Value = "GUID")]
        Guid,

        [Description("https://pid.bayer.com/kos/19050/taxonomies#Number")]
        [EnumMember(Value = "Number")]
        Number,
    }

    public enum Suffix
    {
        [Description("https://pid.bayer.com/kos/19050/taxonomies#Empty")]
        [EnumMember(Value = "")]
        Empty,

        [Description("https://pid.bayer.com/kos/19050/taxonomies#Slash")]
        [EnumMember(Value = "/")]
        Slash,
    }

    public enum LifecycleStatus
    {
        [Description("https://pid.bayer.com/kos/19050/active")]
        Active,

        [Description("https://pid.bayer.com/kos/19050/deprecated")]
        Deprecated,
    }
}
