using System;
using System.Threading;
using System.Threading.Tasks;

namespace COLID.Cache.Services.Lock
{
    /// <summary>
    /// Service to lock shared resources
    /// </summary>
    public interface ILockService: IDisposable
    {
        /// <summary>
        /// Try to create a lock for given resource and return a red lock.
        /// Blocks and retries up to the specified time limits.
        /// </summary>
        /// <param name="resource">The resource string to lock on. Only one lock should be acquired for any given resource at once.</param>
        /// <returns><see cref="ILockService"/></returns>
        public ILockService CreateLock(string resource);

        /// <summary>
        /// Try to create a lock for given resource and return a red lock.
        /// Blocks and retries up to the specified time limits.
        /// </summary>
        /// <param name="resource">The resource string to lock on</param>
        /// <param name="expiryTime">Time after lock expires</param>
        /// <returns><see cref="ILockService"/></returns>
        public ILockService CreateLock(string resource, TimeSpan expiryTime);

        /// <summary>
        /// Try to create a lock for given resource and return a red lock.
        /// Blocks and retries up to the specified time limits.
        /// </summary>
        /// <param name="resource">The resource string to lock on</param>
        /// <param name="expiryTime">Time after lock expires</param>
        /// <param name="waitTime">Time to wait between lock attempts</param>
        /// <param name="retryTime">Total time spent trying to lock resource</param>
        /// <returns><see cref="ILockService"/></returns>
        public ILockService CreateLock(string resource, TimeSpan expiryTime, TimeSpan waitTime, TimeSpan retryTime);

        /// <summary>
        /// Try to create a lock for given resource and return a red lock.
        /// Blocks and retries up to the specified time limits.
        /// </summary>
        /// <param name="resource">The resource string to lock on</param>
        /// <param name="expiryTime">Time after lock expires</param>
        /// <param name="waitTime">Time to wait between lock attempts</param>
        /// <param name="retryTime">Total time spent trying to lock resource</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        /// <returns><see cref="ILockService"/></returns>
        public ILockService CreateLock(string resource, TimeSpan expiryTime, TimeSpan waitTime, TimeSpan retryTime,
            CancellationToken cancellationToken);

        /// <summary>
        /// Try to create a lock for given resource and return a red lock.
        /// Blocks and retries up to the specified time limits.
        /// </summary>
        /// <param name="resource">The resource string to lock on</param>
        /// <returns><see cref="ILockService"/></returns>
        public Task<ILockService> CreateLockAsync(string resource);

        /// <summary>
        /// Try to create a lock for given resource and return a red lock.
        /// Blocks and retries up to the specified time limits.
        /// </summary>
        /// <param name="resource">The resource string to lock on</param>
        /// <param name="expiryTime">Time after lock expires</param>
        /// <returns><see cref="ILockService"/></returns>
        public Task<ILockService> CreateLockAsync(string resource, TimeSpan expiryTime);

        /// <summary>
        /// Try to create a lock for given resource and return a red lock.
        /// Blocks and retries up to the specified time limits.
        /// </summary>
        /// <param name="resource">The resource string to lock on</param>
        /// <param name="expiryTime">Time after lock expires</param>
        /// <param name="waitTime">Time to wait between lock attempts</param>
        /// <param name="retryTime">Total time spent trying to lock resource</param>
        /// <returns><see cref="ILockService"/></returns>
        public Task<ILockService> CreateLockAsync(string resource, TimeSpan expiryTime, TimeSpan waitTime,
            TimeSpan retryTime);

        /// <summary>
        /// Try to create a lock for given resource and return a red lock.
        /// Blocks and retries up to the specified time limits.
        /// </summary>
        /// <param name="resource">The resource string to lock on</param>
        /// <param name="expiryTime">Time after lock expires</param>
        /// <param name="waitTime">Time to wait between lock attempts</param>
        /// <param name="retryTime">Total time spent trying to lock resource</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        /// <returns><see cref="ILockService"/></returns>
        public Task<ILockService> CreateLockAsync(string resource, TimeSpan expiryTime, TimeSpan waitTime,
            TimeSpan retryTime, CancellationToken cancellationToken);

        /// <summary>
        /// Release a lock for given resource
        /// </summary>
        /// <param name="resource">Resource the lock should be removed for</param>
        public void ReleaseLock(string resource);
    }
}
