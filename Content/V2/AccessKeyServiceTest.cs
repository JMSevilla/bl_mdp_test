using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Content.V2;
using WTW.MdpService.Domain.Mdp.Calculations;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.MdpService.Infrastructure.Content;
using WTW.MdpService.Infrastructure.Investment;
using WTW.MdpService.Infrastructure.MdpDb;
using WTW.MdpService.Test.Domain.Mdp.Calculations;
using WTW.MdpService.Test.Domain.Members;
using WTW.TestCommon.FixieConfig;
using WTW.TestCommon.Helpers;
using WTW.Web;
using WTW.Web.Caching;
using WTW.Web.Serialization;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace WTW.MdpService.Test.Content.V2;

public class AccessKeyServiceTest
{
    private readonly Mock<ICalculationsClient> _calculationsClientMock;
    private readonly Mock<IRetirementAccessKeyDataService> _retirementAccessKeyDataServiceMock;
    private readonly Mock<ITransferCalculationRepository> _transferCalculationRepositoryMock;
    private readonly Mock<IAccessKeyWordingFlagsService> _accessKeyWordingFlagsServiceMock;
    private readonly Mock<ICalculationsParser> _calculationsParserMock;
    private readonly Mock<ICache> _cacheMock;
    private readonly Mock<ILogger<AccessKeyService>> _logger;
    private readonly Member _member;
    private readonly AccessKeyService _sut;
    private readonly Mock<IWebChatFlagService> _webChatFlagServiceMock;
    private readonly Mock<IInvestmentServiceClient> _investmentServiceClientMock;
    public AccessKeyServiceTest()
    {
        _calculationsClientMock = new Mock<ICalculationsClient>();
        _retirementAccessKeyDataServiceMock = new Mock<IRetirementAccessKeyDataService>();
        _transferCalculationRepositoryMock = new Mock<ITransferCalculationRepository>();
        _accessKeyWordingFlagsServiceMock = new Mock<IAccessKeyWordingFlagsService>();
        _calculationsParserMock = new Mock<ICalculationsParser>();
        _cacheMock = new Mock<ICache>();
        _logger = new Mock<ILogger<AccessKeyService>>();
        _webChatFlagServiceMock = new Mock<IWebChatFlagService>();
        _investmentServiceClientMock = new Mock<IInvestmentServiceClient>();
        _member = new MemberBuilder().DateOfBirth(DateTimeOffset.UtcNow.AddYears(-38).AddMonths(-1).AddDays(-1)).Build();

        _sut = new AccessKeyService(
            _calculationsClientMock.Object,
            _retirementAccessKeyDataServiceMock.Object,
            _transferCalculationRepositoryMock.Object,
            _accessKeyWordingFlagsServiceMock.Object,
            _calculationsParserMock.Object,
            _cacheMock.Object,
            _logger.Object,
            _webChatFlagServiceMock.Object,
            _investmentServiceClientMock.Object);

        _retirementAccessKeyDataServiceMock
           .Setup(x => x.GetRetirementApplicationStatus(It.IsAny<Member>(), It.IsAny<Either<Error, Calculation>>(), It.IsAny<int>(), It.IsAny<int>()))
           .Returns(RetirementApplicationStatus.RetirementCase);

        SetUpWordingFlagsMock();
    }

    public async Task CalculatesAccessKey_WhenCalcApiDatesAgesEndpointFails()
    {
        var result = await _sut.CalculateKey(_member, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ContentClassifierValue>>(), It.IsAny<bool>(), It.IsAny<bool>());

        result.Should().Be("{\"tenantUrl\":null,\"isCalculationSuccessful\":false,\"hasAdditionalContributions\":false," +
            "\"schemeType\":\"DB\",\"memberStatus\":\"Active\",\"lifeStage\":\"Undefined\",\"retirementApplicationStatus\":\"RetirementCase\"," +
            "\"transferApplicationStatus\":\"Undefined\"," +
            "\"wordingFlags\":[\"RetirementFlag\",\"SchemeFlag\",\"CategoryFlag\",\"IfaReferralFlag\",\"HbsFlag\",\"LinkedMemberFlag\",\"PayTimelineWordingFlag\"," +
            "\"CalcApiDatesAgesFlag\",\"TransferWordingFlag\",\"GenericJourneysFlag\",\"QuoteSelectionFlag\"],\"currentAge\":\"38Y1M\"," +
            "\"dbCalculationStatus\":\"calcNotAccessible\",\"dcLifeStage\":\"undefined\",\"isWebChatEnabled\":false,\"hasProtectedQuote\":false,\"numberOfProtectedQuotes\":0}");
    }

