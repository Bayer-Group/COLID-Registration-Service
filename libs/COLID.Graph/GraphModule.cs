using System;
using COLID.Graph.HashGenerator.Services;
using COLID.Graph.Metadata.Repositories;
using COLID.Graph.Metadata.Services;
using COLID.Graph.TripleStore.Configuration;
using COLID.Graph.TripleStore.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace COLID.Graph
{
    public static class GraphModule
    {
        /// <summary>
        /// This will register all the supported functionality by Repositories module with a scoped <see cref="ITripleStoreRepository"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> object for registration.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> object for registration.</param>
        public static IServiceCollection AddGraphModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ColidTripleStoreOptions>(configuration.GetSection(nameof(ColidTripleStoreOptions)));
            services.AddBaseGraphModule();
            services.AddScoped<ITripleStoreRepository, TripleStoreRepository>();

            return services;
        }

        /// <summary>
        /// This will register all the supported functionality by Repositories module with a singleton <see cref="ITripleStoreRepository"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>collection of service descriptors</param>
        /// <param name="configuration">The <see cref="IConfiguration"/>configuration of the application</param>
        public static IServiceCollection AddSingletonGraphModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ColidTripleStoreOptions>(configuration.GetSection(nameof(ColidTripleStoreOptions)));
            services.AddBaseGraphModule();
            services.AddSingleton<ITripleStoreRepository, TripleStoreRepository>();

            return services;
        }

        /// <summary>
        /// This will register all supported functionalities by the Graph module.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>collection of service descriptors</param>
        private static IServiceCollection AddBaseGraphModule(this IServiceCollection services)
        {
            services.AddTransient<IMetadataService, MetadataService>();
            services.AddTransient<IMetadataGraphConfigurationService, MetadataGraphConfigurationService>();

            services.AddTransient<IGraphRepository, GraphRepository>();
            services.AddTransient<IMetadataRepository, MetadataRepository>();
            services.AddTransient<IMetadataGraphConfigurationRepository, MetadataGraphConfigurationRepository>();

            return services;
        }

        /// <summary>
        /// This will register all the supported functionality by hasher module.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> object for registration.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> object for registration.</param>
        public static IServiceCollection AddHashGeneratorModule(this IServiceCollection services)
        {
            services.AddTransient<IEntityHasher, EntityHasher>();

            return services;
        }
    }
}
