using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.BankAccounts;
using WTW.MdpService.BankAccounts.Services;
using WTW.MdpService.Infrastructure.BankService;
using WTW.TestCommon.FixieConfig;
using WTW.Web;
using WTW.Web.Errors;

namespace WTW.MdpService.Test.BankAccounts;

public class BankAccountsControllerV2Test
{
    protected readonly BankAccountsV2Controller _sut;
    protected readonly Mock<ILogger<BankAccountsV2Controller>> _loggerMock;
    protected readonly Mock<IBankService> _bankServiceMock;

    public BankAccountsControllerV2Test()
    {
        _loggerMock = new Mock<ILogger<BankAccountsV2Controller>>();
        _bankServiceMock = new Mock<IBankService>();
        _sut = new BankAccountsV2Controller(_loggerMock.Object, _bankServiceMock.Object);
        SetupControllerContext();
    }

    private void SetupControllerContext(string referenceNumber = "reference_number", string value = "TestReferenceNumber")
    {
        var id = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim("business_group", "TestBusinessGroup"),
            new Claim(referenceNumber, value),
        };

        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
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
}
public class RetrieveBankAccountV2Test : BankAccountsControllerV2Test
{
    private BankAccountResponseV2 _stubBankAccountResponseV2;

    public RetrieveBankAccountV2Test()
    {
        _stubBankAccountResponseV2 = BankAccountResponseV2Factory.Create();
    }

    [Input(false, false)]
    [Input(false, true)]
    [Input(true, false)]
    [Input(true, true)]
    public async Task RetrieveBankAccountV2Returns200CodeAndCorrectData(bool isIban, bool isNonUk)
    {
        _stubBankAccountResponseV2 = BankAccountResponseV2Factory.Create(isIban, isNonUk);

        _bankServiceMock
            .Setup(x => x.FetchBankAccount(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(_stubBankAccountResponseV2);

        var result = await _sut.RetrieveBankAccountV2();
        var response = (result as OkObjectResult).Value as BankAccountResponseV2;

        result.Should().BeOfType<OkObjectResult>();
        response.Should().BeEquivalentTo(_stubBankAccountResponseV2);
    }

    [Input(true, "Member does not have any bank account.", "member_bank_account_not_found")]
    public async Task RetrieveBankAccountV2Returns404CodeAndCorrectMessage(bool memberExists, string expectedMessage, string expectedCode)
    {
        _bankServiceMock
            .Setup(x => x.FetchBankAccount(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Error.New(MdpConstants.BankErrorCodes.FailedBankAccountNotFoundErrorCode));

        var result = await _sut.RetrieveBankAccountV2();
        var response = (result as NotFoundObjectResult).Value as ApiError;

        result.Should().BeOfType<NotFoundObjectResult>();
        response.Errors[0].Message.Should().Be(expectedMessage);
        response.Errors[0].Code.Should().Be(expectedCode);
    }
}
public class SubmitUkBankAccountV2Test : BankAccountsControllerV2Test
{
    private readonly ValidateBankAccountResponse _stubValidateBankAccountResponse;
    private readonly ValidateBankAccountRequest _stubValidateBankAccountRequest;
    public SubmitUkBankAccountV2Test()
    {
        _stubValidateBankAccountResponse = ValidateBankAccountResponseFactory.Create();
        _stubValidateBankAccountRequest = ValidateBankAccountRequestFactory.Create();
    }
    public async Task SubmitUkBankAccountV2Returns204CodeAndCorrectData()
    {
        _bankServiceMock
            .Setup(x => x.ValidateAndSubmitBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ValidateBankAccountRequest>()))
            .ReturnsAsync(_stubValidateBankAccountResponse);

        var result = await _sut.SubmitUkBankAccountV2(_stubValidateBankAccountRequest);

        result.Should().BeOfType<NoContentResult>();
    }

    public async Task SubmitUkBankAccountV2Returns404NotFound_WhenServiceReturnsNotFound()
    {
        _bankServiceMock
            .Setup(x => x.ValidateAndSubmitBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ValidateBankAccountRequest>()))
            .ReturnsAsync(Error.New(MdpConstants.BankErrorCodes.FailedBankAccountNotFoundErrorCode));

        var result = await _sut.SubmitUkBankAccountV2(_stubValidateBankAccountRequest);
        var response = (result as NotFoundObjectResult).Value as ApiError;

        result.Should().BeOfType<NotFoundObjectResult>();
        response.Errors[0].Message.Should().Be("Member does not have any bank account.");
        response.Errors[0].Code.Should().Be(MdpConstants.BankErrorCodes.FailedBankAccountNotFoundErrorCode);
    }
    public async Task SubmitUkBankAccountV2Returns400BadRequest_WhenServiceReturnsAccountNameValidationError()
    {
        _bankServiceMock
            .Setup(x => x.ValidateAndSubmitBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ValidateBankAccountRequest>()))
            .ReturnsAsync(Error.New(MdpConstants.BankErrorCodes.FailedAccountNameValidationErrorCode));

        var result = await _sut.SubmitUkBankAccountV2(_stubValidateBankAccountRequest);
        var response = (result as BadRequestObjectResult).Value as ApiError;

        result.Should().BeOfType<BadRequestObjectResult>();
        response.Errors[0].Message.Should().Be("The account name does not match the one held on our records. Please try again.");
        response.Errors[0].Code.Should().Be(MdpConstants.BankErrorCodes.FailedAccountNameValidationErrorCode);
    }
    public async Task SubmitUkBankAccountV2Returns400BadRequest_WhenServiceReturnsExternalValidationError()
    {
        _bankServiceMock
            .Setup(x => x.ValidateAndSubmitBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ValidateBankAccountRequest>()))
            .ReturnsAsync(Error.New(MdpConstants.BankErrorCodes.FailedExternalValidationErrorCode));

