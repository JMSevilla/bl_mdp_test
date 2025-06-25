using System;
using System.Threading.Tasks;
using FluentAssertions;
using WTW.MdpService.Infrastructure;
using WTW.TestCommon.FixieConfig;

namespace WTW.MdpService.Test.Infrastructure;

public class TimePeriodCalculatorTest
{
    [Input("2021-2-12", "2023-2-1", "1-11-2-6")]
    [Input("2021-2-12", "2023-3-8", "2-0-3-3")]
    [Input("2022-3-8", "2023-3-8", "1-0-0-0")]
    [Input("2022-3-7", "2023-3-8", "1-0-0-1")]
    [Input("2022-2-28", "2023-3-8", "1-0-1-1")]
    [Input("2022-1-28", "2023-3-8", "1-1-1-1")]
    [Input("2022-2-6", "2023-3-8", "1-1-0-2")]
    [Input("2022-2-8", "2023-3-8", "1-1-0-0")]
    [Input("2022-2-9", "2023-3-8", "1-0-3-6")]
    [Input("2022-2-14", "2023-3-8", "1-0-3-1")]
    [Input("2022-2-15", "2023-3-8", "1-0-3-0")]
    [Input("2022-2-16", "2023-3-8", "1-0-2-6")]
    [Input("2022-3-9", "2023-3-8", "0-11-3-6")]
    [Input("2022-3-14", "2023-3-8", "0-11-3-1")]
    [Input("2022-3-15", "2023-3-8", "0-11-3-0")]
    [Input("2022-3-16", "2023-3-8", "0-11-2-6")]
    [Input("2022-4-7", "2023-3-8", "0-11-0-1")]
    [Input("2022-4-8", "2023-3-8", "0-11-0-0")]
    [Input("2022-4-9", "2023-3-8", "0-10-3-6")]
    [Input("2022-4-14", "2023-3-8", "0-10-3-1")]
    [Input("2022-4-15", "2023-3-8", "0-10-3-0")]
    [Input("2022-4-16", "2023-3-8", "0-10-2-6")]
    [Input("2021-3-8", "2023-3-8", "2-0-0-0")]
    [Input("2021-3-9", "2023-3-8", "1-11-3-6")]
    [Input("2021-3-14", "2023-3-8", "1-11-3-1")]
    [Input("2021-3-15", "2023-3-8", "1-11-3-0")]
    [Input("2021-3-16", "2023-3-8", "1-11-2-6")]
    [Input("2021-4-7", "2023-3-8", "1-11-0-1")]
    [Input("2021-4-8", "2023-3-8", "1-11-0-0")]
    [Input("2021-4-9", "2023-3-8", "1-10-3-6")]
    [Input("2021-4-14", "2023-3-8", "1-10-3-1")]
    [Input("2021-4-15", "2023-3-8", "1-10-3-0")]
    [Input("2021-4-16", "2023-3-8", "1-10-2-6")]
    [Input("2023-3-8", "2023-3-8", "0-0-0-0")]
    [Input("2023-3-9", "2026-11-9", "3-8-0-0")]
    public void CalculatesYearsMonthsWeeksAndDaysCorrectly(string start, string end, string expectedResult)
    {
        var startYear = int.Parse(start.Split('-')[0]);
        var startMonth = int.Parse(start.Split('-')[1]);
        var startDay = int.Parse(start.Split('-')[2]);

        var endYear = int.Parse(end.Split('-')[0]);
        var endMonth = int.Parse(end.Split('-')[1]);
        var endDay = int.Parse(end.Split('-')[2]);

        var result = TimePeriodCalculator.Calculate(new DateTime(startYear, startMonth, startDay), new DateTime(endYear, endMonth, endDay));

        result.Years.Should().Be(int.Parse(expectedResult.Split('-')[0]));
        result.month.Should().Be(int.Parse(expectedResult.Split('-')[1]));
        result.Weeks.Should().Be(int.Parse(expectedResult.Split('-')[2]));
        result.Days.Should().Be(int.Parse(expectedResult.Split('-')[3]));
    }

    public void ThrowsExceptionWhenStartDateIsLargerThanEndDate()
    {
        var action = () => TimePeriodCalculator.Calculate(new DateTime(2026, 1, 2), new DateTime(2023, 3, 9));

        action.Should().Throw<ArgumentException>();
    }
}