    public async Task CalculatesAccessKey_WhenGuaranteedQuoteExist()
    {
        SetUpRetirementDateAgesResponse();

        _retirementAccessKeyDataServiceMock
          .Setup(x => x.GetExistingRetirementJourneyType(_member))
          .ReturnsAsync(ExistingRetirementJourneyType.DbRetirementApplication);

        _retirementAccessKeyDataServiceMock
           .Setup(x => x.GetRetirementCalculationWithJourney(It.IsAny<RetirementDatesAgesResponse>(), It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(new CalculationBuilder().SetCalculationSuccessStatus(true).BuildV2());

        _calculationsParserMock.Setup(x => x.GetRetirementDatesAges(It.IsAny<string>())).Returns(GetRetirementDatesAges());
        _calculationsParserMock
            .Setup(x => x.GetRetirementV2(It.IsAny<string>()))
            .Returns(new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options())));

        Either<Error, GetGuaranteedQuoteResponse> _stubGetGuaranteedQuoteResponse = new GetGuaranteedQuoteResponse
        {
            Pagination = new Pagination(),
            Quotations = new List<Quotation> { new Quotation {
                    Event = "PV", Status = "GUARANTEED", EffectiveDate = DateTime.UtcNow}, new Quotation {  Event = "PO", Status = "GUARANTEED", EffectiveDate = DateTime.UtcNow.AddDays(-1)} }
        };

        _calculationsParserMock
            .Setup(x => x.IsGuaranteedQuoteEnabled(It.IsAny<string>())).Returns(true);

        _calculationsClientMock.Setup(x => x.GetGuaranteedQuotes(It.IsAny<GetGuaranteedQuoteClientRequest>())).ReturnsAsync(_stubGetGuaranteedQuoteResponse);

        var result = await _sut.CalculateKey(_member, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ContentClassifierValue>>(), It.IsAny<bool>(), It.IsAny<bool>());

        result.Should().Be("{\"tenantUrl\":null,\"isCalculationSuccessful\":true,\"hasAdditionalContributions\":false,\"schemeType\":\"DB\",\"memberStatus\":\"Active\",\"lifeStage\":\"NotEligibleToRetire\",\"retirementApplicationStatus\":\"RetirementCase\",\"transferApplicationStatus\":\"Undefined\",\"wordingFlags\":[\"RetirementFlag\",\"SchemeFlag\",\"CategoryFlag\",\"IfaReferralFlag\",\"HbsFlag\",\"LinkedMemberFlag\",\"PayTimelineWordingFlag\",\"CalcApiDatesAgesFlag\",\"TransferWordingFlag\",\"GenericJourneysFlag\",\"QuoteSelectionFlag\",\"SelectedDateProtected\"],\"currentAge\":\"38Y1M\",\"dbCalculationStatus\":null,\"dcLifeStage\":\"farFromTRD\",\"isWebChatEnabled\":false,\"hasProtectedQuote\":true,\"numberOfProtectedQuotes\":2}");
    }

    public async Task CalculatesAccessKey_WhenGuaranteedQuoteNotExist()
    {

        Either<Error, GetGuaranteedQuoteResponse> _stubGetGuaranteedQuoteResponse = new GetGuaranteedQuoteResponse();

        _calculationsClientMock.Setup(x => x.GetGuaranteedQuotes(It.IsAny<GetGuaranteedQuoteClientRequest>())).ReturnsAsync(_stubGetGuaranteedQuoteResponse);

        var result = await _sut.CalculateKey(_member, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ContentClassifierValue>>(), It.IsAny<bool>(), It.IsAny<bool>());

        result.Should().Be("{\"tenantUrl\":null,\"isCalculationSuccessful\":false,\"hasAdditionalContributions\":false," +
            "\"schemeType\":\"DB\",\"memberStatus\":\"Active\",\"lifeStage\":\"Undefined\",\"retirementApplicationStatus\":\"RetirementCase\"," +
            "\"transferApplicationStatus\":\"Undefined\"," +
            "\"wordingFlags\":[\"RetirementFlag\",\"SchemeFlag\",\"CategoryFlag\",\"IfaReferralFlag\",\"HbsFlag\",\"LinkedMemberFlag\",\"PayTimelineWordingFlag\"," +
            "\"CalcApiDatesAgesFlag\",\"TransferWordingFlag\",\"GenericJourneysFlag\",\"QuoteSelectionFlag\"],\"currentAge\":\"38Y1M\"," +
            "\"dbCalculationStatus\":\"calcNotAccessible\",\"dcLifeStage\":\"undefined\",\"isWebChatEnabled\":false,\"hasProtectedQuote\":false,\"numberOfProtectedQuotes\":0}");
    }

    public async Task CalculatesAccessKey_WhenRetirementCalculationEndpointFails()
    {
        SetUpRetirementDateAgesResponse();
        _retirementAccessKeyDataServiceMock
           .Setup(x => x.GetNewRetirementCalculation(It.IsAny<RetirementDatesAgesResponse>(), _member))
           .ReturnsAsync(Error.New("test"));
        var result = await _sut.CalculateKey(_member, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ContentClassifierValue>>(), It.IsAny<bool>(), It.IsAny<bool>());

        result.Should().Be("{\"tenantUrl\":null,\"isCalculationSuccessful\":false,\"hasAdditionalContributions\":false," +
            "\"schemeType\":\"DB\",\"memberStatus\":\"Active\",\"lifeStage\":\"NotEligibleToRetire\",\"retirementApplicationStatus\":\"RetirementCase\"," +
            "\"transferApplicationStatus\":\"Undefined\",\"wordingFlags\":[\"RetirementFlag\",\"SchemeFlag\",\"CategoryFlag\",\"IfaReferralFlag\",\"HbsFlag\"," +
            "\"LinkedMemberFlag\",\"PayTimelineWordingFlag\",\"CalcApiDatesAgesFlag\",\"TransferWordingFlag\",\"GenericJourneysFlag\"," +
            "\"QuoteSelectionFlag\"],\"currentAge\":\"38Y1M\"," +
            "\"dbCalculationStatus\":\"calcNotAccessible\",\"dcLifeStage\":\"undefined\",\"isWebChatEnabled\":false,\"hasProtectedQuote\":false,\"numberOfProtectedQuotes\":0}");
    }

