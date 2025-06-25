using System;
using System.Collections.Generic;
using FluentAssertions;
using WTW.MdpService.Domain.Members;
using WTW.TestCommon.FixieConfig;

namespace WTW.MdpService.Test.Domain.Members;

public class TenantRetirementTimelineTest
{
    public void CreatesTenantRetirementTimeline()
    {
        var sut = new TenantRetirementTimeline(1, "RETAVCSLSRECD", "RT", "RBS", "*", "*", "+,0,+,5,0,0");

        sut.SequenceNumber.Should().Be(1);
        sut.OutputId.Should().Be("RETAVCSLSRECD");
        sut.Event.Should().Be("RT");
        sut.BusinessGroup.Should().Be("RBS");
        sut.SchemeIdentification.Should().Be("*");
        sut.CategoryIdentification.Should().Be("*");
        sut.DateCalculatorFormula.Should().Be("+,0,+,5,0,0");
    }

    public void ReturnsFirstMonthlyPensionPayDate_WhenPaymentDayRegularWorkingDay()
    {
        var sut = new TenantRetirementTimeline(1, "RETFIRSTPAYMADE", "RT", "RBS", "*", "*", "+,0,+,0,0,18");

        var date = sut.FirstMonthlyPensionPayDate(new DateTime(2022, 7, 15), new List<DateTime> { new DateTime(2022, 5, 27) });

        date.Should().Be(new DateTime(2022, 8, 18));
        date.DayOfWeek.Should().Be(DayOfWeek.Thursday);
    }

    public void ReturnsFirstMonthlyPensionPayDate_WhenRetirementDateInDecember()
    {
        var sut = new TenantRetirementTimeline(1, "RETFIRSTPAYMADE", "RT", "RBS", "*", "*", "+,0,+,0,0,18");

        var date = sut.FirstMonthlyPensionPayDate(new DateTime(2022, 12, 15), new List<DateTime> { new DateTime(2022, 5, 27) });

        date.Should().Be(new DateTime(2023, 1, 18));
        date.DayOfWeek.Should().Be(DayOfWeek.Wednesday);
    }

    public void ReturnsFirstMonthlyPensionPayDate_WhenCalculatedPaymentDaySunday()
    {
        var sut = new TenantRetirementTimeline(1, "RETFIRSTPAYMADE", "RT", "RBS", "*", "*", "+,0,+,0,0,21");

        var date = sut.FirstMonthlyPensionPayDate(new DateTime(2022, 7, 15), new List<DateTime> { new DateTime(2022, 5, 27) });

        date.Should().Be(new DateTime(2022, 8, 19));
        date.DayOfWeek.Should().Be(DayOfWeek.Friday);
    }

    public void ReturnsFirstMonthlyPensionPayDate_WhenCalculatedPaymentDaySaturday()
    {
        var sut = new TenantRetirementTimeline(1, "RETFIRSTPAYMADE", "RT", "RBS", "*", "*", "+,0,+,0,0,20");

        var date = sut.FirstMonthlyPensionPayDate(new DateTime(2022, 7, 15), new List<DateTime> { new DateTime(2022, 5, 27) });

        date.Should().Be(new DateTime(2022, 8, 19));
        date.DayOfWeek.Should().Be(DayOfWeek.Friday);
    }

    public void ReturnsFirstMonthlyPensionPayDate_WhenCalculatedPaymentDayBankHoliday_IntheMidleOfTheWeek()
    {
        var sut = new TenantRetirementTimeline(1, "RETFIRSTPAYMADE", "RT", "RBS", "*", "*", "+,0,+,0,0,23");

        var date = sut.FirstMonthlyPensionPayDate(new DateTime(2022, 7, 15), new List<DateTime> { new DateTime(2022, 8, 23) });

        date.Should().Be(new DateTime(2022, 8, 22));
        date.DayOfWeek.Should().Be(DayOfWeek.Monday);
    }

    public void ReturnsFirstMonthlyPensionPayDate_WhenCalculatedPaymentDayBankHoliday_OnMonday()
    {
        var sut = new TenantRetirementTimeline(1, "RETFIRSTPAYMADE", "RT", "RBS", "*", "*", "+,0,+,0,0,22");

        var date = sut.FirstMonthlyPensionPayDate(new DateTime(2022, 7, 15), new List<DateTime> { new DateTime(2022, 8, 22) });

        date.Should().Be(new DateTime(2022, 8, 19));
        date.DayOfWeek.Should().Be(DayOfWeek.Friday);
    }

