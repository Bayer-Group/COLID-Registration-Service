using System;
using System.Collections.Generic;
using COLID.StatisticsLog.DataModel;
using COLID.StatisticsLog.LogTypes;
using COLID.StatisticsLog.Type;
using Serilog.Events;

namespace COLID.StatisticsLog.Services
{
    public interface IGeneralLogService : IDisposable
    {
        void Debug<T>(string message, Dictionary<string, object> additionalInfo = null) where T : ILogType;

        void Debug(string message, Dictionary<string, object> additionalInfo = null);

#pragma warning disable CA1716 // Identifiers should not match keywords

        void Error<T>(string message, Dictionary<string, object> additionalInfo = null) where T : ILogType;

        void Error(string message, Dictionary<string, object> additionalInfo = null);

        void Error<T>(Exception exception, string message, Dictionary<string, object> additionalInfo = null) where T : ILogType;

        void Error(Exception exception, string message, Dictionary<string, object> additionalInfo = null);

#pragma warning restore CA1716 // Identifiers should not match keywords

        void Fatal<T>(string message, Dictionary<string, object> additionalInfo = null) where T : ILogType;

        void Fatal(string message, Dictionary<string, object> additionalInfo = null);

        void Info<T>(string message, Dictionary<string, object> additionalInfo = null) where T : ILogType;

        void Info(string message, Dictionary<string, object> additionalInfo = null);

        void Log<T>(LogEntry logEntry, LogEventLevel logLevel = LogEventLevel.Information, IList<ClaimMetadata> additionalInfoClaims = null) where T : ILogType;

        void Log(LogEntry logEntry, LogEventLevel logLevel = LogEventLevel.Information, IList<ClaimMetadata> additionalInfoClaims = null);

        void Verbose<T>(string message, Dictionary<string, object> additionalInfo = null) where T : ILogType;

        void Verbose(string message, Dictionary<string, object> additionalInfo = null);

        void Warn<T>(string message, Dictionary<string, object> additionalInfo = null) where T : ILogType;

        void Warn(string message, Dictionary<string, object> additionalInfo = null);
    }
}
