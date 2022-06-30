using System.Collections.Generic;
using COLID.Cache;
using COLID.Cache.Configuration;
using COLID.Graph;
using COLID.RegistrationService.Repositories.Implementation;
using COLID.RegistrationService.Repositories.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace COLID.RegistrationService.Repositories
{
    public static class RepositoriesModule
    {
        /// <summary>
        /// This method will register all the supported functions for Repository modules.<br />
        /// <b>Note:</b> The cache can be disabled but no in memory is allowed to use!
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> object for registration</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> object for registration</param>
        public static IServiceCollection RegisterRepositoriesModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.RegisterBaseRepositoriesModule();
            services.AddGraphModule(configuration);
            //services.AddSingletonGraphModule(configuration);
            services.AddDistributedCacheModule(configuration, GetCachingJsonSerializerSettings());

            return services;
        }

        /// <summary>
        /// This will register all the supported functionality by Repositories module for debugging environments.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> object for registration</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> object for registration</param>
        public static IServiceCollection RegisterDebugRepositoriesModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.RegisterBaseRepositoriesModule();
            services.AddGraphModule(configuration);
            //services.AddSingletonGraphModule(configuration);

            services.AddCacheModule(configuration, GetCachingJsonSerializerSettings());

            return services;
        }

        /// <summary>
        /// Global generation of json settings for cache.
        /// </summary>
        private static CachingJsonSerializerSettings GetCachingJsonSerializerSettings()
        {
            return new CachingJsonSerializerSettings
            {
                Converters = new List<JsonConverter>(),
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                },
                Formatting = Formatting.Indented
            };
        }

        /// <summary>
        /// This will register all the supported functionality by Repositories module for debugging environments.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> object for registration</param>
        public static IServiceCollection RegisterBaseRepositoriesModule(this IServiceCollection services)
        {
            services.AddTransient<IResourceRepository, ResourceRepository>();
            services.AddTransient<IConsumerGroupRepository, ConsumerGroupRepository>();
            services.AddTransient<IPidUriTemplateRepository, PidUriTemplateRepository>();
            services.AddTransient<IExtendedUriTemplateRepository, ExtendedUriTemplateRepository>();
            services.AddTransient<IEntityRepository, EntityRepository>();
            services.AddTransient<IIdentifierRepository, IdentifierRepository>();
            //services.AddTransient<IHistoricResourceRepository, HistoricResourceRepository>();
            services.AddTransient<ITaxonomyRepository, TaxonomyRepository>();
            services.AddTransient<IGraphManagementRepository, GraphManagementRepository>();
            services.AddTransient<IAttachmentRepository, AttachmentRepository>();
            services.AddTransient<IIronMountainRepository, IronMountainRepository>();

            return services;
        }
    }
}
