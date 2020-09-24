using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using COLID.StatisticsLog.DataModel;

namespace COLID.StatisticsLog.Services.PerformanceTracking
{
    internal class PerfTracker
    {
        private readonly Stopwatch _sw;
        private readonly LogEntry _infoToLog;
        private readonly IPerformanceLogService _logService;

        public PerfTracker(IPerformanceLogService logService, LogEntry details)
        {
            _logService = logService;
            _sw = Stopwatch.StartNew();
            _infoToLog = details;

            var beginTime = DateTime.Now;
            if (_infoToLog.AdditionalInfo == null)
                _infoToLog.AdditionalInfo = new Dictionary<string, object>()
                {
                    { "Started", beginTime.ToString(CultureInfo.InvariantCulture) }
                };
            else
                _infoToLog.AdditionalInfo.Add(
                    "Started", beginTime.ToString(CultureInfo.InvariantCulture));
        }

        public PerfTracker(string name, string userId, string appId,
                   string location, string product, string layer)
        {
            _infoToLog = new LogEntry()
            {
                Message = name,
                UserId = userId,
                AppId = appId,
                Product = product,
                Layer = layer,
                Location = location,
                HostName = Environment.MachineName
            };

            var beginTime = DateTime.Now;
            _infoToLog.AdditionalInfo = new Dictionary<string, object>()
            {
                { "Started", beginTime.ToString(CultureInfo.InvariantCulture)  }
            };
        }

        public PerfTracker(string name, string userId, string appId,
            string location, string product, string layer,
                   Dictionary<string, object> perfParams)
            : this(name, userId, appId, location, product, layer)
        {
            Contract.Requires(perfParams != null);
            foreach (var item in perfParams)
                _infoToLog.AdditionalInfo.Add("input-" + item.Key, item.Value);
        }

        public void Stop()
        {
            _sw.Stop();
            _infoToLog.ElapsedMilliseconds = _sw.ElapsedMilliseconds;
            _logService.WritePerf(_infoToLog);
        }
    }
}
