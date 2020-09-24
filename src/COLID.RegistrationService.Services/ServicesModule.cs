using Amazon;
using Amazon.S3;
using AutoMapper;
using COLID.Cache;
using COLID.Graph.Metadata.Services;
using COLID.Graph.TripleStore.AWS;
using COLID.Graph.TripleStore.DataModels.AWS;
using COLID.Graph.TripleStore.MappingProfiles;
using COLID.MessageQueue.Services;
using COLID.RegistrationService.Services.Configuration;
using COLID.RegistrationService.Services.Implementation;
using COLID.RegistrationService.Services.Implementation.Comparison;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.Services.MappingProfiles;
using COLID.RegistrationService.Services.Validation;
using COLID.RegistrationService.Services.Validation.Validators;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace COLID.RegistrationService.Services
{
    public static class ServicesModule
    {
        /// <summary>
        /// This will register all the supported functionality by Repositories module.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> object for registration.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> object for registration.</param>
        public static IServiceCollection AddServicesModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddBaseServicesModule(configuration);
            services.AddDistributedLockModule(configuration);
            return services;
        }
        
        /// <summary>
        /// This will register all the supported functionality by Repositories module.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> object for registration.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> object for registration.</param>
        public static IServiceCollection AddDebugServicesModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddBaseServicesModule(configuration);
            services.AddLockModule(configuration);

            return services;
        }
        
        /// <summary>
        /// This will register all the supported functionality by Repositories module.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> object for registration.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> object for registration.</param>
        public static IServiceCollection AddDockerServicesModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddBaseServicesModule(configuration);
            services.AddDistributedLockModule(configuration);

            return services;
        }
        
        /// <summary>
        /// This will register all the supported functionality by Repositories module.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> object for registration.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> object for registration.</param>
        public static IServiceCollection AddLocalServicesModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddBaseServicesModule(configuration);
            services.AddLockModule(configuration);

            return services;
        }
        
        /// <summary>
        /// This will register all the supported functionality by Repositories module.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> object for registration.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> object for registration.</param>
        public static IServiceCollection AddBaseServicesModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAutoMapper(
                typeof(ResourceProfile),
                typeof(ConsumerGroupProfile),
                typeof(EntityWithPidUriTemplateTypeCheckProfile),
                typeof(ExtendedUriTemplateProfile),
                typeof(MetadataGraphConfigurationProfile),
                typeof(KeywordProfile),
                typeof(MetadataPropertyProfile),
                typeof(PidUriTemplateProfile));

            services.Configure<ColidAppDataServiceTokenOptions>(configuration.GetSection("ColidAppDataServiceTokenOptions"));
            services.Configure<ColidIndexingCrawlerServiceTokenOptions>(configuration.GetSection("ColidIndexingCrawlerServiceTokenOptions"));
            services.Configure<AmazonWebServicesOptions>(configuration.GetSection("AmazonWebServicesOptions"));

            services.AddTransient<IStatusService, StatusService>();

            services.AddSingleton<ReindexingService>();
            services.AddSingleton<IReindexingService>(x => x.GetRequiredService<ReindexingService>());
            services.AddSingleton<IMessageQueuePublisher>(x => x.GetRequiredService<ReindexingService>());

            services.AddTransient<IResourceService, ResourceService>();
            services.AddTransient<IResourceLinkingService, ResourceLinkingService>();
            services.AddTransient<IConsumerGroupService, ConsumerGroupService>();
            services.AddTransient<IPidUriTemplateService, PidUriTemplateService>();
            services.AddTransient<IExtendedUriTemplateService, ExtendedUriTemplateService>();
            services.AddTransient<IProxyConfigService, ProxyConfigService>();
            services.AddTransient<IResourcePreprocessService, ResourcePreprocessService>();
            services.AddTransient<IEntityService, EntityService>();
            services.AddTransient<IKeywordService, KeywordService>();
            services.AddTransient<IIdentifierService, IdentifierService>();
            services.AddTransient<IDistributionEndpointService, DistributionEndpointService>();
            services.AddTransient<IHistoricResourceService, HistoricResourceService>();
            services.AddTransient<ITaxonomyService, TaxonomyService>();
            services.AddTransient<IGraphManagementService, GraphManagementService>();

            services.AddTransient<IEntityPropertyValidator, EntityPropertyValidator>();

            services.AddTransient<IRemoteAppDataService, RemoteAppDataService>();
            services.AddTransient<IValidationService, ValidationService>();
            services.AddTransient<IIdentifierValidationService, IdentifierValidationService>();

            services.AddTransient<IAmazonS3Service, AmazonS3Service>();

            services.AddTransient<IResourceComparisonService, ResourceComparisonService>();
            services.AddTransient<IDifferenceCalculationService, DifferenceCalculationService>();
            services.AddTransient<ISimilarityCalculationService, SimilarityCalculationService>();

            // Must be injected as the only instance of a request so that all generated identifiers are stored in the state of the service.
            // TODO: What the heck? Refactor with new Microservice for PID URI Generation
            services.AddScoped<IPidUriGenerationService, PidUriGenerationService>();

            return services;
        }
    }
}
