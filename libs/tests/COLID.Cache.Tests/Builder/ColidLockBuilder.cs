using System;
using COLID.Cache.Models;
using RedLockNet;

namespace COLID.RegistrationService.Tests.Common.Builder
{
    public class ColidLockBuilder
    {
        private ColidLock _lock = new ColidLock();

        public ColidLock Build()
        {
            return _lock;
        }

        /// <summary>
        /// <b>Caution</b>: may override existing content, use it right after:
        /// <code>new KeywordBuilder().GenerateSampleData().With(...)</code>
        /// </summary>
        public ColidLockBuilder GenerateSampleData()
        {
            WithResource("test_data");
            WithLockId(Guid.NewGuid().ToString());
            WithIsAcquired(true);
            WithStatus(RedLockStatus.Acquired);
            WithInstanceSummary(new RedLockInstanceSummary(1, 0, 0));
            WithExtendCount(0);

            return this;
        }

        public ColidLockBuilder WithResource(string resource)
        {
            _lock.Resource = resource;
            return this;
        }

        public ColidLockBuilder WithLockId(string id)
        {
            _lock.LockId = id;
            return this;
        }

        public ColidLockBuilder WithIsAcquired(bool isAquired)
        {
            _lock.IsAcquired = isAquired;
            return this;
        }

        public ColidLockBuilder WithStatus(RedLockStatus status)
        {
            _lock.Status = status;
            return this;
        }

        public ColidLockBuilder WithInstanceSummary(RedLockInstanceSummary instanceSummary)
        {
            _lock.InstanceSummary = instanceSummary;
            return this;
        }

        public ColidLockBuilder WithExtendCount(int extendCount)
        {
            _lock.ExtendCount = extendCount;
            return this;
        }
    }
}
