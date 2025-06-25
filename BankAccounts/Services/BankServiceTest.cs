using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.BankAccounts;
using WTW.MdpService.BankAccounts.Services;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.Infrastructure.BankService;
using WTW.MdpService.Infrastructure.IpaService;
using WTW.MdpService.Infrastructure.MemberDb;
using WTW.MdpService.Test.Domain.Members;
using WTW.TestCommon.FixieConfig;
using WTW.TestCommon.Helpers;
using WTW.Web;
using WTW.Web.LanguageExt;
namespace WTW.MdpService.Test.BankAccounts.Services;

public class BankServiceTest
{
    protected readonly BankService _sut;
    protected readonly Mock<ILogger<BankService>> _loggerMock;
    protected readonly Mock<IBankServiceClient> _bankServiceClientMock;
    protected readonly Mock<IIpaServiceClient> _ipaServiceClientMock;
    protected readonly Mock<IMemberRepository> _memberRepositoryMock;
    protected readonly string _businessGroup;
    protected readonly string _referenceNumber;

    public BankServiceTest()
    {
        _loggerMock = new Mock<ILogger<BankService>>();
        _bankServiceClientMock = new Mock<IBankServiceClient>();
        _ipaServiceClientMock = new Mock<IIpaServiceClient>();
        _memberRepositoryMock = new Mock<IMemberRepository>();

        _businessGroup = "ABC";
        _referenceNumber = "1234567";

        _sut = new BankService(
            _loggerMock.Object,
            _bankServiceClientMock.Object,
            _ipaServiceClientMock.Object,
            _memberRepositoryMock.Object);
    }
    public static class GetBankAccountClientResponseFactory
    {
        public static GetBankAccountClientResponse Create()
        {
            return new GetBankAccountClientResponse()
            {
                Bgroup = "ABC",
                Refno = "1234567",
                Seqno = 1,
                AccountName = "John Doe",
                BankAccountNumber = "12345678",
                LocalSortCode = "123456",
                BankName = "Natwest",
                BankCity = "Manchester",
                Country = "GB",
                BankCountryCode = "GB",
                BankAccountCurrency = "GBP",
            };
        }
    }
    public static class BankAccountResponseV2Factory
    {
        public static BankAccountResponseV2 Create(bool isIban = false, bool isNonUk = false)
        {
            return new BankAccountResponseV2()
            {
                AccountName = "John Doe",
                AccountNumber = "12345678",
                Iban = null,
                SortCode = "123456",
                SortCodeFormatted = "12-34-56",
                Bic = null,
                ClearingCode = null,
                BankName = "Natwest",
                BankCity = "Manchester",
                BankCountry = isNonUk ? (isIban ? "DE" : "PH") : "GB",
                BankCountryCode = isNonUk ? (isIban ? "DE" : "PH") : "GB",
                BankAccountCurrency = isNonUk ? (isIban ? "EUR" : "PHP") : "GBP",
            };
        }
    }
    public static class ValidateBankAccountRequestFactory
    {
        public static ValidateBankAccountRequest Create(bool isIban = false, string accountName = "John Doe")
        {
            return new ValidateBankAccountRequest()
            {
                AccountName = accountName,
                BankCountryCode = isIban ? "DE" : "GB",
                SortCode = "123456",
                AccountNumber = "12345678",
                AccountCurrency = isIban ? "EUR" : "GBP",
            };
        }
    }
    public static class ValidateBankAccountResponseFactory
    {
        public static ValidateBankAccountResponse Create(bool isIban = false)
        {
            return new ValidateBankAccountResponse()
            {
                Name = "John Doe",
                BankName = isIban ? "Germany Bank" : "Natwest",
                BranchName = "BranchName",
                StreetAddress = "StreetAddress",
                City = isIban ? "Berlin" : "Manchester",
                Country = isIban ? "DE" : "GB",
                PostCode = "1234",
                AccountNumber = "12345678",
                SortCode = isIban ? null : "123456",
            };
        }
    }
    public static class VerifySafePaymentResponseFactory
    {
        public static VerifySafePaymentResponse Create(string status = "PASS")
        {
            return new VerifySafePaymentResponse()
            {
                Status = status,
                Comment = "Comment"
            };
        }
    }
}
public class FetchBankAccountTest : BankServiceTest
{
    public async Task FetchBankAccountReturnsBankAccountResponseV2_WhenSuccessful()
    {
        var bankAccountClientResponse = GetBankAccountClientResponseFactory.Create();
        var expectedResponse = BankAccountResponseV2Factory.Create();

        _bankServiceClientMock
            .Setup(x => x.GetBankAccount(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(bankAccountClientResponse);

        var result = await _sut.FetchBankAccount(_businessGroup, _referenceNumber);

        result.IsLeft.Should().BeFalse();
        result.IsRight.Should().BeTrue();
        result.Right().Should().BeOfType<BankAccountResponseV2>();
        result.Right().Should().BeEquivalentTo(expectedResponse);

        _loggerMock.VerifyLogging($"{nameof(_sut.FetchBankAccount)}, BusinessGroup: {_businessGroup}, ReferenceNumber: {_referenceNumber}", LogLevel.Information, Times.Once());
    }
    public async Task FetchBankAccountReturnsError_WhenGetBankAccountReturnsNone()
    {
        var bankAccountClientResponse = GetBankAccountClientResponseFactory.Create();
        var expectedResponse = BankAccountResponseV2Factory.Create();

        _bankServiceClientMock
            .Setup(x => x.GetBankAccount(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((GetBankAccountClientResponse)null!);

        var result = await _sut.FetchBankAccount(_businessGroup, _referenceNumber);

        result.IsLeft.Should().BeTrue();
        result.IsRight.Should().BeFalse();

        result.Left().Should().BeEquivalentTo(Error.New(MdpConstants.BankErrorCodes.FailedBankAccountNotFoundErrorCode));

        _loggerMock.VerifyLogging($"{nameof(_sut.FetchBankAccount)}, BusinessGroup: {_businessGroup}, ReferenceNumber: {_referenceNumber}", LogLevel.Information, Times.Once());
        _loggerMock.VerifyLogging($"Bank details not found for BusinessGroup: {_businessGroup}, ReferenceNumber: {_referenceNumber}", LogLevel.Error, Times.Once());

    }
}
public class ValidateBankAccountTest : BankServiceTest
{
    public async Task ValidateBankAccountReturnsValidationBankAccountResponse_WhenSuccessful()
    {
        var expectedResponse = ValidateBankAccountResponseFactory.Create();
        var member = new MemberBuilder()
            .Name("John", "Doe")
            .Build();
        var request = ValidateBankAccountRequestFactory.Create();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(member);
        _bankServiceClientMock
            .Setup(x => x.ValidateBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ValidateBankAccountPayload>()))
            .ReturnsAsync(ValidateBankAccountResponseFactory.Create());
        _bankServiceClientMock
            .Setup(x => x.VerifySafePayment(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<VerifySafePaymentPayload>()))
            .ReturnsAsync(VerifySafePaymentResponseFactory.Create());

        var result = await _sut.ValidateBankAccount(_businessGroup, _referenceNumber, request);

        result.IsLeft.Should().BeFalse();
        result.IsRight.Should().BeTrue();
        result.Right().Should().BeOfType<ValidateBankAccountResponse>();
        result.Right().Should().BeEquivalentTo(expectedResponse);

        _loggerMock.VerifyLogging($"ExternalBankAccountValidation, BusinessGroup: {_businessGroup}, ReferenceNumber: {_referenceNumber}", LogLevel.Information, Times.Once());
    }
    public async Task ValidateBankAccountReturnsValidationBankAccountResponseNonUk_WhenSuccessful()
    {
        var expectedResponse = ValidateBankAccountResponseFactory.Create();
        var member = new MemberBuilder()
            .Name("John", "Doe")
            .Build();
        var request = ValidateBankAccountRequestFactory.Create(isIban: true);
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(member);
        _bankServiceClientMock
            .Setup(x => x.ValidateBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ValidateBankAccountPayload>()))
            .ReturnsAsync(ValidateBankAccountResponseFactory.Create());

        var result = await _sut.ValidateBankAccount(_businessGroup, _referenceNumber, request);

        result.IsLeft.Should().BeFalse();
        result.IsRight.Should().BeTrue();
        result.Right().Should().BeOfType<ValidateBankAccountResponse>();
        result.Right().Should().BeEquivalentTo(expectedResponse);

        _bankServiceClientMock.Verify(x => x.VerifySafePayment(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<VerifySafePaymentPayload>()), Times.Never);
        _loggerMock.VerifyLogging($"ExternalBankAccountValidation, BusinessGroup: {_businessGroup}, ReferenceNumber: {_referenceNumber}", LogLevel.Information, Times.Once());
    }

    public async Task ValidateBankAccountReturnsError_WhenWhenMemberIsNotFound()
    {
        var expectedResponse = ValidateBankAccountResponseFactory.Create();
        var request = ValidateBankAccountRequestFactory.Create();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync((Option<Member>)null);
        _bankServiceClientMock
            .Setup(x => x.ValidateBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ValidateBankAccountPayload>()))
            .ReturnsAsync(ValidateBankAccountResponseFactory.Create());
        _bankServiceClientMock
            .Setup(x => x.VerifySafePayment(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<VerifySafePaymentPayload>()))
            .ReturnsAsync(VerifySafePaymentResponseFactory.Create());

        var result = await _sut.ValidateBankAccount(_businessGroup, _referenceNumber, request);

        result.IsLeft.Should().BeTrue();
        result.IsRight.Should().BeFalse();

        result.Left().Should().BeEquivalentTo(Error.New(MdpConstants.BankErrorCodes.FailedBankAccountNotFoundErrorCode));

        _loggerMock.VerifyLogging($"Member not found for reference number {_referenceNumber} and business group {_businessGroup}", LogLevel.Error, Times.Once());
    }

    [Input("")]
    [Input(null)]
    public async Task ValidateBankAccountReturnsError_WhenSurnameMismatch(string surname)
    {
        var member = new MemberBuilder()
            .Name("John", surname)
            .Build();
        var errorMessage = "The account name does not match the one held on our records. Please try again.";
        var request = ValidateBankAccountRequestFactory.Create();

        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(member);

        var result = await _sut.ValidateBankAccount(_businessGroup, _referenceNumber, request);

        result.Left().Should().BeEquivalentTo(Error.New(MdpConstants.BankErrorCodes.FailedAccountNameValidationErrorCode));
        _loggerMock.VerifyLogging($"VerifyMemberSurnameMatch, BusinessGroup: {_businessGroup}, ReferenceNumber: {_referenceNumber} Error: {errorMessage}", LogLevel.Error, Times.Once());
    }

    [Input("")]
    [Input(null)]
    public async Task ValidateBankAccountReturnsError_WhenAccountNameMismatch(string surname)
    {
        var member = new MemberBuilder()
            .Name("John", "Doe")
            .Build();
        var request = ValidateBankAccountRequestFactory.Create(accountName: $"John {surname}");
        var errorMessage = "The account name does not match the one held on our records. Please try again.";

        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(member);

        var result = await _sut.ValidateBankAccount(_businessGroup, _referenceNumber, request);

        result.Left().Should().BeEquivalentTo(Error.New(MdpConstants.BankErrorCodes.FailedAccountNameValidationErrorCode));
        _loggerMock.VerifyLogging($"VerifyMemberSurnameMatch, BusinessGroup: {_businessGroup}, ReferenceNumber: {_referenceNumber} Error: {errorMessage}", LogLevel.Error, Times.Once());
    }

    public async Task ValidateBankAccountReturnsError_WhenSurnameWithDiacriticsIsNotInAccountName()
    {
        var member = new MemberBuilder()
            .Name("John", "TestRene")
            .Build();
        var request = ValidateBankAccountRequestFactory.Create(accountName: $"John René");
        var errorMessage = "The account name does not match the one held on our records. Please try again.";

        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(member);

        var result = await _sut.ValidateBankAccount(_businessGroup, _referenceNumber, request);

        result.Left().Should().BeEquivalentTo(Error.New(MdpConstants.BankErrorCodes.FailedAccountNameValidationErrorCode));
        _loggerMock.VerifyLogging($"VerifyMemberSurnameMatch, BusinessGroup: {_businessGroup}, ReferenceNumber: {_referenceNumber} Error: {errorMessage}", LogLevel.Error, Times.Once());
    }

    public async Task ValidateBankAccountReturnsError_WhenValidateBankAccountFails()
    {
        var member = new MemberBuilder()
            .Name("John", "Doe")
            .Build();
        var request = ValidateBankAccountRequestFactory.Create();

        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(member);
        _bankServiceClientMock
            .Setup(x => x.ValidateBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ValidateBankAccountPayload>()))
            .ReturnsAsync((ValidateBankAccountResponse)null);

        var result = await _sut.ValidateBankAccount(_businessGroup, _referenceNumber, request);

        result.Left().Should().BeEquivalentTo(Error.New(MdpConstants.BankErrorCodes.FailedExternalValidationErrorCode));
        _loggerMock.VerifyLogging($"{nameof(_sut.ValidateBankAccount)}, ValidateBankAccount returned non success response for BusinessGroup: {_businessGroup}, ReferenceNumber: {_referenceNumber}", LogLevel.Error, Times.Once());
    }
    public async Task ValidateBankAccountReturnsError_WhenVerifySafePaymentReturnsNull()
    {
        var member = new MemberBuilder()
            .Name("John", "Doe")
            .Build();
        var request = ValidateBankAccountRequestFactory.Create();

        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(member);
        _bankServiceClientMock
            .Setup(x => x.ValidateBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ValidateBankAccountPayload>()))
            .ReturnsAsync(ValidateBankAccountResponseFactory.Create());
        _bankServiceClientMock
            .Setup(x => x.VerifySafePayment(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<VerifySafePaymentPayload>()))
            .ReturnsAsync((VerifySafePaymentResponse)null);

