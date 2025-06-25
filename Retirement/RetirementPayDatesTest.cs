using System;
using System.Collections.Generic;
using FluentAssertions;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.Retirement;

namespace WTW.MdpService.Test.Retirement;

public class RetirementPayDatesTest
{
    public void ReturnsFirstMonthlyPensionPayDate_WhenTenanHasSpecifiedTenantRetirementTimeline()
    {
        var sut = new RetirementPayDates(Timelines(), BankHolidays(), "BCL", new DateTime(2022, 7, 15));

        var date = sut.FirstMonthlyPensionPayDate();

        date.Should().Be(new DateTime(2022, 8, 5));
    }

    public void ReturnsFirstMonthlyPensionPayDate_WhenTenanHasNoSpecifiedTenantRetirementTimeline()
    {
        var sut = new RetirementPayDates(Timelines(), BankHolidays(), "RBS", new DateTime(2022, 7, 15));

        var date = sut.FirstMonthlyPensionPayDate();

        date.Should().Be(new DateTime(2022, 8, 18));
    }

    public void ReturnsLumpSumPayDate_WhenHasNoAvc()
    {
        var sut = new RetirementPayDates(
            new List<TenantRetirementTimeline>()
            {
                new TenantRetirementTimeline(1, "RETLSRECD", "RT","ZZY", "*", "*", "+,0,+,5,0,0")
            },
            BankHolidays(), "BCL", new DateTime(2022, 10, 28));

        var date = sut.LumpSumPayDate(false);

        date.Should().Be(new DateTime(2022, 11, 4));
    }

    public void ReturnsLumpSumPayDate_WhenHasAvc()
    {
        var sut = new RetirementPayDates(
            new List<TenantRetirementTimeline>()
            {
                new TenantRetirementTimeline(1, "RETAVCSLSRECD","RT", "ZZY", "*", "*", "+,0,+,30,0,0")
            },
            BankHolidays(), "BCL", new DateTime(2022, 10, 28));

        var date = sut.LumpSumPayDate(true);

        date.Should().Be(new DateTime(2022, 11, 25));
    }

    public void ReturnsLumpSumPayDate_WhenTenantHasSpecifiedTenantRetirementTimeline_AndHasNoAvc()
    {
        var sut = new RetirementPayDates(Timelines(), BankHolidays(), "BCL", new DateTime(2022, 7, 15));

        var date = sut.LumpSumPayDate(false);

        date.Should().Be(new DateTime(2022, 7, 27));
    }

    public void ReturnsLumpSumPayDate_WhenTenanHasNoSpecifiedTenantRetirementTimeline_AndHasNoAvc()
    {
        var sut = new RetirementPayDates(Timelines(), BankHolidays(), "RBS", new DateTime(2022, 7, 15));

        var date = sut.LumpSumPayDate(false);

        date.Should().Be(new DateTime(2022, 8, 26));
    }

    public void ReturnsLumpSumPayDate_WhenTenanHasSpecifiedTenantRetirementTimeline_AndHasAvc()
    {
        var sut = new RetirementPayDates(Timelines(), BankHolidays(), "BCL", new DateTime(2022, 7, 15));

        var date = sut.LumpSumPayDate(true);

        date.Should().Be(new DateTime(2022, 7, 29));
    }

    public void ReturnsLumpSumPayDate_WhenTenanHasNoSpecifiedTenantRetirementTimeline_AndHasAvc()
    {
        var sut = new RetirementPayDates(Timelines(), BankHolidays(), "RBS", new DateTime(2022, 7, 15));

        var date = sut.LumpSumPayDate(true);

        date.Should().Be(new DateTime(2022, 8, 12));
    }

    private static List<BankHoliday> BankHolidays()
    {
        return new List<BankHoliday>()
        {
            new BankHoliday(new DateTime(2022, 8, 1), "Some description"),
            new BankHoliday(new DateTime(2022, 8, 1), "Some description"),
            new BankHoliday(new DateTime(2022, 8, 1), "Some description"),
            new BankHoliday(new DateTime(2022, 8, 1), "Some description"),
        };
    }

    private static List<TenantRetirementTimeline> Timelines()
    {
        return new List<TenantRetirementTimeline>()
        {
            new TenantRetirementTimeline(1, "RETFIRSTPAYMADE", "RT","BCL", "*", "*", "+,0,+,0,0,23"),
            new TenantRetirementTimeline(2, "RETFIRSTPAYMADE", "RT","BCL", "*", "*", "+,0,+,0,0,5"),
            new TenantRetirementTimeline(1, "RETFIRSTPAYMADE", "RT","ZZY", "*", "*", "+,0,+,0,0,18"),

            new TenantRetirementTimeline(1, "RETAVCSLSRECD","RT", "BCL", "*", "*", "+,0,+,15,0,0"),
            new TenantRetirementTimeline(2, "RETAVCSLSRECD","RT", "BCL", "*", "*", "+,0,+,14,0,0"),
            new TenantRetirementTimeline(1, "RETAVCSLSRECD","RT", "ZZY", "*", "*", "+,0,+,30,0,0"),

            new TenantRetirementTimeline(1, "RETLSRECD","RT", "BCL", "*", "*", "+,0,+,15,0,0"),
            new TenantRetirementTimeline(2, "RETLSRECD","RT", "BCL", "*", "*", "+,0,+,8,0,0"),
            new TenantRetirementTimeline(1, "RETLSRECD","RT", "ZZY", "*", "*", "+,0,+,29,0,0"),
        };
    }
}