using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WTW.MdpService.Domain.Common.Journeys;
using WTW.MdpService.Domain.Mdp.Calculations;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.Infrastructure.Aws;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.MdpService.Infrastructure.Journeys;
using WTW.MdpService.Infrastructure.MdpDb;
using WTW.MdpService.Infrastructure.MemberDb;
using WTW.MdpService.Infrastructure.Redis;
using WTW.MdpService.Infrastructure.Templates.RetirementApplication.Calculations;
using WTW.MdpService.Journeys.Submit.Services;
using WTW.MdpService.Members;
using WTW.MdpService.Retirement;
using WTW.MdpService.Test.Domain.Mdp.Calculations;
using WTW.MdpService.Test.Domain.Mdp.Journeys;
using WTW.MdpService.Test.Domain.Members;
using WTW.MdpService.TransferJourneys;
using WTW.TestCommon.FixieConfig;
using WTW.TestCommon.Helpers;
using WTW.Web.Errors;
using WTW.Web.Extensions;
using WTW.Web.LanguageExt;
using WTW.Web.Models.Internal;
using WTW.Web.Serialization;
using static LanguageExt.Prelude;

namespace WTW.MdpService.Test.Retirement;

public class RetirementControllerTest
{
    protected RetirementController _controller;
    private readonly Mock<ICalculationsClient> _calculationsClientMock;
    private readonly Mock<IMemberRepository> _memberRepositoryMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly Mock<ILogger<RetirementController>> _logger;
    private readonly Mock<ICalculationsRepository> _calculationsRepositoryMock;
    private readonly Mock<ITransferCalculationRepository> _transferCalculationRepositoryMock;
    private readonly Mock<IMdpUnitOfWork> _mdpUnitOfWorkMock;
    private readonly Mock<ITenantRetirementTimelineRepository> _tenantRetirementTimelineRepositoryMock;
    private readonly Mock<IBankHolidayRepository> _bankHolidayRepositoryMock;
    private readonly Mock<ITransferJourneyRepository> _transferJourneyRepositoryMock;
    private readonly Mock<ICalculationsRedisCache> _calculationsRedisCacheMock;
    private readonly Mock<IRetirementCalculationsPdf> _retirementCalculationsPdfMock;
    private readonly Mock<IJourneyDocumentsRepository> _journeyDocumentsRepositoryMock;
    private readonly Mock<ITransferOutsideAssure> _transferOutsideAssureMock;
    private readonly Mock<IRetirementDatesService> _retirementDatesServiceMock;
    private readonly Mock<ICalculationsParser> _calculationsParserMock;
    private readonly Mock<IGenericJourneyService> _genericJourneyServiceMock;
    private readonly Mock<IAwsClient> _awsClientMock;
    private readonly Mock<IOptionsSnapshot<DatePickerConfigOptions>> _datePickerConfigOptionsMock;
    private readonly Mock<IRateOfReturnService> _rateOfReturnServiceMock;
    private readonly Mock<IDocumentsUploaderService> _documentsUploaderServiceMock;
    private readonly Mock<IJourneysRepository> _journeysRepositoryMock;
    private readonly Mock<IJourneyService> _journeyServiceMock;

    public RetirementControllerTest()
    {
        _calculationsClientMock = new Mock<ICalculationsClient>();
        _memberRepositoryMock = new Mock<IMemberRepository>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _logger = new Mock<ILogger<RetirementController>>();
        _calculationsRepositoryMock = new Mock<ICalculationsRepository>();
        _transferCalculationRepositoryMock = new Mock<ITransferCalculationRepository>();
        _mdpUnitOfWorkMock = new Mock<IMdpUnitOfWork>();
        _tenantRetirementTimelineRepositoryMock = new Mock<ITenantRetirementTimelineRepository>();
        _bankHolidayRepositoryMock = new Mock<IBankHolidayRepository>();
        _transferJourneyRepositoryMock = new Mock<ITransferJourneyRepository>();
        _calculationsRedisCacheMock = new Mock<ICalculationsRedisCache>();
        _retirementCalculationsPdfMock = new Mock<IRetirementCalculationsPdf>();
        _journeyDocumentsRepositoryMock = new Mock<IJourneyDocumentsRepository>();
        _transferOutsideAssureMock = new Mock<ITransferOutsideAssure>();
        _retirementDatesServiceMock = new Mock<IRetirementDatesService>();
        _calculationsParserMock = new Mock<ICalculationsParser>();
        _genericJourneyServiceMock = new Mock<IGenericJourneyService>();
        _awsClientMock = new Mock<IAwsClient>();
        _datePickerConfigOptionsMock = new Mock<IOptionsSnapshot<DatePickerConfigOptions>>();
        _rateOfReturnServiceMock = new Mock<IRateOfReturnService>();
        _documentsUploaderServiceMock = new Mock<IDocumentsUploaderService>();
        _journeysRepositoryMock = new Mock<IJourneysRepository>();
        _journeyServiceMock = new Mock<IJourneyService>();

        _datePickerConfigOptionsMock.Setup(o => o.Value).Returns(new DatePickerConfigOptions
        {
            BusinessGroups = new List<BusinessGroup>
            {
                new BusinessGroup { Bgroup = "RWH", MinQuoteWindowIsoDuration = "P37D", MaxQuoteWindowIsoDuration="P6M"},
                new BusinessGroup { Bgroup = "WPS", MinQuoteWindowIsoDuration = "P37D", MaxQuoteWindowIsoDuration="P6M"}
            }
        });

        _controller = new RetirementController(
            _calculationsClientMock.Object,
            _memberRepositoryMock.Object,
            _calculationsRepositoryMock.Object,
            _transferCalculationRepositoryMock.Object,
            _mdpUnitOfWorkMock.Object,
            _tenantRetirementTimelineRepositoryMock.Object,
            _bankHolidayRepositoryMock.Object,
            _calculationsRedisCacheMock.Object,
            _transferJourneyRepositoryMock.Object,
            _loggerFactoryMock.Object,
            _logger.Object,
            _journeyDocumentsRepositoryMock.Object,
            _retirementCalculationsPdfMock.Object,
            _transferOutsideAssureMock.Object,
            _retirementDatesServiceMock.Object,
            _calculationsParserMock.Object,
            _genericJourneyServiceMock.Object,
            _awsClientMock.Object,
            _datePickerConfigOptionsMock.Object,
            _rateOfReturnServiceMock.Object,
            _documentsUploaderServiceMock.Object,
            _journeysRepositoryMock.Object,
            _journeyServiceMock.Object
            );

        SetupControllerContext();
    }

    [Input(MemberStatus.Pensioner, true, true, false, typeof(OkObjectResult), false)]
    [Input(MemberStatus.Active, true, false, false, typeof(OkObjectResult), false)]
    [Input(MemberStatus.Active, true, true, false, typeof(OkObjectResult), false)]
    [Input(MemberStatus.Active, false, true, false, typeof(OkObjectResult), false)]
    [Input(MemberStatus.Active, true, true, true, typeof(OkObjectResult), true)]
    public async Task RetirementCalculationReturnsCorrectHttpStatusCode(MemberStatus status,
        bool calculationSucceed,
        bool datesAgesCalcApiSucceed,
        bool retirementCalculationV2ApiSucceed,
        Type expectedType,
        bool expectedCalculationResult)
    {
        var member = new MemberBuilder().Status(status).Build();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.Some(member));

        var calculation = GetCalculation();
        if (!calculationSucceed)
            calculation.SetCalculationSuccessStatus(false);
        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(calculation);

        if (datesAgesCalcApiSucceed)
            _calculationsClientMock
               .Setup(x => x.RetirementDatesAges(It.IsAny<string>(), It.IsAny<string>()))
               .Returns(async () => JsonSerializer.Deserialize<RetirementDatesAgesResponse>(TestData.RetitementDateAgesResponseV2, SerialiationBuilder.Options()));

        Either<Error, (RetirementResponseV2 RetirementResponseV2, string EventType)> retirementResponseV2 = Error.New("");
        if (retirementCalculationV2ApiSucceed)
            retirementResponseV2 = (JsonSerializer.Deserialize<RetirementResponseV2>(TestData.RetirementV2ResponseJson, SerialiationBuilder.Options()), "PV");

        Either<Error, GetGuaranteedQuoteResponse> _stubGuaranteedQuote = new GetGuaranteedQuoteResponse();

        _calculationsClientMock
           .Setup(x => x.GetGuaranteedQuotes(It.IsAny<GetGuaranteedQuoteClientRequest>()))
           .ReturnsAsync(_stubGuaranteedQuote);

        _calculationsClientMock
           .Setup(x => x.RetirementCalculationV2(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<bool>(), false))
           .ReturnsAsync(retirementResponseV2);

        var result = await _controller.RetirementCalculation();
        var response = ((OkObjectResult)result).Value as RetirementCalculationResponse;

