using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using CorrelationId.Abstractions;
using Microsoft.Extensions.Logging;

namespace COLID.Common.Logger
{
#pragma warning disable CA1063 // Implement IDisposable Correctly
    public class CorrelationIdLoggerProvider : ILoggerProvider
#pragma warning restore CA1063 // Implement IDisposable Correctly
    {
        private readonly ConcurrentDictionary<string, CorrelationIdLogger> _loggers = new ConcurrentDictionary<string, CorrelationIdLogger>();
        private readonly ICorrelationContextAccessor _correlationContextAccessor;

        public CorrelationIdLoggerProvider(ICorrelationContextAccessor correlationContextAccessor)
        {
            _correlationContextAccessor = correlationContextAccessor;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new CorrelationIdLogger(name, _correlationContextAccessor));
        }

#pragma warning disable CA1063 // Implement IDisposable Correctly
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
        public void Dispose()
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
#pragma warning restore CA1063 // Implement IDisposable Correctly
        {
            _loggers.Clear();
        }
    }
}
