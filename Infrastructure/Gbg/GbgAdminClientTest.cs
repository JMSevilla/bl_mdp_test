using FluentAssertions;
using Moq.Protected;
using Moq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System;
using WTW.MdpService.Infrastructure.Gbg;
using System.Net;

namespace WTW.MdpService.Test.Infrastructure.Gbg;

public class GbgAdminClientTest
{
    private readonly Mock<HttpMessageHandler> _handlerMock;
    private readonly HttpClient _httpClient;
    private readonly GbgAdminClient _sut;

    public GbgAdminClientTest()
    {
        _handlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_handlerMock.Object)
        {
            BaseAddress = new Uri("https://fakeapi.com")
        };
        _sut = new GbgAdminClient(_httpClient, "username", "password");
    }

    public async Task CreateToken_ReturnsGbgAccessTokenResponse()
    {
        var responseContent = new StringContent("{\"access_token\":\"fake-token\",\"expires_in\":123456}");
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = responseContent
        };

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri == new Uri("https://fakeapi.com/idscanenterprisesvc/token")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        var result = await _sut.CreateToken();

        result.AccessToken.Should().Be("fake-token");
        result.ExpiresIn.Should().Be(123456);
    }

    public async Task CreateToken_ThrowsException()
    {
        var responseContent = new StringContent("{\"access_token\":\"fake-token\"}");
        var responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = responseContent
        };

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri == new Uri("https://fakeapi.com/idscanenterprisesvc/token")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        var action = async () => await _sut.CreateToken();

        await action.Should().ThrowAsync<HttpRequestException>();
    }

    public async Task DeletesJourneyPerson()
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri == new Uri("https://fakeapi.com/idscanenterprisesvc/entrymanagement/deletejourneyperson")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var result = await _sut.DeleteJourneyPerson("testGbgId", "testJwtToken");

        result.Should().Be(HttpStatusCode.OK);
    }

    public async Task DeletesJourneyPerson_ThrowsException()
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri == new Uri("https://fakeapi.com/idscanenterprisesvc/entrymanagement/deletejourneyperson")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest));

        var action = async () => await _sut.DeleteJourneyPerson("testGbgId", "testJwtToken");

        await action.Should().ThrowAsync<HttpRequestException>();
    }
}