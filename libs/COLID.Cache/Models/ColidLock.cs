using System;
using RedLockNet;

namespace COLID.Cache.Models
{
    public class ColidLock : IRedLock
    {
        /// <summary>
        /// The name of the resource the lock is for.
        /// </summary>
        public string Resource { get; set; }

        /// <summary>
        /// The unique identifier assigned to this lock.
        /// </summary>
        public string LockId { get; set; }

        /// <summary>
        /// Whether the lock has been acquired.
        /// </summary>
        public bool IsAcquired { get; set; }

        /// <summary>
        /// The status of the lock.
        /// </summary>
        public RedLockStatus Status { get; set; }

        /// <summary>
        /// Details of the number of instances the lock was able to be acquired in.
        /// </summary>
        public RedLockInstanceSummary InstanceSummary { get; set; }

        /// <summary>
        /// The number of times the lock has been extended.
        /// </summary>
        public int ExtendCount { get; set; }

        /// <summary>
        /// Action to be taken when the object is disposed of
        /// </summary>
        private Action<string> _disposeAction { get; }

        /// <summary>
        /// Whether the lock has been disposed.
        /// </summary>
        private bool _disposed;

        public ColidLock(string resource, string lockId, bool isAcquired, RedLockStatus status, RedLockInstanceSummary instanceSummary, int extendCount, Action<string> disposeAction)
        {
            Resource = resource;
            LockId = lockId;
            IsAcquired = isAcquired;
            Status = status;
            InstanceSummary = instanceSummary;
            ExtendCount = extendCount;
            _disposeAction = disposeAction;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _disposeAction.Invoke(Resource);
            }
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
