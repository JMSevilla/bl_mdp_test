using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using LanguageExt.Common;
using LanguageExt.Pipes;
using WTW.MdpService.Domain;
using WTW.MdpService.Domain.Common;
using WTW.MdpService.Domain.Mdp.Calculations;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.Domain.Members.Beneficiaries;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.MdpService.Retirement;
using WTW.MdpService.Test.Domain.Mdp.Calculations;
using WTW.TestCommon.FixieConfig;
using WTW.Web.LanguageExt;
using WTW.Web.Serialization;

namespace WTW.MdpService.Test.Domain.Members;

public class MemberTest
{
    public void CreatesMember()
    {
        var sut = new MemberBuilder().Build();

        sut.StatusCode.Should().Be("testStatusCode");
        sut.ComplaintInticator.Should().Be("testcomplaintInticator");
        sut.Dependants.Should().BeEmpty();
    }

    public void CalculateMemberTermToRetirement()
    {
        const int normalRetirementAge = 60;
        var dateOfBirth = new DateTimeOffset(new DateTime(1985, 10, 10));
        var now = new DateTimeOffset(new DateTime(2021, 12, 10));
        var member = new MemberBuilder().NormalRetirementAge(normalRetirementAge).DateOfBirth(dateOfBirth).Build();

        (var years, var months) = member.CalculateTermToRetirement(now, dateOfBirth.AddYears(normalRetirementAge));

        years.Should().Be(23);
        months.Should().Be(10);
    }

    public void CalculateMemberTermToRetirement_RoundMonthUp()
    {
        const int normalRetirementAge = 60;
        var dateOfBirth = new DateTimeOffset(new DateTime(1985, 10, 10));
        var now = new DateTimeOffset(new DateTime(2021, 9, 21));
        var member = new MemberBuilder().NormalRetirementAge(normalRetirementAge).DateOfBirth(dateOfBirth).Build();

        (var years, var months) = member.CalculateTermToRetirement(now, dateOfBirth.AddYears(normalRetirementAge));

        years.Should().Be(24);
        months.Should().Be(1);
    }

    public void CalculateMemberTermToRetirement_OneMonthLeft()
    {
        const int normalRetirementAge = 60;
        var dateOfBirth = new DateTimeOffset(new DateTime(1985, 10, 10));
        var now = new DateTimeOffset(new DateTime(2045, 9, 21));
        var member = new MemberBuilder().NormalRetirementAge(normalRetirementAge).DateOfBirth(dateOfBirth).Build();

        (var years, var months) = member.CalculateTermToRetirement(now, dateOfBirth.AddYears(normalRetirementAge));

        years.Should().Be(0);
        months.Should().Be(1);
    }

    public void CalculateMemberTermToRetirement_NoTimeLeft()
    {
        const int normalRetirementAge = 60;
        var dateOfBirth = new DateTimeOffset(new DateTime(1985, 10, 10));
        var now = new DateTimeOffset(new DateTime(2046, 9, 21));
        var member = new MemberBuilder().NormalRetirementAge(normalRetirementAge).DateOfBirth(dateOfBirth).Build();

        (var years, var months) = member.CalculateTermToRetirement(now, dateOfBirth.AddYears(normalRetirementAge));

        years.Should().Be(0);
        months.Should().Be(0);
    }

    public void GetAgeLines_MemberIsOlderThanEarliestRetirementAgeAndNormalRetirementAge()
    {
        var dateOfBirth = new DateTimeOffset(new DateTime(1961, 10, 10));
        var now = new DateTimeOffset(new DateTime(2021, 12, 10));

        var member = new MemberBuilder()
            .DateOfBirth(dateOfBirth)
            .Build();

        var lineStages = member.GetAgeLines(now, 55, 60);
        lineStages.Count().Should().Be(5);

        lineStages.Should().BeEquivalentTo(new List<int> { 61, 62, 63, 64, 65 });
    }

    public void GetAgeLines_MemberIsYoungerThanNormalRetirementAge()
    {
        var dateOfBirth = new DateTimeOffset(new DateTime(1991, 10, 10));
        var now = new DateTimeOffset(new DateTime(2021, 12, 10));

        var member = new MemberBuilder()
            .DateOfBirth(dateOfBirth)
            .Build();

        var lineStages = member.GetAgeLines(now, 55, 60);
        lineStages.Count().Should().Be(5);

        lineStages.Should().BeEquivalentTo(new List<int> { 55, 56, 57, 58, 59 });
    }

    public void GetAgeLines_MemberIsCloseTo75YearsOld()
    {
        var dateOfBirth = new DateTimeOffset(new DateTime(1949, 10, 10));
        var now = new DateTimeOffset(new DateTime(2021, 12, 10));

        var member = new MemberBuilder()
            .DateOfBirth(dateOfBirth)
            .Build();

        var lineStages = member.GetAgeLines(now, 55, 60);
        lineStages.Count().Should().Be(3);

        lineStages.Should().BeEquivalentTo(new List<int> { 73, 74, 75 });
    }

    public void GetAgeLines_MemberIsOlderThanEarliestRetirementAgeAndYoungerThanNormalRetirementAge()
    {
        var dateOfBirth = new DateTimeOffset(new DateTime(1964, 10, 10));
        var now = new DateTimeOffset(new DateTime(2021, 12, 10));

        var member = new MemberBuilder()
            .DateOfBirth(dateOfBirth)
            .Build();

        var lineStages = member.GetAgeLines(now, 55, 60);
        lineStages.Count().Should().Be(5);

        lineStages.Should().BeEquivalentTo(new List<int> { 60, 61, 62, 63, 64 });
    }

    public void GetAgeLines_MemberIsOlderThanEarliestRetirementAgeAndYoungerThanNormalRetirementAge2()
    {
        var dateOfBirth = new DateTimeOffset(new DateTime(1966, 10, 10));
        var now = new DateTimeOffset(new DateTime(2021, 12, 10));

        var member = new MemberBuilder()
            .DateOfBirth(dateOfBirth)
            .Build();

        var lineStages = member.GetAgeLines(now, 55, 60);
        lineStages.Count().Should().Be(5);

        lineStages.Should().BeEquivalentTo(new List<int> { 60, 61, 62, 63, 64 });
    }



    public void CanGetEffectiveBankAccount()
    {
        var sut = new MemberBuilder().Build();
        sut.TrySubmitUkBankAccount("John Doe", "12345678", DateTimeOffset.UtcNow.AddDays(-100), Bank.CreateUkBank("123456", "Natwest", "Manchester").Right());
        sut.TrySubmitUkBankAccount("John Doe1", "11111111", DateTimeOffset.UtcNow, Bank.CreateUkBank("123456", "Natwest", "Manchester").Right());

        var result = sut.EffectiveBankAccount();

        result.IsSome.Should().BeTrue();
        result.Value().AccountNumber.Should().Be("11111111");
    }

    public void ReturnsNone_WhenNoEffectiveBankAccountExist()
    {
        var sut = new MemberBuilder().Build();

        var result = sut.EffectiveBankAccount();

        result.IsNone.Should().BeTrue();
    }

    public void CanSubmitInitialUkBankAccount()
    {
        var sut = new MemberBuilder().Build();

        sut.TrySubmitUkBankAccount("John Doe2", "22222222", DateTimeOffset.UtcNow, Bank.CreateUkBank("123456", "Natwest", "Manchester").Right());

        sut.BankAccounts.Should().NotBeEmpty();
        sut.BankAccounts.Count.Should().Be(1);
        sut.BankAccounts.Single(x => x.AccountNumber == "22222222").SequenceNumber.Should().Be(1);
    }

    public void CanSubmitNewUkBankAccount()
    {
        var sut = new MemberBuilder().Build();
        sut.TrySubmitUkBankAccount("John Doe2", "22222222", DateTimeOffset.UtcNow, Bank.CreateUkBank("123456", "Natwest", "Manchester").Right());

        sut.TrySubmitUkBankAccount("John Doe2", "11111111", DateTimeOffset.UtcNow, Bank.CreateUkBank("654321", "Natwest", "Manchester").Right());

        sut.BankAccounts.Should().NotBeEmpty();
        sut.BankAccounts.Count.Should().Be(2);
        sut.BankAccounts.Single(x => x.AccountNumber == "11111111").SequenceNumber.Should().Be(2);
    }

    public void CanSubmitInitialNonUkBankAccount()
    {
        var sut = new MemberBuilder().Build();

        sut.TrySubmitIbanBankAccount("John Doe2", "LT222222222222222222", DateTimeOffset.UtcNow, Bank.CreateNonUkBank("12345678", "1", "Natwest", "Vilnius", "LT").Right());

        sut.BankAccounts.Should().NotBeEmpty();
        sut.BankAccounts.Count.Should().Be(1);
        sut.BankAccounts.Single(x => x.Iban == "LT222222222222222222").SequenceNumber.Should().Be(1);
    }

    public void CanSubmitNewNonUkBankAccount()
    {
        var sut = new MemberBuilder().Build();
        sut.TrySubmitIbanBankAccount("John Doe2", "LT222222222222222222", DateTimeOffset.UtcNow, Bank.CreateNonUkBank("12345678", "1", "Natwest", "Vilnius", "LT").Right());

        sut.TrySubmitIbanBankAccount("John Doe2", "LT222222222222222211", DateTimeOffset.UtcNow, Bank.CreateNonUkBank("87654321", "1", "Natwest", "Vilnius", "LT").Right());

        sut.BankAccounts.Should().NotBeEmpty();
        sut.BankAccounts.Count.Should().Be(2);
        sut.BankAccounts.Single(x => x.Iban == "LT222222222222222211").SequenceNumber.Should().Be(2);
    }

    public void ReturnsLeft_OnSubmitNewUkBankAccount_WhenInvalidDataProvided()
    {
        var sut = new MemberBuilder().Build();

        var result = sut.TrySubmitUkBankAccount("John Doe2", "2222222", DateTimeOffset.UtcNow, Bank.CreateUkBank("123456", "Natwest", "Manchester").Right());

        result.IsLeft.Should().BeTrue();
        sut.BankAccounts.Should().BeEmpty();
        sut.BankAccounts.Count.Should().Be(0);
    }

    public void ReturnsLeft_OnSubmitNewNonUkBankAccount_WhenInvalidDataProvided()
    {
        var sut = new MemberBuilder().Build();

        var result = sut.TrySubmitIbanBankAccount("John Doe2", "", DateTimeOffset.UtcNow, Bank.CreateNonUkBank("12345678", "1", "Natwest", "Vilnius", "LT").Right());

        result.IsLeft.Should().BeTrue();
        sut.BankAccounts.Should().BeEmpty();
        sut.BankAccounts.Count.Should().Be(0);
    }

