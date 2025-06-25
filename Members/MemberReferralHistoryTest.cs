using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.Members;

namespace WTW.MdpService.Test.Members;

public class MemberReferralHistoryTest
{
    public void MapMemberReferralHistory_WhenIfaHistoryHasPendingItem()
    {
        var referralInitiatedOn = DateTimeOffset.UtcNow.AddDays(1);
        var referralDate = DateTimeOffset.UtcNow.AddDays(2);

        var histories = new List<IfaReferralHistory>
        {
            new ("123456", "RBS", 1, ReferralStatus.ReferralInitiated, null, referralInitiatedOn,referralDate),
            new ("123456", "RBS", 2, ReferralStatus.WelcomePack, null, referralInitiatedOn, referralDate),
            new ("123456", "RBS", 3, ReferralStatus.WelcomePackIssued, null,referralInitiatedOn, referralDate),
            new ("123456", "RBS", 4, ReferralStatus.FirstAppointmentArranged, null, referralInitiatedOn, referralDate),
            new ("123456", "RBS", 5, ReferralStatus.FactFind, null, referralInitiatedOn, referralDate),
            new ("123456", "RBS", 6, ReferralStatus.FactFindStarted, null, referralInitiatedOn, referralDate),
            new ("123456", "RBS", 7, ReferralStatus.FactFindCompleted, null, referralInitiatedOn, referralDate),
        };

        var builder = new ReferralHistoryItemsBuilder(histories);
        var sut = builder.ReferralHistoryItems();

        sut.Count().Should().Be((int)histories[^1].ReferralStatus + 1);

        sut.ElementAt(0).ReferralDate.Should().Be(referralInitiatedOn);
        sut.ElementAt(1).ReferralDate.Should().Be(referralDate);
        sut.ElementAt(2).ReferralDate.Should().Be(referralDate);
        sut.ElementAt(3).ReferralDate.Should().Be(referralDate);
        sut.ElementAt(4).ReferralDate.Should().Be(referralDate);
        sut.ElementAt(5).ReferralDate.Should().Be(referralDate);
        sut.ElementAt(6).ReferralDate.Should().Be(referralDate);
        sut.ElementAt(7).ReferralDate.Should().Be(null);

        sut.ElementAt(0).ReferralBadgeStatus.Should().Be(ReferralBadgeStatus.Completed);
        sut.ElementAt(1).ReferralBadgeStatus.Should().Be(ReferralBadgeStatus.Completed);
        sut.ElementAt(2).ReferralBadgeStatus.Should().Be(ReferralBadgeStatus.Completed);
        sut.ElementAt(3).ReferralBadgeStatus.Should().Be(ReferralBadgeStatus.Completed);
        sut.ElementAt(4).ReferralBadgeStatus.Should().Be(ReferralBadgeStatus.Completed);
        sut.ElementAt(5).ReferralBadgeStatus.Should().Be(ReferralBadgeStatus.Completed);
        sut.ElementAt(6).ReferralBadgeStatus.Should().Be(ReferralBadgeStatus.Completed);
        sut.ElementAt(7).ReferralBadgeStatus.Should().Be(ReferralBadgeStatus.Pending);

        sut.ElementAt(0).ReferralStatus.Should().Be("REFERRAL_INITIATED");
        sut.ElementAt(1).ReferralStatus.Should().Be("WELCOME_PACK");
        sut.ElementAt(2).ReferralStatus.Should().Be("WELCOME_PACK_ISSUED");
        sut.ElementAt(3).ReferralStatus.Should().Be("FIRST_APPOINTMENT");
        sut.ElementAt(4).ReferralStatus.Should().Be("FACT_FIND");
        sut.ElementAt(5).ReferralStatus.Should().Be("FACT_FIND_STARTED");
        sut.ElementAt(6).ReferralStatus.Should().Be("FACT_FIND_COMPLETED");
    }

