using System;
using FluentAssertions;
using WTW.MdpService.Domain.Members;

namespace WTW.MdpService.Test.Domain.Members;

public class LinkedMemberTest
{
    public void CreatesLinkedMember()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = new LinkedMember("0003994", "RBS", "1111124", "BCL");

        sut.ReferenceNumber.Should().Be("0003994");
        sut.BusinessGroup.Should().Be("RBS");
        sut.LinkedReferenceNumber.Should().Be("1111124");
        sut.LinkedBusinessGroup.Should().Be("BCL");
    }
}