    public void ReturnsNone_WhenNoContactExist_OnAddressRetrieval()
    {
        var sut = new MemberBuilder().Build();

        var result = sut.Address();

        result.IsNone.Should().BeTrue();
    }

    public void CanGetAddress()
    {
        var sut = new MemberBuilder().Build();
        var address = Address.Create("Towers Watson Westgate", "122-130 Station Road", "test dada for addres3", "Redhill",
           "Surrey", "United Kingdom", "GB", "RH1 1WS").Right();
        var tableShort = "AR";
        var now = DateTimeOffset.UtcNow;
        sut.SaveAddress(address, 1171111, 3141271, tableShort, now);

        var result = sut.Address();

        result.IsNone.Should().BeFalse();
        result.IsSome.Should().BeTrue();
        result.Single().Should().BeEquivalentTo(address);
    }

    public void CanSaveInitialAddress()
    {
        var address = Address.Create("Towers Watson Westgate", "122-130 Station Road", "test dada for addres3", "Redhill",
           "Surrey", "United Kingdom", "GB", "RH1 1WS");
        var tableShort = "AR";
        var now = DateTimeOffset.UtcNow;
        var nowDateWithoutTimestamp = new DateTimeOffset(now.Date, TimeSpan.Zero);
        var sut = new MemberBuilder().Build();

        sut.SaveAddress(address.Right(), 1171111, 3141271, tableShort, now);

        sut.ContactReferences.Count.Should().Be(1);
        sut.ContactReferences[0].Contact.Address.Should().BeEquivalentTo(address.Right());
        sut.ContactReferences[0].Contact.Email.Should().BeEquivalentTo(Email.Empty());
        sut.ContactReferences[0].BusinessGroup.Should().Be("RBS");
        sut.ContactReferences[0].ReferenceNumber.Should().Be("0003994");
        sut.ContactReferences[0].SequenceNumber.Should().Be(1);
        sut.ContactReferences[0].StartDate.Should().Be(nowDateWithoutTimestamp);
        sut.RecordsIndicator.Should().Be("AR");
    }

    public void CanSaveNewAddress()
    {
        var initialAddress = Address.Create("Towers Watson Westgate", "122-130 Station Road", "test dada for addres3", "Redhill",
           "Surrey", "United Kingdom", "GB", "RH1 1WS");
        var newAddress = Address.Create("New Towers Watson Westgate", "126-130 Station Road", "test dada for addres3", "Redhill",
           "London", "United Kingdom", "GB", "RH1 1WS");
        var tableShort = "AM";
        var tableShortNew = "AR";
        var now = DateTimeOffset.UtcNow;
        var nowDateWithoutTimestamp = new DateTimeOffset(now.Date, TimeSpan.Zero);
        var sut = new MemberBuilder().Build();
        sut.SaveAddress(initialAddress.Right(), 1171111, 3141271, tableShort, now.AddDays(-234));

        sut.SaveAddress(newAddress.Right(), 1171112, 3141272, tableShortNew, now);

        sut.ContactReferences.Count.Should().Be(2);
        sut.ContactReferences[0].Contact.Address.Should().BeEquivalentTo(initialAddress.Right());
        sut.ContactReferences[0].BusinessGroup.Should().Be("RBS");
        sut.ContactReferences[0].ReferenceNumber.Should().Be("0003994");
        sut.ContactReferences[0].SequenceNumber.Should().Be(1);
        sut.ContactReferences[0].StartDate.Should().Be(nowDateWithoutTimestamp.AddDays(-234));
        sut.ContactReferences[0].EndDate.Should().Be(nowDateWithoutTimestamp.AddDays(-1));

        sut.ContactReferences[1].Contact.Address.Should().BeEquivalentTo(newAddress.Right());
        sut.ContactReferences[1].BusinessGroup.Should().Be("RBS");
        sut.ContactReferences[1].ReferenceNumber.Should().Be("0003994");
        sut.ContactReferences[1].SequenceNumber.Should().Be(2);
        sut.ContactReferences[1].StartDate.Should().Be(nowDateWithoutTimestamp);
        sut.RecordsIndicator.Should().Be($"{tableShort} {tableShortNew}");
    }

    public void CanKeepEmailAndPhoneNumber_WhenSavingNewAddress()
    {
        var address = Address.Create("Towers Watson Westgate", "122-130 Station Road", "test dada for addres3", "Redhill",
          "Surrey", "United Kingdom", "GB", "RH1 1WS").Right();
        var email = Email.Create("test.new@test.com").Right();
        var mobilePhone = Phone.Create("447544111111").Right();
        var tableShort = "AM";
        var tableShortNew = "AR";
        var now = DateTimeOffset.UtcNow;
        var nowDateWithoutTimestamp = new DateTimeOffset(now.Date, TimeSpan.Zero);
        var sut = new MemberBuilder().Build();
        sut.SaveEmail(email, 1171110, 3141270, tableShort, now.AddDays(-234));
        sut.SaveMobilePhone(mobilePhone, 1171111, 3141271, tableShort, now.AddDays(-200));

        sut.SaveAddress(address, 1171112, 3141272, tableShortNew, now);

        sut.ContactReferences.Count.Should().Be(3);
        sut.ContactReferences[0].Contact.Email.Should().BeEquivalentTo(email);
        sut.ContactReferences[0].Contact.Address.Should().BeEquivalentTo(Address.Empty());
        sut.ContactReferences[0].Contact.MobilePhone.Should().BeEquivalentTo(Phone.Empty());
        sut.ContactReferences[0].BusinessGroup.Should().Be("RBS");
        sut.ContactReferences[0].ReferenceNumber.Should().Be("0003994");
        sut.ContactReferences[0].SequenceNumber.Should().Be(1);
        sut.ContactReferences[0].StartDate.Should().Be(nowDateWithoutTimestamp.AddDays(-234));
        sut.ContactReferences[0].EndDate.Should().Be(nowDateWithoutTimestamp.AddDays(-201));

        sut.ContactReferences[2].Contact.Email.Should().BeEquivalentTo(email);
        sut.ContactReferences[2].Contact.Address.Should().BeEquivalentTo(address);
        sut.ContactReferences[2].Contact.MobilePhone.Should().BeEquivalentTo(mobilePhone);
        sut.ContactReferences[2].BusinessGroup.Should().Be("RBS");
        sut.ContactReferences[2].ReferenceNumber.Should().Be("0003994");
        sut.ContactReferences[2].SequenceNumber.Should().Be(3);
        sut.ContactReferences[2].StartDate.Should().Be(nowDateWithoutTimestamp);
        sut.RecordsIndicator.Should().Be($"{tableShort} {tableShortNew}");
    }

    public void DoesNotUpdateRecordsIndicator_WhenRecordsIndicatorContainsProvidedTableShort()
    {
        var initialAddress = Address.Create("Towers Watson Westgate", "122-130 Station Road", "test dada for addres3", "Redhill",
           "Surrey", "United Kingdom", "GB", "RH1 1WS");
        var newAddress = Address.Create("New Towers Watson Westgate", "126-130 Station Road", "test dada for addres3", "Redhill",
           "London", "United Kingdom", "GB", "RH1 1WS");
        var tableShort = "AR";
        var tableShortNew = "AR";
        var now = DateTimeOffset.UtcNow;
        var sut = new MemberBuilder().Build();
        sut.SaveAddress(initialAddress.Right(), 1171111, 3141271, tableShort, now.AddDays(-234));

        sut.SaveAddress(newAddress.Right(), 1171112, 3141272, tableShortNew, now);

        sut.ContactReferences.Count.Should().Be(2);
        sut.RecordsIndicator.Should().Be(tableShort);
    }

    public void ReturnsTrue_WhenSameAsLastAddressIsProvided()
    {
        var initialAddress = Address.Create("Towers Watson Westgate", "122-130 Station Road", "test dada for addres3", "Redhill",
          "Surrey", "United Kingdom", "GB", "RH1 1WS");
        var newAddress = Address.Create("Towers Watson Westgate", "122-130 Station Road", "test dada for addres3", "Redhill",
          "Surrey", "United Kingdom", "GB", "RH1 1WS");
        var tableShort = "AR";
        var now = DateTimeOffset.UtcNow;
        var sut = new MemberBuilder().Build();
        sut.SaveAddress(initialAddress.Right(), 1171111, 3141271, tableShort, now.AddDays(-234));

        var result = sut.HasAddress(newAddress.Right());

        result.Should().BeTrue();
    }

    public void ReturnsFalse_WhenDifferentThanLastAddressIsProvided()
    {
        var initialAddress = Address.Create("Towers Watson Westgate", "122-130 Station Road", "test dada for addres3", "Redhill",
          "Surrey", "United Kingdom", "GB", "RH1 1WS");
        var newAddress = Address.Create("New Towers Watson Westgate", "126-130 Station Road", "test dada for addres3", "Redhill",
           "London", "United Kingdom", "GB", "RH1 1WS");
        var tableShort = "AR";
        var now = DateTimeOffset.UtcNow;
        var sut = new MemberBuilder().Build();
        sut.SaveAddress(initialAddress.Right(), 1171111, 3141271, tableShort, now.AddDays(-234));

        var result = sut.HasAddress(newAddress.Right());

        result.Should().BeFalse();
    }

    public void Throws_WhenSameAsLastAddressIsProvidedToSave()
    {
        var initialAddress = Address.Create("Towers Watson Westgate", "122-130 Station Road", "test dada for addres3", "Redhill",
          "Surrey", "United Kingdom", "GB", "RH1 1WS");
        var newAddress = Address.Create("Towers Watson Westgate", "122-130 Station Road", "test dada for addres3", "Redhill",
           "Surrey", "United Kingdom", "GB", "RH1 1WS");
        var tableShort = "AR";
        var now = DateTimeOffset.UtcNow;
        var sut = new MemberBuilder().Build();
        sut.SaveAddress(initialAddress.Right(), 1171111, 3141271, tableShort, now.AddDays(-234));

        var action = () => sut.SaveAddress(newAddress.Right(), 1171112, 3141272, tableShort, now);

        action.Should().Throw<InvalidOperationException>();
    }

    public void ReturnsNone_WhenNoContactExist_OnEmailAddressRetrieval()
    {
        var sut = new MemberBuilder()
            .EmailView(Email.Empty())
            .Build();

        var result = sut.Email();

        result.IsNone.Should().BeTrue();
    }

    public void ReturnsNone_WhenNoContactExist_OnEmailAddressRetrieval2()
    {
        var sut = new MemberBuilder()
            .EmailView(null)
            .Build();

        var result = sut.Email();

        result.IsNone.Should().BeTrue();
    }

    public void CanGetEmailAddress()
    {
        var sut = new MemberBuilder()
            .EmailView(Email.Create("test.test@test.com").Right())
            .Build();
        var email = Email.Create("test.test@test.com").Right();

        var result = sut.Email();

        result.IsNone.Should().BeFalse();
        result.IsSome.Should().BeTrue();
        result.Single().Should().BeEquivalentTo(email);
    }

