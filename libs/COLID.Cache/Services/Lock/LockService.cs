using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using COLID.Cache.Exceptions;
using RedLockNet;

namespace COLID.Cache.Services.Lock
{
    public class LockService: ILockService
    {
        private bool _disposed;
        private readonly IDictionary<string,IRedLock> _locks;
        private readonly IDistributedLockFactory _lockFactory;
        private readonly TimeSpan _defaultExpireTime = TimeSpan.FromSeconds(10);

        public LockService(IDistributedLockFactory lockFactory)
        {
            _locks = new Dictionary<string, IRedLock>();
            _lockFactory = lockFactory;
        }

        public ILockService CreateLock(string resource)
        {
            return CreateLock(resource, _defaultExpireTime);
        }

        public ILockService CreateLock(string resource, TimeSpan expiryTime)
        {
            var redLock = _lockFactory.CreateLock(resource, expiryTime);
            HandleRedLock(resource, redLock);
            return this;
        }

        public ILockService CreateLock(string resource, TimeSpan expiryTime, TimeSpan waitTime, TimeSpan retryTime)
        {
            var redLock = _lockFactory.CreateLock(resource, expiryTime, waitTime, retryTime);
            HandleRedLock(resource, redLock);
            return this;
        }

        public ILockService CreateLock(string resource, TimeSpan expiryTime, TimeSpan waitTime, TimeSpan retryTime, CancellationToken cancellationToken)
        {
            var redLock = _lockFactory.CreateLock(resource, expiryTime, waitTime, retryTime, cancellationToken);
            HandleRedLock(resource, redLock);
            return this;
        }

        public Task<ILockService> CreateLockAsync(string resource)
        {
            return CreateLockAsync(resource, _defaultExpireTime);
        }

        public async Task<ILockService> CreateLockAsync(string resource, TimeSpan expiryTime)
        {
            var redLock = await _lockFactory.CreateLockAsync(resource, expiryTime);
            HandleRedLock(resource, redLock);
            return this;
        }

        public async Task<ILockService> CreateLockAsync(string resource, TimeSpan expiryTime, TimeSpan waitTime, TimeSpan retryTime)
        {
            var redLock = await _lockFactory.CreateLockAsync(resource, expiryTime, waitTime, retryTime);
            HandleRedLock(resource, redLock);
            return this;
        }

        public async Task<ILockService> CreateLockAsync(string resource, TimeSpan expiryTime, TimeSpan waitTime, TimeSpan retryTime, CancellationToken cancellationToken)
        {
            var redLock = await _lockFactory.CreateLockAsync(resource, expiryTime, waitTime, retryTime, cancellationToken);
            HandleRedLock(resource, redLock);
            return this;
        }

        public void ReleaseLock(string resource)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("Locks already released");
            }

            // May throw a KeyNotFoundException that is correct at this point.
            // It tries to release a lock that does not exist.
            _locks[resource].Dispose();
            _locks.Remove(resource);
        }

        private void HandleRedLock(string resource, IRedLock redLock)
        {
            if (redLock.IsAcquired)
            {
                _locks.Add(resource, redLock);
            }
            else
            {
                Dispose();
                throw new ResourceLockedException($"Resource with id '{resource}' already locked", redLock);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing && _locks.Any())
                {
                    foreach (var redLock in _locks)
                    {
                        redLock.Value.Dispose();
                    }
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
