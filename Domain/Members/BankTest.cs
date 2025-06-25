using FluentAssertions;
using WTW.MdpService.Domain.Members;
using WTW.TestCommon.FixieConfig;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Domain.Members;

public class BankTest
{
    public void CreatesUkBank_WhenValidDataProvided()
    {
        var sut = Bank.CreateUkBank("123456", "Natwest", "Manchester");

        sut.IsRight.Should().BeTrue();
        sut.IsLeft.Should().BeFalse();
        sut.Right().SortCode.Should().Be("123456");
        sut.Right().Name.Should().Be("Natwest");
        sut.Right().City.Should().Be("Manchester");
        sut.Right().CountryCode.Should().Be("GB");
        sut.Right().Country.Should().Be("GB");
        sut.Right().Bic.Should().BeNull();
        sut.Right().ClearingCode.Should().BeNull();
    }

    [Input("12345")]
    [Input("1234567")]
    [Input(null)]
    [Input("")]
    [Input("  ")]
    public void ReturnsError_WhenInvalidSortCodeProvided(string sortCode)
    {
        var sut = Bank.CreateUkBank(sortCode, "Natwest", "Manchester");

        sut.IsRight.Should().BeFalse();
        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("Invalid Sort code: Must be 6 digit length.");
    }

    [Input("12345678901")]
    [Input(null)]
    public void CreatesNonUkBank_WhenValidDataProvided(string clearingCode)
    {
        var sut = Bank.CreateNonUkBank("12345678", clearingCode, "Natwest", "Manchester", "LT");

        sut.IsRight.Should().BeTrue();
        sut.IsLeft.Should().BeFalse();
        sut.Right().SortCode.Should().BeNull();
        sut.Right().Name.Should().Be("Natwest");
        sut.Right().City.Should().Be("Manchester");
        sut.Right().CountryCode.Should().Be("LT");
        sut.Right().Country.Should().Be("LT");
        sut.Right().Bic.Should().Be("12345678");
        sut.Right().ClearingCode.Should().Be(clearingCode);
    }

    [Input("1234567")]
    [Input("123456789")]
    [Input("123456789012")]
    [Input(null)]
    [Input("")]
    [Input("  ")]
    public void ReturnsError_WhenInvalidSortBicProvided(string bic)
    {
        var sut = Bank.CreateNonUkBank(bic, "12345678901", "Natwest", "Manchester", "LT");

        sut.IsRight.Should().BeFalse();
        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("Invalid BIC: Must be 8 or 11 digit length.");
    }

    [Input("")]
    [Input("  ")]
    [Input("123456789011212")]
    public void ReturnsError_WhenInvalidSortClearingCodeProvided(string clearingCode)
    {
        var sut = Bank.CreateNonUkBank("12345678", clearingCode, "Natwest", "Manchester", "LT");

        sut.IsRight.Should().BeFalse();
        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("Invalid Clearing code: Must be between 1 and 11 digit length or null.");
    }

    public void BankAreEqualIfDetailsMatched_WhenCreatingUkBank()
    {
        var sut1 = Bank.CreateUkBank("123456", "Natwest", "Manchester");
        var sut2 = Bank.CreateUkBank("123456", "Natwest", "Manchester");

        sut1.Right().Should().BeEquivalentTo(sut2.Right());
    }

    public void BankAreEqualIfDetailsMatched_WhenCreatingNonUkBank()
    {
        var sut1 = Bank.CreateNonUkBank("12345678", "123456", "Natwest", "Vilnius", "LT");
        var sut2 = Bank.CreateNonUkBank("12345678", "123456", "Natwest", "Vilnius", "LT");

        sut1.Right().Should().BeEquivalentTo(sut2.Right());
    }

    [Input(null, null)]
    [Input("   This is very very long bank name and it should be too long for any bank in any country in the world   ", "This is very very long bank name an")]
    public void TrimsLongBankName_WhenCreatingUkBank(string bankNameInput, string expectedResult)
    {
        var sut1 = Bank.CreateUkBank("123456", bankNameInput, "Manchester");

        sut1.Right().Name.Should().Be(expectedResult);
    }

    public void TrimsLongBankName_WhenCreatingNonUkBank()
    {
        var sut1 = Bank.CreateNonUkBank(
            "12345678",
            "123456",
            "   This is very very long bank name and it should be too long for any bank in any country in the world   ",
            "Vilnius",
            "LT");

        sut1.Right().Name.Length.Should().Be(35);
        sut1.Right().Name.Should().Be("This is very very long bank name an");
    }

    public void ReturnsFormatedSortCodeForUkBank()
    {
        var sut = Bank.CreateUkBank("123456", "Natwest", "Manchester");

        sut.IsRight.Should().BeTrue();
        sut.Right().GetDashFormatedSortCode().Should().Be("12-34-56");
    }

    public void ReturnsNullForSortCodeUsingNonUkBank()
    {
        var sut = Bank.CreateNonUkBank("12345678", "12345678901", "Natwest", "Manchester", "LT");

        sut.IsRight.Should().BeTrue();
        sut.Right().GetDashFormatedSortCode().Should().Be(null);
    }
}