    public void CanSaveEmailAddressInInitialContactReferences()
    {
        var email = Email.Create("test@test.com").Right();
        var tableShort = "AR";
        var now = DateTimeOffset.UtcNow;
        var nowDateWithoutTimestamp = new DateTimeOffset(now.Date, TimeSpan.Zero);
        var sut = new MemberBuilder().Build();

        sut.SaveEmail(email, 1171111, 3141271, tableShort, now);

        sut.ContactReferences.Count.Should().Be(1);
        sut.ContactReferences[0].Contact.Address.Should().BeEquivalentTo(Address.Empty());
        sut.ContactReferences[0].Contact.Email.Should().BeEquivalentTo(email);
        sut.ContactReferences[0].BusinessGroup.Should().Be("RBS");
        sut.ContactReferences[0].ReferenceNumber.Should().Be("0003994");
        sut.ContactReferences[0].SequenceNumber.Should().Be(1);
        sut.ContactReferences[0].StartDate.Should().Be(nowDateWithoutTimestamp);
        sut.RecordsIndicator.Should().Be("AR");
        sut.EpaEmails[0].Email.Should().BeEquivalentTo(email);
        sut.EpaEmails[0].SequenceNumber.Should().Be(1);
    }

    public void CanSaveNewEmailAddress()
    {
        var initialEmail = Email.Create("test@test.com").Right();
        var newEmail = Email.Create("test1@test.com").Right();
        var tableShort = "AM";
        var tableShortNew = "AR";
        var now = DateTimeOffset.UtcNow;
        var nowDateWithoutTimestamp = new DateTimeOffset(now.Date, TimeSpan.Zero);
        var sut = new MemberBuilder().Build();
        sut.SaveEmail(initialEmail, 1171111, 3141271, tableShort, now.AddDays(-234));

        sut.SaveEmail(newEmail, 1171112, 3141272, tableShortNew, now);

        sut.ContactReferences.Count.Should().Be(2);
        sut.ContactReferences[0].Contact.Address.Should().BeEquivalentTo(Address.Empty());
        sut.ContactReferences[0].Contact.Email.Should().BeEquivalentTo(initialEmail);
        sut.ContactReferences[0].BusinessGroup.Should().Be("RBS");
        sut.ContactReferences[0].ReferenceNumber.Should().Be("0003994");
        sut.ContactReferences[0].SequenceNumber.Should().Be(1);
        sut.ContactReferences[0].StartDate.Should().Be(nowDateWithoutTimestamp.AddDays(-234));
        sut.ContactReferences[0].EndDate.Should().Be(nowDateWithoutTimestamp.AddDays(-1));
        sut.EpaEmails[0].Email.Should().BeEquivalentTo(initialEmail);
        sut.EpaEmails[0].SequenceNumber.Should().Be(1);

        sut.ContactReferences[1].Contact.Address.Should().BeEquivalentTo(Address.Empty());
        sut.ContactReferences[1].Contact.Email.Should().BeEquivalentTo(newEmail);
        sut.ContactReferences[1].BusinessGroup.Should().Be("RBS");
        sut.ContactReferences[1].ReferenceNumber.Should().Be("0003994");
        sut.ContactReferences[1].SequenceNumber.Should().Be(2);
        sut.ContactReferences[1].StartDate.Should().Be(nowDateWithoutTimestamp);
        sut.RecordsIndicator.Should().Be($"{tableShort} {tableShortNew}");
        sut.EpaEmails[1].Email.Should().BeEquivalentTo(newEmail);
        sut.EpaEmails[1].SequenceNumber.Should().Be(2);
    }

    public void CanUpdateContact2FaValidationFor2faBusinesGroup()
    {
        var initialEmail = Email.Create("test@test.com").Right();
        var tableShort = "AM";
        var now = DateTimeOffset.UtcNow;
        var sut = new MemberBuilder().Build();
        sut
            .SaveEmail(initialEmail, 1171111, 3141271, tableShort, now.AddDays(-234))
            .UpdateEmailValidationFor2FaBusinessGroup("RBS1111111", 3141271, "123456", "Y", now);

        sut.ContactValidations.Count.Should().Be(1);
        sut.ContactValidations[0].Token.Should().Be(123456);
        sut.ContactValidations[0].ContactType.Should().Be(MemberContactType.EmailAddress);
    }

    public void CanKeepAddressAndMobilePhone_WhenSavingEmailAddress()
    {
        var address = Address.Create("Towers Watson Westgate", "122-130 Station Road", "test dada for addres3", "Redhill",
          "Surrey", "United Kingdom", "GB", "RH1 1WS").Right();
        var email = Email.Create("test.new@test.com").Right();
        var mobilePhone = Phone.Create("447544111111").Right();
        var tableShort = "AM";
        var tableShortNew = "AR";
        var now = DateTimeOffset.UtcNow;
        var nowDateWithoutTimestamp = new DateTimeOffset(now.Date, TimeSpan.Zero);
        var sut = new MemberBuilder().Build();
        sut.SaveAddress(address, 1171110, 3141270, tableShort, now.AddDays(-234));
        sut.SaveMobilePhone(mobilePhone, 1171111, 3141271, tableShort, now.AddDays(-200));

        sut.SaveEmail(email, 1171112, 3141272, tableShortNew, now);

        sut.ContactReferences.Count.Should().Be(3);
        sut.ContactReferences[0].Contact.Email.Should().BeEquivalentTo(Email.Empty());
        sut.ContactReferences[0].Contact.MobilePhone.Should().BeEquivalentTo(Phone.Empty());
        sut.ContactReferences[0].Contact.Address.Should().BeEquivalentTo(address);
        sut.ContactReferences[0].BusinessGroup.Should().Be("RBS");
        sut.ContactReferences[0].ReferenceNumber.Should().Be("0003994");
        sut.ContactReferences[0].SequenceNumber.Should().Be(1);
        sut.ContactReferences[0].StartDate.Should().Be(nowDateWithoutTimestamp.AddDays(-234));
        sut.ContactReferences[0].EndDate.Should().Be(nowDateWithoutTimestamp.AddDays(-201));

        sut.ContactReferences[2].Contact.Email.Should().BeEquivalentTo(email);
        sut.ContactReferences[2].Contact.Address.Should().BeEquivalentTo(address);
        sut.ContactReferences[2].Contact.MobilePhone.Should().BeEquivalentTo(mobilePhone);
        sut.ContactReferences[2].BusinessGroup.Should().Be("RBS");
        sut.ContactReferences[2].ReferenceNumber.Should().Be("0003994");
        sut.ContactReferences[2].SequenceNumber.Should().Be(3);
        sut.ContactReferences[2].StartDate.Should().Be(nowDateWithoutTimestamp);
        sut.RecordsIndicator.Should().Be($"{tableShort} {tableShortNew}");
    }

    public void DoesNotUpdateRecordsIndicator_WhenRecordsIndicatorContainsProvidedTableShort_OnNewEmailSumbmit()
    {
        var initialEmail = Email.Create("test@test.com").Right();
        var newEmail = Email.Create("test1@test.com").Right();
        var tableShort = "AR";
        var tableShortNew = "AR";
        var now = DateTimeOffset.UtcNow;
        var sut = new MemberBuilder().Build();
        sut.SaveEmail(initialEmail, 1171111, 3141271, tableShort, now.AddDays(-234));

        sut.SaveEmail(newEmail, 1171112, 3141272, tableShortNew, now);

        sut.ContactReferences.Count.Should().Be(2);
        sut.RecordsIndicator.Should().Be(tableShort);
    }

    public void ReturnsTrue_WhenSameAsLastEmailAddressIsProvided()
    {
        var newEmail = Email.Create("test@test.com").Right();
        var sut = new MemberBuilder()
            .EmailView(Email.Create("test@test.com").Right())
            .Build();

        var result = sut.HasEmail(newEmail);

        result.Should().BeTrue();
    }

    public void ReturnsFalse_WhenNoEmailIsPresent()
    {
        var newEmail = Email.Create("test@test.com").Right();
        var sut = new MemberBuilder()
            .EmailView(Email.Empty())
            .Build();

        var result = sut.HasEmail(newEmail);

        result.Should().BeFalse();
    }

    public void ReturnsFalse_WhenDifferentThanLastEmailAddressIsProvided()
    {
        var initialEmail = Email.Create("test@test.com").Right();
        var newEmail = Email.Create("test1@test.com").Right();
        var tableShort = "AR";
        var now = DateTimeOffset.UtcNow;
        var sut = new MemberBuilder().Build();
        sut.SaveEmail(initialEmail, 1171111, 3141271, tableShort, now.AddDays(-234));

        var result = sut.HasEmail(newEmail);

        result.Should().BeFalse();
    }

    public void Throws_WhenSameAsLastEmailAddressIsProvidedToSave()
    {
        var newEmail = Email.Create("test@test.com").Right();
        var tableShort = "AR";
        var now = DateTimeOffset.UtcNow;
        var sut = new MemberBuilder()
            .EmailView(Email.Create("test@test.com").Right())
            .Build();

        var action = () => sut.SaveEmail(newEmail, 1171112, 3141272, tableShort, now);

        action.Should().Throw<InvalidOperationException>();
    }

    public void ReturnsNone_WhenNoContactExist_OnMobilePhoneCodeRetrieval()
    {
        var sut = new MemberBuilder().Build();

        var result = sut.MobilePhoneCode();

        result.IsNone.Should().BeTrue();
    }

    public void CanGetMobilePhoneNumberCode()
    {
        var sut = new MemberBuilder().Build();
        var mobilePhone = Phone.Create("44 7544111111").Right();
        var tableShort = "AR";
        var now = DateTimeOffset.UtcNow;
        sut.SaveMobilePhone(mobilePhone, 1171111, 3141271, tableShort, now);

        var result = sut.MobilePhoneCode();

        result.IsNone.Should().BeFalse();
        result.IsSome.Should().BeTrue();
        result.Single().Should().Be("44");
    }

    public void CanUpdateContact2faMobilePhoneNumberFor2FaBusinessGroup()
    {
        var sut = new MemberBuilder().Build();
        var mobilePhone = Phone.Create("44 7544111111").Right();
        var tableShort = "AR";
        var now = DateTimeOffset.UtcNow;
        sut
            .SaveMobilePhone(mobilePhone, 1171111, 3141271, tableShort, now)
            .UpdateMobilePhoneValidationFor2FaBusinessGroup("RBS11111111", 3141271, "123456", "Y", now, "UnitedKingdom");

        sut.ContactValidations.Should().HaveCount(1);
        sut.ContactCountry.Should().NotBeNull();
    }

