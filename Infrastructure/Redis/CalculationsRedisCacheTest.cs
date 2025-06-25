using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Infrastructure.Redis;
using WTW.Web.Caching;

namespace WTW.MdpService.Test.Infrastructure.Redis;

public class CalculationsRedisCacheTest
{
    private readonly Mock<ICache> _cacheMock;
    private readonly Mock<ILogger<CalculationsRedisCache>> _loggerMock;
    private readonly CalculationsRedisCache _calculationsRedisCache;

    public CalculationsRedisCacheTest()
    {
        _cacheMock = new Mock<ICache>();
        _loggerMock = new Mock<ILogger<CalculationsRedisCache>>();
        _calculationsRedisCache = new CalculationsRedisCache(_cacheMock.Object, _loggerMock.Object);
    }

    public async Task ClearsCalculationsCache()
    {
        string referenceNumber = "123";
        string businessGroup = "test";

        await _calculationsRedisCache.Clear(referenceNumber, businessGroup);

        _cacheMock.Verify(cache => cache.RemoveByPrefix($"calc-api-{referenceNumber}-{businessGroup}-"), Times.Once);
    }

    public async Task FailsToClearCalculationsCache()
    {
        string referenceNumber = "123";
        string businessGroup = "test";
        var exception = new Exception("Test exception");

        _cacheMock.Setup(cache => cache.RemoveByPrefix(It.IsAny<string>())).ThrowsAsync(exception);

        await _calculationsRedisCache.Clear(referenceNumber, businessGroup);

        _loggerMock.Verify(
            log => log.Log(
                It.Is<LogLevel>(level => level == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to clear calculations api cache")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);      
    }

    public async Task ClearsCalculationsDatesAgesCache()
    {
        string referenceNumber = "123";
        string businessGroup = "test";

        await _calculationsRedisCache.ClearRetirementDateAges(referenceNumber, businessGroup);

        _cacheMock.Verify(cache => cache.RemoveByPrefix($"calc-api-{referenceNumber}-{businessGroup}-retirement-dates-ages"), Times.Once);
    }

    public async Task FailsToClearCalculationsDatesAgesCache()
    {
        string referenceNumber = "123";
        string businessGroup = "test";
        var exception = new Exception("Test exception");

        _cacheMock.Setup(cache => cache.RemoveByPrefix(It.IsAny<string>())).ThrowsAsync(exception);

        await _calculationsRedisCache.ClearRetirementDateAges(referenceNumber, businessGroup);

        _loggerMock.Verify(
            log => log.Log(
                It.Is<LogLevel>(level => level == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to clear calculations api cache")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }
}