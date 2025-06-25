using System;
using FluentAssertions;

namespace WTW.MdpService.Test.Domain.Members;

public class AuthorizationTest
{
    public void CreatesAuthorization()
    {
        var now = DateTimeOffset.UtcNow;
        var sut  = new WTW.MdpService.Domain.Members.Authorization("RBS", "0003994", 1171111, now);

        sut.AuthorizationNumber.Should().Be(1171111);
        sut.ReferenceNumber.Should().Be("0003994");
        sut.BusinessGroup.Should().Be("RBS");
       
        sut.AuthorizationCode.Should().Be("CIARD");
        sut.UserWhoCarriedOutActivity.Should().Be("MDP");
        sut.UserWhoAuthorisedActivity.Should().Be("MDP");
        sut.SchemeMemberIndicator.Should().Be("M");

        sut.AcitivityCarriedOutDate.Should().Be(now);
        sut.AcitivityAuthorizedDate.Should().Be(now);
        sut.AcitivityProcessedDate.Should().Be(now);
    }
}