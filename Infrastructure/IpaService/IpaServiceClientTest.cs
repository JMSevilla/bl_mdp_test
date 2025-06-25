using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WTW.MdpService.Infrastructure.IpaService;
using WTW.MdpService.Infrastructure.TokenService;
using WTW.TestCommon.Helpers;
using WTW.Web.LanguageExt;

public class IpaServiceClientTest
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<ICachedTokenServiceClient> _cachedTokenServiceClientMock;
    private readonly Mock<IOptionsSnapshot<IpaServiceOptions>> _optionsMock;
    private readonly Mock<ILogger<IpaServiceClient>> _loggerMock;
    private readonly IpaServiceClient _sut;

    public IpaServiceClientTest()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _cachedTokenServiceClientMock = new Mock<ICachedTokenServiceClient>();
        _optionsMock = new Mock<IOptionsSnapshot<IpaServiceOptions>>();
        _loggerMock = new Mock<ILogger<IpaServiceClient>>();

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://ipa-service.dev")
        };

        _cachedTokenServiceClientMock
            .Setup(x => x.GetAccessToken())
            .ReturnsAsync(new TokenServiceResponse { AccessToken = "test-token" });

        _optionsMock
            .Setup(x => x.Value)
            .Returns(new IpaServiceOptions
            {
                GetCountriesAbsolutePath = "/countries",
                GetCurrenciesAbsolutePath = "/currencies"
            });

        _sut = new IpaServiceClient(httpClient, _cachedTokenServiceClientMock.Object, _optionsMock.Object, _loggerMock.Object);
    }

    public async Task GetCountries_ReturnsCountries_WhenSuccessful()
    {
        var _stubGetCountriesResponse = new GetCountriesResponse
        {
            Countries = new List<CountryDetails>
            {
                new CountryDetails { CountryCode = "US", CountryName = "United States" },
                new CountryDetails { CountryCode = "GB", CountryName = "United Kingdom" }
            }
        };
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = JsonContent.Create(_stubGetCountriesResponse),
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.GetCountries();

        result.IsSome.Should().BeTrue();
        result.Value().Should().BeEquivalentTo(_stubGetCountriesResponse);
    }

    public async Task WhenGetCountriesReturn404NotFound_ThenReturnNull()
    {
        var response = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.NotFound
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.GetCountries();

        result.IsNone.Should().BeTrue();
        _loggerMock.VerifyLogging("GetCountries - returned error", LogLevel.Error, Times.Once());
    }
    public async Task WhenGetCountriesReturn500InternalServerError_ThenReturnNull()
    {
        var response = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.InternalServerError
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.GetCountries();

        result.IsNone.Should().BeTrue();
        _loggerMock.VerifyLogging("GetCountries - returned error", LogLevel.Error, Times.Once());
    }

    public async Task GetCurrencies_ReturnsCurrencies_WhenSuccessful()
    {
        var _stubGetCurrenciesResponse = new GetCurrenciesResponse
        {
            Currencies = new List<CurrencyDetails>
            {
                new CurrencyDetails { CountryCode = "US", CurrencyCode = "USD", CurrencyName = "US Dollar" },
                new CurrencyDetails { CountryCode = "GB", CurrencyCode = "GBP", CurrencyName = "British Pound" }
            }
        };
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = JsonContent.Create(_stubGetCurrenciesResponse),
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.GetCurrencies();

        result.IsSome.Should().BeTrue();
        result.Value().Should().BeEquivalentTo(_stubGetCurrenciesResponse);
    }

    public async Task WhenGetCurrenciesReturn404NotFound_ThenReturnNull()
    {
        var response = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.NotFound
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.GetCurrencies();

        result.IsNone.Should().BeTrue();
        _loggerMock.VerifyLogging("GetCurrencies - returned error", LogLevel.Error, Times.Once());
    }
    public async Task WhenGetCurrenciesReturn500InternalServerError_ThenReturnNull()
    {
        var response = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.InternalServerError
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.GetCurrencies();

        result.IsNone.Should().BeTrue();
        _loggerMock.VerifyLogging("GetCurrencies - returned error", LogLevel.Error, Times.Once());
    }
}