        var result = await _sut.SubmitUkBankAccountV2(_stubValidateBankAccountRequest);
        var response = (result as BadRequestObjectResult).Value as ApiError;

        result.Should().BeOfType<BadRequestObjectResult>();
        response.Errors[0].Message.Should().Be("The bank details provided are incorrect. Please try again.");
        response.Errors[0].Code.Should().Be(MdpConstants.BankErrorCodes.FailedExternalValidationErrorCode);
    }
    public async Task SubmitUkBankAccountV2Returns400BadRequest_WhenServiceReturnsFailedBankAccountSubmitErrorCode()
    {
        _bankServiceMock
            .Setup(x => x.ValidateAndSubmitBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ValidateBankAccountRequest>()))
            .ReturnsAsync(Error.New(MdpConstants.BankErrorCodes.FailedBankAccountSubmitErrorCode));

        var result = await _sut.SubmitUkBankAccountV2(_stubValidateBankAccountRequest);
        var response = (result as BadRequestObjectResult).Value as ApiError;

        result.Should().BeOfType<BadRequestObjectResult>();
        response.Errors[0].Message.Should().Be("Error adding bank details.");
        response.Errors[0].Code.Should().Be(MdpConstants.BankErrorCodes.FailedBankAccountSubmitErrorCode);
    }
    public async Task SubmitUkBankAccountV2Returns400BadRequest_WhenServiceReturnsAnUnknownError()
    {
        _bankServiceMock
            .Setup(x => x.ValidateAndSubmitBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ValidateBankAccountRequest>()))
            .ReturnsAsync(Error.New("Unknown Error."));

        var result = await _sut.SubmitUkBankAccountV2(_stubValidateBankAccountRequest);
        var response = (result as BadRequestObjectResult).Value as ApiError;

        result.Should().BeOfType<BadRequestObjectResult>();
        response.Errors[0].Message.Should().Be("Unknown Error.");
        response.Errors[0].Code.Should().Be(MdpConstants.BankErrorCodes.FailedExternalValidationErrorCode);
    }
}
public class SaveIbanBankAccountV2Test : BankAccountsControllerV2Test
{
    private readonly ValidateBankAccountResponse _stubValidateBankAccountResponse;
    private readonly ValidateBankAccountRequest _stubValidateBankAccountRequest;
    public SaveIbanBankAccountV2Test()
    {
        _stubValidateBankAccountResponse = ValidateBankAccountResponseFactory.Create();
        _stubValidateBankAccountRequest = ValidateBankAccountRequestFactory.Create(isIban: true);
    }
    public async Task SaveIbanBankAccountV2Returns200CodeAndCorrectData()
    {
        _bankServiceMock
            .Setup(x => x.ValidateAndSubmitBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ValidateBankAccountRequest>()))
            .ReturnsAsync(_stubValidateBankAccountResponse);

