using System;
using System.Threading.Tasks;
using COLID.Cache.Configuration;
using COLID.Cache.Exceptions;
using COLID.Cache.Extensions;
using COLID.Common.Utilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace COLID.Cache.Services
{
    internal class CacheService : ICacheService
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly IDatabase _cache;
        private readonly IHostEnvironment _environment;
        private readonly TimeSpan _configuredExpirationTime;
        private readonly CachingJsonSerializerSettings _serializerSettings;
        private readonly ILogger<CacheService> _logger;

        public CacheService(
            IConnectionMultiplexer connectionMultiplexer,
            IOptionsMonitor<ColidCacheOptions> cacheOptions,
            IHostEnvironment environment,
            CachingJsonSerializerSettings serializerSettings,
            ILogger<CacheService> logger)
        {
            _environment = environment;
            _configuredExpirationTime = TimeSpan.FromSeconds(cacheOptions.CurrentValue.AbsoluteExpirationRelativeToNow);
            _serializerSettings = serializerSettings;
            _connectionMultiplexer = connectionMultiplexer;
            _cache = _connectionMultiplexer.GetDatabase();
            _logger = logger;
        }

        public T GetValue<T>(string key)
        {
            Guard.ArgumentNotNullOrWhiteSpace(key, nameof(key));

            var entryKey = BuildCacheEntryKey(key);
            try
            {
                string value = _cache.StringGet(entryKey);
                _logger.LogDebug("Get key {entryKey}", entryKey);
                if (value != null)
                {
                    return JsonConvert.DeserializeObject<T>(value, _serializerSettings);
                }
            }
            catch (System.Exception ex) when (ex is RedisException || ex is TimeoutException)
            {
                HandleException(ex);
            }

            return default;
        }

        public bool Set<T>(string key, T value)
        {
            return Set(key, value, _configuredExpirationTime);
        }

        public bool Set<T>(string key, T value, TimeSpan expirationTime)
        {
            Guard.ArgumentNotNullOrWhiteSpace(key, nameof(key));
            Guard.ArgumentNotNull(value, nameof(value));
            Guard.ArgumentNotNull(expirationTime, nameof(expirationTime));

            var entryKey = BuildCacheEntryKey(key);
            try
            {
                _cache.StringSet(entryKey, JsonConvert.SerializeObject(value, _serializerSettings), expirationTime);
                _logger.LogDebug($"Set key {entryKey}", entryKey);
                return true;
            }
            catch (System.Exception ex) when (ex is RedisException || ex is TimeoutException)
            {
                HandleException(ex);
            }
            return false;
        }

        public bool Exists(string key)
        {
            Guard.ArgumentNotNullOrWhiteSpace(key, nameof(key));
            
            var entryKey = BuildCacheEntryKey(key); 
            try
            {
                _logger.LogDebug($"Check if key exists {entryKey}", entryKey);
                return _cache.KeyExists(entryKey);
            }
            catch (System.Exception ex) when (ex is RedisException || ex is TimeoutException)
            {
                HandleException(ex);
            }
            return false;
        }

        public void Clear()
        {
            try
            {
                var endpoints = _connectionMultiplexer.GetEndPoints();
                foreach (var endpoint in endpoints)
                {
                    var server = _connectionMultiplexer.GetServer(endpoint);
                    server.FlushAllDatabases();
                }
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                _logger.LogDebug("Cache cleared");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }
            catch (System.Exception ex) when (ex is RedisException || ex is TimeoutException)
            {
                HandleException(ex);
            }
        }

        public bool TryGetValue<T>(string key, out T cachedEntry)
        {
            Guard.ArgumentNotNullOrWhiteSpace(key, nameof(key));
            var entryKey = BuildCacheEntryKey(key);
            cachedEntry = GetValue<T>(entryKey);
            return cachedEntry != null;
        }

        #region GetOrAdd

        public T GetOrAdd<T>(string key, Func<T> addEntry)
        {
            return GetOrAdd(key, addEntry, _configuredExpirationTime);
        }

        public T GetOrAdd<T>(string key, Func<T> addEntry, TimeSpan expirationTime)
        {
            Guard.ArgumentNotNullOrWhiteSpace(key, nameof(key));
            Guard.ArgumentNotNull(addEntry, "function");
            Guard.ArgumentNotNull(expirationTime, nameof(expirationTime));

            var entryKey = BuildCacheEntryKey(key, addEntry);
            if (TryGetValue<T>(entryKey, out var cacheEntry))
            {
                return cacheEntry;
            }

            cacheEntry = addEntry.Invoke();
            Set(entryKey, cacheEntry, expirationTime);
            return cacheEntry;
        }

        public T GetOrAdd<T>(object o, Func<T> addEntry)
        {
            return GetOrAdd(o.CalculateHash(), addEntry, _configuredExpirationTime);
        }

        public T GetOrAdd<T>(object o, Func<T> addEntry, TimeSpan expirationTime)
        {
            Guard.ArgumentNotNull(o, "object");
            Guard.ArgumentNotNull(addEntry, "function");
            Guard.ArgumentNotNull(expirationTime, nameof(expirationTime));

            return GetOrAdd(o.CalculateHash(), addEntry, expirationTime);
        }

        public async Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> addEntry)
        {
            var result = await GetOrAddAsync(key, addEntry, _configuredExpirationTime);
            return result;
        }

        public async Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> addEntry, TimeSpan expirationTime)
        {
            Guard.ArgumentNotNullOrWhiteSpace(key, nameof(key));
            Guard.ArgumentNotNull(addEntry, "function");
            Guard.ArgumentNotNull(expirationTime, nameof(expirationTime));

            var entryKey = BuildCacheEntryKey(key, addEntry);
            if (!TryGetValue<T>(entryKey, out var cacheEntry))
            {
                cacheEntry = await addEntry.Invoke();
                Set(entryKey, cacheEntry, expirationTime);
            }
            return cacheEntry;
        }

        public async Task<T> GetOrAddAsync<T>(object o, Func<Task<T>> addEntry)
        {
            Guard.ArgumentNotNull(o, "object");
            Guard.ArgumentNotNull(addEntry, "function");

            var result = await GetOrAddAsync(o.CalculateHash(), addEntry);
            return result;
        }

        public async Task<T> GetOrAddAsync<T>(object o, Func<Task<T>> addEntry, TimeSpan expirationTime)
        {
            Guard.ArgumentNotNull(o, "object");
            Guard.ArgumentNotNull(addEntry, "function");
            Guard.ArgumentNotNull(expirationTime, nameof(expirationTime));

            var result = await GetOrAddAsync(o.CalculateHash(), addEntry, expirationTime);
            return result;
        }

        #endregion GetOrAdd

        #region Update

        public T Update<T>(string key, Func<T> updateEntry)
        {
            Guard.ArgumentNotNullOrWhiteSpace(key, nameof(key));
            Guard.ArgumentNotNull(updateEntry, "function");

            var entryKey = BuildCacheEntryKey(key, updateEntry);
            if (TryGetValue<T>(entryKey, out var cacheEntry))
            {
                cacheEntry = updateEntry.Invoke();
                Set(entryKey, cacheEntry);
                _logger.LogDebug($"Update value for key {entryKey}", entryKey);
                return cacheEntry;
            }

            // TODO: what if key doesn't exist?
            throw new RedisKeyNotFoundException($"The following key does not exist: {entryKey}");
        }

        public T Update<T>(object o, Func<T> updateEntry)
        {
            Guard.ArgumentNotNull(o, "object");
            Guard.ArgumentNotNull(updateEntry, "function");

            return Update(o.CalculateHash(), updateEntry);
        }

        #endregion Update

        #region Delete

        public void Delete(string key)
        {
            Guard.ArgumentNotNullOrWhiteSpace(key, nameof(key));

            var entryKey = BuildCacheEntryKey(key);
            try
            {
                _cache.KeyDelete(entryKey);
                _logger.LogDebug($"Removed key {entryKey}", entryKey);
            }
            catch (System.Exception ex) when (ex is RedisException || ex is TimeoutException)
            {
                HandleException(ex);
            }
        }

        public void Delete(object o)
        {
            Guard.ArgumentNotNull(o, "object");

            Delete(o.CalculateHash());
        }

        public void Delete(string key, string pattern, bool addAppAndEnvNameToKey = true)
        {
            Guard.ArgumentNotNullOrWhiteSpace(pattern, nameof(pattern));
            if (!addAppAndEnvNameToKey)
            {
                Guard.ArgumentNotNullOrWhiteSpace(key, nameof(key));
            }

            var entryKey = addAppAndEnvNameToKey ? BuildCacheEntryKey(key) : key;

            try
            {
                var endpoints = _connectionMultiplexer.GetEndPoints();
                foreach (var ep in endpoints)
                {
                    var server = _connectionMultiplexer.GetServer(ep);

                    foreach (var foundKey in server.Keys(pattern: $"{entryKey}:{pattern.ToLower()}"))
                    {
                        _cache.KeyDelete(foundKey);
                        _logger.LogDebug($"Removed key {entryKey}", entryKey);
                    }
                }
            }
            catch (System.Exception ex) when (ex is RedisException || ex is TimeoutException)
            {
                HandleException(ex);
            }
        }

        public void Delete(object o, string pattern)
        {
            Guard.ArgumentNotNull(o, "object");
            Guard.ArgumentNotNullOrWhiteSpace(pattern, nameof(pattern));

            Delete(o.CalculateHash(), pattern);
        }

        public void Delete(string key, Action method)
        {
            Guard.ArgumentNotNullOrWhiteSpace(key, nameof(key));
            Guard.ArgumentNotNull(method, "action");

            var entryKey = BuildCacheEntryKey(key, method);
            Delete(entryKey);
            method.Invoke();
        }

        public void Delete(object o, Action method)
        {
            Guard.ArgumentNotNull(o, "object");
            Guard.ArgumentNotNull(method, "action");

            Delete(o.CalculateHash(), method);
        }

        #endregion Delete

        #region BuildCacheEntryKey

        /// <summary>
        /// Create the correct name for a cache key, based on the ApplicationName and EnvironmentName variables.
        /// </summary>
        /// <param name="suffix">the key suffix, appended to prefix</param>
        private string BuildCacheEntryKey(string suffix)
        {
            var prefix = $"{_environment.ApplicationName}:{_environment.EnvironmentName}".ToLower()
                .Replace(".webapi", "")
                .Replace(".", ":");

            if (!suffix.Contains(prefix))
            {
                return $"{prefix}:{suffix}".ToLower();
            }

            return suffix.ToLower();
        }


        private string BuildCacheEntryKey(string suffix, Action method)
        {
            var calledClassName = method?.Target?.GetType().DeclaringType?.Name ?? method?.Target?.GetType().Name;
            return BuildCacheEntryKey($"{calledClassName}:{suffix}");
        }

        private string BuildCacheEntryKey<T>(string suffix, Func<T> function)
        {
            var calledClassName = function?.Target?.GetType().DeclaringType?.Name ?? function?.Target?.GetType().Name;
            return BuildCacheEntryKey($"{calledClassName}:{suffix}");
        }

        #endregion BuildCacheEntryKey

        private void HandleException(System.Exception ex)
        {
            switch (ex)
            {
                case RedisConnectionException _:
                case RedisServerException _:
                    _logger.LogError($"Error occured while connecting to Redis: {ex.Message}", ex);
                    break;

                case RedisTimeoutException _:
                    _logger.LogError($"Error occured while interacting with Redis (timeout): {ex.Message}", ex);
                    break;

                case RedisException _:
                    _logger.LogError($"Unknown error occured from Redis: {ex.Message}", ex);
                    break;

                case System.Exception _:
                    _logger.LogError("Unknown error occured:", ex.InnerException);
                    break;
            }
        }
    }
}
