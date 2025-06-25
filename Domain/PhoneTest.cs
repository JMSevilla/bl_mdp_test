using FluentAssertions;
using WTW.MdpService.Domain;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Domain;

public class PhoneTest
{
    public void CreatesPhone_WhenValidPhoneNumberProvided()
    {
        var sut = new PhoneBuilder()
            .Number("447544111111")
            .BuildFull();

        sut.IsRight.Should().BeTrue();
        sut.IsLeft.Should().BeFalse();
        sut.Right().FullNumber.Should().Be("447544111111");
    }

    public void ReturnsError_WhenTooLongPhoneNumberIsProvided()
    {
        var sut = new PhoneBuilder()
            .FullNumber("4475441111111111111111111111111111111111111")
            .BuildFull();

        sut.IsRight.Should().BeFalse();
        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("Phone number is required. Up to 24 characters length.");
    }

    public void ReturnsError_WhenPhoneNumberIsNull()
    {
        var sut = new PhoneBuilder()
            .FullNumber(null)
            .BuildFull();

        sut.IsRight.Should().BeFalse();
        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("Phone number is required. Up to 24 characters length.");
    }

    public void ReturnsError_WhenInvalidPhoneNumberFormatIsProvided()
    {
        var sut = new PhoneBuilder()
            .FullNumber("g07544111111")
            .BuildFull();

        sut.IsRight.Should().BeFalse();
        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("Invalid phone number format.");
    }

    public void PhonesAreEqualWhenValuesAreEqual()
    {
        var sut1 = new PhoneBuilder()
            .FullNumber("447544111111")
            .BuildFull();
        var sut2 = new PhoneBuilder()
            .FullNumber("447544111111")
            .BuildFull();

        sut1.IsRight.Should().BeTrue();
        sut1.IsLeft.Should().BeFalse();
        sut2.IsRight.Should().BeTrue();
        sut2.IsLeft.Should().BeFalse();
        sut1.Right().Should().BeEquivalentTo(sut2.Right());
    }

    public void PhonesAreNotEqualWhenValuesAreNotEqual()
    {
        var sut1 = new PhoneBuilder()
            .FullNumber("447544111111")
            .BuildFull();
        var sut2 = new PhoneBuilder()
            .FullNumber("447544111112")
            .BuildFull();

        sut1.IsRight.Should().BeTrue();
        sut1.IsLeft.Should().BeFalse();
        sut2.IsRight.Should().BeTrue();
        sut2.IsLeft.Should().BeFalse();
        sut1.Right().Should().NotBeEquivalentTo(sut2.Right());
    }

    public void CreatesPhoneWithNullValue()
    {
        var sut = Phone.Empty();

        sut.Should().NotBeNull();
        sut.FullNumber.Should().BeNull();
    }

    public void ClonesPhone()
    {
        var sut = new PhoneBuilder()
            .FullNumber("447544111111")
            .BuildFull()
            .Right();

        var clonedEmail = sut.Clone();

        clonedEmail.Should().NotBeNull();
        clonedEmail.Should().BeEquivalentTo(sut);
    }

    public void CreatesPhoneFromCodeAndNumber_WhenValidPhoneNumberProvided()
    {
        var sut = new PhoneBuilder()
            .Code("44")
            .Number("7544111111")
            .Build();

        sut.IsRight.Should().BeTrue();
        sut.IsLeft.Should().BeFalse();
        sut.Right().FullNumber.Should().Be("44 7544111111");
    }

    public void ReturnsError_WhenInvalidCodeProvided()
    {
        var sut = new PhoneBuilder()
            .Code("+44")
            .Number("7544111111")
            .Build();

        sut.IsLeft.Should().BeTrue();
        sut.IsRight.Should().BeFalse();
        sut.Left().Message.Should().Be("Invalid code format.");
    }

    public void ReturnsError_WhenNullCodeProvided()
    {
        var sut = new PhoneBuilder()
            .Code(null)
            .Number("7544111111")
            .Build();

        sut.IsLeft.Should().BeTrue();
        sut.IsRight.Should().BeFalse();
        sut.Left().Message.Should().Be("Phone code is required. Up to 4 characters length.");
    }

    public void ReturnsError_WhenTooLongCodeProvided()
    {
        var sut = new PhoneBuilder()
            .Code("12345")
            .Number("7544111111")
            .Build();

        sut.IsLeft.Should().BeTrue();
        sut.IsRight.Should().BeFalse();
        sut.Left().Message.Should().Be("Phone code is required. Up to 4 characters length.");
    }

    public void ReturnsError_WhenInvalidNumberProvided()
    {
        var sut = new PhoneBuilder()
            .Code("44")
            .Number("+7544111111")
            .Build();

        sut.IsLeft.Should().BeTrue();
        sut.IsRight.Should().BeFalse();
        sut.Left().Message.Should().Be("Invalid number format. It must contain only numbers.");
    }

    public void ReturnsError_WhenNullNumberProvided()
    {
        var sut = new PhoneBuilder()
            .Code("44")
            .Number(null)
            .Build();

        sut.IsLeft.Should().BeTrue();
        sut.IsRight.Should().BeFalse();
        sut.Left().Message.Should().Be("Phone number is required. Up to 20 characters length.");
    }

    public void ReturnsError_WhenTooLongNumberProvided()
    {
        var sut = new PhoneBuilder()
            .Code("44")
            .Number("75441111111111111111111111111111111111")
            .Build();

        sut.IsLeft.Should().BeTrue();
        sut.IsRight.Should().BeFalse();
        sut.Left().Message.Should().Be("Phone number is required. Up to 20 characters length.");
    }

    public void ReturnsPhoneNumberCode()
    {
        var sut = new PhoneBuilder()
            .Code("44")
            .Number("7544111111")
            .Build()
            .Right();

        var result = sut.Code();

        result.Should().Be("44");
    }

    public void ReturnsPhoneNumberCodeNull()
    {
        var sut = Phone.Empty();

        var result = sut.Code();

        result.Should().BeNull();
    }

    public void ReturnsPhoneNumber()
    {
        var sut = new PhoneBuilder()
            .Code("44")
            .Number("7544111111")
            .Build()
            .Right();

        var result = sut.Number();

        result.Should().Be("7544111111");
    }

    public void ReturnsPhoneNumberNull()
    {
        var sut = Phone.Empty();

        var result = sut.Number();

        result.Should().BeNull();
    }
}