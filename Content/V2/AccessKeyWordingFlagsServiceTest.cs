using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.BankAccounts;
using WTW.MdpService.BankAccounts.Services;
using WTW.MdpService.Content.V2;
using WTW.MdpService.Domain.Common.Journeys;
using WTW.MdpService.Domain.Mdp;
using WTW.MdpService.Domain.Mdp.Calculations;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.MdpService.Infrastructure.CasesApi;
using WTW.MdpService.Infrastructure.Content;
using WTW.MdpService.Infrastructure.EpaService;
using WTW.MdpService.Infrastructure.MdpDb;
using WTW.MdpService.Infrastructure.MemberDb;
using WTW.MdpService.Infrastructure.MemberDb.IfaReferrals;
using WTW.MdpService.SingleAuth.Services;
using WTW.MdpService.Test.Domain.Mdp.Calculations;
using WTW.MdpService.Test.Domain.Mdp.Journeys;
using WTW.MdpService.Test.Domain.Members;
using WTW.TestCommon.FixieConfig;
using WTW.TestCommon.Helpers;
using WTW.Web;
using WTW.Web.Serialization;
using static WTW.Web.MdpConstants;

namespace WTW.MdpService.Test.Content.V2;

public class AccessKeyWordingFlagsServiceTest
{
    private readonly Mock<ICalculationsParser> _calculationsParserMock;
    private readonly Mock<ITenantRetirementTimelineRepository> _tenantRetirementTimelineRepositoryMock;
    private readonly Mock<IMemberIfaReferral> _memberIfaReferralMock;
    private readonly Mock<ITransferJourneyRepository> _transferJourneyRepositoryMock;
    private readonly Mock<IJourneysRepository> _journeysRepositoryMock;
    private readonly Mock<IQuoteSelectionJourneyRepository> _quoteSelectionJourneyRepositoryMock;
    private readonly Mock<ICasesClient> _caseClientMock;
    private readonly LoggerFactory _loggerFactory;
    private readonly Mock<ILogger<AccessKeyWordingFlagsService>> _loggerMock;
    private readonly Mock<IRetirementService> _retirementServiceMock;
    private readonly Mock<IEpaServiceClient> _epaServiceClientMock;
    private readonly Mock<ISingleAuthService> _singleAuthServiceMock;
    private readonly Mock<IBankService> _bankServiceMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly AccessKeyWordingFlagsService _sut;

    public AccessKeyWordingFlagsServiceTest()
    {
        _calculationsParserMock = new Mock<ICalculationsParser>();
        _tenantRetirementTimelineRepositoryMock = new Mock<ITenantRetirementTimelineRepository>();
        _memberIfaReferralMock = new Mock<IMemberIfaReferral>();
        _transferJourneyRepositoryMock = new Mock<ITransferJourneyRepository>();
        _journeysRepositoryMock = new Mock<IJourneysRepository>();
        _quoteSelectionJourneyRepositoryMock = new Mock<IQuoteSelectionJourneyRepository>();
        _caseClientMock = new Mock<ICasesClient>();
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerMock = new Mock<ILogger<AccessKeyWordingFlagsService>>();
        _retirementServiceMock = new Mock<IRetirementService>();
        _epaServiceClientMock = new Mock<IEpaServiceClient>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _singleAuthServiceMock = new Mock<ISingleAuthService>();
        _bankServiceMock = new Mock<IBankService>();

        loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_loggerMock.Object);

