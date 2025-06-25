using FluentAssertions;
using WTW.MdpService.Domain.Members;

namespace WTW.MdpService.Test.Domain.Members;

public class ObjectStatusTest
{
    public void CanCreateObjectStatus()
    {
        var sut = new ObjectStatus("RBS", "test", "test1", "test2");

        sut.BusinessGroup.Should().Be("RBS");
        sut.ObjectId.Should().Be("test");
        sut.StatusAccess.Should().Be("test1");
        sut.TableShort.Should().Be("test2");
    }
}