using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Domain.Mdp;
using WTW.MdpService.Domain.Mdp.Calculations;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.MdpService.Infrastructure.MdpDb;
using WTW.MdpService.RetirementJourneys;
using WTW.MdpService.Test.Domain.Mdp.Calculations;
using WTW.MdpService.Test.Domain.Mdp.Journeys;
using WTW.Web.Errors;
using WTW.Web.Serialization;

namespace WTW.MdpService.Test.RetirementJourneys;

public class RetirementJourneyV2ControllerTest
{
    private readonly RetirementJourneyV2Controller _sut;
    private readonly StartRetirementJourneyV3Request _request;
    private readonly RetirementJourney _journey;
    private readonly Calculation _calculation;
    private readonly Mock<IDbContextTransaction> _dbContextTransactionMock;
    private readonly Mock<IRetirementJourneyRepository> _retirementJourneyRepositoryMock;
    private readonly Mock<ICalculationsRepository> _calculationsRepositoryMock;
    private readonly Mock<IMdpUnitOfWork> _mdpDbUnitOfWorkMock;
    private readonly Mock<IQuoteSelectionJourneyRepository> _quoteSelectionJourneyRepositoryMock;
    private readonly Mock<ICalculationsClient> _calculationsClientMock;
    private readonly Mock<ICalculationsParser> _calculationsParserMock;
    private readonly Mock<ILogger<RetirementJourneyV2Controller>> _loggerMock;

    public RetirementJourneyV2ControllerTest()
    {
        _request = new StartRetirementJourneyV3Request { CurrentPageKey = "step1", NextPageKey = "step2" };
        _journey = new RetirementJourneyBuilder().Build().SetCalculation(new CalculationBuilder().BuildV2());
        _calculation = new CalculationBuilder().BuildV2();
        _dbContextTransactionMock = new Mock<IDbContextTransaction>();

        _retirementJourneyRepositoryMock = new Mock<IRetirementJourneyRepository>();
        _calculationsRepositoryMock = new Mock<ICalculationsRepository>();
        _mdpDbUnitOfWorkMock = new Mock<IMdpUnitOfWork>();
        _quoteSelectionJourneyRepositoryMock = new Mock<IQuoteSelectionJourneyRepository>();
        _calculationsClientMock = new Mock<ICalculationsClient>();
        _calculationsParserMock = new Mock<ICalculationsParser>();
        _loggerMock = new Mock<ILogger<RetirementJourneyV2Controller>>();

        _sut = new RetirementJourneyV2Controller(
            _retirementJourneyRepositoryMock.Object,
            _calculationsRepositoryMock.Object,
            _mdpDbUnitOfWorkMock.Object,
            new RetirementJourneyConfiguration(25),
            _quoteSelectionJourneyRepositoryMock.Object,
            _calculationsClientMock.Object,
            _calculationsParserMock.Object,
            _loggerMock.Object);

        _sut.SetupControllerContext();
    }

