using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Moq.Protected;
using WTW.MdpService.Infrastructure.Gbg;

namespace WTW.MdpService.Test.Infrastructure.Gbg;

public class GbgClientTest
{
    private readonly Mock<HttpMessageHandler> _handlerMock;
    private readonly HttpClient _httpClient;
    private readonly GbgClient _sut;

    public GbgClientTest()
    {
        _handlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_handlerMock.Object)
        {
            BaseAddress = new Uri("https://fakeapi.com")
        };
        _sut = new GbgClient(_httpClient, "username", "password");
    }

    public async Task GetDocuments_ReturnsStream()
    {
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var token = "fake-token";
        var responseContent = new MemoryStream();
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(responseContent)
        };

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri == new Uri($"https://fakeapi.com/idscanenterprisesvc/reporting/ExportJourneyReports?evaluatedPersonEntryIds=[\"{string.Join("\",\"", ids)}\"]")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        var result = await _sut.GetDocuments(ids, token);

        result.Length.Should().Be(responseContent.Length);
    }

    public async Task GetDocuments_ThrowsException()
    {
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var token = "fake-token";
        var responseContent = new MemoryStream();
        var responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StreamContent(responseContent)
        };

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri == new Uri($"https://fakeapi.com/idscanenterprisesvc/reporting/ExportJourneyReports?evaluatedPersonEntryIds=[\"{string.Join("\",\"", ids)}\"]")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        var action = async () => await _sut.GetDocuments(ids, token);

        await action.Should().ThrowAsync<HttpRequestException>();
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
}