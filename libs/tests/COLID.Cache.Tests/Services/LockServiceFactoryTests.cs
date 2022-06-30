using COLID.Cache.Models;
using COLID.Cache.Services.Lock;
using Moq;
using RedLockNet;
using Xunit;

namespace COLID.Cache.Tests.Services
{
    public class LockServiceFactoryTests
    {
        private readonly LockServiceFactory _lockFactory;

        public LockServiceFactoryTests()
        {
            Mock<IDistributedLockFactory> distributedLockFactoryMock = new Mock<IDistributedLockFactory>();
            _lockFactory = new LockServiceFactory(distributedLockFactoryMock.Object);
        }

        [Fact]
        public void CreateLockService_ReturnsNewLockServiceObject_Successful()
        {
            var lockService = _lockFactory.CreateLockService();

            Assert.NotNull(lockService);
        }
    }
}