    public async Task StartJourneyV2Returns400_WhenCalculationDoesNotExist()
    {
        _retirementJourneyRepositoryMock
            .Setup(x => x.FindExpiredJourney(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(_journey);

        var result = await _sut.StartJourneyV2(_request);

        result.Should().BeOfType<BadRequestObjectResult>();
        var response = ((BadRequestObjectResult)result).Value as ApiError;
        response.Errors[0].Message.Should().Be("Calculation does not exist.");
    }

    public async Task StartJourneyV2Returns400_WhenRetirementDateIsOutOfRange()
    {
        _retirementJourneyRepositoryMock
            .Setup(x => x.FindExpiredJourney(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(_journey);
        var calculation = new CalculationBuilder().BuildV2();
        calculation.UpdateEffectiveDate(DateTime.Now.AddMonths(7));
        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(calculation);

        var result = await _sut.StartJourneyV2(_request);

        result.Should().BeOfType<BadRequestObjectResult>();
        var response = ((BadRequestObjectResult)result).Value as ApiError;
        response.Errors[0].Message.Should().Be("Effective retirement date is out of range.");
    }

    public async Task StartJourneyV2Returns400_WhenQuoteSelectionJourneyDoesNotExist()
    {
        _retirementJourneyRepositoryMock
            .Setup(x => x.FindExpiredJourney(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(_journey);
        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(_calculation);

        var result = await _sut.StartJourneyV2(_request);

        result.Should().BeOfType<BadRequestObjectResult>();
        var response = ((BadRequestObjectResult)result).Value as ApiError;
        response.Errors[0].Message.Should().Be("Quote Selection journey does not exist.");
    }

    public async Task StartJourneyV2Throws_WhenCalcApiReturnsError()
    {
        _retirementJourneyRepositoryMock
            .Setup(x => x.FindExpiredJourney(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(_journey);
        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(_calculation);
        _quoteSelectionJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new QuoteSelectionJourney("RBS", "1111122", DateTimeOffset.UtcNow, "step1", "step2", "selected-quote-name"));
        _calculationsParserMock.Setup(x => x.GetRetirementDatesAges(It.IsAny<string>())).Returns(GetRetirementDatesAges());
        _calculationsParserMock
             .Setup(x => x.GetRetirementV2(It.IsAny<string>()))
             .Returns(new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options())));
        _calculationsClientMock
            .Setup(x => x.RetirementCalculationV2WithLock(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<decimal?>()))
            .ReturnsAsync(Error.New("Some error."));
        _mdpDbUnitOfWorkMock.Setup(x => x.BeginTransactionAsync()).ReturnsAsync(_dbContextTransactionMock.Object);

        var action = async () => await _sut.StartJourneyV2(_request);

        await action.Should().ThrowAsync<Exception>();

        _mdpDbUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
        _dbContextTransactionMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _dbContextTransactionMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    public async Task StartJourneyV2Returns204()
    {
        _retirementJourneyRepositoryMock
            .Setup(x => x.FindExpiredJourney(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(_journey);
        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(_calculation);
        _calculationsParserMock.Setup(x => x.GetRetirementDatesAges(It.IsAny<string>())).Returns(GetRetirementDatesAges());
        _calculationsClientMock
            .Setup(x => x.RetirementCalculationV2WithLock(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<decimal?>()))
            .ReturnsAsync(JsonSerializer.Deserialize<RetirementResponseV2>(TestData.RetirementV2ResponseJson, SerialiationBuilder.Options()));
        _calculationsParserMock
            .Setup(x => x.GetRetirementJsonV2(It.IsAny<RetirementResponseV2>(), It.IsAny<string>()))
            .Returns(("{}", "test"));
        _calculationsParserMock
            .Setup(x => x.GetRetirementV2(It.IsAny<string>()))
            .Returns(new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options())));
        _quoteSelectionJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new QuoteSelectionJourney("RBS", "1111122", DateTimeOffset.UtcNow, "step1", "step2", "selected-quote-name"));
        _mdpDbUnitOfWorkMock.Setup(x => x.BeginTransactionAsync()).ReturnsAsync(_dbContextTransactionMock.Object);

        var result = await _sut.StartJourneyV2(_request);

        result.Should().BeOfType<NoContentResult>();
        _mdpDbUnitOfWorkMock.Verify(x => x.Commit(), Times.Exactly(2));
        _retirementJourneyRepositoryMock.Verify(x => x.Create(It.IsAny<RetirementJourney>()), Times.Once);
        _dbContextTransactionMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _dbContextTransactionMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static RetirementDatesAges GetRetirementDatesAges()
    {
        return new RetirementDatesAges(new RetirementDatesAgesDto
        {
            EarliestRetirementAge = 50,
            NormalMinimumPensionAge = 55,
            NormalRetirementAge = 62,
            EarliestRetirementDate = new DateTimeOffset(DateTime.Parse("2018-06-30")),
            NormalMinimumPensionDate = new DateTimeOffset(DateTime.Parse("2023-06-30")),
            NormalRetirementDate = new DateTimeOffset(DateTime.Parse("2030-06-30")),
            TargetRetirementDate = new DateTimeOffset(DateTime.Parse("2030-06-30"))
        });
    }
}