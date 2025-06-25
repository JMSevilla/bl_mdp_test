using System;
using FluentAssertions;
using WTW.MdpService.Domain;
using WTW.MdpService.Domain.Common;
using WTW.MdpService.Domain.Members;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Domain.Members;

public class ContactReferenceTest
{
    public void CreatesContactReference()
    {
        var address = Address.Create("Towers Watson Westgate",
            "122-130 Station Road",
            "test dada for addres3",
            "Redhill",
            "Surrey",
            "United Kingdom",
            "GB",
            "RH1 1WS").Right();
        var now = DateTimeOffset.UtcNow;
        var nowDateWithoutTimestamp = new DateTimeOffset(now.Date, TimeSpan.Zero);
        var email = Email.Create("test@test.com").Right();
        var mobilePhone = Phone.Create("447544111111").Right();
        var contact = new Contact(address, email, mobilePhone, Contact.DataToCopy.Empty(), "RBS", 3141271);
        var authorization = new WTW.MdpService.Domain.Members.Authorization("RBS", "0003994", 1171111, now);

        var sut = new ContactReference(contact, authorization, "RBS", "0003994", 1, now);

        sut.Should().NotBeNull();
        sut.ReferenceNumber.Should().Be("0003994");
        sut.BusinessGroup.Should().Be("RBS");
        sut.StartDate.Should().Be(nowDateWithoutTimestamp);
        sut.EndDate.Should().BeNull();
        sut.SequenceNumber.Should().Be(1);
        sut.SchemeMemberIndicator.Should().Be("M");
        sut.AddressCode.Should().Be("GENERAL");
        sut.UseThisAddressForPayslips.Should().BeTrue();
        sut.Status.Should().Be("A");
        sut.Contact.Should().NotBeNull();
        sut.Contact.Address.Should().BeSameAs(address);
        sut.Contact.AddressNumber.Should().Be(3141271);
        sut.Contact.BusinessGroup.Should().Be("RBS");
        sut.Authorization.Should().NotBeNull();      
    }

    public void CanCloseContactReference()
    {
        var address = Address.Create("Towers Watson Westgate",
            "122-130 Station Road",
            "test dada for addres3",
            "Redhill",
            "Surrey",
            "United Kingdom",
            "GB",
            "RH1 1WS").Right();
        var closingDate = DateTimeOffset.UtcNow;
        var closingDateWithoutTimestamp = new DateTimeOffset(closingDate.Date, TimeSpan.Zero);
        var email = Email.Create("test@test.com").Right();
        var mobilePhone = Phone.Create("447544111111").Right();
        var contact = new Contact(address, email, mobilePhone, Contact.DataToCopy.Empty(), "RBS", 3141271);
        var authorization = new WTW.MdpService.Domain.Members.Authorization("RBS", "0003994", 1171111, DateTimeOffset.UtcNow.AddDays(-342));

        var sut = new ContactReference(contact, authorization, "RBS", "0003994", 1, DateTimeOffset.UtcNow.AddDays(-342));

        sut.Close(closingDate);

        sut.Should().NotBeNull();

        sut.EndDate.Should().Be(closingDateWithoutTimestamp.AddDays(-1));
    }   
}