        result.Should().BeOfType(expectedType);
        response.IsCalculationSuccessful.Should().Be(expectedCalculationResult);
        if (retirementCalculationV2ApiSucceed)
        {
            response.TotalAVCFund.Should().Be(null);
            response.TotalPension.Should().Be(32369.4M);
        }
    }


    [Input(MemberStatus.Active, true, true, typeof(OkObjectResult), true, true)]
    [Input(MemberStatus.Active, true, false, typeof(OkObjectResult), false, false)]
    public async Task RetirementCalculationWithGuranteedQuoteReturnsCorrectHttpStatusCode(MemberStatus status,
        bool calculationSucceed,
        bool retirementCalculationV2ApiSucceed,
        Type expectedType,
        bool guaranteedQuoteExist,
        bool expectedCalculationResult)
    {
        var member = new MemberBuilder().Status(status).Build();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.Some(member));

        var calculation = GetCalculation();
        calculation.SetCalculationSuccessStatus(calculationSucceed);
        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(calculation);

        Either<Error, (RetirementResponseV2 RetirementResponseV2, string EventType)> retirementResponseV2 = Error.New("");
        if (retirementCalculationV2ApiSucceed)
            retirementResponseV2 = (JsonSerializer.Deserialize<RetirementResponseV2>(TestData.RetirementV2ResponseJson, SerialiationBuilder.Options()), "PV");

        _calculationsClientMock
           .Setup(x => x.RetirementDatesAges(It.IsAny<string>(), It.IsAny<string>()))
           .Returns(async () => JsonSerializer.Deserialize<RetirementDatesAgesResponse>(TestData.RetitementDateAgesResponseV2, SerialiationBuilder.Options()));


        _calculationsParserMock
            .Setup(x => x.IsGuaranteedQuoteEnabled(It.IsAny<string>())).Returns(true);

        Either<Error, GetGuaranteedQuoteResponse> _stubGuaranteedQuote = Error.New("");

        if (guaranteedQuoteExist)
        {
            _stubGuaranteedQuote = new GetGuaranteedQuoteResponse
            {
                Quotations = new List<Quotation>
                { new Quotation
                {
                    Event = "PV",
                    EffectiveDate = DateTime.UtcNow
                }}
            };
        }

        _calculationsClientMock
           .Setup(x => x.GetGuaranteedQuotes(It.IsAny<GetGuaranteedQuoteClientRequest>()))
           .ReturnsAsync(_stubGuaranteedQuote);

        _calculationsClientMock
           .Setup(x => x.RetirementCalculationV2(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<bool>()))
           .ReturnsAsync(retirementResponseV2);

        var result = await _controller.RetirementCalculation();

        var response = ((OkObjectResult)result).Value as RetirementCalculationResponse;

        result.Should().BeOfType(expectedType);
        response.IsCalculationSuccessful.Should().Be(expectedCalculationResult);
        if (retirementCalculationV2ApiSucceed)
        {
            response.TotalAVCFund.Should().Be(null);
            response.TotalPension.Should().Be(32369.4M);
        }
        if (!guaranteedQuoteExist)
        {
            _logger.VerifyLogging($"GetGuaranteedQuotes failed with error: {_stubGuaranteedQuote.Left().Message}", LogLevel.Error, Times.Once());
        }
    }

    public async Task RetirementCalculationAndGetGuaranteedQuotesReturnError()
    {
        var member = new MemberBuilder().Status(MemberStatus.Active).Build();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.Some(member));

        var calculation = GetCalculation();

        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(calculation);

        Either<Error, GetGuaranteedQuoteResponse> _stubGuaranteedQuote = new Error();

        _calculationsClientMock
           .Setup(x => x.GetGuaranteedQuotes(It.IsAny<GetGuaranteedQuoteClientRequest>()))
           .ReturnsAsync(_stubGuaranteedQuote);

        var result = await _controller.RetirementCalculation();
        var response = ((OkObjectResult)result).Value as RetirementCalculationResponse;

        result.Should().BeOfType(typeof(OkObjectResult));
        response.IsCalculationSuccessful.Should().Be(false);
    }

    [Input(MemberStatus.Pensioner, false, typeof(OkObjectResult), false)]
    [Input(MemberStatus.Active, false, typeof(OkObjectResult), false)]
    [Input(MemberStatus.Active, true, typeof(OkObjectResult), true)]
    public async Task GetOptionsReturnsCorrectHttpStatusCode(MemberStatus status, bool retirementCalculationV2ApiSucceed, Type expectedType, bool expectedCalculationResult)
    {
        var member = new MemberBuilder().Status(status).Build();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.Some(member));

        Either<Error, (RetirementResponseV2 RetirementResponseV2, string EventType)> retirementResponseV2 = Error.New("");
        if (retirementCalculationV2ApiSucceed)
            retirementResponseV2 = (JsonSerializer.Deserialize<RetirementResponseV2>(TestData.RetirementV2ResponseJson, SerialiationBuilder.Options()), "PV");

        _calculationsClientMock
           .Setup(x => x.RetirementCalculationV2(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<bool>()))
           .ReturnsAsync(retirementResponseV2);

        var result = await _controller.GetOptions(4);
        var response = ((OkObjectResult)result).Value as OptionsResponse;

        result.Should().BeOfType(expectedType);
        response.IsCalculationSuccessful.Should().Be(expectedCalculationResult);
        if (retirementCalculationV2ApiSucceed)
        {
            response.FullPensionYearlyIncome.Should().Be(32369.4M);
            response.MaxLumpSum.Should().Be(165346.35M);
            response.MaxLumpSumYearlyIncome.Should().Be(24801.96M);
        }
    }

    [Input(true, true, true, false)]
    [Input(false, false, true, false)]
    [Input(false, true, false, true)]
    [Input(false, true, true, false)]
    public async Task QuotesV3ReturnsCorrectResult_WhenCalculationResultIsTrue(bool validCalculationWithJourneyExists,
        bool calculationExists,
        bool retirementCalculationV2ApiSucceed,
        bool setLumpSum)
    {
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(new MemberBuilder().Status(MemberStatus.Active).Build());

        var calculation = GetCalculation();
        calculation.UpdateRetirementV2(TestData.RetirementJsonV2, TestData.RetirementQuotesJsonV2, It.IsAny<DateTime>(), It.IsAny<DateTimeOffset>(), It.IsAny<bool>(), It.IsAny<DateTime>());
        if (setLumpSum)
            calculation.SetEnteredLumpSum(222);
        if (validCalculationWithJourneyExists)
            _calculationsRepositoryMock
                .Setup(x => x.FindWithValidRetirementJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
                .ReturnsAsync(calculation);

        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(calculationExists ? calculation : null);

        _calculationsClientMock
           .Setup(x => x.RetirementDatesAges(It.IsAny<string>(), It.IsAny<string>()))
           .Returns(async () => JsonSerializer.Deserialize<RetirementDatesAgesResponse>(TestData.RetitementDateAgesResponseV2, SerialiationBuilder.Options()));

        Either<Error, (RetirementResponseV2 RetirementResponseV2, string EventType)> retirementResponseV2 = Error.New("");

        if (retirementCalculationV2ApiSucceed)
            retirementResponseV2 = (JsonSerializer.Deserialize<RetirementResponseV2>(TestData.RetirementV2ResponseJson, SerialiationBuilder.Options()), "PV");
        _calculationsClientMock
           .Setup(x => x.RetirementCalculationV2(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<bool>()))
           .ReturnsAsync(retirementResponseV2);

        _calculationsParserMock
              .Setup(x => x.GetRetirementV2(It.IsAny<string>()))
              .Returns(new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options())));

        _calculationsParserMock
              .Setup(x => x.GetQuotesV2(It.IsAny<string>()))
              .Returns(JsonSerializer.Deserialize<MdpResponseV2>(TestData.RetirementQuotesJsonV2, SerialiationBuilder.Options()));

        var result = await _controller.QuotesV3(new RetirementQuotesRequest() { SelectedRetirementDate = It.IsAny<DateTime>() });

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as QuotesResponseV2;
        response.IsCalculationSuccessful.Should().Be(true);
        response.Quotes.Should().NotBeNull();
        response.WordingFlags.Should().NotBeNullOrEmpty();
        response.TotalAvcFundValue.Should().BeNull();
    }

    public async Task QuotesV3ReturnsHttp404StatusCode()
    {
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.None);

        var result = await _controller.QuotesV3(new RetirementQuotesRequest() { SelectedRetirementDate = It.IsAny<DateTime>() });

        result.Should().BeOfType<NotFoundObjectResult>();
        var response = ((NotFoundObjectResult)result).Value as ApiError;
        response.Errors[0].Code.Should().BeNull();
        response.Errors[0].Message.Should().Be("Not found");
    }

    [Input(MemberStatus.Pensioner, false, false)]
    [Input(MemberStatus.Active, false, true)]
    [Input(MemberStatus.Active, true, false)]
    public async Task QuotesV3ReturnsResult_WhenCalculationResultIsFalse(MemberStatus status, bool datesAgesCalcApiSucceed, bool setLumpSum)
    {
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(new MemberBuilder().Status(status).Build());

        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<Calculation>.None);

        if (datesAgesCalcApiSucceed)
            _calculationsClientMock
               .Setup(x => x.RetirementDatesAges(It.IsAny<string>(), It.IsAny<string>()))
               .Returns(async () => JsonSerializer.Deserialize<RetirementDatesAgesResponse>(TestData.RetitementDateAgesResponseV2, SerialiationBuilder.Options()));

        _calculationsClientMock
           .Setup(x => x.RetirementCalculationV2(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<bool>()))
           .ReturnsAsync(Error.New(""));

        var result = await _controller.QuotesV3(new RetirementQuotesRequest() { SelectedRetirementDate = It.IsAny<DateTime>() });

        result.Should().BeOfType<OkObjectResult>();

        var response = ((OkObjectResult)result).Value as QuotesResponseV2;
        response.IsCalculationSuccessful.Should().BeFalse();
        response.Quotes.Should().BeNull();
        response.WordingFlags.Should().BeNull();
        response.TotalAvcFundValue.Should().BeNull();
    }

    public async Task QuotesV3ReturnsCorrectResult_WhenCalculationExistButCalcApiFails()
    {
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(new MemberBuilder().Status(MemberStatus.Active).Build());

        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(GetCalculation());

        _calculationsClientMock
           .Setup(x => x.RetirementDatesAges(It.IsAny<string>(), It.IsAny<string>()))
           .Returns(async () => JsonSerializer.Deserialize<RetirementDatesAgesResponse>(TestData.RetitementDateAgesResponseV2, SerialiationBuilder.Options()));

        _calculationsClientMock
           .Setup(x => x.RetirementCalculationV2(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<bool>()))
           .ReturnsAsync(Error.New(""));

        var result = await _controller.QuotesV3(new RetirementQuotesRequest() { SelectedRetirementDate = It.IsAny<DateTime>() });

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as QuotesResponseV2;
        response.IsCalculationSuccessful.Should().BeFalse();
        response.Quotes.Should().BeNull();
        response.WordingFlags.Should().BeNull();
        response.TotalAvcFundValue.Should().BeNull();
        response.IsCalculationSuccessful.Should().BeFalse();
        _mdpUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
    }

    public async Task QuotesV3ReturnsCorrectResult_WhenCalculationIsNotSuccessful()
    {
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(new MemberBuilder().Status(MemberStatus.Active).Build());

        var calculation = GetCalculation();
        calculation.UpdateCalculationSuccessStatus(false);
        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(calculation);

        var result = await _controller.QuotesV3(new RetirementQuotesRequest() { SelectedRetirementDate = It.IsAny<DateTime>() });

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as QuotesResponseV2;
        response.IsCalculationSuccessful.Should().BeFalse();
        _mdpUnitOfWorkMock.Verify(x => x.Commit(), Times.Never);
    }

    public async Task WhenGuaranteedQuotesIsCalledWithValidInput_ThenExpectedGetGuaranteedQuoteResponseIsReturned()
    {
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(new MemberBuilder().Status(MemberStatus.Active).Build());

        Either<Error, GetGuaranteedQuoteResponse> _stubGetGuaranteedQuoteResponse = new GetGuaranteedQuoteResponse();

        _calculationsClientMock.Setup(x => x.GetGuaranteedQuotes(It.IsAny<GetGuaranteedQuoteClientRequest>()))
            .ReturnsAsync(_stubGetGuaranteedQuoteResponse);

        var _stubGuaranteedQuotesRequest = new GuaranteedQuotesRequest
        {
            Event = "",
            GuaranteeDateFrom = DateTime.UtcNow,
            GuaranteeDateTo = DateTime.UtcNow,
            PageNumber = 0,
            PageSize = 0,
            QuotationStatus = ""
        };

        var result = await _controller.GuaranteedQuotes(_stubGuaranteedQuotesRequest);

        result.Should().BeOfType<OkObjectResult>();

        var response = ((OkObjectResult)result).Value;

        response.Should().BeOfType<GetGuaranteedQuoteResponse>();
    }


    public async Task WhenGuaranteedQuotesIsCalledAndCalculationClientErrored_ThenExpectedErrorIsReturned()
    {
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(new MemberBuilder().Status(MemberStatus.Active).Build());

        Either<Error, GetGuaranteedQuoteResponse> _stubGetGuaranteedQuoteResponse = new Error();

        _calculationsClientMock.Setup(x => x.GetGuaranteedQuotes(It.IsAny<GetGuaranteedQuoteClientRequest>()))
            .ReturnsAsync(_stubGetGuaranteedQuoteResponse);

        var _stubGuaranteedQuotesRequest = new GuaranteedQuotesRequest
        {
            Event = "",
            GuaranteeDateFrom = DateTime.UtcNow,
            GuaranteeDateTo = DateTime.UtcNow,
            PageNumber = 0,
            PageSize = 0,
            QuotationStatus = ""
        };

        var result = await _controller.GuaranteedQuotes(_stubGuaranteedQuotesRequest);

        result.Should().BeOfType<OkObjectResult>();

        var response = ((OkObjectResult)result).Value;

        response.Should().BeOfType<Error>();
    }

    public async Task WhenGuaranteedQuotesIsCalledWithInvalid_ThenExpectedErrorIsReturned()
    {
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(new MemberBuilder().Status(MemberStatus.Active).Build());

        Either<Error, GetGuaranteedQuoteResponse> _stubGetGuaranteedQuoteResponse = new Error();

        _calculationsClientMock.Setup(x => x.GetGuaranteedQuotes(It.IsAny<GetGuaranteedQuoteClientRequest>()))
            .ReturnsAsync(_stubGetGuaranteedQuoteResponse);

        var _stubGuaranteedQuotesRequest = new GuaranteedQuotesRequest
        {
            Event = "INVALID",
            GuaranteeDateFrom = DateTime.UtcNow,
            GuaranteeDateTo = DateTime.UtcNow,
            PageNumber = 0,
            PageSize = 0,
            QuotationStatus = ""
        };

        var result = await _controller.GuaranteedQuotes(_stubGuaranteedQuotesRequest);

        result.Should().BeOfType<OkObjectResult>();

        var response = ((OkObjectResult)result).Value;

        response.Should().BeOfType<Error>();
    }

    public async Task WhenGuaranteedQuotesIsCalledAndExceptionRaised_ThenExpectedErrorIsReturned()
    {
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(new MemberBuilder().Status(MemberStatus.Active).Build());

        Either<Error, GetGuaranteedQuoteResponse> _stubGetGuaranteedQuoteResponse = new Error();

        _calculationsClientMock.Setup(x => x.GetGuaranteedQuotes(It.IsAny<GetGuaranteedQuoteClientRequest>()))
            .ReturnsAsync(_stubGetGuaranteedQuoteResponse);

        var _stubGuaranteedQuotesRequest = new GuaranteedQuotesRequest
        {
            Event = "",
            GuaranteeDateFrom = DateTime.UtcNow,
            GuaranteeDateTo = DateTime.UtcNow,
            PageNumber = 0,
            PageSize = 0,
            QuotationStatus = ""
        };

        var result = await _controller.GuaranteedQuotes(_stubGuaranteedQuotesRequest);

        result.Should().BeOfType<OkObjectResult>();

        var response = ((OkObjectResult)result).Value;

        response.Should().BeOfType<Error>();
    }

    [Input(MemberStatus.Pensioner, false, false, false, typeof(NotFoundObjectResult), false)]
    [Input(MemberStatus.Pensioner, true, false, false, typeof(OkObjectResult), false)]
    [Input(MemberStatus.Active, true, false, false, typeof(OkObjectResult), false)]
    [Input(MemberStatus.Active, true, true, false, typeof(OkObjectResult), false)]
    [Input(MemberStatus.Active, true, true, true, typeof(OkObjectResult), true)]
    public async Task RecalculateLumpSumReturnsCorrectHttpStatusCode(MemberStatus status,
        bool memberExists,
        bool calculationExists,
        bool retirementCalculationV2ApiSucceed,
        Type expectedType,
        bool expectedCalculationResult)
    {

        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(memberExists ? new MemberBuilder().Status(status).Build() : null);

        var calculation = GetCalculation();
        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(calculationExists ? calculation : null);

        Either<Error, (RetirementResponseV2 RetirementResponseV2, string EventType)> retirementResponseV2 = Error.New("");
        if (retirementCalculationV2ApiSucceed)
            retirementResponseV2 = (JsonSerializer.Deserialize<RetirementResponseV2>(TestData.RetirementV2ResponseJson, SerialiationBuilder.Options()), "PV");
        _calculationsClientMock
           .Setup(x => x.RetirementCalculationV2(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<decimal>(), It.IsAny<DateTime>()))
           .ReturnsAsync(retirementResponseV2);

        _calculationsParserMock.Setup(x => x.GetCalculationFactorDate(It.IsAny<string>())).Returns(DateTime.Now);

        var result = await _controller.RecalculateLumpSum(It.IsAny<decimal>());

        result.Should().BeOfType(expectedType);
        if (result.GetType() == typeof(OkObjectResult) && expectedCalculationResult)
        {
            var response = ((OkObjectResult)result).Value as RecalculatedLumpSumResponse;
            response.IsCalculationSuccessful.Should().Be(expectedCalculationResult);
            response.Quotes.Should().NotBeNull();
        }
    }

    [Input(false, false, typeof(NotFoundObjectResult))]
    [Input(true, false, typeof(BadRequestObjectResult))]
    [Input(true, true, typeof(NoContentResult))]
    public async Task ClearLumpSumReturnsCorrectHttpStatusCode(bool memberExists, bool calculationExists, Type expectedType)
    {
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(memberExists ? new MemberBuilder().Build() : null);

        var calculation = GetCalculation();
        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(calculationExists ? calculation : null);

        var result = await _controller.ClearLumpSum();

        result.Should().BeOfType(expectedType);
    }

    [Input(false, false, true, typeof(BadRequestObjectResult))]
    [Input(true, false, true, typeof(BadRequestObjectResult))]
    [Input(true, true, false, typeof(OkObjectResult))]
    [Input(true, true, true, typeof(NoContentResult))]
    public async Task SubmitRecalculateLumpSumReturnsCorrectHttpStatusCode(bool memberExists, bool calculationExists, bool retirementCalculationV2ApiSucceed, Type expectedType)
    {
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(memberExists ? new MemberBuilder().Build() : null);

        var calculation = GetCalculation();
        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(calculationExists ? calculation : null);

        Either<Error, (RetirementResponseV2 RetirementResponseV2, string EventType)> retirementResponseV2 = Error.New("");
        if (retirementCalculationV2ApiSucceed)
            retirementResponseV2 = (JsonSerializer.Deserialize<RetirementResponseV2>(TestData.RetirementV2ResponseJson, SerialiationBuilder.Options()), "PV");

        _calculationsClientMock
           .Setup(x => x.RetirementCalculationV2(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<decimal>(), null))
           .ReturnsAsync(retirementResponseV2);

        var result = await _controller.SubmitRecalculateLumpSum(It.IsAny<decimal>());

        result.Should().BeOfType(expectedType);
        if (result.GetType() == typeof(OkObjectResult))
        {
            var response = ((OkObjectResult)result).Value as QuotesResponse;
            response.IsCalculationSuccessful.Should().BeFalse();
        }
    }

    [Input(false, false, typeof(NotFoundObjectResult))]
    [Input(true, false, typeof(BadRequestObjectResult))]
    [Input(true, true, typeof(FileContentResult))]
    public async Task GenerateSummaryPdfReturnsCorrectHttpStatusCode(bool memberExists, bool calculationExists, Type expectedType)
    {
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(memberExists ? new MemberBuilder().Build() : null);

        var calculation = GetCalculation();
        calculation.UpdateRetirementV2(TestData.RetirementJsonV2, TestData.RetirementQuotesJsonV2, It.IsAny<DateTime>(), It.IsAny<DateTimeOffset>());
        calculation.UpdateEffectiveDate(DateTime.Now);
        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(calculationExists ? calculation : null);

        _calculationsParserMock
              .Setup(x => x.GetRetirementV2(It.IsAny<string>()))
              .Returns(new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options())));

        using var mem = new MemoryStream();
        _retirementCalculationsPdfMock
            .Setup(x => x.GenerateSummaryPdf(It.IsAny<string>(), It.IsAny<Calculation>(), It.IsAny<Member>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(mem);

        var result = await _controller.GenerateSummaryPdf(It.IsAny<string>(), "test.test1");

        result.Should().BeOfType(expectedType);
    }

    [Input(false, false, typeof(NotFoundObjectResult))]
    [Input(true, false, typeof(BadRequestObjectResult))]
    [Input(true, true, typeof(FileContentResult))]
    public async Task GenerateOptionsPdfReturnsCorrectHttpStatusCode(bool memberExists, bool calculationExists, Type expectedType)
    {
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(memberExists ? new MemberBuilder().Build() : null);

        var calculation = GetCalculation();
        calculation.UpdateRetirementV2(TestData.RetirementJsonV2, TestData.RetirementQuotesJsonV2, It.IsAny<DateTime>(), It.IsAny<DateTimeOffset>());
        calculation.UpdateEffectiveDate(DateTime.Now);
        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(calculationExists ? calculation : null);

        _calculationsParserMock
              .Setup(x => x.GetRetirementV2(It.IsAny<string>()))
              .Returns(new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options())));

        using var mem = new MemoryStream();
        _retirementCalculationsPdfMock
            .Setup(x => x.GenerateOptionsPdf(It.IsAny<string>(), It.IsAny<Calculation>(), It.IsAny<Member>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(mem);

        var result = await _controller.GenerateOptionsPdf(It.IsAny<string>());

        result.Should().BeOfType(expectedType);
    }

    [Input("RBS", MemberStatus.Pensioner, false, typeof(OkObjectResult), false)]
    [Input("RBS", MemberStatus.Active, false, typeof(OkObjectResult), false)]
    [Input("RBS", MemberStatus.Active, true, typeof(OkObjectResult), true)]
    public async Task GetRetirementDatev2ReturnsCorrectHttpStatusCode(string bussinessGroup, MemberStatus status, bool calculationExists, Type expectedType, bool expectedCalculationResult)
    {
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(new MemberBuilder().Status(status).Build());

        var calculation = GetCalculation();
        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(calculationExists ? calculation : null);

        _calculationsParserMock
                .Setup(x => x.GetRetirementDatesAges(It.IsAny<string>()))
                .Returns(new RetirementDatesAges(JsonSerializer.Deserialize<RetirementDatesAgesDto>(TestData.RetiremntDatesAgesJson, SerialiationBuilder.Options())));

        var result = await _controller.GetRetirementDatev2();

        result.Should().BeOfType(expectedType);
        var response = ((OkObjectResult)result).Value as RetirementDateResponse;
        response.IsCalculationSuccessful.Should().Be(expectedCalculationResult);
        if (expectedCalculationResult)
        {
            response.RetirementDate.Should().NotBeNull();
            response.RetirementDate.Should().NotBe(DateTimeOffset.MinValue);
            response.DateOfBirth.Should().NotBeNull();
            response.DateOfBirth.Should().NotBe(DateTimeOffset.MinValue);
            response.AvailableRetirementDateRange.Should().NotBeNull();
            response.AvailableRetirementDateRange.From.Should().NotBe(default);
            response.AvailableRetirementDateRange.To.Should().NotBe(default);
        }
    }

    [Input(true, true)]
    [Input(false, false)]
    [Input(null, false)]
    public async Task GetRetirementDatev2ReturnsCorrectHttpStatusForDatePickerConfiguredTenants(bool? calculationSuccessStatus, bool expectedCalculationResult)
    {
        var id = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim("business_group", "WPS"),
            new Claim("reference_number", "TestReferenceNumber"),
        };

        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(new MemberBuilder().Status(MemberStatus.Active).Build());

        _calculationsParserMock
                .Setup(x => x.IsGuaranteedQuoteEnabled(It.IsAny<string>()))
                .Returns(true);

        var _stubGetGuaranteedQuoteResponse = new GetGuaranteedQuoteResponse
        {
            Quotations = new List<Quotation>
                { new Quotation
                {
                    Event = "PV",
                    EffectiveDate = DateTime.UtcNow
                }}
        };
        _calculationsClientMock
            .Setup(x => x.GetGuaranteedQuotes(It.IsAny<GetGuaranteedQuoteClientRequest>()))
            .ReturnsAsync(_stubGetGuaranteedQuoteResponse);

        var calculation = GetCalculation();
        calculation.SetCalculationSuccessStatus(calculationSuccessStatus);
        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(calculation);

        _calculationsParserMock
                .Setup(x => x.GetRetirementDatesAges(It.IsAny<string>()))
                .Returns(new RetirementDatesAges(JsonSerializer.Deserialize<RetirementDatesAgesDto>(TestData.RetiremntDatesAgesJson, SerialiationBuilder.Options())));

        var result = await _controller.GetRetirementDatev2();

        result.Should().BeOfType(typeof(OkObjectResult));
        var response = ((OkObjectResult)result).Value as RetirementDateResponse;
        response.IsCalculationSuccessful.Should().Be(expectedCalculationResult);
        response.RetirementDate.Should().NotBeNull();
        response.RetirementDate.Should().NotBe(DateTimeOffset.MinValue);
        response.DateOfBirth.Should().NotBeNull();
        response.DateOfBirth.Should().NotBe(DateTimeOffset.MinValue);
        response.AvailableRetirementDateRange.Should().NotBeNull();
        response.AvailableRetirementDateRange.From.Should().NotBe(default);
        response.AvailableRetirementDateRange.To.Should().NotBe(default);
        response.GuaranteedQuoteEffectiveDateList.Should().NotBeNull();
    }

    [Input(true, "")]
    [Input(true, "IOM")]
    [Input(false, "GSY")]
    [Input(false, "JSY")]
    [Input(false, "IOM")]
    public async Task GetRetirementDatev2ForGMPOnlyOrCrownDependencyMemberThenCorrectRetirementDateRangeIsReturned(bool GMPOnly, string memberJuridiction)
    {
        var id = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim("business_group", "RBS"),
            new Claim("reference_number", "TestReferenceNumber"),
        };

        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(new MemberBuilder().Status(MemberStatus.Active).Build());

        _calculationsParserMock
                .Setup(x => x.IsGuaranteedQuoteEnabled(It.IsAny<string>()))
                .Returns(true);

        _calculationsParserMock
            .Setup(x => x.IsMemberGMPONly(It.IsAny<Member>(), It.IsAny<string>()))
            .ReturnsAsync(GMPOnly);

        _calculationsParserMock
            .Setup(x => x.GetMemberJuridiction(It.IsAny<Member>(), It.IsAny<string>()))
            .ReturnsAsync(memberJuridiction);

        DateTime _stubFromDate = DateTime.UtcNow;
        DateTime _stubToDate = DateTime.UtcNow.AddMonths(3);

        _calculationsParserMock
            .Setup(x => x.EvaluateDateRangeForGMPOrCrownDependencyMember(It.IsAny<Member>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(() => (_stubFromDate, _stubToDate));

        var _stubGetGuaranteedQuoteResponse = new GetGuaranteedQuoteResponse
        {
            Quotations = new List<Quotation>
                { new Quotation
                {
                    Event = "PV",
                    EffectiveDate = DateTime.UtcNow
                }}
        };
        _calculationsClientMock
            .Setup(x => x.GetGuaranteedQuotes(It.IsAny<GetGuaranteedQuoteClientRequest>()))
            .ReturnsAsync(_stubGetGuaranteedQuoteResponse);

        var calculation = GetCalculation();
        calculation.SetCalculationSuccessStatus(true);
        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(calculation);

        _calculationsParserMock
                .Setup(x => x.GetRetirementDatesAges(It.IsAny<string>()))
                .Returns(new RetirementDatesAges(JsonSerializer.Deserialize<RetirementDatesAgesDto>(TestData.RetiremntDatesAgesJson, SerialiationBuilder.Options())));

        var result = await _controller.GetRetirementDatev2();

        result.Should().BeOfType(typeof(OkObjectResult));
        var response = ((OkObjectResult)result).Value as RetirementDateResponse;
        response.IsCalculationSuccessful.Should().Be(true);
        response.RetirementDate.Should().NotBeNull();
        response.RetirementDate.Should().NotBe(DateTimeOffset.MinValue);
        response.DateOfBirth.Should().NotBeNull();
        response.DateOfBirth.Should().NotBe(DateTimeOffset.MinValue);
        response.AvailableRetirementDateRange.Should().NotBeNull();
        response.AvailableRetirementDateRange.From.ToString("yyyy/MM/dd").Should().Be(_stubFromDate.ToString("yyyy/MM/dd"));
        response.AvailableRetirementDateRange.To.ToString("yyyy/MM/dd").Should().Be(_stubToDate.ToString("yyyy/MM/dd"));
        response.GuaranteedQuoteEffectiveDateList.Should().NotBeNull();
    }


    public async Task GetRetirementDatev2eturnsCorrectResult_WhenCalculationIsNotSuccessful()
    {
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(new MemberBuilder().Status(MemberStatus.Active).Build());

        var calculation = GetCalculation();
        calculation.SetCalculationSuccessStatus(false);
        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(calculation);

        var result = await _controller.GetRetirementDatev2();

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as RetirementDateResponse;
        response.IsCalculationSuccessful.Should().BeFalse();
    }

    [Input(false, true, false, false, null, typeof(OkObjectResult), false)]
    [Input(true, false, false, false, null, typeof(OkObjectResult), false)]
    [Input(true, true, false, false, null, typeof(OkObjectResult), true)]
    [Input(true, true, true, false, null, typeof(OkObjectResult), true)]
    [Input(true, true, true, true, null, typeof(OkObjectResult), false)]
    [Input(true, true, true, true, TestData.TransferQuoteJson, typeof(OkObjectResult), true)]
    public async Task GetTransferQuoteReturnsCorrectHttpStatusCodeAndResponseModel(bool datesAgesCalcApiSucceed,
        bool transferCalculationCalcApiSucceed,
        bool isTransferLocked,
        bool transferCalculationsExists,
        string transferJson,
        Type expectedType,
        bool expectedResult)
    {
        _memberRepositoryMock
           .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
           .ReturnsAsync(Option<Member>.Some(new MemberBuilder().Build()));

        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(transferCalculationsExists ? new TransferCalculation(It.IsAny<string>(), It.IsAny<string>(), transferJson, DateTimeOffset.UtcNow) : null);

        if (datesAgesCalcApiSucceed)
            _calculationsClientMock
               .Setup(x => x.RetirementDatesAges(It.IsAny<string>(), It.IsAny<string>()))
               .Returns(async () => new RetirementDatesAgesResponse { HasLockedInTransferQuote = isTransferLocked });

        _calculationsClientMock
               .Setup(x => x.TransferCalculation(It.IsAny<string>(), It.IsAny<string>(), false))
               .Returns(async () => transferCalculationCalcApiSucceed ? JsonSerializer.Deserialize<TransferResponse>(TestData.TransferQuoteResponseJson, SerialiationBuilder.Options()) : Error.New(""));

        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(isTransferLocked ? null : new TransferJourneyBuilder().BuildWithSteps());

        _calculationsParserMock
               .Setup(x => x.GetTransferQuote(It.IsAny<string>()))
               .Returns(new TransferQuote(JsonSerializer.Deserialize<TransferQuoteDto>(TestData.TransferQuoteJson, SerialiationBuilder.Options())));

        _memberRepositoryMock
           .Setup(x => x.IsMemberValidForTransferCalculation(It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(true);

        var result = await _controller.GetTransferQuote();
        var response = ((OkObjectResult)result).Value as TransferOptionsResponse;

        result.Should().BeOfType(expectedType);
        response.IsCalculationSuccessful.Should().Be(expectedResult);
    }

    public async Task GetTransferQuoteCalculationBlockedForSomeJspMembers()
    {
        _memberRepositoryMock
           .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
           .ReturnsAsync(Option<Member>.Some(new MemberBuilder().BusinessGroup("JSP").Build()));

        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferCalculation>.None);

        _calculationsClientMock
            .Setup(x => x.RetirementDatesAges(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(async () => new RetirementDatesAgesResponse { HasLockedInTransferQuote = false });

        _memberRepositoryMock
           .Setup(x => x.IsMemberValidForTransferCalculation(It.IsAny<string>(), "JSP"))
           .ReturnsAsync(false);

        var result = await _controller.GetTransferQuote();
        var response = ((OkObjectResult)result).Value as TransferOptionsResponse;

        result.Should().BeOfType(typeof(OkObjectResult));
        response.IsCalculationSuccessful.Should().Be(false);
        _transferCalculationRepositoryMock.Verify(x => x.CreateIfNotExists(It.IsAny<TransferCalculation>()), Times.Once);
    }

    public async Task GetCmsInformationForTokenReturnsCorrectResult_WhenMemberIsNotEligibleForRetirement()
    {
        var member = new MemberBuilder().Name("Test", "Test 1").Status(MemberStatus.Pensioner).Build();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(member);

        var result = await _controller.GetCmsInformationForToken();

        result.Should().BeOfType(typeof(OkObjectResult));
        var response = ((OkObjectResult)result).Value as CmsTokenInformationResponse;
        response.IsCalculationSuccessful.Should().BeFalse();
        response.Name.Should().Be("Test");
        response.AgeAtSelectedRetirementDateIso.Should().BeNull();
    }

    public async Task GetCmsInformationForTokenReturnsCorrectResult_WhenRetirementCalculationDoesNotExist()
    {
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(new MemberBuilder().Build());

        _calculationsClientMock
               .Setup(x => x.RetirementDatesAges(It.IsAny<string>(), It.IsAny<string>()))
               .Returns(async () => JsonSerializer.Deserialize<RetirementDatesAgesResponse>(TestData.RetitementDateAgesResponseV2, SerialiationBuilder.Options()));
        _retirementDatesServiceMock
            .Setup(x => x.GetFormattedTimeUntilNormalRetirement(It.IsAny<Member>(), It.IsAny<RetirementDatesAges>(), It.IsAny<DateTimeOffset>()))
            .Returns("1Y2M3W4D");

        _retirementDatesServiceMock
            .Setup(x => x.GetFormattedTimeUntilTargetRetirement(It.IsAny<RetirementDatesAges>(), It.IsAny<DateTimeOffset>()))
            .Returns("0Y2M3W4D");

        var result = await _controller.GetCmsInformationForToken();

        result.Should().BeOfType(typeof(OkObjectResult));
        var response = ((OkObjectResult)result).Value as CmsTokenInformationResponse;
        response.IsCalculationSuccessful.Should().BeFalse();
        response.DateOfBirth.Should().Be(DateTime.Parse("1985-10-10"));
        response.Email.Should().Be("test@gmail.com");
        response.PhoneNumber.Should().BeNull();
        response.RetirementJourneyExpirationDate.Should().BeNull();
        response.SystemDate.Should().BeAfter(DateTime.Now.AddHours(-1));
        response.InsuranceNumber.Should().Be("insuranceNumber");
        response.Address.Should().BeNull();
        response.NormalRetirementAge.Should().Be(60);
        response.EarliestRetirementAge.Should().Be(55);
        response.EarliestRetirementDate.Should().NotBeNull();
        response.LatestRetirementAge.Should().Be(75);
        response.TimeToNormalRetirementIso.Should().Be("1Y2M3W4D");

        response.NormalRetirementDate.Value.Date.Should().Be(DateTime.Parse("2022-11-07"));
        response.TargetRetirementDate.Value.Date.Should().Be(DateTime.Parse("2022-11-07"));
        response.TargetRetirementAgeIso.Should().Be("P65Y20D");
        response.TimeToTargetRetirementIso.Should().Be("0Y2M3W4D");
        response.AgeAtNormalRetirementIso.Should().Be("P60Y17D");
    }

    public async Task GetCmsInformationForTokenReturnsCorrectResult_RetirementCalculationsExistAndTransferCalculationDoesNot()
    {
        var member = new MemberBuilder().Category("1335").DateOfBirth(DateTimeOffset.UtcNow.AddYears(-37).Date).Build();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(member);

        _calculationsClientMock
               .Setup(x => x.RetirementDatesAges(It.IsAny<string>(), It.IsAny<string>()))
               .Returns(async () => JsonSerializer.Deserialize<RetirementDatesAgesResponse>(TestData.RetitementDateAgesResponseV2, SerialiationBuilder.Options()));
        _retirementDatesServiceMock
           .Setup(x => x.GetFormattedTimeUntilNormalRetirement(It.IsAny<Member>(), It.IsAny<RetirementDatesAges>(), It.IsAny<DateTimeOffset>()))
           .Returns("1Y2M3W4D");
        _retirementDatesServiceMock
           .Setup(x => x.GetFormattedTimeUntilTargetRetirement(It.IsAny<RetirementDatesAges>(), It.IsAny<DateTimeOffset>()))
           .Returns("0Y2M3W4D");

        _retirementDatesServiceMock
           .Setup(x => x.GetRetirementApplicationExpiryDate(It.IsAny<Calculation>(), It.IsAny<Member>(), It.IsAny<DateTimeOffset>()))
           .ReturnsAsync(DateTimeOffset.UtcNow);

        var calculation = GetCalculation();
        calculation.UpdateRetirementV2(TestData.RetirementJsonV2, TestData.RetirementQuotesJsonV2, It.IsAny<DateTime>(), It.IsAny<DateTimeOffset>());
        calculation.UpdateEffectiveDate(DateTime.Now);
        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(calculation);

        _tenantRetirementTimelineRepositoryMock
            .Setup(x => x.FindPentionPayTimelines(It.IsAny<string>()))
            .ReturnsAsync(Timelines());

        _calculationsParserMock
               .Setup(x => x.GetTransferQuote(It.IsAny<string>()))
               .Returns(new TransferQuote(JsonSerializer.Deserialize<TransferQuoteDto>(TestData.TransferQuoteJson, SerialiationBuilder.Options())));

        _calculationsParserMock
                .Setup(x => x.GetRetirementDatesAges(It.IsAny<string>()))
                .Returns(new RetirementDatesAges(JsonSerializer.Deserialize<RetirementDatesAgesDto>(TestData.RetiremntDatesAgesJson, SerialiationBuilder.Options())));

        _calculationsParserMock
                      .Setup(x => x.GetRetirementV2(It.IsAny<string>()))
                      .Returns(new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options())));

        _calculationsParserMock
            .Setup(x => x.IsGuaranteedQuoteEnabled(It.IsAny<string>()))
            .Returns(true);

        var expectedJourney = new RetirementJourneyBuilder().BuildWithSubmissionDate(DateTimeOffset.UtcNow);

        _journeyServiceMock.Setup(x => x.FindUnexpiredOrSubmittedJourney(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(expectedJourney);

        var result = await _controller.GetCmsInformationForToken();

        result.Should().BeOfType(typeof(OkObjectResult));
        var response = ((OkObjectResult)result).Value as CmsTokenInformationResponse;
        response.IsCalculationSuccessful.Should().BeTrue();
        response.DateOfBirth.Should().Be(DateTimeOffset.UtcNow.AddYears(-37).Date);
        response.Email.Should().Be("test@gmail.com");
        response.PhoneNumber.Should().BeNull();
        response.RetirementJourneyExpirationDate.Should().NotBeNull();
        response.SystemDate.Should().BeAfter(DateTime.Now.AddHours(-1));
        response.InsuranceNumber.Should().Be("insuranceNumber");
        response.Address.Should().BeNull();
        response.NormalRetirementAge.Should().Be(60);
        response.EarliestRetirementAge.Should().Be(55);
        response.EarliestRetirementDate.Should().NotBeNull();
        response.LatestRetirementAge.Should().BeNull();
        response.SubmissionDate.Should().NotBeNull();
        response.SelectedRetirementDate.Should().NotBeNull();
        response.SelectedRetirementAge.Should().NotBeNull();
        response.RemainingLtaPercentage.Should().Be(0);
        response.LtaLimit.Should().Be(1073100.0M);
        response.GmpAgeYears.Should().Be(60);
        response.GmpAgeMonths.Should().Be(0);
        response.Pre88GMPAtGMPAge.Should().Be(960.96M);
        response.Post88GMPAtGMPAge.Should().Be(1604.2M);
        response.Post88GMPIncreaseCap.Should().Be(3M);
        response.StatePensionDeduction.Should().BeNull();
        response.NormalMinimumPensionAgeYears.Should().Be(55);
        response.NormalMinimumPensionAgeMonths.Should().Be(0);
        response.NormalRetirementDate.Value.Date.Should().Be(DateTime.Parse("2022-11-07"));
        response.TargetRetirementDate.Value.Date.Should().Be(DateTime.Parse("2022-11-07"));
        response.TargetRetirementAgeIso.Should().Be("P65Y20D");
        response.TargetRetirementAge.Should().Be(66);
        response.TimeToTargetRetirementIso.Should().Be("0Y2M3W4D");
        response.PensionPaymentDay.Should().Be("23");
        response.ChosenLtaPercentage.Should().BeNull();
        response.TimeToNormalRetirementIso.Should().Be("1Y2M3W4D");
        response.CurrentAgeIso.Should().Be(member.CurrentAgeIso(DateTimeOffset.UtcNow));
        response.AgeAtNormalRetirementIso.Should().Be("P60Y17D");
    }

    public async Task GetCmsInformationForTokenReturnsCorrectResult_WhenTransferAndRetirementCalculationsExist()
    {
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(new MemberBuilder().Category("1335").Build());

        _calculationsClientMock
               .Setup(x => x.RetirementDatesAges(It.IsAny<string>(), It.IsAny<string>()))
               .Returns(async () => JsonSerializer.Deserialize<RetirementDatesAgesResponse>(TestData.RetitementDateAgesResponseV2, SerialiationBuilder.Options()));
        _retirementDatesServiceMock
          .Setup(x => x.GetFormattedTimeUntilNormalRetirement(It.IsAny<Member>(), It.IsAny<RetirementDatesAges>(), It.IsAny<DateTimeOffset>()))
          .Returns("1Y2M3W4D");
        _retirementDatesServiceMock
          .Setup(x => x.GetFormattedTimeUntilTargetRetirement(It.IsAny<RetirementDatesAges>(), It.IsAny<DateTimeOffset>()))
          .Returns("0Y2M3W4D");
        _retirementDatesServiceMock
          .Setup(x => x.GetRetirementApplicationExpiryDate(It.IsAny<Calculation>(), It.IsAny<Member>(), It.IsAny<DateTimeOffset>()))
          .ReturnsAsync(DateTimeOffset.UtcNow);

        var calculation = GetCalculation();
        calculation.UpdateRetirementV2(TestData.RetirementJsonV2, TestData.RetirementQuotesJsonV2, It.IsAny<DateTime>(), It.IsAny<DateTimeOffset>());
        calculation.UpdateEffectiveDate(DateTime.Now);
        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(calculation);

        _transferCalculationRepositoryMock
           .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(new TransferCalculation(It.IsAny<string>(), It.IsAny<string>(), TestData.TransferQuoteJson, DateTimeOffset.UtcNow));

        _tenantRetirementTimelineRepositoryMock
            .Setup(x => x.FindPentionPayTimelines(It.IsAny<string>()))
            .ReturnsAsync(Timelines());

        _calculationsParserMock
             .Setup(x => x.GetTransferQuote(It.IsAny<string>()))
             .Returns(new TransferQuote(JsonSerializer.Deserialize<TransferQuoteDto>(TestData.TransferQuoteJson, SerialiationBuilder.Options())));

        _calculationsParserMock
                .Setup(x => x.GetRetirementDatesAges(It.IsAny<string>()))
                .Returns(new RetirementDatesAges(JsonSerializer.Deserialize<RetirementDatesAgesDto>(TestData.RetiremntDatesAgesJson, SerialiationBuilder.Options())));

        _calculationsParserMock
                      .Setup(x => x.GetRetirementV2(It.IsAny<string>()))
                      .Returns(new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options())));

        _calculationsParserMock
            .Setup(x => x.IsGuaranteedQuoteEnabled(It.IsAny<string>()))
            .Returns(true);

        var result = await _controller.GetCmsInformationForToken();

        result.Should().BeOfType(typeof(OkObjectResult));
        var response = ((OkObjectResult)result).Value as CmsTokenInformationResponse;
        response.IsCalculationSuccessful.Should().BeTrue();
        response.DateOfBirth.Should().Be(DateTime.Parse("1985-10-10"));
        response.Email.Should().Be("test@gmail.com");
        response.PhoneNumber.Should().BeNull();
        response.RetirementJourneyExpirationDate.Should().NotBeNull();
        response.SystemDate.Should().BeAfter(DateTime.Now.AddHours(-1));
        response.InsuranceNumber.Should().Be("insuranceNumber");
        response.Address.Should().BeNull();
        response.NormalRetirementAge.Should().Be(60);
        response.EarliestRetirementAge.Should().Be(55);
        response.EarliestRetirementDate.Should().NotBeNull();
        response.LatestRetirementAge.Should().BeNull();
        response.NormalRetirementDate.Value.Date.Should().Be(DateTime.Parse("2022-11-07"));
        response.TargetRetirementDate.Value.Date.Should().Be(DateTime.Parse("2022-11-07"));
        response.TargetRetirementAgeIso.Should().Be("P65Y20D");
        response.TimeToTargetRetirementIso.Should().Be("0Y2M3W4D");
        response.SubmissionDate.Should().BeNull();
        response.SelectedRetirementDate.Should().NotBeNull();
        response.SelectedRetirementAge.Should().NotBeNull();
        response.RemainingLtaPercentage.Should().Be(0);
        response.LtaLimit.Should().Be(1073100.0M);
        response.GmpAgeYears.Should().Be(60);
        response.GmpAgeMonths.Should().Be(0);
        response.Pre88GMPAtGMPAge.Should().Be(960.96M);
        response.Post88GMPAtGMPAge.Should().Be(1604.2M);
        response.Post88GMPIncreaseCap.Should().Be(3M);
        response.StatePensionDeduction.Should().BeNull();
        response.NormalMinimumPensionAgeYears.Should().Be(55);
        response.NormalMinimumPensionAgeMonths.Should().Be(0);
        response.PensionPaymentDay.Should().Be("23");
        response.ChosenLtaPercentage.Should().BeNull();

        response.TransferReplyByDate.Should().NotBeNull();
        response.TransferGuaranteeExpiryDate.Should().NotBeNull();
        response.TransferGuaranteePeriodMonths.Should().NotBeNull();
        response.TransferQuoteRunDate.Should().NotBeNull();
        response.TimeToNormalRetirementIso.Should().Be("1Y2M3W4D");
        response.AgeAtNormalRetirementIso.Should().Be("P60Y17D");
    }

    public async Task GetCmsInformationForTokenReturnsCorrectResult_WhenMemberSchemeTypeIsDC()
    {
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(new MemberBuilder().SchemeType("DC").Build());

        _calculationsClientMock
               .Setup(x => x.RetirementDatesAges(It.IsAny<string>(), It.IsAny<string>()))
               .Returns(async () => JsonSerializer.Deserialize<RetirementDatesAgesResponse>(TestData.RetitementDateAgesResponseV2, SerialiationBuilder.Options()));
        _retirementDatesServiceMock
            .Setup(x => x.GetFormattedTimeUntilNormalRetirement(It.IsAny<Member>(), It.IsAny<RetirementDatesAges>(), It.IsAny<DateTimeOffset>()))
            .Returns("1Y2M3W4D");

        _retirementDatesServiceMock
            .Setup(x => x.GetFormattedTimeUntilTargetRetirement(It.IsAny<RetirementDatesAges>(), It.IsAny<DateTimeOffset>()))
            .Returns("0Y2M3W4D");

        var result = await _controller.GetCmsInformationForToken();

        result.Should().BeOfType(typeof(OkObjectResult));
        var response = ((OkObjectResult)result).Value as CmsTokenInformationResponse;
        response.IsCalculationSuccessful.Should().BeFalse();
        response.DateOfBirth.Should().Be(DateTime.Parse("1985-10-10"));
        response.Email.Should().Be("test@gmail.com");
        response.PhoneNumber.Should().BeNull();
        response.RetirementJourneyExpirationDate.Should().BeNull();
        response.SystemDate.Should().BeAfter(DateTime.Now.AddHours(-1));
        response.InsuranceNumber.Should().Be("insuranceNumber");
        response.Address.Should().BeNull();
        response.NormalRetirementAge.Should().Be(60);
        response.EarliestRetirementAge.Should().Be(55);
        response.EarliestRetirementDate.Should().NotBeNull();
        response.LatestRetirementAge.Should().Be(75);
        response.TimeToNormalRetirementIso.Should().Be("1Y2M3W4D");
        response.NormalRetirementDate.Value.Date.Should().Be(DateTime.Parse("2022-11-07"));
        response.TargetRetirementDate.Value.Date.Should().Be(DateTime.Parse("2022-11-07"));
        response.TargetRetirementAgeIso.Should().Be("P65Y20D");
        response.TimeToTargetRetirementIso.Should().Be("0Y2M3W4D");
        response.AgeAtNormalRetirementIso.Should().Be("P60Y17D");
    }

    public async Task GetCmsInformationForTokenReturnsCorrectResult_WhenCalculationIsNotSuccessful()
    {
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(new MemberBuilder().Category("1335").DateOfBirth(DateTimeOffset.UtcNow.AddYears(-37).Date).Build());

        _retirementDatesServiceMock
           .Setup(x => x.GetFormattedTimeUntilNormalRetirement(It.IsAny<Member>(), It.IsAny<RetirementDatesAges>(), It.IsAny<DateTimeOffset>()))
           .Returns("1Y2M3W4D");
        _retirementDatesServiceMock
           .Setup(x => x.GetFormattedTimeUntilTargetRetirement(It.IsAny<RetirementDatesAges>(), It.IsAny<DateTimeOffset>()))
           .Returns("0Y2M3W4D");

        _retirementDatesServiceMock
           .Setup(x => x.GetRetirementApplicationExpiryDate(It.IsAny<Calculation>(), It.IsAny<Member>(), It.IsAny<DateTimeOffset>()))
           .ReturnsAsync(DateTimeOffset.UtcNow);

        var calculation = GetCalculation();
        calculation.SetCalculationSuccessStatus(false);
        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(calculation);

        _tenantRetirementTimelineRepositoryMock
            .Setup(x => x.FindPentionPayTimelines(It.IsAny<string>()))
            .ReturnsAsync(Timelines());

        _calculationsParserMock
                .Setup(x => x.GetRetirementDatesAges(It.IsAny<string>()))
                .Returns(new RetirementDatesAges(JsonSerializer.Deserialize<RetirementDatesAgesDto>(TestData.RetiremntDatesAgesJson, SerialiationBuilder.Options())));

        _calculationsParserMock
            .Setup(x => x.IsGuaranteedQuoteEnabled(It.IsAny<string>()))
            .Returns(true);

        var result = await _controller.GetCmsInformationForToken();

        result.Should().BeOfType(typeof(OkObjectResult));
        var response = ((OkObjectResult)result).Value as CmsTokenInformationResponse;
        response.IsCalculationSuccessful.Should().BeFalse();
        response.DateOfBirth.Should().Be(DateTimeOffset.UtcNow.AddYears(-37).Date);
        response.Email.Should().Be("test@gmail.com");
        response.PhoneNumber.Should().BeNull();
        response.RetirementJourneyExpirationDate.Should().NotBeNull();
        response.SystemDate.Should().BeAfter(DateTime.Now.AddHours(-1));
        response.InsuranceNumber.Should().Be("insuranceNumber");
        response.Address.Should().BeNull();
        response.NormalRetirementAge.Should().Be(60);
        response.EarliestRetirementAge.Should().Be(55);
        response.EarliestRetirementDate.Should().NotBeNull();
        response.LatestRetirementAge.Should().BeNull();

        response.SelectedRetirementDate.Should().NotBeNull();
        response.SelectedRetirementAge.Should().NotBeNull();
        response.RemainingLtaPercentage.Should().Be(0);
    }

    public async Task GetCmsInformationForTokenReturnsCorrectResult_WithTransferWhenCalculationIsNotSuccessful()
    {
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(new MemberBuilder().Category("1335").DateOfBirth(DateTimeOffset.UtcNow.AddYears(-37).Date).Build());

        _retirementDatesServiceMock
           .Setup(x => x.GetFormattedTimeUntilNormalRetirement(It.IsAny<Member>(), It.IsAny<RetirementDatesAges>(), It.IsAny<DateTimeOffset>()))
           .Returns("1Y2M3W4D");
        _retirementDatesServiceMock
           .Setup(x => x.GetFormattedTimeUntilTargetRetirement(It.IsAny<RetirementDatesAges>(), It.IsAny<DateTimeOffset>()))
           .Returns("0Y2M3W4D");

        _retirementDatesServiceMock
           .Setup(x => x.GetRetirementApplicationExpiryDate(It.IsAny<Calculation>(), It.IsAny<Member>(), It.IsAny<DateTimeOffset>()))
           .ReturnsAsync(DateTimeOffset.UtcNow);

        var calculation = GetCalculation();
        calculation.SetCalculationSuccessStatus(false);
        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(calculation);

        _tenantRetirementTimelineRepositoryMock
            .Setup(x => x.FindPentionPayTimelines(It.IsAny<string>()))
            .ReturnsAsync(Timelines());

        _calculationsParserMock
                .Setup(x => x.GetRetirementDatesAges(It.IsAny<string>()))
                .Returns(new RetirementDatesAges(JsonSerializer.Deserialize<RetirementDatesAgesDto>(TestData.RetiremntDatesAgesJson, SerialiationBuilder.Options())));

        _transferCalculationRepositoryMock
           .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(new TransferCalculation(It.IsAny<string>(), It.IsAny<string>(), TestData.TransferQuoteJson, DateTimeOffset.UtcNow));

        var transferQuote = new TransferQuote(JsonSerializer.Deserialize<TransferQuoteDto>(TestData.TransferQuoteJson, SerialiationBuilder.Options()));
        _calculationsParserMock
               .Setup(x => x.GetTransferQuote(It.IsAny<string>()))
               .Returns(transferQuote);

        _calculationsParserMock
            .Setup(x => x.IsGuaranteedQuoteEnabled(It.IsAny<string>()))
            .Returns(true);

        var result = await _controller.GetCmsInformationForToken();

        result.Should().BeOfType(typeof(OkObjectResult));
        var response = ((OkObjectResult)result).Value as CmsTokenInformationResponse;
        response.IsCalculationSuccessful.Should().BeFalse();
        response.DateOfBirth.Should().Be(DateTimeOffset.UtcNow.AddYears(-37).Date);
        response.Email.Should().Be("test@gmail.com");
        response.PhoneNumber.Should().BeNull();

        response.TransferReplyByDate.Should().NotBeNull();
        response.TransferReplyByDate.Should().Be(transferQuote.ReplyByDate);
        response.TransferGuaranteeExpiryDate.Should().NotBeNull();
        response.TransferGuaranteeExpiryDate.Should().Be(transferQuote.GuaranteeDate);
        response.TransferGuaranteePeriodMonths.Should().NotBeNull();
        response.TransferGuaranteePeriodMonths.Should().Be(transferQuote.GuaranteePeriod.ParseIsoDuration()?.Months);
        response.TransferQuoteRunDate.Should().NotBeNull();
        response.TransferQuoteRunDate.Should().Be(transferQuote.OriginalEffectiveDate);
    }

    [Input(false, typeof(NotFoundObjectResult))]
    [Input(true, typeof(OkObjectResult))]
    public async Task TimelineReturnsCorrectResults(bool retirementCalculationExists, Type expectedReturnType)
    {
        var date = DateTime.Now;
        var calculation = GetCalculation();
        calculation.UpdateRetirementV2(TestData.RetirementJsonV2, TestData.RetirementQuotesJsonV2, It.IsAny<DateTime>(), It.IsAny<DateTimeOffset>());
        calculation.UpdateEffectiveDate(date);
        if (retirementCalculationExists)
            _calculationsRepositoryMock
                .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(calculation);

        _tenantRetirementTimelineRepositoryMock
           .Setup(x => x.Find(It.IsAny<string>()))
           .ReturnsAsync(Timelines());

        _bankHolidayRepositoryMock
            .Setup(x => x.ListFrom(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<BankHoliday>());

        _calculationsParserMock
              .Setup(x => x.GetRetirementV2(It.IsAny<string>()))
              .Returns(new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options())));

        var result = await _controller.Timeline();

        result.Should().BeOfType(expectedReturnType);
        if (expectedReturnType == typeof(OkObjectResult))
        {
            var response = ((OkObjectResult)result).Value as RetirementTimelineResponse;
            response.RetirementDate.Should().Be(date);
            response.EarliestStartRaDateForSelectedDate.Should().BeNull();
            response.LatestStartRaDateForSelectedDate.Should().BeBefore(date);
            response.RetirementConfirmationDate.Should().BeBefore(date);
            response.FirstMonthlyPensionPayDate.Should().BeAfter(date);
            response.LumpSumPayDate.Should().Be(date);
        }
    }

    public async Task GetRetirementQuote_ReturnsBadRequest_WhenRetirementQuoteFails()
    {
        _calculationsRepositoryMock.Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new CalculationBuilder().BuildV2());
        _calculationsClientMock.Setup(x => x.RetirementQuote(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync(Left<Error, (string, int)>(Error.New("Error")));

        var result = await _controller.GetRetirementQuote();

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    public async Task GetRetirementQuote_ReturnsBadRequest_WhenAwsClientFails()
    {
        _calculationsRepositoryMock.Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new CalculationBuilder().BuildV2());
        _calculationsClientMock.Setup(x => x.RetirementQuote(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync(Right<Error, (string, int)>(("s3://dev-test/TEST/test.pdf", 1)));
        _awsClientMock
            .Setup(x => x.File(It.IsAny<string>()))
            .ReturnsAsync(Left<Error, MemoryStream>(Error.New("Error")));

        var result = await _controller.GetRetirementQuote();

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    public async Task GetRetirementQuote_ReturnsFileResult_WhenSuccessful()
    {
        _calculationsRepositoryMock.Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new CalculationBuilder().BuildV2());
        _calculationsClientMock.Setup(x => x.RetirementQuote(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync(Right<Error, (string, int)>(("s3://dev-test/TEST/test.pdf", 1)));
        _awsClientMock.Setup(x => x.File(It.IsAny<string>()))
            .ReturnsAsync(Right<Error, MemoryStream>(new MemoryStream()));

        var result = await _controller.GetRetirementQuote();

        _documentsUploaderServiceMock
            .Verify(x => x.UploadNonCaseRetirementQuoteDocument(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>(), It.IsAny<Int32>()), Times.Once);
        result.Should().BeOfType<FileStreamResult>();
    }

    public async Task GetRetirementQuote_ReturnsNotFound_WhenCalculationDoesNotExist()
    {
        _calculationsRepositoryMock.Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(Option<Calculation>.None);

        var result = await _controller.GetRetirementQuote();

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Input("TestBusinessGroup", "TestReferenceNumber", "2023-11-27T00:00:00Z", "2024-11-27T00:00:00Z", true, typeof(OkObjectResult))]
    [Input("TestBusinessGroup", "TestReferenceNumber", "2023-01-01T00:00:00Z", "2023-12-31T00:00:00Z", false, typeof(BadRequestObjectResult))]
    public async Task GetRateOfReturnReturnsCorrectHttpStatusCode(string businessGroup, string referenceNumber, string startDateString, string effectiveDateString, bool isSuccessful, Type expectedType)
    {
        var startDate = DateTimeOffset.Parse(startDateString);
        var effectiveDate = DateTimeOffset.Parse(effectiveDateString);

        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(new MemberBuilder().Status(MemberStatus.Active).Build());

        var rateOfReturnResponse = new RateOfReturnResponse
        {
            personalRateOfReturn = 18.0454M,
            changeInValue = 10719.44M
        };

        Either<Error, Option<RateOfReturnResponse>> rateOfReturnResult = isSuccessful
            ? Right<Error, Option<RateOfReturnResponse>>(Option<RateOfReturnResponse>.Some(rateOfReturnResponse))
            : Left<Error, Option<RateOfReturnResponse>>(Error.New("Error"));

        _rateOfReturnServiceMock
            .Setup(x => x.GetRateOfReturn(businessGroup, referenceNumber, startDate, effectiveDate))
            .ReturnsAsync(rateOfReturnResult);

        var result = await _controller.GetRateOfReturn(startDate, effectiveDate);
        result.Should().BeOfType(expectedType);

        if (isSuccessful)
        {
            var response = ((OkObjectResult)result).Value as RateOfReturnResponse;
            response.Should().NotBeNull();
            response.personalRateOfReturn.Should().Be(18.0454M);
            response.changeInValue.Should().Be(10719.44M);
        }
    }

    public async Task GetDbCoreRetirementApplicationStatus_Returns404_WhenMemberDoesNotExist()
    {
        var result = await _controller.GetDbCoreRetirementApplicationStatus(10, 12, DateTime.Now);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task GetDbCoreRetirementApplicationStatus_ReturnsCorrectStatusDetails_WhenCalculationRecordDoesNotExist()
    {
        _memberRepositoryMock
           .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
           .ReturnsAsync(new MemberBuilder().Status(MemberStatus.Active).Build());

        var result = await _controller.GetDbCoreRetirementApplicationStatus(10, 12, DateTime.Now);

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as RetirementApplicationStatusResponse;
        response.RetirementApplicationStatus.Should().Be(RetirementApplicationStatus.Undefined);
        response.EarliestStartRaDateForSelectedDate.Should().BeNull();
        response.LatestStartRaDateForSelectedDate.Should().BeNull();
        response.ExpirationRaDateForSelectedDate.Should().BeNull();
        response.LifeStage.Should().Be(MemberLifeStage.Undefined);

        _logger.VerifyLogging("Calculation record does not exist. Unable to get proper db core application status and dates.",
            LogLevel.Warning, Times.Once());
    }

    [Input("2025-07-17", "2025-06-10", false)]
    [Input("2025-07-22", "2025-06-15", false)]
    [Input("2030-09-17", "2030-08-11", true)]
    [Input("2030-09-22", "2030-08-16", true)]
    public async Task GetDbCoreRetirementApplicationStatus_ReturnsCorrectStatusDetails_WhenDbCoreRetirementApplicationDoesNotExist(
        string selectedDbCoreRetirementDate,
        string expectedLatestStartRaDateForSelectedDate,
        bool expectedEarliestStartRaDateForSelectedDateHasValue)
    {
        _memberRepositoryMock
           .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
           .ReturnsAsync(new MemberBuilder().Status(MemberStatus.Active).Build());

        _calculationsRepositoryMock.Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new CalculationBuilder().BuildV2());

        var _stubRetirementDatesAges = new RetirementDatesAges(JsonSerializer.Deserialize<RetirementDatesAgesDto>(TestData.RetiremntDatesAgesJson, SerialiationBuilder.Options()));

        _calculationsParserMock
               .Setup(x => x.GetRetirementDatesAges(It.IsAny<string>()))
               .Returns(_stubRetirementDatesAges);

        var result = await _controller.GetDbCoreRetirementApplicationStatus(10, 12, DateTime.Parse(selectedDbCoreRetirementDate));

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as RetirementApplicationStatusResponse;
        response.RetirementApplicationStatus.Should().Be(RetirementApplicationStatus.NotEligibleToStart);
        response.EarliestStartRaDateForSelectedDate.HasValue.Should().Be(expectedEarliestStartRaDateForSelectedDateHasValue);
        response.LatestStartRaDateForSelectedDate.Value.Date.Should().Be(DateTime.Parse(expectedLatestStartRaDateForSelectedDate).Date);
        response.ExpirationRaDateForSelectedDate.Should().BeNull();
        response.LifeStage.Should().Be(MemberLifeStage.NotEligibleToRetire);
    }

    public async Task GetDbCoreRetirementApplicationStatus_ReturnsCorrectStatusDetails_WhenDbCoreRetirementApplicationExists()
    {
        _memberRepositoryMock
           .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
           .ReturnsAsync(new MemberBuilder().Status(MemberStatus.Active).Build());

        _calculationsRepositoryMock.Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new CalculationBuilder().BuildV2());

        _calculationsParserMock
               .Setup(x => x.GetRetirementDatesAges(It.IsAny<string>()))
               .Returns(new RetirementDatesAges(JsonSerializer.Deserialize<RetirementDatesAgesDto>(TestData.RetiremntDatesAgesJson, SerialiationBuilder.Options())));

        _journeysRepositoryMock.Setup(repo => repo.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(new GenericJourney("RBS", "1234567", "transfer", "step1", "step2", true, "Started", DateTimeOffset.UtcNow));

        var result = await _controller.GetDbCoreRetirementApplicationStatus(10, 12, DateTime.Parse("2025-03-06"));

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as RetirementApplicationStatusResponse;
        response.RetirementApplicationStatus.Should().Be(RetirementApplicationStatus.StartedRA);
        response.EarliestStartRaDateForSelectedDate.Should().BeNull();
        response.LatestStartRaDateForSelectedDate.Should().NotBeNull();
        response.ExpirationRaDateForSelectedDate.Should().BeNull();
        response.LifeStage.Should().Be(MemberLifeStage.NotEligibleToRetire);
    }

    private static Calculation GetCalculation()
    {
        return new Calculation(It.IsAny<string>(), It.IsAny<string>(), TestData.RetiremntDatesAgesJson, TestData.RetirementJsonV2, It.IsAny<DateTime>(), It.IsAny<DateTimeOffset>(), It.IsAny<bool?>(), It.IsAny<bool>(), DateTime.UtcNow);
    }

    private void SetupControllerContext()
    {
        var id = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim("business_group", "TestBusinessGroup"),
            new Claim("reference_number", "TestReferenceNumber"),
        };

        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    private static List<TenantRetirementTimeline> Timelines()
    {
        return new List<TenantRetirementTimeline>()
        {
            new TenantRetirementTimeline(1, "RETFIRSTPAYMADE","RT", "RBS", "*",
                "1300,1302,1303,1304,1325,1326,1330,1332,1333,1334,1335,1336,1340,1342,1343,1344,1345,1346", "+,0,+,0,0,23,+"),
            new TenantRetirementTimeline(2, "RETLSRECD","RT", "TestBusinessGroup",
                "1400,1402,1403,1404,1425,1426,1435,1436,1445,1446", "*", "+,0,+,0,0,30,+"),
            new TenantRetirementTimeline(1, "RETFIRSTPAYMADE","RT", "TestBusinessGroup",
                "2100,2102,2103,2125,2126,2135,2136,2140,2142,2143,2145,2146,2200,2202,2203,2225,2226",
                "3100,3102,3103,3125,3126,3130,3132,3133,3135,3136,3140,3142,3143,3145,3146", "+,0,+,0,0,18"),
        };
    }

    public async Task GetLatestProtectedQuote_WhenGuaranteedQuoteNotEnabled_ReturnsBadRequest()
    {
        var member = new MemberBuilder()
            .DateOfBirth(DateTime.UtcNow.AddYears(-40))
            .Build();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.Some(member));

        _calculationsParserMock
            .Setup(x => x.IsGuaranteedQuoteEnabled("TestBusinessGroup"))
            .Returns(false);

        var result = await _controller.GetLatestProtectedQuote();

        result.Should().BeOfType<BadRequestObjectResult>();
        var response = ((BadRequestObjectResult)result).Value as ApiError;
        response.Errors[0].Message.Should().Be("Guaranteed quote is not enabled for this business group.");
    }

    public async Task GetLatestProtectedQuote_WhenGetGuaranteedQuotesFails_ReturnsCalculationFailed()
    {
        var member = new MemberBuilder()
            .DateOfBirth(DateTime.UtcNow.AddYears(-40))
            .Build();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.Some(member));

        _calculationsParserMock
            .Setup(x => x.IsGuaranteedQuoteEnabled("TestBusinessGroup"))
            .Returns(true);

        _calculationsClientMock
            .Setup(x => x.GetGuaranteedQuotes(It.IsAny<GetGuaranteedQuoteClientRequest>()))
            .ReturnsAsync(Left<Error, GetGuaranteedQuoteResponse>(Error.New("Failed to get quotes")));

        var result = await _controller.GetLatestProtectedQuote();

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as LatestProtectedQuoteResponse;
        response.Should().NotBeNull();
        response.QuoteExpiryDate.Should().BeNull();
        response.QuoteRetirementDate.Should().BeNull();
        response.TotalPension.Should().BeNull();
        response.TotalAVCFund.Should().BeNull();
        response.AgeAtRetirementDateIso.Should().BeNull();
        response.IsCalculationSuccessful.Should().BeFalse();
    }

    public async Task GetLatestProtectedQuote_WhenNoQuotations_ReturnsCalculationFailed()
    {
        var member = new MemberBuilder()
            .DateOfBirth(DateTime.UtcNow.AddYears(-40))
            .Build();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.Some(member));

        _calculationsParserMock
            .Setup(x => x.IsGuaranteedQuoteEnabled("TestBusinessGroup"))
            .Returns(true);

        _calculationsClientMock
            .Setup(x => x.GetGuaranteedQuotes(It.IsAny<GetGuaranteedQuoteClientRequest>()))
            .ReturnsAsync(Right<Error, GetGuaranteedQuoteResponse>(new GetGuaranteedQuoteResponse { Quotations = new List<Quotation>() }));

        var result = await _controller.GetLatestProtectedQuote();

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as LatestProtectedQuoteResponse;
        response.Should().NotBeNull();
        response.QuoteExpiryDate.Should().BeNull();
        response.QuoteRetirementDate.Should().BeNull();
        response.TotalPension.Should().BeNull();
        response.TotalAVCFund.Should().BeNull();
        response.IsCalculationSuccessful.Should().BeFalse();
    }

    public async Task GetLatestProtectedQuote_WhenLatestQuoteHasNoEffectiveDate_ReturnsCalculationFailed()
    {
        var member = new MemberBuilder()
            .DateOfBirth(DateTime.UtcNow.AddYears(-40))
            .Build();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.Some(member));

        _calculationsParserMock
            .Setup(x => x.IsGuaranteedQuoteEnabled("TestBusinessGroup"))
            .Returns(true);

        var quotations = new List<Quotation>
        {
            new Quotation { RunDate = DateTime.UtcNow, EffectiveDate = null }
        };

        _calculationsClientMock
            .Setup(x => x.GetGuaranteedQuotes(It.IsAny<GetGuaranteedQuoteClientRequest>()))
            .ReturnsAsync(Right<Error, GetGuaranteedQuoteResponse>(new GetGuaranteedQuoteResponse { Quotations = quotations }));

        var result = await _controller.GetLatestProtectedQuote();

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as LatestProtectedQuoteResponse;
        response.Should().NotBeNull();
        response.QuoteExpiryDate.Should().BeNull();
        response.QuoteRetirementDate.Should().BeNull();
        response.TotalPension.Should().BeNull();
        response.TotalAVCFund.Should().BeNull();
        response.IsCalculationSuccessful.Should().BeFalse();
    }

    public async Task GetLatestProtectedQuote_WhenRetirementCalculationFails_ReturnsCalculationFailed()
    {
        var member = new MemberBuilder()
            .DateOfBirth(DateTime.UtcNow.AddYears(-40))
            .Build();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.Some(member));

        _calculationsParserMock
            .Setup(x => x.IsGuaranteedQuoteEnabled("TestBusinessGroup"))
            .Returns(true);

        var effectiveDate = DateTime.UtcNow;
        var quotations = new List<Quotation>
        {
            new Quotation { RunDate = DateTime.UtcNow, EffectiveDate = effectiveDate }
        };

        _calculationsClientMock
            .Setup(x => x.GetGuaranteedQuotes(It.IsAny<GetGuaranteedQuoteClientRequest>()))
            .ReturnsAsync(Right<Error, GetGuaranteedQuoteResponse>(new GetGuaranteedQuoteResponse { Quotations = quotations }));

        _calculationsClientMock
            .Setup(x => x.RetirementCalculationV2("TestReferenceNumber", "TestBusinessGroup", effectiveDate, false, false))
            .ReturnsAsync(Left<Error, (RetirementResponseV2, string)>(Error.New("Failed")));

        var result = await _controller.GetLatestProtectedQuote();

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as LatestProtectedQuoteResponse;
        response.Should().NotBeNull();
        response.QuoteExpiryDate.Should().BeNull();
        response.QuoteRetirementDate.Should().BeNull();
        response.TotalPension.Should().BeNull();
        response.TotalAVCFund.Should().BeNull();
        response.IsCalculationSuccessful.Should().BeFalse();
    }

    public async Task GetLatestProtectedQuote_WhenMemberNotFound_ReturnsNotFound()
    {
        _calculationsParserMock
            .Setup(x => x.IsGuaranteedQuoteEnabled("TestBusinessGroup"))
            .Returns(true);

        var effectiveDate = DateTime.UtcNow;
        var quotations = new List<Quotation>
        {
            new Quotation { RunDate = DateTime.UtcNow, EffectiveDate = effectiveDate }
        };

        _calculationsClientMock
            .Setup(x => x.GetGuaranteedQuotes(It.IsAny<GetGuaranteedQuoteClientRequest>()))
            .ReturnsAsync(Right<Error, GetGuaranteedQuoteResponse>(new GetGuaranteedQuoteResponse { Quotations = quotations }));

        _calculationsClientMock
            .Setup(x => x.RetirementCalculationV2("TestReferenceNumber", "TestBusinessGroup", effectiveDate, false, false))
            .ReturnsAsync(Right<Error, (RetirementResponseV2, string)>((new RetirementResponseV2(), "event")));

        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.None);

        var result = await _controller.GetLatestProtectedQuote();

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task GetLatestProtectedQuote_WhenSuccessful_ReturnsLatestProtectedQuote()
    {
        _calculationsParserMock
            .Setup(x => x.IsGuaranteedQuoteEnabled("TestBusinessGroup"))
            .Returns(true);

        var effectiveDate = DateTime.UtcNow;
        var expiryDate = DateTime.UtcNow.AddDays(30);
        var quotations = new List<Quotation>
        {
            new Quotation {
                RunDate = DateTime.UtcNow.AddDays(-1),
                EffectiveDate = effectiveDate.AddDays(-1),
                ExpiryDate = expiryDate.AddDays(-1)
            },
            new Quotation {
                RunDate = DateTime.UtcNow,
                EffectiveDate = effectiveDate,
                ExpiryDate = expiryDate
            }
        };

        _calculationsClientMock
            .Setup(x => x.GetGuaranteedQuotes(It.IsAny<GetGuaranteedQuoteClientRequest>()))
            .ReturnsAsync(Right<Error, GetGuaranteedQuoteResponse>(new GetGuaranteedQuoteResponse { Quotations = quotations }));

        var retirementResponse = new RetirementResponseV2();
        _calculationsClientMock
            .Setup(x => x.RetirementCalculationV2("TestReferenceNumber", "TestBusinessGroup", effectiveDate, false, false))
            .ReturnsAsync(Right<Error, (RetirementResponseV2, string)>((retirementResponse, "event")));

        var member = new MemberBuilder()
            .DateOfBirth(DateTime.UtcNow.AddYears(-40))
            .Build();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.Some(member));

        var calculation = GetCalculation();
        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(calculation);

        _calculationsParserMock
            .Setup(x => x.GetRetirementJsonV2(It.IsAny<RetirementResponseV2>(), It.IsAny<string>()))
            .Returns((TestData.RetirementJsonV2, TestData.RetirementQuotesJsonV2));

        _calculationsParserMock
            .Setup(x => x.GetGuaranteedQuoteDetail(It.IsAny<RetirementResponseV2>()))
            .Returns((true, expiryDate));

        var retirement = new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options()));
        _calculationsParserMock
            .Setup(x => x.GetRetirementV2(It.IsAny<string>()))
            .Returns(retirement);

        var result = await _controller.GetLatestProtectedQuote();

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as LatestProtectedQuoteResponse;
        response.Should().NotBeNull();
        response.QuoteExpiryDate.Should().Be(expiryDate);
        response.QuoteRetirementDate.Should().Be(effectiveDate);
        response.TotalPension.Should().Be(retirement.TotalPension());
        response.TotalAVCFund.Should().Be(retirement.TotalAVCFund());
        response.AgeAtRetirementDateIso.Should().NotBeNull();
        _mdpUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
    }

    public async Task GetLatestProtectedQuote_WhenNoCalculationExists_CreatesNewCalculationAndReturnsLatestProtectedQuote()
    {
        _calculationsParserMock
            .Setup(x => x.IsGuaranteedQuoteEnabled("TestBusinessGroup"))
            .Returns(true);

        var effectiveDate = DateTime.UtcNow;
        var expiryDate = DateTime.UtcNow.AddDays(30);
        var quotations = new List<Quotation>
        {
            new Quotation {
                RunDate = DateTime.UtcNow,
                EffectiveDate = effectiveDate,
                ExpiryDate = expiryDate
            }
        };

        _calculationsClientMock
            .Setup(x => x.GetGuaranteedQuotes(It.IsAny<GetGuaranteedQuoteClientRequest>()))
            .ReturnsAsync(Right<Error, GetGuaranteedQuoteResponse>(new GetGuaranteedQuoteResponse { Quotations = quotations }));

        var retirementResponse = new RetirementResponseV2();
        _calculationsClientMock
            .Setup(x => x.RetirementCalculationV2("TestReferenceNumber", "TestBusinessGroup", effectiveDate, false, false))
            .ReturnsAsync(Right<Error, (RetirementResponseV2, string)>((retirementResponse, "event")));

        var member = new MemberBuilder()
            .DateOfBirth(DateTime.UtcNow.AddYears(-40))
            .Build();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.Some(member));

        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<Calculation>.None);

        _calculationsClientMock
            .Setup(x => x.RetirementDatesAges(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(async () => JsonSerializer.Deserialize<RetirementDatesAgesResponse>(TestData.RetitementDateAgesResponseV2, SerialiationBuilder.Options()));

        _calculationsParserMock
            .Setup(x => x.GetRetirementDatesAgesJson(It.IsAny<RetirementDatesAgesResponse>()))
            .Returns(TestData.RetiremntDatesAgesJson);

        _calculationsParserMock
            .Setup(x => x.GetRetirementJsonV2(It.IsAny<RetirementResponseV2>(), It.IsAny<string>()))
            .Returns((TestData.RetirementJsonV2, TestData.RetirementQuotesJsonV2));

        var retirement = new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options()));
        _calculationsParserMock
            .Setup(x => x.GetRetirementV2(It.IsAny<string>()))
            .Returns(retirement);

        var result = await _controller.GetLatestProtectedQuote();

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as LatestProtectedQuoteResponse;
        response.Should().NotBeNull();
        response.QuoteExpiryDate.Should().Be(expiryDate);
        response.QuoteRetirementDate.Should().Be(effectiveDate);
        response.TotalPension.Should().Be(retirement.TotalPension());
        response.TotalAVCFund.Should().Be(retirement.TotalAVCFund());
        response.AgeAtRetirementDateIso.Should().NotBeNull();

        _calculationsRepositoryMock.Verify(x => x.Create(It.IsAny<Calculation>()), Times.Once);
        _mdpUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
    }
}