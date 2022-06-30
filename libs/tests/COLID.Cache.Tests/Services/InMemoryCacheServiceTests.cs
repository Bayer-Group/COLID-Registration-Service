using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using COLID.Cache.Configuration;
using COLID.Cache.Extensions;
using COLID.Cache.Services;
using COLID.Exception.Models.Business;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace COLID.Cache.Tests.Services
{
    public class InMemoryCacheServiceTests
    {
        private static InMemoryCacheService _cacheService;
        private readonly Mock<IMemoryCache> _memoryCacheMock;

        private readonly string _applicationName = "COLID_AppName";
        private readonly string _environmentName = "Test";
        private readonly ColidCacheOptions _colidCacheOptions;

        public InMemoryCacheServiceTests()
        {
            _memoryCacheMock = new Mock<IMemoryCache>();
            var hostEnvironmentMock = new Mock<IHostEnvironment>();
            hostEnvironmentMock.SetupGet(e => e.ApplicationName).Returns(_applicationName);
            hostEnvironmentMock.SetupGet(e => e.EnvironmentName).Returns(_environmentName);

            _colidCacheOptions = new ColidCacheOptions();
            _colidCacheOptions.AbsoluteExpirationRelativeToNow = 1337;

            var optionMonitorMock = Mock.Of<IOptionsMonitor<ColidCacheOptions>>(_ => _.CurrentValue == _colidCacheOptions);
            var loggerMock = new Mock<ILogger<InMemoryCacheService>>();

            _cacheService = new InMemoryCacheService(_memoryCacheMock.Object, hostEnvironmentMock.Object, optionMonitorMock, loggerMock.Object);
        }

        // Unit Tests for method: T GetValue<T>(string key);
        [Fact]
        public void GetValue_KeyInCache_ReturnsCorrectValue()
        {
            object testCacheValue = "foo1234!§$%&/()=?cache_result;:-.";
            _memoryCacheMock
                .Setup(x => x.TryGetValue(It.IsAny<object>(), out testCacheValue))
                .Returns(true);

            var result = _cacheService.GetValue<string>("cachekey");
            Assert.Equal(testCacheValue, result);
        }

        // SL: Minor incident with InMemoryCacheService.GetValue. Method should return default value, if the value != null
        // in IMemoryCache. This case never happen (expect for requested type object), because IMemoryCache already returns
        // the default value in internal extension methods. Therefore the InMemoryCache is never null
        [Fact]
        public void GetValue_KeyInCacheAndValueNull_ReturnsDefaultValue()
        {
            object memoryCacheValue = null;
            _memoryCacheMock
                .Setup(x => x.TryGetValue(It.IsAny<object>(), out memoryCacheValue))
                .Returns(true);

            var result = _cacheService.GetValue<object>("cachekey");
            Assert.Null(result);
        }

        [Fact]
        public void GetValue_KeyNotInCache_ReturnsDefaultValue()
        {
            object memoryCacheValue = null;
            _memoryCacheMock
                .Setup(x => x.TryGetValue(It.IsAny<object>(), out memoryCacheValue))
                .Returns(false);

            var result = _cacheService.GetValue<int>("cachekey");
            Assert.Equal(0, result);
        }

        // Unit Tests for method: bool TryGetValue<T>(string key, out T cachedEntry);
        [Fact]
        public void TryGetValue_KeyInCache_ReturnsCorrectValue()
        {
            object testCacheValue = "foo1234!§$%&/()=?cache_result;:-.";
            _memoryCacheMock
                .Setup(x => x.TryGetValue(It.IsAny<object>(), out testCacheValue))
                .Returns(true);

            var resultInCache = _cacheService.TryGetValue<string>("cachekey", out string result);
            Assert.True(resultInCache);
            Assert.Equal(testCacheValue, result);
        }

        [Fact]
        public void TryGetValue_KeyInCacheAndValueNull_ReturnsDefaultValue()
        {
            object memoryCacheValue = null;
            _memoryCacheMock
                .Setup(x => x.TryGetValue(It.IsAny<object>(), out memoryCacheValue))
                .Returns(true);

            var resultInCache = _cacheService.TryGetValue<int>("cachekey", out int result);
            Assert.True(resultInCache);
            Assert.Equal(0, result);
        }

        // SL: Minor incident with InMemoryCacheService.GetValue. Method should return default value, if the value != null
        // in IMemoryCache. This case never happen (expect for requested type object), because IMemoryCache already returns
        // the default value in internal extension methods. Therefore the InMemoryCache is never null
        [Fact]
        public void TryGetValue_KeyNotInCache_ReturnsDefaultValue()
        {
            object memoryCacheValue = null;
            _memoryCacheMock
                .Setup(x => x.TryGetValue(It.IsAny<object>(), out memoryCacheValue))
                .Returns(false);

            var resultInCache = _cacheService.TryGetValue<object>("cachekey", out object result);
            Assert.False(resultInCache);
            Assert.Null(result);
        }

        // Unit Tests for method: bool Set<T>(string key, T value);
        [Fact]
        public void Set_SuccessfulSetWithDefaultExpirationTime_ValueSetOnce()
        {
            var testCacheKey = "cachekey";
            var expectedCacheKey = (_applicationName + ":" + _environmentName + ":" + testCacheKey).ToLower();
            var testCacheValue = "cacheValue";

            var cacheEntryMock = new Mock<ICacheEntry>();
            _memoryCacheMock
                .Setup(x => x.CreateEntry(It.IsAny<object>()))
                .Returns(cacheEntryMock.Object);

            var result = _cacheService.Set<string>(testCacheKey, testCacheValue);

            Assert.True(result);
            _memoryCacheMock.Verify(mock => mock.CreateEntry(expectedCacheKey), Times.Once());
            cacheEntryMock.Verify(mock => mock.Dispose(), Times.Once());
            cacheEntryMock.VerifySet(mock => mock.Value = testCacheValue);
            cacheEntryMock.VerifySet(mock => mock.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_colidCacheOptions.AbsoluteExpirationRelativeToNow));

        }

        // Unit Tests for method: bool Set<T>(string key, T value, TimeSpan expirationTime);
        [Fact]
        public void Set_WithExpirationTimeAndSuccessfulSet_ValueSetOnce()
        {
            var testCacheKey = "cachekey";
            var expectedCacheKey = (_applicationName + ":" + _environmentName + ":" + testCacheKey).ToLower();
            var testCacheValue = "cacheValue";
            var testExpirationTime = TimeSpan.FromSeconds(42);

            var cacheEntryMock = new Mock<ICacheEntry>();
            _memoryCacheMock
                .Setup(x => x.CreateEntry(It.IsAny<object>()))
                .Returns(cacheEntryMock.Object);

            var result = _cacheService.Set<string>(testCacheKey, testCacheValue, testExpirationTime);

            Assert.True(result);
            _memoryCacheMock.Verify(mock => mock.CreateEntry(expectedCacheKey), Times.Once());
            cacheEntryMock.Verify(mock => mock.Dispose(), Times.Once());
            cacheEntryMock.VerifySet(mock => mock.Value = testCacheValue);
            cacheEntryMock.VerifySet(mock => mock.AbsoluteExpirationRelativeToNow = testExpirationTime);
        }

        // Unit Tests for method: bool Exists(string key);
        [Fact]
        public void Exists_ValueFound_ReturnsTrue()
        {
            object memoryCacheValue = null;
            _memoryCacheMock
                .Setup(x => x.TryGetValue(It.IsAny<object>(), out memoryCacheValue))
                .Returns(true);

            var result = _cacheService.Exists("cachekey");
            Assert.True(result);
        }

        [Fact]
        public void Exists_ValueNotFound_ReturnsFalse()
        {
            object memoryCacheValue = null;
            _memoryCacheMock
                .Setup(x => x.TryGetValue(It.IsAny<object>(), out memoryCacheValue))
                .Returns(false);

            var result = _cacheService.Exists("cachekey");
            Assert.False(result);
        }

        // Unit Tests for method: void Clear();
        [Fact]
        public void Clear_ClearCache_AllKeysRemoved()
        {
            var cacheEntryMock = new Mock<ICacheEntry>();
            _memoryCacheMock
                .Setup(x => x.CreateEntry(It.IsAny<object>()))
                .Returns(cacheEntryMock.Object);

            Assert.True(_cacheService.Set<string>("key1", "val1"));
            Assert.True(_cacheService.Set<string>("key2", "val2"));
            Assert.True(_cacheService.Set<string>("key3", "val3"));
            Assert.True(_cacheService.Set<string>("key4", "val4"));
            Assert.True(_cacheService.Set<string>("key5", "val5"));

            _cacheService.Clear();

            _memoryCacheMock.Verify(mock => mock.Remove(It.IsAny<string>()), Times.Exactly(5));
        }

        // Unit Tests for method: T GetOrAdd<T>(string key, Func<T> addEntry);
        [Fact]
        public void GetOrAdd_ValueCached_ReturnsValue()
        {
            object testCacheValue = "foo1234!§$%&/()=?cache_result;:-.";
            _memoryCacheMock
                .Setup(x => x.TryGetValue(It.IsAny<object>(), out testCacheValue))
                .Returns(true);

            bool methodInvoked = false;
            var result = _cacheService.GetOrAdd<string>("cachekey", () => {
                methodInvoked = true;
                return testCacheValue.ToString();
            });

            Assert.False(methodInvoked);
            Assert.Equal(testCacheValue, result);
        }

        [Fact]
        public void GetOrAdd_ValueNotCached_InvokesAddMethod()
        {
            var testCacheKey = "cachekey";
            var expectedCacheKey = (_applicationName + ":" + _environmentName + ":" + nameof(InMemoryCacheServiceTests) + ":" + testCacheKey).ToLower();
            object testCacheValue = "foo1234!§$%&/()=?cache_result;:-.";

            object cachedValue = null;
            var cacheEntryMock = new Mock<ICacheEntry>();
            _memoryCacheMock
                .Setup(x => x.CreateEntry(It.IsAny<object>()))
                .Returns(cacheEntryMock.Object);

            _memoryCacheMock
                .Setup(x => x.TryGetValue(It.IsAny<object>(), out cachedValue))
                .Returns(false);

            bool methodInvoked = false;
            var result = _cacheService.GetOrAdd<string>(testCacheKey, () => {
                methodInvoked = true;
                return testCacheValue.ToString();
            });

            Assert.True(methodInvoked);
            Assert.Equal(testCacheValue.ToString(), result);

            _memoryCacheMock.Verify(mock => mock.CreateEntry(expectedCacheKey), Times.Once());
            cacheEntryMock.Verify(mock => mock.Dispose(), Times.Once());
            cacheEntryMock.VerifySet(mock => mock.Value = testCacheValue.ToString());
            cacheEntryMock.VerifySet(mock => mock.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_colidCacheOptions.AbsoluteExpirationRelativeToNow));
        }

        // Unit Tests for method: T GetOrAdd<T>(string key, Func<T> addEntry, TimeSpan expirationTime);
        [Fact]
        public void GetOrAdd_WithExpirationTimeAndValueNotCached_InvokesAddMethod()
        {
            var testCacheKey = "cachekey";
            var expectedCacheKey = (_applicationName + ":" + _environmentName + ":" + nameof(InMemoryCacheServiceTests) + ":" + testCacheKey).ToLower();
            object testCacheValue = "foo1234!§$%&/()=?cache_result;:-.";
            var testExpirationTime = TimeSpan.FromSeconds(42);

            object cachedValue = null;
            var cacheEntryMock = new Mock<ICacheEntry>();
            _memoryCacheMock
                .Setup(x => x.CreateEntry(It.IsAny<object>()))
                .Returns(cacheEntryMock.Object);

            _memoryCacheMock
                .Setup(x => x.TryGetValue(It.IsAny<object>(), out cachedValue))
                .Returns(false);

            bool methodInvoked = false;
            var result = _cacheService.GetOrAdd<string>(testCacheKey, () => {
                methodInvoked = true;
                return testCacheValue.ToString();
            }, testExpirationTime);

            Assert.True(methodInvoked);
            Assert.Equal(testCacheValue.ToString(), result);

            _memoryCacheMock.Verify(mock => mock.CreateEntry(expectedCacheKey), Times.Once());
            cacheEntryMock.Verify(mock => mock.Dispose(), Times.Once());
            cacheEntryMock.VerifySet(mock => mock.Value = testCacheValue.ToString());
            cacheEntryMock.VerifySet(mock => mock.AbsoluteExpirationRelativeToNow = testExpirationTime);
        }

        // Unit Tests for method: T GetOrAdd<T>(object o, Func<T> addEntry);
        [Fact]
        public void GetOrAdd_ObjectKeyAndValueCached_ReturnsValue()
        {
            object testCacheValue = "foo1234!§$%&/()=?cache_result;:-.";
            _memoryCacheMock
                .Setup(x => x.TryGetValue(It.IsAny<object>(), out testCacheValue))
                .Returns(true);

            bool methodInvoked = false;
            var result = _cacheService.GetOrAdd<string>(new { foo = "bar" }, () => {
                methodInvoked = true;
                return testCacheValue.ToString();
            });

            Assert.False(methodInvoked);
            Assert.Equal(testCacheValue, result);
        }

        [Fact]
        public void GetOrAdd_ObjectKeyAndValueNotCached_InvokesAddMethod()
        {
            object testCacheKey = new { foo = "bar" };
            var expectedCacheKey = (_applicationName + ":" + _environmentName + ":" + nameof(InMemoryCacheServiceTests) + ":" + testCacheKey.CalculateHash()).ToLower();
            object testCacheValue = "foo1234!§$%&/()=?cache_result;:-.";

            object cachedValue = null;
            var cacheEntryMock = new Mock<ICacheEntry>();
            _memoryCacheMock
                .Setup(x => x.CreateEntry(It.IsAny<object>()))
                .Returns(cacheEntryMock.Object);

            _memoryCacheMock
                .Setup(x => x.TryGetValue(It.IsAny<object>(), out cachedValue))
                .Returns(false);

            bool methodInvoked = false;
            var result = _cacheService.GetOrAdd<string>(testCacheKey, () => {
                methodInvoked = true;
                return testCacheValue.ToString();
            });

            Assert.True(methodInvoked);
            Assert.Equal(testCacheValue.ToString(), result);

            _memoryCacheMock.Verify(mock => mock.CreateEntry(expectedCacheKey), Times.Once());
            cacheEntryMock.Verify(mock => mock.Dispose(), Times.Once());
            cacheEntryMock.VerifySet(mock => mock.Value = testCacheValue.ToString());
            cacheEntryMock.VerifySet(mock => mock.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_colidCacheOptions.AbsoluteExpirationRelativeToNow));
        }

        // Unit Tests for method: T GetOrAdd<T>(object o, Func<T> addEntry, TimeSpan expirationTime);
        [Fact]
        public void GetOrAdd_ObjectKeyWithExpirationTimeAndValueNotCached_InvokesAddMethod()
        {
            object testCacheKey = new { foo = "bar" };
            var expectedCacheKey = (_applicationName + ":" + _environmentName + ":" + nameof(InMemoryCacheServiceTests) + ":" + testCacheKey.CalculateHash()).ToLower();
            object testCacheValue = "foo1234!§$%&/()=?cache_result;:-.";
            var testExpirationTime = TimeSpan.FromSeconds(42);

            object cachedValue = null;
            var cacheEntryMock = new Mock<ICacheEntry>();
            _memoryCacheMock
                .Setup(x => x.CreateEntry(It.IsAny<object>()))
                .Returns(cacheEntryMock.Object);

            _memoryCacheMock
                .Setup(x => x.TryGetValue(It.IsAny<object>(), out cachedValue))
                .Returns(false);

            bool methodInvoked = false;
            var result = _cacheService.GetOrAdd<string>(testCacheKey, () => {
                methodInvoked = true;
                return testCacheValue.ToString();
            }, testExpirationTime);

            Assert.True(methodInvoked);
            Assert.Equal(testCacheValue.ToString(), result);

            _memoryCacheMock.Verify(mock => mock.CreateEntry(expectedCacheKey), Times.Once());
            cacheEntryMock.Verify(mock => mock.Dispose(), Times.Once());
            cacheEntryMock.VerifySet(mock => mock.Value = testCacheValue.ToString());
            cacheEntryMock.VerifySet(mock => mock.AbsoluteExpirationRelativeToNow = testExpirationTime);
        }
        
        // Unit Tests for method: Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> addEntry);
        [Fact]
        public async void GetOrAddAsync_ValueCached_ReturnsValue()
        {
            object testCacheValue = "foo1234!§$%&/()=?cache_result;:-.";
            _memoryCacheMock
                .Setup(x => x.TryGetValue(It.IsAny<object>(), out testCacheValue))
                .Returns(true);

            bool methodInvoked = false;
            var result = await _cacheService.GetOrAddAsync<string>("cachekey", () => {
                methodInvoked = true;
                return Task.FromResult(testCacheValue.ToString());
            });

            Assert.False(methodInvoked);
            Assert.Equal(testCacheValue, result);
        }

        [Fact]
        public async void GetOrAddAsync_ValueNotCached_InvokesAddMethod()
        {
            var testCacheKey = "cachekey";
            var expectedCacheKey = (_applicationName + ":" + _environmentName + ":" + testCacheKey).ToLower();
            object testCacheValue = "foo1234!§$%&/()=?cache_result;:-.";

            object cachedValue = null;
            var cacheEntryMock = new Mock<ICacheEntry>();
            _memoryCacheMock
                .Setup(x => x.CreateEntry(It.IsAny<object>()))
                .Returns(cacheEntryMock.Object);

            _memoryCacheMock
                .Setup(x => x.TryGetValue(It.IsAny<object>(), out cachedValue))
                .Returns(false);

            bool methodInvoked = false;
            var result = await _cacheService.GetOrAddAsync<string>(testCacheKey, () => {
                methodInvoked = true;
                return Task.FromResult(testCacheValue.ToString());
            });

            Assert.True(methodInvoked);
            Assert.Equal(testCacheValue.ToString(), result);

            _memoryCacheMock.Verify(mock => mock.CreateEntry(expectedCacheKey), Times.Once());
            cacheEntryMock.Verify(mock => mock.Dispose(), Times.Once());
            cacheEntryMock.VerifySet(mock => mock.Value = testCacheValue.ToString());
            cacheEntryMock.VerifySet(mock => mock.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_colidCacheOptions.AbsoluteExpirationRelativeToNow));
        }

        // Unit Tests for method: Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> addEntry, TimeSpan expirationTime);
        [Fact]
        public async void GetOrAddAsync_WithExpirationTimeAndValueNotCached_InvokesAddMethod()
        {
            var testCacheKey = "cachekey";
            var expectedCacheKey = (_applicationName + ":" + _environmentName + ":" + testCacheKey).ToLower();
            object testCacheValue = "foo1234!§$%&/()=?cache_result;:-.";
            var testExpirationTime = TimeSpan.FromSeconds(42);

            object cachedValue = null;
            var cacheEntryMock = new Mock<ICacheEntry>();
            _memoryCacheMock
                .Setup(x => x.CreateEntry(It.IsAny<object>()))
                .Returns(cacheEntryMock.Object);

            _memoryCacheMock
                .Setup(x => x.TryGetValue(It.IsAny<object>(), out cachedValue))
                .Returns(false);

            bool methodInvoked = false;
            var result = await _cacheService.GetOrAddAsync<string>(testCacheKey, () => {
                methodInvoked = true;
                return Task.FromResult(testCacheValue.ToString());
            }, testExpirationTime);

            Assert.True(methodInvoked);
            Assert.Equal(testCacheValue.ToString(), result);

            _memoryCacheMock.Verify(mock => mock.CreateEntry(expectedCacheKey), Times.Once());
            cacheEntryMock.Verify(mock => mock.Dispose(), Times.Once());
            cacheEntryMock.VerifySet(mock => mock.Value = testCacheValue.ToString());
            cacheEntryMock.VerifySet(mock => mock.AbsoluteExpirationRelativeToNow = testExpirationTime);
        }

        // Unit Tests for method: Task<T> GetOrAddAsync<T>(object o, Func<Task<T>> addEntry);
        [Fact]
        public async void GetOrAddAsync_ObjectKeyAndValueCached_ReturnsValue()
        {
            object testCacheValue = "foo1234!§$%&/()=?cache_result;:-.";
            _memoryCacheMock
                .Setup(x => x.TryGetValue(It.IsAny<object>(), out testCacheValue))
                .Returns(true);

            bool methodInvoked = false;
            var result = await _cacheService.GetOrAddAsync<string>(new { foo = "bar" }, () => {
                methodInvoked = true;
                return Task.FromResult(testCacheValue.ToString());
            });

            Assert.False(methodInvoked);
            Assert.Equal(testCacheValue, result);
        }

        [Fact]
        public async void GetOrAddAsync_ObjectKeyAndValueNotCached_InvokesAddMethod()
        {
            object testCacheKey = new { foo = "bar" };
            var expectedCacheKey = (_applicationName + ":" + _environmentName + ":" + testCacheKey.CalculateHash()).ToLower();
            object testCacheValue = "foo1234!§$%&/()=?cache_result;:-.";

            object cachedValue = null;
            var cacheEntryMock = new Mock<ICacheEntry>();
            _memoryCacheMock
                .Setup(x => x.CreateEntry(It.IsAny<object>()))
                .Returns(cacheEntryMock.Object);

            _memoryCacheMock
                .Setup(x => x.TryGetValue(It.IsAny<object>(), out cachedValue))
                .Returns(false);

            bool methodInvoked = false;
            var result = await _cacheService.GetOrAddAsync<string>(testCacheKey, () => {
                methodInvoked = true;
                return Task.FromResult(testCacheValue.ToString());
            });

            Assert.True(methodInvoked);
            Assert.Equal(testCacheValue.ToString(), result);

            _memoryCacheMock.Verify(mock => mock.CreateEntry(expectedCacheKey), Times.Once());
            cacheEntryMock.Verify(mock => mock.Dispose(), Times.Once());
            cacheEntryMock.VerifySet(mock => mock.Value = testCacheValue.ToString());
            cacheEntryMock.VerifySet(mock => mock.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_colidCacheOptions.AbsoluteExpirationRelativeToNow));
        }

        // Unit Tests for method: Task<T> GetOrAddAsync<T>(object o, Func<Task<T>> addEntry, TimeSpan expirationTime);
        [Fact]
        public async void GetOrAddAsync_ObjectKeyWithExpirationTimeAndValueNotCached_InvokesAddMethod()
        {
            object testCacheKey = new { foo = "bar" };
            var expectedCacheKey = (_applicationName + ":" + _environmentName + ":" + testCacheKey.CalculateHash()).ToLower();
            object testCacheValue = "foo1234!§$%&/()=?cache_result;:-.";
            var testExpirationTime = TimeSpan.FromSeconds(42);

            object cachedValue = null;
            var cacheEntryMock = new Mock<ICacheEntry>();
            _memoryCacheMock
                .Setup(x => x.CreateEntry(It.IsAny<object>()))
                .Returns(cacheEntryMock.Object);

            _memoryCacheMock
                .Setup(x => x.TryGetValue(It.IsAny<object>(), out cachedValue))
                .Returns(false);

            bool methodInvoked = false;
            var result = await _cacheService.GetOrAddAsync<string>(testCacheKey, () => {
                methodInvoked = true;
                return Task.FromResult(testCacheValue.ToString());
            }, testExpirationTime);

            Assert.True(methodInvoked);
            Assert.Equal(testCacheValue.ToString(), result);

            _memoryCacheMock.Verify(mock => mock.CreateEntry(expectedCacheKey), Times.Once());
            cacheEntryMock.Verify(mock => mock.Dispose(), Times.Once());
            cacheEntryMock.VerifySet(mock => mock.Value = testCacheValue.ToString());
            cacheEntryMock.VerifySet(mock => mock.AbsoluteExpirationRelativeToNow = testExpirationTime);
        }

        // Unit Tests for method: public T Update<T>(string key, Func<T> updateEntry);
        [Fact]
        public void Update_ValueCached_UpdatesValue()
        {
            var testCacheKey = "cachekey";
            var expectedCacheKey = (_applicationName + ":" + _environmentName + ":" + nameof(InMemoryCacheServiceTests) + ":" + testCacheKey).ToLower();
            object testCacheValue = "foo1234!§$%&/()=?cache_result;:-.";
            var cacheEntryMock = new Mock<ICacheEntry>();
            _memoryCacheMock
                .Setup(x => x.CreateEntry(It.IsAny<object>()))
                .Returns(cacheEntryMock.Object);

            _memoryCacheMock
                .Setup(x => x.TryGetValue(It.IsAny<object>(), out testCacheValue))
                .Returns(true);

            bool methodInvoked = false;
            var result = _cacheService.Update<string>("cachekey", () => {
                methodInvoked = true;
                return testCacheValue.ToString();
            });

            Assert.True(methodInvoked);
            Assert.Equal(testCacheValue, result);

            _memoryCacheMock.Verify(mock => mock.CreateEntry(expectedCacheKey), Times.Once());
            cacheEntryMock.Verify(mock => mock.Dispose(), Times.Once());
            cacheEntryMock.VerifySet(mock => mock.Value = testCacheValue.ToString());
            cacheEntryMock.VerifySet(mock => mock.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_colidCacheOptions.AbsoluteExpirationRelativeToNow));
        }

        [Fact]
        public void Update_ValueNotCached_ThrowsEntityNotFoundException()
        {
            var testCacheKey = "cachekey";
            object testCacheValue = "foo1234!§$%&/()=?cache_result;:-.";

            object cachedValue = null;
            var cacheEntryMock = new Mock<ICacheEntry>();
            _memoryCacheMock
                .Setup(x => x.CreateEntry(It.IsAny<object>()))
                .Returns(cacheEntryMock.Object);

            _memoryCacheMock
                .Setup(x => x.TryGetValue(It.IsAny<object>(), out cachedValue))
                .Returns(false);

            bool methodInvoked = false;
            Assert.Throws<EntityNotFoundException>(() => _cacheService.Update<string>(testCacheKey, () => {
                methodInvoked = true;
                return testCacheValue.ToString();
            }));

            Assert.False(methodInvoked);
        }

        // Unit Tests for method: public T Update<T>(object o, Func<T> updateEntry);
        [Fact]
        public void Update_WithObjectKeyAndValueCached_UpdatesValue()
        {
            object testCacheKey = new { foo = "bar" };
            var expectedCacheKey = (_applicationName + ":" + _environmentName + ":" + nameof(InMemoryCacheServiceTests) + ":" + testCacheKey.CalculateHash()).ToLower();
            object testCacheValue = "foo1234!§$%&/()=?cache_result;:-.";
            var cacheEntryMock = new Mock<ICacheEntry>();
            _memoryCacheMock
                .Setup(x => x.CreateEntry(It.IsAny<object>()))
                .Returns(cacheEntryMock.Object);

            _memoryCacheMock
                .Setup(x => x.TryGetValue(It.IsAny<object>(), out testCacheValue))
                .Returns(true);

            bool methodInvoked = false;
            var result = _cacheService.Update<string>(testCacheKey, () => {
                methodInvoked = true;
                return testCacheValue.ToString();
            });

            Assert.True(methodInvoked);
            Assert.Equal(testCacheValue, result);

            _memoryCacheMock.Verify(mock => mock.CreateEntry(expectedCacheKey), Times.Once());
            cacheEntryMock.Verify(mock => mock.Dispose(), Times.Once());
            cacheEntryMock.VerifySet(mock => mock.Value = testCacheValue.ToString());
            cacheEntryMock.VerifySet(mock => mock.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_colidCacheOptions.AbsoluteExpirationRelativeToNow));
        }

        [Fact]
        public void Update_WithObjectKeyAndValueNotCached_ThrowsEntityNotFoundException()
        {
            object testCacheKey = new { foo = "bar" };
            object testCacheValue = "foo1234!§$%&/()=?cache_result;:-.";

            object cachedValue = null;
            var cacheEntryMock = new Mock<ICacheEntry>();
            _memoryCacheMock
                .Setup(x => x.CreateEntry(It.IsAny<object>()))
                .Returns(cacheEntryMock.Object);

            _memoryCacheMock
                .Setup(x => x.TryGetValue(It.IsAny<object>(), out cachedValue))
                .Returns(false);

            bool methodInvoked = false;
            Assert.Throws<EntityNotFoundException>(() => _cacheService.Update<string>(testCacheKey, () => {
                methodInvoked = true;
                return testCacheValue.ToString();
            }));

            Assert.False(methodInvoked);
        }

        // Unit Tests for method: void Delete(string key);
        [Fact]
        public void Delete_CacheKeyBuild_CacheRemoveCalledWithBuiltKey()
        {
            var testCacheKey = "cachekey";
            var expectedCacheKey = (_applicationName + ":" + _environmentName + ":" + testCacheKey).ToLower();

            _cacheService.Delete(testCacheKey);

            _memoryCacheMock.Verify(mock => mock.Remove(expectedCacheKey), Times.Once());
        }

        // Unit Tests for method: void Delete(object o);
        [Fact]
        public void Delete_WithObjectKeyAndCacheKeyBuild_CacheRemoveCalledWithBuiltKey()
        {
            object testCacheKey = new { foo = "bar" };
            var expectedCacheKey = (_applicationName + ":" + _environmentName + ":" + testCacheKey.CalculateHash()).ToLower();

            _cacheService.Delete(testCacheKey);

            _memoryCacheMock.Verify(mock => mock.Remove(expectedCacheKey), Times.Once());
        }

        // Unit Tests for method: void Delete(string key, string pattern, bool addAppAndEnvNameToKey = true);
        [Fact]
        public void Delete_WithPattern_CacheRemoveCalledForMatchingEntries()
        {
            var expectedCacheKeyPrefix = (_applicationName + ":" + _environmentName + ":").ToLower();
            var cacheEntryMock = new Mock<ICacheEntry>();
            _memoryCacheMock
                .Setup(x => x.CreateEntry(It.IsAny<object>()))
                .Returns(cacheEntryMock.Object);

            Assert.True(_cacheService.Set<string>("key1", "val1"));
            Assert.True(_cacheService.Set<string>("key2", "val2"));
            Assert.True(_cacheService.Set<string>("notDeletedKey1", "val7"));

            _cacheService.Delete("key", "*");

            _memoryCacheMock.Verify(mock => mock.Remove(expectedCacheKeyPrefix + "key1"), Times.Once());
            _memoryCacheMock.Verify(mock => mock.Remove(expectedCacheKeyPrefix + "key2"), Times.Once());
            _memoryCacheMock.Verify(mock => mock.Remove(expectedCacheKeyPrefix + "notDeletedKey1"), Times.Never());
        }

        [Fact]
        public void Delete_WithPatternAndAddAppAndEnvNameToKey_CacheRemoveCalledForMatchingEntries()
        {
            var expectedCacheKeyPrefix = (_applicationName + ":" + _environmentName + ":").ToLower();
            var cacheEntryMock = new Mock<ICacheEntry>();
            _memoryCacheMock
                .Setup(x => x.CreateEntry(It.IsAny<object>()))
                .Returns(cacheEntryMock.Object);

            // Set keys with prefixes
            Assert.True(_cacheService.Set<string>("key1", "val1"));
            Assert.True(_cacheService.Set<string>("key2", "val2"));
            Assert.True(_cacheService.Set<string>("notDeletedKey1", "val7"));

            _cacheService.Delete("key", "*", true);

            _memoryCacheMock.Verify(mock => mock.Remove(expectedCacheKeyPrefix + "key1"), Times.Once());
            _memoryCacheMock.Verify(mock => mock.Remove(expectedCacheKeyPrefix + "key2"), Times.Once());
            _memoryCacheMock.Verify(mock => mock.Remove(expectedCacheKeyPrefix + "notDeletedKey1"), Times.Never());
        }

        [Fact]
        public void Delete_WithPatternAndWithoutAddAppAndEnvNameToKey_CacheRemoveCalledForMatchingEntries()
        {
            var expectedCacheKeyPrefix = (_applicationName + ":" + _environmentName + ":").ToLower();
            var cacheEntryMock = new Mock<ICacheEntry>();
            _memoryCacheMock
                .Setup(x => x.CreateEntry(It.IsAny<object>()))
                .Returns(cacheEntryMock.Object);

            // Set keys with prefixes
            Assert.True(_cacheService.Set<string>("key1", "val1"));
            Assert.True(_cacheService.Set<string>("key2", "val2"));
            Assert.True(_cacheService.Set<string>("notDeletedKey1", "val7"));

            // Try delete all keys beginning with "key" without the prefix
            _cacheService.Delete("^key", "*", false);

            // The keys shouldn't get deleted, since the prefixes were added while setting the cache value
            _memoryCacheMock.Verify(mock => mock.Remove("key1"), Times.Never());
            _memoryCacheMock.Verify(mock => mock.Remove("key2"), Times.Never());
            _memoryCacheMock.Verify(mock => mock.Remove(expectedCacheKeyPrefix + "key1"), Times.Never());
            _memoryCacheMock.Verify(mock => mock.Remove(expectedCacheKeyPrefix + "key2"), Times.Never());
            _memoryCacheMock.Verify(mock => mock.Remove(expectedCacheKeyPrefix + "notDeletedKey1"), Times.Never());
        }

        // Unit Tests for method: void Delete(object o, string pattern);
        [Fact(Skip="SL: Due to the problem, that sometimes the calling class name is added to the cache prefix, it is not possible to delete objects from the cache")]
        public void Delete_WithObjectKeyAndWithPattern_CacheRemoveCalledForMatchingEntries()
        {
            object keyObject1 = new { foo = "bar1" };
            object keyObject2 = new { foo = "bar2" };

            var expectedCacheKeyPrefix = (_applicationName + ":" + _environmentName + ":" + nameof(InMemoryCacheServiceTests) + ":").ToLower();
            var cacheEntryMock = new Mock<ICacheEntry>();
            _memoryCacheMock
                .Setup(x => x.CreateEntry(It.IsAny<object>()))
                .Returns(cacheEntryMock.Object);

            Assert.Equal("val1", _cacheService.GetOrAdd<string>(keyObject1, () => { return "val1"; }));
            Assert.Equal("val2", _cacheService.GetOrAdd<string>(keyObject2, () => { return "val2"; }));

            Assert.True(_cacheService.Set<string>("notDeletedKey1", "val7"));

            _cacheService.Delete(keyObject1, "*");

            _memoryCacheMock.Verify(mock => mock.Remove(expectedCacheKeyPrefix + keyObject1.CalculateHash()), Times.Once());
            _memoryCacheMock.Verify(mock => mock.Remove(expectedCacheKeyPrefix + keyObject2.CalculateHash()), Times.Never());
            _memoryCacheMock.Verify(mock => mock.Remove(expectedCacheKeyPrefix + "notDeletedKey1"), Times.Never());
        }

        // Unit Tests for method: void Delete(string key, Action method);
        [Fact]
        public void Delete_WithCallbackMethod_DeletedKeysAndCalledMethodAfterwards()
        {
            var expectedCacheKeyPrefix = (_applicationName + ":" + _environmentName + ":" + nameof(InMemoryCacheServiceTests) + ":").ToLower();
            var cacheEntryMock = new Mock<ICacheEntry>();
            _memoryCacheMock
                .Setup(x => x.CreateEntry(It.IsAny<object>()))
                .Returns(cacheEntryMock.Object);

            // Set keys with prefixes
            Assert.True(_cacheService.Set<string>("key1", "val1"));
            Assert.True(_cacheService.Set<string>("key2", "val2"));

            bool methodInvokedKey1 = false;

            _cacheService.Delete("key1", () =>
            {
                methodInvokedKey1 = true;
            });

            Assert.True(methodInvokedKey1);
            _memoryCacheMock.Verify(mock => mock.Remove(expectedCacheKeyPrefix + "key1"), Times.Once());
            _memoryCacheMock.Verify(mock => mock.Remove(expectedCacheKeyPrefix + "key2"), Times.Never());
        }

        // Unit Tests for method: void Delete(object o, Action method);
        [Fact]
        public void Delete_WithObjectKeyAndCallbackMethod_DeletedKeysAndCalledMethodAfterwards()
        {
            object keyObject1 = new { foo = "bar1" };
            object keyObject2 = new { foo = "bar2" };

            var expectedCacheKeyPrefix = (_applicationName + ":" + _environmentName + ":" + nameof(InMemoryCacheServiceTests) + ":").ToLower();
            var cacheEntryMock = new Mock<ICacheEntry>();
            _memoryCacheMock
                .Setup(x => x.CreateEntry(It.IsAny<object>()))
                .Returns(cacheEntryMock.Object);

            // Set keys with prefixes
            Assert.Equal("val1", _cacheService.GetOrAdd<string>(keyObject1, () => { return "val1"; }));
            Assert.Equal("val2", _cacheService.GetOrAdd<string>(keyObject2, () => { return "val2"; }));

            bool methodInvokedKey1 = false;

            _cacheService.Delete(keyObject1, () =>
            {
                methodInvokedKey1 = true;
            });

            Assert.True(methodInvokedKey1);
            _memoryCacheMock.Verify(mock => mock.Remove(expectedCacheKeyPrefix + keyObject1.CalculateHash()), Times.Once());
            _memoryCacheMock.Verify(mock => mock.Remove(expectedCacheKeyPrefix + keyObject2.CalculateHash()), Times.Never());
        }

        public static TheoryData ArgumentExceptionTestMethods => new TheoryData<Action>
        {
            () => _cacheService.GetValue<string>(null),
            () => _cacheService.GetValue<string>("   "),
            () => _cacheService.GetValue<string>(string.Empty),
            () => _cacheService.GetOrAdd<string>(null, It.IsAny<Func<string>>()),
            () => _cacheService.GetOrAdd<string>("   ", It.IsAny<Func<string>>()),
            () => _cacheService.GetOrAdd<string>(string.Empty, It.IsAny<Func<string>>()),
            () => _cacheService.GetOrAdd<string>(null, It.IsAny<Func<string>>(), TimeSpan.FromSeconds(1)),
            () => _cacheService.GetOrAdd<string>("   ", It.IsAny<Func<string>>(), TimeSpan.FromSeconds(1)),
            () => _cacheService.GetOrAdd<string>(string.Empty, It.IsAny<Func<string>>(), TimeSpan.FromSeconds(1)),
            () => _cacheService.GetOrAdd<object>(null, It.IsAny<Func<string>>()),
            () => _cacheService.GetOrAdd<object>(null, It.IsAny<Func<string>>(), TimeSpan.FromSeconds(1)),
            () => _cacheService.Update<string>(null, It.IsAny<Func<string>>()),
            () => _cacheService.Update<string>("   ", It.IsAny<Func<string>>()),
            () => _cacheService.Update<string>(string.Empty, It.IsAny<Func<string>>()),
            () => _cacheService.Update<object>(null, It.IsAny<Func<object>>()),
            () => _cacheService.TryGetValue<string>(null, out string result),
            () => _cacheService.TryGetValue<string>("   ", out string result),
            () => _cacheService.TryGetValue<string>(string.Empty, out string result),
            () => _cacheService.Set<string>(null, It.IsAny<string>()),
            () => _cacheService.Set<string>("   ", It.IsAny<string>()),
            () => _cacheService.Set<string>(string.Empty, It.IsAny<string>()),
            () => _cacheService.Set<string>(null, It.IsAny<string>(), TimeSpan.FromSeconds(1)),
            () => _cacheService.Set<string>("   ", It.IsAny<string>(), TimeSpan.FromSeconds(1)),
            () => _cacheService.Set<string>(string.Empty, It.IsAny<string>(), TimeSpan.FromSeconds(1)),
            () => _cacheService.Set<string>("valid", null),
            () => _cacheService.Set<string>("valid", "   "),
            () => _cacheService.Set<string>("valid", string.Empty),
            () => _cacheService.Set<string>("valid", null, TimeSpan.FromSeconds(1)),
            () => _cacheService.Set<string>("valid", "   ", TimeSpan.FromSeconds(1)),
            () => _cacheService.Set<string>("valid", string.Empty, TimeSpan.FromSeconds(1)),
            () => _cacheService.Exists(null),
            () => _cacheService.Exists("   "),
            () => _cacheService.Exists(string.Empty),
            () => _cacheService.Delete(null),
            () => _cacheService.Delete("   "),
            () => _cacheService.Delete(string.Empty),
            () => _cacheService.Delete((object)null),
            () => _cacheService.Delete(null, It.IsAny<string>(), true),
            () => _cacheService.Delete("   ", It.IsAny<string>(), true),
            () => _cacheService.Delete(string.Empty, It.IsAny<string>(), true),
            () => _cacheService.Delete(null, It.IsAny<string>(), false),
            () => _cacheService.Delete("   ", It.IsAny<string>(), false),
            () => _cacheService.Delete(string.Empty, It.IsAny<string>(), false),
            () => _cacheService.Delete((object)null, It.IsAny<string>()),
            () => _cacheService.Delete(new object(), (string)null),
            () => _cacheService.Delete(new object(), "   "),
            () => _cacheService.Delete(new object(), string.Empty),
            () => _cacheService.Delete(null, It.IsAny<Action>()),
            () => _cacheService.Delete("   ", It.IsAny<Action>()),
            () => _cacheService.Delete(string.Empty, It.IsAny<Action>()),
            () => _cacheService.Delete("valid", null),
            () => _cacheService.Delete((object)null, It.IsAny<Action>()),
            () => _cacheService.Delete((object)null, It.IsAny<Action>()),
            () => _cacheService.Delete(new object(), (Action)null),
        };

        [Theory]
        [MemberData(nameof(ArgumentExceptionTestMethods))]
        public void Method_KeyIsNullOrWhitespace_ThrowsArgumentException(Action action) => Assert.Throws<ArgumentNullException>(() => action.Invoke());

        [Fact]
        public async void Method_KeyIsNullOrWhitespace_ThrowsAsyncArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _cacheService.GetOrAddAsync<string>(null, It.IsAny<Func<Task<string>>>()));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _cacheService.GetOrAddAsync<string>("   ", It.IsAny<Func<Task<string>>>()));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _cacheService.GetOrAddAsync<string>(string.Empty, It.IsAny<Func<Task<string>>>()));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _cacheService.GetOrAddAsync<string>(null, It.IsAny<Func<Task<string>>>(), TimeSpan.FromSeconds(1)));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _cacheService.GetOrAddAsync<string>("   ", It.IsAny<Func<Task<string>>>(), TimeSpan.FromSeconds(1)));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _cacheService.GetOrAddAsync<string>(string.Empty, It.IsAny<Func<Task<string>>>(), TimeSpan.FromSeconds(1)));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _cacheService.GetOrAddAsync<object>((object)null, It.IsAny<Func<Task<object>>>()));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _cacheService.GetOrAddAsync<object>((object)null, It.IsAny<Func<Task<object>>>(), TimeSpan.FromSeconds(1)));
        }
    }
}
