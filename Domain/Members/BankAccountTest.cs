using System;
using FluentAssertions;
using WTW.MdpService.Domain.Members;
using WTW.TestCommon.FixieConfig;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Domain.Members;

public class BankAccountTest
{
    public void CreatesUkBankAccount_WhenValidDataProvided()
    {
        var bank = Bank.CreateUkBank("123456", "Natwest", "Manchester").Right();
        var utcNow = DateTimeOffset.UtcNow;

        var sut = BankAccount.CreateUkAccount(1, "John Doe", "12345678", utcNow, bank);

        sut.IsRight.Should().BeTrue();
        sut.IsLeft.Should().BeFalse();
        sut.Right().Bank.Should().BeSameAs(bank);
        sut.Right().ReferenceNumber.Should().BeNull();
        sut.Right().BusinessGroup.Should().BeNull();
        sut.Right().AccountName.Should().Be("John Doe");
        sut.Right().AccountNumber.Should().Be("12345678");
        sut.Right().SequenceNumber.Should().Be(1);
        sut.Right().EffectiveDate.Should().Be(utcNow);
        sut.Right().Iban.Should().BeNull();
    }

    [Input(null)]
    [Input("")]
    [Input("        ")]
    [Input("1234567812")]
    [Input("1234567")]
    public void ReturnsError_WhenInValidUkAccountNumberProvided(string accountNumber)
    {
        var bank = Bank.CreateUkBank("123456", "Natwest", "Manchester").Right();
        var utcNow = DateTimeOffset.UtcNow;

        var sut = BankAccount.CreateUkAccount(1, "John Doe", accountNumber, utcNow, bank);

        sut.IsRight.Should().BeFalse();
        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("Invalid account number: Must be 8 digits length.");
    }

    [Input(null)]
    [Input("")]
    [Input("This should be absolutely too long bank account name for For any country in the world")]
    public void ReturnsError_WhenInValidUkAccountNameProvided(string accountName)
    {
        var bank = Bank.CreateUkBank("123456", "Natwest", "Manchester").Right();
        var utcNow = DateTimeOffset.UtcNow;

        var sut = BankAccount.CreateUkAccount(1, accountName, "12345678", utcNow, bank);

        sut.IsRight.Should().BeFalse();
        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("Invalid account name: Must be between 1 and 40 digits length.");
    }

    public void Throws_WhenInvalidSequenceNumberIsprovidedForUkBankAccount()
    {
        var bank = Bank.CreateUkBank("123456", "Natwest", "Manchester").Right();
        var utcNow = DateTimeOffset.UtcNow;

        var action = () => BankAccount.CreateUkAccount(0, "John Doe", "12345678", utcNow, bank);

        action.Should().Throw<ArgumentException>();
    }

    public void Throws_WhenInternationalBankIsProvidedForUkAccount()
    {
        var bank = Bank.CreateNonUkBank("12345678", "12345678901", "Natwest", "Manchester", "LT").Right();
        var utcNow = DateTimeOffset.UtcNow;

        var action = () => BankAccount.CreateUkAccount(1, "John Doe", "12345678", utcNow, bank);

        action.Should().Throw<ArgumentException>();
    }

    public void CreatesNonUkBankAccount_WhenValidDataProvided()
    {
        var bank = Bank.CreateNonUkBank("12345678", "12345678901", "Natwest", "Manchester", "LT").Right();
        var utcNow = DateTimeOffset.UtcNow;

        var sut = BankAccount.CreateIbanAccount(1, "John Doe", "LT1234567890123456", utcNow, bank);

        sut.IsRight.Should().BeTrue();
        sut.IsLeft.Should().BeFalse();
        sut.Right().Bank.Should().BeSameAs(bank);
        sut.Right().AccountName.Should().Be("John Doe");
        sut.Right().AccountNumber.Should().BeNull();
        sut.Right().SequenceNumber.Should().Be(1);
        sut.Right().EffectiveDate.Should().Be(utcNow);
        sut.Right().Iban.Should().Be("LT1234567890123456");
    }

    [Input(null)]
    [Input("")]
    [Input("LT1234567890123456123569879879879821321321321321321")]
    [Input("    ")]
    public void ReturnsError_WhenInValidIbanProvided(string accountNumber)
    {
        var bank = Bank.CreateNonUkBank("12345678", "12345678901", "Natwest", "Manchester", "LT").Right();
        var utcNow = DateTimeOffset.UtcNow;

        var sut = BankAccount.CreateIbanAccount(1, "John Doe", accountNumber, utcNow, bank);

        sut.IsRight.Should().BeFalse();
        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("Invalid IBAN: Must be between 1 and 34 digits length.");
    }

    [Input(null)]
    [Input("")]
    [Input("This should be absolutely too long bank account name for For any country in the world")]
    public void ReturnsError_WhenInValidNonUkAccountNameProvided(string accountName)
    {
        var bank = Bank.CreateNonUkBank("12345678", "12345678901", "Natwest", "Vilnius", "LT").Right();
        var utcNow = DateTimeOffset.UtcNow;

        var sut = BankAccount.CreateIbanAccount(1, accountName, "LT1234567890123456123561", utcNow, bank);

        sut.IsRight.Should().BeFalse();
        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be("Invalid account name: Must be between 1 and 40 digits length.");
    }

    public void Throws_WhenUkBankIsProvidedForInternationalAccount()
    {
        var bank = Bank.CreateUkBank("123456", "Natwest", "Manchester").Right();
        var utcNow = DateTimeOffset.UtcNow;

        var action = () => BankAccount.CreateIbanAccount(1, "John Doe", "LT1234567890123456", utcNow, bank);

        action.Should().Throw<ArgumentException>();
    }

    public void Throws_WhenInvalidSequenceNumberIsprovidedForNonUkBankAccount()
    {
        var bank = Bank.CreateNonUkBank("12345678", "12345678901", "Natwest", "Vilnius", "LT").Right();
        var utcNow = DateTimeOffset.UtcNow;

        var action = () => BankAccount.CreateIbanAccount(0, "John Doe", "LT1234567890123456", utcNow, bank);

        action.Should().Throw<ArgumentException>();
    }
}