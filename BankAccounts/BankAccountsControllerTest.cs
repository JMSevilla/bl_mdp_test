using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.BankAccounts;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.Infrastructure.ApplyFinancials;
using WTW.MdpService.Infrastructure.MemberDb;
using WTW.MdpService.Test.Domain.Members;
using WTW.TestCommon.FixieConfig;
using WTW.Web.Errors;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.BankAccounts;

public class BankAccountsControllerTest
{
    private readonly Mock<IMemberRepository> _memberRepositoryMock;
    private readonly Mock<IMemberDbUnitOfWork> _memberDbUnitOfWorkMock;
    private readonly Mock<IApplyFinancialsClient> _applyFinancialsClientMock;
    private readonly BankAccountsController _sut;
    private readonly Mock<ILogger<BankAccountsController>> _loggerMock;

    public BankAccountsControllerTest()
    {
        _memberRepositoryMock = new Mock<IMemberRepository>();
        _memberDbUnitOfWorkMock = new Mock<IMemberDbUnitOfWork>();
        _applyFinancialsClientMock = new Mock<IApplyFinancialsClient>();
        _loggerMock = new Mock<ILogger<BankAccountsController>>();
        _sut = new BankAccountsController(_memberRepositoryMock.Object, _memberDbUnitOfWorkMock.Object, _applyFinancialsClientMock.Object, _loggerMock.Object);
        SetupControllerContext();
    }

    public async Task RetrieveBankAccountReturns200CodeAndCorrectData()
    {
        var member = new MemberBuilder()
            .AddBankAccount(BankAccount.CreateUkAccount(1, "John Doe", "12345678", DateTimeOffset.UtcNow, Bank.CreateUkBank("123456", "Natwest", "Manchester").Right()).Right())
            .Build();

        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(member);

        var result = await _sut.RetrieveBankAccount();
        var response = (result as OkObjectResult).Value as BankAccountResponse;

        result.Should().BeOfType<OkObjectResult>();
        response.AccountName.Should().Be("John Doe");
        response.AccountNumber.Should().Be("12345678");
        response.Iban.Should().BeNull();
        response.SortCode.Should().Be("123456");
        response.SortCodeFormatted.Should().Be("12-34-56");
        response.Bic.Should().BeNull();
        response.ClearingCode.Should().BeNull();
        response.BankName.Should().Be("Natwest");
        response.BankCity.Should().Be("Manchester");
        response.BankCountry.Should().Be("GB");
        response.BankCountryCode.Should().Be("GB");
    }

    [Input(false, "Not found", null)]
    [Input(true, "Member does not have any bank account.", "member_bank_account_not_found")]
    public async Task RetrieveBankAccountReturns404CodeAndCorrectMessage(bool memberExists, string expectedMessage, string expectedCode)
    {
        Option<Member> member = memberExists ? new MemberBuilder().Build() : null;
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(member);

        var result = await _sut.RetrieveBankAccount();
        var response = (result as NotFoundObjectResult).Value as ApiError;

        result.Should().BeOfType<NotFoundObjectResult>();
        response.Errors[0].Message.Should().Be(expectedMessage);
        response.Errors[0].Code.Should().Be(expectedCode);
    }

