using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using COLID.Exception;
using COLID.Identity;
using COLID.Maintenance;
using COLID.Maintenance.Filters;
using COLID.MessageQueue;
using COLID.RegistrationService.Common;
using COLID.RegistrationService.Services.Authorization;
using COLID.RegistrationService.WebApi.Filters;
using COLID.StatisticsLog;
using COLID.Swagger;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace COLID.RegistrationService.WebApi
{
    /// <summary>
    /// 
    /// </summary>
    public partial class Startup
    {
        /// <summary>
        /// The class to handle startup operations.
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <param name="env">The environment</param>
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// Represents a set of key/value application configuration properties.
        /// </summary>
        public IConfiguration Configuration { get; private set; }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services">The service collection</param>
        public void ConfigureServices(IServiceCollection services)
        {
            Settings.InitializeServiceUrl(Configuration);
            services.AddCors();

            var mvcBuilder = services.AddControllers(configure => configure.Filters.Add(typeof(ValidateActionParametersAttribute)))
                .AddNewtonsoftJson(opt =>
                {
                    opt.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    opt.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                });

            services.AddApiVersioning();
            services.AddHttpContextAccessor();
            services.AddHttpClient("NoProxy").ConfigurePrimaryHttpMessageHandler(() =>
            {
                return new HttpClientHandler
                {
                    UseProxy = false,
                    Proxy = null
                };
            });


            // Disable automatic model state validation. Model state will be checked in ValidateActionParametersAttribute.
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });

            services.AddColidSwaggerGeneration(Configuration, GetApiVersionsByReflection());

            AddServices(services);
            AddModules(services, mvcBuilder);
            
        }

        private void AddServices(IServiceCollection services)
        {
            services.AddSingleton(Configuration);
            services.AddTransient<MaintenanceFilter>();
            services.AddTransient<IHttpContextAccessor, HttpContextAccessor>();
        }

        private void AddModules(IServiceCollection services, IMvcBuilder mvcBuilder)
        {
            services.AddIdentityModule(Configuration);
            services.AddRegistrationServiceAuthorizationModule(Configuration);
            services.AddMessageQueueModule(Configuration);
            services.AddStatisticsLogModule(Configuration);
            services.AddMaintenanceModule(mvcBuilder);            
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">The application builder</param>
        public void Configure(IApplicationBuilder app)
        {
            app.UseExceptionMiddleware();

            app.UseRouting();

            app.UseCors(
                options => options.SetIsOriginAllowed(x => _ = true).AllowAnyMethod().AllowAnyHeader().AllowCredentials()
            );

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseColidSwaggerUI(Configuration, GetApiVersionsByReflection());

            app.UseMessageQueueModule(Configuration);
        }

        private static string[] GetApiVersionsByReflection()
        {
            IList<string> apiVersions = new List<string>();
            foreach (FieldInfo field in typeof(Constants.API.Version).GetFields().Where(f => System.Text.RegularExpressions.Regex.IsMatch(f.Name, Common.Constants.Regex.APIVersionField)))
            {
                apiVersions.Add(field.GetRawConstantValue().ToString());
            }
            return apiVersions.ToArray();
        }
    }
}