        var result = await _sut.ValidateBankAccount(_businessGroup, _referenceNumber, request);

        result.Left().Should().BeEquivalentTo(Error.New(MdpConstants.BankErrorCodes.FailedExternalValidationErrorCode));
        _loggerMock.VerifyLogging($"{nameof(_sut.ValidateBankAccount)}, VerifySafePayment returned non success response for BusinessGroup: {_businessGroup}, ReferenceNumber: {_referenceNumber}", LogLevel.Error, Times.Once());
    }

    [Input("FAIL")]
    [Input("CAUTION")]
    public async Task ValidateBankAccountReturnsError_WhenVerifySafePaymentFails(string status)
    {
        var member = new MemberBuilder()
            .Name("John", "Doe")
            .Build();
        var request = ValidateBankAccountRequestFactory.Create();

        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(member);
        _bankServiceClientMock
            .Setup(x => x.ValidateBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ValidateBankAccountPayload>()))
            .ReturnsAsync(ValidateBankAccountResponseFactory.Create());
        _bankServiceClientMock
            .Setup(x => x.VerifySafePayment(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<VerifySafePaymentPayload>()))
            .ReturnsAsync(VerifySafePaymentResponseFactory.Create(status));

        var result = await _sut.ValidateBankAccount(_businessGroup, _referenceNumber, request);

        result.Left().Should().BeEquivalentTo(Error.New(MdpConstants.BankErrorCodes.FailedExternalValidationErrorCode));
        _loggerMock.VerifyLogging($"{nameof(_sut.ValidateBankAccount)}, VerifySafePayment returned non success response for BusinessGroup: {_businessGroup}, ReferenceNumber: {_referenceNumber}", LogLevel.Error, Times.Once());
    }
}
public class ValidateAndSubmitBankAccountTest : BankServiceTest
{
    public async Task ValidateAndSubmitBankAccountReturnsValidationBankAccountResponse_WhenSuccessful()
    {
        var expectedResponse = ValidateBankAccountResponseFactory.Create();
        var member = new MemberBuilder()
            .Name("John", "Doe")
            .Build();
        var request = ValidateBankAccountRequestFactory.Create();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(member);
        _bankServiceClientMock
            .Setup(x => x.ValidateBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ValidateBankAccountPayload>()))
            .ReturnsAsync(ValidateBankAccountResponseFactory.Create());
        _bankServiceClientMock
            .Setup(x => x.VerifySafePayment(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<VerifySafePaymentPayload>()))
            .ReturnsAsync(VerifySafePaymentResponseFactory.Create());
        _bankServiceClientMock
            .Setup(x => x.AddBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AddBankAccountPayload>()))
            .ReturnsAsync(HttpStatusCode.NoContent);

        var result = await _sut.ValidateAndSubmitBankAccount(_businessGroup, _referenceNumber, request);

        result.IsLeft.Should().BeFalse();
        result.IsRight.Should().BeTrue();
        result.Right().Should().BeOfType<ValidateBankAccountResponse>();
        result.Right().Should().BeEquivalentTo(expectedResponse);

        _loggerMock.VerifyLogging($"ExternalBankAccountValidation, BusinessGroup: {_businessGroup}, ReferenceNumber: {_referenceNumber}", LogLevel.Information, Times.Once());
    }
    public async Task ValidateAndSubmitBankAccountReturnsValidationBankAccountResponseNonUk_WhenSuccessful()
    {
        var expectedResponse = ValidateBankAccountResponseFactory.Create();
        var member = new MemberBuilder()
            .Name("John", "Doe")
            .Build();
        var request = ValidateBankAccountRequestFactory.Create(isIban: true);
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(member);
        _bankServiceClientMock
            .Setup(x => x.ValidateBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ValidateBankAccountPayload>()))
            .ReturnsAsync(ValidateBankAccountResponseFactory.Create());
        _bankServiceClientMock
            .Setup(x => x.AddBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AddBankAccountPayload>()))
            .ReturnsAsync(HttpStatusCode.NoContent);

        var result = await _sut.ValidateAndSubmitBankAccount(_businessGroup, _referenceNumber, request);

        result.IsLeft.Should().BeFalse();
        result.IsRight.Should().BeTrue();
        result.Right().Should().BeOfType<ValidateBankAccountResponse>();
        result.Right().Should().BeEquivalentTo(expectedResponse);

        _bankServiceClientMock.Verify(x => x.VerifySafePayment(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<VerifySafePaymentPayload>()), Times.Never);
        _loggerMock.VerifyLogging($"ExternalBankAccountValidation, BusinessGroup: {_businessGroup}, ReferenceNumber: {_referenceNumber}", LogLevel.Information, Times.Once());
    }

    public async Task ValidateAndSubmitBankAccountReturnsError_WhenWhenMemberIsNotFound()
    {
        var expectedResponse = ValidateBankAccountResponseFactory.Create();
        var request = ValidateBankAccountRequestFactory.Create();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync((Option<Member>)null);
        _bankServiceClientMock
            .Setup(x => x.ValidateBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ValidateBankAccountPayload>()))
            .ReturnsAsync(ValidateBankAccountResponseFactory.Create());
        _bankServiceClientMock
            .Setup(x => x.VerifySafePayment(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<VerifySafePaymentPayload>()))
            .ReturnsAsync(VerifySafePaymentResponseFactory.Create());

        var result = await _sut.ValidateAndSubmitBankAccount(_businessGroup, _referenceNumber, request);

        result.IsLeft.Should().BeTrue();
        result.IsRight.Should().BeFalse();

        result.Left().Should().BeEquivalentTo(Error.New(MdpConstants.BankErrorCodes.FailedBankAccountNotFoundErrorCode));

        _loggerMock.VerifyLogging($"Member not found for reference number {_referenceNumber} and business group {_businessGroup}", LogLevel.Error, Times.Once());
    }

    [Input("")]
    [Input(null)]
    public async Task ValidateAndSubmitBankAccountReturnsError_WhenSurnameMismatch(string surname)
    {
        var member = new MemberBuilder()
            .Name("John", surname)
            .Build();
        var errorMessage = "The account name does not match the one held on our records. Please try again.";
        var request = ValidateBankAccountRequestFactory.Create();

        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(member);

        var result = await _sut.ValidateAndSubmitBankAccount(_businessGroup, _referenceNumber, request);

        result.Left().Should().BeEquivalentTo(Error.New(MdpConstants.BankErrorCodes.FailedAccountNameValidationErrorCode));
        _loggerMock.VerifyLogging($"VerifyMemberSurnameMatch, BusinessGroup: {_businessGroup}, ReferenceNumber: {_referenceNumber} Error: {errorMessage}", LogLevel.Error, Times.Once());
    }

    [Input("")]
    [Input(null)]
    public async Task ValidateAndSubmitBankAccountReturnsError_WhenAccountNameMismatch(string surname)
    {
        var member = new MemberBuilder()
            .Name("John", "Doe")
            .Build();
        var request = ValidateBankAccountRequestFactory.Create(accountName: $"John {surname}");
        var errorMessage = "The account name does not match the one held on our records. Please try again.";

        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(member);

        var result = await _sut.ValidateAndSubmitBankAccount(_businessGroup, _referenceNumber, request);

        result.Left().Should().BeEquivalentTo(Error.New(MdpConstants.BankErrorCodes.FailedAccountNameValidationErrorCode));
        _loggerMock.VerifyLogging($"VerifyMemberSurnameMatch, BusinessGroup: {_businessGroup}, ReferenceNumber: {_referenceNumber} Error: {errorMessage}", LogLevel.Error, Times.Once());
    }

    public async Task ValidateAndSubmitBankAccountReturnsError_WhenSurnameWithDiacriticsIsNotInAccountName()
    {
        var member = new MemberBuilder()
            .Name("John", "TestRene")
            .Build();
        var request = ValidateBankAccountRequestFactory.Create(accountName: $"John René");
        var errorMessage = "The account name does not match the one held on our records. Please try again.";

        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(member);

        var result = await _sut.ValidateAndSubmitBankAccount(_businessGroup, _referenceNumber, request);

        result.Left().Should().BeEquivalentTo(Error.New(MdpConstants.BankErrorCodes.FailedAccountNameValidationErrorCode));
        _loggerMock.VerifyLogging($"VerifyMemberSurnameMatch, BusinessGroup: {_businessGroup}, ReferenceNumber: {_referenceNumber} Error: {errorMessage}", LogLevel.Error, Times.Once());
    }

    public async Task ValidateAndSubmitBankAccountReturnsError_WhenValidateBankAccountFails()
    {
        var member = new MemberBuilder()
            .Name("John", "Doe")
            .Build();
        var request = ValidateBankAccountRequestFactory.Create();

        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(member);
        _bankServiceClientMock
            .Setup(x => x.ValidateBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ValidateBankAccountPayload>()))
            .ReturnsAsync((ValidateBankAccountResponse)null);

        var result = await _sut.ValidateAndSubmitBankAccount(_businessGroup, _referenceNumber, request);

        result.Left().Should().BeEquivalentTo(Error.New(MdpConstants.BankErrorCodes.FailedExternalValidationErrorCode));
        _loggerMock.VerifyLogging($"{nameof(_sut.ValidateBankAccount)}, ValidateBankAccount returned non success response for BusinessGroup: {_businessGroup}, ReferenceNumber: {_referenceNumber}", LogLevel.Error, Times.Once());
    }
    public async Task ValidateAndSubmitBankAccountReturnsError_WhenVerifySafePaymentReturnsNull()
    {
        var member = new MemberBuilder()
            .Name("John", "Doe")
            .Build();
        var request = ValidateBankAccountRequestFactory.Create();

        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(member);
        _bankServiceClientMock
            .Setup(x => x.ValidateBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ValidateBankAccountPayload>()))
            .ReturnsAsync(ValidateBankAccountResponseFactory.Create());
        _bankServiceClientMock
            .Setup(x => x.VerifySafePayment(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<VerifySafePaymentPayload>()))
            .ReturnsAsync((VerifySafePaymentResponse)null);

        var result = await _sut.ValidateAndSubmitBankAccount(_businessGroup, _referenceNumber, request);

        result.Left().Should().BeEquivalentTo(Error.New(MdpConstants.BankErrorCodes.FailedExternalValidationErrorCode));
        _loggerMock.VerifyLogging($"{nameof(_sut.ValidateBankAccount)}, VerifySafePayment returned non success response for BusinessGroup: {_businessGroup}, ReferenceNumber: {_referenceNumber}", LogLevel.Error, Times.Once());
    }

    [Input("FAIL")]
    [Input("CAUTION")]
    public async Task ValidateAndSubmitBankAccountReturnsError_WhenVerifySafePaymentFails(string status)
    {
        var member = new MemberBuilder()
            .Name("John", "Doe")
            .Build();
        var request = ValidateBankAccountRequestFactory.Create();

        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(member);
        _bankServiceClientMock
            .Setup(x => x.ValidateBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ValidateBankAccountPayload>()))
            .ReturnsAsync(ValidateBankAccountResponseFactory.Create());
        _bankServiceClientMock
            .Setup(x => x.VerifySafePayment(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<VerifySafePaymentPayload>()))
            .ReturnsAsync(VerifySafePaymentResponseFactory.Create(status));

        var result = await _sut.ValidateAndSubmitBankAccount(_businessGroup, _referenceNumber, request);

        result.Left().Should().BeEquivalentTo(Error.New(MdpConstants.BankErrorCodes.FailedExternalValidationErrorCode));
        _loggerMock.VerifyLogging($"{nameof(_sut.ValidateBankAccount)}, VerifySafePayment returned non success response for BusinessGroup: {_businessGroup}, ReferenceNumber: {_referenceNumber}", LogLevel.Error, Times.Once());
    }
    public async Task ValidateAndSubmitBankAccountReturnsError_WhenAddBankAccountFails()
    {
        var member = new MemberBuilder()
            .Name("John", "Doe")
            .Build();
        var request = ValidateBankAccountRequestFactory.Create();
        var errorMessage = $"AddBankAccount, Error: Failed to Add Bank Details: {HttpStatusCode.NotFound}";

        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(member);
        _bankServiceClientMock
            .Setup(x => x.ValidateBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ValidateBankAccountPayload>()))
            .ReturnsAsync(ValidateBankAccountResponseFactory.Create());
        _bankServiceClientMock
            .Setup(x => x.VerifySafePayment(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<VerifySafePaymentPayload>()))
            .ReturnsAsync(VerifySafePaymentResponseFactory.Create());
        _bankServiceClientMock
            .Setup(x => x.AddBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AddBankAccountPayload>()))
            .ReturnsAsync(HttpStatusCode.NotFound);

        var result = await _sut.ValidateAndSubmitBankAccount(_businessGroup, _referenceNumber, request);

        result.Left().Should().BeEquivalentTo(Error.New(MdpConstants.BankErrorCodes.FailedBankAccountSubmitErrorCode));
        _loggerMock.VerifyLogging(errorMessage, LogLevel.Error, Times.Once());
    }
}