    public void ReturnErrorWhenUpdateMobilePhoneValidationFor2FaBusinessGroupIsCalledWithEmptyCountry()
    {
        var sut = new MemberBuilder().Build();
        var mobilePhone = Phone.Create("44 7544111111").Right();
        var tableShort = "AR";
        var now = DateTimeOffset.UtcNow;
        var result = sut
            .SaveMobilePhone(mobilePhone, 1171111, 3141271, tableShort, now)
            .UpdateMobilePhoneValidationFor2FaBusinessGroup("RBS11111111", 3141271, "123456", "Y", now, " ");

        sut.ContactValidations.Should().HaveCount(1);
        sut.ContactCountry.Should().BeNull();
        result.IsLeft.Should().BeTrue();
        result.Left().Should().BeOfType<Error>();
    }

    public void ReturnsNone_WhenNoContactExist_OnMobilePhoneNumberRetrieval()
    {
        var sut = new MemberBuilder().Build();

        var result = sut.MobilePhoneNumber();

        result.IsNone.Should().BeTrue();
    }

    public void CanGetMobilePhoneNumber()
    {
        var sut = new MemberBuilder().Build();
        var mobilePhone = Phone.Create("44 7544111111").Right();
        var tableShort = "AR";
        var now = DateTimeOffset.UtcNow;
        sut.SaveMobilePhone(mobilePhone, 1171111, 3141271, tableShort, now);

        var result = sut.MobilePhoneNumber();

        result.IsNone.Should().BeFalse();
        result.IsSome.Should().BeTrue();
        result.Single().Should().Be("7544111111");
    }

    public void ReturnsNone_WhenNoContactExist_OnFullMobilePhoneNumberRetrieval()
    {
        var sut = new MemberBuilder().Build();

        var result = sut.FullMobilePhoneNumber();

        result.IsNone.Should().BeTrue();
    }

    public void CanGetFullMobilePhoneNumber()
    {
        var sut = new MemberBuilder().Build();
        var mobilePhone = Phone.Create("447544111111").Right();
        var tableShort = "AR";
        var now = DateTimeOffset.UtcNow;
        var nowDateWithoutTimestamp = new DateTimeOffset(now.Date, TimeSpan.Zero);
        sut.SaveMobilePhone(mobilePhone, 1171111, 3141271, tableShort, now);

        var result = sut.FullMobilePhoneNumber();

        result.IsNone.Should().BeFalse();
        result.IsSome.Should().BeTrue();
        result.Single().Should().BeEquivalentTo(mobilePhone);
    }

    public void CanSaveMobilePhoneInInitialContactReferences()
    {
        var mobilePhone = Phone.Create("447544111111").Right();
        var tableShort = "AR";
        var now = DateTimeOffset.UtcNow;
        var nowDateWithoutTimestamp = new DateTimeOffset(now.Date, TimeSpan.Zero);
        var sut = new MemberBuilder().Build();

        sut.SaveMobilePhone(mobilePhone, 1171111, 3141271, tableShort, now);

        sut.ContactReferences.Count.Should().Be(1);
        sut.ContactReferences[0].Contact.Address.Should().BeEquivalentTo(Address.Empty());
        sut.ContactReferences[0].Contact.Email.Should().BeEquivalentTo(Email.Empty());
        sut.ContactReferences[0].Contact.MobilePhone.Should().BeEquivalentTo(mobilePhone);
        sut.ContactReferences[0].BusinessGroup.Should().Be("RBS");
        sut.ContactReferences[0].ReferenceNumber.Should().Be("0003994");
        sut.ContactReferences[0].SequenceNumber.Should().Be(1);
        sut.ContactReferences[0].StartDate.Should().Be(nowDateWithoutTimestamp);
        sut.RecordsIndicator.Should().Be("AR");
    }

    public void CanSaveMobilePhoneInInitialContactReferences2()
    {
        var mobilePhone = Phone.Create("447544111111").Right();
        var mobilePhone2 = Phone.Create("447544111122").Right();
        var tableShort = "AR";
        var now = DateTimeOffset.UtcNow;
        var nowDateWithoutTimestamp = new DateTimeOffset(now.Date, TimeSpan.Zero);
        var sut = new MemberBuilder().Build();
        sut.SaveMobilePhone(mobilePhone, 1171111, 3141271, tableShort, now);

        sut.SaveMobilePhone(mobilePhone2, 1171111, 3141271, tableShort, now);

        sut.ContactReferences.Count.Should().Be(2);
        sut.ContactReferences.Single(x => x.SequenceNumber == 2).Contact.Address.Should().BeEquivalentTo(Address.Empty());
        sut.ContactReferences.Single(x => x.SequenceNumber == 2).Contact.Email.Should().BeEquivalentTo(Email.Empty());
        sut.ContactReferences.Single(x => x.SequenceNumber == 2).Contact.MobilePhone.Should().BeEquivalentTo(mobilePhone2);
        sut.ContactReferences.Single(x => x.SequenceNumber == 2).BusinessGroup.Should().Be("RBS");
        sut.ContactReferences.Single(x => x.SequenceNumber == 2).ReferenceNumber.Should().Be("0003994");
        sut.ContactReferences.Single(x => x.SequenceNumber == 2).SequenceNumber.Should().Be(2);
        sut.ContactReferences.Single(x => x.SequenceNumber == 2).StartDate.Should().Be(nowDateWithoutTimestamp);
        sut.RecordsIndicator.Should().Be("AR");
    }

    public void CanSaveNewMobilePhoneNumber()
    {
        var initialMobilePhone = Phone.Create("447544111111").Right();
        var newMobilePhone = Phone.Create("447544111112").Right();
        var tableShort = "AM";
        var tableShortNew = "AR";
        var now = DateTimeOffset.UtcNow;
        var nowDateWithoutTimestamp = new DateTimeOffset(now.Date, TimeSpan.Zero);
        var sut = new MemberBuilder().Build();
        sut.SaveMobilePhone(initialMobilePhone, 1171111, 3141271, tableShort, now.AddDays(-234));

        sut.SaveMobilePhone(newMobilePhone, 1171112, 3141272, tableShortNew, now);

        sut.ContactReferences.Count.Should().Be(2);
        sut.ContactReferences[0].Contact.Address.Should().BeEquivalentTo(Address.Empty());
        sut.ContactReferences[0].Contact.Email.Should().BeEquivalentTo(Email.Empty());
        sut.ContactReferences[0].Contact.MobilePhone.Should().BeEquivalentTo(initialMobilePhone);
        sut.ContactReferences[0].BusinessGroup.Should().Be("RBS");
        sut.ContactReferences[0].ReferenceNumber.Should().Be("0003994");
        sut.ContactReferences[0].SequenceNumber.Should().Be(1);
        sut.ContactReferences[0].StartDate.Should().Be(nowDateWithoutTimestamp.AddDays(-234));
        sut.ContactReferences[0].EndDate.Should().Be(nowDateWithoutTimestamp.AddDays(-1));

        sut.ContactReferences[1].Contact.Address.Should().BeEquivalentTo(Address.Empty());
        sut.ContactReferences[1].Contact.Email.Should().BeEquivalentTo(Email.Empty());
        sut.ContactReferences[1].Contact.MobilePhone.Should().BeEquivalentTo(newMobilePhone);
        sut.ContactReferences[1].BusinessGroup.Should().Be("RBS");
        sut.ContactReferences[1].ReferenceNumber.Should().Be("0003994");
        sut.ContactReferences[1].SequenceNumber.Should().Be(2);
        sut.ContactReferences[1].StartDate.Should().Be(nowDateWithoutTimestamp);
        sut.RecordsIndicator.Should().Be($"{tableShort} {tableShortNew}");
        sut.EpaEmails.Should().BeEmpty();
    }

    public void CanKeepAddressAndEmail_WhenSavingEmailAddress()
    {
        var address = Address.Create("Towers Watson Westgate", "122-130 Station Road", "test dada for addres3", "Redhill",
          "Surrey", "United Kingdom", "GB", "RH1 1WS").Right();
        var email = Email.Create("test.new@test.com").Right();
        var mobilePhone = Phone.Create("447544111111").Right();
        var tableShort = "AM";
        var tableShortNew = "AR";
        var now = DateTimeOffset.UtcNow;
        var nowDateWithoutTimestamp = new DateTimeOffset(now.Date, TimeSpan.Zero);
        var sut = new MemberBuilder().Build();
        sut.SaveAddress(address, 1171110, 3141270, tableShort, now.AddDays(-234));
        sut.SaveEmail(email, 1171111, 3141271, tableShort, now.AddDays(-200));

        sut.SaveMobilePhone(mobilePhone, 1171112, 3141272, tableShortNew, now);

        sut.ContactReferences.Count.Should().Be(3);
        sut.ContactReferences[0].Contact.Email.Should().BeEquivalentTo(Email.Empty());
        sut.ContactReferences[0].Contact.Address.Should().BeEquivalentTo(address);
        sut.ContactReferences[0].BusinessGroup.Should().Be("RBS");
        sut.ContactReferences[0].ReferenceNumber.Should().Be("0003994");
        sut.ContactReferences[0].SequenceNumber.Should().Be(1);
        sut.ContactReferences[0].StartDate.Should().Be(nowDateWithoutTimestamp.AddDays(-234));
        sut.ContactReferences[0].EndDate.Should().Be(nowDateWithoutTimestamp.AddDays(-201));

        sut.ContactReferences[2].Contact.Email.Should().BeEquivalentTo(email);
        sut.ContactReferences[2].Contact.Address.Should().BeEquivalentTo(address);
        sut.ContactReferences[2].Contact.MobilePhone.Should().BeEquivalentTo(mobilePhone);
        sut.ContactReferences[2].BusinessGroup.Should().Be("RBS");
        sut.ContactReferences[2].ReferenceNumber.Should().Be("0003994");
        sut.ContactReferences[2].SequenceNumber.Should().Be(3);
        sut.ContactReferences[2].StartDate.Should().Be(nowDateWithoutTimestamp);
        sut.RecordsIndicator.Should().Be($"{tableShort} {tableShortNew}");
    }

    public void DoesNotUpdateRecordsIndicator_WhenRecordsIndicatorContainsProvidedTableShort_OnNewMobilePhoneSumbmit()
    {
        var initialMobilePhone = Phone.Create("447544111111").Right();
        var newMobilePhone = Phone.Create("447544111112").Right();
        var tableShort = "AR";
        var tableShortNew = "AR";
        var now = DateTimeOffset.UtcNow;
        var sut = new MemberBuilder().Build();
        sut.SaveMobilePhone(initialMobilePhone, 1171111, 3141271, tableShort, now.AddDays(-234));

        sut.SaveMobilePhone(newMobilePhone, 1171112, 3141272, tableShortNew, now);

        sut.ContactReferences.Count.Should().Be(2);
        sut.RecordsIndicator.Should().Be(tableShort);
    }

