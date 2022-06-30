using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using COLID.Cache.Models;
using RedLockNet;

namespace COLID.Cache.Services.Lock
{
    /// <summary>
    /// Factory to wrap the original redlock.net factory to provide locks without distributed cache as well
    /// </summary>
    public class InMemoryLockFactory : IDistributedLockFactory
    {
        /// <summary>
        /// All locks created by the instance of the class
        /// </summary>
        private readonly Dictionary<string, IRedLock> _locks;

        public InMemoryLockFactory()
        {
            _locks = new Dictionary<string, IRedLock>();
        }

        public IRedLock CreateLock(string resource, TimeSpan expiryTime)
        {
            return CreateLock(resource);
        }

        public IRedLock CreateLock(string resource, TimeSpan expiryTime, TimeSpan waitTime, TimeSpan retryTime, CancellationToken? cancellationToken = null)
        {
            return CreateLock(resource);
        }

        public Task<IRedLock> CreateLockAsync(string resource, TimeSpan expiryTime)
        {
            return Task.FromResult(CreateLock(resource));
        }

        public Task<IRedLock> CreateLockAsync(string resource, TimeSpan expiryTime, TimeSpan waitTime, TimeSpan retryTime, CancellationToken? cancellationToken = null)
        {
            return Task.FromResult(CreateLock(resource));
        }

        /// <summary>
        /// Creates a colid lock
        /// </summary>
        /// <param name="resource">The resource string to lock on</param>
        /// <returns>colid lock</returns>
        private IRedLock CreateLock(string resource)
        {
            var lockId = Guid.NewGuid().ToString();
            var acquired = IsAcquired(resource);
            var redLockStatus = GetRedLockStatus(resource);
            var lockError = acquired ? 0 : 1;

            var redLockInstanceSummary = new RedLockInstanceSummary(acquired ? 1 : 0, lockError, lockError);

            var colidLock = new ColidLock(resource, lockId, !_locks.ContainsKey(resource), redLockStatus, redLockInstanceSummary, 1, ReleaseLock);
            
            if (acquired)
            {
                _locks.TryAdd(resource, colidLock);
            }

            return colidLock;
        }

        private bool IsAcquired(string resource)
        {
            if (_locks.TryGetValue(resource, out var redLock))
            {
                return redLock.Status != RedLockStatus.Acquired;
            }
            return true;
        }

        private RedLockStatus GetRedLockStatus(string resource)
        {
            if (_locks.TryGetValue(resource, out var redLock))
            {
                return redLock.Status == RedLockStatus.Acquired ? RedLockStatus.Conflicted : RedLockStatus.Acquired;
            }
            return  RedLockStatus.Acquired;
        }

        /// <summary>
        /// Released a lock for given resource string
        /// </summary>
        /// <param name="resource">The locked resource string</param>
        private void ReleaseLock(string resource)
        {
            this._locks.Remove(resource);
        }
    }
}
