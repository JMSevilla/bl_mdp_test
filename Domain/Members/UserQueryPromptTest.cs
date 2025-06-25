using FluentAssertions;
using WTW.MdpService.Domain.Members;

namespace WTW.MdpService.Test.Domain.Members;

public class UserQueryPromptTest
{
    public void CanCreateRelationship()
    {
        var sut = new UserQueryPrompt(1, "RBS", "123456", "testStatus", "testEvent");

        sut.ScoreNumber.Should().Be(1);
        sut.BusinessGroup.Should().Be("RBS");
        sut.CaseCode.Should().Be("123456");
        sut.Status.Should().Be("testStatus");
        sut.Event.Should().Be("testEvent");
    }
}