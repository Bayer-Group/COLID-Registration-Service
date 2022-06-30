using System;
using System.Threading;
using System.Threading.Tasks;
using COLID.Cache.Exceptions;
using COLID.Cache.Models;
using COLID.Cache.Services.Lock;
using COLID.RegistrationService.Tests.Common.Builder;
using Moq;
using RedLockNet;
using Xunit;

namespace COLID.Cache.Tests.Services
{
    public class LockServiceTests
    {
        private readonly Mock<IDistributedLockFactory> _distributedLockFactoryMock;
        private readonly LockService _lockService;

        public LockServiceTests()
        {
            _distributedLockFactoryMock = new Mock<IDistributedLockFactory>();
            _lockService = new LockService(_distributedLockFactoryMock.Object);
        }

        [Fact]
        public void CreateLock_DefaultExpiryTime_CallWithDefaultValues_FactoryShouldGetCalledOnce()
        {
            var colidLockBuilder = new ColidLockBuilder();

            var colidLockSample = colidLockBuilder.GenerateSampleData().Build();
            _distributedLockFactoryMock.Setup(mock => mock.CreateLock(It.IsAny<string>(), It.IsAny<TimeSpan>())).Returns(colidLockSample);

            var colidLock = _lockService.CreateLock(colidLockSample.Resource);
            _distributedLockFactoryMock.Verify(mock => mock.CreateLock(colidLockSample.Resource, TimeSpan.FromSeconds(10)), Times.Once());
        }

        [Fact]
        public void CreateLock_WithExpiryTime_CallWithDefaultValues_FactoryShouldGetCalledOnce()
        {
            var colidLockBuilder = new ColidLockBuilder();

            var colidLockSample = colidLockBuilder.GenerateSampleData().Build();
            _distributedLockFactoryMock.Setup(mock => mock.CreateLock(It.IsAny<string>(), It.IsAny<TimeSpan>())).Returns(colidLockSample);

            var colidLock = _lockService.CreateLock(colidLockSample.Resource, TimeSpan.FromSeconds(42));
            _distributedLockFactoryMock.Verify(mock => mock.CreateLock(colidLockSample.Resource, TimeSpan.FromSeconds(42)), Times.Once());
        }

        [Fact]
        public void CreateLock_WithAllTimes_CallWithDefaultValues_FactoryShouldGetCalledOnce()
        {
            var colidLockBuilder = new ColidLockBuilder();

            var colidLockSample = colidLockBuilder.GenerateSampleData().Build();
            _distributedLockFactoryMock.Setup(mock => mock.CreateLock(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), null)).Returns(colidLockSample);

            var colidLock = _lockService.CreateLock(colidLockSample.Resource, TimeSpan.FromSeconds(42), TimeSpan.FromSeconds(47), TimeSpan.FromSeconds(11));
            _distributedLockFactoryMock.Verify(mock => mock.CreateLock(colidLockSample.Resource, TimeSpan.FromSeconds(42), TimeSpan.FromSeconds(47), TimeSpan.FromSeconds(11), null), Times.Once());
        }

