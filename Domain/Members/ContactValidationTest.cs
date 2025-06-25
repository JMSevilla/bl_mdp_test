using System;
using FluentAssertions;
using WTW.MdpService.Domain.Mdp;
using WTW.MdpService.Domain.Members;

namespace WTW.MdpService.Test.Domain.Members;

public class ContactValidationTest
{
    public void CreatesContactValidation()
    {
        var date = DateTimeOffset.UtcNow;
        var sut = new ContactValidation("RBS11111111", MemberContactType.EmailAddress, date, 11111, "123456");

        sut.Should().NotBeNull();
        sut.ContactType.Should().Be(MemberContactType.EmailAddress);
        sut.AddressNumber.Should().Be(11111);
        sut.Token.Should().Be(123456);
    }
}
