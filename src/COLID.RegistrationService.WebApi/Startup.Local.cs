﻿using COLID.Cache;
using COLID.RegistrationService.Repositories;
using COLID.RegistrationService.Services;
using COLID.RegistrationService.Services.Implementation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace COLID.RegistrationService.WebApi
{
    public partial class Startup
    {
        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>The service collection</param>
        public void ConfigureLocalServices(IServiceCollection services)
        {
            var builder = new ConfigurationBuilder()
                .AddConfiguration(Configuration)
                .AddUserSecrets<Startup>();

            Configuration = builder.Build();

            ConfigureServices(services);
            services.RegisterDebugRepositoriesModule(Configuration);
            services.AddLocalServicesModule(Configuration);
            //services.AddHostedService<BulkProcessBackgroundService>();
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">The application builder</param>
        public void ConfigureLocal(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();
            Configure(app);
        }
    }
}
