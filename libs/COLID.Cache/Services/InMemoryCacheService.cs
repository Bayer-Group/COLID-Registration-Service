using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using COLID.Cache.Configuration;
using COLID.Cache.Extensions;
using COLID.Common.Utilities;
using COLID.Exception.Models.Business;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace COLID.Cache.Services
{
    internal class InMemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IHostEnvironment _environment;
        private readonly TimeSpan _configuredExpirationTime;
        private readonly ILogger<InMemoryCacheService> _logger;
        private readonly ISet<string> _keys;

        public InMemoryCacheService(
            IMemoryCache memoryCache,
            IHostEnvironment environment,
            IOptionsMonitor<ColidCacheOptions> cacheOptionsAccessor,
            ILogger<InMemoryCacheService> logger)
        {
            _memoryCache = memoryCache;
            _environment = environment;
            _configuredExpirationTime = TimeSpan.FromSeconds(cacheOptionsAccessor.CurrentValue.AbsoluteExpirationRelativeToNow);
            _logger = logger;
            _keys = new HashSet<string>();
        }

        public T GetValue<T>(string key)
        {
            Guard.ArgumentNotNullOrWhiteSpace(key, nameof(key));
            var entryKey = BuildCacheEntryKey(key);
            try
            {
                T value = _memoryCache.Get<T>(entryKey);
                _logger.LogDebug("Get key {entryKey}", entryKey);
                if (value != null)
                {
                    return value;
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (System.Exception)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                return default;
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
            _memoryCache.Set(entryKey, value, expirationTime);
            _logger.LogDebug("Added {key} with value {value}", entryKey, value);
            _keys.Add(entryKey);

            return true;
        }

        public bool Exists(string key)
        {
            Guard.ArgumentNotNullOrWhiteSpace(key, nameof(key));

            var entryKey = BuildCacheEntryKey(key);
            try
            {
                _logger.LogDebug($"Check if key exists {entryKey}", entryKey);
                return _memoryCache.TryGetValue(entryKey, out _);
            }
            catch (System.Exception ex) when (ex is NotSupportedException || ex is TimeoutException)
            {
                return false;
            }
        }

        public void Clear()
        {
            foreach (var key in _keys)
            {
                try
                {
                    _memoryCache.Remove(key);
                    _logger.LogInformation("Removed {key}", key);
                }
                catch (System.Exception ex) when (ex is NotSupportedException || ex is ArgumentNullException)
                {
                    Console.WriteLine($"Memory Cache Delete error for {key}", ex);
                }
            }
        }

        public bool TryGetValue<T>(string key, out T cachedEntry)
        {
            Guard.ArgumentNotNullOrWhiteSpace(key, nameof(key));
            var entryKey = BuildCacheEntryKey(key);
            cachedEntry = GetValue<T>(key);
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

            var entryKey = BuildCacheEntryKey(key);
            if (!TryGetValue<T>(entryKey, out var cacheEntry))
            {
                cacheEntry = await addEntry();
                Set(entryKey, cacheEntry, expirationTime);
            }
            return cacheEntry;
        }

        public async Task<T> GetOrAddAsync<T>(object o, Func<Task<T>> addEntry)
        {
            Guard.ArgumentNotNull(o, "object");
            Guard.ArgumentNotNull(addEntry, "function");

            return await GetOrAddAsync(o.CalculateHash(), addEntry);
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

            throw new EntityNotFoundException($"The following key does not exist: {entryKey}");
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
                _memoryCache.Remove(entryKey);
                _logger.LogDebug("Removed key {entryKey}", entryKey);
            }
            catch (System.Exception ex) when (ex is NotSupportedException || ex is ArgumentNullException)
            {
                Console.WriteLine($"Memory Cache Delete error for {key}", ex);
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

            foreach (var k in _keys)
            {
                if (Regex.IsMatch(k, $"{entryKey}{pattern}"))
                {
                    try
                    {
                        _memoryCache.Remove(k);
                        _logger.LogDebug("Removed key {entryKey}", entryKey);
                    }
                    catch (System.Exception ex) when (ex is NotSupportedException || ex is ArgumentNullException)
                    {
                        _logger.LogError($"Memory Cache Delete error for {k}", ex);
                    }
                }
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
    }
}
