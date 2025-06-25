using FluentAssertions;
using WTW.MdpService.Domain.Common.Journeys;

namespace WTW.MdpService.Test.Domain.Common;

public class CheckboxTest
{
    public void CreatesCheckbox()
    {
        var sut = new Checkbox("testKey", true);

        sut.AnswerValue.Should().BeTrue();
        sut.Key.Should().Be("testKey");
    }

    public void CreatesCheckboxWithAnswerString()
    {
        var sut = new Checkbox("testKey", true, "YES");

        sut.AnswerValue.Should().BeTrue();
        sut.Key.Should().Be("testKey");
        sut.Answer.Should().Be("YES");
    }

    public void DuplicatesCheckbox()
    {
        var sut = new Checkbox("testKey", true);

        var clonedCheckbox = sut.Duplicate();

        clonedCheckbox.AnswerValue.Should().BeTrue();
        clonedCheckbox.Key.Should().Be("testKey");
    }
}