using FluentAssertions;
using WTW.MdpService.Domain.Common;
using WTW.Web;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Domain.Members;

public class AddressTest
{
    public void CreatesAddress_WhenValidDataProvided()
    {
        var sut = Address.Create("Towers Watson Westgate",
            "122-130 Station Road",
            "test dada for addres3",
            "Redhill",
            "Surrey",
            "United Kingdom",
            "GB",
            "RH1 1WS");

        sut.IsRight.Should().BeTrue();
        sut.IsLeft.Should().BeFalse();
        sut.Right().StreetAddress1.Should().Be("Towers Watson Westgate");
        sut.Right().StreetAddress2.Should().Be("122-130 Station Road");
        sut.Right().StreetAddress3.Should().Be("test dada for addres3");
        sut.Right().StreetAddress4.Should().Be("Redhill");
        sut.Right().StreetAddress5.Should().Be("Surrey");
        sut.Right().Country.Should().Be("United Kingdom");
        sut.Right().CountryCode.Should().Be("GB");
        sut.Right().PostCode.Should().Be("RH1 1WS");
    }

    public void CreatesAddress_WhenValidDataProvided2()
    {
        var sut = Address.Create("Towers Watson Westgate", null, null, null, null, null, "GB", null);

        sut.IsRight.Should().BeTrue();
        sut.IsLeft.Should().BeFalse();
        sut.Right().StreetAddress1.Should().Be("Towers Watson Westgate");
        sut.Right().StreetAddress2.Should().BeNull();
        sut.Right().StreetAddress3.Should().BeNull();
        sut.Right().StreetAddress4.Should().BeNull();
        sut.Right().StreetAddress5.Should().BeNull();
        sut.Right().Country.Should().BeNull();
        sut.Right().CountryCode.Should().Be("GB");
        sut.Right().PostCode.Should().BeNull();
    }

    public void AddressWithSamePropertyValuesAreEqual()
    {
        var sut1 = Address.Create("Towers Watson Westgate",
            "122-130 Station Road",
            "test dada for addres3",
            "Redhill",
            "Surrey",
            "United Kingdom",
            "GB",
            "RH1 1WS");

        var sut2 = Address.Create("Towers Watson Westgate",
            "122-130 Station Road",
            "test dada for addres3",
            "Redhill",
            "Surrey",
            "United Kingdom",
            "GB",
            "RH1 1WS");

        sut1.IsRight.Should().BeTrue();
        sut1.IsLeft.Should().BeFalse();
        sut2.IsRight.Should().BeTrue();
        sut2.IsLeft.Should().BeFalse();
        sut1.Should().Equal(sut2);
    }

    public void AddressWithDifferentPropertyValuesAreNotEqual()
    {
        var sut1 = Address.Create("Towers Watson Westgate1",
            "122-130 Station Road",
            "test dada for addres3",
            "Redhill",
            "Surrey",
            "United Kingdom",
            "GB",
            "RH1 1WS");

        var sut2 = Address.Create("Towers Watson Westgate",
            "122-130 Station Road",
            "test dada for addres3",
            "Redhill",
            "Surrey",
            "United Kingdom",
            "GB",
            "RH1 1WS");

        sut1.IsRight.Should().BeTrue();
        sut1.IsLeft.Should().BeFalse();
        sut2.IsRight.Should().BeTrue();
        sut2.IsLeft.Should().BeFalse();
        sut1.Should().NotEqual(sut2);
    }

    public void ReturnsError_WhenInvalidAddress1Provided()
    {
        var sut = Address.Create("Towers Watson Westgate Towers Watson Westgate Towers Watson Westgate Towers Watson Westgate",
            "122-130 Station Road",
            "test dada for addres3",
            "Redhill",
            "Surrey",
            "United Kingdom",
            "GB",
            "RH1 1WS");

        sut.IsRight.Should().BeFalse();
        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("StreetAddress1 must be up to 50 characters length.");
    }

    public void ReturnsError_WhenInvalidAddress2Provided()
    {
        var sut = Address.Create("Towers Watson Westgate",
            "122-130 Station Road 122-130 Station Road 122-130 Station Road 122-130 Station Road",
            "test dada for addres3",
            "Redhill",
            "Surrey",
            "United Kingdom",
            "GB",
            "RH1 1WS");

        sut.IsRight.Should().BeFalse();
        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("StreetAddress2 must be up to 50 characters length.");
    }

