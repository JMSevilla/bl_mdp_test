using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Options;
using Moq;
using WTW.MdpService.Domain.Mdp.Calculations;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.MdpService.Infrastructure.EpaService;
using WTW.MdpService.Test.Domain.Mdp.Calculations;
using WTW.MdpService.Test.Domain.Members;
using WTW.TestCommon.FixieConfig;
using WTW.Web.Serialization;

namespace WTW.MdpService.Test.Infrastructure.Calculations;

public class CalculationsParserTest
{
    private CalculationsParser _sut;
    protected Mock<IOptionsSnapshot<CalculationServiceOptions>> _optionsMock;
    protected readonly Mock<IEpaServiceClient> _epaServiceClientMock;

    public CalculationsParserTest()
    {
        _optionsMock = new Mock<IOptionsSnapshot<CalculationServiceOptions>>();
        _optionsMock.Setup(m => m.Value).Returns(new CalculationServiceOptions
        {
            BaseUrl = "https://calcapi-dev.awstas.net/",
            CacheExpiresInMs = 10,
            TimeOutInSeconds = 30000,
            GuaranteedQuotesEnabledFor = new List<string>() { "RBS", "JPM" },
            GetGuaranteedQuotesApiPath = "bgroups/{0}/members/{1}/quotations?guaranteeDateFrom={2}&guaranteeDateTo={3}&event={4}&status={5}&pageNumber={6}&pageSize={7}"
        });

        _epaServiceClientMock = new Mock<IEpaServiceClient>();
        _sut = new CalculationsParser(_optionsMock.Object, _epaServiceClientMock.Object);
    }

    public void GetRetirementJsonFromRetirementResponse()
    {
        var result = _sut.GetRetirementJson(JsonSerializer.Deserialize<RetirementResponse>(TestData.RetirementResponseJson, SerialiationBuilder.Options()), "PO");

        result.Should().NotBeNullOrWhiteSpace();
        result.Should().StartWith("{\"calculationEventType\":\"PO\"");
    }

    public void GetRetirementJsonV2AndMdpFromRetirementResponseV2AndEventType()
    {
        var response = JsonSerializer.Deserialize<RetirementResponseV2>(TestData.RetirementV2ResponseJson, SerialiationBuilder.Options());
        var (retirementV2, mdp) = _sut.GetRetirementJsonV2(response, "PV");

        retirementV2.Should().NotBeNullOrWhiteSpace();
        retirementV2.Should().StartWith("{\"calculationEventType\":\"PV\"");
        mdp.Should().NotBeNullOrWhiteSpace();
        mdp.Should().StartWith("{\"options\":{\"fullPension\":{\"attributes\":{\"pensionTranches\":{\"post88GMP\":1604.2");
    }

    public void GetRetirementDatesAgesJsonFromRetirementDatesAgesResponse()
    {
        var response = JsonSerializer.Deserialize<RetirementDatesAgesResponse>(TestData.RetitementDateAgesResponseV2, SerialiationBuilder.Options());
        var result = _sut.GetRetirementDatesAgesJson(response);

        result.Should().NotBeNullOrWhiteSpace();
        result.Should().StartWith("{\"earliestRetirementAge\":55,\"normalMinimumPensionAge\":null,\"latestRetirementAge\":75,\"normalRetirementAge\":60");
    }

    public void GetRetirementDatesAgesJsonFromTransferResponse()
    {
        var response = JsonSerializer.Deserialize<TransferResponse>(TestData.TransferQuoteResponseJson, SerialiationBuilder.Options());
        var result = _sut.GetTransferQuoteJson(response);

        result.Should().NotBeNullOrWhiteSpace();
        result.Should().StartWith($"{{\"guaranteeDate\":\"{DateTimeOffset.Parse("2023-02-16").ToString("yyyy-MM-ddTHH:mm:sszzz")}\",\"guaranteePeriod\":\"P3M\",\"replyByDate\":\"{DateTimeOffset.Parse("2023-01-26").ToString("yyyy-MM-ddTHH:mm:sszzz")}\",");
    }

    public void GetsRetirementFromJson()
    {
        var result = _sut.GetRetirement(TestData.RetiremntJson);

        result.Should().NotBeNull();
        result.CalculationEventType.Should().Be("PV");
    }

    public void GetsRetirementV2FromJson()
    {
        var result = _sut.GetRetirementV2(TestData.RetirementJsonV2);

        result.Should().NotBeNull();
        result.CalculationEventType.Should().Be("PV");
    }

    public void GetsRetirementV1OrV2FromJsonWhenRetirementJsonV2Null()
    {
        var result = _sut.GetRetirementV1OrV2(TestData.RetiremntJson, null);

        result.retirementV1.Should().NotBeNull();
        result.retirementV2.Should().BeNull();
    }

    public void GetsRetirementV1OrV2FromJsonWhenRetirementJsonV1Null()
    {
        var result = _sut.GetRetirementV1OrV2(null, TestData.RetirementJsonV2);

        result.retirementV2.Should().NotBeNull();
        result.retirementV1.Should().BeNull();
    }

    public void GetsQuotesV2FromJson()
    {
        var result = _sut.GetQuotesV2(TestData.RetirementQuotesJsonV2);

        result.TotalPensionableService.Should().Be("P23Y6M");
        result.ResidualFundValue.Should().BeNull();
    }