public class GetCountriesAndCurrenciesTest : BankServiceTest
{
    public async Task GetCountriesAndCurrenciesReturnsCountryCurrencyResponse_WhenSuccessful()
    {
        var countriesResponse = new GetCountriesResponse
        {
            Countries = new List<CountryDetails>
            {
                new CountryDetails { CountryCode = "US", CountryName = "United States" },
                new CountryDetails { CountryCode = "GB", CountryName = "United Kingdom" }
            }
        };

        var currenciesResponse = new GetCurrenciesResponse
        {
            Currencies = new List<CurrencyDetails>
            {
                new CurrencyDetails { CountryCode = "US", CurrencyCode = "USD", CurrencyName = "US Dollar" },
                new CurrencyDetails { CountryCode = "GB", CurrencyCode = "GBP", CurrencyName = "British Pound" }
            }
        };

        _ipaServiceClientMock
            .Setup(x => x.GetCountries())
            .ReturnsAsync(Option<GetCountriesResponse>.Some(countriesResponse));

        _ipaServiceClientMock
            .Setup(x => x.GetCurrencies())
            .ReturnsAsync(Option<GetCurrenciesResponse>.Some(currenciesResponse));

        var result = await _sut.GetCountriesAndCurrencies();

        result.IsLeft.Should().BeFalse();
        result.IsRight.Should().BeTrue();

        var expectedResponse = new List<CountryCurrencyResponse>
        {
            new CountryCurrencyResponse
            {
                CountryCode = "GB",
                CountryName = "United Kingdom",
                CurrencyCode = "GBP",
                CurrencyName = "British Pound"
            },
            new CountryCurrencyResponse
            {
                CountryCode = "US",
                CountryName = "United States",
                CurrencyCode = "USD",
                CurrencyName = "US Dollar"
            }
        };

        result.Right().Should().BeEquivalentTo(expectedResponse.OrderBy(x => x.CountryName));
    }

