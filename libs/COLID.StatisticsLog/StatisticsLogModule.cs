using System.Diagnostics.Contracts;
using COLID.StatisticsLog.Configuration;
using COLID.StatisticsLog.Services;
using COLID.StatisticsLog.Services.PerformanceTracking;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace COLID.StatisticsLog
{
    /// <summary>
    /// Module to facilitate integration in WebAPI project.
    /// </summary>
    public static class StatisticsLogModule
    {
        /// <summary>
        /// This will register all the supported functionality by Logging module.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> object for registration.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> object for registration.</param>
        public static IServiceCollection AddStatisticsLogModule(this IServiceCollection services, IConfiguration configuration)
        {
            return services
                .AddBasicStatisticsLog(configuration)
                .AddPerformanceTracking();
        }

        /// <summary>
        /// This will register logging service.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> object for registration.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> object for registration.</param>
        public static IServiceCollection AddBasicStatisticsLog(this IServiceCollection services, IConfiguration configuration)
        {
            Contract.Requires(configuration != null);
            return services
                .Configure<ColidStatisticsLogOptions>(configuration.GetSection(nameof(ColidStatisticsLogOptions)))
                .AddSingleton<IGeneralLogService, GeneralLogService>()
                .AddSingleton<IAuditTrailLogService, AuditTrailLogService>()
                .AddSingleton<IPerformanceLogService, PerformanceLogService>();
        }

        /// <summary>
        /// This will register performance tracking log.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> object for registration.</param>
        public static IServiceCollection AddPerformanceTracking(this IServiceCollection services)
        {
            services.AddMvc(options => options.Filters.Add(typeof(TrackPerformanceFilter)));
            return services;
        }
    }
}