        var result = await _sut.SaveIbanBankAccountV2(_stubValidateBankAccountRequest);

        result.Should().BeOfType<NoContentResult>();
    }

    public async Task SaveIbanBankAccountV2Returns404NotFound_WhenServiceReturnsNotFound()
    {
        _bankServiceMock
            .Setup(x => x.ValidateAndSubmitBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ValidateBankAccountRequest>()))
            .ReturnsAsync(Error.New(MdpConstants.BankErrorCodes.FailedBankAccountNotFoundErrorCode));

        var result = await _sut.SaveIbanBankAccountV2(_stubValidateBankAccountRequest);
        var response = (result as NotFoundObjectResult).Value as ApiError;

        result.Should().BeOfType<NotFoundObjectResult>();
        response.Errors[0].Message.Should().Be("Member does not have any bank account.");
        response.Errors[0].Code.Should().Be(MdpConstants.BankErrorCodes.FailedBankAccountNotFoundErrorCode);
    }

    public async Task SaveIbanBankAccountV2Returns400CodeAndCorrectData_WhenServiceReturnsAccountNameValidationError()
    {
        _bankServiceMock
            .Setup(x => x.ValidateAndSubmitBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ValidateBankAccountRequest>()))
            .ReturnsAsync(Error.New(MdpConstants.BankErrorCodes.FailedAccountNameValidationErrorCode));

        var result = await _sut.SaveIbanBankAccountV2(_stubValidateBankAccountRequest);

        result.Should().BeOfType<BadRequestObjectResult>();
        var response = (result as BadRequestObjectResult).Value as ApiError;

        result.Should().BeOfType<BadRequestObjectResult>();
        response.Errors[0].Message.Should().Be("The account name does not match the one held on our records. Please try again.");
        response.Errors[0].Code.Should().Be(MdpConstants.BankErrorCodes.FailedAccountNameValidationErrorCode);
    }

    public async Task SaveIbanBankAccountV2Returns400BadRequest_WhenServiceReturnsExternalValidationError()
    {
        _bankServiceMock
            .Setup(x => x.ValidateAndSubmitBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ValidateBankAccountRequest>()))
            .ReturnsAsync(Error.New(MdpConstants.BankErrorCodes.FailedExternalValidationErrorCode));

        var result = await _sut.SaveIbanBankAccountV2(_stubValidateBankAccountRequest);
        var response = (result as BadRequestObjectResult).Value as ApiError;

        result.Should().BeOfType<BadRequestObjectResult>();
        response.Errors[0].Message.Should().Be("The bank details provided are incorrect. Please try again.");
        response.Errors[0].Code.Should().Be(MdpConstants.BankErrorCodes.FailedExternalValidationErrorCode);
    }
    public async Task SaveIbanBankAccountV2Returns400BadRequest_WhenServiceReturnsFailedBankAccountSubmitErrorCode()
    {
        _bankServiceMock
            .Setup(x => x.ValidateAndSubmitBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ValidateBankAccountRequest>()))
            .ReturnsAsync(Error.New(MdpConstants.BankErrorCodes.FailedBankAccountSubmitErrorCode));

        var result = await _sut.SaveIbanBankAccountV2(_stubValidateBankAccountRequest);
        var response = (result as BadRequestObjectResult).Value as ApiError;

        result.Should().BeOfType<BadRequestObjectResult>();
        response.Errors[0].Message.Should().Be("Error adding bank details.");
        response.Errors[0].Code.Should().Be(MdpConstants.BankErrorCodes.FailedBankAccountSubmitErrorCode);
    }
    public async Task SaveIbanBankAccountV2Returns400BadRequest_WhenServiceReturnsAnUnknownError()
    {
        _bankServiceMock
            .Setup(x => x.ValidateAndSubmitBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ValidateBankAccountRequest>()))
            .ReturnsAsync(Error.New("Unknown Error."));

        var result = await _sut.SaveIbanBankAccountV2(_stubValidateBankAccountRequest);
        var response = (result as BadRequestObjectResult).Value as ApiError;

        result.Should().BeOfType<BadRequestObjectResult>();
        response.Errors[0].Message.Should().Be("Unknown Error.");
        response.Errors[0].Code.Should().Be(MdpConstants.BankErrorCodes.FailedExternalValidationErrorCode);
    }
}
public class ValidateIbanBankAccountV2Test : BankAccountsControllerV2Test
{
    private readonly ValidateBankAccountResponse _stubValidateBankAccountResponse;
    private readonly ValidateBankAccountRequest _stubValidateBankAccountRequest;
    public ValidateIbanBankAccountV2Test()
    {
        _stubValidateBankAccountResponse = ValidateBankAccountResponseFactory.Create(true);
        _stubValidateBankAccountRequest = ValidateBankAccountRequestFactory.Create(true);
    }
    public async Task ValidateIbanBankAccountV2Returns200CodeAndCorrectData()
    {
        _bankServiceMock
            .Setup(x => x.ValidateBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ValidateBankAccountRequest>()))
            .ReturnsAsync(_stubValidateBankAccountResponse);

