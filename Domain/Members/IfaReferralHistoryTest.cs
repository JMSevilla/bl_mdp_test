using System;
using FluentAssertions;
using WTW.MdpService.Domain.Members;

namespace WTW.MdpService.Test.Domain.Members;

public class IfaReferralHistoryTest
{
    public void CanCreateIfaReferralHistory()
    {
        var referralInitiatedOn = DateTimeOffset.UtcNow.AddDays(1);
        var referralDate = DateTimeOffset.UtcNow.AddDays(2);

        var sut = new IfaReferralHistory("1234567", "RBS", 1, ReferralStatus.ReferralInitiated, "null", referralInitiatedOn, referralDate);

        sut.ReferenceNumber.Should().Be("1234567");
        sut.BusinessGroup.Should().Be("RBS");
        sut.BusinessGroup.Should().Be("RBS");
        sut.ReferralResult.Should().Be("null");
        sut.SequenceNumber.Should().Be(1);
        sut.ReferralStatus.Should().Be(ReferralStatus.ReferralInitiated);
        sut.ReferralInitiatedOn.Should().Be(referralInitiatedOn);
        sut.ReferralStatusDate.Should().Be(referralDate);
    }
}