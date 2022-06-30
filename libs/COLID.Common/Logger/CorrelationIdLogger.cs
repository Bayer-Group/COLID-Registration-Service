using System;
using System.Collections.Generic;
using System.Text;
using CorrelationId.Abstractions;
using Microsoft.Extensions.Logging;

namespace COLID.Common.Logger
{
    public class CorrelationIdLogger : ILogger
    {
        private readonly string _name;
        private readonly ICorrelationContextAccessor _correlationContextAccessor;

        public CorrelationIdLogger(string name, ICorrelationContextAccessor correlationContextAccessor)
        {
            _name = name;
            _correlationContextAccessor = correlationContextAccessor;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            var logLevelString = string.Empty;
            var correlationContext = _correlationContextAccessor?.CorrelationContext;
            var correlationId = correlationContext?.CorrelationId;

            logLevelString = GetLogLevelString(logLevel);

            // use correlationID if exists, else eventID
            Console.WriteLine(!string.IsNullOrWhiteSpace(correlationId)
                ? $"{logLevelString} ({correlationId}): {_name}\n     {formatter(state, exception)}"
                : $"{logLevelString} ({eventId}): {_name}\n     {formatter(state, exception)}");
        }

        private static string GetLogLevelString(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    return "trce";
                case LogLevel.Debug:
                    return "dbug";
                case LogLevel.Information:
                    return "info";
                case LogLevel.Warning:
                    return "warn";
                case LogLevel.Error:
                    return "fail";
                case LogLevel.Critical:
                    return "crit";
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel));
            }
        }
    }
}
