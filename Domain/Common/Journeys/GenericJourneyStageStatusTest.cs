using System;
using FluentAssertions;
using WTW.MdpService.Domain.Common.Journeys;

namespace WTW.MdpService.Test.Domain.Common.Journeys;

public class GenericJourneyStageStatusTest
{
    public void CreatesGenericJourneyStageStatus_WhenFirstPageKeyIsGiven()
    {
        var date = DateTime.Now;
        var sut = new GenericJourneyStageStatus("start", date, true, "first-page-key");

        sut.Stage.Should().Be("start");
        sut.CompletedDate.Should().Be(date);
        sut.InProgress.Should().BeTrue();
        sut.FirstPageKey.Should().Be("first-page-key");
    }

    public void CreatesGenericJourneyStageStatus_WhenFirstPageKeyIsNotGiven()
    {
        var date = DateTime.Now;
        var sut = new GenericJourneyStageStatus("start2", date, false);

        sut.Stage.Should().Be("start2");
        sut.CompletedDate.Should().Be(date);
        sut.InProgress.Should().BeFalse();
        sut.FirstPageKey.Should().BeNull();
    }
}