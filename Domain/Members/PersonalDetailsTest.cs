using System;
using FluentAssertions;
using WTW.MdpService.Domain.Members;
using WTW.TestCommon.FixieConfig;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Domain.Members;

public class PersonalDetailsTest
{
    [Input(default, default, default)]
    [Input("Mr", "John F.", "Doe")]
    public void CreatesPerson_WhenValidDataProvided(string title, string fornames, string surnames)
    {
        var dob = DateTimeOffset.UtcNow.AddYears(-33);
        var sut = PersonalDetails.Create(title, "M", fornames, surnames, dob);

        sut.IsRight.Should().BeTrue();
        sut.IsLeft.Should().BeFalse();
        sut.Right().Title.Should().Be(title);
        sut.Right().Gender.Should().Be("M");
        sut.Right().Forenames.Should().Be(fornames);
        sut.Right().Surname.Should().Be(surnames);
        sut.Right().DateOfBirth.Should().Be(dob);
    }

    public void ReturnsError_WhenInvalidTitleProvided()
    {
        var dob = DateTimeOffset.UtcNow.AddYears(-33);
        var sut = PersonalDetails.Create("Mrrrrrrrrrrrrrrrrrrrrr", "M", "John F.", "Doe", dob);

        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("Title must be less than 10 characters lenght.");
    }

    [Input(default)]
    [Input("Ono")]
    public void ReturnsError_WhenInvalidGenderProvided(string gender)
    {
        var dob = DateTimeOffset.UtcNow.AddYears(-33);
        var sut = PersonalDetails.Create("Mr", gender, "John F.", "Doe", dob);

        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("Invalid gender provided: It must be F for female or M for male.");
    }

    public void ReturnsError_WhenInvalidForenamesProvided()
    {
        var dob = DateTimeOffset.UtcNow.AddYears(-33);
        var sut = PersonalDetails.Create("Mr", "M", "Too long forenames restricted by db up to 32 chars", "Doe", dob);

        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("Forenames must be less than 32 characters lenght.");
    }

    public void ReturnsError_WhenInvalidSurnameProvided()
    {
        var dob = DateTimeOffset.UtcNow.AddYears(-33);
        var sut = PersonalDetails.Create("Mr", "M", "John F.", "Too long surname restricted by db up to 20 chars", dob);

        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("Surname must be less than 20 characters lenght.");
    }

    public void ReturnsError_WhenInvalidFateOfBirhProvided()
    {
        var dob = DateTimeOffset.UtcNow.AddDays(1);
        var sut = PersonalDetails.Create("Mr", "M", "John F.", "Doe", dob);

        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("Invalid date of birth: Future date unable to be date of birth.");
    }

    public void PersonalDetailsAreEqualWhenValuesAreEqual()
    {
        var dob = DateTimeOffset.UtcNow.AddYears(-33);
        var sut1 = PersonalDetails.Create("Mr", "M", "John F.", "Doe", dob);
        var sut2 = PersonalDetails.Create("Mr", "M", "John F.", "Doe", dob);


        sut1.IsRight.Should().BeTrue();
        sut1.IsLeft.Should().BeFalse();
        sut2.IsRight.Should().BeTrue();
        sut2.IsLeft.Should().BeFalse();
        sut1.Right().Should().BeEquivalentTo(sut2.Right());
    }

    public void PersonalDetailsAreNotEqualWhenValuesAreNotEqual()
    {
        var dob = DateTimeOffset.UtcNow.AddYears(-33);
        var sut1 = PersonalDetails.Create("Mr", "M", "John F.", "Doe", dob);
        var sut2 = PersonalDetails.Create("Mr", "M", "John F.", "Doe1", dob);


        sut1.IsRight.Should().BeTrue();
        sut1.IsLeft.Should().BeFalse();
        sut2.IsRight.Should().BeTrue();
        sut2.IsLeft.Should().BeFalse();
        sut1.Right().Should().NotBeEquivalentTo(sut2.Right());
    }
}