using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using WTW.MdpService.Infrastructure.TokenService;

namespace WTW.MdpService.Test.Infrastructure.TokenService;

public class TokenServiceClientTest
{
    private readonly TokenServiceClient _sut;
    private readonly Mock<ILogger<TokenServiceClient>> _loggerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;

    public TokenServiceClientTest()
    {
        _loggerMock = new Mock<ILogger<TokenServiceClient>>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        _httpClientFactoryMock
        .Setup(x => x.CreateClient("TokenService"))
        .Returns(new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://tokenservice.dev.awstas.net/")
        });

        _sut = new TokenServiceClient(
            _httpClientFactoryMock.Object.CreateClient("TokenService"),
            new TokenServiceClientConfiguration("client_credentials", "client_id", "client_secret", new string[] { "investment/get_investment_forecast" }),
            _loggerMock.Object);
    }

    public async Task GetAccessTokenReturnsAccessToken()
    {
        var response = new HttpResponseMessage
        {
            Content = new StringContent(JsonSerializer.Serialize(new { access_token = "test-jwt-token", expires_in = 300, token_type = "Bearer" })),
            StatusCode = HttpStatusCode.OK
        };

        _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
                           "SendAsync",
                           ItExpr.IsAny<HttpRequestMessage>(),
                           ItExpr.IsAny<CancellationToken>())
             .ReturnsAsync(response);

        var result = await _sut.GetAccessToken();

        result.AccessToken.Should().Be("test-jwt-token");
        result.TokenType.Should().Be("Bearer");
        result.SecondsExpiresIn.Should().Be(300);
    }

    public async Task GetAccessThrows_WhenUnableToRetrieveAccessToken()
    {
        var response = new HttpResponseMessage
        {
            Content = new StringContent(JsonSerializer.Serialize(new { message = "test-message" })),
            StatusCode = HttpStatusCode.BadRequest
        };

        _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
                           "SendAsync",
                           ItExpr.IsAny<HttpRequestMessage>(),
                           ItExpr.IsAny<CancellationToken>())
             .ReturnsAsync(response);

        var action = async () => await _sut.GetAccessToken();

        await action.Should().ThrowAsync<InvalidOperationException>();
    }
}