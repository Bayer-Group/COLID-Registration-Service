using COLID.StatisticsLog.DataModel;

namespace COLID.StatisticsLog.Services
{
    public interface IPerformanceLogService
    {
        void WritePerf(LogEntry performanceEntry);
    }
}
