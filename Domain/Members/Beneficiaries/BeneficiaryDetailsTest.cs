using System;
using FluentAssertions;
using WTW.MdpService.Domain.Members.Beneficiaries;
using WTW.TestCommon.FixieConfig;
using WTW.Web;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Domain.Members.Beneficiaries;

public class BeneficiaryDetailsTest
{
    public void BeneficiaryDetailsWithSamePropertyValuesAreEqual()
    {
        var sut1 = BeneficiaryDetails.CreateCharity("Unicef long charity name1", 12345678, 25, "note1");

        var sut2 = BeneficiaryDetails.CreateCharity("Unicef long charity name1", 12345678, 25, "note1");

        sut1.IsRight.Should().BeTrue();
        sut1.IsLeft.Should().BeFalse();
        sut2.IsRight.Should().BeTrue();
        sut2.IsLeft.Should().BeFalse();
        sut1.Should().Equal(sut2);
    }

    public void BeneficiaryDetailsWithDifferentPropertyValuesAreNotEqual()
    {
        var sut1 = BeneficiaryDetails.CreateCharity("Unicef long charity name1", 12345678, 25, "note1");

        var sut2 = BeneficiaryDetails.CreateCharity("Unicef long charity name2", 12345678, 25, "note1");

        sut1.IsRight.Should().BeTrue();
        sut1.IsLeft.Should().BeFalse();
        sut2.IsRight.Should().BeTrue();
        sut2.IsLeft.Should().BeFalse();
        sut1.Should().NotEqual(sut2);
    }

    [Input("Unicef long charity name", "UNICEF LONG CHARITY ")]
    [Input("Short one", "SHORT ONE")]
    public void CreatesCharityDetails_WhenValidDataProvided(string charityNameInput, string expectedReult)
    {
        var sut = BeneficiaryDetails.CreateCharity(charityNameInput, 12345678, 25, null);

        sut.IsRight.Should().BeTrue();
        sut.IsLeft.Should().BeFalse();
        sut.Right().Relationship.Should().Be(BeneficiaryDetails.CharityStatus);
        sut.Right().CharityName.Should().Be(charityNameInput);
        sut.Right().CharityNumber.Should().Be(12345678);
        sut.Right().LumpSumPercentage.Should().Be(25);
        sut.Right().Surname.Should().Be(expectedReult);
        sut.Right().Notes.Should().BeNull();

        sut.Right().Forenames.Should().BeNull();
        sut.Right().MixedCaseSurname.Should().BeNull();
        sut.Right().DateOfBirth.Should().BeNull();
        sut.Right().PensionPercentage.Should().BeNull();
    }

    public void ReturnsError_WhenNullCharityName()
    {
        var sut = BeneficiaryDetails.CreateCharity(null, 12345678, 25, "note1");

        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("\"charityName\" and \"charityNumber\" fields are required for charity beneficiaries.");
    }

    public void ReturnsError_WhenNullCharityNumber()
    {
        var sut = BeneficiaryDetails.CreateCharity("Unicef long charity name", null, 25, null);

        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("\"charityName\" and \"charityNumber\" fields are required for charity beneficiaries.");
    }

    [Input(99999999999)]
    [Input(0)]
    public void ReturnsError_WhenInvalidCharityNumberProvided(long charityNumber)
    {
        var sut = BeneficiaryDetails.CreateCharity("Unicef long charity name", charityNumber, 25, null);

        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("\"charityNumber\" value must be from 1 to 9999999999.");
    }

    [Input(125)]
    [Input(-1)]
    public void ReturnsError_WhenInvalidLumpSumpPercentage(int lumpSumPercentage)
    {
        var sut = BeneficiaryDetails.CreateCharity("Unicef long charity name", 12345678, lumpSumPercentage, null);

        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("\"lumpSumPercentage\" field must be between 0 and 100.");
    }

    public void ReturnsError_WhenInvalidCharityNameLength()
    {
        var sut = BeneficiaryDetails.CreateCharity("Unicef long charity nameUnicef long charity nameUnicef long charity nameUnicef long charity nameUnicef long charity nameUnicef long charity name", 12345678, 5, null);

        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("Charity name max length 120 characters.");
    }

    public void ReturnsError_WhenInvalidNotesLength()
    {
        var sut = BeneficiaryDetails.CreateCharity("Unicef", 12345678, 5, "notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars");

        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("Notes max length 180 characters.");
    }

    public void ReturnsError_WhenCharityNameContainsHTMLTags()
    {
        var sut = BeneficiaryDetails.CreateCharity("<p>Paragraph</p>", 12345678, 5, "Sample notes");

        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be(MdpConstants.InputContainingHTMLTagError);
    }