    public void ReturnsFirstMonthlyPensionPayDate_WhenCalculatedPaymentDayBankHoliday_OnMonday_AndBankHoliddayOnFridayAsWell()
    {
        var sut = new TenantRetirementTimeline(1, "RETFIRSTPAYMADE", "RT", "RBS", "*", "*", "+,0,+,0,0,22");

        var date = sut.FirstMonthlyPensionPayDate(new DateTime(2022, 7, 15), new List<DateTime> { new DateTime(2022, 8, 22), new DateTime(2022, 8, 19) });

        date.Should().Be(new DateTime(2022, 8, 18));
        date.DayOfWeek.Should().Be(DayOfWeek.Thursday);
    }

    public void ReturnsLumpSumWithNoAvcPayDate_WhenMoreThanFiveWorkDaysAdded()
    {
        var sut = new TenantRetirementTimeline(1, "RETAVCSLSRECD", "RT", "RBS", "*", "*", "+,0,+,18,0,0");

        var date = sut.LumpSumWithNoAvcPayDate(new DateTime(2022, 7, 15), new List<DateTime> { new DateTime(2022, 5, 27) });

        date.Should().Be(new DateTime(2022, 8, 10));
        date.DayOfWeek.Should().Be(DayOfWeek.Wednesday);
    }

    public void ReturnsLumpSumWithNoAvcPayDate_WhenMoreLessFiveWorkDaysAdded()
    {
        var sut = new TenantRetirementTimeline(1, "RETAVCSLSRECD", "RT", "RBS", "*", "*", "+,0,+,4,0,0");

        var date = sut.LumpSumWithNoAvcPayDate(new DateTime(2022, 7, 15), new List<DateTime> { new DateTime(2022, 5, 27) });

        date.Should().Be(new DateTime(2022, 7, 21));
        date.DayOfWeek.Should().Be(DayOfWeek.Thursday);
    }

    public void ReturnsLumpSumWithNoAvcPayDate_WhenMoreThanFiveWorkDaysAdded_AndBankHolidayExistIntPeriod()
    {
        var sut = new TenantRetirementTimeline(1, "RETAVCSLSRECD", "RT", "RBS", "*", "*", "+,0,+,18,0,0");

        var date = sut.LumpSumWithNoAvcPayDate(new DateTime(2022, 7, 15), new List<DateTime> { new DateTime(2022, 7, 21) });

        date.Should().Be(new DateTime(2022, 8, 11));
        date.DayOfWeek.Should().Be(DayOfWeek.Thursday);
    }

    public void ReturnsLumpSumWithNoAvcPayDate_WhenMoreThanFiveWorkDaysAdded_AndBankHolidayOnWeekendExistIntPeriod()
    {
        var sut = new TenantRetirementTimeline(1, "RETAVCSLSRECD", "RT", "RBS", "*", "*", "+,0,+,18,0,0");

        var date = sut.LumpSumWithNoAvcPayDate(new DateTime(2022, 7, 15), new List<DateTime> { new DateTime(2022, 7, 23) });

        date.Should().Be(new DateTime(2022, 8, 10));
        date.DayOfWeek.Should().Be(DayOfWeek.Wednesday);
    }

    public void ReturnsLumpSumWithNoAvcPayDate_WhenRetirementDateInDecember()
    {
        var sut = new TenantRetirementTimeline(1, "RETAVCSLSRECD", "RT", "RBS", "*", "*", "+,0,+,18,0,0");

        var date = sut.LumpSumWithNoAvcPayDate(new DateTime(2022, 12, 15), new List<DateTime> { new DateTime(2022, 5, 26), new DateTime(2023, 1, 1) });

        date.Should().Be(new DateTime(2023, 1, 10));
        date.DayOfWeek.Should().Be(DayOfWeek.Tuesday);
    }

    public void ReturnsLumpSumWithAvcPayDate_WhenPaymentDayRegularWorkingDay()
    {
        var sut = new TenantRetirementTimeline(1, "RETFIRSTPAYMADE", "RT", "RBS", "*", "*", "+,0,+,18,0,0");

        var date = sut.LumpSumWithAvcPayDate(new DateTime(2022, 7, 15), new List<DateTime> { new DateTime(2022, 5, 27) });

        date.Should().Be(new DateTime(2022, 8, 2));
        date.DayOfWeek.Should().Be(DayOfWeek.Tuesday);
    }