    public void ReturnsTrue_WhenSameAsLastMobilePhoneNumberIsProvided()
    {
        var initialMobilePhone = Phone.Create("447544111111").Right();
        var newMobilePhone = Phone.Create("447544111111").Right();
        var tableShort = "AR";
        var now = DateTimeOffset.UtcNow;
        var sut = new MemberBuilder().Build();
        sut.SaveMobilePhone(initialMobilePhone, 1171111, 3141271, tableShort, now.AddDays(-234));

        var result = sut.HasMobilePhone(newMobilePhone);

        result.Should().BeTrue();
    }

    public void ReturnsFalse_WhenDifferentThanLastMobilePhoneNumberIsProvided()
    {
        var initialMobilePhone = Phone.Create("447544111111").Right();
        var newMobilePhone = Phone.Create("447544111112").Right();
        var tableShort = "AR";
        var now = DateTimeOffset.UtcNow;
        var sut = new MemberBuilder().Build();
        sut.SaveMobilePhone(initialMobilePhone, 1171111, 3141271, tableShort, now.AddDays(-234));

        var result = sut.HasMobilePhone(newMobilePhone);

        result.Should().BeFalse();
    }

    public void Throws_WhenSameAsLastMobilePhoneNumberIsProvidedToSave()
    {
        var initialMobilePhone = Phone.Create("447544111111").Right();
        var newMobilePhone = Phone.Create("447544111111").Right();
        var tableShort = "AR";
        var now = DateTimeOffset.UtcNow;
        var sut = new MemberBuilder().Build();
        sut.SaveMobilePhone(initialMobilePhone, 1171111, 3141271, tableShort, now.AddDays(-234));

        var action = () => sut.SaveMobilePhone(newMobilePhone, 1171112, 3141272, tableShort, now);

        action.Should().Throw<InvalidOperationException>();
    }

    public void AddsInitialNotificationSetting()
    {
        var sut = new MemberBuilder().Build();

        sut.UpdateNotificationsSettings("SMS", false, true, false, DateTimeOffset.UtcNow);

        sut.NotificationSettings.Should().NotBeEmpty();
        sut.NotificationSettings.Single(x => x.EndDate == null).Settings.Should().Be("S");
        sut.NotificationSettings.Single(x => x.EndDate == null).SequenceNumber.Should().Be(1);
    }

    public void AddsNewNotificationSettingAndIncreasesSequenceNumber()
    {
        var sut = new MemberBuilder().Build();
        sut.UpdateNotificationsSettings("SMS", false, true, false, DateTimeOffset.UtcNow);

        sut.UpdateNotificationsSettings("Email", true, true, false, DateTimeOffset.UtcNow);

        sut.NotificationSettings.Should().NotBeEmpty();
        sut.NotificationSettings.Single(x => x.EndDate == null).Settings.Should().Be("B");
        sut.NotificationSettings.Single(x => x.EndDate == null).SequenceNumber.Should().Be(2);
    }

    public void ReturnsError_OnAddingNewNotificationSetting_WhenInvalidData()
    {
        var sut = new MemberBuilder().Build();
        sut.UpdateNotificationsSettings("SMS", false, true, false, DateTimeOffset.UtcNow);

        var result = sut.UpdateNotificationsSettings("Email", false, false, false, DateTimeOffset.UtcNow);

        result.Left().Message.Should().Be("At least one of notification preference must be selected.");
    }

    public void ReturnsCorrectNotificationSettings()
    {
        var sut = new MemberBuilder().Build();
        sut.UpdateNotificationsSettings("SMS", true, true, false, DateTimeOffset.UtcNow);

        var result = sut.NotificationsSettings();

        result.Email.Should().BeTrue();
        result.Sms.Should().BeTrue();
        result.Post.Should().BeFalse();
    }

    public void PostNotificationSettingActive_WhenNotificationSettingsCollectionEmpty()
    {
        var sut = new MemberBuilder().Build();

        var result = sut.NotificationsSettings();

        result.Email.Should().BeFalse();
        result.Sms.Should().BeFalse();
        result.Post.Should().BeTrue();
    }

    public void GetPublicMemberDetails()
    {
        var member = new MemberBuilder()
            .Status(MemberStatus.Active)
            .LocationCode("locationCodeTest")
            .EmployerCode("employerCodeTest")
            .Build();

        member.Status.Should().Be(MemberStatus.Active);
        member.LocationCode.Should().Be("locationCodeTest");
        member.EmployerCode.Should().Be("employerCodeTest");
    }

    public void UpdateBeneficiaries_ReturnsError_WhenMoreThanOnePensionBeneficiarySelected()
    {
        var beneficiaries = new List<(int? SequenceNumber, BeneficiaryDetails Details, BeneficiaryAddress Address)>
        {
            (null, BeneficiaryDetails.CreateNonCharity("Wife", "Jane", "Doe", DateTime.Now.AddYears(-25).Date, 75, true, "note1").Right(),
            BeneficiaryAddress.Create("Towers Watson Westgate","122-130 Station Road","test dada for addres3","Redhill","Surrey","United Kingdom","GB","RH1 1WS").Right()
            ),
            (null, BeneficiaryDetails.CreateNonCharity("Wife", "Jane", "Doe", DateTime.Now.AddYears(-25).Date, 25, true, "note1").Right(),
            BeneficiaryAddress.Create("Towers Watson Westgate","122-130 Station Road","test dada for addres3","Redhill","Surrey","United Kingdom","GB","RH1 1WS").Right()
            )
        };
        var member = new MemberBuilder().Build();

        var result = member.UpdateBeneficiaries(beneficiaries, DateTimeOffset.UtcNow);

        result.HasValue.Should().BeTrue();
        result.Value.Message.Should().Be("Only one beneficiary is eligible for pension.");
    }

    public void UpdateBeneficiaries_ReturnsError_WhenLumpSumPercentageIsNot100()
    {
        var beneficiaries = new List<(int? SequenceNumber, BeneficiaryDetails Details, BeneficiaryAddress Address)>
        {
            (null, BeneficiaryDetails.CreateNonCharity("Wife", "Jane", "Doe", DateTime.Now.AddYears(-25).Date, 25, false, "note1").Right(),
            BeneficiaryAddress.Create("Towers Watson Westgate","122-130 Station Road","test dada for addres3","Redhill","Surrey","United Kingdom","GB","RH1 1WS").Right()
            ),
            (null, BeneficiaryDetails.CreateNonCharity("Wife", "Jane", "Doe", DateTime.Now.AddYears(-25).Date, 25, true, "note1").Right(),
            BeneficiaryAddress.Create("Towers Watson Westgate","122-130 Station Road","test dada for addres3","Redhill","Surrey","United Kingdom","GB","RH1 1WS").Right()
            )
        };
        var member = new MemberBuilder().Build();

        var result = member.UpdateBeneficiaries(beneficiaries, DateTimeOffset.UtcNow);

        result.HasValue.Should().BeTrue();
        result.Value.Message.Should().Be("Sum of beneficiaries lump sum percentages must be 100.");
    }

    [Input(null, null)]
    [Input("", "")]
    [Input("  ", "  ")]
    [Input("BD OL SH AR BH OC", "BD OL SH AR BH OC")]
    [Input("BD OL SH AR BH OC ND", "BD OL SH AR BH OC")]
    [Input("ND BD OL SH AR BH OC", "BD OL SH AR BH OC")]
    [Input("BD OL SH AR BH ND OC", "BD OL SH AR BH OC")]
    public void UpdateBeneficiaries_WhenEmptyBeneficiariesListEmpty(string initialRecordsIndicator, string expectedRecordsIndicator)
    {
        var beneficiaries = new List<(int? SequenceNumber, BeneficiaryDetails Details, BeneficiaryAddress Address)>();
        var member = new MemberBuilder()
            .RecordsIndicator(initialRecordsIndicator)
            .Build();

        var result = member.UpdateBeneficiaries(beneficiaries, DateTimeOffset.UtcNow);

        result.HasValue.Should().BeFalse();
        member.Beneficiaries.Should().BeEmpty();
        member.RecordsIndicator.Should().Be(expectedRecordsIndicator);
    }

    [Input(null, "ND")]
    [Input("", "ND")]
    [Input("  ", "ND")]
    [Input("BD OL SH AR BH OC", "BD OL SH AR BH OC ND")]
    [Input("BD OL SH AR BH OC ND", "BD OL SH AR BH OC ND")]
    [Input("ND BD OL SH AR BH OC", "ND BD OL SH AR BH OC")]
    [Input("BD OL SH AR BH ND OC", "BD OL SH AR BH ND OC")]
    public void AddsNewBeneficiariesIfValidCollectionIsProvided(string initialRecordsIndicator, string expectedRecordsIndicator)
    {
        var beneficiaries = new List<(int? SequenceNumber, BeneficiaryDetails Details, BeneficiaryAddress Address)>
        {
            (null, BeneficiaryDetails.CreateNonCharity("Wife", "Jane", "Doe", DateTime.Now.AddYears(-25).Date, 75, false, "note1").Right(),
            BeneficiaryAddress.Create("Towers Watson Westgate","122-130 Station Road","test dada for addres3","Redhill","Surrey","United Kingdom","GB","RH1 1WS").Right()
            ),
            (null, BeneficiaryDetails.CreateNonCharity("Wife", "Jane", "Doe", DateTime.Now.AddYears(-25).Date, 25, true, "note1").Right(),
            BeneficiaryAddress.Create("Towers Watson Westgate","122-130 Station Road","test dada for addres3","Redhill","Surrey","United Kingdom","GB","RH1 1WS").Right()
            )
        };
        var member = new MemberBuilder().RecordsIndicator(initialRecordsIndicator).Build();

        var result = member.UpdateBeneficiaries(beneficiaries, DateTimeOffset.UtcNow);

        result.HasValue.Should().BeFalse();
        member.Beneficiaries.Should().NotBeEmpty();
        member.Beneficiaries.Count.Should().Be(2);
        member.Beneficiaries.ToList()[0].Should().NotBeNull();
        member.Beneficiaries.ToList()[0].SequenceNumber.Should().Be(1);
        member.Beneficiaries.ToList()[1].Should().NotBeNull();
        member.Beneficiaries.ToList()[1].SequenceNumber.Should().Be(2);
        member.RecordsIndicator.Should().Be(expectedRecordsIndicator);
    }