    public void ReturnsError_WhenInvalidAddress3Provided()
    {
        var sut = Address.Create("Towers Watson Westgate",
            "122-130 Station Road",
            " test dada for addres3 test dada for addres3 test dada for addres3 test dada for addres3",
            "Redhill",
            "Surrey",
            "United Kingdom",
            "GB",
            "RH1 1WS");

        sut.IsRight.Should().BeFalse();
        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("StreetAddress3 must be up to 50 characters length.");
    }

    public void ReturnsError_WhenInvalidAddress4Provided()
    {
        var sut = Address.Create("Towers Watson Westgate",
            "122-130 Station Road",
            "test dada for addres3",
            "Redhill Redhill Redhill Redhill Redhill Redhill Redhill Redhill Redhill Redhill Redhill Redhill",
            "Surrey",
            "United Kingdom",
            "GB",
            "RH1 1WS");

        sut.IsRight.Should().BeFalse();
        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("StreetAddress4 must be up to 50 characters length.");
    }

    public void ReturnsError_WhenInvalidAddress5Provided()
    {
        var sut = Address.Create("Towers Watson Westgate",
            "122-130 Station Road",
            "test dada for addres3",
            "Redhill",
            "Surrey Surrey Surrey Surrey Surrey Surrey Surrey Surrey Surrey Surrey Surrey Surrey Surrey",
            "United Kingdom",
            "GB",
            "RH1 1WS");

        sut.IsRight.Should().BeFalse();
        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("StreetAddress5 must be up to 50 characters length.");
    }

    public void ReturnsError_WhenInvalidCountryProvided()
    {
        var sut = Address.Create("Towers Watson Westgate",
            "122-130 Station Road",
            "test dada for addres3",
            "Redhill",
            "Surrey",
            "United Kingdom United Kingdom United Kingdom United Kingdom United Kingdom",
            "GB",
            "RH1 1WS");

        sut.IsRight.Should().BeFalse();
        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("Country must be up to 30 characters length.");
    }

    public void ReturnsError_WhenInvalidCountryCodeProvided()
    {
        var sut = Address.Create("Towers Watson Westgate",
            "122-130 Station Road",
            "test dada for addres3",
            "Redhill",
            "Surrey",
            "United",
            "Great Britain",
            "RH1 1WS");

        sut.IsRight.Should().BeFalse();
        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("CountryCode must be up to 3 characters length.");
    }

    public void ReturnsError_WhenInvalidPostCodeProvided()
    {
        var sut = Address.Create("Towers Watson Westgate",
            "122-130 Station Road",
            "test dada for addres3",
            "Redhill",
            "Surrey",
            "United",
            "GB",
            "RH1 1WS RH1 1WS");

        sut.IsRight.Should().BeFalse();
        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("PostCode must be up to 8 characters length.");
    }

    public void ClonesAddress()
    {
        var sut = Address.Create("Towers Watson Westgate",
            "122-130 Station Road",
            "test dada for addres3",
            "Redhill",
            "Surrey",
            "United Kingdom",
            "GB",
            "RH1 1WS").Right();

        var clonedEmail = sut.Clone();

        clonedEmail.Should().NotBeNull();
        clonedEmail.Should().BeEquivalentTo(sut);
    }

    public void CreatesAddressWithNullValues()
    {
        var sut = Address.Empty();

        sut.Should().NotBeNull();
        sut.StreetAddress1.Should().BeNull();
        sut.StreetAddress2.Should().BeNull();
        sut.StreetAddress3.Should().BeNull();
        sut.StreetAddress4.Should().BeNull();
        sut.StreetAddress5.Should().BeNull();
        sut.Country.Should().BeNull();
        sut.CountryCode.Should().BeNull();
        sut.PostCode.Should().BeNull();
    }

    public void ReturnsError_WithDataProvidedThatHasHTMLTagsInStreetAddress1()
    {
        var sut = Address.Create("<script>alert('test');</script><p>Paragraph</p>",
            "122-130 Station Road",
            "test dada for addres3",
            "Redhill",
            "Surrey",
            "United Kingdom",
            "GB",
            "RH1 1WS");

        sut.IsRight.Should().BeFalse();
        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be(MdpConstants.InputContainingHTMLTagError);
    }
}