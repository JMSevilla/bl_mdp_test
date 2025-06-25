using FluentAssertions;
using WTW.MdpService.Domain;
using WTW.MdpService.Domain.Common;
using WTW.MdpService.Domain.Members;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Domain.Members;

public class ContactTest
{
    public void CreatesContact()
    {
        var address = Address.Create("Towers Watson Westgate",
            "122-130 Station Road",
            "test dada for addres3",
            "Redhill",
            "Surrey",
            "United Kingdom",
            "GB",
            "RH1 1WS").Right();
        var email = Email.Create("test@test.com").Right();
        var mobilePhone = Phone.Create("447544111111").Right();

        var sut = new Contact(address, email, mobilePhone, Contact.DataToCopy.Empty(), "RBS", 34937774);

        sut.Should().NotBeNull();
        sut.Address.Should().BeSameAs(address);
        sut.Email.Should().BeSameAs(email);
        sut.BusinessGroup.Should().Be("RBS");
        sut.AddressNumber.Should().Be(34937774);
        sut.Data.Should().Be(Contact.DataToCopy.Empty());
    }

    public void CanCreateEmptyContactDataToCopy()
    {
        var sut = Contact.DataToCopy.Empty();

        sut.Should().NotBeNull();
        sut.Telephone.Should().BeNull();
        sut.Fax.Should().BeNull();
        sut.OrganizationName.Should().BeNull();
        sut.WorkMail.Should().BeNull();
        sut.HomeMail.Should().BeNull();
        sut.WorkPhone.Should().BeNull();
        sut.HomePhone.Should().BeNull();
        sut.MobilePhone2.Should().BeNull();
        sut.NonUkPostCode.Should().BeNull();
    }

    public void CanCloneContactDataToCopy()
    {
        var sut = Contact.DataToCopy.Empty();

        var result = sut.Clone();

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(sut);
        result.Telephone.Should().BeNull();
        result.Fax.Should().BeNull();
        result.OrganizationName.Should().BeNull();
        result.WorkMail.Should().BeNull();
        result.HomeMail.Should().BeNull();
        result.WorkPhone.Should().BeNull();
        result.HomePhone.Should().BeNull();
        result.MobilePhone2.Should().BeNull();
        result.NonUkPostCode.Should().BeNull();
    }
}