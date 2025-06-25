using System;
using FluentAssertions;
using WTW.MdpService.Domain.Mdp;
using WTW.MdpService.Test.Domain.Members;
using WTW.TestCommon.FixieConfig;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Domain.Mdp.Calculations;

public class CalculationTest
{
    public void CanCreateCalculation()
    {
        var now = DateTimeOffset.UtcNow;

        var sut = new CalculationBuilder()
            .EffectiveRetirementDate(now.AddDays(60).Date)
            .CurrentDate(now)
            .BuildV1();

        sut.BusinessGroup.Should().Be("RBS");
        sut.ReferenceNumber.Should().Be("0003994");
        sut.RetirementDatesAgesJson.Should().Be(TestData.RetiremntDatesAgesJson);
        sut.RetirementJson.Should().Be(TestData.RetiremntJson);
        sut.RetirementJsonV2.Should().BeNull();
        sut.QuotesJsonV2.Should().BeNull();
        sut.EffectiveRetirementDate.Should().Be(now.AddDays(60).Date);
        sut.SelectedQuoteName.Should().BeNull();
        sut.RetirementJourney.Should().BeNull();
        sut.EnteredLumpSum.Should().BeNull();
        sut.CreatedAt.Should().Be(now);
        sut.UpdatedAt.Should().BeNull();
        sut.IsCalculationSuccessful.Should().BeTrue();
    }

    public void CanCreateCalculation2()
    {
        var now = DateTimeOffset.UtcNow;

        var sut = new CalculationBuilder()
            .EffectiveRetirementDate(now.AddDays(60).Date)
            .CurrentDate(now)
            .BuildV2();

        sut.BusinessGroup.Should().Be("RBS");
        sut.ReferenceNumber.Should().Be("0003994");
        sut.RetirementDatesAgesJson.Should().Be(TestData.RetiremntDatesAgesJson);
        sut.RetirementJson.Should().BeNull();
        sut.RetirementJsonV2.Should().Be(TestData.RetirementJsonV2);
        sut.QuotesJsonV2.Should().Be(TestData.TransferQuoteJson);
        sut.EffectiveRetirementDate.Should().Be(now.AddDays(60).Date);
        sut.SelectedQuoteName.Should().BeNull();
        sut.RetirementJourney.Should().BeNull();
        sut.EnteredLumpSum.Should().BeNull();
        sut.CreatedAt.Should().Be(now);
        sut.UpdatedAt.Should().BeNull();
    }

    public void CanUpdateRetirementDatesAgesJson()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = new CalculationBuilder()
            .EffectiveRetirementDate(now.AddDays(60).Date)
            .CurrentDate(now)
            .RetirementDatesAgesJson(TestData.RetiremntDatesAgesJson)
            .BuildV2();

        sut.UpdateRetirementDatesAgesJson(TestData.RetirementDatesAgesJsonNew);

