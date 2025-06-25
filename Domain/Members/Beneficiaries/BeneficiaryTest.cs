using System;
using FluentAssertions;
using WTW.MdpService.Domain.Members.Beneficiaries;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Domain.Members.Beneficiaries;

public class BeneficiaryTest
{
    public void CreatesBeneficiary()
    {
        var address = BeneficiaryAddress.Create("Towers Watson Westgate",
            "122-130 Station Road",
            "test dada for addres3",
            "Redhill",
            "Surrey",
            "United Kingdom",
            "GB",
            "RH1 1WS").Right();
        var details = BeneficiaryDetails.CreateCharity("Unicef long charity name1", 12345678, 25, "note1").Right();
        var date = DateTimeOffset.UtcNow;

        var sut = new Beneficiary(1, address, details, date);

        sut.Address.Should().BeEquivalentTo(address);
        sut.BeneficiaryDetails.Should().BeEquivalentTo(details);
        sut.SequenceNumber.Should().Be(1);
        sut.NominationDate.Should().Be(date);
        sut.BusinessGroup.Should().BeNull();
        sut.ReferenceNumber.Should().BeNull();
    }

    public void ReturnsTrueIfCharityBeneficiary()
    {
        var address = BeneficiaryAddress.Create("Towers Watson Westgate",
            "122-130 Station Road",
            "test dada for addres3",
            "Redhill",
            "Surrey",
            "United Kingdom",
            "GB",
            "RH1 1WS").Right();
        var details = BeneficiaryDetails.CreateCharity("Unicef long charity name1", 12345678, 25, "note1").Right();
        var date = DateTimeOffset.UtcNow;
        var sut = new Beneficiary(1, address, details, date);

        var result = sut.IsCharity();

        result.Should().BeTrue();
    }

    public void ReturnsFalseIfNonCharityBeneficiary()
    {
        var address = BeneficiaryAddress.Create("Towers Watson Westgate",
            "122-130 Station Road",
            "test dada for addres3",
            "Redhill",
            "Surrey",
            "United Kingdom",
            "GB",
            "RH1 1WS").Right();
        var details = BeneficiaryDetails.CreateNonCharity("Wife", "Jane", "Doe", DateTime.Now.AddYears(-25).Date, 25, true, "note1").Right();
        var date = DateTimeOffset.UtcNow;
        var sut = new Beneficiary(1, address, details, date);

        var result = sut.IsCharity();

        result.Should().BeFalse();
    }

    public void ReturnsTrueIfPensionBeneficiary()
    {
        var address = BeneficiaryAddress.Create("Towers Watson Westgate",
            "122-130 Station Road",
            "test dada for addres3",
            "Redhill",
            "Surrey",
            "United Kingdom",
            "GB",
            "RH1 1WS").Right();
        var details = BeneficiaryDetails.CreateNonCharity("Wife", "Jane", "Doe", DateTime.Now.AddYears(-25).Date, 25, true, "note1").Right();
        var date = DateTimeOffset.UtcNow;
        var sut = new Beneficiary(1, address, details, date);

        var result = sut.IsPensionBeneficiary();

        result.Should().BeTrue();
    }

    public void ReturnsFalseIfNonPensionBeneficiary()
    {
        var address = BeneficiaryAddress.Create("Towers Watson Westgate",
            "122-130 Station Road",
            "test dada for addres3",
            "Redhill",
            "Surrey",
            "United Kingdom",
            "GB",
            "RH1 1WS").Right();
        var details = BeneficiaryDetails.CreateNonCharity("Wife", "Jane", "Doe", DateTime.Now.AddYears(-25).Date, 25, false, "note1").Right();
        var date = DateTimeOffset.UtcNow;
        var sut = new Beneficiary(1, address, details, date);

        var result = sut.IsPensionBeneficiary();

        result.Should().BeFalse();
    }

    public void RevokesBeneficiary()
    {
        var address = BeneficiaryAddress.Create("Towers Watson Westgate",
            "122-130 Station Road",
            "test dada for addres3",
            "Redhill",
            "Surrey",
            "United Kingdom",
            "GB",
            "RH1 1WS").Right();
        var details = BeneficiaryDetails.CreateNonCharity("Wife", "Jane", "Doe", DateTime.Now.AddYears(-25).Date, 25, false, "note1").Right();
        var date = DateTimeOffset.UtcNow.AddDays(-25);
        var revokeDate = DateTimeOffset.UtcNow;
        var sut = new Beneficiary(1, address, details, date);

        sut.Revoke(revokeDate);

        sut.RevokeDate.Should().Be(revokeDate);
    }
}