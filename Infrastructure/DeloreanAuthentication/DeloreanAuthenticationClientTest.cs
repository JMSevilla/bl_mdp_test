using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WTW.MdpService.Infrastructure.DeloreanAuthentication;
using WTW.MdpService.Infrastructure.TokenService;
using WTW.TestCommon;
using WTW.TestCommon.Helpers;
using WTW.Web;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Infrastructure.DeloreanAuthentication;

public class DeloreanAuthenticationClientTest
{
    private readonly DeloreanAuthenticationClient _sut;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ICachedTokenServiceClient> _tokenServiceClientMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<IOptionsSnapshot<DeloreanAuthenticationOptions>> _optionsMock;
    private readonly Mock<ILogger<DeloreanAuthenticationClient>> _loggerMock;

    public DeloreanAuthenticationClientTest()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _tokenServiceClientMock = new Mock<ICachedTokenServiceClient>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _loggerMock = new Mock<ILogger<DeloreanAuthenticationClient>>();

        _httpClientFactoryMock
        .Setup(x => x.CreateClient("DeloreanAuthentication"))
        .Returns(new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://authenticationservice.dev.awstas.net")
        });
        _tokenServiceClientMock.Setup(x => x.GetAccessToken()).ReturnsAsync(new TokenServiceResponse { AccessToken = "access token", SecondsExpiresIn = 300 });
        _optionsMock = new Mock<IOptionsSnapshot<DeloreanAuthenticationOptions>>();
        _optionsMock.Setup(m => m.Value).Returns(new DeloreanAuthenticationOptions
        {

            GetMemberAbsolutePath = "/member/{0}/{1}",
            UpdateMemberAbsolutePath = "/member/{0}",
            CheckEligibilityAbsolutePath = "/member/{0}/check-eligibility"
        });
        _sut = new DeloreanAuthenticationClient(_tokenServiceClientMock.Object,
             _httpClientFactoryMock.Object.CreateClient("DeloreanAuthentication"), _optionsMock.Object, _loggerMock.Object);

    }

    public async Task WhenGetMemberAccessIsCalledWithValidData_ThenReturnGetMemberAccessResponse()
    {
        var memberGuid = Guid.NewGuid();
        var response = new HttpResponseMessage
        {
            Content = JsonContent.Create(GetMemberAccessClientResponseFactory.Create(memberGuid)),
            StatusCode = System.Net.HttpStatusCode.OK
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.GetMemberAccess(MdpConstants.AppName, It.IsAny<Guid>());

        result.Value().Should().BeEquivalentTo(GetMemberAccessClientResponseFactory.Create(memberGuid));
    }

    public async Task WhenGetMemberAccessDataNotFound_ThenReturnOptionNone()
    {
        var memberGuid = Guid.NewGuid();
        var response = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.NotFound
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.GetMemberAccess(MdpConstants.AppName, TestData.Sub);

        result.IsNone.Should().BeTrue();
        _loggerMock.VerifyLogging($"No memberAccess record found for {TestData.Sub}", LogLevel.Warning, Times.Once());
    }

    public async Task WhenGetMemberAccessReturnsUnHandledStatusCode_ThenThrowException()
    {
        var memberGuid = Guid.NewGuid();
        var response = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.InternalServerError
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = async () => await _sut.GetMemberAccess(MdpConstants.AppName, memberGuid);

        await result.Should().ThrowAsync<HttpRequestException>();
    }

    public async Task WhenUpdateMemberIsCalledWithValidData_ThenReturnSuccessResponse()
    {
        var memberGuid = Guid.NewGuid();
        var memberAuthGuid = Guid.NewGuid();
        var response = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.OK
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = async () => await _sut.UpdateMember(MdpConstants.AppName, memberGuid, memberAuthGuid);
        await result.Should().NotThrowAsync<HttpRequestException>();
    }

    public async Task WhenUpdateMemberReturnNon200StatusCode_ThenThrowException()
    {
        var memberGuid = Guid.NewGuid();
        var memberAuthGuid = Guid.NewGuid();
        var response = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.InternalServerError
        };
        _httpMessageHandlerMock.SetupHandler(response);
        var result = async () => await _sut.UpdateMember(MdpConstants.AppName, memberGuid, memberAuthGuid);

        await result.Should().ThrowAsync<HttpRequestException>();
    }

    public async Task WhenCheckEligibilityIsCalledWithValidData_ThenReturnCheckEligibilityClientResponse()
    {
        var memberGuid = Guid.NewGuid();
        var response = new HttpResponseMessage
        {
            Content = JsonContent.Create(CheckEligibilityClientResponseFactory.Create(true)),
            StatusCode = System.Net.HttpStatusCode.OK
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.CheckEligibility(It.IsAny<string>(), It.IsAny<string>(), MdpConstants.AppName);

        result.Value().Should().BeEquivalentTo(CheckEligibilityClientResponseFactory.Create(true));
    }

    public async Task WhenCheckEligibilityDataNotFound_ThenReturnOptionNone()
    {
        var memberGuid = Guid.NewGuid();
        var response = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.NotFound
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.CheckEligibility(It.IsAny<string>(), TestData.RefNo, MdpConstants.AppName);

        result.IsNone.Should().BeTrue();
        _loggerMock.VerifyLogging($"Eligibility record not found for refno - {TestData.RefNo}", LogLevel.Warning, Times.Once());
    }

    public async Task WhenCheckEligibilityThrowsException_ThenThrowException()
    {
        var memberGuid = Guid.NewGuid();
        var response = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.InternalServerError
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = async () => await _sut.CheckEligibility(It.IsAny<string>(), It.IsAny<string>(), MdpConstants.AppName);

        await result.Should().ThrowAsync<HttpRequestException>();
    }

    public async Task WhenGenerateTokenIsCalledWithValidData_ThenReturnToken()
    {
        var response = new HttpResponseMessage
        {
            Content = JsonContent.Create(OutboundSsoGenerateTokenClientResponseFactory.Create()),
            StatusCode = System.Net.HttpStatusCode.OK
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.GenerateToken(It.IsAny<OutboundSsoGenerateTokenClientRequest>());

        result.Should().BeEquivalentTo(OutboundSsoGenerateTokenClientResponseFactory.Create());
    }

    public async Task WhenGenerateTokenIsCalledAndThrowsException_ThenThrowException()
    {
        var response = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.InternalServerError
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = async () => await _sut.GenerateToken(It.IsAny<OutboundSsoGenerateTokenClientRequest>());

        await result.Should().ThrowAsync<HttpRequestException>();
    }

    public async Task WhenRegisterRelatedMemberIsCalledWithValidData_ThenReturnRelatedMemberList()
    {
        var response = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.OK
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = async () => await _sut.RegisterRelatedMember(It.IsAny<string>(), It.IsAny<string>(),
                                            It.IsAny<List<RegisterRelatedMemberDeloreanClientRequest>>());

        await result.Should().NotThrowAsync<HttpRequestException>();
    }
}
public static class GetMemberAccessClientResponseFactory
{
    public static GetMemberAccessClientResponse Create(Guid memberGuid, string status = MdpConstants.MemberActive)
    {
        return new GetMemberAccessClientResponse()
        {
            Members = new List<GetMemberDataClient>()
            {
                new GetMemberDataClient()
                {
                    MemberGuid=memberGuid,
                    BusinessGroup=TestData.Bgroup,
                    ReferenceNumber=TestData.RefNo,
                    Status=status
                }
            }
        };
    }
    public static GetMemberAccessClientResponse Create(Guid memberGuid, string refno, string bGroup)
    {
        return new GetMemberAccessClientResponse()
        {
            Members = new List<GetMemberDataClient>()
            {
                new GetMemberDataClient()
                {
                    MemberGuid=memberGuid,
                    BusinessGroup=bGroup,
                    ReferenceNumber=refno,
                    Status=MdpConstants.MemberActive
                }
            }
        };
    }
}
public static class CheckEligibilityClientResponseFactory
{
    public static CheckEligibilityClientResponse Create(bool eligible)
    {
        return new CheckEligibilityClientResponse()
        {
            Eligible = eligible,
            RegistrationStatus = MdpConstants.MemberActive,
        };
    }
}


public static class OutboundSsoGenerateTokenClientResponseFactory
{
    public static OutboundSsoGenerateTokenClientResponse Create()
    {
        return new OutboundSsoGenerateTokenClientResponse()
        {
            AccessToken = TestData.ValidToken
        };
    }
}
