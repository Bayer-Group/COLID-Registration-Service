﻿using System;
using System.Threading.Tasks;

namespace COLID.Cache.Services
{
    public class NoCacheService : ICacheService
    {
        public void Clear()
        {
            // do nothing
        }

        public bool Exists(string key)
        {
            return false;
        }

        public bool Exists<T>(string key, Func<T> function)
        {            
            return false;            
        }

        public T GetValue<T>(string key)
        {
            return default;
        }

        public bool Set<T>(string key, T value)
        {
            return false;
        }

        public bool Set<T>(string key, T value, TimeSpan expirationTime)
        {
            return false;
        }

        public bool TryGetValue<T>(string key, out T cachedEntry)
        {
            cachedEntry = default;
            return false;
        }

        #region GetOrAdd

        public T GetOrAdd<T>(string key, Func<T> addEntry)
        {
            return addEntry.Invoke();
        }

        public T GetOrAdd<T>(string key, Func<T> addEntry, TimeSpan expirationTime)
        {
            return addEntry.Invoke();
        }

        public T GetOrAdd<T>(object o, Func<T> addEntry)
        {
            return addEntry.Invoke();
        }

        public T GetOrAdd<T>(object o, Func<T> addEntry, TimeSpan expirationTime)
        {
            return addEntry.Invoke();
        }

        public async Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> addEntry)
        {
            var result = await addEntry.Invoke();
            return result;
        }

        public Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> addEntry, TimeSpan expirationTime)
        {
            return addEntry.Invoke();
        }

        public Task<T> GetOrAddAsync<T>(object o, Func<Task<T>> addEntry)
        {
            return addEntry.Invoke();
        }

        public Task<T> GetOrAddAsync<T>(object o, Func<Task<T>> addEntry, TimeSpan expirationTime)
        {
            return addEntry.Invoke();
        }

        #endregion GetOrAdd

        #region Update

        public T Update<T>(string key, Func<T> updateEntry)
        {
            return updateEntry.Invoke();
        }

        public T Update<T>(object o, Func<T> updateEntry)
        {
            return updateEntry.Invoke();
        }

        #endregion Update

        #region Delete

        public void Delete(string key)
        {
            // do nothing
        }

        public void Delete(object o)
        {
            // do nothing
        }

        public void Delete(string key, string pattern, bool addAppAndEnvNameToKey = true)
        {
            // do nothing
        }

        public void Delete(object o, string pattern)
        {
            // do nothing
        }

        public void Delete(string key, Action method)
        {
            // do nothing
            method.Invoke();
        }

        public void Delete(object o, Action method)
        {
            // do nothing
            method.Invoke();
        }

        #endregion Delete

        public string BuildCacheEntryKey(string suffix)
        {            
            return suffix.ToLower();
        }

        public string BuildCacheEntryKey(string suffix, Action method)
        {
            var calledClassName = method?.Target?.GetType().DeclaringType?.Name ?? method?.Target?.GetType().Name;
            return BuildCacheEntryKey($"{calledClassName}:{suffix}");
        }

        public string BuildCacheEntryKey<T>(string suffix, Func<T> function)
        {
            var calledClassName = function?.Target?.GetType().DeclaringType?.Name ?? function?.Target?.GetType().Name;
            return BuildCacheEntryKey($"{calledClassName}:{suffix}");
        }
    }
}
