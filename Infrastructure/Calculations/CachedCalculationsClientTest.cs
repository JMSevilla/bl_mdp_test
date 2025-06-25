using System;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.TestCommon.Helpers;
using WTW.Web.Caching;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Infrastructure.Calculations;

public class CachedCalculationsClientTest
{
    private readonly CachedCalculationsClient _sut;
    private readonly Mock<ICalculationsClient> _clientMock;
    private readonly Mock<ICache> _cacheMock;
    private readonly Mock<ILogger<CachedCalculationsClient>> _logger;

    public CachedCalculationsClientTest()
    {
        _clientMock = new Mock<ICalculationsClient>();
        _cacheMock = new Mock<ICache>();
        _logger = new();

        _sut = new CachedCalculationsClient(_clientMock.Object, _cacheMock.Object, 20000, _logger.Object);
    }

    public async Task RetirementDatesAges_ReturnsFromCache()
    {
        var referenceNumber = "123";
        var businessGroup = "group1";
        var cacheKey = $"calc-api-{referenceNumber}-{businessGroup}-retirement-dates-ages";
        var cachedResponse = new RetirementDatesAgesResponse() { LockedInTransferQuoteImageId = 1236667 };

        _cacheMock.Setup(c => c.Get<RetirementDatesAgesResponse>(cacheKey))
            .ReturnsAsync(Option<RetirementDatesAgesResponse>.Some(cachedResponse));

        var result = await _sut.RetirementDatesAges(referenceNumber, businessGroup)();

        result.Value().LockedInTransferQuoteImageId.Should().Be(1236667);
        _clientMock.Verify(c => c.RetirementDatesAges(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _cacheMock.Verify(c => c.Set<RetirementDatesAgesResponse>(cacheKey, It.IsAny<RetirementDatesAgesResponse>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    public async Task RetirementDatesAges_ReturnsFromCalcApi()
    {
        var referenceNumber = "123";
        var businessGroup = "group1";
        var cacheKey = $"calc-api-{referenceNumber}-{businessGroup}-retirement-dates-ages";

        _cacheMock.Setup(c => c.Get<RetirementDatesAgesResponse>(cacheKey))
            .ReturnsAsync(Option<RetirementDatesAgesResponse>.None);
        _clientMock.Setup(c => c.RetirementDatesAges(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(async () => new RetirementDatesAgesResponse
            {
                HasLockedInTransferQuote = false,
                LockedInTransferQuoteImageId = 123456
            });

        var result = await _sut.RetirementDatesAges(referenceNumber, businessGroup)();

        result.Value().LockedInTransferQuoteImageId.Should().Be(123456);
        _clientMock.Verify(c => c.RetirementDatesAges(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _cacheMock.Verify(c => c.Set<RetirementDatesAgesResponse>(cacheKey, It.IsAny<RetirementDatesAgesResponse>(), It.IsAny<TimeSpan>()), Times.Once);
    }

    public async Task RetirementCalculation_ReturnsFromCache()
    {
        var referenceNumber = "123";
        var businessGroup = "group1";
        var date = DateTime.UtcNow;
        var cacheKey = $"calc-api-{referenceNumber}-{businessGroup}-retirement-{date:yyyy-MM-dd}";
        var cachedResponse = new RetirementRedisStore
        {
            EventType = "PO",
            RetirementResponse = new RetirementResponse()
            {
                Results = new RetirementResponse.ResultsResponse { Mdp = new RetirementResponse.MdpResponse { MaximumPermittedTotalLumpSum = 123 } }
            }
        };

        _cacheMock.Setup(c => c.Get<RetirementRedisStore>(cacheKey))
            .ReturnsAsync(cachedResponse);

        var result = await _sut.RetirementCalculation(referenceNumber, businessGroup, date);

        result.Right().EventType.Should().Be("PO");
        result.Right().RetirementResponse.Results.Mdp.MaximumPermittedTotalLumpSum.Should().Be(123);
        _clientMock.Verify(c => c.RetirementCalculation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
        _cacheMock.Verify(c => c.Set<RetirementResponse>(cacheKey, It.IsAny<RetirementResponse>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    public async Task RetirementCalculation_ReturnsError()
    {
        var referenceNumber = "123";
        var businessGroup = "group1";
        var date = DateTime.UtcNow;
        var cacheKey = $"calc-api-{referenceNumber}-{businessGroup}-retirement-{date:yyyy-MM-dd}";

        _cacheMock.Setup(c => c.Get<RetirementRedisStore>(cacheKey))
            .ReturnsAsync(Option<RetirementRedisStore>.None);

        _clientMock.Setup(c => c.RetirementCalculation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
          .ReturnsAsync(Error.New("test message"));

        var result = await _sut.RetirementCalculation(referenceNumber, businessGroup, date);

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("test message");
        _clientMock.Verify(c => c.RetirementCalculation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Once);
        _cacheMock.Verify(c => c.Set<RetirementRedisStore>(cacheKey, It.IsAny<RetirementRedisStore>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    public async Task RetirementCalculation_ReturnsFromApi()
    {
        var referenceNumber = "123";
        var businessGroup = "group1";
        var date = DateTime.UtcNow;
        var cacheKey = $"calc-api-{referenceNumber}-{businessGroup}-retirement-{date:yyyy-MM-dd}";

        Either<Error, (RetirementResponse, string)> retirementResponse = (new RetirementResponse()
        {
            Results = new RetirementResponse.ResultsResponse { Mdp = new RetirementResponse.MdpResponse { MaximumPermittedTotalLumpSum = 123 } }
        }, "PO");

        _cacheMock.Setup(c => c.Get<RetirementRedisStore>(cacheKey))
            .ReturnsAsync(Option<RetirementRedisStore>.None);

        _clientMock.Setup(c => c.RetirementCalculation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
          .ReturnsAsync(retirementResponse);

        var result = await _sut.RetirementCalculation(referenceNumber, businessGroup, date);

        result.IsRight.Should().BeTrue();
        result.Right().EventType.Should().Be("PO");
        result.Right().RetirementResponse.Results.Mdp.MaximumPermittedTotalLumpSum.Should().Be(123);
        _clientMock.Verify(c => c.RetirementCalculation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Once);
        _cacheMock.Verify(c => c.Set<RetirementRedisStore>(cacheKey, It.IsAny<RetirementRedisStore>(), It.IsAny<TimeSpan>()), Times.Once);
    }

    public async Task RetirementCalculationWithLumpSum_ReturnsFromCache()
    {
        var referenceNumber = "123";
        var businessGroup = "group1";
        var date = DateTime.UtcNow;
        var cacheKey = $"calc-api-{referenceNumber}-{businessGroup}-retirement-{date:yyyy-MM-dd}-20";
        var cachedResponse = new RetirementRedisStore
        {
            EventType = "PO",
            RetirementResponse = new RetirementResponse()
            {
                Results = new RetirementResponse.ResultsResponse { Mdp = new RetirementResponse.MdpResponse { MaximumPermittedTotalLumpSum = 123 } }
            }
        };

        _cacheMock.Setup(c => c.Get<RetirementRedisStore>(cacheKey))
            .ReturnsAsync(cachedResponse);

        var result = await _sut.RetirementCalculation(referenceNumber, businessGroup, date, 20);

        result.Right().EventType.Should().Be("PO");
        result.Right().RetirementResponse.Results.Mdp.MaximumPermittedTotalLumpSum.Should().Be(123);
        _clientMock.Verify(c => c.RetirementCalculation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<decimal>()), Times.Never);
        _cacheMock.Verify(c => c.Set<RetirementRedisStore>(cacheKey, It.IsAny<RetirementRedisStore>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    public async Task RetirementCalculationWithLumpSum_ReturnsError()
    {
        var referenceNumber = "123";
        var businessGroup = "group1";
        var date = DateTime.UtcNow;
        var cacheKey = $"calc-api-{referenceNumber}-{businessGroup}-retirement-{date:yyyy-MM-dd}-20";

        _cacheMock.Setup(c => c.Get<RetirementRedisStore>(cacheKey))
            .ReturnsAsync(Option<RetirementRedisStore>.None);

        _clientMock.Setup(c => c.RetirementCalculation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<decimal>()))
          .ReturnsAsync(Error.New("test message"));

        var result = await _sut.RetirementCalculation(referenceNumber, businessGroup, date, 20);

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("test message");
        _clientMock.Verify(c => c.RetirementCalculation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<decimal>()), Times.Once);
        _cacheMock.Verify(c => c.Set<RetirementRedisStore>(cacheKey, It.IsAny<RetirementRedisStore>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    public async Task RetirementCalculationWithLumpSum_ReturnsFromApi()
    {
        var referenceNumber = "123";
        var businessGroup = "group1";
        var date = DateTime.UtcNow;
        var cacheKey = $"calc-api-{referenceNumber}-{businessGroup}-retirement-{date:yyyy-MM-dd}-20";

        Either<Error, (RetirementResponse, string)> retirementResponse = (new RetirementResponse()
        {
            Results = new RetirementResponse.ResultsResponse { Mdp = new RetirementResponse.MdpResponse { MaximumPermittedTotalLumpSum = 123 } }
        }, "PO");

        _cacheMock.Setup(c => c.Get<RetirementRedisStore>(cacheKey))
            .ReturnsAsync(Option<RetirementRedisStore>.None);

        _clientMock.Setup(c => c.RetirementCalculation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<decimal>()))
          .ReturnsAsync(retirementResponse);

        var result = await _sut.RetirementCalculation(referenceNumber, businessGroup, date, 20);

        result.IsRight.Should().BeTrue();
        result.Right().EventType.Should().Be("PO");
        result.Right().RetirementResponse.Results.Mdp.MaximumPermittedTotalLumpSum.Should().Be(123);
        _clientMock.Verify(c => c.RetirementCalculation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<decimal>()), Times.Once);
        _cacheMock.Verify(c => c.Set<RetirementRedisStore>(cacheKey, It.IsAny<RetirementRedisStore>(), It.IsAny<TimeSpan>()), Times.Once);
    }

    public async Task RetirementCalculationV2WithLock_ReturnsFromApi()
    {
        var referenceNumber = "123";
        var businessGroup = "group1";
        var date = DateTime.UtcNow;
        var cacheKey = $"calc-api-{referenceNumber}-{businessGroup}-retirement-{date:yyyy-MM-dd}-20";

        Either<Error, RetirementResponseV2> retirementResponse = new RetirementResponseV2()
        {
            Results = new ResultsResponse { Mdp = new MdpResponseV2 { MaximumPermittedTotalLumpSum = 123 } }
        };

        _clientMock.Setup(c => c.RetirementCalculationV2WithLock(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<decimal>()))
          .ReturnsAsync(retirementResponse);

        var result = await _sut.RetirementCalculationV2WithLock(referenceNumber, businessGroup, "PO", date, 20);

        result.IsRight.Should().BeTrue();
        result.Right().Results.Mdp.MaximumPermittedTotalLumpSum.Should().Be(123);
        _clientMock.Verify(c => c.RetirementCalculationV2WithLock(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<decimal>()), Times.Once);
    }

    public async Task TransferEventType_ReturnsFromCache()
    {
        var referenceNumber = "123";
        var businessGroup = "group1";
        var cacheKey = $"calc-api-{referenceNumber}-{businessGroup}-transfer-event-type-transfer";
        var cachedResponse = new TypeResponse() { Type = "PA" };

        _cacheMock.Setup(c => c.Get<TypeResponse>(cacheKey))
            .ReturnsAsync(Option<TypeResponse>.Some(cachedResponse));

        var result = await _sut.TransferEventType(businessGroup, referenceNumber)();

        result.Value().Type.Should().Be("PA");
        _clientMock.Verify(c => c.TransferEventType(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _cacheMock.Verify(c => c.Set<TypeResponse>(cacheKey, It.IsAny<TypeResponse>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    public async Task TransferEventType_ReturnsFromCalcApi()
    {
        var referenceNumber = "123";
        var businessGroup = "group1";
        var cacheKey = $"calc-api-{referenceNumber}-{businessGroup}-transfer-event-type-transfer";

        _cacheMock.Setup(c => c.Get<TypeResponse>(cacheKey))
            .ReturnsAsync(Option<TypeResponse>.None);
        _clientMock.Setup(c => c.TransferEventType(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(async () => new TypeResponse { Type = "PA", });

        var result = await _sut.TransferEventType(businessGroup, referenceNumber)();

        result.Value().Type.Should().Be("PA");
        _clientMock.Verify(c => c.TransferEventType(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _cacheMock.Verify(c => c.Set<TypeResponse>(cacheKey, It.IsAny<TypeResponse>(), It.IsAny<TimeSpan>()), Times.Once);
    }

    public async Task RetirementQuote_ReturnsFromApi()
    {
        var referenceNumber = "123";
        var businessGroup = "group1";
        var date = DateTime.UtcNow;

        _clientMock.Setup(c => c.RetirementQuote(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
          .ReturnsAsync(("123456789", 1));

        Either<Error, (string, int)> result = await _sut.RetirementQuote(referenceNumber, businessGroup, date);


        result.IsRight.Should().BeTrue();
        result.Right().Item1.Should().Be("123456789");
        result.Right().Item2.Should().Be(1);
        _clientMock.Verify(c => c.RetirementQuote(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Once);
    }

    public async Task PartialTransferValues_ReturnsFromCache()
    {
        var referenceNumber = "123";
        var businessGroup = "group1";
        var date = DateTime.UtcNow;
        var cacheKey = $"calc-api-{referenceNumber}-{businessGroup}-30-20";
        var cachedResponse = new PartialTransferResponse.MdpResponse
        {
            PensionTranchesResidual = new PartialTransferResponse.PensionTranchesResidualResponse { Total = 1, Gmp = 2, Post97 = 3, Pre97Excess = 4 },
            PensionTranchesFull = new PartialTransferResponse.PensionTranchesFull { Pre88Gmp = 2, Post88Gmp = 1, Pre97Excess = 3, Post97 = 4, Total = 5 },
            TransferValuesFull = new PartialTransferResponse.TransferValuesFullResponse { Total = 1, Gmp = 2, NonGuaranteed = 3, Pre97Excess = 4, Post97 = 5 },
            TransferValuesPartial = new PartialTransferResponse.TransferValuesPartialResponse { Total = 1, Gmp = 2, Pre97Excess = 4, Post97 = 5 },
        };

        _cacheMock.Setup(c => c.Get<PartialTransferResponse.MdpResponse>(cacheKey))
            .ReturnsAsync(cachedResponse);

        var result = await _sut.PartialTransferValues(businessGroup, referenceNumber, 30, 20);

        result.IsRight.Should().BeTrue();
        result.Right().PensionTranchesFull.Pre88Gmp.Should().Be(2);
        _clientMock.Verify(c => c.PartialTransferValues(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal?>(), It.IsAny<decimal?>()), Times.Never);
        _cacheMock.Verify(c => c.Set<PartialTransferResponse.MdpResponse>(cacheKey, It.IsAny<PartialTransferResponse.MdpResponse>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    public async Task PartialTransferValues_ReturnsError()
    {
        var referenceNumber = "123";
        var businessGroup = "group1";
        var date = DateTime.UtcNow;
        var cacheKey = $"calc-api-{referenceNumber}-{businessGroup}-30-20";

        _cacheMock.Setup(c => c.Get<PartialTransferResponse.MdpResponse>(cacheKey))
            .ReturnsAsync(Option<PartialTransferResponse.MdpResponse>.None);

        _clientMock.Setup(c => c.PartialTransferValues(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal?>(), It.IsAny<decimal?>()))
          .ReturnsAsync(Error.New("test message"));

        var result = await _sut.PartialTransferValues(businessGroup, referenceNumber, 30, 20);

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("test message");
        _clientMock.Verify(c => c.PartialTransferValues(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal?>(), It.IsAny<decimal?>()), Times.Once);
        _cacheMock.Verify(c => c.Set<PartialTransferResponse.MdpResponse>(cacheKey, It.IsAny<PartialTransferResponse.MdpResponse>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    public async Task PartialTransferValues_ReturnsFromApi()
    {
        var referenceNumber = "123";
        var businessGroup = "group1";
        var date = DateTime.UtcNow;
        var cacheKey = $"calc-api-{referenceNumber}-{businessGroup}-30-20";

        var response = new PartialTransferResponse.MdpResponse
        {
            PensionTranchesResidual = new PartialTransferResponse.PensionTranchesResidualResponse { Total = 1, Gmp = 2, Post97 = 3, Pre97Excess = 4 },
            PensionTranchesFull = new PartialTransferResponse.PensionTranchesFull { Pre88Gmp = 2, Post88Gmp = 1, Pre97Excess = 3, Post97 = 4, Total = 5 },
            TransferValuesFull = new PartialTransferResponse.TransferValuesFullResponse { Total = 1, Gmp = 2, NonGuaranteed = 3, Pre97Excess = 4, Post97 = 5 },
            TransferValuesPartial = new PartialTransferResponse.TransferValuesPartialResponse { Total = 1, Gmp = 2, Pre97Excess = 4, Post97 = 5 },
        };

        _cacheMock.Setup(c => c.Get<PartialTransferResponse.MdpResponse>(cacheKey))
            .ReturnsAsync(Option<PartialTransferResponse.MdpResponse>.None);

        _clientMock.Setup(c => c.PartialTransferValues(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal?>(), It.IsAny<decimal?>()))
          .ReturnsAsync(response);

        var result = await _sut.PartialTransferValues(businessGroup, referenceNumber, 30, 20);

        result.IsRight.Should().BeTrue();
        result.Right().PensionTranchesFull.Pre88Gmp.Should().Be(2);
        _clientMock.Verify(c => c.PartialTransferValues(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal?>(), It.IsAny<decimal?>()), Times.Once);
        _cacheMock.Verify(c => c.Set<PartialTransferResponse.MdpResponse>(cacheKey, It.IsAny<PartialTransferResponse.MdpResponse>(), It.IsAny<TimeSpan>()), Times.Once);
    }

    public async Task PensionTranches_ReturnsFromCache()
    {
        var referenceNumber = "123";
        var businessGroup = "group1";
        var date = DateTime.UtcNow;
        var cacheKey = $"calc-api-{referenceNumber}-{businessGroup}-30";
        var cachedResponse = new PartialTransferResponse.MdpResponse
        {
            PensionTranchesResidual = new PartialTransferResponse.PensionTranchesResidualResponse { Total = 1, Gmp = 2, Post97 = 3, Pre97Excess = 4 },
            PensionTranchesFull = new PartialTransferResponse.PensionTranchesFull { Pre88Gmp = 2, Post88Gmp = 1, Pre97Excess = 3, Post97 = 4, Total = 5 },
            TransferValuesFull = new PartialTransferResponse.TransferValuesFullResponse { Total = 1, Gmp = 2, NonGuaranteed = 3, Pre97Excess = 4, Post97 = 5 },
            TransferValuesPartial = new PartialTransferResponse.TransferValuesPartialResponse { Total = 1, Gmp = 2, Pre97Excess = 4, Post97 = 5 },
        };

        _cacheMock.Setup(c => c.Get<PartialTransferResponse.MdpResponse>(cacheKey))
            .ReturnsAsync(cachedResponse);

        var result = await _sut.PensionTranches(businessGroup, referenceNumber, 30);

        result.IsRight.Should().BeTrue();
        result.Right().PensionTranchesFull.Pre88Gmp.Should().Be(2);
        _clientMock.Verify(c => c.PensionTranches(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()), Times.Never);
        _cacheMock.Verify(c => c.Set<PartialTransferResponse.MdpResponse>(cacheKey, It.IsAny<PartialTransferResponse.MdpResponse>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    public async Task PensionTranches_ReturnsError()
    {
        var referenceNumber = "123";
        var businessGroup = "group1";
        var date = DateTime.UtcNow;
        var cacheKey = $"calc-api-{referenceNumber}-{businessGroup}-30";

        _cacheMock.Setup(c => c.Get<PartialTransferResponse.MdpResponse>(cacheKey))
            .ReturnsAsync(Option<PartialTransferResponse.MdpResponse>.None);

        _clientMock.Setup(c => c.PensionTranches(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()))
          .ReturnsAsync(Error.New("test message"));

        var result = await _sut.PensionTranches(businessGroup, referenceNumber, 30);

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("test message");
        _clientMock.Verify(c => c.PensionTranches(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()), Times.Once);
        _cacheMock.Verify(c => c.Set<PartialTransferResponse.MdpResponse>(cacheKey, It.IsAny<PartialTransferResponse.MdpResponse>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    public async Task PensionTranches_ReturnsFromApi()
    {
        var referenceNumber = "123";
        var businessGroup = "group1";
        var date = DateTime.UtcNow;
        var cacheKey = $"calc-api-{referenceNumber}-{businessGroup}-30";

        var response = new PartialTransferResponse.MdpResponse
        {
            PensionTranchesResidual = new PartialTransferResponse.PensionTranchesResidualResponse { Total = 1, Gmp = 2, Post97 = 3, Pre97Excess = 4 },
            PensionTranchesFull = new PartialTransferResponse.PensionTranchesFull { Pre88Gmp = 2, Post88Gmp = 1, Pre97Excess = 3, Post97 = 4, Total = 5 },
            TransferValuesFull = new PartialTransferResponse.TransferValuesFullResponse { Total = 1, Gmp = 2, NonGuaranteed = 3, Pre97Excess = 4, Post97 = 5 },
            TransferValuesPartial = new PartialTransferResponse.TransferValuesPartialResponse { Total = 1, Gmp = 2, Pre97Excess = 4, Post97 = 5 },
        };

        _cacheMock.Setup(c => c.Get<PartialTransferResponse.MdpResponse>(cacheKey))
            .ReturnsAsync(Option<PartialTransferResponse.MdpResponse>.None);

        _clientMock.Setup(c => c.PensionTranches(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()))
          .ReturnsAsync(response);

        var result = await _sut.PensionTranches(businessGroup, referenceNumber, 30);

        result.IsRight.Should().BeTrue();
        result.Right().PensionTranchesFull.Pre88Gmp.Should().Be(2);
        _clientMock.Verify(c => c.PensionTranches(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()), Times.Once);
        _cacheMock.Verify(c => c.Set<PartialTransferResponse.MdpResponse>(cacheKey, It.IsAny<PartialTransferResponse.MdpResponse>(), It.IsAny<TimeSpan>()), Times.Once);
    }

    public async Task TransferCalculation_ReturnsFromApi()
    {
        var referenceNumber = "123";
        var businessGroup = "group1";
        var date = DateTime.UtcNow;

        _clientMock.Setup(c => c.TransferCalculation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
          .ReturnsAsync(new TransferResponse { Results = new TransferResponse.ResultsResponse { Mdp = new TransferResponse.MdpResponse { TotalPensionAtDOL = 31 } } });

        var result = await _sut.TransferCalculation(referenceNumber, businessGroup);

        result.IsRight.Should().BeTrue();
        result.Right().Results.Mdp.TotalPensionAtDOL.Should().Be(31);
        _clientMock.Verify(c => c.TransferCalculation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
    }

    public async Task TransferValues_ReturnsFromCache()
    {
        var referenceNumber = "123";
        var businessGroup = "group1";
        var date = DateTime.UtcNow;
        var cacheKey = $"calc-api-{referenceNumber}-{businessGroup}-30";
        var cachedResponse = new PartialTransferResponse.MdpResponse
        {
            PensionTranchesResidual = new PartialTransferResponse.PensionTranchesResidualResponse { Total = 1, Gmp = 2, Post97 = 3, Pre97Excess = 4 },
            PensionTranchesFull = new PartialTransferResponse.PensionTranchesFull { Pre88Gmp = 2, Post88Gmp = 1, Pre97Excess = 3, Post97 = 4, Total = 5 },
            TransferValuesFull = new PartialTransferResponse.TransferValuesFullResponse { Total = 1, Gmp = 2, NonGuaranteed = 3, Pre97Excess = 4, Post97 = 5 },
            TransferValuesPartial = new PartialTransferResponse.TransferValuesPartialResponse { Total = 1, Gmp = 2, Pre97Excess = 4, Post97 = 5 },
        };

        _cacheMock.Setup(c => c.Get<PartialTransferResponse.MdpResponse>(cacheKey))
            .ReturnsAsync(cachedResponse);

        var result = await _sut.TransferValues(businessGroup, referenceNumber, 30);

        result.IsRight.Should().BeTrue();
        result.Right().PensionTranchesFull.Pre88Gmp.Should().Be(2);
        _clientMock.Verify(c => c.TransferValues(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()), Times.Never);
        _cacheMock.Verify(c => c.Set<PartialTransferResponse.MdpResponse>(cacheKey, It.IsAny<PartialTransferResponse.MdpResponse>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    public async Task TransferValues_ReturnsError()
    {
        var referenceNumber = "123";
        var businessGroup = "group1";
        var date = DateTime.UtcNow;
        var cacheKey = $"calc-api-{referenceNumber}-{businessGroup}-30";

        _cacheMock.Setup(c => c.Get<PartialTransferResponse.MdpResponse>(cacheKey))
            .ReturnsAsync(Option<PartialTransferResponse.MdpResponse>.None);

        _clientMock.Setup(c => c.TransferValues(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()))
          .ReturnsAsync(Error.New("test message"));

        var result = await _sut.TransferValues(businessGroup, referenceNumber, 30);

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("test message");
        _clientMock.Verify(c => c.TransferValues(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()), Times.Once);
        _cacheMock.Verify(c => c.Set<PartialTransferResponse.MdpResponse>(cacheKey, It.IsAny<PartialTransferResponse.MdpResponse>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    public async Task TransferValues_ReturnsFromApi()
    {
        var referenceNumber = "123";
        var businessGroup = "group1";
        var date = DateTime.UtcNow;
        var cacheKey = $"calc-api-{referenceNumber}-{businessGroup}-30";

        var response = new PartialTransferResponse.MdpResponse
        {
            PensionTranchesResidual = new PartialTransferResponse.PensionTranchesResidualResponse { Total = 1, Gmp = 2, Post97 = 3, Pre97Excess = 4 },
            PensionTranchesFull = new PartialTransferResponse.PensionTranchesFull { Pre88Gmp = 2, Post88Gmp = 1, Pre97Excess = 3, Post97 = 4, Total = 5 },
            TransferValuesFull = new PartialTransferResponse.TransferValuesFullResponse { Total = 1, Gmp = 2, NonGuaranteed = 3, Pre97Excess = 4, Post97 = 5 },
            TransferValuesPartial = new PartialTransferResponse.TransferValuesPartialResponse { Total = 1, Gmp = 2, Pre97Excess = 4, Post97 = 5 },
        };

        _cacheMock.Setup(c => c.Get<PartialTransferResponse.MdpResponse>(cacheKey))
            .ReturnsAsync(Option<PartialTransferResponse.MdpResponse>.None);

        _clientMock.Setup(c => c.TransferValues(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()))
          .ReturnsAsync(response);

        var result = await _sut.TransferValues(businessGroup, referenceNumber, 30);

        result.IsRight.Should().BeTrue();
        result.Right().PensionTranchesFull.Pre88Gmp.Should().Be(2);
        _clientMock.Verify(c => c.TransferValues(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()), Times.Once);
        _cacheMock.Verify(c => c.Set<PartialTransferResponse.MdpResponse>(cacheKey, It.IsAny<PartialTransferResponse.MdpResponse>(), It.IsAny<TimeSpan>()), Times.Once);
    }

    public async Task RetirementCalculationV2_ReturnsFromCache()
    {
        var referenceNumber = "123";
        var businessGroup = "group1";
        var date = DateTime.UtcNow;
        var cacheKey = $"calc-api-{referenceNumber}-{businessGroup}-retirementV2-{date:yyyy-MM-dd}";
        var cachedResponse = new RetirementRedisStoreV2
        {
            EventType = "PO",
            RetirementResponseV2 = new RetirementResponseV2()
            {
                Results = new ResultsResponse { Mdp = new MdpResponseV2 { MaximumPermittedTotalLumpSum = 123 } }
            }
        };

        _cacheMock.Setup(c => c.Get<RetirementRedisStoreV2>(cacheKey))
            .ReturnsAsync(cachedResponse);

        var result = await _sut.RetirementCalculationV2(referenceNumber, businessGroup, date, false, false);

        result.Right().EventType.Should().Be("PO");
        result.Right().RetirementResponseV2.Results.Mdp.MaximumPermittedTotalLumpSum.Should().Be(123);
        _clientMock.Verify(c => c.RetirementCalculationV2(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
        _cacheMock.Verify(c => c.Set<RetirementResponseV2>(cacheKey, It.IsAny<RetirementResponseV2>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    public async Task RetirementCalculationV2_ReturnsError()
    {
        var referenceNumber = "123";
        var businessGroup = "group1";
        var date = DateTime.UtcNow;
        var cacheKey = $"calc-api-{referenceNumber}-{businessGroup}-retirementV2-{date:yyyy-MM-dd}";

        _cacheMock.Setup(c => c.Get<RetirementRedisStore>(cacheKey))
            .ReturnsAsync(Option<RetirementRedisStore>.None);

        _clientMock.Setup(c => c.RetirementCalculationV2(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<bool>()))
          .ReturnsAsync(Error.New("test message"));

        var result = await _sut.RetirementCalculationV2(referenceNumber, businessGroup, date, false, false);

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("test message");
        _clientMock.Verify(c => c.RetirementCalculationV2(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
        _cacheMock.Verify(c => c.Set<RetirementResponseV2>(cacheKey, It.IsAny<RetirementResponseV2>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    public async Task RetirementCalculationV2_ReturnsFromApi()
    {
        var referenceNumber = "123";
        var businessGroup = "group1";
        var date = DateTime.UtcNow;
        var cacheKey = $"calc-api-{referenceNumber}-{businessGroup}-retirementV2-{date:yyyy-MM-dd}";

        Either<Error, (RetirementResponseV2, string)> retirementResponse = (new RetirementResponseV2()
        {
            Results = new ResultsResponse { Mdp = new MdpResponseV2 { MaximumPermittedTotalLumpSum = 123 } }
        }, "PO");

        _cacheMock.Setup(c => c.Get<RetirementRedisStore>(cacheKey))
            .ReturnsAsync(Option<RetirementRedisStore>.None);

        _clientMock.Setup(c => c.RetirementCalculationV2(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<bool>()))
          .ReturnsAsync(retirementResponse);

        var result = await _sut.RetirementCalculationV2(referenceNumber, businessGroup, date, true, true);

        result.IsRight.Should().BeTrue();
        result.Right().EventType.Should().Be("PO");
        result.Right().RetirementResponseV2.Results.Mdp.MaximumPermittedTotalLumpSum.Should().Be(123);
        _clientMock.Verify(c => c.RetirementCalculationV2(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
        _cacheMock.Verify(c => c.Set<RetirementRedisStoreV2>(cacheKey, It.IsAny<RetirementRedisStoreV2>(), It.IsAny<TimeSpan>()), Times.Once);
        _cacheMock.Verify(c => c.Get<RetirementRedisStoreV2>(cacheKey), Times.Never);
    }

    public async Task RetirementCalculationV2WithLumpSum_ReturnsFromCache()
    {
        var referenceNumber = "123";
        var businessGroup = "group1";
        var date = DateTime.UtcNow;
        var cacheKey = $"calc-api-{referenceNumber}-{businessGroup}-retirementV2-{date:yyyy-MM-dd}-30";
        var cachedResponse = new RetirementRedisStoreV2
        {
            EventType = "PO",
            RetirementResponseV2 = new RetirementResponseV2()
            {
                Results = new ResultsResponse { Mdp = new MdpResponseV2 { MaximumPermittedTotalLumpSum = 123 } }
            }
        };

        _cacheMock.Setup(c => c.Get<RetirementRedisStoreV2>(cacheKey))
            .ReturnsAsync(cachedResponse);

        var result = await _sut.RetirementCalculationV2(referenceNumber, businessGroup, date, 30);

        result.Right().EventType.Should().Be("PO");
        result.Right().RetirementResponseV2.Results.Mdp.MaximumPermittedTotalLumpSum.Should().Be(123);
        _clientMock.Verify(c => c.RetirementCalculationV2(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<decimal>(), It.IsAny<DateTime>()), Times.Never);
        _cacheMock.Verify(c => c.Set<RetirementResponseV2>(cacheKey, It.IsAny<RetirementResponseV2>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    public async Task RetirementCalculationV2WithLumpSum_ReturnsError()
    {
        var referenceNumber = "123";
        var businessGroup = "group1";
        var date = DateTime.UtcNow;
        var cacheKey = $"calc-api-{referenceNumber}-{businessGroup}-retirementV2-{date:yyyy-MM-dd}-30";

        _cacheMock.Setup(c => c.Get<RetirementRedisStore>(cacheKey))
            .ReturnsAsync(Option<RetirementRedisStore>.None);

        _clientMock.Setup(c => c.RetirementCalculationV2(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<decimal>(), It.IsAny<DateTime>()))
          .ReturnsAsync(Error.New("test message"));

        var result = await _sut.RetirementCalculationV2(referenceNumber, businessGroup, date, 30, DateTime.UtcNow);

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("test message");
        _clientMock.Verify(c => c.RetirementCalculationV2(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<decimal>(), It.IsAny<DateTime>()), Times.Once);
        _cacheMock.Verify(c => c.Set<RetirementResponseV2>(cacheKey, It.IsAny<RetirementResponseV2>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    public async Task RetirementCalculationV2WithLumpSum_ReturnsFromApi()
    {
        var referenceNumber = "123";
        var businessGroup = "group1";
        var date = DateTime.UtcNow;
        var cacheKey = $"calc-api-{referenceNumber}-{businessGroup}-retirementV2-{date:yyyy-MM-dd}-30";

        Either<Error, (RetirementResponseV2, string)> retirementResponse = (new RetirementResponseV2()
        {
            Results = new ResultsResponse { Mdp = new MdpResponseV2 { MaximumPermittedTotalLumpSum = 123 } }
        }, "PO");

        _cacheMock.Setup(c => c.Get<RetirementRedisStore>(cacheKey))
            .ReturnsAsync(Option<RetirementRedisStore>.None);

        _clientMock.Setup(c => c.RetirementCalculationV2(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<decimal>(), It.IsAny<DateTime>()))
          .ReturnsAsync(retirementResponse);

        var result = await _sut.RetirementCalculationV2(referenceNumber, businessGroup, date, 30, DateTime.UtcNow);

        result.IsRight.Should().BeTrue();
        result.Right().EventType.Should().Be("PO");
        result.Right().RetirementResponseV2.Results.Mdp.MaximumPermittedTotalLumpSum.Should().Be(123);
        _clientMock.Verify(c => c.RetirementCalculationV2(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<decimal>(), It.IsAny<DateTime>()), Times.Once);
        _cacheMock.Verify(c => c.Set<RetirementRedisStoreV2>(cacheKey, It.IsAny<RetirementRedisStoreV2>(), It.IsAny<TimeSpan>()), Times.Once);
    }
    public async Task RateOfReturn_ReturnsFromCache()
    {
        var referenceNumber = "123";
        var businessGroup = "group1";
        var startDate = DateTimeOffset.UtcNow.AddYears(-1);
        var endDate = DateTimeOffset.UtcNow;
        var cacheKey = $"calc-api-{referenceNumber}-{businessGroup}-rate-of-return-{startDate:yyyy-MM-dd}-{endDate:yyyy-MM-dd}";
        var cachedResponse = new RetirementResponseV2
        {
            Results = new ResultsResponse { Mdp = new MdpResponseV2 { MaximumPermittedTotalLumpSum = 123 } }
        };

        _cacheMock.Setup(c => c.Get<RetirementResponseV2>(cacheKey))
            .ReturnsAsync(Option<RetirementResponseV2>.Some(cachedResponse));

        var result = await _sut.RateOfReturn(businessGroup, referenceNumber, startDate, endDate);

        result.IsRight.Should().BeTrue();
        result.Right().Results.Mdp.MaximumPermittedTotalLumpSum.Should().Be(123);
        _clientMock.Verify(c => c.RateOfReturn(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()), Times.Never);
        _cacheMock.Verify(c => c.Set<RetirementResponseV2>(cacheKey, It.IsAny<RetirementResponseV2>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    public async Task RateOfReturn_ReturnsFromCalcApi()
    {
        var referenceNumber = "123";
        var businessGroup = "group1";
        var startDate = DateTimeOffset.UtcNow.AddYears(-1);
        var endDate = DateTimeOffset.UtcNow;
        var cacheKey = $"calc-api-{referenceNumber}-{businessGroup}-rate-of-return-{startDate:yyyy-MM-dd}-{endDate:yyyy-MM-dd}";

        _cacheMock.Setup(c => c.Get<RetirementResponseV2>(cacheKey))
            .ReturnsAsync(Option<RetirementResponseV2>.None);
        _clientMock.Setup(c => c.RateOfReturn(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(new RetirementResponseV2
            {
                Results = new ResultsResponse { Mdp = new MdpResponseV2 { MaximumPermittedTotalLumpSum = 123 } }
            });

        var result = await _sut.RateOfReturn(businessGroup, referenceNumber, startDate, endDate);

        result.IsRight.Should().BeTrue();
        result.Right().Results.Mdp.MaximumPermittedTotalLumpSum.Should().Be(123);
        _clientMock.Verify(c => c.RateOfReturn(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()), Times.Once);
        _cacheMock.Verify(c => c.Set<RetirementResponseV2>(cacheKey, It.IsAny<RetirementResponseV2>(), It.IsAny<TimeSpan>()), Times.Once);
    }

    public async Task RateOfReturn_ReturnsError_WhenCalcApiReturnsError()
    {
        var referenceNumber = "123";
        var businessGroup = "group1";
        var startDate = DateTimeOffset.UtcNow.AddYears(-1);
        var endDate = DateTimeOffset.UtcNow;
        var cacheKey = $"calc-api-{referenceNumber}-{businessGroup}-rate-of-return-{startDate:yyyy-MM-dd}-{endDate:yyyy-MM-dd}";

        _cacheMock.Setup(c => c.Get<RetirementResponseV2>(cacheKey))
            .ReturnsAsync(Option<RetirementResponseV2>.None);
        _clientMock.Setup(c => c.RateOfReturn(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(Error.New("test message"));

        var result = await _sut.RateOfReturn(businessGroup, referenceNumber, startDate, endDate);

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("test message");
        _clientMock.Verify(c => c.RateOfReturn(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()), Times.Once);
        _cacheMock.Verify(c => c.Set<RetirementResponseV2>(cacheKey, It.IsAny<RetirementResponseV2>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    public async Task RateOfReturn_ReturnsError_WhenExceptionIsThrown()
    {
        var referenceNumber = "123";
        var businessGroup = "group1";
        var startDate = DateTimeOffset.UtcNow.AddYears(-1);
        var endDate = DateTimeOffset.UtcNow;
        var cacheKey = $"calc-api-{referenceNumber}-{businessGroup}-rate-of-return-{startDate:yyyy-MM-dd}-{endDate:yyyy-MM-dd}";

        _cacheMock.Setup(c => c.Get<RetirementResponseV2>(cacheKey))
            .ReturnsAsync(Option<RetirementResponseV2>.None);
        _clientMock.Setup(c => c.RateOfReturn(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()))
            .ThrowsAsync(new ArgumentException("test-message"));

        var result = await _sut.RateOfReturn(businessGroup, referenceNumber, startDate, endDate);

        _logger.VerifyLogging("test-message", LogLevel.Error, Times.Once());
        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("test-message");
        _clientMock.Verify(c => c.RateOfReturn(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()), Times.Once);
        _cacheMock.Verify(c => c.Set<RetirementResponseV2>(cacheKey, It.IsAny<RetirementResponseV2>(), It.IsAny<TimeSpan>()), Times.Never);
    }


    public async Task GetGuaranteedTransfer_ReturnsFromApi()
    {
        var businessGroup = "ABC";
        var referenceNumber = "123";
        var expectedResponse = "http://example.com";

        _clientMock.Setup(c => c.GetGuaranteedTransfer(businessGroup, referenceNumber)).ReturnsAsync(expectedResponse);

        var result = await _sut.GetGuaranteedTransfer(businessGroup, referenceNumber);

        result.IsRight.Should().BeTrue();
        result.Right().Should().Be(expectedResponse);
    }

    public async Task GetGuaranteedTransfer_ReturnsError()
    {
        var businessGroup = "ABC";
        var referenceNumber = "123";
        var expectedError = Error.New("Failed");

        _clientMock.Setup(c => c.GetGuaranteedTransfer(businessGroup, referenceNumber)).ReturnsAsync(expectedError);

        var result = await _sut.GetGuaranteedTransfer(businessGroup, referenceNumber);

        result.IsLeft.Should().BeTrue();
        result.Left().Should().Be(expectedError);
    }

    public async Task GetGuaranteedQuotes_ReturnsGetGuaranteedQuoteResponseFromApi()
    {
        var _stubGetGuaranteedQuoteClientRequest = new GetGuaranteedQuoteClientRequest
        {
            Bgroup = "ABC",
            RefNo = "1234567",
            Event = "retirement",
            GuaranteeDateFrom = DateTimeOffset.UtcNow,
            GuaranteeDateTo = DateTimeOffset.UtcNow,
            PageNumber = 1,
            PageSize = 1,
            QuotationStatus = "GUARANTEED"
        };

        Either<Error, GetGuaranteedQuoteResponse> _stubGuranteedQuotes = new GetGuaranteedQuoteResponse
        {
            Pagination = new Pagination(),
            Quotations = new System.Collections.Generic.List<Quotation>()
        };

        _clientMock.Setup(c => c.GetGuaranteedQuotes(It.IsAny<GetGuaranteedQuoteClientRequest>()))
          .ReturnsAsync(_stubGuranteedQuotes);

        var result = await _sut.GetGuaranteedQuotes(_stubGetGuaranteedQuoteClientRequest);

        result.IsRight.Should().BeTrue();
        result.IfRight(result => result.Should().BeOfType<GetGuaranteedQuoteResponse>());
        _clientMock.Verify(c => c.GetGuaranteedQuotes(It.IsAny<GetGuaranteedQuoteClientRequest>()), Times.Once);
    }

    public async Task GetGuaranteedQuotesFailed_ReturnsErrorResponseFromApi()
    {
        var _stubGetGuaranteedQuoteClientRequest = new GetGuaranteedQuoteClientRequest
        {
            Bgroup = "ABC",
            RefNo = "1234567",
            Event = "retirement",
            GuaranteeDateFrom = DateTimeOffset.UtcNow,
            GuaranteeDateTo = DateTimeOffset.UtcNow,
            PageNumber = 1,
            PageSize = 1,
            QuotationStatus = "GUARANTEED"
        };

        Either<Error, GetGuaranteedQuoteResponse> _stubGuranteedQuotes = new Error();

        _clientMock.Setup(c => c.GetGuaranteedQuotes(It.IsAny<GetGuaranteedQuoteClientRequest>()))
          .ReturnsAsync(_stubGuranteedQuotes);

        var result = await _sut.GetGuaranteedQuotes(_stubGetGuaranteedQuoteClientRequest);

        result.IsRight.Should().BeFalse();
        result.IfRight(result => result.Should().BeOfType<Error>());
        _clientMock.Verify(c => c.GetGuaranteedQuotes(It.IsAny<GetGuaranteedQuoteClientRequest>()), Times.Once);
    }
}