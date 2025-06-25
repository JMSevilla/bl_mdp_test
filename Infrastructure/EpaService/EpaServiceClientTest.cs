using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using WTW.MdpService.Infrastructure.EpaService;
using WTW.MdpService.Infrastructure.TokenService;
using WTW.TestCommon;
using WTW.TestCommon.FixieConfig;
using WTW.TestCommon.Helpers;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Infrastructure.EpaService;

public class EpaServiceClientTest
{
    private readonly EpaServiceClient _sut;

    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ICachedTokenServiceClient> _cachedTokenServiceClientMock;
    private readonly Mock<ILogger<EpaServiceClient>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<IOptionsSnapshot<EpaServiceOptions>> _optionsMock;

    public EpaServiceClientTest()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _cachedTokenServiceClientMock = new Mock<ICachedTokenServiceClient>();
        _loggerMock = new Mock<ILogger<EpaServiceClient>>();

        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        _httpClientFactoryMock
        .Setup(x => x.CreateClient("EpaServiceClient"))
        .Returns(new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://epaservice.dev.awstas.net/")
        });

        _cachedTokenServiceClientMock.Setup(x => x.GetAccessToken()).ReturnsAsync(new TokenServiceResponse { AccessToken = "access token", SecondsExpiresIn = 300 });
        _cachedTokenServiceClientMock.Setup(x => x.GetAccessToken()).ReturnsAsync(new TokenServiceResponse { AccessToken = "access token", SecondsExpiresIn = 300 });


        _optionsMock = new Mock<IOptionsSnapshot<EpaServiceOptions>>();
        _optionsMock.Setup(m => m.Value).Returns(new EpaServiceOptions
        {
            BaseUrl = "https://epaservice.dev.awstas.net/",
            GetEpaUserAbsolutePath = "/epauser/{0}/{1}",
            GetWebRuleAbsolutePath = "/webrule/{0}/{1}/{2}/{3}/{4}"
        });
        _sut = new EpaServiceClient(_httpClientFactoryMock.Object.CreateClient("EpaServiceClient"), _cachedTokenServiceClientMock.Object,
                                    _loggerMock.Object, _optionsMock.Object);
    }

    public async Task WhenGetWebRuleResultCalledWithValidInput_ThenExpectedResultIsReturnedAndLoggingOccurred()
    {
        var webRuleResultResponse = new WebRuleResultResponse
        {
            Result = "Success"
        };
        var response = new HttpResponseMessage
        {
            Content = JsonContent.Create(webRuleResultResponse),
            StatusCode = HttpStatusCode.OK
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var actualResult = await _sut.GetWebRuleResult(TestData.Bgroup, TestData.RefNo, TestData.EpaUserId, TestData.RuleId, TestData.Scheme, true);

        actualResult.IsSome.Should().BeTrue();
        actualResult.Value<WebRuleResultResponse>().Should().BeEquivalentTo(webRuleResultResponse);

        _loggerMock.VerifyLogging($"GetWebRuleResult is called - bgroup: {TestData.Bgroup}, refno: {TestData.RefNo}, userId: {TestData.EpaUserId}, ruleId: {TestData.RuleId}, schemeNo: {TestData.Scheme}", LogLevel.Information, Times.Once());
    }

    public async Task WhenGetWebRuleResultCalledAndExceptionIsCaught_ThenExpectedResultIsReturnedAndLoggingOccurred()
    {
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.InternalServerError
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var actualResult = await _sut.GetWebRuleResult(TestData.Bgroup, TestData.RefNo, TestData.EpaUserId, TestData.RuleId, TestData.Scheme, true);

        actualResult.IsNone.Should().BeTrue();

        _loggerMock.VerifyLogging($"GetWebRuleResult - Failed to retrieve WebRule result for {TestData.Bgroup} {TestData.RefNo}", LogLevel.Error, Times.Once());
    }

    public async Task WhenGetWebRuleResultCalledAndErrorIsReturned_ThenExpectedResultIsReturnedAndLoggingOccurred()
    {
        var errorResponse = new EpaServiceErrorResponse { Code = 404, Message = "Not Found Error" };
        var response = new HttpResponseMessage
        {
            Content = JsonContent.Create(errorResponse),
            StatusCode = HttpStatusCode.NotFound
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var actualResult = await _sut.GetWebRuleResult(TestData.Bgroup, TestData.RefNo, TestData.EpaUserId, TestData.RuleId, TestData.Scheme, true);

        actualResult.IsNone.Should().BeTrue();

        _loggerMock.VerifyLogging($"GetWebRuleResult - WebRule result endpoint for {TestData.Bgroup} {TestData.RefNo} returned error: Message: {errorResponse.Message}, Code: {errorResponse.Code}", LogLevel.Error, Times.Once());
    }

    public async Task WhenGetEpaUserIsCalledWithValidData_ThenReturnUserDetails()
    {
        var response = new HttpResponseMessage
        {
            Content = JsonContent.Create(GetEpaUserClientResponseFactory.Create()),
            StatusCode = HttpStatusCode.OK
        };
        _httpMessageHandlerMock.SetupHandler(response);
        var actualResult = await _sut.GetEpaUser(TestData.Bgroup, TestData.RefNo);

        actualResult.Value<GetEpaUserClientResponse>().Should().BeEquivalentTo(GetEpaUserClientResponseFactory.Create());
    }

    public async Task WhenEpaUserNotFound_ThenReturnNoneOption()
    {
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.NotFound
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var actualResult = await _sut.GetEpaUser(TestData.Bgroup, TestData.RefNo);

        actualResult.IsNone.Should().BeTrue();
    }

    [Input(true, "&refreshCache=true")]
    [Input(false, "")]
    public async Task GetWebRuleResult_ShouldIncludeRefreshCacheParameterBasedOnInput(bool cacheFlag, string expectedQuery)
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains(expectedQuery)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}")
            });

        await _sut.GetWebRuleResult(TestData.Bgroup, TestData.RefNo, TestData.EpaUserId, TestData.RuleId, TestData.Scheme, cacheFlag);

        _httpMessageHandlerMock.Protected().Verify("SendAsync", Times.Once(), ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains(expectedQuery)),
            ItExpr.IsAny<CancellationToken>());
    }
}

public static class GetEpaUserClientResponseFactory
{
    public static GetEpaUserClientResponse Create()
    {
        return new GetEpaUserClientResponse
        {
            UserId = TestData.EpaUserId
        };
    }
}
