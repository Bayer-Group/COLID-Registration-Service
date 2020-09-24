using System;
using Microsoft.AspNetCore.Mvc.Filters;

namespace COLID.StatisticsLog.Type
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class LogAttribute : Attribute, IFilterMetadata
    {
        public LogAttribute(LogType logType, params ClaimMetadata[] claimMetadatas)
        {
            LogType = logType;
            ClaimMetadatas = claimMetadatas;
        }

        public LogAttribute(LogType logType)
        {
            LogType = logType;
            ClaimMetadatas = Array.Empty<ClaimMetadata>();
        }

        public LogType LogType { get; }
        public ClaimMetadata[] ClaimMetadatas { get; }
    }
}
