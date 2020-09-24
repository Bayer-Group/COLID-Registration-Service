using System;
using System.Runtime.Serialization;

namespace COLID.Graph.Metadata.DataModels.Validation
{
    public enum ValidationResultSeverity
    {
        [EnumMember(Value = Constants.Shacl.Severity.Info)]
        Info,
        [EnumMember(Value = Constants.Shacl.Severity.Warning)]
        Warning,
        [EnumMember(Value = Constants.Shacl.Severity.Violation)]
        Violation
    }
}