    [Input(false, "123456", "12345678", true, "Not found", typeof(NotFoundObjectResult))]
    [Input(true, "123456", "12345678", false, "External bank details validation failed.", typeof(BadRequestObjectResult))]
    [Input(true, "1234567", "12345678", true, "Invalid Sort code: Must be 6 digit length.", typeof(BadRequestObjectResult))]
    [Input(true, "123456", "123456789", true, "Invalid account number: Must be 8 digits length.", typeof(BadRequestObjectResult))]
    [Input(true, "123456", "12345678", true, null, typeof(NoContentResult))]
    public async Task SubmitUkBankAccountReturnsCorrectCodeAndCorrectErrorMessage(bool memberExists, string sortCode, string accountNumber, bool isValidExternalValidation, string expectedMessage, Type expectedType)
    {
        Option<Member> member = memberExists ? new MemberBuilder().Build() : null;
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(member);
        _applyFinancialsClientMock
            .Setup(x => x.ValidateUkBankAccount(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(isValidExternalValidation ? new AccountValidationResponse { BranchDetails = new List<BranchDetail> { new BranchDetail { BankName = "TestBank", City = "TestCity" } } } : Error.New(""));

        var result = await _sut.SubmitUkBankAccount(new SubmitUkBankAccountRequest
        {
            AccountNumber = accountNumber,
            AccountName = "John Doe",
            SortCode = sortCode
        });

        result.Should().BeOfType(expectedType);

        if (expectedType == typeof(NotFoundObjectResult) || expectedType == typeof(BadRequestObjectResult))
        {
            var response = (result as NotFoundObjectResult)?.Value as ApiError ?? (result as BadRequestObjectResult)?.Value as ApiError;
            response.Errors[0].Message.Should().Be(expectedMessage);
        }
    }

    [Input(true, "123456", "12345678", true, null, typeof(NoContentResult))]
    public async Task WhenSubmitUkBankAccountIsCalled_ThenSysAuditMethodsAreCalledSuccesfully(bool memberExists, string sortCode, string accountNumber, bool isValidExternalValidation, string expectedMessage, Type expectedType)
    {
        Option<Member> member = memberExists ? new MemberBuilder().Build() : null;
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(member);
        _applyFinancialsClientMock
            .Setup(x => x.ValidateUkBankAccount(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(isValidExternalValidation ? new AccountValidationResponse { BranchDetails = new List<BranchDetail> { new BranchDetail { BankName = "TestBank", City = "TestCity" } } } : Error.New(""));

        var result = await _sut.SubmitUkBankAccount(new SubmitUkBankAccountRequest
        {
            AccountNumber = accountNumber,
            AccountName = "John Doe",
            SortCode = sortCode
        });

        result.Should().BeOfType(expectedType);

        _memberRepositoryMock.Verify(x => x.PopulateSessionDetails(It.IsAny<string>()), Times.Once);
        _memberRepositoryMock.Verify(x => x.DisableSysAudit(It.IsAny<string>()), Times.Once);

    }

    [Input(false, "123456", "12345678", true, "Not found", typeof(NotFoundObjectResult))]
    [Input(true, "12345678", "LT1234567890123456", false, "External bank details validation failed.", typeof(BadRequestObjectResult))]
    [Input(true, "1234567891011", "LT1234567890123456", true, "Invalid BIC: Must be 8 or 11 digit length.", typeof(BadRequestObjectResult))]
    [Input(true, "12345678", "", true, "Invalid IBAN: Must be between 1 and 34 digits length.", typeof(BadRequestObjectResult))]
    [Input(true, "12345678", "LT1234567890123456", true, null, typeof(NoContentResult))]
    public async Task SaveIbanBankAccountReturnsCorrectCodeAndCorrectErrorMessage(bool memberExists, string bic, string iban, bool isValidExternalValidation, string expectedMessage, Type expectedType)
    {
        Option<Member> member = memberExists ? new MemberBuilder().Build() : null;
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(member);
        _applyFinancialsClientMock
            .Setup(x => x.ValidateIbanBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(isValidExternalValidation ? new AccountValidationResponse { BranchDetails = new List<BranchDetail> { new BranchDetail { BankName = "TestBank", City = "TestCity" } } } : Error.New(""));

        var result = await _sut.SaveIbanBankAccount(new SubmitIbanBankAccountRequest
        {
            BankCountryCode = "GB",
            Iban = iban,
            Bic = bic,
            AccountName = "John Doe",
            ClearingCode = "Test"
        });

        result.Should().BeOfType(expectedType);

        if (expectedType == typeof(NotFoundObjectResult) || expectedType == typeof(BadRequestObjectResult))
        {
            var response = (result as NotFoundObjectResult)?.Value as ApiError ?? (result as BadRequestObjectResult)?.Value as ApiError;
            response.Errors[0].Message.Should().Be(expectedMessage);
        }
    }

    [Input(true, "12345678", "LT1234567890123456", true, null, typeof(NoContentResult))]
    public async Task WhenSaveIbanBankAccountIsCalled_ThenSysAuditMethodsAreCalledSuccesfully(bool memberExists, string bic, string iban, bool isValidExternalValidation, string expectedMessage, Type expectedType)
    {
        Option<Member> member = memberExists ? new MemberBuilder().Build() : null;
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(member);
        _applyFinancialsClientMock
            .Setup(x => x.ValidateIbanBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(isValidExternalValidation ? new AccountValidationResponse { BranchDetails = new List<BranchDetail> { new BranchDetail { BankName = "TestBank", City = "TestCity" } } } : Error.New(""));

        var result = await _sut.SaveIbanBankAccount(new SubmitIbanBankAccountRequest
        {
            BankCountryCode = "GB",
            Iban = iban,
            Bic = bic,
            AccountName = "John Doe",
            ClearingCode = "Test"
        });

        result.Should().BeOfType(expectedType);

        _memberRepositoryMock.Verify(x => x.PopulateSessionDetails(It.IsAny<string>()), Times.Once);
        _memberRepositoryMock.Verify(x => x.DisableSysAudit(It.IsAny<string>()), Times.Once);
    }

    [Input(false, typeof(BadRequestObjectResult))]
    [Input(true, typeof(OkObjectResult))]
    public async Task ValidateIbanBankAccountReturnsCorrectCodeAndCorrectErrorMessage(bool isValidExternalValidation, Type expectedType)
    {
        _applyFinancialsClientMock
             .Setup(x => x.ValidateIbanBankAccount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
             .ReturnsAsync(isValidExternalValidation ? new AccountValidationResponse
             {
                 AccountNumber = "LT1234567890123456",
                 RecommendedBIC = "12345678",
                 CountryCode = "GB",
                 BranchDetails = new List<BranchDetail>
                 {
                     new BranchDetail
                     {
                         BankName = "TestBank",
                         Branch = "TestBranchName",
                         Street = "TestBranchStreet",
                         City = "TestCity",
                         Country = "TestCountry",
                         PostZip = "TestZip",
                     }
                 }
             } : Error.New(""));

        var result = await _sut.ValidateIbanBankAccount(new ValidateIbanBankAccountRequest
        {
            BankCountryCode = "GB",
            Iban = "LT1234567890123456",
            Bic = "12345678",
            AccountName = "John Doe",
            ClearingCode = "Test"
        });

        result.Should().BeOfType(expectedType);

        if (expectedType == typeof(BadRequestObjectResult))
        {
            var response = (result as BadRequestObjectResult).Value as ApiError;
            response.Errors[0].Message.Should().Be("External bank details validation failed.");
        }

        if (expectedType == typeof(OkObjectResult))
        {
            var response = (result as OkObjectResult).Value as IbanBankAccountValidationResponse;
            response.Iban.Should().Be("LT1234567890123456");
            response.Bic.Should().Be("12345678");
            response.Name.Should().Be("John Doe");
            response.BankName.Should().Be("TestBank");
            response.BranchName.Should().Be("TestBranchName");
            response.StreetAddress.Should().Be("TestBranchStreet");
            response.City.Should().Be("TestCity");
            response.Country.Should().Be("TestCountry");
            response.CountryCode.Should().Be("GB");
            response.PostCode.Should().Be("TestZip");
        }
    }

    [Input(false, typeof(BadRequestObjectResult))]
    [Input(true, typeof(OkObjectResult))]
    public async Task ValidateUkBankAccountReturnsCorrectCodeAndCorrectErrorMessage(bool isValidExternalValidation, Type expectedType)
    {
        _applyFinancialsClientMock
             .Setup(x => x.ValidateUkBankAccount(It.IsAny<string>(), It.IsAny<string>()))
             .ReturnsAsync(isValidExternalValidation ? new AccountValidationResponse
             {
                 AccountNumber = "123456789",
                 RecommendedBIC = "12345678",
                 CountryCode = "GB",
                 NationalId = "123456",
                 BranchDetails = new List<BranchDetail>
                 {
                     new BranchDetail
                     {
                         BankName = "TestBank",
                         Branch = "TestBranchName",
                         Street = "TestBranchStreet",
                         City = "TestCity",
                         Country = "TestCountry",
                         PostZip = "TestZip",
                     }
                 }
             } : Error.New(""));

        var result = await _sut.ValidateUkBankAccount(new ValidateUkBankAccountRequest
        {
            AccountNumber = "12345678",
            AccountName = "John Doe",
            SortCode = "123456"
        });

        result.Should().BeOfType(expectedType);

        if (expectedType == typeof(BadRequestObjectResult))
        {
            var response = (result as BadRequestObjectResult).Value as ApiError;
            response.Errors[0].Message.Should().Be("External bank details validation failed.");
        }

        if (expectedType == typeof(OkObjectResult))
        {
            var response = (result as OkObjectResult).Value as UkBankAccountValidationResponse;
            response.AccountNumber.Should().Be("123456789");
            response.SortCode.Should().Be("123456");
            response.Name.Should().Be("John Doe");
            response.BankName.Should().Be("TestBank");
            response.BranchName.Should().Be("TestBranchName");
            response.StreetAddress.Should().Be("TestBranchStreet");
            response.City.Should().Be("TestCity");
            response.Country.Should().Be("TestCountry");
            response.CountryCode.Should().Be("GB");
            response.PostCode.Should().Be("TestZip");
        }
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
}