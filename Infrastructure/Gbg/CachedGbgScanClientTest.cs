using Moq;
using System.Net.Http;
using System;
using WTW.MdpService.Infrastructure.Gbg;
using WTW.Web.Caching;
using Moq.Protected;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using FluentAssertions;

namespace WTW.MdpService.Test.Infrastructure.Gbg;

public class CachedGbgScanClientTest
{
    private readonly Mock<ICache> _mockCache;
    private readonly CachedGbgScanClient _sut;
    private readonly Mock<HttpMessageHandler> _handlerMock;

    public CachedGbgScanClientTest()
    {
        _handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_handlerMock.Object)
        {
            BaseAddress = new Uri("https://fakeapi.com")
        };
        var client = new GbgScanClient(httpClient, "username", "password");
        _mockCache = new Mock<ICache>();
        _sut = new CachedGbgScanClient(client, _mockCache.Object, 10000);
    }

    public async Task CreateTokenReturnsTokenFromCache()
    {
        var documentIds = new List<Guid> { Guid.NewGuid() };
        var expectedStream = new MemoryStream();

        _mockCache
            .Setup(c => c.Get<GbgAccessTokenResponse>("gbg_scan_client_token"))
            .ReturnsAsync(new GbgAccessTokenResponse { AccessToken = "cached_token" });

        var result = await _sut.CreateToken();

        result.AccessToken.Should().Be("cached_token");
    }

    public async Task CreateTokenReturnsTokenFromApi()
    {
        var documentIds = new List<Guid> { Guid.NewGuid() };
        var expectedStream = new MemoryStream();
               
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/idscanenterprisesvc/token")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"access_token\":\"fake-token\",\"expires_in\":123456}")
            });


        var result = await _sut.CreateToken();

        result.AccessToken.Should().Be("fake-token");
    }
}
