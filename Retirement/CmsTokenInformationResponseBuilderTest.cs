using System;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using LanguageExt.Common;
using WTW.MdpService.Domain.Mdp.Calculations;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.MdpService.Retirement;
using WTW.MdpService.Test.Domain.Mdp.Calculations;
using WTW.MdpService.Test.Domain.Members;
using WTW.Web.LanguageExt;
using WTW.Web.Serialization;

namespace WTW.MdpService.Test.Retirement;

public class CmsTokenInformationResponseBuilderTest
{
    public void CmsTokenInformationResponseBuilder_WithRetirementDatesAges()
    {
        var now = DateTime.Now;
        var datesResponse = JsonSerializer.Deserialize<RetirementDatesAgesResponse>(TestData.RetitementDateAgesResponse, SerialiationBuilder.Options());
        var retirementDatesAges = new MdpService.Domain.Mdp.Calculations.RetirementDatesAges(datesResponse);

        var sut = new CmsTokenInformationResponseBuilder()
        .WithRetirementDatesAges(retirementDatesAges, now, "1", now)
        .Build();

        sut.NormalRetirementDate.Should().Be(retirementDatesAges.NormalRetirementDate);
        sut.TargetRetirementDate.Should().Be(retirementDatesAges.TargetRetirementDate);
        sut.EarliestRetirementAge.Should().Be(retirementDatesAges.EarliestRetirement());
        sut.EarliestRetirementDate.Should().Be(retirementDatesAges.EarliestRetirementDate);
        sut.LatestRetirementAge.Should().Be(retirementDatesAges.GetLatestRetirementAge());
        sut.AgeAtNormalRetirementIso.Should().Be(retirementDatesAges.AgeAtNormalRetirementIso);
        sut.SelectedRetirementDate.Should().Be(now);
        sut.PensionPaymentDay.Should().Be("1");
        sut.MemberNormalRetirementDate.Should().Be(sut.NormalRetirementDate);
    }

    public void CmsTokenInformationResponseBuilder_WithRetirementTime()
    {
        var datesResponse = JsonSerializer.Deserialize<RetirementDatesAgesResponse>(TestData.RetitementDateAgesResponse, SerialiationBuilder.Options());
        var retirementDatesAges = new MdpService.Domain.Mdp.Calculations.RetirementDatesAges(datesResponse);

        var sut = new CmsTokenInformationResponseBuilder()
        .WithRetirementDatesAges(retirementDatesAges, DateTime.Now, "1", DateTimeOffset.Now)
        .WithRetirementTime("1", "2")
        .Build();

        sut.TimeToTargetRetirementIso.Should().Be("1");
        sut.TimeToNormalRetirementIso.Should().Be("2");
    }

    public void CmsTokenInformationResponseBuilder_WithRetirementV2Data()
    {
        var retirement = new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options()));
        var totalLtaPercentage = retirement.QuotesV2.FirstOrDefault(x => x.Name == "fullPension")?.Attributes.FirstOrDefault(x => x.Name == "totalLTAUsedPerc")?.Value;

        var sut = new CmsTokenInformationResponseBuilder()
        .CalculationSuccessful(true)
        .WithRetirementV2Data(retirement, "fullPension", "RBS", "DB")
        .Build();

        sut.ChosenLtaPercentage.Should().Be(totalLtaPercentage);
        sut.RemainingLtaPercentage.Should().Be(retirement.TotalLtaRemainingPercentage);
        sut.LtaLimit.Should().Be(retirement.StandardLifetimeAllowance);
        sut.GmpAgeYears.Should().Be(retirement.GMPAgeYears());
        sut.GmpAgeMonths.Should().Be(retirement.GMPAgeMonths());
        sut.TotalPension.Should().Be(retirement.TotalPension());
        sut.TotalAVCFund.Should().Be(retirement.TotalAVCFund());
    }

    public void CmsTokenInformationResponseBuilder_WithMemberData()
    {
        var now = DateTime.Now;
        var member = new MemberBuilder().Build();

        var sut = new CmsTokenInformationResponseBuilder()
        .WithMemberData(member, now, DateTimeOffset.Now)
        .Build();

        sut.Name.Should().Be(member.PersonalDetails.Forenames);
        sut.SelectedRetirementAge.Should().Be(member.AgeOnSelectedDate(now));
        sut.Email.Should().Be(member.Email().SingleOrDefault());
        sut.PhoneNumber.Should().Be(member.FullMobilePhoneNumber().SingleOrDefault());
        sut.DateOfBirth.Should().Be(member.PersonalDetails.DateOfBirth);
        sut.AgeAtSelectedRetirementDateIso.Should().NotBeNull();
    }

    public void CmsTokenInformationResponseBuilder_WithTransferQuoteData()
    {
        var transferQuote = new TransferQuote(JsonSerializer.Deserialize<TransferQuoteDto>(TestData.TransferQuoteJson, SerialiationBuilder.Options()));

        var sut = new CmsTokenInformationResponseBuilder()
        .WithTransferQuoteData(transferQuote)
        .Build();

        sut.TransferReplyByDate.Should().Be(transferQuote.ReplyByDate);
        sut.TransferGuaranteeExpiryDate.Should().Be(transferQuote.GuaranteeDate);
        sut.TransferQuoteRunDate.Should().Be(transferQuote.OriginalEffectiveDate);
    }

    public void CmsTokenInformationResponseBuilder_WithDirectRetirementDatesAgesResponseFromApi()
    {
        var retirementDatesAgesResponse = new Result<RetirementDatesAgesResponse>(
            JsonSerializer.Deserialize<RetirementDatesAgesResponse>(TestData.RetitementDateAgesResponse, SerialiationBuilder.Options()));

        var sut = new CmsTokenInformationResponseBuilder()
        .WithDirectRetirementDatesAgesResponseFromApi(retirementDatesAgesResponse)
        .Build();

        sut.NormalRetirementDate.Should().Be(retirementDatesAgesResponse.Value().RetirementDates.NormalRetirementDate);
        sut.TargetRetirementDate.Should().Be(retirementDatesAgesResponse.Value().RetirementDates.TargetRetirementDate);
        sut.AgeAtNormalRetirementIso.Should().Be(retirementDatesAgesResponse.Value().RetirementAges.AgeAtNormalRetirementIso);
        sut.MemberNormalRetirementDate.Should().Be(sut.NormalRetirementDate);
    }
}
