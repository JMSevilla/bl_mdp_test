using System;
using FluentAssertions;
using WTW.MdpService.Domain;
using WTW.MdpService.Domain.Common;
using WTW.MdpService.Domain.Mdp;
using WTW.TestCommon.FixieConfig;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Domain.Mdp.Journeys;

public class TransferJourneyContactTest
{
    [Input(null, null)]
    [Input("", "")]
    [Input("  ", "    ")]
    [Input("tesName", "testCompanyName")]
    public void CanCreateTransferJourneyContact(string name, string companyName)
    {
        var date = DateTimeOffset.UtcNow;

        var transferJourneyContact = new TransferJourneyContactFactory();
        var sut = transferJourneyContact.Create(
                     name,
                     "Advisor name",
                     companyName,
                     Email.Create("test@gmail.com").Right(),
                     Phone.Create("370", "67878788").Right(),
                     "Ifa",
                     "Scheme name",
                     date).Right();


        sut.Type.Should().Be("Ifa");
        sut.Name.Should().Be(name);
        sut.CompanyName.Should().Be(companyName);
        sut.CreatedAt.Should().Be(date);
        sut.Email.Address.Should().Be("test@gmail.com");
        sut.Phone.FullNumber.Should().Be("370 67878788");
        sut.Address.Should().Be(Address.Empty());
        sut.SchemeName.Should().Be("Scheme name");
        sut.AdvisorName.Should().Be("Advisor name");
    }

    [Input("test", "Test Test Test Test Test Test Test Test Test Test Test Test ", "Company name must be up to 50 characters length.")]
    [Input("Test Test Test Test Test Test Test Test Test Test Test Test ", "test", "Name must be up to 50 characters length.")]
    public void ReturnsError_OnTransferJourneyContactCreation_WhenDataIsInvalid(string name, string companyName, string error)
    {
        var date = DateTimeOffset.UtcNow;

        var transferJourneyContact = new TransferJourneyContactFactory();
        var sut = transferJourneyContact.Create(
                     name,
                     null,
                     companyName,
                     Email.Create("test@gmail.com").Right(),
                     Phone.Create("370", "67878788").Right(),
                     "Ifa",
                     null,
                     date).Left();

        sut.Message.Should().Be(error);       
    }

    public void CanSubmitTransferJourneyContactAddress()
    {
        var date = DateTimeOffset.UtcNow;

        var transferJourneyContact = new TransferJourneyContactFactory();
        var sut = transferJourneyContact.Create(
                     "test",
                     null,
                     "test comp name",
                     Email.Create("test@gmail.com").Right(),
                     Phone.Create("370", "67878788").Right(),
                     "Ifa",
                     null,
                     date).Right();

        sut.SubmitAddress(Address.Create("Towers Watson Westgate",
            "122-130 Station Road",
            "test dada for addres3",
            "Redhill",
            "Surrey",
            "United Kingdom",
            "GB",
            "RH1 1WS").Right());


        sut.Type.Should().Be("Ifa");
        sut.Name.Should().Be("test");
        sut.CompanyName.Should().Be("test comp name");
        sut.CreatedAt.Should().Be(date);
        sut.Email.Address.Should().Be("test@gmail.com");
        sut.Phone.FullNumber.Should().Be("370 67878788");
        sut.Address.StreetAddress1.Should().Be("Towers Watson Westgate");
        sut.Address.StreetAddress2.Should().Be("122-130 Station Road");
        sut.Address.StreetAddress3.Should().Be("test dada for addres3");
        sut.Address.StreetAddress4.Should().Be("Redhill");
        sut.Address.StreetAddress5.Should().Be("Surrey");
        sut.Address.Country.Should().Be("United Kingdom");
        sut.Address.CountryCode.Should().Be("GB");
        sut.Address.PostCode.Should().Be("RH1 1WS");
    }
}