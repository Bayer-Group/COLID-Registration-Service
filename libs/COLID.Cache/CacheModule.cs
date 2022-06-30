using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using COLID.Cache.Configuration;
using COLID.Cache.Services;
using COLID.Cache.Services.Lock;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;

namespace COLID.Cache
{
    public static class CacheModule
    {
        public static IServiceCollection AddCacheModule(this IServiceCollection services, IConfiguration configuration,
            CachingJsonSerializerSettings cachingJsonSerializerSettings)
        {

            var cacheOptions = ParseCachingConfiguration(configuration);

            if (!cacheOptions.Enabled)
            {
                return AddNoCacheModule(services);
            }

            if (cacheOptions.UseInMemory)
            {
                return AddMemoryCacheModule(services, configuration, cachingJsonSerializerSettings);
            }

            return AddDistributedCacheModule(services, configuration, cachingJsonSerializerSettings);
        }

        /// <summary>
        /// This will register distributed cache functionality.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> object for registration.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> object for registration.</param>
        public static IServiceCollection AddDistributedCacheModule(this IServiceCollection services, IConfiguration configuration, CachingJsonSerializerSettings cachingJsonSerializerSettings)
        {
            Contract.Requires(configuration != null && cachingJsonSerializerSettings != null);

            var cacheOptions = ParseCachingConfiguration(configuration);
            if (!cacheOptions.Enabled)
            {
                return AddNoCacheModule(services);
            }

            services.AddSingleton(cachingJsonSerializerSettings);
            services.AddSingleton<ICacheService, CacheService>();
            services.Configure<ColidCacheOptions>(configuration.GetSection(nameof(ColidCacheOptions)));

            var connectionMultiplexer = CreateConnectionMultiplexer(configuration);
            services.AddSingleton<IConnectionMultiplexer>(connectionMultiplexer);

            return services;
        }

        /// <summary>
        /// This will register memory cache functionality.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> object for registration.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> object for registration.</param>
        public static IServiceCollection AddMemoryCacheModule(this IServiceCollection services, IConfiguration configuration, CachingJsonSerializerSettings cachingJsonSerializerSettings)
        {
            Contract.Requires(configuration != null && cachingJsonSerializerSettings != null);

            var cacheOptions = ParseCachingConfiguration(configuration);
            if (!cacheOptions.Enabled)
            {
                return AddNoCacheModule(services);
            }

            services.AddMemoryCache();
            services.AddSingleton(cachingJsonSerializerSettings);
            services.AddSingleton<ICacheService, InMemoryCacheService>();
            services.Configure<ColidCacheOptions>(configuration.GetSection(nameof(ColidCacheOptions)));
#pragma warning disable CA1303 // Do not pass literals as localized parameters
            Console.WriteLine($"- Caching: InMemory");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            return services;
        }

        /// <summary>
        /// This will register memory cache functionality.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> object for registration.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> object for registration.</param>
        public static IServiceCollection AddNoCacheModule(this IServiceCollection services)
        {
            services.AddSingleton<ICacheService, NoCacheService>();
#pragma warning disable CA1303 // Do not pass literals as localized parameters
            Console.WriteLine($"- Caching: Disabled");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            return services;
        }

        private static ConnectionMultiplexer CreateConnectionMultiplexer(IConfiguration configuration)
        {
            var cacheOptions = ParseCachingConfiguration(configuration);

            var configOptions = new ConfigurationOptions()
            {
                AbortOnConnectFail = cacheOptions.AbortOnConnectFail,
                SyncTimeout = cacheOptions.SyncTimeout,
                KeepAlive = cacheOptions.KeepAlive,
                ReconnectRetryPolicy = new LinearRetry(cacheOptions.ReconnectRetryPolicy),
                SocketManager = new SocketManager("IncreasedWorkerCount", 100, true),
                Password = cacheOptions.Password,
                AllowAdmin = cacheOptions.AllowAdmin,
                Ssl = cacheOptions.Ssl
            };

            foreach (var endpoint in cacheOptions.EndpointUrls)
            {
                configOptions.EndPoints.Add(endpoint);
            }

            var connectionMultiplexer = ConnectionMultiplexer.Connect(configOptions);

            Console.WriteLine($"- Caching: Distributed ({configOptions.EndPoints})");
            return connectionMultiplexer;
        }

        private static ColidCacheOptions ParseCachingConfiguration(IConfiguration configuration)
        {
            var cacheOptions = new ColidCacheOptions();
            configuration.Bind(nameof(ColidCacheOptions), cacheOptions);

            return cacheOptions;
        }

        #region Lock

        /// <summary>
        /// This will register lock functionality.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> object for registration.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> object for registration.</param>
        public static IServiceCollection AddLockModule(this IServiceCollection services, IConfiguration configuration)
        {
            Contract.Requires(condition: configuration != null);

            services.AddSingleton<IDistributedLockFactory, InMemoryLockFactory>();
            services.AddTransient<ILockServiceFactory, LockServiceFactory>();

            return services;
        }

        /// <summary>
        /// This will register distributed lock functionality.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> object for registration.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> object for registration.</param>
        public static IServiceCollection AddDistributedLockModule(this IServiceCollection services, IConfiguration configuration)
        {
            Contract.Requires(condition: configuration != null);

            var connectionMultiplexer = CreateConnectionMultiplexer(configuration);
            var multiplexers = new List<RedLockMultiplexer>
            {
                connectionMultiplexer
            };

#pragma warning disable CA2000 // Dispose objects before losing scope
            services.AddSingleton<IDistributedLockFactory>(RedLockFactory.Create(multiplexers));
#pragma warning restore CA2000 // Dispose objects before losing scope
            services.AddTransient<ILockServiceFactory, LockServiceFactory>();

            return services;
        }

        #endregion Lock
    }
}
