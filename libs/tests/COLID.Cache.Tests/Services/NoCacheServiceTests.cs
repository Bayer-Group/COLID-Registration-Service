using System;
using System.Threading.Tasks;
using COLID.Cache.Services;
using Moq;
using Xunit;

namespace COLID.Cache.Tests.Services
{
    public class NoCacheServiceTests
    {
        private readonly NoCacheService _cacheService;

        public NoCacheServiceTests()
        {
            _cacheService = new NoCacheService();
        }

        [Fact]
        public void Exists_WithAnyKey_ReturnsFalse()
        {
            Assert.False(_cacheService.Exists("abc"));
            Assert.False(_cacheService.Exists("abc1234"));
            Assert.False(_cacheService.Exists("abc123/:?%$§&/\\;_4"));
        }

        [Fact]
        public void GetValue_WithAnyKey_ReturnsDefaultFalse()
        {
            Assert.Equal(default, _cacheService.GetValue<bool>("abc"));
            Assert.Equal(default, _cacheService.GetValue<byte>("abc"));
            Assert.Equal(default, _cacheService.GetValue<sbyte>("abc"));
            Assert.Equal(default, _cacheService.GetValue<char>("abc"));
            Assert.Equal(default, _cacheService.GetValue<decimal>("abc"));
            Assert.Equal(default, _cacheService.GetValue<double>("abc"));
            Assert.Equal(default, _cacheService.GetValue<float>("abc"));
            Assert.Equal(default, _cacheService.GetValue<int>("abc"));
            Assert.Equal(default, _cacheService.GetValue<uint>("abc"));
            Assert.Equal(default, _cacheService.GetValue<long>("abc"));
            Assert.Equal(default, _cacheService.GetValue<ulong>("abc"));
            Assert.Equal(default, _cacheService.GetValue<short>("abc"));
            Assert.Equal(default, _cacheService.GetValue<ushort>("abc"));
            Assert.Equal(default, _cacheService.GetValue<object>("abc"));
            Assert.Equal(default, _cacheService.GetValue<string>("abc"));
            Assert.Equal(default(dynamic), _cacheService.GetValue<dynamic>("abc"));
        }

        [Fact]
        public void Set_WithAnyKey_ReturnsFalse()
        {
            Assert.False(_cacheService.Set("abc", "1234"));
        }

        [Fact]
        public void Set_WithTimeSpan_ReturnsFalse()
        {
            Assert.False(_cacheService.Set("abc", "1234", TimeSpan.FromSeconds(10)));
        }

        [Fact]
        public void TryGetValue_WithAnyKey_ReturnsDefaultFalse()
        {
            Assert.False(_cacheService.TryGetValue("abc", out bool testBool));
            Assert.False(_cacheService.TryGetValue("abc", out byte testByte));
            Assert.False(_cacheService.TryGetValue("abc", out sbyte testSbyte));
            Assert.False(_cacheService.TryGetValue("abc", out char testChar));
            Assert.False(_cacheService.TryGetValue("abc", out decimal testDecimal));
            Assert.False(_cacheService.TryGetValue("abc", out double testDouble));
            Assert.False(_cacheService.TryGetValue("abc", out float testFloat));
            Assert.False(_cacheService.TryGetValue("abc", out int testInt));
            Assert.False(_cacheService.TryGetValue("abc", out uint testUint));
            Assert.False(_cacheService.TryGetValue("abc", out long testLong));
            Assert.False(_cacheService.TryGetValue("abc", out ulong testUlong));
            Assert.False(_cacheService.TryGetValue("abc", out short testShort));
            Assert.False(_cacheService.TryGetValue("abc", out ushort testUshort));
            Assert.False(_cacheService.TryGetValue("abc", out object testObject));
            Assert.False(_cacheService.TryGetValue("abc", out string testString));
            Assert.False(_cacheService.TryGetValue("abc", out dynamic testDynamic));

            Assert.Equal(default, testBool);
            Assert.Equal(default, testByte);
            Assert.Equal(default, testSbyte);
            Assert.Equal(default, testChar);
            Assert.Equal(default, testDecimal);
            Assert.Equal(default, testDouble);
            Assert.Equal(default, testFloat);
            Assert.Equal(default, testInt);
            Assert.Equal(default, testUint);
            Assert.Equal(default, testLong);
            Assert.Equal(default, testUlong);
            Assert.Equal(default, testShort);
            Assert.Equal(default, testUshort);
            Assert.Equal(default, testObject);
            Assert.Equal(default, testString);
            Assert.Equal(default(dynamic), testDynamic);
        }

