using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using WTW.MdpService.DcRetirement;
using WTW.MdpService.Infrastructure.Investment;
using WTW.MdpService.Infrastructure.Investment.AnnuityBroker;
using WTW.MdpService.Infrastructure.TokenService;
using WTW.TestCommon.FixieConfig;
using WTW.TestCommon.Helpers;
using WTW.Web.LanguageExt;
using WTW.Web.Serialization;

namespace WTW.MdpService.Test.Infrastructure.Investment;

public class InvestmentServiceClientTest
{
    private readonly InvestmentServiceClient _sut;
    private readonly Mock<ILogger<InvestmentServiceClient>> _loggerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ICachedTokenServiceClient> _tokenServiceClientMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;

    public InvestmentServiceClientTest()
    {
        _loggerMock = new Mock<ILogger<InvestmentServiceClient>>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _tokenServiceClientMock = new Mock<ICachedTokenServiceClient>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        _httpClientFactoryMock
        .Setup(x => x.CreateClient("Investment"))
        .Returns(new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://investmentservice.dev.awstas.net")
        });
        _tokenServiceClientMock.Setup(x => x.GetAccessToken()).ReturnsAsync(new TokenServiceResponse { AccessToken = "access token", SecondsExpiresIn = 300 });
        _sut = new InvestmentServiceClient(
            _httpClientFactoryMock.Object.CreateClient("Investment"),
            _tokenServiceClientMock.Object,
            _loggerMock.Object);
    }

    public async Task GetInternalBalance_WhenSchemeTypeIsNotDC_ReturnsNone()
    {
        var referenceNumber = "1234567";
        var businessGroup = "test";
        var schemeType = "DB";

        var result = await _sut.GetInternalBalance(referenceNumber, businessGroup, schemeType);

        result.IsNone.Should().BeTrue();
    }

    public async Task GetInternalBalance_WhenSchemeTypeIsDC_ReturnsInternalBalance()
    {
        var referenceNumber = "0240707";
        var businessGroup = "LIF";
        var schemeType = "DC";

        var response = new HttpResponseMessage
        {
            Content = new StringContent(InvestmentTestData.BalanceResponseJson),
            StatusCode = System.Net.HttpStatusCode.OK
        };

        var responseModel = JsonSerializer.Deserialize<InvestmentInternalBalanceResponse>(InvestmentTestData.BalanceResponseJson, SerialiationBuilder.Options());

        _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
            "SendAsync",
             ItExpr.IsAny<HttpRequestMessage>(),
             ItExpr.IsAny<CancellationToken>())
             .ReturnsAsync(response);

        var result = await _sut.GetInternalBalance(referenceNumber, businessGroup, schemeType);

        result.IsSome.Should().BeTrue();
        result.Value().Should().BeEquivalentTo(responseModel);
    }

    public async Task GetInternalBalance_ReturnsNone_WhenApiReturnsError()
    {
        var referenceNumber = "0240707";
        var businessGroup = "LIF";
        var schemeType = "DC";

        var response = new HttpResponseMessage
        {
            Content = new StringContent(JsonSerializer.Serialize(new InvestmentServiceErrorResponse { Message = "test", Code = 123 })),
            StatusCode = System.Net.HttpStatusCode.BadRequest
        };

        var responseModel = JsonSerializer.Deserialize<InvestmentInternalBalanceResponse>(InvestmentTestData.BalanceResponseJson, SerialiationBuilder.Options());

        _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
            "SendAsync",
             ItExpr.IsAny<HttpRequestMessage>(),
             ItExpr.IsAny<CancellationToken>())
             .ReturnsAsync(response);

        var result = await _sut.GetInternalBalance(referenceNumber, businessGroup, schemeType);

        result.IsSome.Should().BeFalse();
    }

    public async Task GetInternalBalance_WhenSchemeTypeIsDCAndResponseIsNotSuccessful_ReturnsNone()
    {
        var referenceNumber = "0240707";
        var businessGroup = "LIF";
        var schemeType = "DC";
        var errorMessage = $"Failed to retrieve internal balance for {businessGroup} {referenceNumber}.";

        var response = new HttpResponseMessage
        {
            Content = new StringContent("Request Error"),
            StatusCode = System.Net.HttpStatusCode.InternalServerError
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var result = await _sut.GetInternalBalance(referenceNumber, businessGroup, schemeType);

        result.IsNone.Should().BeTrue();
        _loggerMock.Verify(logger => logger.Log(
              It.Is<LogLevel>(logLevel => logLevel == LogLevel.Error),
              It.Is<EventId>(eventId => eventId.Id == 0),
              It.Is<It.IsAnyType>((@object, @type) => @object.ToString() == errorMessage && @type.Name == "FormattedLogValues"),
              It.IsAny<Exception>(),
              It.IsAny<Func<It.IsAnyType, Exception, string>>()),
        Times.Once);
    }

    public async Task GetInvestmentForecast_WhenSchemeTypeIsNotDC_ReturnsInvestmentForecastResponse()
    {
        var referenceNumber = "123456";
        var businessGroup = "test";
        var schemeType = "DB";
        var targetAge = 65;

        var result = await _sut.GetInvestmentForecast(referenceNumber, businessGroup, targetAge, schemeType);

        result.Right().Should().BeOfType<InvestmentForecastResponse>();
    }

    public async Task GetInvestmentForecast_WhenSchemeTypeIsDC_ByAge_ReturnsInvestmentForecastResponse()
    {
        var referenceNumber = "0240707";
        var businessGroup = "LIF";
        var schemeType = "DC";
        var targetAge = 65;

        var response = new HttpResponseMessage
        {
            Content = new StringContent(InvestmentTestData.ForecastResponseJson),
            StatusCode = HttpStatusCode.OK,
        };

        var responseModel = JsonSerializer.Deserialize<InvestmentForecastResponse>(InvestmentTestData.ForecastResponseJson, SerialiationBuilder.Options());

        _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
                           "SendAsync",
                           ItExpr.IsAny<HttpRequestMessage>(),
                           ItExpr.IsAny<CancellationToken>())
             .ReturnsAsync(response);

        var result = await _sut.GetInvestmentForecast(referenceNumber, businessGroup, targetAge, schemeType);

        result.Right().Should().BeEquivalentTo(responseModel);
    }

    public async Task GetInvestmentForecast_WhenThrows()
    {
        var referenceNumber = "0240707";
        var businessGroup = "LIF";
        var schemeType = "DC";
        var targetAge = 65;


        var result = await _sut.GetInvestmentForecast(referenceNumber, businessGroup, targetAge, schemeType);

        result.Right().Should().NotBeNull();
        result.Right().Ages.Should().BeEmpty();
    }

    public async Task GetInvestmentForecast_WhenSchemeTypeIsDC_ByAgesList_ReturnsInvestmentForecastResponse()
    {
        var referenceNumber = "0240707";
        var businessGroup = "LIF";
        var targetAges = new List<int> { 65 };

        var response = new HttpResponseMessage
        {
            Content = new StringContent(InvestmentTestData.ForecastResponseJson),
            StatusCode = HttpStatusCode.OK,
        };

        var responseModel = JsonSerializer.Deserialize<InvestmentForecastResponse>(InvestmentTestData.ForecastResponseJson, SerialiationBuilder.Options());

        _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
                           "SendAsync",
                           ItExpr.IsAny<HttpRequestMessage>(),
                           ItExpr.IsAny<CancellationToken>())
             .ReturnsAsync(response);

        var result = await _sut.GetInvestmentForecast(referenceNumber, businessGroup, targetAges);

        result.IsSome.Should().BeTrue();
        result.Value().Should().BeEquivalentTo(responseModel);
    }

    public async Task GetInvestmentForecastOverload_WhenThrows()
    {
        var referenceNumber = "0240707";
        var businessGroup = "LIF";
        var targetAges = new List<int> { 65 };


        var result = await _sut.GetInvestmentForecast(referenceNumber, businessGroup, targetAges);

        result.IsNone.Should().BeTrue();
    }

    public async Task GetInvestmentStrategies_ReturnsStrategyContributionTypeResponse()
    {
        var businessGroup = "LIF";
        var schemeCode = "0022";
        string contType = "SPEN";

        var response = new HttpResponseMessage
        {
            Content = new StringContent(InvestmentTestData.StrategiesResponseJson),
            StatusCode = HttpStatusCode.OK,
        };

        var responseModel = JsonSerializer.Deserialize<DcSpendingResponse<StrategyContributionTypeResponse>>(InvestmentTestData.StrategiesResponseJson, SerialiationBuilder.Options());

        _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
                           "SendAsync",
                           ItExpr.IsAny<HttpRequestMessage>(),
                           ItExpr.IsAny<CancellationToken>())
             .ReturnsAsync(response);

        var result = await _sut.GetInvestmentStrategies(businessGroup, schemeCode, contType);

        result.Value().Should().BeEquivalentTo(responseModel);
    }

    public async Task GetInvestmentStrategies_ReturnsNone_WhenApiReturnsError()
    {
        var businessGroup = "LIF";
        var schemeCode = "0022";
        string contType = "SPEN";

        var response = new HttpResponseMessage
        {
            Content = new StringContent(JsonSerializer.Serialize(new InvestmentServiceErrorResponse { Message = "test", Code = 123 })),
            StatusCode = HttpStatusCode.BadRequest,
        };

        _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
                           "SendAsync",
                           ItExpr.IsAny<HttpRequestMessage>(),
                           ItExpr.IsAny<CancellationToken>())
             .ReturnsAsync(response);

        var result = await _sut.GetInvestmentStrategies(businessGroup, schemeCode, contType);

        result.IsNone.Should().BeTrue();
    }

    public async Task GetInvestmentStrategies_ReturnsNone_WhenApiThrows()
    {
        var businessGroup = "LIF";
        var schemeCode = "0022";
        string contType = "SPEN";

        _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
                           "SendAsync",
                           ItExpr.IsAny<HttpRequestMessage>(),
                           ItExpr.IsAny<CancellationToken>())
             .ThrowsAsync(new Exception());

        var result = await _sut.GetInvestmentStrategies(businessGroup, schemeCode, contType);

        result.IsNone.Should().BeTrue();
    }

    public async Task GetInvestmentFunds_ReturnsFundContributionTypeResponse()
    {
        var businessGroup = "LIF";
        var schemeCode = "0022";
        string contType = "SPEN";

        var response = new HttpResponseMessage
        {
            Content = new StringContent(InvestmentTestData.FundsResponseJson),
            StatusCode = HttpStatusCode.OK,
        };

        var responseModel = JsonSerializer.Deserialize<DcSpendingResponse<FundContributionTypeResponse>>(InvestmentTestData.FundsResponseJson, SerialiationBuilder.Options());

        _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
                           "SendAsync",
                           ItExpr.IsAny<HttpRequestMessage>(),
                           ItExpr.IsAny<CancellationToken>())
             .ReturnsAsync(response);

        var result = await _sut.GetInvestmentFunds(businessGroup, schemeCode, contType);

        result.Value().Should().BeEquivalentTo(responseModel);
    }

    public async Task GetInvestmentFunds_ReturnsNone_WhenApiReturnsError()
    {
        var businessGroup = "LIF";
        var schemeCode = "0022";
        string contType = "SPEN";

        var response = new HttpResponseMessage
        {
            Content = new StringContent(JsonSerializer.Serialize(new InvestmentServiceErrorResponse { Message = "test", Code = 123 })),
            StatusCode = HttpStatusCode.BadRequest,
        };

        _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
                           "SendAsync",
                           ItExpr.IsAny<HttpRequestMessage>(),
                           ItExpr.IsAny<CancellationToken>())
             .ReturnsAsync(response);

        var result = await _sut.GetInvestmentFunds(businessGroup, schemeCode, contType);

        result.IsNone.Should().BeTrue();
    }

    public async Task GetInvestmentFunds_ReturnsNone_WhenThrowsOnApiCall()
    {
        var businessGroup = "LIF";
        var schemeCode = "0022";
        string contType = "SPEN";

        _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
                           "SendAsync",
                           ItExpr.IsAny<HttpRequestMessage>(),
                           ItExpr.IsAny<CancellationToken>())
             .ThrowsAsync(new Exception());

        var result = await _sut.GetInvestmentFunds(businessGroup, schemeCode, contType);

        result.IsNone.Should().BeTrue();
    }

    public async Task GetTargetSchemeMappings_ReturnsTargetSchemeMappingResponse()
    {
        var businessGroup = "LIF";
        var schemeCode = "0022";
        string contType = "SPEN";

        var response = new HttpResponseMessage
        {
            Content = new StringContent(InvestmentTestData.TargetSchemeMappingResponseJson),
            StatusCode = HttpStatusCode.OK,
        };

        var responseModel = JsonSerializer.Deserialize<TargetSchemeMappingResponse>(InvestmentTestData.TargetSchemeMappingResponseJson, SerialiationBuilder.Options());

        _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
                           "SendAsync",
                           ItExpr.IsAny<HttpRequestMessage>(),
                           ItExpr.IsAny<CancellationToken>())
             .ReturnsAsync(response);

        var result = await _sut.GetTargetSchemeMappings(businessGroup, schemeCode, contType);

        result.Value().Should().BeEquivalentTo(responseModel);
    }

    public async Task GetTargetSchemeMappings_ReturnsNone_WhenApiReturnsError()
    {
        var businessGroup = "LIF";
        var schemeCode = "0022";
        string contType = "SPEN";

        var response = new HttpResponseMessage
        {
            Content = new StringContent(JsonSerializer.Serialize(new InvestmentServiceErrorResponse { Message = "test", Code = 123 })),
            StatusCode = HttpStatusCode.BadRequest,
        };

        var responseModel = JsonSerializer.Deserialize<TargetSchemeMappingResponse>(InvestmentTestData.TargetSchemeMappingResponseJson, SerialiationBuilder.Options());

        _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
                           "SendAsync",
                           ItExpr.IsAny<HttpRequestMessage>(),
                           ItExpr.IsAny<CancellationToken>())
             .ReturnsAsync(response);

        var result = await _sut.GetTargetSchemeMappings(businessGroup, schemeCode, contType);

        result.IsNone.Should().BeTrue();
    }

    public async Task GetTargetSchemeMappings_ReturnsNone_WhenThrowsOnApiCall()
    {
        var businessGroup = "LIF";
        var schemeCode = "0022";
        string contType = "SPEN";

        _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
                           "SendAsync",
                           ItExpr.IsAny<HttpRequestMessage>(),
                           ItExpr.IsAny<CancellationToken>())
              .ThrowsAsync(new Exception());

        var result = await _sut.GetTargetSchemeMappings(businessGroup, schemeCode, contType);

        result.IsNone.Should().BeTrue();
    }

    public async Task GetInvestmentForecastAge_ReturnsInvestmentForecastAgeResponse()
    {
        var businessGroup = "LIF";
        var referenceNumber = "7000003";

        var responseModel = new InvestmentForecastAgeResponse { RetirementAge = 65, RetirementDate = "20-02-2026" };
        var response = new HttpResponseMessage
        {
            Content = new StringContent(JsonSerializer.Serialize(responseModel, SerialiationBuilder.Options())),
            StatusCode = HttpStatusCode.OK,
        };

        _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
                           "SendAsync",
                           ItExpr.IsAny<HttpRequestMessage>(),
                           ItExpr.IsAny<CancellationToken>())
             .ReturnsAsync(response);

        var result = await _sut.GetInvestmentForecastAge(businessGroup, referenceNumber);

        result.IsSome.Should().BeTrue();
        result.Value().Should().BeEquivalentTo(responseModel);
    }

    [Input(HttpStatusCode.Unauthorized)]
    [Input(HttpStatusCode.NotFound)]
    [Input(HttpStatusCode.BadRequest)]
    public async Task GetInvestmentForecastAgeReturnsNone_WhenInvestmentServiceEndpointReturnsErrorResponse(HttpStatusCode httpStatusCode)
    {
        var businessGroup = "LIF";
        var referenceNumber = "7000003";

        var responseModel = new InvestmentServiceErrorResponse { Code = 123456, Message = "test-1" };
        var response = new HttpResponseMessage
        {
            Content = new StringContent(JsonSerializer.Serialize(responseModel, SerialiationBuilder.Options())),
            StatusCode = httpStatusCode,
        };

        _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
                           "SendAsync",
                           ItExpr.IsAny<HttpRequestMessage>(),
                           ItExpr.IsAny<CancellationToken>())
             .ReturnsAsync(response);

        var result = await _sut.GetInvestmentForecastAge(businessGroup, referenceNumber);

        result.IsSome.Should().BeFalse();
    }

    public async Task GetInvestmentForecastAgesReturnsNone_WhenThrows()
    {
        var businessGroup = "LIF";
        var referenceNumber = "7000003";

        var responseModel = new InvestmentForecastAgeResponse { RetirementAge = 65, RetirementDate = "20-02-2026" };
        var response = new HttpResponseMessage
        {
            Content = new StringContent(JsonSerializer.Serialize(responseModel, SerialiationBuilder.Options())),
            StatusCode = HttpStatusCode.OK,
        };

        _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
                           "SendAsync",
                           ItExpr.IsAny<HttpRequestMessage>(),
                           ItExpr.IsAny<CancellationToken>())
             .ThrowsAsync(new Exception());

        var result = await _sut.GetInvestmentForecastAge(businessGroup, referenceNumber);

        result.IsSome.Should().BeFalse();
    }

    public async Task GetLatestContribution_LatestContributionResponse()
    {
        var response = new HttpResponseMessage
        {
            Content = new StringContent(InvestmentTestData.LatestContributionJson),
            StatusCode = HttpStatusCode.OK,
        };

        var responseModel = JsonSerializer.Deserialize<LatestContributionResponse>(InvestmentTestData.LatestContributionJson);

        _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
                           "SendAsync",
                           ItExpr.IsAny<HttpRequestMessage>(),
                           ItExpr.IsAny<CancellationToken>())
             .ReturnsAsync(response);

        var result = await _sut.GetLatestContribution("WPS", "1234567");

        result.Value().Should().BeEquivalentTo(responseModel);
    }

    public async Task GetLatestContribution_ReturnsNone_WhenApiReturnsError()
    {
        var response = new HttpResponseMessage
        {
            Content = new StringContent(JsonSerializer.Serialize(new InvestmentServiceErrorResponse { Message = "test", Code = 123 })),
            StatusCode = HttpStatusCode.BadRequest,
        };

        _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
                           "SendAsync",
                           ItExpr.IsAny<HttpRequestMessage>(),
                           ItExpr.IsAny<CancellationToken>())
             .ReturnsAsync(response);

        var result = await _sut.GetLatestContribution("WPS", "1234567");

        result.IsNone.Should().BeTrue();
        _loggerMock.VerifyLogging("Latest Contribution endpoint for WPS-1234567 returned error: Message: test. Code: 123",
                LogLevel.Error,
                Times.Once());
    }

    public async Task GetLatestContribution_ReturnsNone_WhenThrowsOnApiCall()
    {
        _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
                           "SendAsync",
                           ItExpr.IsAny<HttpRequestMessage>(),
                           ItExpr.IsAny<CancellationToken>())
             .ThrowsAsync(new Exception("test message"));

        var result = await _sut.GetLatestContribution("WPS", "1234567");

        result.IsNone.Should().BeTrue();
        _loggerMock.VerifyLogging("Failed to retrieve latest Contribution.",
                LogLevel.Error,
                Times.Once());
    }

    public async Task CreateAnnuityQuote_ReturnsHttpStatusCodeCreated()
    {
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.Created,
        };

        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.CreateAnnuityQuote("WPS", "1234567", new InvestmentQuoteRequest());

        result.IsRight.Should().BeTrue();
        result.Right().Should().Be(Unit.Default);
    }

    public async Task CreateAnnuityQuote_ReturnsErrorIfStatusCodeIsNotCreated()
    {
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.BadRequest,
        };

        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.CreateAnnuityQuote("WPS", "1234567", new InvestmentQuoteRequest());

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be("Response status code does not indicate success: 400 (Bad Request).");
        _loggerMock.VerifyLogging("Annuity quote request failed. Error: Response status code does not indicate success: 400 (Bad Request).",
                LogLevel.Error,
                Times.Once());
    }
}