        sut.RetirementDatesAgesJson.Should().NotBe(TestData.RetiremntDatesAgesJson);
        sut.RetirementDatesAgesJson.Should().Be(TestData.RetirementDatesAgesJsonNew);
    }

    public void CanUpdateRetirement()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = new CalculationBuilder()
            .EffectiveRetirementDate(now.AddDays(60).Date)
            .CurrentDate(now)
            .BuildV1();
        sut.UpdateRetirement(TestData.RetiremntJsonNew, now.AddDays(90).Date, now.AddDays(1));

        sut.RetirementJson.Should().NotBe(TestData.RetiremntJson);
        sut.RetirementJson.Should().Be(TestData.RetiremntJsonNew);
        sut.EffectiveRetirementDate.Should().NotBe(now.AddDays(60).Date);
        sut.EffectiveRetirementDate.Should().Be(now.AddDays(90).Date);
        sut.CreatedAt.Should().Be(now);
        sut.UpdatedAt.Should().Be(now.AddDays(1));
    }

    public void CanUpdateRetirementV2()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = new CalculationBuilder()
            .EffectiveRetirementDate(now.AddDays(60).Date)
            .CurrentDate(now)
            .BuildV2();
        sut.UpdateRetirementV2(TestData.RetiremntJsonNew, "{}", now.AddDays(90).Date, now.AddDays(1));

        sut.RetirementJson.Should().BeNull();
        sut.RetirementJsonV2.Should().NotBe(TestData.RetirementV2ResponseJson);
        sut.RetirementJsonV2.Should().Be(TestData.RetiremntJsonNew);
        sut.QuotesJsonV2.Should().NotBe(TestData.RetiremntJsonNew);
        sut.QuotesJsonV2.Should().Be("{}");
        sut.EffectiveRetirementDate.Should().NotBe(now.AddDays(60).Date);
        sut.EffectiveRetirementDate.Should().Be(now.AddDays(90).Date);
        sut.CreatedAt.Should().Be(now);
        sut.UpdatedAt.Should().Be(now.AddDays(1));
    }

    public void CanUpdateRetirementJson()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = new CalculationBuilder()
            .EffectiveRetirementDate(now.AddDays(60).Date)
            .CurrentDate(now)
            .BuildV1();
        sut.UpdateRetirementJson(TestData.RetiremntJsonNew);

        sut.RetirementJson.Should().NotBe(TestData.RetiremntJson);
        sut.RetirementJson.Should().Be(TestData.RetiremntJsonNew);
        sut.EffectiveRetirementDate.Should().Be(now.AddDays(60).Date);
        sut.CreatedAt.Should().Be(now);
        sut.UpdatedAt.Should().BeNull();
    }

    public void CanUpdateRetirementJsonV2()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = new CalculationBuilder()
            .EffectiveRetirementDate(now.AddDays(60).Date)
            .CurrentDate(now)
            .BuildV2();
        sut.UpdateRetirementJsonV2(TestData.RetirementJsonV2New);

        sut.RetirementJsonV2.Should().NotBe(TestData.RetirementJsonV2);
        sut.RetirementJsonV2.Should().Be(TestData.RetirementJsonV2New);
    }

    public void CanSetRetirementJourney()
    {
        var now = DateTimeOffset.UtcNow;
        var utcNow = DateTimeOffset.UtcNow;
        var quote = MemberQuote.Create(utcNow,
            "FullPension", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
            18, 19, 20, 21, 22, false, 85, 55,
            65, utcNow, utcNow, utcNow, "15", "16", 17, "PV", "SPD", 1, 9);
        var journey = new RetirementJourney("0003994", "RBS", utcNow, "current", "next", quote.Right(), 7, 37);
        var sut = new CalculationBuilder()
            .EffectiveRetirementDate(now.AddDays(60).Date)
            .CurrentDate(now)
            .BuildV1();
        sut.SetJourney(journey, "FullPension");

        sut.RetirementJourney.Should().BeEquivalentTo(journey);
        sut.SelectedQuoteName.Should().Be("FullPension");
    }

    public void RetirementApplicationStartDateRangWhenSelectedDateInRange()
    {
        var now = new DateTimeOffset(new DateTime(2022, 11, 15), TimeSpan.FromHours(0));
        var effectiveRetirementDate = new DateTime(2022, 09, 10);
        var sut = new CalculationBuilder()
            .EffectiveRetirementDate(effectiveRetirementDate.Date)
            .CurrentDate(now)
            .BuildV1();
        var range = sut.RetirementApplicationStartDateRange(now);

        range.EarliestStartRaDateForSelectedDate.Should().BeNull();
        range.LatestStartRaDateForSelectedDate.Should().Be(effectiveRetirementDate.AddDays(-WTW.MdpService.Retirement.RetirementConstants.RetirementProcessingPeriodInDays));
    }

    public void RetirementApplicationStartDateRangWhenSelectedDateOutOfRange()
    {
        var now = new DateTimeOffset(new DateTime(2022, 05, 11), TimeSpan.FromHours(0));
        var effectiveRetirementDate = new DateTime(2022, 11, 12);
        var sut = new CalculationBuilder()
            .EffectiveRetirementDate(effectiveRetirementDate.Date)
            .CurrentDate(now)
            .BuildV1();
        var range = sut.RetirementApplicationStartDateRange(now);

        range.EarliestStartRaDateForSelectedDate.Should().Be(effectiveRetirementDate.AddMonths(-WTW.MdpService.Retirement.RetirementConstants.RetirementApplicationPeriodInMonths));
        range.LatestStartRaDateForSelectedDate.Should().Be(effectiveRetirementDate.AddDays(-WTW.MdpService.Retirement.RetirementConstants.RetirementProcessingPeriodInDays));
    }

    public void ReturnsRetirementConfirmationDate()
    {
        var now = new DateTimeOffset(new DateTime(2022, 05, 11), TimeSpan.FromHours(0));
        var effectiveRetirementDate = new DateTime(2022, 11, 12);
        var sut = new CalculationBuilder()
            .EffectiveRetirementDate(effectiveRetirementDate.Date)
            .CurrentDate(now)
            .BuildV1();

        var date = sut.RetirementConfirmationDate();

        date.Should().Be(effectiveRetirementDate.AddDays(-WTW.MdpService.Retirement.RetirementConstants.RetirementConfirmationInDays));
    }

    public void ReturnsCorrectRetirementDateForDcUsers()
    {
        var member = new MemberBuilder()
            .SchemeType("DC")
            .DateOfBirth(new DateTimeOffset(1960, 1, 2, 0, 0, 0, TimeSpan.FromHours(0)))
            .Build();
        var now = new DateTimeOffset(new DateTime(2022, 05, 11), TimeSpan.FromHours(0));
        var effectiveRetirementDate = now.AddDays(58);
        var sut = new CalculationBuilder()
            .EffectiveRetirementDate(effectiveRetirementDate.Date)
            .CurrentDate(now)
            .BuildV1();

        var date = sut.RetirementDateWithAppliedRetirementProcessingPeriod(37, member.Scheme?.Type, now);

        date.Should().Be(effectiveRetirementDate.DateTime);
    }

    public void ReturnsCorrectRetirementDateForRbsUsersWithoutRetirementJourney()
    {
        var member = new MemberBuilder()
            .SchemeType("DB")
            .BusinessGroup("RBS")
            .DateOfBirth(new DateTimeOffset(1960, 1, 2, 0, 0, 0, TimeSpan.FromHours(0)))
            .Build();
        var now = new DateTimeOffset(new DateTime(2022, 05, 11), TimeSpan.FromHours(0));
        var effectiveRetirementDate = now.AddDays(58);
        var sut = new CalculationBuilder()
            .SetBusinessGroup("RBS")
            .EffectiveRetirementDate(effectiveRetirementDate.Date)
            .CurrentDate(now)
            .BuildV1();

        var date = sut.RetirementDateWithAppliedRetirementProcessingPeriod(37, member.Scheme?.Type, now);

        date.Should().Be(effectiveRetirementDate.DateTime);
    }


    public void ReturnsCorrectRetirementDateWithAppliedRetirementProcessingPeriod_WhenRetirementJourneyIsNotStartedAndEffectiveDateIsNotInThePast()
    {
        var member = new MemberBuilder()
            .SchemeType("DB")
            .Build();
        var now = new DateTimeOffset(new DateTime(2022, 05, 11), TimeSpan.FromHours(0));
        var effectiveRetirementDate = now.AddDays(58);
        var sut = new CalculationBuilder()
            .SetBusinessGroup("BCL")
            .EffectiveRetirementDate(effectiveRetirementDate.Date)
            .CurrentDate(now)
            .BuildV1();

        var date = sut.RetirementDateWithAppliedRetirementProcessingPeriod(37, member.Scheme?.Type, now.AddDays(10));

        date.Should().Be(effectiveRetirementDate.Date);
    }

    public void ReturnsCorrectRetirementDateWithAppliedRetirementProcessingPeriod_WhenRetirementJourneyIsNotStartedAndEffectiveDateIsInThePast()
    {
        var member = new MemberBuilder()
            .SchemeType("DB")
            .Build();
        var now = new DateTimeOffset(new DateTime(2022, 05, 11), TimeSpan.FromHours(0));
        var effectiveRetirementDate = now.AddDays(37);
        var sut = new CalculationBuilder()
            .SetBusinessGroup("BCL")
            .EffectiveRetirementDate(effectiveRetirementDate.Date)
            .CurrentDate(now)
            .BuildV1();

        var date = sut.RetirementDateWithAppliedRetirementProcessingPeriod(37, member.Scheme?.Type, now.AddDays(38));

        date.Should().Be(now.AddDays(38).Date.AddDays(37));
    }

    public void ReturnsCorrectRetirementDateWithAppliedRetirementProcessingPeriod_WhenRetirementJourneyIsStarted()
    {
        var member = new MemberBuilder()
            .SchemeType("DB")
            .Build();
        var utcNow = new DateTimeOffset(new DateTime(2022, 05, 11), TimeSpan.FromHours(0));
        var quote = MemberQuote.Create(utcNow,
            "FullPension", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
            18, 19, 20, 21, 22, false, 85, 55,
            65, utcNow, utcNow, utcNow, "15", "16", 17, "PV", "SPD", 1, 9);
        var effectiveDate = utcNow.AddDays(37).Date;
        var journey = new RetirementJourney("0003994", "RBS", utcNow, "current", "next", quote.Right(), 7, 37);
        var sut = new CalculationBuilder()
            .EffectiveRetirementDate(effectiveDate)
            .CurrentDate(utcNow)
            .BuildV1();
        ;
        sut.SetJourney(journey, "FullPension");

        var date = sut.RetirementDateWithAppliedRetirementProcessingPeriod(37, member.Scheme?.Type, utcNow.AddDays(5));

        date.Should().Be(effectiveDate);
    }

    public void ReturnsCorrectRetirementDateWithAppliedRetirementProcessingPeriod_WhenRetirementJourneyIsExpired()
    {
        var member = new MemberBuilder()
            .SchemeType("DB")
            .Build();
        var utcNow = new DateTimeOffset(new DateTime(2022, 05, 11), TimeSpan.FromHours(0));
        var quote = MemberQuote.Create(utcNow,
            "FullPension", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
            18, 19, 20, 21, 22, false, 85, 55,
            65, utcNow, utcNow, utcNow, "15", "16", 17, "PV", "SPD", 1, 9);
        var effectiveDate = utcNow.AddDays(37).Date;
        var journey = new RetirementJourney("0003994", "BCL", utcNow, "current", "next", quote.Right(), 7, 37);
        var sut = new CalculationBuilder()
            .EffectiveRetirementDate(effectiveDate)
            .CurrentDate(utcNow)
            .BuildV1();
        sut.SetJourney(journey, "FullPension");

        var date = sut.RetirementDateWithAppliedRetirementProcessingPeriod(37, member.Scheme?.Type, utcNow.AddDays(15));

        date.Should().Be(utcNow.AddDays(15).AddDays(37).Date);
    }

    public void UpdatesEffectiveDate()
    {
        var now = new DateTimeOffset(new DateTime(2022, 05, 11), TimeSpan.FromHours(0));
        var effectiveRetirementDate = new DateTime(2022, 7, 12);
        var sut = new CalculationBuilder()
            .EffectiveRetirementDate(effectiveRetirementDate)
            .CurrentDate(now)
            .BuildV1();
        sut.EffectiveRetirementDate.Should().Be(effectiveRetirementDate);

        sut.UpdateEffectiveDate(new DateTime(2022, 11, 12));

        sut.EffectiveRetirementDate.Should().Be(new DateTime(2022, 11, 12));
    }

    public void ReturnsRetirementJourneyStartStatusFalse_WhenRAIsNotStarted()
    {
        var now = DateTimeOffset.UtcNow;
        var utcNow = DateTimeOffset.UtcNow;
        var quote = MemberQuote.Create(utcNow,
            "FullPension", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
            18, 19, 20, 21, 22, false, 85, 55,
            65, utcNow, utcNow, utcNow, "15", "16", 17, "PV", "SPD", 1, 9);
        var journey = new RetirementJourney("0003994", "RBS", utcNow, "current", "next", quote.Right(), 7, 37);
        var sut = new CalculationBuilder()
            .EffectiveRetirementDate(now.AddDays(60).Date)
            .CurrentDate(now)
            .BuildV1();
        //sut.SetJourney(journey, "FullPension");

        var result = sut.HasRetirementJourneyStarted();

        result.Should().BeFalse();
    }

    public void ReturnsRetirementJourneyStartStatusTrue_WhenRAIsStarted()
    {
        var now = DateTimeOffset.UtcNow;
        var utcNow = DateTimeOffset.UtcNow;
        var quote = MemberQuote.Create(utcNow,
            "FullPension", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
            18, 19, 20, 21, 22, false, 85, 55,
            65, utcNow, utcNow, utcNow, "15", "16", 17, "PV", "SPD", 1, 9);
        var journey = new RetirementJourney("0003994", "RBS", utcNow, "current", "next", quote.Right(), 7, 37);
        var sut = new CalculationBuilder()
            .EffectiveRetirementDate(now.AddDays(60).Date)
            .CurrentDate(now)
            .BuildV1();

        sut.SetJourney(journey, "FullPension");

        var result = sut.HasRetirementJourneyStarted();

        result.Should().BeTrue();
    }

    public void ReturnsRetirementJourneySubmitStatusFalse_WhenRAIsNotSubmitted()
    {
        var now = DateTimeOffset.UtcNow;
        var utcNow = DateTimeOffset.UtcNow;
        var quote = MemberQuote.Create(utcNow,
            "FullPension", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
            18, 19, 20, 21, 22, false, 85, 55,
            65, utcNow, utcNow, utcNow, "15", "16", 17, "PV", "SPD", 1, 9);
        var journey = new RetirementJourney("0003994", "RBS", utcNow, "current", "next", quote.Right(), 7, 37);
        var sut = new CalculationBuilder()
            .EffectiveRetirementDate(now.AddDays(60).Date)
            .CurrentDate(now)
            .BuildV1();
        sut.SetJourney(journey, "FullPension");

        var result = sut.IsRetirementJourneySubmitted();

        result.Should().BeFalse();
    }

    public void ReturnsRetirementJourneySubmitStatusTrue_WhenRAIsSubmitted()
    {
        var now = DateTimeOffset.UtcNow;
        var utcNow = DateTimeOffset.UtcNow;
        var quote = MemberQuote.Create(utcNow,
            "FullPension", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
            18, 19, 20, 21, 22, false, 85, 55,
            65, utcNow, utcNow, utcNow, "15", "16", 17, "PV", "SPD", 1, 9);
        var journey = new RetirementJourney("0003994", "RBS", utcNow, "current", "next", quote.Right(), 7, 37);
        var sut = new CalculationBuilder()
            .EffectiveRetirementDate(now.AddDays(60).Date)
            .CurrentDate(now)
            .BuildV1();
        sut.SetJourney(journey, "FullPension");
        sut.RetirementJourney.Submit(new byte[5], now.AddDays(1), "12345");

        var result = sut.IsRetirementJourneySubmitted();

        result.Should().BeTrue();
    }

    public void ReturnsRetirementJourneyExpireStatusFalse_WhenRAIsNotExpired()
    {
        var now = DateTimeOffset.UtcNow;
        var utcNow = DateTimeOffset.UtcNow;
        var quote = MemberQuote.Create(utcNow.AddDays(40),
            "FullPension", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
            18, 19, 20, 21, 22, false, 85, 55,
            65, utcNow, utcNow, utcNow, "15", "16", 17, "PV", "SPD", 1, 9);
        var journey = new RetirementJourney("0003994", "RBS", utcNow, "current", "next", quote.Right(), 3, 37);
        var sut = new CalculationBuilder()
            .EffectiveRetirementDate(now.AddDays(40).Date)
            .CurrentDate(now)
            .BuildV1();
        sut.SetJourney(journey, "FullPension");

        var result = sut.HasRetirementJourneyExpired(utcNow.AddDays(1));

        result.Should().BeFalse();
    }

    public void ReturnsRetirementJourneyExpireStatusTrue_WhenRAIsExpired()
    {
        var now = DateTimeOffset.UtcNow;
        var utcNow = DateTimeOffset.UtcNow;
        var quote = MemberQuote.Create(utcNow,
            "FullPension", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
            18, 19, 20, 21, 22, false, 85, 55,
            65, utcNow, utcNow, utcNow, "15", "16", 17, "PV", "SPD", 1, 9);
        var journey = new RetirementJourney("0003994", "RBS", utcNow, "current", "next", quote.Right(), 7, 37);
        var sut = new CalculationBuilder()
            .EffectiveRetirementDate(now.AddDays(60).Date)
            .CurrentDate(now)
            .BuildV1();
        sut.SetJourney(journey, "FullPension");

        var result = sut.HasRetirementJourneyExpired(utcNow.AddMonths(8));

        result.Should().BeTrue();
    }

    public void CanSetLumpSum()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = new CalculationBuilder()
            .EffectiveRetirementDate(now.AddDays(60).Date)
            .CurrentDate(now)
            .BuildV1();

        sut.SetEnteredLumpSum(20.2m);

        sut.EnteredLumpSum.Should().Be(20.2m);
    }

    public void CanClearLumpSum()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = new CalculationBuilder()
            .EffectiveRetirementDate(now.AddDays(60).Date)
            .CurrentDate(now)
            .BuildV1();
        sut.SetEnteredLumpSum(20.2m);
        sut.EnteredLumpSum.Should().Be(20.2m);

        sut.ClearLumpSum();

        sut.EnteredLumpSum.Should().BeNull();
    }

    public void ReturnsCorrectExpectedRetirementJourneyExpirationDate()
    {
        var now = DateTimeOffset.UtcNow;
        var utcNow = DateTimeOffset.UtcNow;
        var quote = MemberQuote.Create(utcNow.AddDays(140),
            "FullPension", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
            18, 19, 20, 21, 22, false, 85, 55,
            65, utcNow, utcNow, utcNow, "15", "16", 17, "PV", "SPD", 1, 9);
        var journey = new RetirementJourney("0003994", "RBS", utcNow, "current", "next", quote.Right(), 90, 37);
        var sut = new CalculationBuilder()
            .EffectiveRetirementDate(now.AddDays(140).Date)
            .CurrentDate(now)
            .BuildV1();
        sut.SetJourney(journey, "FullPension");

        var result = sut.ExpectedRetirementJourneyExpirationDate(utcNow, 37);

        result.Date.Should().Be(utcNow.Date.AddDays(90));
    }

    public void ReturnsCorrectExpectedRetirementJourneyExpirationDate2()
    {
        var now = new DateTimeOffset(DateTime.Now);
        var sut = new CalculationBuilder()
            .EffectiveRetirementDate(now.AddDays(140).Date)
            .CurrentDate(now)
            .BuildV1();

        var result = sut.ExpectedRetirementJourneyExpirationDate(now, 90);

        result.Date.Should().Be(now.AddDays(90).Date);
    }

    [Input(true, true)]
    [Input(false, false)]
    [Input(null, null)]
    public void SetsCalculationSuccessStatusCorrectly(bool? calculationSuccessStatus, bool? expectedResult)
    {
        var sut = new CalculationBuilder()
            .SetCalculationSuccessStatus(null)
            .BuildV1();

        sut.SetCalculationSuccessStatus(calculationSuccessStatus);

        sut.IsCalculationSuccessful.Should().Be(expectedResult);
    }

    [Input(true, true)]
    [Input(false, false)]
    public void UpdatesCalculationSuccessStatusCorrectly(bool calculationSuccessStatus, bool expectedResult)
    {
        var sut = new CalculationBuilder()
            .SetCalculationSuccessStatus(null)
            .BuildV1();

        sut.UpdateCalculationSuccessStatus(calculationSuccessStatus);

        sut.IsCalculationSuccessful.Should().Be(expectedResult);
    }

    [Input(5, false)]
    [Input(6, false)]
    [Input(7, true)]
    public void IsRetirementDateOutOfRangeReturnsCorrectResult(int month, bool expectedResult)
    {
        var sut = new CalculationBuilder()
            .SetCalculationSuccessStatus(null)
            .EffectiveRetirementDate(DateTime.UtcNow.AddMonths(month))
            .BuildV2();

        var result = sut.IsRetirementDateOutOfRange();

        result.Should().Be(expectedResult);
    }
}