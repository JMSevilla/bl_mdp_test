using System;
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
using WTW.MdpService.Infrastructure.TokenService;
using WTW.Web.Caching;

namespace WTW.MdpService.Test.Infrastructure.TokenService;

public class CachedTokenServiceClientTest
{
    private readonly Mock<ICache> _cacheMock;
    private readonly CachedTokenServiceClient _sut;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;

    public CachedTokenServiceClientTest()
    {
        var loggerMock = new Mock<ILogger<TokenServiceClient>>();
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        httpClientFactoryMock
        .Setup(x => x.CreateClient("TokenService"))
        .Returns(new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://tokenservice.dev.awstas.net/")
        });
        var tokenServiceClient = new TokenServiceClient(
            httpClientFactoryMock.Object.CreateClient("TokenService"),
            new TokenServiceClientConfiguration("client_credentials", "client_id", "client_secret", new string[] { "investment/get_investment_forecast" }),
            loggerMock.Object);

        _cacheMock = new Mock<ICache>();
        _sut = new CachedTokenServiceClient(tokenServiceClient, _cacheMock.Object);
    }

    public async Task ReturnsAccessTokenFromCache()
    {
        var response = new HttpResponseMessage
        {
            Content = new StringContent(JsonSerializer.Serialize(new { access_token = "test-jwt-token", expires_in = 300, token_type = "Bearer" })),
            StatusCode = HttpStatusCode.OK,
        };

        _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
                           "SendAsync",
                           ItExpr.IsAny<HttpRequestMessage>(),
                           ItExpr.IsAny<CancellationToken>())
             .ReturnsAsync(response);

        _cacheMock
            .Setup(x => x.Get<TokenServiceResponse>(It.IsAny<string>()))
            .ReturnsAsync(new TokenServiceResponse { AccessToken = "test-jwt-token", SecondsExpiresIn = 300, TokenType = "Bearer" });

        var result = await _sut.GetAccessToken();

        result.AccessToken.Should().Be("test-jwt-token");
        result.TokenType.Should().Be("Bearer");
        result.SecondsExpiresIn.Should().Be(300);

        _cacheMock.Verify(x => x.Get<TokenServiceResponse>("token-service-get-token-scopes"), Times.Once);
        _cacheMock.Verify(x => x.Set<TokenServiceResponse>("token-service-get-token-scopes", It.IsAny<TokenServiceResponse>(), TimeSpan.FromSeconds(295)), Times.Never);
    }

    public async Task ReturnsAccessTokenFromClientAndSetsCache()
    {
        var response = new HttpResponseMessage
        {
            Content = new StringContent(JsonSerializer.Serialize(new { access_token = "test-jwt-token", expires_in = 300, token_type = "Bearer" })),
            StatusCode = HttpStatusCode.OK,
        };

        _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
                           "SendAsync",
                           ItExpr.IsAny<HttpRequestMessage>(),
                           ItExpr.IsAny<CancellationToken>())
             .ReturnsAsync(response);

        _cacheMock
            .Setup(x => x.Get<TokenServiceResponse>(It.IsAny<string>()))
            .ReturnsAsync(Option<TokenServiceResponse>.None);

        var result = await _sut.GetAccessToken();

        result.AccessToken.Should().Be("test-jwt-token");
        result.TokenType.Should().Be("Bearer");
        result.SecondsExpiresIn.Should().Be(300);

        _cacheMock.Verify(x => x.Get<TokenServiceResponse>("token-service-get-token-scopes"), Times.Once);
        _cacheMock.Verify(x => x.Set<TokenServiceResponse>("token-service-get-token-scopes", It.IsAny<TokenServiceResponse>(), TimeSpan.FromSeconds(295)), Times.Once);
    }
}