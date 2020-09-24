using System;
using System.Collections.Generic;
using COLID.StatisticsLog.Configuration;
using COLID.StatisticsLog.DataModel;
using COLID.StatisticsLog.LogTypes;
using COLID.StatisticsLog.Type;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;

namespace COLID.StatisticsLog.Services
{
    internal class GeneralLogService : LogServiceBase, IGeneralLogService
    {
        public GeneralLogService(IOptionsMonitor<ColidStatisticsLogOptions> optionsAccessor, IHttpContextAccessor httpContextAccessor)
            : base(optionsAccessor, httpContextAccessor) { }

        protected override string Suffix() => "general";

        protected override LoggerConfiguration GetLoggerConfiguration(ColidStatisticsLogOptions options)
        {
            return base.GetLoggerConfiguration(options)
                .WriteTo.Console(LogEventLevel.Error);
        }

        public void Debug<T>(string message, Dictionary<string, object> additionalInfo = null) where T : ILogType
            => CreateAndLogEntry<T>(message, additionalInfo, LogEventLevel.Debug);

        public void Debug(string message, Dictionary<string, object> additionalInfo = null)
            => Debug<General>(message, additionalInfo);

        public void Error<T>(string message, Dictionary<string, dynamic> additionalInfo = null) where T : ILogType
            => CreateAndLogEntry<T>(message, additionalInfo, LogEventLevel.Error);

        public void Error(string message, Dictionary<string, dynamic> additionalInfo = null)
            => Error<General>(message, additionalInfo);

        public void Error<T>(Exception exception, string message, Dictionary<string, dynamic> additionalInfo = null) where T : ILogType
        {
            var logEntry = CreateLogEntry<T>(message, additionalInfo);
            logEntry.AdditionalInfo.Add("exception", exception);
            _logger.Error(exception, "{@logEntry}", logEntry);
        }

        public void Error(Exception exception, string message, Dictionary<string, dynamic> additionalInfo = null)
            => Error<General>(exception, message, additionalInfo);

        public void Fatal<T>(string message, Dictionary<string, dynamic> additionalInfo = null) where T : ILogType
            => CreateAndLogEntry<T>(message, additionalInfo, LogEventLevel.Fatal);

        public void Fatal(string message, Dictionary<string, dynamic> additionalInfo = null)
            => Fatal<General>(message, additionalInfo);

        public void Info<T>(string message, Dictionary<string, dynamic> additionalInfo = null) where T : ILogType
            => CreateAndLogEntry<T>(message, additionalInfo, LogEventLevel.Information);

        public void Info(string message, Dictionary<string, dynamic> additionalInfo = null)
            => Info<General>(message, additionalInfo);

        public void Log<T>(LogEntry logEntry, LogEventLevel logLevel = LogEventLevel.Information, IList<ClaimMetadata> additionalInfoClaims = null) where T : ILogType
        {
            EnrichLogEntry<T>(logEntry);
            logEntry.AddAdditionalClaims(_httpContextAccessor.HttpContext, additionalInfoClaims);
            InternalLog(logEntry, logLevel);
        }

        public void Log(LogEntry logEntry, LogEventLevel logLevel = LogEventLevel.Information, IList<ClaimMetadata> additionalInfoClaims = null)
            => Log<General>(logEntry, logLevel, additionalInfoClaims);

        public void Verbose<T>(string message, Dictionary<string, dynamic> additionalInfo = null) where T : ILogType
            => CreateAndLogEntry<T>(message, additionalInfo, LogEventLevel.Verbose);

        public void Verbose(string message, Dictionary<string, dynamic> additionalInfo = null)
            => Verbose<General>(message, additionalInfo);

        public void Warn<T>(string message, Dictionary<string, dynamic> additionalInfo = null) where T : ILogType
            => CreateAndLogEntry<T>(message, additionalInfo, LogEventLevel.Warning);

        public void Warn(string message, Dictionary<string, dynamic> additionalInfo = null)
            => Warn<General>(message, additionalInfo);

        private void CreateAndLogEntry<T>(string message, Dictionary<string, dynamic> additionalInfo, LogEventLevel logLevel) where T : ILogType
        {
            var logEntry = CreateLogEntry<T>(message, additionalInfo);
            InternalLog(logEntry, logLevel);
        }
    }
}
