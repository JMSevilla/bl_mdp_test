using FluentAssertions;
using WTW.MdpService.Domain.Members;

namespace WTW.MdpService.Test.Domain.Members;

public class CalculationHistoryTest
{
    public void CreatesCalculationHistory()
    {
        var sut = new CalculationHistory("0003994", "RBS", "someEvent", 1, null, null);

        sut.ReferenceNumber.Should().Be("0003994");
        sut.BusinessGroup.Should().Be("RBS");
        sut.Event.Should().Be("someEvent");
        sut.SequenceNumber.Should().Be(1);
        sut.ImageId.Should().BeNull();
        sut.FileId.Should().BeNull();
    }

    public void CanUpdateIds()
    {
        var sut = new CalculationHistory("0003994", "RBS", "someEvent", 1, null, null);

        sut.UpdateIds(123456, 654321);

        sut.ImageId.Should().Be(123456);
        sut.FileId.Should().Be(654321);
    }
}