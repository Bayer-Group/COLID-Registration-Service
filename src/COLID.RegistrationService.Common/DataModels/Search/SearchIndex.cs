using System.Runtime.Serialization;

namespace COLID.RegistrationService.Common.DataModel.Search
{
    public enum SearchIndex
    {
        [EnumMember(Value = "draft")]
        Draft,

        [EnumMember(Value = "published")]
        Published,

        [EnumMember(Value = "all")]
        All
    }
}