        [Fact]
        public void GetOrAdd_WithAnyKey_CallbackGotInvoked()
        {
            Mock<Func<object>> callbackMock = new Mock<Func<object>>();

            _cacheService.GetOrAdd("abc", callbackMock.Object);
            callbackMock.Verify(x => x(), Times.Once());
        }

        [Fact]
        public void GetOrAdd_WithAnyKeyAndTimeSpan_CallbackGotInvoked()
        {
            Mock<Func<object>> callbackMock = new Mock<Func<object>>();

            _cacheService.GetOrAdd("abc", callbackMock.Object, TimeSpan.FromSeconds(10));
            callbackMock.Verify(x => x(), Times.Once());
        }

        [Fact]
        public void GetOrAdd_WithObjectKey_CallbackGotInvoked()
        {
            Mock<Func<object>> callbackMock = new Mock<Func<object>>();

            _cacheService.GetOrAdd(new object(), callbackMock.Object);
            callbackMock.Verify(x => x(), Times.Once());
        }

        [Fact]
        public void GetOrAdd_WithObjectKeyAndTimeSpan_CallbackGotInvoked()
        {
            Mock<Func<object>> callbackMock = new Mock<Func<object>>();

            _cacheService.GetOrAdd(new object(), callbackMock.Object, TimeSpan.FromSeconds(10));
            callbackMock.Verify(x => x(), Times.Once());
        }

        [Fact]
        public async void GetOrAddAsync_WithAnyKey_CallbackGotInvoked()
        {
            Mock<Func<Task<object>>> callbackMock = new Mock<Func<Task<object>>>();

            await _cacheService.GetOrAddAsync("abc", callbackMock.Object);
            callbackMock.Verify(x => x(), Times.Once());
        }

        [Fact]
        public async void GetOrAddAsync_WithAnyKeyAndTimeSpan_CallbackGotInvoked()
        {
            Mock<Func<Task<object>>> callbackMock = new Mock<Func<Task<object>>>();

            await _cacheService.GetOrAddAsync("abc", callbackMock.Object, TimeSpan.FromSeconds(10));
            callbackMock.Verify(x => x(), Times.Once());
        }

        [Fact]
        public async void GetOrAddAsync_WithObjectKey_CallbackGotInvoked()
        {
            Mock<Func<Task<object>>> callbackMock = new Mock<Func<Task<object>>>();

            await _cacheService.GetOrAddAsync(new object(), callbackMock.Object);
            callbackMock.Verify(x => x(), Times.Once());
        }

        [Fact]
        public async void GetOrAddAsync_WithObjectKeyAndTimeSpan_CallbackGotInvoked()
        {
            Mock<Func<Task<object>>> callbackMock = new Mock<Func<Task<object>>>();

            await _cacheService.GetOrAddAsync(new object(), callbackMock.Object, TimeSpan.FromSeconds(10));
            callbackMock.Verify(x => x(), Times.Once());
        }

        [Fact]
        public void Update_WithAnyKey_CallbackGotInvoked()
        {
            Mock<Func<object>> callbackMock = new Mock<Func<object>>();

            _cacheService.Update("abc", callbackMock.Object);
            callbackMock.Verify(x => x(), Times.Once());
        }

        [Fact]
        public void Update_WithObjectKey_CallbackGotInvoked()
        {
            Mock<Func<object>> callbackMock = new Mock<Func<object>>();

            _cacheService.Update(new object(), callbackMock.Object);
            callbackMock.Verify(x => x(), Times.Once());
        }

        [Fact]
        public void Delete_WithAnyKey_CallbackGotInvoked()
        {
            Mock<Action> callbackMock = new Mock<Action>();

            _cacheService.Delete("abc", callbackMock.Object);
            callbackMock.Verify(x => x(), Times.Once());
        }

        [Fact]
        public void Delete_WithObjectKey_CallbackGotInvoked()
        {
            Mock<Action> callbackMock = new Mock<Action>();

            _cacheService.Delete(new object(), callbackMock.Object);
            callbackMock.Verify(x => x(), Times.Once());
        }
    }
}
