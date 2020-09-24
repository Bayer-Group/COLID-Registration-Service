using COLID.MessageQueue.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace COLID.MessageQueue.Extensions
{
    /// <summary>
    /// Extention for message queue opertion handling.
    /// </summary>
    internal static class ApplicationBuilderExtentions
    {
        private static IMessageQueueService _messageQueueListenerService;
        private static IApplicationBuilder _app;

        /// <summary>
        /// Register message listener.
        /// </summary>
        /// <param name="app">The object of <see cref="IApplicationBuilder"/>.</param>
        /// <returns>The object of <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder RegisterMessageQueueReceiver(this IApplicationBuilder app)
        {
            _app = app;
            var lifetime = app.ApplicationServices.GetService<IApplicationLifetime>();

            lifetime.ApplicationStarted.Register(OnStarted);
            lifetime.ApplicationStopping.Register(OnStopping);

            return app;
        }

        private static void OnStarted()
        {
            //Subscribe MQ for published resources in PID
            _messageQueueListenerService = _app.ApplicationServices.GetService<IMessageQueueService>();
            _messageQueueListenerService.Register();
        }

        private static void OnStopping()
        {
            _messageQueueListenerService.Unregister();
        }
    }
}