    public void ReturnsLumpSumWithAvcPayDate_WhenCalculatedPaymentDaySunday()
    {
        var sut = new TenantRetirementTimeline(1, "RETAVCSLSRECD", "RT", "RBS", "*", "*", "+,0,+,23,0,0");

        var date = sut.LumpSumWithAvcPayDate(new DateTime(2022, 7, 15), new List<DateTime> { new DateTime(2022, 5, 27) });

        date.Should().Be(new DateTime(2022, 8, 5));
        date.DayOfWeek.Should().Be(DayOfWeek.Friday);
    }

    public void ReturnsLumpSumWithAvcPayDate_WhenCalculatedPaymentDaySaturday()
    {
        var sut = new TenantRetirementTimeline(1, "RETAVCSLSRECD", "RT", "RBS", "*", "*", "+,0,+,22,0,0");

        var date = sut.LumpSumWithAvcPayDate(new DateTime(2022, 7, 15), new List<DateTime> { new DateTime(2022, 5, 27) });

        date.Should().Be(new DateTime(2022, 8, 5));
        date.DayOfWeek.Should().Be(DayOfWeek.Friday);
    }

    public void ReturnsLumpSumWithAvcPayDate_WhenCalculatedPaymentDayBankHoliday_IntheMidleOfTheWeek()
    {
        var sut = new TenantRetirementTimeline(1, "RETAVCSLSRECD", "RT", "RBS", "*", "*", "+,0,+,18,0,0");

        var date = sut.LumpSumWithAvcPayDate(new DateTime(2022, 7, 15), new List<DateTime> { new DateTime(2022, 8, 2) });

        date.Should().Be(new DateTime(2022, 8, 1));
        date.DayOfWeek.Should().Be(DayOfWeek.Monday);
    }

    public void ReturnsLumpSumWithAvcPayDate_WhenCalculatedPaymentDayBankHoliday_OnMonday()
    {
        var sut = new TenantRetirementTimeline(1, "RETAVCSLSRECD", "RT", "RBS", "*", "*", "+,0,+,17,0,0");

        var date = sut.LumpSumWithAvcPayDate(new DateTime(2022, 7, 15), new List<DateTime> { new DateTime(2022, 8, 1) });

        date.Should().Be(new DateTime(2022, 7, 29));
        date.DayOfWeek.Should().Be(DayOfWeek.Friday);
    }

    public void ReturnsLumpSumWithAvcPayDate_WhenCalculatedPaymentDayBankHoliday_OnMonday_AndBankHoliddayOnFridayAsWell()
    {
        var sut = new TenantRetirementTimeline(1, "RETAVCSLSRECD", "RT", "RBS", "*", "*", "+,0,+,17,0,0");

        var date = sut.LumpSumWithAvcPayDate(new DateTime(2022, 7, 15), new List<DateTime> { new DateTime(2022, 8, 1), new DateTime(2022, 7, 29) });

        date.Should().Be(new DateTime(2022, 7, 28));
        date.DayOfWeek.Should().Be(DayOfWeek.Thursday);
    }

    public void ReturnsLumpSumWithAvcPayDate_WhenRetirementDateInDecember()
    {
        var sut = new TenantRetirementTimeline(1, "RETAVCSLSRECD", "RT", "RBS", "*", "*", "+,0,+,30,0,0");

        var date = sut.LumpSumWithAvcPayDate(new DateTime(2022, 12, 15), new List<DateTime> { new DateTime(2022, 5, 26), new DateTime(2023, 1, 1) });

        date.Should().Be(new DateTime(2023, 1, 13));
        date.DayOfWeek.Should().Be(DayOfWeek.Friday);
    }

    [Input("1", "1")]
    [Input("2", "2")]
    [Input("3", "3")]
    [Input("4", "4")]
    [Input("5", "5")]
    [Input("31", "31")]
    public void ReturnsCorrectPayDay(string day, string expectedResult)
    {
        var sut = new TenantRetirementTimeline(1, "RETAVCSLSRECD", "RT", "RBS", "*", "*", $"+,0,+,30,0,{day}");

        var date = sut.PensionMonthPayDay();

        date.Should().Be(expectedResult);
    }

    [Input("+,0,+,30,0,28,+", "+")]
    [Input("+,0,+,30,0,28,-", "-")]
    [Input("+,0,+,30,0,28", null)]
    public void ReturnsCorrectPayDayIndicator(string formula, string expectedResult)
    {
        var sut = new TenantRetirementTimeline(1, "RETAVCSLSRECD", "RT", "RBS", "*", "*", formula);

        var indicator = sut.PensionMonthPayDayIndicator();

        indicator.Should().Be(expectedResult);
    }
}