    public void UpdateBeneficiaries_ReturnsError_WhenNoneBeneficiariesToUpdateNotFound()
    {
        var beneficiaries = new List<(int? SequenceNumber, BeneficiaryDetails Details, BeneficiaryAddress Address)>
        {
            (null, BeneficiaryDetails.CreateNonCharity("Wife", "Jane", "Doe", DateTime.Now.AddYears(-25).Date, 75, true, "note1").Right(),
            BeneficiaryAddress.Create("Towers Watson Westgate","122-130 Station Road","test dada for addres3","Redhill","Surrey","United Kingdom","GB","RH1 1WS").Right()
            ),
            (null, BeneficiaryDetails.CreateNonCharity("Son", "John", "Doe", DateTime.Now.AddYears(-25).Date, 25, false, "note1").Right(),
            BeneficiaryAddress.Create("Towers Watson Westgate","122-130 Station Road","test dada for addres3","Redhill","Surrey","United Kingdom","GB","RH1 1WS").Right()
            )
        };
        var member = new MemberBuilder().Build();
        member.UpdateBeneficiaries(beneficiaries, DateTimeOffset.UtcNow);

        var newBeneficiaries = new List<(int? SequenceNumber, BeneficiaryDetails Details, BeneficiaryAddress Address)>
        {
            (67, BeneficiaryDetails.CreateNonCharity("Wife", "Jane2", "Doe2", DateTime.Now.AddYears(-25).Date, 75, true, "note1").Right(),
            BeneficiaryAddress.Create("Towers Watson Westgate","122-130 Station Road","test dada for addres3","Redhill","Surrey","United Kingdom","GB","RH1 1WS").Right()
            ),
            (68, BeneficiaryDetails.CreateNonCharity("Son", "John2", "Doe2", DateTime.Now.AddYears(-25).Date, 25, false, "note1").Right(),
            BeneficiaryAddress.Create("Towers Watson Westgate","122-130 Station Road","test dada for addres3","Redhill","Surrey","United Kingdom","GB","RH1 1WS").Right()
            )
        };

        var result = member.UpdateBeneficiaries(newBeneficiaries, DateTimeOffset.UtcNow);

        result.HasValue.Should().BeTrue();
        result.Value.Message.Should().Be("Beneficiaries to be updated do not exist. Ids: 67, 68.");
    }

    public void UpdatesAndAddsNewBeneficiaries()
    {
        var beneficiaries = new List<(int? SequenceNumber, BeneficiaryDetails Details, BeneficiaryAddress Address)>
        {
            (null, BeneficiaryDetails.CreateNonCharity("Wife", "Jane", "Doe", DateTime.Now.AddYears(-25).Date, 75, true, "note1").Right(),
            BeneficiaryAddress.Create("Towers Watson Westgate","122-130 Station Road","test dada for addres3","Redhill","Surrey","United Kingdom","GB","RH1 1WS").Right()
            ),
            (null, BeneficiaryDetails.CreateNonCharity("Son", "John", "Doe", DateTime.Now.AddYears(-25).Date, 25, false, "note1").Right(),
            BeneficiaryAddress.Create("Towers Watson Westgate","122-130 Station Road","test dada for addres3","Redhill","Surrey","United Kingdom","GB","RH1 1WS").Right()
            )
        };
        var member = new MemberBuilder().Build();
        member.UpdateBeneficiaries(beneficiaries, DateTimeOffset.UtcNow);

        var newBeneficiaries = new List<(int? SequenceNumber, BeneficiaryDetails Details, BeneficiaryAddress Address)>
        {
            (1, BeneficiaryDetails.CreateNonCharity("Wife", "Jane2", "Doe2", DateTime.Now.AddYears(-25).Date, 50, true, "note1").Right(),
            BeneficiaryAddress.Create("Towers Watson Westgate","122-130 Station Road","test dada for addres3","Redhill","Surrey","United Kingdom","GB","RH1 1WS").Right()
            ),
            (2, BeneficiaryDetails.CreateNonCharity("Son", "John2", "Doe2", DateTime.Now.AddYears(-25).Date, 25, false, "note1").Right(),
            BeneficiaryAddress.Create("Towers Watson Westgate","122-130 Station Road","test dada for addres3","Redhill","Surrey","United Kingdom","GB","RH1 1WS").Right()
            ),
            (null, BeneficiaryDetails.CreateNonCharity("Daughter", "Lisa", "Doe", DateTime.Now.AddYears(-25).Date, 25, false, "note1").Right(),
            BeneficiaryAddress.Create("Towers Watson Westgate","122-130 Station Road","test dada for addres3","Redhill","Surrey","United Kingdom","GB","RH1 1WS").Right()
            )
        };

        var result = member.UpdateBeneficiaries(newBeneficiaries, DateTimeOffset.UtcNow);

        result.HasValue.Should().BeFalse();
        member.Beneficiaries.Should().NotBeEmpty();
        member.Beneficiaries.Count.Should().Be(5);

        member.Beneficiaries.ToList()[0].Should().NotBeNull();
        member.Beneficiaries.ToList()[0].SequenceNumber.Should().Be(1);
        member.Beneficiaries.ToList()[0].RevokeDate.Should().NotBeNull();
        member.Beneficiaries.ToList()[0].BeneficiaryDetails.Forenames.Should().Be("Jane");

        member.Beneficiaries.ToList()[1].Should().NotBeNull();
        member.Beneficiaries.ToList()[1].SequenceNumber.Should().Be(2);
        member.Beneficiaries.ToList()[1].RevokeDate.Should().NotBeNull();
        member.Beneficiaries.ToList()[1].BeneficiaryDetails.Forenames.Should().Be("John");

        member.Beneficiaries.ToList()[2].Should().NotBeNull();
        member.Beneficiaries.ToList()[2].SequenceNumber.Should().Be(3);
        member.Beneficiaries.ToList()[2].RevokeDate.Should().BeNull();
        member.Beneficiaries.ToList()[2].BeneficiaryDetails.Forenames.Should().Be("Jane2");

        member.Beneficiaries.ToList()[3].Should().NotBeNull();
        member.Beneficiaries.ToList()[3].SequenceNumber.Should().Be(4);
        member.Beneficiaries.ToList()[3].RevokeDate.Should().BeNull();
        member.Beneficiaries.ToList()[3].BeneficiaryDetails.Forenames.Should().Be("John2");

        member.Beneficiaries.ToList()[4].Should().NotBeNull();
        member.Beneficiaries.ToList()[4].SequenceNumber.Should().Be(5);
        member.Beneficiaries.ToList()[4].RevokeDate.Should().BeNull();
        member.Beneficiaries.ToList()[4].BeneficiaryDetails.Forenames.Should().Be("Lisa");
    }

    public void ReturnsActiveBeneficiaries()
    {
        var beneficiaries = new List<(int? SequenceNumber, BeneficiaryDetails Details, BeneficiaryAddress Address)>
        {
            (null, BeneficiaryDetails.CreateNonCharity("Wife", "Jane", "Doe", DateTime.Now.AddYears(-25).Date, 75, true, "note1").Right(),
            BeneficiaryAddress.Create("Towers Watson Westgate","122-130 Station Road","test dada for addres3","Redhill","Surrey","United Kingdom","GB","RH1 1WS").Right()
            ),
            (null, BeneficiaryDetails.CreateNonCharity("Son", "John", "Doe", DateTime.Now.AddYears(-25).Date, 25, false, "note1").Right(),
            BeneficiaryAddress.Create("Towers Watson Westgate","122-130 Station Road","test dada for addres3","Redhill","Surrey","United Kingdom","GB","RH1 1WS").Right()
            )
        };
        var member = new MemberBuilder().Build();
        member.UpdateBeneficiaries(beneficiaries, DateTimeOffset.UtcNow);

        var newBeneficiaries = new List<(int? SequenceNumber, BeneficiaryDetails Details, BeneficiaryAddress Address)>
        {
            (1, BeneficiaryDetails.CreateNonCharity("Wife", "Jane2", "Doe2", DateTime.Now.AddYears(-25).Date, 50, true, "note1").Right(),
            BeneficiaryAddress.Create("Towers Watson Westgate","122-130 Station Road","test dada for addres3","Redhill","Surrey","United Kingdom","GB","RH1 1WS").Right()
            ),
            (2, BeneficiaryDetails.CreateNonCharity("Son", "John2", "Doe2", DateTime.Now.AddYears(-25).Date, 25, false, "note1").Right(),
            BeneficiaryAddress.Create("Towers Watson Westgate","122-130 Station Road","test dada for addres3","Redhill","Surrey","United Kingdom","GB","RH1 1WS").Right()
            ),
            (null, BeneficiaryDetails.CreateNonCharity("Daughter", "Lisa", "Doe", DateTime.Now.AddYears(-25).Date, 25, false, "note1").Right(),
            BeneficiaryAddress.Create("Towers Watson Westgate","122-130 Station Road","test dada for addres3","Redhill","Surrey","United Kingdom","GB","RH1 1WS").Right()
            )
        };
        member.UpdateBeneficiaries(newBeneficiaries, DateTimeOffset.UtcNow);

        var result = member.ActiveBeneficiaries();

        result.Count().Should().Be(3);
        result.Select(x => x.BeneficiaryDetails.Forenames).Except(new List<string> { "Jane2", "John2", "Lisa" }).Count().Should().Be(0);
    }

    public void ReturnsFalse_WhenMembersDateOfBirthIsNotSet()
    {
        var sut = new MemberBuilder()
            .DateOfBirth(null)
            .Build();

        var result = sut.HasDateOfBirth();

        result.Should().BeFalse();
    }

    public void ReturnsTrue_WhenMembersDateOfBirthIsSet()
    {
        var dob = DateTimeOffset.UtcNow.AddYears(-44);
        var sut = new MemberBuilder()
            .DateOfBirth(dob)
            .Build();

        var result = sut.HasDateOfBirth();

        result.Should().BeTrue();
        sut.PersonalDetails.DateOfBirth.Should().Be(dob);
    }

    public void ReturnsTrue_WhenMemberIsEligibleForRetirementCalculation_WhenMemberStatusDeferred()
    {
        var sut = new MemberBuilder()
            .Status(MemberStatus.Deferred)
            .Build();

        var result = sut.CanCalculateRetirement();

        result.Should().BeTrue();
    }

    public void ReturnsTrue_WhenMemberIsEligibleForRetirementCalculation_WhenMemberStatusActive()
    {
        var sut = new MemberBuilder()
            .Status(MemberStatus.Active)
            .Build();

        var result = sut.CanCalculateRetirement();

        result.Should().BeTrue();
    }

    public void ReturnsFalse_WhenMemberIsNotEligibleForRetirementCalculation_WhenMemberStatusIsAnyOtherThanActiveOrDeferred()
    {
        var sut = new MemberBuilder()
            .Status(MemberStatus.Pensioner)
            .Build();

        var result = sut.CanCalculateRetirement();

        result.Should().BeFalse();
    }

    public void ReturnsCorrentTransferApplicationStatus_WhenMemberStatusIsNotActiveAndTransferCalculationUnavailable()
    {
        var sut = new MemberBuilder()
            .Status(MemberStatus.Deferred)
            .Build();

        var result = sut.GetTransferApplicationStatus(null);

        result.Should().Be(TransferApplicationStatus.Undefined);
    }

