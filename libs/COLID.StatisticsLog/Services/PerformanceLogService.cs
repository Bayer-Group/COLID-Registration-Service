using System.Diagnostics.Contracts;
using COLID.StatisticsLog.Configuration;
using COLID.StatisticsLog.DataModel;
using COLID.StatisticsLog.LogTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace COLID.StatisticsLog.Services
{
    internal class Performance : ILogType { }

    public class PerformanceLogService : LogServiceBase, IPerformanceLogService
    {
        public PerformanceLogService(IOptionsMonitor<ColidStatisticsLogOptions> optionsAccessor, IHttpContextAccessor httpContextAccessor, IHostEnvironment environment)
            : base(optionsAccessor, httpContextAccessor, environment) { }

        protected override string Suffix() => "performance";

        public void WritePerf(LogEntry performanceEntry)
        {
            Contract.Requires(performanceEntry != null);
            EnrichLogEntry<Performance>(performanceEntry);
            _logger.Information("{@logEntry}", performanceEntry);
        }
    }
}
