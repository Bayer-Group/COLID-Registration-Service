using System.Collections.Generic;
using COLID.StatisticsLog.Configuration;
using COLID.StatisticsLog.DataModel;
using COLID.StatisticsLog.LogTypes;
using COLID.StatisticsLog.Type;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog.Events;

namespace COLID.StatisticsLog.Services
{
    internal class AuditTrail : ILogType { }

    public class AuditTrailLogService : LogServiceBase, IAuditTrailLogService
    {
        public AuditTrailLogService(IOptionsMonitor<ColidStatisticsLogOptions> optionsAccessor, IHttpContextAccessor httpContextAccessor, IHostEnvironment environment)
            : base(optionsAccessor, httpContextAccessor, environment) { }

        protected override string Suffix() => "audit";

        public void AuditTrail(string message, IList<ClaimMetadata> additionalInfoClaims = null)
        {
            var logEntry = CreateLogEntry<AuditTrail>(message);
            logEntry.EnrichAuditTrailIfApplicable(_httpContextAccessor.HttpContext, additionalInfoClaims);
            InternalLog(logEntry, LogEventLevel.Information);
        }

        public void AuditTrail(LogEntry logEntry, IList<ClaimMetadata> additionalInfoClaims = null)
        {
            EnrichLogEntry<AuditTrail>(logEntry);
            logEntry.EnrichAuditTrailIfApplicable(_httpContextAccessor.HttpContext, additionalInfoClaims);
            InternalLog(logEntry, LogEventLevel.Information);
        }
    }
}
