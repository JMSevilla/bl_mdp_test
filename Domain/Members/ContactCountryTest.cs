using FluentAssertions;
using WTW.MdpService.Domain.Members;

namespace WTW.MdpService.Test.Domain.Members;

public class ContactCountryTest
{
    public void CreatesContactCountry()
    {
        var sut = new ContactCountry(123456, "UK");

        sut.AddressNumber.Should().Be(123456);
        sut.Country.Should().Be("UK");
        sut.AddressCode.Should().Be("GENERAL");
        sut.PhoneType.Should().Be("MOBPHON1");
    }
}