    public void GetsRetirementDatesAgesFromJson()
    {
        var result = _sut.GetRetirementDatesAges(TestData.RetirementDatesAgesJsonNew);

        result.EarliestRetirementAge.Should().Be(55);
    }

    public void GetsTransferQuoteFromJson()
    {
        var result = _sut.GetTransferQuote(TestData.TransferQuoteJson);

        result.MaximumResidualPension.Should().Be(17910.46M);
    }

    public void GetsRetirementJsonV2FromRetirementV2()
    {
        var result = _sut.GetRetirementJsonV2FromRetirementV2(new RetirementV2(new RetirementV2Params()));

        result.Should().StartWith("{\"calculationEventType\":null");
    }

    [Input("RBS", true)]
    [Input("HSB", false)]
    [Input("JPM", true)]
    public void IsGuaranteedQuoteEnabled_ReturnsExpectedValue(string bgroup, bool expectedResult)
    {
        var result = _sut.IsGuaranteedQuoteEnabled(bgroup);
        result.Should().Be(expectedResult);
    }
    [Input("1", true)]
    [Input("", false)]
    public async Task IsMemberGMPONly_ReturnsExpectedValue(string webruleResult, bool expectedResult)
    {
        _epaServiceClientMock.Setup(x => x.GetWebRuleResult(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Boolean>()))
                    .ReturnsAsync(Option<WebRuleResultResponse>.Some(new WebRuleResultResponse { Result = webruleResult }));

        var _stubMember = new MemberBuilder().Build();

        var result = await _sut.IsMemberGMPONly(_stubMember, It.IsAny<string>());

        result.Should().Be(expectedResult);
    }

    [Input("JSY", "JSY")]
    [Input("GSY", "GSY")]
    [Input("IOM", "IOM")]
    [Input(null, null)]
    [Input("", "")]
    public async Task GetMemberJuridiction_ReturnsExpectedValue(string webruleResult, string expectedResult)
    {
        _epaServiceClientMock.Setup(x => x.GetWebRuleResult(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Boolean>()))
                    .ReturnsAsync(Option<WebRuleResultResponse>.Some(new WebRuleResultResponse { Result = webruleResult }));

        var _stubMember = new MemberBuilder().Build();

        var result = await _sut.GetMemberJuridiction(_stubMember, It.IsAny<string>());

        result.Should().Be(expectedResult);
    }

    [Input(true, "", "M", "1962-01-01", 65, 75)]
    [Input(true, "", "F", "1966-01-01", 60, 75)]
    [Input(false, "JSY", "M", "1971-01-01", 55, 75)]
    [Input(false, "IOM", "M", "1970-01-01", 55, 70)]
    [Input(false, "GSY", "M", "1978-08-01", 55, 75)]
    public void EvaluateDateRangeForGMPOrCrownDependencyMember_ReturnsExpectedValue(bool GMPOnlyMember, string memberJuridiction, string gender, string dob, int minPensionAge, int maxPensionAge)
    {
        var _stubDob = DateTime.Parse(dob);

        var _stubMember = new MemberBuilder().Gender(gender).DateOfBirth(_stubDob).Build();

        var _stubFromDate = _stubMember.PersonalDetails.DateOfBirth.Value.AddYears(minPensionAge);
        var _stubToDate = _stubMember.PersonalDetails.DateOfBirth.Value.AddYears(maxPensionAge);


        var expectedFromDate = _stubMember.PersonalDetails.DateOfBirth.Value.AddYears(minPensionAge).DateTime > DateTime.UtcNow ? _stubMember.PersonalDetails.DateOfBirth.Value.AddYears(minPensionAge).DateTime : DateTime.UtcNow.AddDays(1);

        var expectedToDate = _stubMember.PersonalDetails.DateOfBirth.Value.AddYears(maxPensionAge).DateTime;

        var result = _sut.EvaluateDateRangeForGMPOrCrownDependencyMember(_stubMember, GMPOnlyMember, memberJuridiction, _stubFromDate.DateTime, _stubToDate.DateTime);

        result.Item1.ToString("yyyy/MM/dd").Should().Be(expectedFromDate.ToString("yyyy/MM/dd"));
        result.Item2.ToString("yyyy/MM/dd").Should().Be(expectedToDate.ToString("yyyy/MM/dd"));

    }

    public void GetCalculationFactorDate_ReturnsValue()
    {
        var result = _sut.GetCalculationFactorDate(TestData.RetirementQuotesJsonV2);

        result.Should().NotBeNull();
    }

    public void GetGetGuaranteedQuoteDetailWithValidData_ReturnsValue()
    {
        var _stubRetirementResponseV2 = new RetirementResponseV2
        {
            Results = new ResultsResponse { Quotation = new QuotationResponse { Guaranteed = true, ExpiryDate = DateTime.Now } }
        };
        var result = _sut.GetGuaranteedQuoteDetail(_stubRetirementResponseV2);

        result.Item1.Should().BeTrue();
        result.Item2.Value.ToString().Should().NotBeNullOrEmpty();
    }

    public void GetGetGuaranteedQuoteDetailWithInValidData_ReturnsValue()
    {
        var _stubRetirementResponseV2 = new RetirementResponseV2();

        var result = _sut.GetGuaranteedQuoteDetail(_stubRetirementResponseV2);

        result.Item1.Should().BeFalse();
        result.Item2.Should().BeNull();
    }
}