    public void ReturnsCorrentTransferApplicationStatus_WhenMemberStatusIsNotActiveAndTransferCalculationIsAvailable()
    {
        var sut = new MemberBuilder()
            .Status(MemberStatus.Deferred)
            .Build();
        var calculation = new MdpService.Domain.Mdp.Calculations.TransferCalculation("RBS", "1111124", "{}", DateTimeOffset.UtcNow);

        var result = sut.GetTransferApplicationStatus(calculation);

        result.Should().Be(TransferApplicationStatus.NotStartedTA);
    }


    public void CalculatesLatestRetirementDateUsingLatestRetirementAgeConstant()
    {
        var datesResponse = JsonSerializer.Deserialize<RetirementDatesAgesResponse>(TestData.RetitementDateAgesResponse, SerialiationBuilder.Options());
        var retirementDates = new RetirementDatesAges(datesResponse);
        var sut = new MemberBuilder()
            .DateOfBirth(new DateTimeOffset(1950, 1, 2, 0, 0, 0, TimeSpan.FromHours(0)))
            .Build();

        var result = sut.LatestRetirementDate(datesResponse.RetirementDates.LatestRetirementDate, retirementDates.GetLatestRetirementAge(), "RBS", new DateTime(2022, 1, 1));

        result.Should().Be(new DateTime(2025, 1, 2));
        datesResponse.RetirementAges.LatestRetirementAge.Should().BeNull();
    }

    public void CalculatesLatestRetirementDateUsingLatestRetirementAgeFromCalcApi()
    {
        var datesResponse = JsonSerializer.Deserialize<RetirementDatesAgesResponse>(TestData.RetitementDateAgesResponseV2, SerialiationBuilder.Options());
        var retirementDates = new RetirementDatesAges(datesResponse);
        var sut = new MemberBuilder()
            .DateOfBirth(new DateTimeOffset(1950, 1, 2, 0, 0, 0, TimeSpan.FromHours(0)))
            .Build();

        var result = sut.LatestRetirementDate(datesResponse.RetirementDates.LatestRetirementDate, retirementDates.GetLatestRetirementAge(), "RBS", new DateTime(2022, 1, 1));

        datesResponse.RetirementAges.LatestRetirementAge.Should().NotBeNull();
        datesResponse.RetirementAges.LatestRetirementAge.Should().Be(75);
    }

    public void CalculatesLatestRetirement1()
    {
        var datesResponse = JsonSerializer.Deserialize<RetirementDatesAgesResponse>(TestData.RetitementDateAgesResponseV2, SerialiationBuilder.Options());
        var sut = new MemberBuilder()
            .DateOfBirth(new DateTimeOffset(1950, 1, 2, 0, 0, 0, TimeSpan.FromHours(0)))
            .Build();

        var result = sut.LatestRetirementDate(datesResponse.RetirementDates.LatestRetirementDate, 75, "RBS", new DateTime(2022, 1, 1));

        result.Should().Be(new DateTime(2022, 11, 07));
    }

    public void CalculatesLatestRetirementDateForBclUser()
    {
        var datesResponse = JsonSerializer.Deserialize<RetirementDatesAgesResponse>(TestData.RetitementDateAgesResponseV2, SerialiationBuilder.Options());
        var now = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.FromHours(0));

        var sut = new MemberBuilder()
            .DateOfBirth(new DateTimeOffset(1950, 1, 2, 0, 0, 0, TimeSpan.FromHours(0)))
            .Build();

        var result = sut.LatestRetirementDate(datesResponse.RetirementDates.LatestRetirementDate, 75, "BCL", now);

