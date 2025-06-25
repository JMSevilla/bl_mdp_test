using FluentAssertions;
using WTW.MdpService.Domain;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Domain;

public class EmailTest
{
    public void CreatesEmail_WhenValidEmailProvided()
    {
        var sut = new EmailBuilder()
            .EmailAddress(" tEST@test.com  ")
            .Build();

        sut.IsRight.Should().BeTrue();
        sut.IsLeft.Should().BeFalse();
        sut.Right().Address.Should().Be("test@test.com");
    }

    public void ReturnsError_WhenTooLongEmailIsProvided()
    {
        var sut = new EmailBuilder()
            .EmailAddress("testtesttesttesttesttesttesttesttesttesttesttest@test.com")
            .Build();

        sut.IsRight.Should().BeFalse();
        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("Email is required. Up to 50 characters length.");
    }

    public void ReturnsError_WhenEmailIsNull()
    {
        var sut = new EmailBuilder()
            .EmailAddress(null)
            .Build();

        sut.IsRight.Should().BeFalse();
        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("Email is required. Up to 50 characters length.");
    }

    public void ReturnsError_WhenInvalidEmailFormatIsProvided()
    {
        var sut = new EmailBuilder()
            .EmailAddress("test@test")
            .Build();

        sut.IsRight.Should().BeFalse();
        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("Invalid email address format.");
    }

    public void EmailsAreEqualWhenValuesAreEqual()
    {
        var sut1 = new EmailBuilder()
            .EmailAddress("test@test.com")
            .Build();
        var sut2 = new EmailBuilder()
            .EmailAddress("test@test.com")
            .Build();

        sut1.IsRight.Should().BeTrue();
        sut1.IsLeft.Should().BeFalse();
        sut2.IsRight.Should().BeTrue();
        sut2.IsLeft.Should().BeFalse();
        sut1.Right().Should().BeEquivalentTo(sut2.Right());
    }

    public void EmailsAreEqualWhenValuesAreEqual2()
    {
        var sut1 = new EmailBuilder()
            .EmailAddress("test@test.com")
            .Build()
            .Right();
        var sut2 = new EmailBuilder()
            .EmailAddress("test@test.com")
            .Build()
            .Right();

        (sut1 == sut2).Should().BeTrue();
    }

    public void EmailsAreEqualWhenValuesAreEqual3()
    {
        Email sut1 = null;
        Email sut2 = null;

        (sut1 == sut2).Should().BeTrue();
    }

    public void EmailsAreEqualWhenValuesAreEqual4()
    {
        var sut1 = new EmailBuilder()
            .EmailAddress("test@test.com")
            .Build();
        Email sut2 = null;

        (sut1.Right() == sut2).Should().BeFalse();
    }

    public void EmailsAreEqualWhenValuesAreEqual5()
    {
        var sut1 = new EmailBuilder()
            .EmailAddress("test@test.com")
            .Build();
        Email sut2 = null;

        (sut1.Right().Equals(sut2)).Should().BeFalse();
    }

    public void EmailsAreEqualWhenValuesAreEqual6()
    {
        var sut1 = new EmailBuilder()
            .EmailAddress("test@test.com")
            .Build();
        var sut2 = new PhoneBuilder()
            .FullNumber("44 123456789")
            .BuildFull();

        (sut1.Right().Equals(sut2.Right())).Should().BeFalse();
    }

    public void EmailsAreNotEqualWhenValuesAreNotEqual()
    {
        var sut1 = new EmailBuilder()
            .EmailAddress("test@test.com")
            .Build();
        var sut2 = new EmailBuilder()
            .EmailAddress("test1@test.com")
            .Build();

        sut1.IsRight.Should().BeTrue();
        sut1.IsLeft.Should().BeFalse();
        sut2.IsRight.Should().BeTrue();
        sut2.IsLeft.Should().BeFalse();
        sut1.Right().Should().NotBeEquivalentTo(sut2.Right());
    }

    public void EmailsAreNotEqualWhenValuesAreNotEqual2()
    {
        var sut1 = new EmailBuilder()
            .EmailAddress("test@test.com")
            .Build();
        var sut2 = new EmailBuilder()
            .EmailAddress("test1@test.com")
            .Build();

        (sut1.Right() != sut2.Right()).Should().BeTrue();
    }

    public void EmailsAreNotEqualWhenValuesAreNotEqual3()
    {
        var sut1 = new EmailBuilder()
            .EmailAddress("test@test.com")
            .Build();
        var sut2 = new EmailBuilder()
            .EmailAddress("test@test.com")
            .Build();

        (sut1.Right() != sut2.Right()).Should().BeFalse();
    }

    public void CreatesEmailWithNullValue()
    {
        var sut = Email.Empty();

        sut.Should().NotBeNull();
        sut.Address.Should().BeNull();
    }

    public void ClonesEmail()
    {
        var sut = new EmailBuilder()
            .EmailAddress("test@test.com")
            .Build();

        var clonedEmail = sut.Right().Clone();

        clonedEmail.Should().NotBeNull();
        clonedEmail.Should().BeEquivalentTo(sut.Right());
    }

    public void GetsHashCode()
    {
        var sut = new EmailBuilder()
            .EmailAddress("test@test.com")
            .Build();
        var sut2 = new EmailBuilder()
            .EmailAddress("test@test.com")
            .Build();

        sut.Right().GetHashCode().Should().Be(sut2.Right().GetHashCode());
    }

    public void GetsHashCode2()
    {
        Email sut = Email.Empty();

        sut.GetHashCode().Should().Be(23);
    }
}