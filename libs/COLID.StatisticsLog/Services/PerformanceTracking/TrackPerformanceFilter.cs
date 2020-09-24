using System;
using System.Collections.Generic;
using System.Linq;
using COLID.StatisticsLog.Configuration;
using COLID.StatisticsLog.DataModel;
using COLID.StatisticsLog.Type;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace COLID.StatisticsLog.Services.PerformanceTracking
{
    internal class TrackPerformanceFilter : IActionFilter
    {
        private readonly IPerformanceLogService _logService;
        private PerfTracker _tracker;
        private readonly string _product, _layer;
        private readonly IServiceProvider _provider;

        public TrackPerformanceFilter(IPerformanceLogService logService, IOptionsMonitor<ColidStatisticsLogOptions> optionsAccessor, IServiceProvider provider)
        {
            _logService = logService;
            _product = optionsAccessor.CurrentValue.ProductName;
            _layer = optionsAccessor.CurrentValue.LayerName;
            _provider = provider;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (context?.RouteData?.Values == null) return;

            var request = context.HttpContext.Request;
            var activity = $"{request.Path}-{request.Method}";

            var dict = new Dictionary<string, object>();
            foreach (var key in context.RouteData.Values?.Keys)
                dict.Add($"RouteData-{key}", (string)context.RouteData.Values[key]);

            var performanceLogEntry = CreateLogEntry(activity, dict);

            var logAttributes = GetLogAttributes(context);

            // TODO: After splitting logService into 3 types, we need to add dependency here and then select the correct one.
            // But during refactoring discussion, it was discovered that AuditTrail as an Attribute over Controller is not that useful.
            // Ideally we
            foreach (var logAttribute in logAttributes)
            {
                // Log shall be written irrespective of LogType.Performance.
                // So here it just adds claims in case of performance this entry is log later on.
                if (logAttribute.LogType == LogType.Performance)
                {
                    performanceLogEntry.AddAdditionalClaims(context.HttpContext, logAttribute.ClaimMetadatas);
                }
                else
                {
                    var details = CreateLogEntry(activity, dict);

                    switch (logAttribute.LogType)
                    {
                        case LogType.General:
                            _provider.GetService<IGeneralLogService>().Log(details, additionalInfoClaims: logAttribute.ClaimMetadatas.ToList());
                            break;

                        case LogType.AuditTrail:
                            _provider.GetService<IAuditTrailLogService>().AuditTrail(details, logAttribute.ClaimMetadatas.ToList());
                            break;

                        default:
                            Console.WriteLine($"Error: {logAttribute.LogType} not handled in TrackPerformanceFilter.cs");
                            break;
                    }
                }
            }

            _tracker = new PerfTracker(_logService, performanceLogEntry);
        }

        private LogEntry CreateLogEntry(string activity, Dictionary<string, object> dict)
        {
            return new LogEntry
            {
                Product = _product,
                Layer = _layer,
                Message = activity,
                HostName = Environment.MachineName,
                AdditionalInfo = new Dictionary<string, object>(dict)
            };
        }

        private IEnumerable<LogAttribute> GetLogAttributes(ActionExecutingContext context)
        {
            var logAttributes = new List<LogAttribute>();
            foreach (var item in context.ActionDescriptor.FilterDescriptors)
            {
                if (item.Filter is LogAttribute logAttribute)
                {
                    logAttributes.Add(logAttribute);
                }
            }

            return logAttributes;
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            _tracker?.Stop();
        }
    }
}
