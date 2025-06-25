using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using Moq;
using Moq.Protected;
using WTW.MdpService.Infrastructure.Gbg;
using WTW.Web.Caching;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Infrastructure.Gbg;

public class CachedGbgClientTest
{
    private readonly Mock<ICache> _mockCache;
    private readonly CachedGbgClient _cachedGbgClient;
    private readonly Mock<HttpMessageHandler> _handlerMock;

    public CachedGbgClientTest()
    {
        _handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_handlerMock.Object)
        {
            BaseAddress = new Uri("https://fakeapi.com")
        };
        var client = new GbgClient(httpClient, "username", "password");
        _mockCache = new Mock<ICache>();
        _cachedGbgClient = new CachedGbgClient(client, _mockCache.Object, 10000);
    }

    public async Task GetDocuments_ShouldReturnDocumentsStream_WhenTokenIsCached()
    {
        var documentIds = new List<Guid> { Guid.NewGuid() };
        var expectedStream = new MemoryStream();

        _mockCache
            .Setup(c => c.Get<GbgAccessTokenResponse>("gbg_client_token"))
            .ReturnsAsync(new GbgAccessTokenResponse { AccessToken = "cached_token" });

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri == new Uri($"https://fakeapi.com/idscanenterprisesvc/reporting/ExportJourneyReports?evaluatedPersonEntryIds=[\"{string.Join("\",\"", documentIds)}\"]")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(expectedStream)
            });


        var result = await _cachedGbgClient.GetDocuments(documentIds).Try();

        result.IsSuccess.Should().BeTrue();
        result.Value().Length.Should().Be(expectedStream.Length);
    }

    public async Task GetDocuments_ShouldReturnDocumentsStream_WhenTokenIsNotCached()
    {
        var documentIds = new List<Guid> { Guid.NewGuid() };
        var expectedStream = new MemoryStream();
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri == new Uri($"https://fakeapi.com/idscanenterprisesvc/reporting/ExportJourneyReports?evaluatedPersonEntryIds=[\"{string.Join("\",\"", documentIds)}\"]")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(expectedStream)
            });

        _handlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>(
               "SendAsync",
               ItExpr.Is<HttpRequestMessage>(req =>
                   req.Method == HttpMethod.Post &&
                   req.RequestUri == new Uri("https://fakeapi.com/idscanenterprisesvc/token")),
               ItExpr.IsAny<CancellationToken>())
           .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
           {
               Content = new StringContent("{\"access_token\":\"fake-token\",\"expires_in\":123456}")
           });


        var result = await _cachedGbgClient.GetDocuments(documentIds).Try();

        result.IsSuccess.Should().BeTrue();
        result.Value().Length.Should().Be(expectedStream.Length);
    }

    public async Task GetDocumentsFails()
    {
        var documentIds = new List<Guid> { Guid.NewGuid() };    

        var result = await _cachedGbgClient.GetDocuments(documentIds).Try();

        result.IsFaulted.Should().BeTrue();
    }
}