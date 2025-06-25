using Moq.Protected;
using Moq;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System;
using WTW.MdpService.Infrastructure.Gbg;
using WTW.Web.Caching;
using System.Net;
using FluentAssertions;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Infrastructure.Gbg;

public class CachedGbgAdminClientTest
{
    private readonly Mock<ICache> _mockCache;
    private readonly CachedGbgAdminClient _sut;
    private readonly Mock<HttpMessageHandler> _handlerMock;

    public CachedGbgAdminClientTest()
    {
        _handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_handlerMock.Object)
        {
            BaseAddress = new Uri("https://fakeapi.com")
        };
        var client = new GbgAdminClient(httpClient, "username", "password");
        _mockCache = new Mock<ICache>();
        _sut = new CachedGbgAdminClient(client, _mockCache.Object, 10000);
    }

    public async Task DeleteJourneyPerson_ShouldReturnStatusCode_WhenTokenIsCached()
    {
        var personId = Guid.NewGuid().ToString();

        _mockCache
            .Setup(c => c.Get<GbgAccessTokenResponse>("gbg_admin_token"))
            .ReturnsAsync(new GbgAccessTokenResponse { AccessToken = "cached_token" });

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri.ToString().Contains("idscanenterprisesvc/entrymanagement/deletejourneyperson")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));


        var result = await _sut.DeleteJourneyPerson(personId).Try();

        result.IsSuccess.Should().BeTrue();
        result.Value().Should().Be(HttpStatusCode.OK);
    }

    public async Task DeleteJourneyPerson_ShouldReturnStatusCode_WhenTokenIsNotCached()
    {
        var personId = Guid.NewGuid().ToString();

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri.ToString().Contains("idscanenterprisesvc/entrymanagement/deletejourneyperson")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

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


        var result = await _sut.DeleteJourneyPerson(personId).Try();

        result.IsSuccess.Should().BeTrue();
        result.Value().Should().Be(HttpStatusCode.OK);
    }

    public async Task DeleteJourneyPersonFails()
    {
        var personId = Guid.NewGuid().ToString();

       
        var result = await _sut.DeleteJourneyPerson(personId).Try();

        result.IsFaulted.Should().BeTrue();
    }
}