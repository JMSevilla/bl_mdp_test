using FluentAssertions;
using WTW.MdpService.Domain.Members.Beneficiaries;
using WTW.Web;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Domain.Members.Beneficiaries;

public class BeneficiaryAddressTest
{
    public void CreatesAddress_WhenValidDataProvided()
    {
        var sut = BeneficiaryAddress.Create("Towers Watson Westgate",
            "122-130 Station Road",
            "test dada for addres3",
            "Redhill",
            "Surrey",
            "United Kingdom",
            "GB",
            "RH1 1WS");

        sut.IsRight.Should().BeTrue();
        sut.IsLeft.Should().BeFalse();
        sut.Right().Line1.Should().Be("Towers Watson Westgate");
        sut.Right().Line2.Should().Be("122-130 Station Road");
        sut.Right().Line3.Should().Be("test dada for addres3");
        sut.Right().Line4.Should().Be("Redhill");
        sut.Right().Line5.Should().Be("Surrey");
        sut.Right().Country.Should().Be("United Kingdom");
        sut.Right().CountryCode.Should().Be("GB");
        sut.Right().PostCode.Should().Be("RH1 1WS");
    }

    public void CreatesAddress_WhenValidDataProvided2()
    {
        var sut = BeneficiaryAddress.Create(null, null, null, null, null, null, null, null);

        sut.IsRight.Should().BeTrue();
        sut.IsLeft.Should().BeFalse();
        sut.Right().Line1.Should().BeNull();
        sut.Right().Line2.Should().BeNull();
        sut.Right().Line3.Should().BeNull();
        sut.Right().Line4.Should().BeNull();
        sut.Right().Line5.Should().BeNull();
        sut.Right().Country.Should().BeNull();
        sut.Right().CountryCode.Should().BeNull();
        sut.Right().PostCode.Should().BeNull();
    }

    public void AddressWithSamePropertyValuesAreEqual()
    {
        var sut1 = BeneficiaryAddress.Create("Towers Watson Westgate",
            "122-130 Station Road",
            "test dada for addres3",
            "Redhill",
            "Surrey",
            "United Kingdom",
            "GB",
            "RH1 1WS");

        var sut2 = BeneficiaryAddress.Create("Towers Watson Westgate",
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
        var sut1 = BeneficiaryAddress.Create("Towers Watson Westgate1",
            "122-130 Station Road",
            "test dada for addres3",
            "Redhill",
            "Surrey",
            "United Kingdom",
            "GB",
            "RH1 1WS");

        var sut2 = BeneficiaryAddress.Create("Towers Watson Westgate",
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
        var sut = BeneficiaryAddress.Create("Towers Watson Westgate Towers Watson Westgate Towers Watson Westgate Towers Watson Westgate",
            "122-130 Station Road",
            "test dada for addres3",
            "Redhill",
            "Surrey",
            "United Kingdom",
            "GB",
            "RH1 1WS");

        sut.IsRight.Should().BeFalse();
        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("\'Address Line1\' must be up to 25 characters length.");
    }

    public void ReturnsError_WhenInvalidAddress2Provided()
    {
        var sut = BeneficiaryAddress.Create("Towers Watson Westgate",
            "122-130 Station Road 122-130 Station Road 122-130 Station Road 122-130 Station Road",
            "test dada for addres3",
            "Redhill",
            "Surrey",
            "United Kingdom",
            "GB",
            "RH1 1WS");

        sut.IsRight.Should().BeFalse();
        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("\'Address Line2\' must be up to 25 characters length.");
    }

    public void ReturnsError_WhenInvalidAddress3Provided()
    {
        var sut = BeneficiaryAddress.Create("Towers Watson Westgate",
            "122-130 Station Road",
            " test dada for addres3 test dada for addres3 test dada for addres3 test dada for addres3",
            "Redhill",
            "Surrey",
            "United Kingdom",
            "GB",
            "RH1 1WS");

        sut.IsRight.Should().BeFalse();
        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("\'Address Line3\' must be up to 25 characters length.");
    }

    public void ReturnsError_WhenInvalidAddress4Provided()
    {
        var sut = BeneficiaryAddress.Create("Towers Watson Westgate",
            "122-130 Station Road",
            "test dada for addres3",
            "Redhill Redhill Redhill Redhill Redhill Redhill Redhill Redhill Redhill Redhill Redhill Redhill",
            "Surrey",
            "United Kingdom",
            "GB",
            "RH1 1WS");

        sut.IsRight.Should().BeFalse();
        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("\'Address Line4\' must be up to 25 characters length.");
    }

    public void ReturnsError_WhenInvalidAddress5Provided()
    {
        var sut = BeneficiaryAddress.Create("Towers Watson Westgate",
            "122-130 Station Road",
            "test dada for addres3",
            "Redhill",
            "Surrey Surrey Surrey Surrey Surrey Surrey Surrey Surrey Surrey Surrey Surrey Surrey Surrey",
            "United Kingdom",
            "GB",
            "RH1 1WS");

        sut.IsRight.Should().BeFalse();
        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("\'Address Line5\' must be up to 25 characters length.");
    }

    public void ReturnsError_WhenInvalidCountryProvided()
    {
        var sut = BeneficiaryAddress.Create("Towers Watson Westgate",
            "122-130 Station Road",
            "test dada for addres3",
            "Redhill",
            "Surrey",
            "United Kingdom United Kingdom United Kingdom United Kingdom United Kingdom",
            "GB",
            "RH1 1WS");

        sut.IsRight.Should().BeFalse();
        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("Country must be up to 25 characters length.");
    }

    public void ReturnsError_WhenInvalidCountryCodeProvided()
    {
        var sut = BeneficiaryAddress.Create("Towers Watson Westgate",
            "122-130 Station Road",
            "test dada for addres3",
            "Redhill",
            "Surrey",
            "United",
            "Great Britain",
            "RH1 1WS");

        sut.IsRight.Should().BeFalse();
        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("CountryCode must be up 3 characters length.");
    }

    public void ReturnsError_WhenInvalidPostCodeProvided()
    {
        var sut = BeneficiaryAddress.Create("Towers Watson Westgate",
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

    public void ReturnsError_WithDataProvidedThatHasHTMLTagsInLine1()
    {
        var sut = BeneficiaryAddress.Create("<p>Paragraph</p>",
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

    public void CreatesAddressWithNullValues()
    {
        var sut = BeneficiaryAddress.Empty();

        sut.Should().NotBeNull();
        sut.Line1.Should().BeNull();
        sut.Line2.Should().BeNull();
        sut.Line3.Should().BeNull();
        sut.Line4.Should().BeNull();
        sut.Line5.Should().BeNull();
        sut.Country.Should().BeNull();
        sut.CountryCode.Should().BeNull();
        sut.PostCode.Should().BeNull();
    }
}