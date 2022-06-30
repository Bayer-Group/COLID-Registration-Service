using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace COLID.Common.Logger
{
    public static class LoggerModule
    {
        /// <summary>
        /// This will register all the supported functionality by service logging module.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> object for registration.</param>
        public static IServiceCollection AddCorrelationIdLogger(this IServiceCollection services)
        {
            services.AddLogging(builder =>
            {
                builder.Services.AddTransient<ILoggerProvider, CorrelationIdLoggerProvider>();
            });

            return services;
        }
    }
}