    [Input(true, "true")]
    [Input(false, "false")]
    [Input(null, "null")]
    public async Task CalculatesAccessKey_WhenRetirementJourneyIsStarted(bool? isCalculationSuccessful, string expectedResultIsCalculationSuccessful)
    {
        SetUpRetirementDateAgesResponse();

        _retirementAccessKeyDataServiceMock
          .Setup(x => x.GetExistingRetirementJourneyType(_member))
          .ReturnsAsync(ExistingRetirementJourneyType.DbRetirementApplication);

        _retirementAccessKeyDataServiceMock
           .Setup(x => x.GetRetirementCalculationWithJourney(It.IsAny<RetirementDatesAgesResponse>(), It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(new CalculationBuilder().SetCalculationSuccessStatus(isCalculationSuccessful).BuildV2());

        _calculationsParserMock.Setup(x => x.GetRetirementDatesAges(It.IsAny<string>())).Returns(GetRetirementDatesAges());
        _calculationsParserMock
            .Setup(x => x.GetRetirementV2(It.IsAny<string>()))
            .Returns(new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options())));

        var result = await _sut.CalculateKey(_member, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ContentClassifierValue>>(), It.IsAny<bool>(), It.IsAny<bool>());

        result.Should().Be("{\"tenantUrl\":null,\"isCalculationSuccessful\":" + $"{expectedResultIsCalculationSuccessful}" + ",\"hasAdditionalContributions\":false," +
            "\"schemeType\":\"DB\",\"memberStatus\":\"Active\",\"lifeStage\":\"NotEligibleToRetire\",\"retirementApplicationStatus\":\"RetirementCase\"," +
            "\"transferApplicationStatus\":\"Undefined\",\"wordingFlags\":[\"RetirementFlag\",\"SchemeFlag\",\"CategoryFlag\",\"IfaReferralFlag\",\"HbsFlag\",\"LinkedMemberFlag\"," +
            "\"PayTimelineWordingFlag\",\"CalcApiDatesAgesFlag\",\"TransferWordingFlag\",\"GenericJourneysFlag\",\"QuoteSelectionFlag\"],\"currentAge\":\"38Y1M\"," +
            "\"dbCalculationStatus\":null,\"dcLifeStage\":\"farFromTRD\",\"isWebChatEnabled\":false,\"hasProtectedQuote\":false,\"numberOfProtectedQuotes\":0}");
    }

    [Input(true, "true")]
    [Input(false, "false")]
    [Input(null, "null")]
    public async Task CalculatesAccessKey_WhenDCRetirementJourneyIsStarted(bool? isCalculationSuccessful, string expectedResultIsCalculationSuccessful)
    {
        SetUpRetirementDateAgesResponse();

        _retirementAccessKeyDataServiceMock
          .Setup(x => x.GetExistingRetirementJourneyType(_member))
          .ReturnsAsync(ExistingRetirementJourneyType.DcRetirementApplication);

        _retirementAccessKeyDataServiceMock
           .Setup(x => x.GetRetirementCalculation(It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(new CalculationBuilder().SetCalculationSuccessStatus(isCalculationSuccessful).BuildV2());

        _calculationsParserMock.Setup(x => x.GetRetirementDatesAges(It.IsAny<string>())).Returns(GetRetirementDatesAges());
        _calculationsParserMock
            .Setup(x => x.GetRetirementV2(It.IsAny<string>()))
            .Returns(new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options())));

        var result = await _sut.CalculateKey(_member, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ContentClassifierValue>>(), It.IsAny<bool>(), It.IsAny<bool>());

        result.Should().Be("{\"tenantUrl\":null,\"isCalculationSuccessful\":" + $"{expectedResultIsCalculationSuccessful}" + ",\"hasAdditionalContributions\":false," +
            "\"schemeType\":\"DB\",\"memberStatus\":\"Active\",\"lifeStage\":\"NotEligibleToRetire\",\"retirementApplicationStatus\":\"RetirementCase\"," +
            "\"transferApplicationStatus\":\"Undefined\",\"wordingFlags\":[\"RetirementFlag\",\"SchemeFlag\",\"CategoryFlag\",\"IfaReferralFlag\",\"HbsFlag\",\"LinkedMemberFlag\"," +
            "\"PayTimelineWordingFlag\",\"CalcApiDatesAgesFlag\",\"TransferWordingFlag\",\"GenericJourneysFlag\",\"QuoteSelectionFlag\"],\"currentAge\":\"38Y1M\"," +
            "\"dbCalculationStatus\":null,\"dcLifeStage\":\"farFromTRD\",\"isWebChatEnabled\":false,\"hasProtectedQuote\":false,\"numberOfProtectedQuotes\":0}");
    }

