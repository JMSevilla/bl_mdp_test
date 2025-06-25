using System;
using FluentAssertions;
using WTW.MdpService.Domain;
using WTW.MdpService.Domain.Members;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Domain.Members;

public class EpaEmailTest
{
    public void CanCreateEpaEmail()
    {
        var now = DateTimeOffset.UtcNow;
        var email = Email.Create("test@wtw.com").Right();
        var sut = new EpaEmail(email,now, 1);

        sut.Email.Should().Be(Email.Create("test@wtw.com").Right());
        sut.SequenceNumber.Should().Be(1);
        sut.EffectiveDate.Should().Be(now);
        sut.CreaetedAt.Should().Be(now);
    }
}