        var result = await _sut.ValidateIbanBankAccountV2(_stubValidateBankAccountRequest);
        var response = (result as OkObjectResult).Value as ValidateBankAccountResponse;

        result.Should().BeOfType<OkObjectResult>();
        response.Should().BeEquivalentTo(_stubValidateBankAccountResponse);
    }

    public async Task ValidateIbanBankAccountV2Returns404NotFound_WhenServiceReturnsNotFound()
    {
        _bankServiceMock
            .Setup(x => x.ValidateBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ValidateBankAccountRequest>()))
            .ReturnsAsync(Error.New(MdpConstants.BankErrorCodes.FailedBankAccountNotFoundErrorCode));

        var result = await _sut.ValidateIbanBankAccountV2(_stubValidateBankAccountRequest);
        var response = (result as NotFoundObjectResult).Value as ApiError;

        result.Should().BeOfType<NotFoundObjectResult>();
        response.Errors[0].Message.Should().Be("Member does not have any bank account.");
        response.Errors[0].Code.Should().Be(MdpConstants.BankErrorCodes.FailedBankAccountNotFoundErrorCode);
    }
    public async Task ValidateIbanBankAccountV2Returns400CodeAndCorrectData_WhenServiceReturnsAccountNameValidationError()
    {
        _bankServiceMock
            .Setup(x => x.ValidateBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ValidateBankAccountRequest>()))
            .ReturnsAsync(Error.New(MdpConstants.BankErrorCodes.FailedAccountNameValidationErrorCode));

        var result = await _sut.ValidateIbanBankAccountV2(_stubValidateBankAccountRequest);

        result.Should().BeOfType<BadRequestObjectResult>();
        var response = (result as BadRequestObjectResult).Value as ApiError;

        result.Should().BeOfType<BadRequestObjectResult>();
        response.Errors[0].Message.Should().Be("The account name does not match the one held on our records. Please try again.");
        response.Errors[0].Code.Should().Be(MdpConstants.BankErrorCodes.FailedAccountNameValidationErrorCode);
    }
    public async Task ValidateIbanBankAccountV2Returns400BadRequest_WhenServiceReturnsExternalValidationError()
    {
        _bankServiceMock
            .Setup(x => x.ValidateBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ValidateBankAccountRequest>()))
            .ReturnsAsync(Error.New(MdpConstants.BankErrorCodes.FailedExternalValidationErrorCode));

        var result = await _sut.ValidateIbanBankAccountV2(_stubValidateBankAccountRequest);
        var response = (result as BadRequestObjectResult).Value as ApiError;

        result.Should().BeOfType<BadRequestObjectResult>();
        response.Errors[0].Message.Should().Be("The bank details provided are incorrect. Please try again.");
        response.Errors[0].Code.Should().Be(MdpConstants.BankErrorCodes.FailedExternalValidationErrorCode);
    }
    public async Task ValidateIbanBankAccountV2Returns400BadRequest_WhenServiceReturnsAnUnknownError()
    {
        _bankServiceMock
            .Setup(x => x.ValidateBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ValidateBankAccountRequest>()))
            .ReturnsAsync(Error.New("Unknown Error."));

        var result = await _sut.ValidateIbanBankAccountV2(_stubValidateBankAccountRequest);
        var response = (result as BadRequestObjectResult).Value as ApiError;

        result.Should().BeOfType<BadRequestObjectResult>();
        response.Errors[0].Message.Should().Be("Unknown Error.");
        response.Errors[0].Code.Should().Be(MdpConstants.BankErrorCodes.FailedExternalValidationErrorCode);
    }
}
public class ValidateUkBankAccountV2Test : BankAccountsControllerV2Test
{
    private readonly ValidateBankAccountResponse _stubValidateBankAccountResponse;
    private readonly ValidateBankAccountRequest _stubValidateBankAccountRequest;
    public ValidateUkBankAccountV2Test()
    {
        _stubValidateBankAccountResponse = ValidateBankAccountResponseFactory.Create();
        _stubValidateBankAccountRequest = ValidateBankAccountRequestFactory.Create();
    }
    public async Task ValidateUkBankAccountV2Returns200CodeAndCorrectData()
    {
        _bankServiceMock
            .Setup(x => x.ValidateBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ValidateBankAccountRequest>()))
            .ReturnsAsync(_stubValidateBankAccountResponse);