    public async Task RecalculatesAccessKey_WhenCalculationExists()
    {
        _retirementAccessKeyDataServiceMock
          .Setup(x => x.GetRetirementCalculation(It.IsAny<string>(), It.IsAny<string>()))
          .ReturnsAsync(new CalculationBuilder().BuildV2());

        _calculationsParserMock.Setup(x => x.GetRetirementDatesAges(It.IsAny<string>())).Returns(GetRetirementDatesAges());
        _calculationsParserMock
            .Setup(x => x.GetRetirementV2(It.IsAny<string>()))
            .Returns(new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options())));

        var result = await _sut.RecalculateKey(_member, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ContentClassifierValue>>(), It.IsAny<bool>(), It.IsAny<bool>());

        result.Should().Be("{\"tenantUrl\":null,\"isCalculationSuccessful\":true,\"hasAdditionalContributions\":false," +
            "\"schemeType\":\"DB\",\"memberStatus\":\"Active\",\"lifeStage\":\"NotEligibleToRetire\",\"retirementApplicationStatus\":\"RetirementCase\"," +
            "\"transferApplicationStatus\":\"Undefined\",\"wordingFlags\":[\"RetirementFlag\",\"SchemeFlag\",\"CategoryFlag\",\"IfaReferralFlag\",\"HbsFlag\"," +
            "\"LinkedMemberFlag\",\"PayTimelineWordingFlag\",\"CalcApiDatesAgesFlag\",\"TransferWordingFlag\",\"GenericJourneysFlag\",\"QuoteSelectionFlag\"]," +
            "\"currentAge\":\"38Y1M\",\"dbCalculationStatus\":null,\"dcLifeStage\":\"farFromTRD\",\"isWebChatEnabled\":false,\"hasProtectedQuote\":false,\"numberOfProtectedQuotes\":0}");
    }

    public async Task RecalculatesAccessKey_WhenCalculationExistsAndSchemeIsDcAndCalcApiFails()
    {
        _retirementAccessKeyDataServiceMock
          .Setup(x => x.GetRetirementCalculation(It.IsAny<string>(), It.IsAny<string>()))
          .ReturnsAsync(new CalculationBuilder().BuildV2());

        _calculationsParserMock.Setup(x => x.GetRetirementDatesAges(It.IsAny<string>())).Returns(GetRetirementDatesAges());
        _calculationsParserMock
            .Setup(x => x.GetRetirementV2(It.IsAny<string>()))
            .Returns(new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options())));

        var result = await _sut.RecalculateKey(new MemberBuilder()
            .DateOfBirth(DateTimeOffset.UtcNow.AddYears(-38).AddMonths(-1).AddDays(-1))
            .SchemeType("DC").Build(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ContentClassifierValue>>(), It.IsAny<bool>(), It.IsAny<bool>());

        result.Should().Be("{\"tenantUrl\":null,\"isCalculationSuccessful\":true,\"hasAdditionalContributions\":false," +
            "\"schemeType\":\"DC\",\"memberStatus\":\"Active\",\"lifeStage\":\"Undefined\",\"retirementApplicationStatus\":\"RetirementCase\"," +
            "\"transferApplicationStatus\":\"Undefined\",\"wordingFlags\":[\"RetirementFlag\",\"SchemeFlag\",\"CategoryFlag\",\"IfaReferralFlag\",\"HbsFlag\"," +
            "\"LinkedMemberFlag\",\"PayTimelineWordingFlag\",\"CalcApiDatesAgesFlag\",\"TransferWordingFlag\",\"GenericJourneysFlag\",\"QuoteSelectionFlag\"]," +
            "\"currentAge\":\"38Y1M\",\"dbCalculationStatus\":null,\"dcLifeStage\":\"farFromTRD\",\"isWebChatEnabled\":false,\"hasProtectedQuote\":false,\"numberOfProtectedQuotes\":0}");
        _cacheMock.Verify(x => x.Remove(It.IsAny<string>()), Times.Once);
        _retirementAccessKeyDataServiceMock.Verify(x => x.UpdateRetirementDatesAges(It.IsAny<Calculation>(), It.IsAny<RetirementDatesAgesResponse>()), Times.Never);
    }

    public async Task RecalculatesAccessKey_WhenCalculationExistsAndSchemeIsDcAndCalcApiSucceeds()
    {
        SetUpRetirementDateAgesResponse();
        _retirementAccessKeyDataServiceMock
          .Setup(x => x.GetRetirementCalculation(It.IsAny<string>(), It.IsAny<string>()))
          .ReturnsAsync(new CalculationBuilder().BuildV2());

        _calculationsParserMock.Setup(x => x.GetRetirementDatesAges(It.IsAny<string>())).Returns(GetRetirementDatesAges());
        _calculationsParserMock
            .Setup(x => x.GetRetirementV2(It.IsAny<string>()))
            .Returns(new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options())));

        var result = await _sut.RecalculateKey(new MemberBuilder()
            .DateOfBirth(DateTimeOffset.UtcNow.AddYears(-38).AddMonths(-1).AddDays(-1))
            .SchemeType("DC").Build(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ContentClassifierValue>>(), It.IsAny<bool>(), It.IsAny<bool>());

        result.Should().Be("{\"tenantUrl\":null,\"isCalculationSuccessful\":true,\"hasAdditionalContributions\":false," +
            "\"schemeType\":\"DC\",\"memberStatus\":\"Active\",\"lifeStage\":\"NotEligibleToRetire\",\"retirementApplicationStatus\":\"RetirementCase\"," +
            "\"transferApplicationStatus\":\"Undefined\",\"wordingFlags\":[\"RetirementFlag\",\"SchemeFlag\",\"CategoryFlag\",\"IfaReferralFlag\",\"HbsFlag\"," +
            "\"LinkedMemberFlag\",\"PayTimelineWordingFlag\",\"CalcApiDatesAgesFlag\",\"TransferWordingFlag\",\"GenericJourneysFlag\",\"QuoteSelectionFlag\"]," +
            "\"currentAge\":\"38Y1M\",\"dbCalculationStatus\":null,\"dcLifeStage\":\"farFromTRD\",\"isWebChatEnabled\":false,\"hasProtectedQuote\":false,\"numberOfProtectedQuotes\":0}");
        _cacheMock.Verify(x => x.Remove(It.IsAny<string>()), Times.Once);
        _retirementAccessKeyDataServiceMock.Verify(x => x.UpdateRetirementDatesAges(It.IsAny<Calculation>(), It.IsAny<RetirementDatesAgesResponse>()), Times.Once);
    }

    public async Task RecalculatesAccessKey_WhenCalculationDoesExistsAndDatesAgesEndpointFails()
    {
        _calculationsParserMock.Setup(x => x.GetRetirementDatesAges(It.IsAny<string>())).Returns(GetRetirementDatesAges());
        _calculationsParserMock
            .Setup(x => x.GetRetirementV2(It.IsAny<string>()))
            .Returns(new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options())));

        var result = await _sut.RecalculateKey(_member, It.IsAny<string>(), "natwest.wtwco.com", It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ContentClassifierValue>>(), It.IsAny<bool>(), It.IsAny<bool>());

        result.Should().Be("{\"tenantUrl\":\"natwest.wtwco.com\",\"isCalculationSuccessful\":false,\"hasAdditionalContributions\":false," +
            "\"schemeType\":\"DB\",\"memberStatus\":\"Active\",\"lifeStage\":\"Undefined\",\"retirementApplicationStatus\":\"RetirementCase\"," +
            "\"transferApplicationStatus\":\"Undefined\",\"wordingFlags\":[\"RetirementFlag\",\"SchemeFlag\",\"CategoryFlag\",\"IfaReferralFlag\",\"HbsFlag\"," +
            "\"LinkedMemberFlag\",\"PayTimelineWordingFlag\",\"CalcApiDatesAgesFlag\",\"TransferWordingFlag\",\"GenericJourneysFlag\"," +
            "\"QuoteSelectionFlag\"],\"currentAge\":\"38Y1M\",\"dbCalculationStatus\":\"calcNotAccessible\",\"dcLifeStage\":\"undefined\",\"isWebChatEnabled\":false,\"hasProtectedQuote\":false,\"numberOfProtectedQuotes\":0}");
    }

    public async Task RecalculatesAccessKey_WhenCalculationDoesExistsAndDatesAgesEndpointSucceeds()
    {
        SetUpRetirementDateAgesResponse();
        _retirementAccessKeyDataServiceMock
         .Setup(x => x.GetNewRetirementCalculation(It.IsAny<RetirementDatesAgesResponse>(), _member))
         .ReturnsAsync(Error.New("test"));
        _calculationsParserMock.Setup(x => x.GetRetirementDatesAges(It.IsAny<string>())).Returns(GetRetirementDatesAges());
        _calculationsParserMock
            .Setup(x => x.GetRetirementV2(It.IsAny<string>()))
            .Returns(new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options())));

        var result = await _sut.RecalculateKey(_member, It.IsAny<string>(), "natwest.wtwco.com", It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ContentClassifierValue>>(), It.IsAny<bool>(), It.IsAny<bool>());

        result.Should().Be("{\"tenantUrl\":\"natwest.wtwco.com\",\"isCalculationSuccessful\":false,\"hasAdditionalContributions\":false," +
            "\"schemeType\":\"DB\",\"memberStatus\":\"Active\",\"lifeStage\":\"NotEligibleToRetire\",\"retirementApplicationStatus\":\"RetirementCase\"," +
            "\"transferApplicationStatus\":\"Undefined\",\"wordingFlags\":[\"RetirementFlag\",\"SchemeFlag\",\"CategoryFlag\",\"IfaReferralFlag\",\"HbsFlag\"," +
            "\"LinkedMemberFlag\",\"PayTimelineWordingFlag\",\"CalcApiDatesAgesFlag\",\"TransferWordingFlag\",\"GenericJourneysFlag\"," +
            "\"QuoteSelectionFlag\"],\"currentAge\":\"38Y1M\",\"dbCalculationStatus\":\"calcNotAccessible\",\"dcLifeStage\":\"undefined\",\"isWebChatEnabled\":false,\"hasProtectedQuote\":false,\"numberOfProtectedQuotes\":0}");
    }

    [Input(223675)]
    public async Task CalculatesAccessKey_SetHasDCAssetsWhenInternalBalanceApiSuccess(int totalValue)
    {
        _investmentServiceClientMock.Setup(x => x.GetInternalBalance(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new InvestmentInternalBalanceResponse() { TotalValue = totalValue });
        var result = await _sut.CalculateKey(_member, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ContentClassifierValue>>(), It.IsAny<bool>(), It.IsAny<bool>());

        result.Should().Be("{\"tenantUrl\":null,\"isCalculationSuccessful\":false,\"hasAdditionalContributions\":false," +
            "\"schemeType\":\"DB\",\"memberStatus\":\"Active\",\"lifeStage\":\"Undefined\",\"retirementApplicationStatus\":\"RetirementCase\"," +
            "\"transferApplicationStatus\":\"Undefined\"," +
            "\"wordingFlags\":[\"RetirementFlag\",\"SchemeFlag\",\"CategoryFlag\",\"IfaReferralFlag\",\"HbsFlag\",\"LinkedMemberFlag\",\"PayTimelineWordingFlag\"," +
            "\"CalcApiDatesAgesFlag\",\"TransferWordingFlag\",\"GenericJourneysFlag\",\"QuoteSelectionFlag\",\"HasDCAssets\"],\"currentAge\":\"38Y1M\"," +
            "\"dbCalculationStatus\":\"calcNotAccessible\",\"dcLifeStage\":\"undefined\",\"isWebChatEnabled\":false,\"hasProtectedQuote\":false,\"numberOfProtectedQuotes\":0}");
    }


    [Input(0)]
    [Input(-10)]
    public async Task CalculatesAccessKey_SetHasDCAssetsToFalseWhenInternalBalanceApiFailure(int totalValue)
    {
        _investmentServiceClientMock.Setup(x => x.GetInternalBalance(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new InvestmentInternalBalanceResponse() { TotalValue = totalValue });

        var result = await _sut.CalculateKey(_member, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ContentClassifierValue>>(), It.IsAny<bool>(), It.IsAny<bool>());

        result.Should().Be("{\"tenantUrl\":null,\"isCalculationSuccessful\":false,\"hasAdditionalContributions\":false,\"schemeType\":\"DB\",\"memberStatus\":\"Active\",\"lifeStage\":\"Undefined\",\"retirementApplicationStatus\":\"RetirementCase\",\"transferApplicationStatus\":\"Undefined\",\"wordingFlags\":[\"RetirementFlag\",\"SchemeFlag\",\"CategoryFlag\",\"IfaReferralFlag\",\"HbsFlag\",\"LinkedMemberFlag\",\"PayTimelineWordingFlag\",\"CalcApiDatesAgesFlag\",\"TransferWordingFlag\",\"GenericJourneysFlag\",\"QuoteSelectionFlag\"],\"currentAge\":\"38Y1M\",\"dbCalculationStatus\":\"calcNotAccessible\",\"dcLifeStage\":\"undefined\",\"isWebChatEnabled\":false,\"hasProtectedQuote\":false,\"numberOfProtectedQuotes\":0}");
    }

    public async Task CalculatesAccessKeyIsCalledForDcMember_DoNotSetHasDCAssets()
    {
        var dcMember = new MemberBuilder().DateOfBirth(DateTimeOffset.UtcNow.AddYears(-38).AddMonths(-1).AddDays(-1)).SchemeType("DC").Build();

        var result = await _sut.CalculateKey(dcMember, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ContentClassifierValue>>(), It.IsAny<bool>(), It.IsAny<bool>());

        result.Should().Be("{\"tenantUrl\":null,\"isCalculationSuccessful\":false,\"hasAdditionalContributions\":false," +
            "\"schemeType\":\"DC\",\"memberStatus\":\"Active\",\"lifeStage\":\"Undefined\",\"retirementApplicationStatus\":\"RetirementCase\"," +
            "\"transferApplicationStatus\":\"Undefined\"," +
            "\"wordingFlags\":[\"RetirementFlag\",\"SchemeFlag\",\"CategoryFlag\",\"IfaReferralFlag\",\"HbsFlag\",\"LinkedMemberFlag\",\"PayTimelineWordingFlag\"," +
            "\"CalcApiDatesAgesFlag\",\"TransferWordingFlag\",\"GenericJourneysFlag\",\"QuoteSelectionFlag\"],\"currentAge\":\"38Y1M\"," +
            "\"dbCalculationStatus\":\"calcNotAccessible\",\"dcLifeStage\":\"undefined\",\"isWebChatEnabled\":false,\"hasProtectedQuote\":false,\"numberOfProtectedQuotes\":0}");
    }

    public async Task WhenParseJsonToAccessKeyCalledWithValidInput_ThenExpectedAccessKeyReturned()
    {
        var accessKeyJsonString = "{\"tenantUrl\": \"tenant.com\", \"isCalculationSuccessful\": true, \"hasAdditionalContributions\": false, \"schemeType\": \"DB\", \"memberStatus\": \"Deferred\", \"lifeStage\": \"EligibleToRetire\", \"retirementApplicationStatus\": \"RetirementDateOutOfRange\", \"transferApplicationStatus\": \"UnavailableTA\", \"wordingFlags\": [\"SPD\"],\"currentAge\": \"60Y7M\", \"dbCalculationStatus\": null, \"dcLifeStage\": \"farFromTRD\", \"isWebChatEnabled\":false,\"hasProtectedQuote\":false,\"numberOfProtectedQuotes\":0}";

        var expectedResult = JsonSerializer.Deserialize<AccessKey>(accessKeyJsonString, SerialiationBuilder.Options());

        var actualResult = _sut.ParseJsonToAccessKey(accessKeyJsonString);

        actualResult.Should().NotBeNull();
        actualResult.Should().BeOfType<AccessKey>();
        actualResult.Should().BeEquivalentTo(expectedResult);
    }

    public async Task WhenParseJsonToAccessKeyCalledWithInValidInput_ThenExpectedAccessKeyReturned()
    {
        var actualResult = _sut.ParseJsonToAccessKey("");

        actualResult.Should().BeNull();
    }

    public async Task CalculateAccessKeyReturnsBasicAccessKeyWhenBasicModeIsPassedAndExpectedLoggingOccurs()
    {
        var result = await _sut.CalculateKey(_member, It.IsAny<string>(), "natwest.wtwco.com", It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ContentClassifierValue>>(), useBasicMode: true, It.IsAny<bool>());

        _logger.VerifyLogging("Bulding Basic access key using BuildAccessKeyBasic", LogLevel.Information);
    }

    public async Task RecalculateAccessKeyReturnsBasicAccessKeyWhenBasicModeIsPassedAndExpectedLoggingOccurs()
    {
        var result = await _sut.RecalculateKey(_member, It.IsAny<string>(), "natwest.wtwco.com", It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ContentClassifierValue>>(), useBasicMode: true, It.IsAny<bool>());

        _logger.VerifyLogging("Bulding Basic access key using BuildAccessKeyBasic", LogLevel.Information);
    }

    public async Task CalculateAccessKeyReturnsAccessKeyWhenBasicModeIsPassedWithMemberHavingLinkedMemberAndExpectedLoggingOccurs()
    {
        _accessKeyWordingFlagsServiceMock.Setup(x => x.GetWordingsForWebRules(It.IsAny<Member>(), It.IsAny<string>(), It.IsAny<List<ContentClassifierValue>>())).ReturnsAsync(new List<string>() { "WebRuleWordingsFlag" });
        var member = new MemberBuilder().LinkedMembers(new LinkedMember("1111124", "RBS", "1234567", "BCL")).Build();

        var result = await _sut.CalculateKey(member, It.IsAny<string>(), "natwest.wtwco.com", It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ContentClassifierValue>>(), useBasicMode: true, It.IsAny<bool>());

        result.Should().Contain("LinkedMemberFlag");
        result.Should().Contain("WebRuleWordingsFlag");
        _logger.VerifyLogging("Bulding Basic access key using BuildAccessKeyBasic", LogLevel.Information);
    }

    public async Task CalculateAccessKeyReturnsAccessKeyWithSchemeCodeAndCategoryFlagsWhenBasicModeIsPassedAndExpectedLoggingOccurs()
    {
        _accessKeyWordingFlagsServiceMock.Setup(x => x.GetWordingsForWebRules(It.IsAny<Member>(), It.IsAny<string>(), It.IsAny<List<ContentClassifierValue>>())).ReturnsAsync(new List<string>() { "WebRuleWordingsFlag" });

        var result = await _sut.CalculateKey(_member, It.IsAny<string>(), "natwest.wtwco.com", It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<ContentClassifierValue>>(), useBasicMode: true, It.IsAny<bool>());

        result.Should().Contain("WebRuleWordingsFlag");
        result.Should().Contain("SchemeFlag");
        result.Should().Contain("CategoryFlag");
        _logger.VerifyLogging("Bulding Basic access key using BuildAccessKeyBasic", LogLevel.Information);
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

    private void SetUpRetirementDateAgesResponse()
    {
        var datesResponse = JsonSerializer.Deserialize<RetirementDatesAgesResponse>(TestData.RetitementDateAgesResponse, SerialiationBuilder.Options());
        _calculationsClientMock
            .Setup(x => x.RetirementDatesAges(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(async () => datesResponse);
    }

    private void SetUpWordingFlagsMock()
    {
        _accessKeyWordingFlagsServiceMock
            .Setup(x => x.GetCalcApiDatesAgesEndpointWordingFlags(It.IsAny<Option<RetirementDatesAges>>()))
            .Returns(new List<string> { "CalcApiDatesAgesFlag" });

        _accessKeyWordingFlagsServiceMock
            .Setup(x => x.GetHbsFlags(It.IsAny<Member>(), It.IsAny<Option<RetirementDatesAges>>()))
            .Returns(new List<string> { "HbsFlag" });

        _accessKeyWordingFlagsServiceMock
            .Setup(x => x.GetIfaReferralFlags(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<string> { "IfaReferralFlag" });

        _accessKeyWordingFlagsServiceMock
            .Setup(x => x.GetLinkedMemberFlags(It.IsAny<Member>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<string> { "LinkedMemberFlag" });

        _accessKeyWordingFlagsServiceMock
            .Setup(x => x.GetLinkedMemberFlags(It.IsAny<Member>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<string> { "LinkedMemberFlag" });

        _accessKeyWordingFlagsServiceMock
            .Setup(x => x.GetPayTimelineWordingFlags(It.IsAny<Member>()))
            .ReturnsAsync(new List<string> { "PayTimelineWordingFlag" });

        _accessKeyWordingFlagsServiceMock
            .Setup(x => x.GetRetirementFlags(It.IsAny<Either<Error, Calculation>>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<string> { "RetirementFlag" });

        _accessKeyWordingFlagsServiceMock
            .Setup(x => x.GetSchemeFlags(It.IsAny<Member>()))
            .Returns(new List<string> { "SchemeFlag" });

        _accessKeyWordingFlagsServiceMock
            .Setup(x => x.GetCategoryFlags(It.IsAny<Member>()))
            .Returns(new List<string> { "CategoryFlag" });

        _accessKeyWordingFlagsServiceMock
            .Setup(x => x.GetTransferWordingFlags(It.IsAny<Option<TransferCalculation>>()))
            .ReturnsAsync(new List<string> { "TransferWordingFlag" });

        _accessKeyWordingFlagsServiceMock
            .Setup(x => x.GetGenericJourneysFlags(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<string> { "GenericJourneysFlag" });

        _accessKeyWordingFlagsServiceMock
           .Setup(x => x.GetQuoteSelectionFlags(It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(new List<string> { "QuoteSelectionFlag" });

        _investmentServiceClientMock.Setup(x => x.GetInternalBalance(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<InvestmentInternalBalanceResponse>.None);
    }

    public void GetDcJourneyStatus_ReturnsSubmitted_WhenApplicationSubmitted()
    {
        var accessKeyJsonString = "{\"wordingFlags\": [\"dcretirementapplication\",\"Submitted\",\"dcretirementapplication-Submitted\"]}";
        var accessKey = JsonSerializer.Deserialize<AccessKey>(accessKeyJsonString, SerialiationBuilder.Options());

        var result = _sut.GetDcJourneyStatus(accessKey.WordingFlags);
        result.Should().Be(MdpConstants.DcJourneyStatus.Submitted);
    }

    public void GetDcJourneyStatus_ReturnsStarted_WhenApplicationStarted()
    {
        var accessKeyJsonString = "{\"wordingFlags\": [\"dcretirementapplication\",\"started\",\"dcretirementapplication-started\"]}";
        var accessKey = JsonSerializer.Deserialize<AccessKey>(accessKeyJsonString, SerialiationBuilder.Options());

        var result = _sut.GetDcJourneyStatus(accessKey.WordingFlags);
        result.Should().Be(MdpConstants.DcJourneyStatus.Started);
    }

    public void GetDcJourneyStatus_ReturnsExploreOptions_WhenExploreOptionsStarted()
    {
        var accessKeyJsonString = "{\"wordingFlags\": [\"dcexploreoptions\",\"Started\",\"dcexploreoptions-Started\"]}";
        var accessKey = JsonSerializer.Deserialize<AccessKey>(accessKeyJsonString, SerialiationBuilder.Options());

        var result = _sut.GetDcJourneyStatus(accessKey.WordingFlags);
        result.Should().Be(MdpConstants.DcJourneyStatus.ExploreOptions);
    }

    public void GetDcJourneyStatus_ReturnsNull_WhenNoMatchingFlags()
    {
        var wordingFlags = new[] { "OTHER_FLAG" };
        var result = _sut.GetDcJourneyStatus(wordingFlags);
        result.Should().BeNull();
    }

    public void GetDcJourneyStatus_ReturnsNull_WhenWordingFlagsNull()
    {
        var result = _sut.GetDcJourneyStatus(null);
        result.Should().BeNull();
    }

    public void GetDcJourneyStatus_ReturnsSubmitted_WhenMultipleFlagsPresent()
    {
        var wordingFlags = new[] {
            MdpConstants.DcJourneyStatus.DcExploreOptionsStarted,
            MdpConstants.DcJourneyStatus.DcRetirementApplicationStarted,
            MdpConstants.DcJourneyStatus.DcRetirementApplicationSubmitted
        };
        var result = _sut.GetDcJourneyStatus(wordingFlags);
        result.Should().Be(MdpConstants.DcJourneyStatus.Submitted);
    }
}