    public void MapMemberReferralHistory_WhenIfaHistoryHasCancelledItem()
    {
        var referralInitiatedOn = DateTimeOffset.UtcNow.AddDays(1);
        var referralDate = DateTimeOffset.UtcNow.AddDays(2);

        var histories = new List<IfaReferralHistory>
        {
            new ("123456", "RBS", 1, ReferralStatus.ReferralInitiated, null, referralInitiatedOn,referralDate),
            new ("123456", "RBS", 2, ReferralStatus.WelcomePack, "3", referralInitiatedOn, referralDate),
        };

        var builder = new ReferralHistoryItemsBuilder(histories);
        var sut = builder.ReferralHistoryItems();

        sut.Count().Should().Be(histories.Count + 1);

        sut.ElementAt(0).ReferralDate.Should().Be(referralInitiatedOn);
        sut.ElementAt(1).ReferralDate.Should().Be(referralDate);
        sut.ElementAt(2).ReferralDate.Should().Be(referralDate);

        sut.ElementAt(0).ReferralBadgeStatus.Should().Be(ReferralBadgeStatus.Completed);
        sut.ElementAt(1).ReferralBadgeStatus.Should().Be(ReferralBadgeStatus.Completed);
        sut.ElementAt(2).ReferralBadgeStatus.Should().Be(ReferralBadgeStatus.Cancelled);

        sut.ElementAt(0).ReferralStatus.Should().Be("REFERRAL_INITIATED");
        sut.ElementAt(1).ReferralStatus.Should().Be("WELCOME_PACK");
        sut.ElementAt(2).ReferralStatus.Should().Be("WELCOME_PACK_ISSUED");
    }
    
    public void MapMemberReferralHistory_WhenIfaHistoryHasGaps()
    {
        var referralInitiatedOn = DateTimeOffset.UtcNow.AddDays(1);
        var referralDate = DateTimeOffset.UtcNow.AddDays(2);

        var histories = new List<IfaReferralHistory>
        {
            new ("0-123456", "RBS", 1, ReferralStatus.ReferralInitiated, null, referralInitiatedOn,referralDate),
            new ("1-123456", "RBS", 3, ReferralStatus.WelcomePackIssued, null, referralInitiatedOn,referralDate),
            new ("2-123456", "RBS", 5, ReferralStatus.FactFind, null, referralInitiatedOn, referralDate),
            new ("3-123456", "RBS", 7, ReferralStatus.FactFindCompleted, null, referralInitiatedOn, referralDate),
        };

        var builder = new ReferralHistoryItemsBuilder(histories);
        var sut = builder.ReferralHistoryItems();

        sut.Count().Should().Be((int)histories[^1].ReferralStatus + 1);

        sut.ElementAt(0).ReferralDate.Should().Be(referralInitiatedOn);
        sut.ElementAt(1).ReferralDate.Should().Be(referralDate);
        sut.ElementAt(2).ReferralDate.Should().Be(referralDate);
        sut.ElementAt(3).ReferralDate.Should().Be(referralDate);
        sut.ElementAt(4).ReferralDate.Should().Be(referralDate);
        sut.ElementAt(5).ReferralDate.Should().Be(referralDate);
        sut.ElementAt(6).ReferralDate.Should().Be(referralDate);
        sut.ElementAt(7).ReferralDate.Should().Be(null);

        sut.ElementAt(0).ReferralBadgeStatus.Should().Be(ReferralBadgeStatus.Completed);
        sut.ElementAt(1).ReferralBadgeStatus.Should().Be(ReferralBadgeStatus.Completed);
        sut.ElementAt(2).ReferralBadgeStatus.Should().Be(ReferralBadgeStatus.Completed);
        sut.ElementAt(3).ReferralBadgeStatus.Should().Be(ReferralBadgeStatus.Completed);
        sut.ElementAt(4).ReferralBadgeStatus.Should().Be(ReferralBadgeStatus.Completed);
        sut.ElementAt(5).ReferralBadgeStatus.Should().Be(ReferralBadgeStatus.Completed);
        sut.ElementAt(6).ReferralBadgeStatus.Should().Be(ReferralBadgeStatus.Completed);
        sut.ElementAt(7).ReferralBadgeStatus.Should().Be(ReferralBadgeStatus.Pending);

        sut.ElementAt(0).ReferralStatus.Should().Be("REFERRAL_INITIATED");
        sut.ElementAt(1).ReferralStatus.Should().Be("WELCOME_PACK");
        sut.ElementAt(2).ReferralStatus.Should().Be("WELCOME_PACK_ISSUED");
        sut.ElementAt(3).ReferralStatus.Should().Be("FIRST_APPOINTMENT");
        sut.ElementAt(4).ReferralStatus.Should().Be("FACT_FIND");
        sut.ElementAt(5).ReferralStatus.Should().Be("FACT_FIND_STARTED");
        sut.ElementAt(6).ReferralStatus.Should().Be("FACT_FIND_COMPLETED");
        sut.ElementAt(7).ReferralStatus.Should().Be("RECOMMENDATION");
    }
    