        [Fact]
        public void CreateLock_WithAllParameters_CallWithDefaultValues_FactoryShouldGetCalledOnce()
        {
            var colidLockBuilder = new ColidLockBuilder();

            var colidLockSample = colidLockBuilder.GenerateSampleData().Build();
            _distributedLockFactoryMock.Setup(mock => mock.CreateLock(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(colidLockSample);

            var colidLock = _lockService.CreateLock(colidLockSample.Resource, TimeSpan.FromSeconds(42), TimeSpan.FromSeconds(47), TimeSpan.FromSeconds(11), new CancellationToken(true));
            _distributedLockFactoryMock.Verify(mock => mock.CreateLock(colidLockSample.Resource, TimeSpan.FromSeconds(42), TimeSpan.FromSeconds(47), TimeSpan.FromSeconds(11), new CancellationToken(true)), Times.Once());
        }

        [Fact]
        public void CreateLockAsync_DefaultExpiryTime_CallWithDefaultValues_FactoryShouldGetCalledOnce()
        {
            var colidLockBuilder = new ColidLockBuilder();

            IRedLock colidLockSample = colidLockBuilder.GenerateSampleData().Build();
            _distributedLockFactoryMock.Setup(mock => mock.CreateLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>())).Returns(Task.FromResult(colidLockSample));

            var colidLock = _lockService.CreateLockAsync(colidLockSample.Resource);
            _distributedLockFactoryMock.Verify(mock => mock.CreateLockAsync(colidLockSample.Resource, TimeSpan.FromSeconds(10)), Times.Once());
        }

        [Fact]
        public void CreateLockAsync_WithExpiryTime_CallWithDefaultValues_FactoryShouldGetCalledOnce()
        {
            var colidLockBuilder = new ColidLockBuilder();

            IRedLock colidLockSample = colidLockBuilder.GenerateSampleData().Build();
            _distributedLockFactoryMock.Setup(mock => mock.CreateLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>())).Returns(Task.FromResult(colidLockSample));

            var colidLock = _lockService.CreateLockAsync(colidLockSample.Resource, TimeSpan.FromSeconds(42));
            _distributedLockFactoryMock.Verify(mock => mock.CreateLockAsync(colidLockSample.Resource, TimeSpan.FromSeconds(42)), Times.Once());
        }

        [Fact]
        public void CreateLockAsync_WithAllTimes_CallWithDefaultValues_FactoryShouldGetCalledOnce()
        {
            var colidLockBuilder = new ColidLockBuilder();

            IRedLock colidLockSample = colidLockBuilder.GenerateSampleData().Build();
            _distributedLockFactoryMock.Setup(mock => mock.CreateLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), null)).Returns(Task.FromResult(colidLockSample));

            var colidLock = _lockService.CreateLockAsync(colidLockSample.Resource, TimeSpan.FromSeconds(42), TimeSpan.FromSeconds(47), TimeSpan.FromSeconds(11));
            _distributedLockFactoryMock.Verify(mock => mock.CreateLockAsync(colidLockSample.Resource, TimeSpan.FromSeconds(42), TimeSpan.FromSeconds(47), TimeSpan.FromSeconds(11), null), Times.Once());
        }

        [Fact]
        public void CreateLockAsync_WithAllParameters_CallWithDefaultValues_FactoryShouldGetCalledOnce()
        {
            var colidLockBuilder = new ColidLockBuilder();

            IRedLock colidLockSample = colidLockBuilder.GenerateSampleData().Build();
            _distributedLockFactoryMock.Setup(mock => mock.CreateLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(colidLockSample));

            var colidLock = _lockService.CreateLockAsync(colidLockSample.Resource, TimeSpan.FromSeconds(42), TimeSpan.FromSeconds(47), TimeSpan.FromSeconds(11), new CancellationToken(true));
            _distributedLockFactoryMock.Verify(mock => mock.CreateLockAsync(colidLockSample.Resource, TimeSpan.FromSeconds(42), TimeSpan.FromSeconds(47), TimeSpan.FromSeconds(11), new CancellationToken(true)), Times.Once());
        }

        [Fact]
        public void CreateLock_ResourceLocked_ExceptionThrown()
        {
            var colidLockBuilder = new ColidLockBuilder();

            var colidLockSample = colidLockBuilder.GenerateSampleData()
                .WithIsAcquired(false)
                .WithInstanceSummary(new RedLockInstanceSummary(0,1,1))
                .Build();

            _distributedLockFactoryMock.Setup(mock => mock.CreateLock(It.IsAny<string>(), It.IsAny<TimeSpan>())).Returns(colidLockSample);

            Assert.Throws<ResourceLockedException>(() => _lockService.CreateLock(colidLockSample.Resource));
            _distributedLockFactoryMock.Verify(mock => mock.CreateLock(colidLockSample.Resource, TimeSpan.FromSeconds(10)), Times.Once());
        }
    }
}
