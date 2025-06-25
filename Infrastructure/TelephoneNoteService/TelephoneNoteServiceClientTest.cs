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
using WTW.MdpService.Infrastructure.TelephoneNoteService;
using WTW.TestCommon.FixieConfig;

namespace WTW.MdpService.Test.Infrastructure.TelephoneNoteService;

public class TelephoneNoteServiceClientTest
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<IOptionsSnapshot<TelephoneNoteServiceOptions>> _optionsMock;
    private readonly Mock<ILogger<TelephoneNoteServiceClient>> _loggerMock;
    private readonly TelephoneNoteServiceOptions _telephoneNoteServiceOptions;
    private readonly TelephoneNoteServiceClient _sut;

    public TelephoneNoteServiceClientTest()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://telnoteservice.uat.awstas.net")
        };

        _optionsMock = new Mock<IOptionsSnapshot<TelephoneNoteServiceOptions>>();
        _loggerMock = new Mock<ILogger<TelephoneNoteServiceClient>>();

        _telephoneNoteServiceOptions = new TelephoneNoteServiceOptions
        {
            GetIntentContextAbsolutePath = "bgroups/{0}/members/{1}/intent-context?platform={2}"
        };

        _optionsMock.Setup(o => o.Value).Returns(_telephoneNoteServiceOptions);

        _sut = new TelephoneNoteServiceClient(httpClient,
            _optionsMock.Object,
            _loggerMock.Object);
    }

    public async Task GetIntentContextReturnsSuccessResponse_WhenApiCallSucceeds()
    {
        var businessGroup = "ABC";
        var referenceNumber = "123456";
        var platform = "assure";
        var expectedResponse = new IntentContextResponse
        {
            Intent = "caseUpdate",
            Ttl = "1730716603",
            SessionId = "6a29a4c2324b48308e813ec1421ef8af"
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri.ToString().Contains(string.Format(_telephoneNoteServiceOptions.GetIntentContextAbsolutePath, businessGroup, referenceNumber, platform))),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"intent\":\"caseUpdate\",\"ttl\":\"1730716603\",\"sessionId\":\"6a29a4c2324b48308e813ec1421ef8af\"}")
            });

        var result = await _sut.GetIntentContext(businessGroup, referenceNumber, platform);

        result.IsSome.Should().BeTrue();
        var value = result.Match(some => some, () => null);
        value.Intent.Should().Be(expectedResponse.Intent);
        value.Ttl.Should().Be(expectedResponse.Ttl);
        value.SessionId.Should().Be(expectedResponse.SessionId);

        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
            ItExpr.IsAny<CancellationToken>());
    }

    [Input(HttpStatusCode.NotFound)]
    [Input(HttpStatusCode.BadRequest)]
    [Input(HttpStatusCode.InternalServerError)]
    public async Task GetIntentContextReturnsNone_WhenApiCallFails(HttpStatusCode httpStatusCode)
    {
        var businessGroup = "ABC";
        var referenceNumber = "123456";
        var platform = "assure";

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri.ToString().Contains(string.Format(_telephoneNoteServiceOptions.GetIntentContextAbsolutePath, businessGroup, referenceNumber, platform))),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = httpStatusCode
            });

        var result = await _sut.GetIntentContext(businessGroup, referenceNumber, platform);

        result.IsNone.Should().BeTrue();

        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
            ItExpr.IsAny<CancellationToken>());
    }
} 