        _sut = new AccessKeyWordingFlagsService(
            _calculationsParserMock.Object,
            _tenantRetirementTimelineRepositoryMock.Object,
            _memberIfaReferralMock.Object,
            _transferJourneyRepositoryMock.Object,
            _journeysRepositoryMock.Object,
            _quoteSelectionJourneyRepositoryMock.Object,
            _caseClientMock.Object,
            loggerFactoryMock.Object,
            _retirementServiceMock.Object,
            _epaServiceClientMock.Object,
            _singleAuthServiceMock.Object,
            _httpContextAccessorMock.Object,
            _bankServiceMock.Object);
    }

    [Input(true, true)]
    [Input(false, false)]
    public void GetsCalcApiDatesAgesEndpointWordingFlags(bool retirementDatesAgesExists, bool expectedResult)
    {
        var result = _sut.GetCalcApiDatesAgesEndpointWordingFlags(
            retirementDatesAgesExists ? GetRetirementDatesAges() : Option<RetirementDatesAges>.None);

        result.Contains("test1").Should().Be(expectedResult);
    }

    [Input(false, 2, "transfer3;qesKey-ansKey")]
    [Input(true, 4, "transfer3;transferFlag1;transferFlag2;qesKey-ansKey")]
    public async Task GetsTransferWordingFlags_WhenHasTransferQuoteJson(bool hasTransferQuoteJson, int expectedCount, string flagsToContain)
    {
        var transferCalculation = new MdpService.Domain.Mdp.Calculations.TransferCalculation(
            "RBS", "1111124",
            hasTransferQuoteJson ? TestData.TransferQuoteJson : null,
            DateTimeOffset.UtcNow);

        _calculationsParserMock
            .Setup(x => x.GetTransferQuote(It.IsAny<string>()))
            .Returns(new TransferQuote(JsonSerializer.Deserialize<TransferQuoteDto>(TestData.TransferQuoteJson, SerialiationBuilder.Options())));
        var journey = new TransferJourneyBuilder().BuildWithSteps();
        journey.TrySubmitStep("transfer_start_1", "transfer_start_2", DateTimeOffset.UtcNow, "qesKey", "ansKey");
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(journey);

        var result = await _sut.GetTransferWordingFlags(transferCalculation);

        result.Should().HaveCount(expectedCount);
        foreach (var flag in flagsToContain.Split(';'))
            result.Contains(flag).Should().BeTrue();
    }

    public async Task GetsTransferWordingFlags_WhenCalculationDoesNotExist()
    {
        var result = await _sut.GetTransferWordingFlags(Option<MdpService.Domain.Mdp.Calculations.TransferCalculation>.None);

        result.Should().BeEmpty();
    }

    [Input(false, 0)]
    [Input(true, 1)]
    public async Task GetsIfaReferralFlags(bool hasIfaReferral, int expectedCount)
    {
        _memberIfaReferralMock
            .Setup(x => x.HasIfaReferral(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(hasIfaReferral);

        var result = await _sut.GetIfaReferralFlags(It.IsAny<string>(), It.IsAny<string>());

        result.Should().HaveCount(expectedCount);
    }

    [Input("", "1436", "PD-BOTH-DAY")]
    [Input("1335", "", "PD-MONTH-DAY")]
    [Input("3132", "2140", "PD-LAST-DAY")]
    public async Task GetsPayTimelineWordingFlags(string category, string scheme, string expectedResult)
    {
        _tenantRetirementTimelineRepositoryMock
            .Setup(x => x.FindPentionPayTimelines(It.IsAny<string>()))
            .ReturnsAsync(Timelines());

        var result = await _sut.GetPayTimelineWordingFlags(new MemberBuilder().Category(category).SchemeCode(scheme).Build());

        result.Should().Contain(expectedResult);
    }

    [Input(true, 1)]
    [Input(false, 0)]
    public async Task GetsLinkedMemberFlags(bool hasLinkedMembers, int expectedCount)
    {
        var builder = new MemberBuilder();
        if (hasLinkedMembers)
            builder = builder.LinkedMembers(new LinkedMember("1111124", "RBS", "1234567", "BCL"));

        var result = await _sut.GetLinkedMemberFlags(builder.Build());

        result.Contains("HASLINKEDMEMBER").Should().Be(hasLinkedMembers);
        result.Should().HaveCount(expectedCount);
    }

    public async Task ReturnsHasLinkedMemberFlag_WhenAuthSchemeIsNotOpenAm()
    {
        _singleAuthServiceMock.Setup(x => x.GetSingleAuthClaim(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>())).Returns(new Guid());
        _singleAuthServiceMock.Setup(x => x.GetLinkedRecord(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(new List<(string, string)> { ("Tenant1", "Record1"), ("Tenant2", "Record2") });
        _httpContextAccessorMock.SetupGet(x => x.HttpContext)
             .Returns(new DefaultHttpContext()
             {
                 User = new ClaimsPrincipal(new ClaimsIdentity(new[] {
                    new Claim(MdpConstants.MemberClaimNames.Sub, WTW.TestCommon.TestData.Sub.ToString())
                 }))
             });

        var result = await _sut.GetLinkedMemberFlags(new MemberBuilder().Build(), false);

        result.Should().HaveCount(1);
        result.Should().Contain("HASLINKEDMEMBER");
        _loggerMock.VerifyLogging("Linked records found for user", LogLevel.Information);
    }

    public async Task GetLinkedMemberFlagsReturnsEmptyListIfLinkedRecordsNotFound_WhenAuthSchemeIsNotOpenAm()
    {
        _singleAuthServiceMock.Setup(x => x.GetSingleAuthClaim(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>())).Returns(new Guid());
        _singleAuthServiceMock.Setup(x => x.GetLinkedRecord(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(new List<(string, string)> { });
        _httpContextAccessorMock.SetupGet(x => x.HttpContext)
            .Returns(new DefaultHttpContext()
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] {
                    new Claim(MdpConstants.MemberClaimNames.Sub, WTW.TestCommon.TestData.Sub.ToString())
                }))
            });

        var result = await _sut.GetLinkedMemberFlags(new MemberBuilder().Build(), false);
        result.Should().HaveCount(0);
        _loggerMock.VerifyLogging("No linked records found for user", LogLevel.Warning);
    }

    public async Task GetLinkedMemberFlagsReturnsEmptyListIfGetSingleAuthAuthClaimReturnsNotRightResult_WhenAuthSchemeIsNotOpenAm()
    {
        _httpContextAccessorMock.SetupGet(x => x.HttpContext)
             .Returns(new DefaultHttpContext()
             {
                 User = new ClaimsPrincipal(new ClaimsIdentity(new[] {
                    new Claim(MdpConstants.MemberClaimNames.Sub, WTW.TestCommon.TestData.Sub.ToString())
                 }))
             });
        _singleAuthServiceMock.Setup(x => x.GetSingleAuthClaim(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>())).Returns(new Error());

        var result = await _sut.GetLinkedMemberFlags(new MemberBuilder().Build(), false);
        result.Should().HaveCount(0);
        _loggerMock.VerifyLogging($"SubResult is not a Right value, unable to proceed with linked member retrieval.", LogLevel.Error);
    }

    [Input("RBS", false, "55")]
    [Input("RBS", true, "55")]
    [Input("HBS", false, "55")]
    [Input("HBS", true, "55")]
    [Input("HBS", true, null)]
    public void GetsHbsFlags_WhenZeroFlagsReturned(string businessGroup, bool retirementDatesAgesExists, string normalMinimumPensionAge)
    {
        var retirementDatesAges = retirementDatesAgesExists
            ? GetRetirementDatesAges(normalMinimumPensionAge != null ? decimal.Parse(normalMinimumPensionAge) : null)
            : Option<RetirementDatesAges>.None;

        var result = _sut.GetHbsFlags(new MemberBuilder().BusinessGroup(businessGroup).Build(), retirementDatesAges);

        result.Should().BeEmpty();
    }


    [Input(52, "PMP-NE")]
    [Input(55, "PMP-E")]
    public void GetsHbsFlags_ReturnsFlags(int years, string expectedFlag)
    {
        var retirementDatesAges = GetRetirementDatesAges();
        var member = new MemberBuilder().BusinessGroup("HBS").DateOfBirth(DateTimeOffset.UtcNow.AddYears(-years).AddMonths(1)).Build();
        var result = _sut.GetHbsFlags(member, retirementDatesAges);

        result.Should().HaveCount(1);
        result.Should().Contain(expectedFlag);
    }

    public void GetsSchemeFlags()
    {
        var result = _sut.GetSchemeFlags(new MemberBuilder().Build());

        result.Should().HaveCount(1);
        result.Should().Contain("scheme_schemeCode");
    }

    public void GetsCategoryFlags()
    {
        var member = new MemberBuilder()
            .Category("1111")
            .Build();

        var result = _sut.GetCategoryFlags(member);

        result.Should().HaveCount(1);
        result.Should().Contain("category_1111");
    }

    public async Task GetsRetirementFlags_WhenNoCalculationExists()
    {
        var result = await _sut.GetRetirementFlags(Error.New("test"), It.IsAny<bool>());

        result.Should().HaveCount(0);
    }

    public async Task GetsRetirementFlags_WhenRetirementV1Exists()
    {
        _calculationsParserMock
            .Setup(x => x.GetRetirement(It.IsAny<string>()))
            .Returns(new MdpService.Domain.Mdp.Calculations.Retirement(JsonSerializer.Deserialize<RetirementDto>(TestData.RetiremntJson, SerialiationBuilder.Options())));

        var result = await _sut.GetRetirementFlags(new CalculationBuilder().BuildV1(), It.IsAny<bool>());

        result.Should().HaveCount(4);
        result.Should().Contain("GMP", "GMPINPAY", "GMPPOST88", "GMPPRE88");
    }

    public async Task GetsRetirementFlags_WhenRetirementV2ExistsAndJourneyExpired()
    {
        _calculationsParserMock
            .Setup(x => x.GetRetirementV2(It.IsAny<string>()))
            .Returns(new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options())));

        var calculation = new CalculationBuilder().BuildV2();
        var journey = new RetirementJourneyBuilder().daysToExpire(-90).BuildWithSteps();
        journey.TrySubmitStep("step2", "step3", DateTimeOffset.UtcNow, "qesKey1", "ansKey1");
        calculation.SetJourney(journey, "label");

        var result = await _sut.GetRetirementFlags(calculation, It.IsAny<bool>());

        result.Should().HaveCount(11);
        result.Should().Contain("GMP", "GMPINPAY", "GMPPOST88", "GMPPRE88",
            "GMPEQCHECKED", "tranche_post88GMP_GMP", "tranche_post97_LPI5",
            "tranche_pre88GMP_NIL", "tranche_pre97Excess_LPI5", "EXPIREDRA", "qesKey1-ansKey1");
    }

    [Input("", TestData.RetirementV2ReducedPensionNoLumpSumJson, TestData.SelectedQuoteV2ReducedPensionNoLumpSumDict, false)]
    [Input(null, TestData.RetirementV2ReducedPensionNoLumpSumJson, TestData.SelectedQuoteV2ReducedPensionNoLumpSumDict, false)]
    [Input("DoesNotStartWithreducedPension", TestData.RetirementV2ReducedPensionNoLumpSumJson, TestData.SelectedQuoteV2ReducedPensionNoLumpSumDict, false)]
    [Input("reducedPension", TestData.RetirementV2ReducedPensionNoLumpSumJson, TestData.SelectedQuoteV2ReducedPensionNoLumpSumDict, false)]
    [Input("reducedPension", TestData.RetirementV2ReducedPensionNoMaxPermittedLumpSumJson, TestData.SelectedQuoteV2ReducedPensionNoMaxPermittedLumpSumDict, false)]
    [Input("reducedPension", TestData.RetirementV2ReducedPensionLumsumSameAsMaxPermittedLumpSumJson, TestData.SelectedQuoteV2ReducedPensionLumsumSameAsMaxPermittedLumpSumDict, false)]
    [Input("reducedPension", TestData.RetirementV2ReducedPensionLumsumSmallerThanMaxPermittedLumpSumJson, TestData.SelectedQuoteV2ReducedPensionLumsumSmallerThanMaxPermittedLumpSumDict, true)]
    [Input("reducedPension.DCAsPCLS", TestData.RetirementV2ReducedPensionNestedLumsumSmallerThanMaxPermittedLumpSumJson, TestData.SelectedQuoteV2ReducedPensionNestedLumsumSmallerThanMaxPermittedLumpSumDict, true)]
    public async Task GetsRetirementFlags_WhenRetirementV2ExistsAndReducedPensionWithLumsum(string selectedOptionStartString, string RetirementJsonV2Data,
        string selectedQuoteDetailsString, bool isExpectedFlagPresent)
    {
        _calculationsParserMock
            .Setup(x => x.GetRetirementV2(It.IsAny<string>()))
            .Returns(new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(RetirementJsonV2Data, SerialiationBuilder.Options())));

        Dictionary<string, object> selectedQuoteDetails = new Dictionary<string, object>();
        selectedQuoteDetails = JsonSerializer.Deserialize<Dictionary<string, object>>(selectedQuoteDetailsString);

        _retirementServiceMock
            .Setup(x => x.GetSelectedQuoteDetails(It.IsAny<string>(), It.IsAny<RetirementV2>()))
            .Returns(selectedQuoteDetails);

        var calculation = new CalculationBuilder().BuildV2();
        calculation.SetJourney(new RetirementJourneyBuilder().Build(), selectedOptionStartString);

        var result = await _sut.GetRetirementFlags(calculation, false);

        result.Contains("RequestedLumpSumLessThanMax").Should().Be(isExpectedFlagPresent);
    }

    public async Task WhenGetRetirementFlagsIsCalledAndQuoteJourneyExists_ThenExpectedWordingFlagIsAdded()
    {
        var calculation = new CalculationBuilder().BuildV2();
        _quoteSelectionJourneyRepositoryMock.Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(Option<QuoteSelectionJourney>.Some(new("test", "TEST123", DateTimeOffset.UtcNow, "curr-pagekey", "next-pagekey", "reducedPension")));
        _calculationsParserMock
             .Setup(x => x.GetRetirementV2(It.IsAny<string>()))
             .Returns(new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options())));
        Dictionary<string, object> selectedQuoteDetails = new Dictionary<string, object>();
        selectedQuoteDetails = JsonSerializer.Deserialize<Dictionary<string, object>>(TestData.SelectedQuoteV2ReducedPensionLumsumSmallerThanMaxPermittedLumpSumDict);
        _retirementServiceMock
            .Setup(x => x.GetSelectedQuoteDetails(It.IsAny<string>(), It.IsAny<RetirementV2>()))
            .Returns(selectedQuoteDetails);

        var result = await _sut.GetRetirementFlags(calculation, false);

        result.Contains("RequestedLumpSumLessThanMax").Should().Be(true);
    }

    public async Task WhenGetRetirementFlagsIsCalledAndQuoteJourneyExistsButLumpSumConditionIsNotMet_ThenExpectedWordingFlagIsAdded()
    {
        var calculation = new CalculationBuilder().BuildV2();
        _quoteSelectionJourneyRepositoryMock.Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(Option<QuoteSelectionJourney>.Some(new("test", "TEST123", DateTimeOffset.UtcNow, "curr-pagekey", "next-pagekey", "reducedPension")));
        _calculationsParserMock
             .Setup(x => x.GetRetirementV2(It.IsAny<string>()))
             .Returns(new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options())));
        Dictionary<string, object> selectedQuoteDetails = new Dictionary<string, object>();
        selectedQuoteDetails = JsonSerializer.Deserialize<Dictionary<string, object>>(TestData.SelectedQuoteV2ReducedPensionLumsumSameAsMaxPermittedLumpSumDict);
        _retirementServiceMock
            .Setup(x => x.GetSelectedQuoteDetails(It.IsAny<string>(), It.IsAny<RetirementV2>()))
            .Returns(selectedQuoteDetails);

        var result = await _sut.GetRetirementFlags(calculation, false);

        result.Contains("RequestedLumpSumLessThanMax").Should().Be(false);
    }

    public async Task WhenGetRetirementFlagsIsCalledAndQuoteJourneyDoesNotExist_ThenExpectedWordingFlagIsNotReturned()
    {
        var calculation = new CalculationBuilder().BuildV2();
        _quoteSelectionJourneyRepositoryMock.Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(Option<QuoteSelectionJourney>.None);
        _calculationsParserMock
             .Setup(x => x.GetRetirementV2(It.IsAny<string>()))
             .Returns(new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options())));
        Dictionary<string, object> selectedQuoteDetails = new Dictionary<string, object>();
        selectedQuoteDetails = JsonSerializer.Deserialize<Dictionary<string, object>>(TestData.SelectedQuoteV2ReducedPensionLumsumSmallerThanMaxPermittedLumpSumDict);

        var result = await _sut.GetRetirementFlags(calculation, false);
        result.Should().NotContain("RequestedLumpSumLessThanMax");
    }

    [Input(TestData.RetirementV2DCJourneyLumpSumLessThanMaxPermittedLumpSum, TestData.SelectedQuoteDetailsLessThanMaxDCJourney, true)]
    [Input(TestData.RetirementV2DCJourneyLumpSumEqualToMaxPermittedLumpSum, TestData.SelectedQuoteDetailsEqualToMaxDCJourney, false)]
    public async Task WhenGetRetirementJourneyFlagIsCalledForDCJourney_ThenExpectedResultIsReturned(string RetirementJsonV2Data,
         string selectedQuoteDetailsString, bool isExpectedFlagPresent)
    {
        _calculationsParserMock
           .Setup(x => x.GetRetirementV2(It.IsAny<string>()))
           .Returns(new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(RetirementJsonV2Data, SerialiationBuilder.Options())));

        Dictionary<string, object> selectedQuoteDetails = new Dictionary<string, object>();
        selectedQuoteDetails = JsonSerializer.Deserialize<Dictionary<string, object>>(selectedQuoteDetailsString);

        selectedQuoteDetails.TryGetValue("selectedQuoteFullName", out var selectedQuoteName);
        _retirementServiceMock
            .Setup(x => x.GetSelectedQuoteDetails(It.IsAny<string>(), It.IsAny<RetirementV2>()))
            .Returns(selectedQuoteDetails);

        var _stubGenericJourney = new GenericJourney("LIF", "0028542", "dcretirementapplication", "page1", "page2", false, "started", DateTimeOffset.UtcNow);
        _stubGenericJourney.TrySubmitStep("step2", "step3", DateTimeOffset.UtcNow, "TestQuestionKey", "TestAnswerKey");
        _stubGenericJourney.GetFirstStep().UpdateGenericData("SelectedQuoteDetails", selectedQuoteDetailsString);

        _journeysRepositoryMock.Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(_stubGenericJourney);

        var calculation = new CalculationBuilder().RetirementJsonV2(RetirementJsonV2Data).BuildV2();
        calculation.SetJourney(new RetirementJourneyBuilder().Build(), selectedQuoteName.ToString());

        var result = await _sut.GetRetirementFlags(calculation, isSchemeDC: true);

        result.Contains("RequestedLumpSumLessThanMax").Should().Be(isExpectedFlagPresent);
    }

    [Input(TestData.RetirementV2DCJourneyLumpSumLessThanMaxPermittedLumpSum, TestData.SelectedQuoteDetailsLessThanMaxDCJourney, true)]
    [Input(TestData.RetirementV2DCJourneyLumpSumEqualToMaxPermittedLumpSum, TestData.SelectedQuoteDetailsEqualToMaxDCJourney, false)]
    public async Task WhenGetRetirementJourneyFlagIsCalledForDCJourneyAndQuoteJourneyExists_ThenExpectedResultIsReturned(string RetirementJsonV2Data,
         string selectedQuoteDetailsString, bool isExpectedFlagPresent)
    {
        _calculationsParserMock
           .Setup(x => x.GetRetirementV2(It.IsAny<string>()))
           .Returns(new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(RetirementJsonV2Data, SerialiationBuilder.Options())));

        _quoteSelectionJourneyRepositoryMock.Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(Option<QuoteSelectionJourney>.Some(new("test", "TEST123", DateTimeOffset.UtcNow, "curr-pagekey", "next-pagekey", "annuityBrokerHUBTFC")));

        Dictionary<string, object> selectedQuoteDetails = new Dictionary<string, object>();
        selectedQuoteDetails = JsonSerializer.Deserialize<Dictionary<string, object>>(selectedQuoteDetailsString);

        selectedQuoteDetails.TryGetValue("selectedQuoteFullName", out var selectedQuoteName);
        _retirementServiceMock
            .Setup(x => x.GetSelectedQuoteDetails(It.IsAny<string>(), It.IsAny<RetirementV2>()))
            .Returns(selectedQuoteDetails);

        var calculation = new CalculationBuilder().RetirementJsonV2(RetirementJsonV2Data).BuildV2();
        calculation.SetJourney(new RetirementJourneyBuilder().Build(), selectedQuoteName.ToString());

        var result = await _sut.GetRetirementFlags(calculation, isSchemeDC: true);

        result.Contains("RequestedLumpSumLessThanMax").Should().Be(isExpectedFlagPresent);
    }

    public async Task GetsGenericJourneysFlags()
    {
        var journeyWithQuestion = new GenericJourney("RBS", "1111122", "genericJourneyFlag2", "page1", "page2", false,
            "started2", DateTimeOffset.UtcNow);
        journeyWithQuestion.SetWordingFlags(new List<string> { "a-b", "c-d", " ", "d-e", "" });
        journeyWithQuestion.TrySubmitStep("page2", "page3", DateTimeOffset.UtcNow, "questionKey1", "answerKey1",
            "answerValue1");
        _journeysRepositoryMock
            .Setup(x => x.FindAll(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<GenericJourney>
            {
                new GenericJourney("RBS", "1111122", "genericJourneyFlag1", "page1", "page2", false, "started1", DateTimeOffset.UtcNow),
                journeyWithQuestion,
            });

        var result = await _sut.GetGenericJourneysFlags(It.IsAny<string>(), It.IsAny<string>());

        result.Should().HaveCount(10);
        result.Should().Contain("genericJourneyFlag1", "genericJourneyFlag2", "started1", "a-b", "c-d", "d-e",
            "started2", "genericJourneyFlag1-started1", "genericJourneyFlag2-started2", "questionKey1-answerKey1");
    }

    [Input("cashLumpsum", true, 3)]
    [Input("incomeDrawdownTFC", true, 3)]
    [Input("incomeDrawdownITF", true, 3)]
    [Input("incomeDrawdownOMTFC", true, 3)]
    [Input("incomeDrawdownOMITF", true, 3)]
    [Input("random", false, 2)]
    public async Task GetsQuoteSelectionFlags(string quoteSelection, bool containsMpaaFlag, int wordingFlagsCount)
    {
        _quoteSelectionJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new WTW.MdpService.Domain.Mdp.QuoteSelectionJourney("RBS", "0304442", DateTimeOffset.Now, "current", "nextPageKey", $"TestQuoteName.InnerQuoteName.{quoteSelection}"));

        var result = await _sut.GetQuoteSelectionFlags(It.IsAny<string>(), It.IsAny<string>());

        result.Should().HaveCount(wordingFlagsCount);
        result.Should().Contain($"TestQuoteName.InnerQuoteName.{quoteSelection}");
        result.Should().Contain("SelectedQuoteName-" + $"TestQuoteName.InnerQuoteName.{quoteSelection}");
        result.Contains("MPAA").Should().Be(containsMpaaFlag);
    }

    public async Task GetRetirementOrTransferCasesFlag_ReturnsWordingFlag_WhenAtLeastOneAnyCaseExists()
    {
        _caseClientMock
            .Setup(x => x.GetRetirementOrTransferCases(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<CasesResponse> { GetRTQ9CasesResponse() });

        var result = await _sut.GetRetirementOrTransferCasesFlag(It.IsAny<string>(), It.IsAny<string>());

        result.Should().HaveCount(1);
        result.Should().Contain("QUOTE_CASES_AVAILABLE");
    }

    public async Task GetRetirementOrTransferCasesFlag_ReturnsWordingFlags_WhenFiveOrMoreOpenedRTQ9CasesExists()
    {
        _caseClientMock
            .Setup(x => x.GetRetirementOrTransferCases(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<CasesResponse> { GetRTQ9CasesResponse(), GetRTQ9CasesResponse(), GetRTQ9CasesResponse(), GetRTQ9CasesResponse(), GetRTQ9CasesResponse() });

        var result = await _sut.GetRetirementOrTransferCasesFlag(It.IsAny<string>(), It.IsAny<string>());

        result.Should().HaveCount(2);
        result.Should().Contain("QUOTE_CASES_AVAILABLE");
        result.Should().Contain("overTheRetirementQuoteLimit");
    }

    public async Task GetRetirementOrTransferCasesFlag_ReturnsWordingFlags_WhenFiveOrMoreOpenedRTQ9AndAtLeastOneTOQ9CasesExists()
    {
        _caseClientMock
            .Setup(x => x.GetRetirementOrTransferCases(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<CasesResponse> {
                GetRTQ9CasesResponse(),
                GetRTQ9CasesResponse(),
                GetRTQ9CasesResponse(),
                GetRTQ9CasesResponse(),
                GetRTQ9CasesResponse(),
                new CasesResponse
                {
                    CaseCode = "TOQ9",
                    CaseStatus = "Open",
                    CreationDate = "2024-02-13 00:00:00",
                    CompletionDate = null,
                    CaseNumber = "1707113"
                }});

        var result = await _sut.GetRetirementOrTransferCasesFlag(It.IsAny<string>(), It.IsAny<string>());

        result.Should().HaveCount(3);
        result.Should().Contain("QUOTE_CASES_AVAILABLE");
        result.Should().Contain("overTheRetirementQuoteLimit");
        result.Should().Contain("overTheTransferQuoteLimit");
    }

    private static CasesResponse GetRTQ9CasesResponse()
    {
        return new CasesResponse
        {
            CaseCode = "RTQ9",
            CaseStatus = "Open",
            CreationDate = "2024-02-13 00:00:00",
            CompletionDate = null,
            CaseNumber = "1707113"
        };
    }

    public async Task GetRetirementOrTransferCasesFlag_ReturnsEmptyList_WhenCaseDoesNotExists()
    {
        _caseClientMock
            .Setup(x => x.GetRetirementOrTransferCases(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<CasesResponse>());

        var result = await _sut.GetRetirementOrTransferCasesFlag(It.IsAny<string>(), It.IsAny<string>());

        result.Should().BeEmpty();
    }

    public async Task GetRetirementOrTransferCasesFlag_ReturnsEmptyList_WhenCaseApiReturnsNoResult()
    {
        _caseClientMock
            .Setup(x => x.GetRetirementOrTransferCases(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<IEnumerable<CasesResponse>>.None);

        var result = await _sut.GetRetirementOrTransferCasesFlag(It.IsAny<string>(), It.IsAny<string>());

        result.Should().BeEmpty();
    }


    [Input("RTP9", "PaperRetirementApplicationInProgress")]
    [Input("TOP9", "PaperTransferApplicationInProgress")]
    public async Task WhenGetRetirementOrTransferCasesFlagIsCalledAndOpenApplicationIsFound_ThenExpectedWordingFlagIsReturned(string caseCode, string expectedFlag)
    {
        _caseClientMock
            .Setup(x => x.GetRetirementOrTransferCases(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<IEnumerable<CasesResponse>>.Some(new List<CasesResponse>() { GetPaperCase(caseCode) }));

        var result = await _sut.GetRetirementOrTransferCasesFlag("RBS", "1008ABC");

        result.Should().NotBeNullOrEmpty();
        result.Should().Contain(expectedFlag);
    }

    private static CasesResponse GetPaperCase(string caseCode)
    {
        return new CasesResponse()
        {
            CaseCode = caseCode,
            CaseNumber = "12345",
            CaseStatus = "Open",
            CreationDate = DateTime.UtcNow.ToString(),
            CompletionDate = null,
            CaseSource = "ERP"
        };
    }


    private static RetirementDatesAges GetRetirementDatesAges(decimal? normalMinimumPensionAge = 55)
    {
        return new RetirementDatesAges(new RetirementDatesAgesDto
        {
            EarliestRetirementAge = 50,
            NormalMinimumPensionAge = normalMinimumPensionAge,
            NormalRetirementAge = 62,
            EarliestRetirementDate = new DateTimeOffset(DateTime.Parse("2018-06-30")),
            NormalMinimumPensionDate = new DateTimeOffset(DateTime.Parse("2023-06-30")),
            NormalRetirementDate = new DateTimeOffset(DateTime.Parse("2030-06-30")),
            TargetRetirementDate = new DateTimeOffset(DateTime.Parse("2030-06-30")),
            WordingFlags = new List<string> { "test1" }
        });
    }

    private static List<TenantRetirementTimeline> Timelines()
    {
        return new List<TenantRetirementTimeline>()
        {
            new TenantRetirementTimeline(1, "RETFIRSTPAYMADE","RT", "HBS", "*",
                "1300,1302,1303,1304,1325,1326,1330,1332,1333,1334,1335,1336,1340,1342,1343,1344,1345,1346", "+,0,+,0,0,23,+"),
            new TenantRetirementTimeline(2, "RETFIRSTPAYMADE","RT", "HBS",
                "1400,1402,1403,1404,1425,1426,1435,1436,1445,1446", "*", "+,0,+,0,0,5,-"),
            new TenantRetirementTimeline(1, "RETFIRSTPAYMADE","RT", "HBS",
                "2100,2102,2103,2125,2126,2135,2136,2140,2142,2143,2145,2146,2200,2202,2203,2225,2226",
                "3100,3102,3103,3125,3126,3130,3132,3133,3135,3136,3140,3142,3143,3145,3146", "+,0,+,0,0,29,+"),
        };
    }

    [Input("AVCDET", "1", "HasExternalAVCs")]
    [Input("HASDC", "2", "HasDCAssets")]
    [Input("DBONLY", "'Y'", "HasDBOnly")]
    [Input("IDDMOD", "'S'", "WithdrawalSummaryMode")]
    [Input("IDDMOD", "'M'", "WithdrawalStandardMode")]
    public async Task WhenGetWordingsForWebRulesCalledWithValidInput_ThenExpectedFlagIsReturnedAndLoggingOccurred(string webRule, string expectedWebruleResult, string inputWordingFlags)
    {
        var expectedWordingFlags = inputWordingFlags.Split(',').ToList();
        var testMember = new MemberBuilder().Build();
        var testUserId = "test";

        _epaServiceClientMock.Setup(x => x.GetWebRuleResult(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Boolean>()))
            .ReturnsAsync(Option<WebRuleResultResponse>.Some(new WebRuleResultResponse { Result = expectedWebruleResult }));

        var testWebRuleWordingFlags = new List<ContentClassifierValue> {
            new ContentClassifierValue { Key = new ContentClassifierElement {ElementType= "text", Value ="HasExternalAVCs"}, Value = new ContentClassifierElement { ElementType = "text", Value = "AVCDET=1" } },
            new ContentClassifierValue { Key = new ContentClassifierElement {ElementType= "text", Value ="HasDCAssets"}, Value = new ContentClassifierElement { ElementType = "text", Value = "HASDC=2" } },

            new ContentClassifierValue { Key = new ContentClassifierElement {ElementType= "text", Value ="HasDBOnly"}, Value = new ContentClassifierElement { ElementType = "text", Value = "DBONLY='Y'" } },

            new ContentClassifierValue { Key = new ContentClassifierElement {ElementType= "text", Value ="WithdrawalSummaryMode"}, Value = new ContentClassifierElement { ElementType = "text", Value = "IDDMOD='S'" } },

            new ContentClassifierValue { Key = new ContentClassifierElement {ElementType= "text", Value ="WithdrawalStandardMode"}, Value = new ContentClassifierElement { ElementType = "text", Value = "IDDMOD='M'" } }
        };

        var actualWebRuleWordingFlagsResult = await _sut.GetWordingsForWebRules(testMember, testUserId, testWebRuleWordingFlags);

        actualWebRuleWordingFlagsResult.Should().NotBeNullOrEmpty();
        actualWebRuleWordingFlagsResult.Should().HaveCount(expectedWordingFlags.Count());
        actualWebRuleWordingFlagsResult.Should().BeEquivalentTo(expectedWordingFlags);

        _loggerMock.VerifyLogging($"GetWordingsForWebRules is called - member bgroup {testMember.BusinessGroup}, refno {testMember.ReferenceNumber}", LogLevel.Information, Times.Once());
        _loggerMock.VerifyLogging($"GetWordingsForWebRules - ProcessWordingFlag - Webrule result for bgroup {testMember.BusinessGroup}, refno {testMember.ReferenceNumber} is retrieved for wordingflag {inputWordingFlags}, rule {webRule} with result= {expectedWebruleResult}, expected result= {expectedWebruleResult}", LogLevel.Information, Times.Once());
    }

    public async Task WhenGetWordingsForWebRulesCalledWithValidCachePrefix_ThenExpectedFlagIsReturnedAndLoggingOccurred()
    {
        var expectedWordingFlag = "WordingFlagWithValidCacheOption";
        var validCachePrefix = "noCache";
        var cachePrefixSeparator = ":";
        var webRuleId = "WebRuleId";
        var webRuleResult = "'success'";
        var webRuleSeparator = "=";
        var testMember = new MemberBuilder().Build();
        var testUserId = "test";

        _epaServiceClientMock.Setup(x => x.GetWebRuleResult(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Boolean>()))
            .ReturnsAsync(Option<WebRuleResultResponse>.Some(new WebRuleResultResponse { Result = webRuleResult }));

        var testWebRuleWordingFlags = new List<ContentClassifierValue> {
            new ContentClassifierValue { Key = new ContentClassifierElement {ElementType= "text", Value = expectedWordingFlag}, Value = new ContentClassifierElement { ElementType = "text", Value = validCachePrefix + cachePrefixSeparator + webRuleId + webRuleSeparator + webRuleResult } },
        };

        var actualWebRuleWordingFlagsResult = await _sut.GetWordingsForWebRules(testMember, testUserId, testWebRuleWordingFlags);

        actualWebRuleWordingFlagsResult.Should().NotBeNullOrEmpty();
        actualWebRuleWordingFlagsResult.Should().BeEquivalentTo(expectedWordingFlag);

        _loggerMock.VerifyLogging($"GetWordingsForWebRules is called - member bgroup {testMember.BusinessGroup}, refno {testMember.ReferenceNumber}", LogLevel.Information, Times.Once());
        _loggerMock.VerifyLogging($"GetWordingsForWebRules - ProcessWordingFlag - wordingflag {expectedWordingFlag}, cachePrefix {validCachePrefix} is specified - rule {webRuleId} , result {webRuleResult}, for bgroup {testMember.BusinessGroup}, refno {testMember.ReferenceNumber}", LogLevel.Information, Times.Once());
        _loggerMock.VerifyLogging($"GetWordingsForWebRules - ProcessWordingFlag - Webrule result for bgroup {testMember.BusinessGroup}, refno {testMember.ReferenceNumber} is retrieved for wordingflag {expectedWordingFlag}, rule {webRuleId} with result= {webRuleResult}, expected result= {webRuleResult}", LogLevel.Information, Times.Once());
    }

    public async Task WhenGetWordingsForWebRulesCalledWithInValidCachePrefix_ThenNoFlagIsReturnedAndLoggingOccurred()
    {
        var expectedWordingFlag = "WordingFlagWithInValidCacheOption";
        var inValidCachePrefix = "InvalidnoCache";
        var cachePrefixSeparator = ":";
        var webRuleId = "WebRuleId";
        var webRuleResult = "'success'";
        var webRuleSeparator = "=";
        var testMember = new MemberBuilder().Build();
        var testUserId = "test";

        _epaServiceClientMock.Setup(x => x.GetWebRuleResult(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Boolean>()))
            .ReturnsAsync(Option<WebRuleResultResponse>.Some(new WebRuleResultResponse { Result = webRuleResult }));

        var testWebRuleWordingFlags = new List<ContentClassifierValue> {
            new ContentClassifierValue { Key = new ContentClassifierElement {ElementType= "text", Value = expectedWordingFlag}, Value = new ContentClassifierElement { ElementType = "text", Value = inValidCachePrefix + cachePrefixSeparator + webRuleId + webRuleSeparator + webRuleResult } },
        };

        var actualWebRuleWordingFlagsResult = await _sut.GetWordingsForWebRules(testMember, testUserId, testWebRuleWordingFlags);

        actualWebRuleWordingFlagsResult.Should().BeNullOrEmpty();

        _loggerMock.VerifyLogging($"GetWordingsForWebRules is called - member bgroup {testMember.BusinessGroup}, refno {testMember.ReferenceNumber}", LogLevel.Information, Times.Once());
        _loggerMock.VerifyLogging($"GetWordingsForWebRules - ProcessWordingFlag - Invalid ContentClassifierValue - wordingflag {expectedWordingFlag}, rule {inValidCachePrefix}{cachePrefixSeparator}{webRuleId} , result {webRuleResult}, for bgroup {testMember.BusinessGroup}, refno {testMember.ReferenceNumber}", LogLevel.Warning, Times.Once());
    }

    public async Task WhenGetWordingsForWebRulesCalledWithWrongWebrule_ThenNoFlagIsReturnedAndLoggingOccurred()
    {
        var testMember = new MemberBuilder().Build();
        var testWordingFlag = "HasExternalAVCs";
        var testIncorrectWebRule = "WrongWebRuleNotPresentInSystem";
        var testWebRuleResult = "1";
        var testSeparator = "=";
        var testUserId = "test";

        _epaServiceClientMock.Setup(x => x.GetWebRuleResult(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Boolean>()))
            .ReturnsAsync(Option<WebRuleResultResponse>.None);

        var testWebRuleWordingFlags = new List<ContentClassifierValue> {
            new ContentClassifierValue { Key = new ContentClassifierElement {ElementType= "text", Value = testWordingFlag }, Value = new ContentClassifierElement { ElementType = "text", Value = $"{testIncorrectWebRule}{testSeparator}{testWebRuleResult}" } }
        };

        var actualWebRuleWordingFlagsResult = await _sut.GetWordingsForWebRules(testMember, testUserId, testWebRuleWordingFlags);

        actualWebRuleWordingFlagsResult.Should().BeNullOrEmpty();

        _loggerMock.VerifyLogging($"GetWordingsForWebRules is called - member bgroup {testMember.BusinessGroup}, refno {testMember.ReferenceNumber}", LogLevel.Information, Times.Once());
        _loggerMock.VerifyLogging($"GetWordingsForWebRules - ProcessWordingFlag - Webrule result for bgroup {testMember.BusinessGroup}, refno {testMember.ReferenceNumber} is not retrieved for wordingflag {testWordingFlag}, rule {testIncorrectWebRule}", LogLevel.Warning, Times.Once());
    }

    public async Task WhenGetWordingsForWebRulesCalledWIthIncorrectInput_ThenNoFlagIsReturnedAndLoggingOccurred()
    {
        var testMember = new MemberBuilder().Build();
        var expectedWordingFlag = "HasExternalAVCs";
        var testWebRule = "DummyWebRule";
        var testWebRuleResult = "1";
        var testSeparator = ":";
        var testUserId = string.Empty;

        _epaServiceClientMock.Setup(x => x.GetWebRuleResult(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Boolean>()))
            .ReturnsAsync(Option<WebRuleResultResponse>.None);

        var testWebRuleWordingFlags = new List<ContentClassifierValue> {
            new ContentClassifierValue { Key = new ContentClassifierElement {ElementType= "text", Value = expectedWordingFlag }, Value = new ContentClassifierElement { ElementType = "text", Value = $"{testWebRule}{testSeparator}{testWebRuleResult}" } }
        };

        var actualWebRuleWordingFlagsResult = await _sut.GetWordingsForWebRules(testMember, null, testWebRuleWordingFlags);

        actualWebRuleWordingFlagsResult.Should().BeNullOrEmpty();

        _loggerMock.VerifyLogging($"GetWordingsForWebRules is called - member bgroup {testMember.BusinessGroup}, refno {testMember.ReferenceNumber}", LogLevel.Information, Times.Once());
        _loggerMock.VerifyLogging($"GetWordingsForWebRules - ProcessWordingFlag - Invalid ContentClassifierValue - wordingflag {expectedWordingFlag}, for bgroup {testMember.BusinessGroup}, refno {testMember.ReferenceNumber}", LogLevel.Warning, Times.Once());
    }

    public async Task WhenGetWordingsForWebRulesCalledWIthNoClassifierContentValue_ThenNoFlagIsReturned()
    {
        var testMember = new MemberBuilder().Build();
        var testUserId = "TestUserId";

        _epaServiceClientMock.Setup(x => x.GetWebRuleResult(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Boolean>()))
            .ReturnsAsync(Option<WebRuleResultResponse>.None);


        var actualWebRuleWordingFlagsResult = await _sut.GetWordingsForWebRules(testMember, testUserId, null);

        actualWebRuleWordingFlagsResult.Should().BeNullOrEmpty();

        _loggerMock.VerifyLogging($"GetWordingsForWebRules is called - member bgroup {testMember.BusinessGroup}, refno {testMember.ReferenceNumber}", LogLevel.Information, Times.Once());
        _loggerMock.VerifyLogging($"webRuleWordingFlags have no value", LogLevel.Information, Times.Once());
    }

    public async Task WhenGetWordingsForWebRulesCalledWIthEmptyRuleIdOrResultInput_ThenNoFlagIsReturnedAndLoggingOccurred()
    {
        var testMember = new MemberBuilder().Build();
        var expectedWordingFlag = "HasExternalAVCs";
        var testWebRule = "DummyWebRule";
        var testWebRuleResult = "";
        var testSeparator = "=";
        var testUserId = string.Empty;

        _epaServiceClientMock.Setup(x => x.GetWebRuleResult(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Boolean>()))
            .ReturnsAsync(Option<WebRuleResultResponse>.None);

        var testWebRuleWordingFlags = new List<ContentClassifierValue> {
            new ContentClassifierValue { Key = new ContentClassifierElement {ElementType= "text", Value = expectedWordingFlag }, Value = new ContentClassifierElement { ElementType = "text", Value = $"{testWebRule}{testSeparator}{testWebRuleResult}" } }
        };

        var actualWebRuleWordingFlagsResult = await _sut.GetWordingsForWebRules(testMember, null, testWebRuleWordingFlags);

        actualWebRuleWordingFlagsResult.Should().BeNullOrEmpty();

        _loggerMock.VerifyLogging($"GetWordingsForWebRules is called - member bgroup {testMember.BusinessGroup}, refno {testMember.ReferenceNumber}", LogLevel.Information, Times.Once());
        _loggerMock.VerifyLogging($"GetWordingsForWebRules - ProcessWordingFlag - Invalid ContentClassifierValue - wordingflag {expectedWordingFlag}, rule {testWebRule} , result {testWebRuleResult}, for bgroup {testMember.BusinessGroup}, refno {testMember.ReferenceNumber}", LogLevel.Warning, Times.Once());
    }

    public async Task WhenGetWordingsForWebRulesCalledAndExceptionGetsRaised_ThenNoFlagIsReturnedAndLoggingOccurred()
    {
        var testMember = new MemberBuilder().Build();
        var testWebRule = 123;
        var testWebRuleResult = 56;
        var testSeparator = "=";
        var testUserId = string.Empty;

        _epaServiceClientMock.Setup(x => x.GetWebRuleResult(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Boolean>()))
            .Throws(new Exception("Test exception"));

        var testWebRuleWordingFlags = new List<ContentClassifierValue> {
            new ContentClassifierValue { Key = new ContentClassifierElement {ElementType= "text", Value ="HasExternalAVCs"}, Value = new ContentClassifierElement { ElementType = "text", Value = $"{testWebRule}{testSeparator}{testWebRuleResult}" } }
        };

        var actualWebRuleWordingFlagsResult = await _sut.GetWordingsForWebRules(testMember, null, testWebRuleWordingFlags);

        actualWebRuleWordingFlagsResult.Should().BeNullOrEmpty();

        _loggerMock.VerifyLogging($"GetWordingsForWebRules is called - member bgroup {testMember.BusinessGroup}, refno {testMember.ReferenceNumber}", LogLevel.Information, Times.Once());
        _loggerMock.VerifyLogging($"GetWordingsForWebRules - Failed to retrieve WebRule result for bgroup {testMember.BusinessGroup}, refno {testMember.ReferenceNumber}", LogLevel.Error, Times.Once());
    }

    [Input("AB", "DDR9", "HASDTH")]
    [Input("AC", "DDR9", "")]
    [Input("AC", "RRR", "")]
    public async Task ReturnsDeathCaseWordingFlag(string code, string caseCode, string wordingFlag)
    {
        var paperApplication1 = new PaperRetirementApplication(
            code, caseCode, "11111111", "someType", "someStatus", null, null, new BatchCreateDetails("paper"));

        var testMember = new MemberBuilder()
            .PaperRetirementApplications(paperApplication1)
            .Build();

        var deathCasesWordingFlagsResult = _sut.GetDeathCasesWordingFlag(testMember);

        if (string.IsNullOrEmpty(wordingFlag))
            deathCasesWordingFlagsResult.Should().BeEmpty();
        else
            deathCasesWordingFlagsResult.Should().Contain(wordingFlag);
    }

    public async Task GetBankAccountWordingFlagShouldNotContainNonUkBankCountry_WhenBankCountryCodeIsUk(string bankCountryCode, bool shouldContainNonUkFlag)
    {
        var member = new MemberBuilder().Build();

        var bankAccountResponse = BankAccountResponseV2Factory.Create("GB");

        _bankServiceMock
            .Setup(x => x.FetchBankAccount(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(bankAccountResponse);

        var result = await _sut.GetBankAccountWordingFlag(member);

        result.Should().BeEmpty();
    }

    [Input("FR", true)]
    [Input("", false)]
    [Input(null, false)]
    public async Task GetBankAccountWordingFlagReturnsExpectedFlag_ForBankCountryCode(string bankCountryCode, bool shouldContainNonUkFlag)
    {
        var member = new MemberBuilder().Build();

        var bankAccountResponse = BankAccountResponseV2Factory.Create(bankCountryCode);

        _bankServiceMock
            .Setup(x => x.FetchBankAccount(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(bankAccountResponse);

        var result = await _sut.GetBankAccountWordingFlag(member);

        if (shouldContainNonUkFlag)
        {
            result.Should().Contain(MdpConstants.WordingFlags.NonUkBankCountry);
        }
        else
        {
            result.Should().BeEmpty();
        }
    }
    public async Task GetBankAccountWordingFlag_ReturnsEmpty_WhenBankServiceReturnsError()
    {
        var member = new MemberBuilder().Build();

        _bankServiceMock
            .Setup(x => x.FetchBankAccount(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Error.New("Error"));

        var result = await _sut.GetBankAccountWordingFlag(member);

        result.Should().BeEmpty();
    }
    public static class BankAccountResponseV2Factory
    {
        public static BankAccountResponseV2 Create(string bankCountryCode = null)
        {
            var bankAccountResponse = new BankAccountResponseV2
            {
                BankCountryCode = bankCountryCode
            };
            return bankAccountResponse;
        }
    }

    [Input("04/05/1971", NMPA.Pre)]
    [Input("04/06/1971", NMPA.Current)]
    [Input("04/06/1972", NMPA.Current)]
    [Input("04/06/1973", NMPA.Post)]
    [Input("04/06/1975", NMPA.Post)]
    public void ReturnsGetNmpaFlagsGetNmpaFlags(string dob, string wordingFlag)
    {
        var date = string.IsNullOrEmpty(dob) ? (DateTimeOffset?)null : DateTime.Parse(dob);
        var testMember = new MemberBuilder()
            .DateOfBirth(date)
            .Build();

        var deathCasesWordingFlagsResult = _sut.GetNmpaFlags(testMember.PersonalDetails.DateOfBirth);
        deathCasesWordingFlagsResult.Should().Contain(wordingFlag);
    }
}