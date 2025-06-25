using System;
using FluentAssertions;
using WTW.MdpService.Domain.Mdp;

namespace WTW.MdpService.Test.Domain.Mdp;

public class RetirementPostIndexEventTest
{
    public void CreateRetirementPostIndexEvent()
    {
        var sut = new RetirementPostIndexEvent("RBS", "1111124", "123456", 123456, 654321);

        sut.BusinessGroup.Should().Be("RBS");
        sut.ReferenceNumber.Should().Be("1111124");
        sut.CaseNumber.Should().Be("123456");
        sut.BatchNumber.Should().Be(123456);
        sut.RetirementApplicationImageId.Should().Be(654321);
        sut.Error.Should().BeNull();
    }

    public void CanSetError()
    {
        var sut = new RetirementPostIndexEvent("RBS", "1111124", "123456", 123456, 654321);

        sut.SetError("Some error message");

        sut.Error.Should().Be("Some error message");
    }

    public void ReturnFormatedString()
    {
        var sut = new RetirementPostIndexEvent("RBS", "1111124", "123456", 123456, 654321);

        var result = sut.ToString();

        result.Should().Be("bgroup: RBS, refno: 1111124, caseno: 123456, batchno: 123456");
    }
}