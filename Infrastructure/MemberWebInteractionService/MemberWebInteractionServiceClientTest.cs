using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using WTW.MdpService.Infrastructure.EngagementEvents;
using WTW.MdpService.Infrastructure.MemberWebInteractionService;
using WTW.MdpService.Infrastructure.TokenService;
using WTW.TestCommon.Helpers;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Infrastructure.MemberWebInteractionService;

public class MemberWebInteractionServiceTest
{
    private readonly MemberWebInteractionServiceClient _sut;
    private readonly Mock<ILogger<MemberWebInteractionServiceClient>> _loggerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ICachedTokenServiceClient> _tokenServiceClientMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<IOptionsSnapshot<MemberWebInteractionServiceOptions>> _optionsMock;
    public MemberWebInteractionServiceTest()
    {
        _loggerMock = new Mock<ILogger<MemberWebInteractionServiceClient>>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _tokenServiceClientMock = new Mock<ICachedTokenServiceClient>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        _httpClientFactoryMock
        .Setup(x => x.CreateClient("MemberWebInteractionService"))
        .Returns(new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://memberwebinteractionservice.dev.awstas.net")
        });
        _tokenServiceClientMock.Setup(x => x.GetAccessToken()).ReturnsAsync(new TokenServiceResponse { AccessToken = "access token", SecondsExpiresIn = 300 });
        _optionsMock = new Mock<IOptionsSnapshot<MemberWebInteractionServiceOptions>>();
        _optionsMock.Setup(m => m.Value).Returns(new MemberWebInteractionServiceOptions
        {
            GetEngagementEventsPath = "internal/v1/bgroups/{0}/members/{1}/engagement-events",
            GetMessagesPath = "internal/v1/bgroups/{0}/members/{1}/messages",
        });

        _sut = new MemberWebInteractionServiceClient(_httpClientFactoryMock.Object.CreateClient("MemberWebInteractionService"),
                                                    _tokenServiceClientMock.Object,
                                                    _optionsMock.Object,
                                                    _loggerMock.Object);
    }

    public async Task WhenGetEngagementEventsIsCalledWithValidData_ThenReturnGetEngagementEventsResponseWithValidData()
    {
        var response = new HttpResponseMessage
        {
            Content = JsonContent.Create(MemberWebInteractionEngagementEventsResponseFactory.Create()),
            StatusCode = System.Net.HttpStatusCode.OK
        };

        _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
        "SendAsync",
             ItExpr.IsAny<HttpRequestMessage>(),
             ItExpr.IsAny<CancellationToken>())
             .ReturnsAsync(response);

        var result = await _sut.GetEngagementEvents(It.IsAny<string>(), It.IsAny<string>());

        result.IsSome.Should().BeTrue();
        result.Value<MemberWebInteractionEngagementEventsResponse>().Should().BeEquivalentTo(MemberWebInteractionEngagementEventsResponseFactory.Create());
    }

    public async Task WhenGetEngagementEventsReturnNon200StatusCode_ThenReturnNoData()
    {
        var response = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.InternalServerError
        };

        _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
        "SendAsync",
             ItExpr.IsAny<HttpRequestMessage>(),
             ItExpr.IsAny<CancellationToken>())
             .ReturnsAsync(response);

        var result = await _sut.GetEngagementEvents("ABC", "1234567");

        result.IsNone.Should().BeTrue();
        _loggerMock.VerifyLogging($"Failed to retrieve engagements events for ABC 1234567.",
            LogLevel.Error, Times.Once());
    }

    public async Task WhenGetEngagementEventsReturnsErrorResponse_ThenReturnNoData()
    {
        var errorResponse = new HttpResponseMessage
        {
            Content = JsonContent.Create(new { message = "Some error message", statusCode = 400 }),
            StatusCode = System.Net.HttpStatusCode.BadRequest
        };

        _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(errorResponse);

        var result = await _sut.GetEngagementEvents("ABC", "1234567");

        result.IsNone.Should().BeTrue();
        _loggerMock.VerifyLogging(
            "Get engagements events for ABC 1234567 returned error: Message: Some error message. Code: 400",
            LogLevel.Error,
            Times.Once());
    }

    public async Task GetMessages_ShouldReturnValidData_WhenRequestIsValid()
    {
        var memberMessagesResponse = new MemberMessagesResponse
        {
            Messages = new List<MemberMessage>
            {
                new MemberMessage
                {
                    MessageNo = 1,
                    MessageText = "Test Text",
                    EffectiveDate = DateTime.Parse("2025-02-26"),
                    Title = "Test Title"
                }
            }
        };

        var response = new HttpResponseMessage
        {
            Content = JsonContent.Create(memberMessagesResponse),
            StatusCode = System.Net.HttpStatusCode.OK
        };

        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.GetMessages(It.IsAny<string>(), It.IsAny<string>());

        result.IsSome.Should().BeTrue();
        result.Value<MemberMessagesResponse>().Should().BeEquivalentTo(memberMessagesResponse);
    }

    public async Task GetMessages_ShouldFilterMessages_IfMessageTextIsEmpty()
    {
        var memberMessagesResponse = new MemberMessagesResponse
        {
            Messages = new List<MemberMessage>
            {
                new MemberMessage
                {
                    MessageNo = 1,
                    MessageText = "Test Text",
                    EffectiveDate = DateTime.Parse("2025-02-26"),
                    Title = "Test Title"
                },
                new MemberMessage
                {
                    MessageNo = 2,
                    MessageText = "",
                    EffectiveDate = DateTime.Parse("2025-02-26"),
                    Title = "Test Title 1"
                }
            }
        };

        var response = new HttpResponseMessage
        {
            Content = JsonContent.Create(memberMessagesResponse),
            StatusCode = System.Net.HttpStatusCode.OK
        };

        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.GetMessages(It.IsAny<string>(), It.IsAny<string>());

        result.IsSome.Should().BeTrue();
        result.Value().Messages.Should().HaveCount(1);
    }

    public async Task GetMessages_ReturnsNone_IfMessagesIsEmpty()
    {
        var memberMessagesResponse = new MemberMessagesResponse
        {
            Messages = new List<MemberMessage>()
        };

        var response = new HttpResponseMessage
        {
            Content = JsonContent.Create(memberMessagesResponse),
            StatusCode = System.Net.HttpStatusCode.OK
        };

        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.GetMessages(It.IsAny<string>(), It.IsAny<string>());

        result.IsNone.Should().BeTrue();
    }

    public async Task GetMessagesReturnsErrorResponse_ThenReturnNoData()
    {
        var errorResponse = new HttpResponseMessage
        {
            Content = JsonContent.Create(new { message = "Error message test", statusCode = 400 }),
            StatusCode = System.Net.HttpStatusCode.BadRequest
        };

        _httpMessageHandlerMock.SetupHandler(errorResponse);

        var result = async () => await _sut.GetMessages(It.IsAny<string>(), It.IsAny<string>());

        await result.Should().ThrowAsync<HttpRequestException>();
    }

    public async Task GetMessagesReturnNon200StatusCode_ThenReturnNoData()
    {
        var response = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.NotFound
        };

        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.GetMessages("TEST", "1234567");

        result.IsNone.Should().BeTrue();
        _loggerMock.VerifyLogging("Messages not found for TEST 1234567.",
            LogLevel.Warning, Times.Once());
    }

}
public static class MemberWebInteractionEngagementEventsResponseFactory
{
    public static MemberWebInteractionEngagementEventsResponse Create()
    {
        return new MemberWebInteractionEngagementEventsResponse();
    }
}