        var result = await _sut.ValidateUkBankAccountV2(_stubValidateBankAccountRequest);
        var response = (result as OkObjectResult).Value as ValidateBankAccountResponse;

        result.Should().BeOfType<OkObjectResult>();
        response.Should().BeEquivalentTo(_stubValidateBankAccountResponse);
    }

    public async Task ValidateUkBankAccountV2Returns400CodeAndCorrectData_WhenServiceReturnsNotFound()
    {
        _bankServiceMock
            .Setup(x => x.ValidateBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ValidateBankAccountRequest>()))
            .ReturnsAsync(Error.New(MdpConstants.BankErrorCodes.FailedBankAccountNotFoundErrorCode));

        var result = await _sut.ValidateUkBankAccountV2(_stubValidateBankAccountRequest);
        var response = (result as NotFoundObjectResult).Value as ApiError;

        response.Errors[0].Message.Should().Be("Member does not have any bank account.");
        response.Errors[0].Code.Should().Be(MdpConstants.BankErrorCodes.FailedBankAccountNotFoundErrorCode);
    }

    public async Task ValidateUkBankAccountV2Returns400CodeAndCorrectData_WhenServiceReturnsAccountNameValidationError()
    {
        _bankServiceMock
            .Setup(x => x.ValidateBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ValidateBankAccountRequest>()))
            .ReturnsAsync(Error.New(MdpConstants.BankErrorCodes.FailedAccountNameValidationErrorCode));

        var result = await _sut.ValidateUkBankAccountV2(_stubValidateBankAccountRequest);

        result.Should().BeOfType<BadRequestObjectResult>();
        var response = (result as BadRequestObjectResult).Value as ApiError;

        result.Should().BeOfType<BadRequestObjectResult>();
        response.Errors[0].Message.Should().Be("The account name does not match the one held on our records. Please try again.");
        response.Errors[0].Code.Should().Be(MdpConstants.BankErrorCodes.FailedAccountNameValidationErrorCode);
    }


    public async Task ValidateUkBankAccountV2Returns400CodeAndCorrectData_WhenServiceReturnsExternalValidationError()
    {
        _bankServiceMock
            .Setup(x => x.ValidateBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ValidateBankAccountRequest>()))
            .ReturnsAsync(Error.New(MdpConstants.BankErrorCodes.FailedExternalValidationErrorCode));

        var result = await _sut.ValidateUkBankAccountV2(_stubValidateBankAccountRequest);
        var response = (result as BadRequestObjectResult).Value as ApiError;

        response.Errors[0].Message.Should().Be("The bank details provided are incorrect. Please try again.");
        response.Errors[0].Code.Should().Be(MdpConstants.BankErrorCodes.FailedExternalValidationErrorCode);
    }

    public async Task ValidateUkBankAccountV2Returns400CodeAndCorrectData_WhenServiceReturnsAnUnknownError()
    {
        _bankServiceMock
            .Setup(x => x.ValidateBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ValidateBankAccountRequest>()))
            .ReturnsAsync(Error.New("Unknown Error."));

        var result = await _sut.ValidateUkBankAccountV2(_stubValidateBankAccountRequest);
        var response = (result as BadRequestObjectResult).Value as ApiError;

        response.Errors[0].Message.Should().Be("Unknown Error.");
        response.Errors[0].Code.Should().Be(MdpConstants.BankErrorCodes.FailedExternalValidationErrorCode);
    }
}