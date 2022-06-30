using COLID.Cache.Models;
using COLID.Cache.Services.Lock;
using RedLockNet;
using Xunit;

namespace COLID.Cache.Tests.Services
{
    public class InMemoryLockFactoryTests
    {
        private readonly InMemoryLockFactory _lockFactory;

        public InMemoryLockFactoryTests()
        {
            _lockFactory = new InMemoryLockFactory();
        }

        [Fact]
        public void CreateLock_SingleLock_SuccessfulAquired()
        {
            var resourceName = "test_lock";
            IRedLock colidLock = _lockFactory.CreateLock(resourceName, new System.TimeSpan(0, 1, 0));
            AssertSuccessfulLock(colidLock, resourceName);
        }

        [Fact]
        public void CreateLock_MultiLock_SameResourceOneConflicted()
        {
            var resourceName = "test_lock";

            IRedLock colidLock = _lockFactory.CreateLock(resourceName, new System.TimeSpan(0, 1, 0));
            IRedLock colidLock2 = _lockFactory.CreateLock(resourceName, new System.TimeSpan(0, 1, 0));

            AssertSuccessfulLock(colidLock, resourceName);
            AssertConflictLock(colidLock2, resourceName);
        }

        [Fact]
        public void CreateLock_MultiLock_TwoResourcesNoConflicted()
        {
            var resourceName1 = "test_lock";
            var resourceName2 = "test_lock2";

            IRedLock colidLock = _lockFactory.CreateLock(resourceName1, new System.TimeSpan(0, 1, 0));
            IRedLock colidLock2 = _lockFactory.CreateLock(resourceName2, new System.TimeSpan(0, 1, 0));

            AssertSuccessfulLock(colidLock, resourceName1);
            AssertSuccessfulLock(colidLock2, resourceName2);
        }

        private void AssertSuccessfulLock(IRedLock redLock, string resourceName)
        {
            Assert.NotNull(redLock);

            Assert.Equal(resourceName, redLock.Resource);

            Assert.NotNull(redLock.LockId);
            Assert.NotEqual(string.Empty, redLock.LockId);

            Assert.True(redLock.IsAcquired);
            Assert.Equal(RedLockStatus.Acquired, redLock.Status);

            Assert.Equal(1, redLock.InstanceSummary.Acquired);
            Assert.Equal(0, redLock.InstanceSummary.Conflicted);
            Assert.Equal(0, redLock.InstanceSummary.Error);

            Assert.Equal(1, redLock.ExtendCount);
        }

        private void AssertConflictLock(IRedLock redLock, string resourceName)
        {
            Assert.NotNull(redLock);

            Assert.Equal(resourceName, redLock.Resource);

            Assert.NotNull(redLock.LockId);
            Assert.NotEqual(string.Empty, redLock.LockId);

            Assert.False(redLock.IsAcquired);
            Assert.Equal(RedLockStatus.Conflicted, redLock.Status);

            Assert.Equal(0, redLock.InstanceSummary.Acquired);
            Assert.Equal(1, redLock.InstanceSummary.Conflicted);
            Assert.Equal(1, redLock.InstanceSummary.Error);

            Assert.Equal(1, redLock.ExtendCount);
        }
    }
}
