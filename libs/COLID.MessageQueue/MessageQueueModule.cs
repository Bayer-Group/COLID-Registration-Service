using System.Diagnostics.Contracts;
using COLID.MessageQueue.Configuration;
using COLID.MessageQueue.Extensions;
using COLID.MessageQueue.Services;
using CorrelationId;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace COLID.MessageQueue
{
    public static class MessageQueueModule
    {
        /// <summary>
        /// This will register all the supported functionality by Repositories module.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> object for registration.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> object for registration.</param>
        public static IServiceCollection AddMessageQueueModule(this IServiceCollection services, IConfiguration configuration)
        {
            Contract.Requires(configuration != null);
            
            services.AddSingleton<IMessageQueueService, MessageQueueService>();

            services.Configure<ColidMessageQueueOptions>(configuration.GetSection(nameof(ColidMessageQueueOptions)));

            return services;
        }

        /// <summary>
        /// The extension method to use message queue.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> object.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> object.</param>
        /// <returns></returns>
        public static IApplicationBuilder UseMessageQueueModule(this IApplicationBuilder app, IConfiguration configuration)
        {
            app.UseCorrelationId();

            if (configuration.GetValue<bool>($"{nameof(ColidMessageQueueOptions)}:Enabled"))
            {
                app.RegisterMessageQueueReceiver();
            }

            return app;
        }
    }
}