    public async Task GetCountriesAndCurrenciesReturnsError_WhenCountriesIsNone()
    {
        _ipaServiceClientMock
            .Setup(x => x.GetCountries())
            .ReturnsAsync(Option<GetCountriesResponse>.None);

        _ipaServiceClientMock
            .Setup(x => x.GetCurrencies())
            .ReturnsAsync(Option<GetCurrenciesResponse>.Some(new GetCurrenciesResponse
            {
                Currencies = new List<CurrencyDetails>
                {
                    new CurrencyDetails { CountryCode = "US", CurrencyCode = "USD", CurrencyName = "US Dollar" }
                }
            }));

        var result = await _sut.GetCountriesAndCurrencies();

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be("Failed to fetch countries from IPA service.");
        _loggerMock.VerifyLogging("Failed to fetch countries from IPA service.", LogLevel.Error, Times.Once());
    }

    public async Task GetCountriesAndCurrenciesReturnsError_WhenCurrenciesIsNone()
    {
        _ipaServiceClientMock
            .Setup(x => x.GetCountries())
            .ReturnsAsync(Option<GetCountriesResponse>.Some(new GetCountriesResponse
            {
                Countries = new List<CountryDetails>
                {
                    new CountryDetails { CountryCode = "US", CountryName = "United States" }
                }
            }));

        _ipaServiceClientMock
            .Setup(x => x.GetCurrencies())
            .ReturnsAsync(Option<GetCurrenciesResponse>.None);

        var result = await _sut.GetCountriesAndCurrencies();

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be("Failed to fetch currencies from IPA service.");
        _loggerMock.VerifyLogging("Failed to fetch currencies from IPA service.", LogLevel.Error, Times.Once());
    }

    public async Task GetCountriesAndCurrenciesReturnsEmptyList_WhenNoMatchingCountryCodes()
    {
        var countriesResponse = new GetCountriesResponse
        {
            Countries = new List<CountryDetails>
            {
                new CountryDetails { CountryCode = "FR", CountryName = "France" }
            }
        };

        var currenciesResponse = new GetCurrenciesResponse
        {
            Currencies = new List<CurrencyDetails>
            {
                new CurrencyDetails { CountryCode = "US", CurrencyCode = "USD", CurrencyName = "US Dollar" }
            }
        };

        _ipaServiceClientMock
            .Setup(x => x.GetCountries())
            .ReturnsAsync(Option<GetCountriesResponse>.Some(countriesResponse));

        _ipaServiceClientMock
            .Setup(x => x.GetCurrencies())
            .ReturnsAsync(Option<GetCurrenciesResponse>.Some(currenciesResponse));

        var result = await _sut.GetCountriesAndCurrencies();

        result.IsLeft.Should().BeFalse();
        result.IsRight.Should().BeTrue();
        result.Right().Should().BeEmpty();
    }
}