    [Input(null)]
    [Input(25)]
    public void CreatesNonCharityDetails_WhenValidDataProvided_AndPensionBeneficiary(int? age)
    {
        var sut = BeneficiaryDetails.CreateNonCharity("Wife", "Jane", "Doe", age.HasValue ? DateTime.Now.AddYears(-age.Value).Date : null, 25, true, "note1");

        sut.IsRight.Should().BeTrue();
        sut.IsLeft.Should().BeFalse();
        sut.Right().Relationship.Should().Be("Wife");
        sut.Right().Forenames.Should().Be("Jane");
        sut.Right().Surname.Should().Be("Doe".ToUpper());
        sut.Right().MixedCaseSurname.Should().Be("Doe");
        sut.Right().DateOfBirth.Should().Be(age.HasValue ? DateTime.Now.AddYears(-age.Value).Date : null);
        sut.Right().LumpSumPercentage.Should().Be(25);
        sut.Right().PensionPercentage.Should().Be(100);
        sut.Right().Notes.Should().Be("note1");

        sut.Right().CharityName.Should().BeNull();
        sut.Right().CharityNumber.Should().BeNull();
    }

    public void CreatesNonCharityDetails_WhenValidDataProvided_AndNotPensionBeneficiary()
    {
        var sut = BeneficiaryDetails.CreateNonCharity("Wife", "Jane", "Doe", DateTime.Now.AddYears(-25).Date, 25, false, null);

        sut.IsRight.Should().BeTrue();
        sut.IsLeft.Should().BeFalse();
        sut.Right().Relationship.Should().Be("Wife");
        sut.Right().Forenames.Should().Be("Jane");
        sut.Right().Surname.Should().Be("Doe".ToUpper());
        sut.Right().MixedCaseSurname.Should().Be("Doe");
        sut.Right().DateOfBirth.Should().Be(DateTime.Now.AddYears(-25).Date);
        sut.Right().LumpSumPercentage.Should().Be(25);
        sut.Right().PensionPercentage.Should().Be(0);
        sut.Right().Notes.Should().BeNull();

        sut.Right().CharityName.Should().BeNull();
        sut.Right().CharityNumber.Should().BeNull();
    }

    public void ThrowsError_WhenCharityRelationshipIsProvided()
    {
        var sut = () => BeneficiaryDetails.CreateNonCharity("Charity", "Jane", "Doe", DateTime.Now.AddYears(-25).Date, 25, false, "note1");

        sut.Should().Throw<InvalidOperationException>();
    }

    public void ReturnsError_WhenRelationshipIsNotProvided()
    {
        var sut = BeneficiaryDetails.CreateNonCharity(null, "Jane", "Doe", DateTime.Now.AddYears(-25).Date, 25, false, "note1");

        sut.Left().Message.Should().Be("relationship is required.");
    }

    public void ReturnsError_WhenInvalidRelationshipIsProvided()
    {
        var sut = BeneficiaryDetails.CreateNonCharity("some long relationship name", "Jane", "Doe", DateTime.Now.AddYears(-25).Date, 25, false, "note1");

        sut.Left().Message.Should().Be("Relationship status max length 20 characters.");
    }

    public void ReturnsError_WhenFornamesIsNull()
    {
        var sut = BeneficiaryDetails.CreateNonCharity("Wife", null, "Doe", DateTime.Now.AddYears(-25).Date, 25, false, "note1");

        sut.Left().Message.Should().Be("\"forenames\" and \"surname\" fields are required for non charity beneficiaries.");
    }

    public void ReturnsError_WhenSurnameIsNull()
    {
        var sut = BeneficiaryDetails.CreateNonCharity("Wife", "Jane", null, DateTime.Now.AddYears(-25).Date, 25, false, "note1");

        sut.Left().Message.Should().Be("\"forenames\" and \"surname\" fields are required for non charity beneficiaries.");
    }

    [Input(125)]
    [Input(-1)]
    public void ReturnsError_WhenInvalidLumpSump(int lumpSumPercentage)
    {
        var sut = BeneficiaryDetails.CreateNonCharity("Wife", "Jane", "Doe", DateTime.Now.AddYears(-25).Date, lumpSumPercentage, false, "note1");

        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("\"lumpSumPercentage\" field must be between 0 and 100.");
    }

    public void ReturnsError_WhenInvalidForenamesProvided()
    {
        var sut = BeneficiaryDetails.CreateNonCharity("Wife", "JaneJaneJaneJaneJaneJaneJaneJaneJaneJaneJaneJaneJane", "Doe", DateTime.Now.AddYears(-25).Date, 25, false, "note1");

        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("Forenames max length 32 characters.");
    }

    public void ReturnsError_WhenInvalidSurnameProvided()
    {
        var sut = BeneficiaryDetails.CreateNonCharity("Wife", "Jane", "DoeDoeDoeDoeDoeDoeDoeDoeDoeDoeDoeDoeDoeDoeDoeDoeDoeDoeDoeDoeDoeDoeDoeDoe", DateTime.Now.AddYears(-25).Date, 25, false, "note1");

        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("Surname max length 20 characters.");
    }

    public void ReturnsError_WhenInvalidNotesLengthProvided()
    {
        var sut = BeneficiaryDetails.CreateNonCharity("Wife", "Jane", "Doe", DateTime.Now.AddYears(-25).Date, 25, false, "notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars, notes over 500chars");

        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("Notes max length 180 characters.");
    }

    public void ReturnsError_WhenSurnameContainsHTMLTags()
    {
        var sut = BeneficiaryDetails.CreateNonCharity("Wife", "Jane", "<p>Paragraph</p>", DateTime.Now.AddYears(-25).Date, 25, false, "Sample notes");

        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be(MdpConstants.InputContainingHTMLTagError);
    }
}