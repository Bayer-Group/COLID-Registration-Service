using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using CorrelationId.Abstractions;
using Microsoft.Extensions.Logging;

namespace COLID.Common.Logger
{
    public class CorrelationIdLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, CorrelationIdLogger> _loggers = new ConcurrentDictionary<string, CorrelationIdLogger>();
        private readonly ICorrelationContextAccessor _correlationContextAccessor;

        public CorrelationIdLoggerProvider(ICorrelationContextAccessor correlationContextAccessor)
        {
            _correlationContextAccessor = correlationContextAccessor;
        }

        public ILogger CreateLogger(string loggerName)
        {
            return _loggers.GetOrAdd(loggerName, name => new CorrelationIdLogger(name, _correlationContextAccessor));
        }

        public void Dispose()
        {
            _loggers.Clear();
        }
    }
}
