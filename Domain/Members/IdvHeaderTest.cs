using System;
using FluentAssertions;
using WTW.MdpService.Domain.Common;
using WTW.MdpService.Domain.Members;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Domain.Members;

public class IdvHeaderTest
{
    public void CanCreateEventRti()
    {
        var now = DateTimeOffset.UtcNow;
        var address = Address.Create("Towers Watson Westgate",
            "122-130 Station Road",
            "test dada for addres3",
            "Redhill",
            "Surrey",
            "United Kingdom",
            "GB",
            "RH1 1WS").Right();
        var sut = IdvHeader.Create("RBS", "1111124", "schemeMember", 1, now, "111111", address, "44 123456789", "test@wtw.com", 123456, "Pass", "Driving License", 321456);

        sut.Should().NotBeNull();
        sut.BusinessGroup.Should().Be("RBS");
        sut.ReferenceNumber.Should().Be("1111124");
        sut.SchemeMember.Should().Be("schemeMember");
        sut.SequenceNumber.Should().Be(1);
        sut.Date.Should().Be(now);
        sut.CaseNumber.Should().Be("111111");
        sut.AddressLine1.Should().Be("Towers Watson Westgate");
        sut.AddressLine2.Should().Be("122-130 Station Road");
        sut.AddressLine3.Should().Be("test dada for addres3");
        sut.AddressLine4.Should().Be("Redhill");
        sut.AddressCity.Should().Be("Surrey");
        sut.AddressPostCode.Should().Be("RH1 1WS");
        sut.IssuingCountryCode.Should().Be("GB");
        sut.Phone.Should().Be("44 123456789");
        sut.Email.Should().Be("test@wtw.com");
        sut.Type.Should().Be("B");
        sut.Status.Should().Be("Y");
        sut.Detail.Should().NotBeNull();
    }
}