    public void MapMemberReferralHistory_WhenIfaHistoryHasAllCompleted()
    {
        var referralInitiatedOn = DateTimeOffset.UtcNow.AddDays(1);
        var referralDate = DateTimeOffset.UtcNow.AddDays(2);

        var histories = new List<IfaReferralHistory>
        {
            new ("123456", "RBS", 1, ReferralStatus.ReferralInitiated, null, referralInitiatedOn,referralDate),
            new ("123456", "RBS", 8, ReferralStatus.Recommendation, null, referralInitiatedOn, referralDate),
        };

        var builder = new ReferralHistoryItemsBuilder(histories);
        var sut = builder.ReferralHistoryItems();

        sut.Count().Should().Be((int)histories[^1].ReferralStatus);

        sut.ElementAt(0).ReferralDate.Should().Be(referralInitiatedOn);
        sut.ElementAt(1).ReferralDate.Should().Be(referralDate);
        sut.ElementAt(2).ReferralDate.Should().Be(referralDate);
        sut.ElementAt(3).ReferralDate.Should().Be(referralDate);
        sut.ElementAt(4).ReferralDate.Should().Be(referralDate);
        sut.ElementAt(5).ReferralDate.Should().Be(referralDate);
        sut.ElementAt(6).ReferralDate.Should().Be(referralDate);
        sut.ElementAt(7).ReferralDate.Should().Be(referralDate);

        sut.ElementAt(0).ReferralBadgeStatus.Should().Be(ReferralBadgeStatus.Completed);
        sut.ElementAt(1).ReferralBadgeStatus.Should().Be(ReferralBadgeStatus.Completed);
        sut.ElementAt(2).ReferralBadgeStatus.Should().Be(ReferralBadgeStatus.Completed);
        sut.ElementAt(3).ReferralBadgeStatus.Should().Be(ReferralBadgeStatus.Completed);
        sut.ElementAt(4).ReferralBadgeStatus.Should().Be(ReferralBadgeStatus.Completed);
        sut.ElementAt(5).ReferralBadgeStatus.Should().Be(ReferralBadgeStatus.Completed);
        sut.ElementAt(6).ReferralBadgeStatus.Should().Be(ReferralBadgeStatus.Completed);
        sut.ElementAt(7).ReferralBadgeStatus.Should().Be(ReferralBadgeStatus.Completed);

        sut.ElementAt(0).ReferralStatus.Should().Be("REFERRAL_INITIATED");
        sut.ElementAt(1).ReferralStatus.Should().Be("WELCOME_PACK");
        sut.ElementAt(2).ReferralStatus.Should().Be("WELCOME_PACK_ISSUED");
        sut.ElementAt(3).ReferralStatus.Should().Be("FIRST_APPOINTMENT");
        sut.ElementAt(4).ReferralStatus.Should().Be("FACT_FIND");
        sut.ElementAt(5).ReferralStatus.Should().Be("FACT_FIND_STARTED");
        sut.ElementAt(6).ReferralStatus.Should().Be("FACT_FIND_COMPLETED");
        sut.ElementAt(7).ReferralStatus.Should().Be("RECOMMENDATION");
    }

    public void MapMemberReferralHistory_WhenIfaHistoryIsEmpty()
    {
        var builder = new ReferralHistoryItemsBuilder(new List<IfaReferralHistory>());
        var sut = builder.ReferralHistoryItems();

        sut.Count().Should().Be(1);
    }
}