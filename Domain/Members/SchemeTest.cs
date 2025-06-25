using FluentAssertions;
using WTW.MdpService.Domain.Members;

namespace WTW.MdpService.Test.Domain.Members;

public class SchemeTest
{
    public void CreatesScheme()
    {
        var sut = new Scheme("GBP", "SchemeName", "SomeType");

        sut.BaseCurrency.Should().Be("GBP");
        sut.Name.Should().Be("SchemeName");
        sut.Type.Should().Be("SomeType");
    }
}