using System.Runtime.Serialization;

namespace COLID.RegistrationService.Common.DataModel.Search
{
    public enum SearchOrder
    {
        [EnumMember(Value = "asc")]
        Asc,

        [EnumMember(Value = "desc")]
        Desc
    }
}
