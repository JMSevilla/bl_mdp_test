using FluentAssertions;
using WTW.MdpService.Domain.Members;

namespace WTW.MdpService.Test.Domain.Members;

public class IfaConfigurationTest
{
    public void CreatesIfaConfiguration()
    {
        var sut = new IfaConfiguration("RBS", "testname", "testtype", "test@wtw.com");

        sut.BusinessGroup.Should().Be("RBS");
        sut.IfaName.Should().Be("testname");
        sut.CalculationType.Should().Be("testtype");
        sut.IfaEmail.Should().Be("test@wtw.com");
    }
}