using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WTW.MdpService.Infrastructure.BankService;
using WTW.MdpService.Infrastructure.TokenService;
using WTW.TestCommon.Helpers;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Infrastructure.BankService;

public class BankServiceClientTest
{
    protected BankServiceClient _sut;
    protected Mock<ILogger<BankServiceClient>> _loggerMock;
    protected Mock<IHttpClientFactory> _httpClientFactoryMock;
    protected Mock<ICachedTokenServiceClient> _tokenServiceClientMock;
    protected Mock<HttpMessageHandler> _httpMessageHandlerMock;
    protected Mock<IOptionsSnapshot<BankServiceOptions>> _optionsMock;

    public BankServiceClientTest()
    {
        _loggerMock = new Mock<ILogger<BankServiceClient>>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _tokenServiceClientMock = new Mock<ICachedTokenServiceClient>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        _httpClientFactoryMock
            .Setup(x => x.CreateClient("BankService"))
            .Returns(new HttpClient(_httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri("https://bankservice.dev.awstas.net")
            });
        _tokenServiceClientMock.Setup(x => x.GetAccessToken()).ReturnsAsync(new TokenServiceResponse { AccessToken = "access token", SecondsExpiresIn = 300 });
        _optionsMock = new Mock<IOptionsSnapshot<BankServiceOptions>>();
        _optionsMock.Setup(m => m.Value)
            .Returns(new BankServiceOptions
            {
                BaseUrl = "https://bankservice.dev.awstas.net",
                GetBankAccountPath = "internal/v1/bgroups/{0}/members/{1}/bankaccounts",
                PostBankAccountPath = "internal/v1/bgroups/{0}/members/{1}/bankaccounts",
                ValidateBankAccountPath = "internal/v1/bgroups/{0}/members/{1}/bankaccounts/validate",
                VerifySafePaymentPath = "internal/v1/bgroups/{0}/members/{1}/bankaccounts/validate/safe-payment"
            });

        _sut = new BankServiceClient(
            _httpClientFactoryMock.Object.CreateClient("BankService"),
            _tokenServiceClientMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);
    }
}

