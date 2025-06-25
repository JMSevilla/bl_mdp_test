using FluentAssertions;
using WTW.MdpService.Domain.Members;

namespace WTW.MdpService.Test.Domain.Members;

public class RelationshipTest
{
    public void CanCreateRelationship()
    {
        var sut = new SdDomainList("RBS", "ZZY", "QWERT");

        sut.BusinessGroup.Should().Be("RBS");
        sut.Domain.Should().Be("ZZY");
        sut.ListValue.Should().Be("QWERT");
    }
}