        result.Should().Be(new DateTime(2022, 11, 07));
    }

    public void CalculatesAgeAndMonth()
    {
        var now = new DateTimeOffset(2022, 10, 6, 0, 0, 0, TimeSpan.FromHours(0));
        var sut = new MemberBuilder()
            .DateOfBirth(new DateTimeOffset(1950, 1, 2, 0, 0, 0, TimeSpan.FromHours(0)))
            .Build();

        var result = sut.GetAgeAndMonth(now);

        result.Should().Be("72Y9M");
    }

    public void CalculatesAgeAndMonth2()
    {
        var now = new DateTimeOffset(2022, 10, 6, 0, 0, 0, TimeSpan.FromHours(0));
        var sut = new MemberBuilder()
            .DateOfBirth(null)
            .Build();

        var result = sut.GetAgeAndMonth(now);

        result.Should().BeNull();
    }

    public void CalculatesDcRetirementDate()
    {
        var now = new DateTimeOffset(2022, 10, 6, 0, 0, 0, TimeSpan.FromHours(0));
        var sut = new MemberBuilder()
            .DateOfBirth(new DateTimeOffset(1950, 1, 2, 0, 0, 0, TimeSpan.FromHours(0)))
            .Build();

        var memberAge = sut.GetAgeAndMonth(now);
        var result = sut.DcRetirementDate(now);

        memberAge.Should().Be("72Y9M");
        result.Should().Be(now.DateTime.AddMonths(RetirementConstants.DcRetirementDateAdditionalPeriodInMonth));
    }

    public void CalculatesDcRetirementDate2()
    {
        var now = new DateTimeOffset(2022, 10, 6, 0, 0, 0, TimeSpan.FromHours(0));
        var member = new MemberBuilder()
            .DateOfBirth(new DateTimeOffset(1948, 1, 2, 0, 0, 0, TimeSpan.FromHours(0)))
            .Build();

        var memberAge = member.GetAgeAndMonth(now);
        var sut = member.DcRetirementDate(now);

        memberAge.Should().Be("74Y9M");
        sut.Should().Be(member.PersonalDetails.DateOfBirth.Value.AddYears(75).Date);
    }

    public void GetExactAge_ReturnsNull_WhenDateOfBirthHasNoValue()
    {
        var now = new DateTimeOffset(2022, 10, 6, 0, 0, 0, TimeSpan.FromHours(0));
        var sut = new MemberBuilder()
            .DateOfBirth(null)
            .Build();

        var result = sut.GetExactAge(now);

        result.Should().BeNull();
    }

    [Input("2022-11-2", 52)]
    [Input("2022-12-2", 52.0833)]
    [Input("2023-1-2", 52.1667)]
    [Input("2023-2-2", 52.25)]
    [Input("2023-3-2", 52.3333)]
    [Input("2023-4-2", 52.4167)]
    [Input("2023-5-2", 52.5)]
    [Input("2023-6-2", 52.5833)]
    [Input("2023-7-2", 52.6667)]
    [Input("2023-8-2", 52.75)]
    [Input("2023-9-2", 52.8333)]
    [Input("2023-10-2", 52.9167)]
    [Input("2023-8-1", 52.7489)]
    [Input("2023-7-31", 52.7461)]
    [Input("2023-7-30", 52.7434)]
    [Input("2023-7-29", 52.7406)]
    [Input("2023-7-28", 52.7379)]
    [Input("2023-8-3", 52.7527)]
    [Input("2023-8-4", 52.7555)]
    [Input("2023-8-5", 52.7582)]
    [Input("2023-8-6", 52.761)]
    [Input("2023-8-7", 52.7637)]
    public void ReturnsCorrectExactAge(string nowDate, double expectedResult)
    {
        var now = new DateTimeOffset(
            new DateTime(int.Parse(nowDate.Split("-")[0]), int.Parse(nowDate.Split("-")[1]), int.Parse(nowDate.Split("-")[2])));
        var sut = new MemberBuilder()
            .DateOfBirth(new DateTimeOffset(new DateTime(1970, 11, 2)))
            .Build();

        var result = Math.Round(sut.GetExactAge(now).Value, 4);

        result.Should().Be(expectedResult);
    }

    public void ReturnsTrue_WhenMemberHasLinkedMembers()
    {
        var sut = new MemberBuilder()
            .LinkedMembers(new LinkedMember("1111124", "RBS", "1234567", "BCL"))
            .Build();

        var result = sut.HasLinkedMembers();

        result.Should().BeTrue();
    }

    public void ReturnsFalse_WhenMemberHasNotLinkedMembers()
    {
        var sut = new MemberBuilder()
            .Build();

        var result = sut.HasLinkedMembers();

        result.Should().BeFalse();
    }

    public void ReturnsNullAgeOnSelectedDate_whenDateOfBirthIsNull()
    {
        var now = new DateTimeOffset(2022, 10, 6, 0, 0, 0, TimeSpan.FromHours(0));
        var sut = new MemberBuilder()
            .DateOfBirth(null)
            .Build();

        var result = sut.AgeOnSelectedDate(now.Date);

        result.Should().BeNull();
    }

    public void ReturnsFloorRoundedAgeOnSelectedDate()
    {
        var now = new DateTimeOffset(2022, 10, 6, 0, 0, 0, TimeSpan.FromHours(0));
        var sut = new MemberBuilder()
            .DateOfBirth(new DateTimeOffset(1957, 1, 12, 0, 0, 0, TimeSpan.FromHours(0)))
            .Build();

        var result = sut.AgeOnSelectedDate(now.Date);

        result.Should().Be(65);
    }

    public void ReturnsCorrectLastRTP9ClosedOrAbandoned1()
    {
        var sut = new MemberBuilder()
            .PaperRetirementApplications()
            .Build();

        var result = sut.IsLastRTP9ClosedOrAbandoned();

        result.Should().BeFalse();
    }

    public void ReturnsCorrectLastRTP9ClosedOrAbandoned2()
    {
        var paperApplication1 = new PaperRetirementApplication(
            "AC", "RTP9", "11111111", "someType", "someStatus", null, null, new BatchCreateDetails("paper"));
        var paperApplication2 = new PaperRetirementApplication(
            "AC", "RTP9", "11111112", "someType", "someStatus", DateTimeOffset.UtcNow.AddDays(-23), null, new BatchCreateDetails("paper"));

        var sut = new MemberBuilder()
            .PaperRetirementApplications(paperApplication1, paperApplication2)
            .Build();

        var result = sut.IsLastRTP9ClosedOrAbandoned();

        result.Should().BeTrue();
    }

    public void ReturnsTrue_WhenRTP9CaseIsAbandoned()
    {
        var paperApplication1 = new PaperRetirementApplication(
            "AC", "RTP9", "11111111", "someType", "someStatus", null, null, new BatchCreateDetails("paper"));
        var paperApplication2 = new PaperRetirementApplication(
            "AC", "RTP9", "11111112", "someType", "someStatus", DateTimeOffset.UtcNow.AddDays(-23), null, new BatchCreateDetails("paper"));

        var sut = new MemberBuilder()
            .PaperRetirementApplications(paperApplication1, paperApplication2)
            .Build();

        var result = sut.IsRTP9CaseAbandoned("11111112");

        result.Should().BeTrue();
    }

    public void ReturnsFalse_WhenRTP9CaseIsNotAbandoned()
    {
        var paperApplication1 = new PaperRetirementApplication(
            "AC", "RTP9", "11111111", "someType", "someStatus", null, null, new BatchCreateDetails("paper"));
        var paperApplication2 = new PaperRetirementApplication(
            "PF", "RTP9", "11111112", "someType", "someStatus", DateTimeOffset.UtcNow.AddDays(-23), null, new BatchCreateDetails("paper"));

        var sut = new MemberBuilder()
            .PaperRetirementApplications(paperApplication1, paperApplication2)
            .Build();

        var result = sut.IsRTP9CaseAbandoned("11111112");

        result.Should().BeFalse();
    }

    public void ReturnsFalse_WhenRTP9CaseIsNotFound()
    {
        var paperApplication1 = new PaperRetirementApplication(
            "AC", "RTP9", "11111111", "someType", "someStatus", null, null, new BatchCreateDetails("paper"));
        var paperApplication2 = new PaperRetirementApplication(
            "PF", "RTP9", "11111112", "someType", "someStatus", DateTimeOffset.UtcNow.AddDays(-23), null, new BatchCreateDetails("paper"));

        var sut = new MemberBuilder()
            .PaperRetirementApplications(paperApplication1, paperApplication2)
            .Build();

        var result = sut.IsRTP9CaseAbandoned("11111113");

        result.Should().BeFalse();
    }

    [Input("1966-11-09", 60, "rbs", 60)]
    [Input("1966-11-09", 60, "BCL", 56)]
    [Input("1956-11-09", 60, "BCL", 66)]
    [Input(null, 60, "BCL", 60)]
    public void ReturnsCorrectLastRTP9ClosedOrAbandoned2111(string dateOfBirth, int normalRetirementAge, string businessGroup, int expectedResult)
    {
        var now = new DateTimeOffset(new DateTime(2023, 1, 5), TimeSpan.FromHours(0));
        var sut = new MemberBuilder()
            .BusinessGroup(businessGroup)
            .DateOfBirth(dateOfBirth == null ? null : new DateTimeOffset(DateTime.Parse(dateOfBirth), TimeSpan.FromHours(0)))
            .Build();

        var result = sut.AgeAtRetirement(normalRetirementAge, now);

        result.Should().Be(expectedResult);
    }

    [Input("1985-10-10", "2016-10-10", MemberLifeStage.NotEligibleToRetire, MemberStatus.Active)]
    [Input("1969-10-10", "2016-10-10", MemberLifeStage.PreRetiree, MemberStatus.Active)]
    [Input("1968-01-10", "2016-10-10", MemberLifeStage.EligibleToApplyForRetirement, MemberStatus.Active)]
    [Input("1962-10-10", "2016-10-10", MemberLifeStage.EligibleToRetire, MemberStatus.Active)]
    [Input("1955-10-10", "2016-10-10", MemberLifeStage.LateRetirement, MemberStatus.Active)]
    [Input("1947-09-27", "2016-10-10", MemberLifeStage.CloseToLatestRetirementAge, MemberStatus.Active)]
    [Input("1947-09-02", "2016-10-10", MemberLifeStage.OverLatestRetirementAge, MemberStatus.Active)]
    [Input("1962-05-10", "2021-12-10", MemberLifeStage.NewlyRetired, MemberStatus.Pensioner)]
    [Input("1963-05-10", "2016-10-10", MemberLifeStage.EstablishedRetiree, MemberStatus.Pensioner)]

    public void ReturnsCloseToLatestRetirementAge_WhenTodayIs38DaysFrom75thBirthDate(string dob, string earliestRetirement, MemberLifeStage expectedMmberLifeStage, MemberStatus memberStatus)
    {
        var dateOfBirth = new DateTimeOffset(DateTime.Parse(dob));
        var earliestRetirementDate = new DateTimeOffset(DateTime.Parse(earliestRetirement));
        var now = new DateTimeOffset(new DateTime(2022, 07, 27));
        var retirementDates = new RetirementDatesAges(new RetirementDatesAgesDto()
        {
            EarliestRetirementDate = earliestRetirementDate,
            NormalMinimumPensionDate = new DateTimeOffset(new DateTime(2025, 7, 30)),
            NormalRetirementDate = new DateTimeOffset(new DateTime(2030, 7, 30)),
            EarliestRetirementAge = 55,
            NormalMinimumPensionAge = 55,
            NormalRetirementAge = 60
        });

        var member = new MemberBuilder()
            .MinimumPensionAge(59)
            .DateOfBirth(dateOfBirth)
            .Status(memberStatus)
            .Build();

        var lifeStage = member.GetLifeStage(now, 3, 10, retirementDates);
        lifeStage.Should().Be(expectedMmberLifeStage);
    }

    [Input("RTP9", false)]
    [Input("TOP9", true)]
    public void ReturnsTransferCase(string caseCode, bool hasTransferCase)
    {
        var paperApplication = new PaperRetirementApplication(null, caseCode, null, null, null, null, null, new BatchCreateDetails("paper"));
        var sut = new MemberBuilder().PaperRetirementApplications(paperApplication).Build();

        var result = sut.TransferPaperCase();

        result.IsSome.Should().Be(hasTransferCase);
    }

    [Input(TransferApplicationStatus.SubmittedTA)]
    [Input(TransferApplicationStatus.SubmitStarted)]
    public void ReturnsTrueWhenTransferApplicationStatusStartedTaOrSubmitStarted(TransferApplicationStatus status)
    {
        var sut = new MemberBuilder()
            .Status(MemberStatus.Deferred)
            .Build();
        var calculation = new MdpService.Domain.Mdp.Calculations.TransferCalculation("RBS", "1111124", "{}", DateTimeOffset.UtcNow);
        calculation.LockTransferQoute();
        if (status == TransferApplicationStatus.SubmitStarted)
            calculation.SetStatus(status);

        var result = sut.IsTransferStatusStatedTaOrSubmitStarted(calculation);

        result.Should().BeTrue();
    }

    [Input(null, "0Y0M0W0D")]
    [Input("1970-8-17", "53Y0M0W0D")]
    [Input("1970-10-31", "52Y9M2W3D")]
    public void ReturnsCorrectAgeInIsoDurationFormat(string dateOfBirth, string expectedCurrentAgeIso)
    {
        var now = new DateTimeOffset(new DateTime(2023, 8, 17));
        var sut = new MemberBuilder()
            .DateOfBirth(dateOfBirth != null ? new DateTimeOffset(DateTime.Parse(dateOfBirth)) : null)
            .Build();


        var result = sut.CurrentAgeIso(now);

        result.Should().Be(expectedCurrentAgeIso);
    }

    public void TenureInYears_ShouldReturnCorrectYears_WhenBothDatesAreProvided()
    {
        var dateJoinedCompany = new DateTimeOffset(new DateTime(2015, 6, 1));
        var datePensionableServiceStarted = new DateTimeOffset(new DateTime(2023, 6, 1));
        var sut = new MemberBuilder()
            .DateJoinedCompany(dateJoinedCompany)
            .DatePensionServiceStarted(datePensionableServiceStarted)
            .Build();

        var tenure = sut.TenureInYears();

        tenure.Should().Be(8);
    }

    public void TenureInYears_ShouldReturnNull_WhenOnlyDatePensionableServiceStartedIsProvided()
    {
        var datePensionableServiceStarted = new DateTimeOffset(new DateTime(2023, 6, 1));
        var sut = new MemberBuilder()
            .DatePensionServiceStarted(datePensionableServiceStarted)
            .Build();

        var tenure = sut.TenureInYears();

        tenure.Should().BeNull();
    }

    public void TenureInYears_ShouldReturnNull_WhenOnlyDateJoinedCompanyIsProvided()
    {
        var dateJoinedCompany = new DateTimeOffset(new DateTime(2015, 6, 1));
        var sut = new MemberBuilder()
            .DateJoinedCompany(dateJoinedCompany)
            .Build();

        var tenure = sut.TenureInYears();

        tenure.Should().BeNull();
    }

    public void TenureInYears_ShouldReturnNull_WhenNoDatesAreProvided()
    {
        var sut = new MemberBuilder().Build();

        var tenure = sut.TenureInYears();

        tenure.Should().BeNull();
    }

    public void TenureInYears_ShouldSubtractOneYear_IfEndDateIsBeforeCurrentYearEnd()
    {
        var dateJoinedCompany = new DateTimeOffset(new DateTime(2015, 6, 1));
        var datePensionableServiceStarted = new DateTimeOffset(new DateTime(2023, 5, 31));
        var sut = new MemberBuilder()
            .DateJoinedCompany(dateJoinedCompany)
            .DatePensionServiceStarted(datePensionableServiceStarted)
            .Build();

        var tenure = sut.TenureInYears();

        tenure.Should().Be(7);
    }

    public void IsDeathCasesLogged_ShouldReturnTrue_ForExistingDeathCases()
    {
        var paperApplication1 = new PaperRetirementApplication(
            "AB", "DDD9", "11111111", "someType", "someStatus", null, null, new BatchCreateDetails("paper"));
        var paperApplication2 = new PaperRetirementApplication(
            "AC", "RTP9", "11111112", "someType", "someStatus", DateTimeOffset.UtcNow.AddDays(-23), null, new BatchCreateDetails("paper"));

        var sut = new MemberBuilder()
            .PaperRetirementApplications(paperApplication1, paperApplication2)
            .Build();

        var result = sut.IsDeathCasesLogged();

        result.Should().BeTrue();
    }

    public void IsDeathCasesLogged_ShouldReturnFalse_ForDeathCases_WithCodeAC()
    {
        var paperApplication1 = new PaperRetirementApplication(
            "AC", "DDR9", "11111111", "someType", "someStatus", null, null, new BatchCreateDetails("paper"));

        var sut = new MemberBuilder()
            .PaperRetirementApplications(paperApplication1)
            .Build();

        var result = sut.IsDeathCasesLogged();

        result.Should().BeFalse();
    }


    public void IsDeathCasesLogged_ShouldReturnFalse_IfThereAreNoDeathCases()
    {
        var paperApplication1 = new PaperRetirementApplication(
            "AC", "RTP9", "11111111", "someType", "someStatus", null, null, new BatchCreateDetails("paper"));
        var paperApplication2 = new PaperRetirementApplication(
            "AC", "RTP9", "11111112", "someType", "someStatus", DateTimeOffset.UtcNow.AddDays(-23), null, new BatchCreateDetails("paper"));

        var sut = new MemberBuilder()
            .PaperRetirementApplications(paperApplication1, paperApplication2)
            .Build();

        var result = sut.IsDeathCasesLogged();

        result.Should().BeFalse();
    }
}