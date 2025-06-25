using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using WTW.MdpService.IdentityVerification;
using WTW.MdpService.Infrastructure.IdvService;
using WTW.MdpService.Infrastructure.TokenService;
using WTW.TestCommon.FixieConfig;

namespace WTW.MdpService.Test.Infrastructure.IdvService;

public class IdentityVerificationClientTest
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<ICachedTokenServiceClient> _cachedTokenServiceClientMock;
    private readonly Mock<IOptionsSnapshot<IdvServiceOptions>> _optionsMock;
    private readonly Mock<ILogger<IdentityVerificationClient>> _loggerMock;
    private readonly IdvServiceOptions _idvServiceOptions;
    private readonly IdentityVerificationClient _sut;

    public IdentityVerificationClientTest()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://api.example.com")
        };

        _cachedTokenServiceClientMock = new Mock<ICachedTokenServiceClient>();
        _optionsMock = new Mock<IOptionsSnapshot<IdvServiceOptions>>();
        _loggerMock = new Mock<ILogger<IdentityVerificationClient>>();

        _idvServiceOptions = new IdvServiceOptions
        {
            VerifyIdentityAbsolutePath = "/internal/v1/bgroups/{0}/members/{1}/identity/verify",
            SaveIdentityVerificationAbsolutePath = "/internal/v1/bgroups/{0}/members/{1}/identity/result"
        };

        _optionsMock.Setup(o => o.Value).Returns(_idvServiceOptions);

        _sut = new IdentityVerificationClient(httpClient,
            _cachedTokenServiceClientMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);
    }

    public async Task VerifyIdentityReturnsResponse_WhenApiCallIsSuccessful()
    {
        var businessGroup = "ABC";
        var referenceNumber = "123456";
        var payload = new VerifyIdentityRequest(Guid.NewGuid(), "user123", false, "MDP_test", false);
        var expectedResponse = new VerifyIdentityResponse
        {
            IdentityVerificationStatus = "Verified",
            DocumentValidationStatus = "Valid"
        };

        _cachedTokenServiceClientMock
            .Setup(x => x.GetAccessToken())
            .ReturnsAsync(new TokenServiceResponse { AccessToken = "test-token" });

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri.ToString().Contains(string.Format(_idvServiceOptions.VerifyIdentityAbsolutePath, businessGroup, referenceNumber)) &&
                    req.Headers.Authorization != null &&
                    req.Headers.Authorization.Parameter == "test-token"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("{\"IdentityVerificationStatus\":\"Verified\",\"DocumentValidationStatus\":\"Valid\"}")
            });

        var result = await _sut.VerifyIdentity(businessGroup, referenceNumber, payload);

        result.Should().NotBeNull();
        result.IdentityVerificationStatus.Should().Be(expectedResponse.IdentityVerificationStatus);
        result.DocumentValidationStatus.Should().Be(expectedResponse.DocumentValidationStatus);

        _cachedTokenServiceClientMock.Verify(x => x.GetAccessToken(), Times.Once);
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
            ItExpr.IsAny<CancellationToken>());
    }

    [Input(HttpStatusCode.BadRequest)]
    [Input(HttpStatusCode.NotFound)]
    [Input(HttpStatusCode.InternalServerError)]
    public async Task VerifyIdentityReturnsNull_WhenApiCallReturnsNonSuccessCode(HttpStatusCode httpStatusCode)
    {
        var businessGroup = "ABC";
        var referenceNumber = "123456";
        var payload = new VerifyIdentityRequest(Guid.NewGuid(), "user123", false, "MDP_test", false);
        var expectedResponse = new VerifyIdentityResponse
        {
            IdentityVerificationStatus = "Verified",
            DocumentValidationStatus = "Valid"
        };

        _cachedTokenServiceClientMock
            .Setup(x => x.GetAccessToken())
            .ReturnsAsync(new TokenServiceResponse { AccessToken = "test-token" });

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri.ToString().Contains(string.Format(_idvServiceOptions.VerifyIdentityAbsolutePath, businessGroup, referenceNumber)) &&
                    req.Headers.Authorization != null &&
                    req.Headers.Authorization.Parameter == "test-token"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = httpStatusCode
            });

        var result = await _sut.VerifyIdentity(businessGroup, referenceNumber, payload);

        result.Should().BeNull();

        _cachedTokenServiceClientMock.Verify(x => x.GetAccessToken(), Times.Once);
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
            ItExpr.IsAny<CancellationToken>());
    }

    public async Task SaveIdentityVerificationReturnsResponse_WhenApiCallIsSuccessful()
    {
        var businessGroup = "ABC";
        var referenceNumber = "123456";
        var payload = new SaveIdentityVerificationRequest(caseCode: "ABC9", caseNo: "AB12345");
        var expectedResponse = new UpdateIdentityResultResponse
        {
            Message = "Success",
        };

        _cachedTokenServiceClientMock
            .Setup(x => x.GetAccessToken())
            .ReturnsAsync(new TokenServiceResponse { AccessToken = "test-token" });

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Put &&
                    req.RequestUri.ToString().Contains(string.Format(_idvServiceOptions.SaveIdentityVerificationAbsolutePath, businessGroup, referenceNumber)) &&
                    req.Headers.Authorization != null &&
                    req.Headers.Authorization.Parameter == "test-token"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("{\"Message\":\"Success\"}")
            });

        var result = await _sut.SaveIdentityVerification(businessGroup, referenceNumber, payload);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedResponse);

        _cachedTokenServiceClientMock.Verify(x => x.GetAccessToken(), Times.Once);
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put),
            ItExpr.IsAny<CancellationToken>());
    }

    [Input(HttpStatusCode.BadRequest)]
    [Input(HttpStatusCode.NotFound)]
    [Input(HttpStatusCode.InternalServerError)]
    public async Task SaveIdentityVerificationReturnsNull_WhenApiCallReturnsNonSuccessCode(HttpStatusCode httpStatusCode)
    {
        var businessGroup = "ABC";
        var referenceNumber = "1234567";
        var payload = new SaveIdentityVerificationRequest(caseCode: "ABC9", caseNo: "AB12345");

        _cachedTokenServiceClientMock
            .Setup(x => x.GetAccessToken())
            .ReturnsAsync(new TokenServiceResponse { AccessToken = "test-token" });

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = httpStatusCode,
            });

        var result = await _sut.SaveIdentityVerification(businessGroup, referenceNumber, payload);

        result.Should().BeNull();

        _cachedTokenServiceClientMock.Verify(x => x.GetAccessToken(), Times.Once);
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put),
            ItExpr.IsAny<CancellationToken>());
    }
}
