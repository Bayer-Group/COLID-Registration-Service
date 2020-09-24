using System.Collections.Generic;
using COLID.StatisticsLog.DataModel;
using COLID.StatisticsLog.Type;

namespace COLID.StatisticsLog.Services
{
    public interface IAuditTrailLogService
    {
        void AuditTrail(string message, IList<ClaimMetadata> additionalInfoClaims = null);

        void AuditTrail(LogEntry logEntry, IList<ClaimMetadata> additionalInfoClaims = null);
    }
}
