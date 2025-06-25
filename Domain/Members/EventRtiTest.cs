using FluentAssertions;
using WTW.MdpService.Domain.Members;

namespace WTW.MdpService.Test.Domain.Members;

public class EventRtiTest
{
    public void CanCreateEventRti()
    {
        var sut = new EventRti(123,"RBS","testCode", "testStatus");

        sut.Score.Should().Be(123);
        sut.BusinessGroup.Should().Be("RBS");
        sut.CaseCode.Should().Be("testCode");
        sut.Status.Should().Be("testStatus");
    }
}