public class AddBankAccountTest : BankServiceClientTest
{
    public async Task WhenAddBankAccountIsSuccessful_ThenReturnNoContent()
    {
        var _stubAddBankAccountPayload = AddBankAccountPayloadResponseFactory.Create();
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.NoContent
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.AddBankAccount("ABC", "1234567", _stubAddBankAccountPayload);

        result.Should().Be(HttpStatusCode.NoContent);
    }
    public static class AddBankAccountPayloadResponseFactory
    {
        public static AddBankAccountPayload Create()
        {
            return new AddBankAccountPayload()
            {
                ApplicationName = "APPNAME",
                AccountName = "John Doe",
                AccountNumber = "12345678",
                SortCode = "123456",
                BankCountryCode = "GB",
                BankAccountCurrency = "GBP",
                BankName = "BankName",
                BankCity = "BankCity",
                Country = "United Kingdom"
            };
        }
    }
}
public class GetBankAccountTest : BankServiceClientTest
{
    public async Task WhenGetBankAccountWithValidData_ThenReturnGetBankAccountClientResponse()
    {
        var _stubGetBankAccountClientResponse = GetBankAccountClientResponseFactory.Create();
        var response = new HttpResponseMessage
        {
            Content = JsonContent.Create(_stubGetBankAccountClientResponse),
            StatusCode = System.Net.HttpStatusCode.OK
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.GetBankAccount("ABC", "1234567");

        result.IsSome.Should().BeTrue();
        result.Value<GetBankAccountClientResponse>().Should().BeEquivalentTo(_stubGetBankAccountClientResponse);
    }
    public async Task WhenGetBankAccountReturn404NotFound_ThenReturnNull()
    {
        var response = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.NotFound
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.GetBankAccount("ABC", "1234567");

        result.IsNone.Should().BeTrue();
        _loggerMock.VerifyLogging($"No bank account record found for ABC 1234567.", LogLevel.Warning, Times.Once());
    }
    public async Task WhenGetBankAccountReturn500InternalServerError_ThenReturnNull()
    {
        var response = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.InternalServerError
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.GetBankAccount("ABC", "1234567");

        result.IsNone.Should().BeTrue();
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
                LocalSortCode = "LocalSortCode",
                BankAccountNumber = "12345678",
                AccountName = "John Doe",
                Country = "GB",
                BankAccountCurrency = "GBP",
                BankName = "BankName",
                BankCity = "BankCity"
            };
        }
    }
}
public class ValidateBankAccountTest : BankServiceClientTest
{
    public async Task WhenValidateBankAccountIsSuccessful_ThenReturnValidateBankAccountResponse()
    {
        var _stubValidateBankAccountPayload = ValidateBankAccountPayloadFactory.Create();
        var _stubValidateBankAccountResponse = ValidateBankAccountResponseResponseFactory.Create();
        var response = new HttpResponseMessage
        {
            Content = JsonContent.Create(_stubValidateBankAccountResponse),
            StatusCode = System.Net.HttpStatusCode.OK
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.ValidateBankAccount("ABC", "1234567", _stubValidateBankAccountPayload);

        result.Should().BeEquivalentTo(_stubValidateBankAccountResponse);
    }
    public async Task WhenValidateBankAccountIsNotSuccessful_ThenReturnNull()
    {
        var _stubValidateBankAccountPayload = ValidateBankAccountPayloadFactory.Create();
        var _stubValidateBankAccountResponse = ValidateBankAccountResponseResponseFactory.Create();
        var response = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.NotFound
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.ValidateBankAccount("ABC", "1234567", _stubValidateBankAccountPayload);

        result.Should().BeNull();
    }
    public static class ValidateBankAccountPayloadFactory
    {
        public static ValidateBankAccountPayload Create()
        {
            return new ValidateBankAccountPayload()
            {
                AccountName = "John Doe",
                BankCountryCode = "GB",
                AccountNumber = "12345678",
                AccountCurrency = "GBP",
                SortCode = "123456",
            };
        }
    }
    public static class ValidateBankAccountResponseResponseFactory
    {
        public static ValidateBankAccountResponse Create()
        {
            return new ValidateBankAccountResponse()
            {
                Name = "John Doe",
                BankName = "BankName",
                City = "BankCity",
                AccountNumber = "12345678",
                SortCode = "123456",
                Country = "United Kingdom"
            };
        }
    }
}
public class VerifySafePaymentTest : BankServiceClientTest
{
    public async Task WhenVerifySafePaymentIsSuccessful_ThenReturnVerifySafePaymentResponse()
    {
        var _stubVerifySafePaymentPayload = VerifySafePaymentPayloadFactory.Create();
        var _stubVerifySafePaymentResponse = VerifySafePaymentResponseFactory.Create();
        var response = new HttpResponseMessage
        {
            Content = JsonContent.Create(_stubVerifySafePaymentResponse),
            StatusCode = System.Net.HttpStatusCode.OK
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.VerifySafePayment("ABC", "1234567", _stubVerifySafePaymentPayload);

        result.Should().BeEquivalentTo(_stubVerifySafePaymentResponse);
    }
    public async Task WhenVerifySafePaymentIsNotSuccessful_ThenReturnNull()
    {
        var _stubVerifySafePaymentPayload = VerifySafePaymentPayloadFactory.Create();
        var _stubVerifySafePaymentResponse = VerifySafePaymentResponseFactory.Create();
        var response = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.NotFound
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.VerifySafePayment("ABC", "1234567", _stubVerifySafePaymentPayload);

        result.Should().BeNull();
        ;
    }
    public static class VerifySafePaymentPayloadFactory
    {
        public static VerifySafePaymentPayload Create()
        {
            return new VerifySafePaymentPayload()
            {
                BankCountryCode = "GB",
                FirstName = "John Doe",
                LastName = "12345678",
                AccountType = "Personal",
                NationalId = "123456",
                AccountNumber = "12345678",
            };
        }
    }
    public static class VerifySafePaymentResponseFactory
    {
        public static VerifySafePaymentResponse Create()
        {
            return new VerifySafePaymentResponse()
            {
                Status = "PASS",
                Comment = "Comment"
            };
        }
    }
}
