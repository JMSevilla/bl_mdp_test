using System;
using FluentAssertions;
using WTW.MdpService.Domain.Members;

namespace WTW.MdpService.Test.Domain.Members;

public class BankHolidayTest
{
    public void CreatesBankHoliday()
    {
        var sut = new BankHoliday(new DateTime(2022, 8, 1), "Some description");

        sut.Date.Should().Be(new DateTime(2022, 8, 1));
        sut.Description.Should().Be("Some description");
    }
}