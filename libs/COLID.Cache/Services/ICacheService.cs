using System;
using System.Threading.Tasks;
using COLID.Common.Utilities;

namespace COLID.Cache.Services
{
    public interface ICacheService
    {
        T GetValue<T>(string key);
        bool TryGetValue<T>(string key, out T cachedEntry);
        bool Set<T>(string key, T value);
        bool Set<T>(string key, T value, TimeSpan expirationTime);
        bool Exists(string key);
        void Clear();

        #region GetOrAdd
        T GetOrAdd<T>(string key, Func<T> addEntry);
        T GetOrAdd<T>(string key, Func<T> addEntry, TimeSpan expirationTime);
        T GetOrAdd<T>(object o, Func<T> addEntry);
        T GetOrAdd<T>(object o, Func<T> addEntry, TimeSpan expirationTime);
        Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> addEntry);
        Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> addEntry, TimeSpan expirationTime);
        Task<T> GetOrAddAsync<T>(object o, Func<Task<T>> addEntry);
        Task<T> GetOrAddAsync<T>(object o, Func<Task<T>> addEntry, TimeSpan expirationTime);
        #endregion

        #region Update
        public T Update<T>(string key, Func<T> updateEntry);
        public T Update<T>(object o, Func<T> updateEntry);
        #endregion

        #region Delete
        void Delete(string key);
        void Delete(object o);
        void Delete(string key, string pattern, bool addAppAndEnvNameToKey = true);
        void Delete(object o, string pattern);
        void Delete(string key, Action method);
        void Delete(object o, Action method);
        #endregion

    }
}
