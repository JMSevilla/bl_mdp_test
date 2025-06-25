using System;
using FluentAssertions;
using WTW.MdpService.Domain.Members;

namespace WTW.MdpService.Test.Domain.Members;

public class IfaReferralTest
{
    public void CreatesIfaReferral()
    {
        var date = DateTimeOffset.UtcNow.AddDays(-38);

        var sut = new IfaReferral("1111124", "RBS", date, "testResult", "calcType");

        sut.Should().NotBeNull();
        sut.BusinessGroup.Should().Be("RBS");
        sut.ReferenceNumber.Should().Be("1111124");
        sut.ReferralInitiatedOn.Should().Be(date);
        sut.ReferralResult.Should().Be("testResult");
        sut.CalculationType.Should().Be("calcType");
    }
}