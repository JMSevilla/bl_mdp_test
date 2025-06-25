using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WTW.MdpService.Beneficiaries;
using WTW.MdpService.Infrastructure.MemberService;
using WTW.MdpService.Infrastructure.TokenService;
using WTW.TestCommon;
using WTW.TestCommon.Helpers;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Infrastructure.MemberService;

public class MemberServiceClientTest
{
    private readonly MemberServiceClient _sut;
    private readonly Mock<ILogger<MemberServiceClient>> _loggerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ICachedTokenServiceClient> _tokenServiceClientMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<IOptionsSnapshot<MemberServiceOptions>> _optionsMock;
    public MemberServiceClientTest()
    {
        _loggerMock = new Mock<ILogger<MemberServiceClient>>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _tokenServiceClientMock = new Mock<ICachedTokenServiceClient>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        _httpClientFactoryMock
        .Setup(x => x.CreateClient("MemberService"))
        .Returns(new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://memberservice.dev.awstas.net")
        });
        _tokenServiceClientMock.Setup(x => x.GetAccessToken()).ReturnsAsync(new TokenServiceResponse { AccessToken = "access token", SecondsExpiresIn = 300 });
        _optionsMock = new Mock<IOptionsSnapshot<MemberServiceOptions>>();
        _optionsMock.Setup(m => m.Value).Returns(new MemberServiceOptions
        {
            GetBeneficiariesPath = "internal/v1/bgroups/{0}/members/{1}/beneficiaries?includeRevoked={2}&refreshCache={3}",
            GetLinkedMemberPath = "internal/v1/bgroups/{0}/members/{1}/linked",
            GetMemberSummaryPath = "internal/v1/bgroups/{0}/members/{1}/summary",
            GetPensionDetailsPath = "internal/v1/bgroups/{0}/members/{1}/pension-details",
            GetPersonalDetailPath = "internal/v1/bgroups/{0}/members/{1}/personal-details",
            GetContactDetailsPath = "internal/v1/bgroups/{0}/members/{1}/contact-details",
            GetMemberMatchingAbsolutePath = "internal/v1/members/matching"
        });

        _sut = new MemberServiceClient(_httpClientFactoryMock.Object.CreateClient("MemberService"),
                                       _tokenServiceClientMock.Object,
                                       _optionsMock.Object,
                                       _loggerMock.Object);
    }
    public async Task WhenGetBeneficiariesIsCalledWithValidData_ThenReturnGetBeneficiariesResponseWithValidData()
    {
        var response = new HttpResponseMessage
        {
            Content = JsonContent.Create(BeneficiariesV2ResponseFactory.Create()),
            StatusCode = System.Net.HttpStatusCode.OK
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.GetBeneficiaries(It.IsAny<string>(), It.IsAny<string>(), false, false);

        result.IsSome.Should().BeTrue();
        result.Value<BeneficiariesV2Response>().Should().BeEquivalentTo(BeneficiariesV2ResponseFactory.Create());
    }

    public async Task WhenGetBeneficiariesNotFound_ThenReturnNoneOption()
    {
        var response = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.NotFound
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.GetBeneficiaries("ABC", "1234567", false, false);

        result.IsNone.Should().BeTrue();
        _loggerMock.VerifyLogging("GetBeneficiaries details not found for refno 1234567",
            LogLevel.Warning, Times.Once());
    }

    public async Task WhenGetBeneficiariesThrowsException_ThenThrowException()
    {
        var response = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.InternalServerError
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = async () => await _sut.GetBeneficiaries("ABC", "1234567", false, false);

        await result.Should().ThrowAsync<HttpRequestException>();
    }

    public async Task WhenGetLinkedMemberIsCalledWithValidData_ThenReturnGetLinkedRecordClientResponse()
    {
        var response = new HttpResponseMessage
        {
            Content = JsonContent.Create(GetLinkedRecordClientResponseFactory.Create(TestData.Bgroup)),
            StatusCode = System.Net.HttpStatusCode.OK
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.GetBcUkLinkedRecord(It.IsAny<string>(), It.IsAny<string>());

        result.Value().Should().BeEquivalentTo(GetLinkedRecordClientResponseFactory.Create(TestData.Bgroup));
    }

    public async Task WhenGetLinkedMemberNotFound_ThenReturnNoneOption()
    {
        var response = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.NotFound
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.GetBcUkLinkedRecord(It.IsAny<string>(), TestData.RefNo);

        result.IsNone.Should().BeTrue();
        _loggerMock.VerifyLogging($"Linked record not found for refno {TestData.RefNo}", LogLevel.Warning, Times.Once());
    }

    public async Task WhenGetLinkedMemberThrowsException_ThenThrowException()
    {
        var response = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.InternalServerError
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = async () => await _sut.GetBcUkLinkedRecord(It.IsAny<string>(), It.IsAny<string>());

        await result.Should().ThrowAsync<HttpRequestException>();
    }

    public async Task WhenGetPensionDetailsIsCalledWithValidData_ThenReturnGetPensionDetailsClientResponse()
    {
        var response = new HttpResponseMessage
        {
            Content = JsonContent.Create(GetPensionDetailsClientResponseFactory.Create()),
            StatusCode = System.Net.HttpStatusCode.OK
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.GetPensionDetails(It.IsAny<string>(), It.IsAny<string>());

        result.Value().Should().BeEquivalentTo(GetPensionDetailsClientResponseFactory.Create());
    }

    public async Task WhenGetPensionDetailsNoFound_ThenReturnNoneData()
    {
        var response = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.NotFound
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.GetPensionDetails(It.IsAny<string>(), TestData.RefNo);

        result.IsNone.Should().BeTrue();
        _loggerMock.VerifyLogging($"Member pension details not found for {TestData.RefNo}", LogLevel.Warning, Times.Once());
    }

    public async Task WhenGetPensionDetailsThrowsException_ThenThrowException()
    {
        var response = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.InternalServerError
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = async () => await _sut.GetPensionDetails(It.IsAny<string>(), It.IsAny<string>());

        await result.Should().ThrowAsync<HttpRequestException>();
    }

    public async Task WhenGetMemberSummaryIsCalledWithValidData_ThenReturnGetMemberSummaryClientResponse()
    {
        var response = new HttpResponseMessage
        {
            Content = JsonContent.Create(GetMemberSummaryClientResponseFactory.Create()),
            StatusCode = System.Net.HttpStatusCode.OK
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.GetMemberSummary(It.IsAny<string>(), It.IsAny<string>());

        result.Value().Should().BeEquivalentTo(GetMemberSummaryClientResponseFactory.Create());
    }

    public async Task WhenGetMemberSummaryDataNotFound_ThenReturnThenReturnNoneOption()
    {
        var response = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.NotFound
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.GetMemberSummary(It.IsAny<string>(), TestData.RefNo);

        result.IsNone.Should().BeTrue();
        _loggerMock.VerifyLogging($"Member summary details not found for {TestData.RefNo}", LogLevel.Warning, Times.Once());
    }

    public async Task WhenGetMemberSummaryThrowsException_ThenThrowException()
    {
        var response = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.InternalServerError
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = async () => await _sut.GetMemberSummary(It.IsAny<string>(), It.IsAny<string>());

        await result.Should().ThrowAsync<HttpRequestException>();
    }

    public async Task WhenGetPersonalDetailIsCalledWithValidData_ThenReturnGetMemberPersonalDetailClientResponse()
    {
        var response = new HttpResponseMessage
        {
            Content = JsonContent.Create(GetMemberPersonalDetailClientResponseFactory.Create()),
            StatusCode = System.Net.HttpStatusCode.OK
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.GetPersonalDetail(It.IsAny<string>(), It.IsAny<string>());

        result.Value().Should().BeEquivalentTo(GetMemberPersonalDetailClientResponseFactory.Create());
    }

    public async Task WhenGetPersonalDetailDataNottFound_ThenReturnNoneOption()
    {
        var response = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.NotFound
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.GetPersonalDetail(It.IsAny<string>(), TestData.RefNo);

        result.IsNone.Should().BeTrue();
        _loggerMock.VerifyLogging($"Member personal details not found for {TestData.RefNo}", LogLevel.Warning, Times.Once());
    }

    public async Task WhenGetPersonalDetailThrowsException_ThenThrowException()
    {
        var response = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.InternalServerError
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = async () => await _sut.GetPersonalDetail(It.IsAny<string>(), It.IsAny<string>());

        await result.Should().ThrowAsync<HttpRequestException>();
    }
    public async Task WhenGetMemberMatchingRecordsIsCalledWithValidData_ThenReturnGetMatchingMemberClientResponse()
    {
        var response = new HttpResponseMessage
        {
            Content = JsonContent.Create(GetMatchingMemberClientResponseFactory.Create(TestData.RefNo)),
            StatusCode = System.Net.HttpStatusCode.OK
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.GetMemberMatchingRecords(It.IsAny<string>(), It.IsAny<GetMemberMatchingClientRequest>());

        result.Value().Should().BeEquivalentTo(GetMatchingMemberClientResponseFactory.Create(TestData.RefNo));
    }

    public async Task WhenGetMemberMatchingRecordsHandledStatusCodeException_ThenReturnNoneOption()
    {
        var response = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.NotFound
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.GetMemberMatchingRecords(It.IsAny<string>(), It.IsAny<GetMemberMatchingClientRequest>());

        result.IsNone.Should().BeTrue();
    }

    public async Task WhenGetMemberMatchingRecordsThrowsException_ThenThrowException()
    {
        var response = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.InternalServerError
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = async () => await _sut.GetMemberMatchingRecords(It.IsAny<string>(), It.IsAny<GetMemberMatchingClientRequest>());

        await result.Should().ThrowAsync<HttpRequestException>();
    }

    public async Task WhenGetContactDetailsIsCalledWithValidData_ThenReturnGetMemberPersonalDetailClientResponse()
    {
        var response = new HttpResponseMessage
        {
            Content = JsonContent.Create(MemberContactDetailsClientResponseFactory.Create()),
            StatusCode = System.Net.HttpStatusCode.OK
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.GetContactDetails(It.IsAny<string>(), It.IsAny<string>());

        result.Value().Should().BeEquivalentTo(MemberContactDetailsClientResponseFactory.Create());
    }

    public async Task WhenGetContactDetailsDataNotFound_ThenReturnNoneOption()
    {
        var response = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.NotFound
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = await _sut.GetContactDetails(It.IsAny<string>(), TestData.RefNo);

        result.IsNone.Should().BeTrue();
        _loggerMock.VerifyLogging($"Member contact details not found for {TestData.RefNo}", LogLevel.Warning, Times.Once());
    }

    public async Task WhenGetContactDetailsThrowsException_ThenThrowException()
    {
        var response = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.InternalServerError
        };
        _httpMessageHandlerMock.SetupHandler(response);

        var result = async () => await _sut.GetContactDetails(It.IsAny<string>(), It.IsAny<string>());

        await result.Should().ThrowAsync<HttpRequestException>();
    }
}
public static class BeneficiariesV2ResponseFactory
{
    public static BeneficiariesV2Response Create()
    {
        return new BeneficiariesV2Response();
    }
}

public static class GetLinkedRecordClientResponseFactory
{
    public static GetLinkedRecordClientResponse Create(string bGroup)
    {
        return new GetLinkedRecordClientResponse()
        {
            LinkedRecords = new List<LinkedMemberClientResponse>() {
              new LinkedMemberClientResponse()
              {
                 Bgroup=bGroup,
                 ReferenceNumber=TestData.RefNo
              }
          }
        };
    }
    public static GetLinkedRecordClientResponse Create(string bGroup, string refno)
    {
        return new GetLinkedRecordClientResponse()
        {
            LinkedRecords = new List<LinkedMemberClientResponse>() {
              new LinkedMemberClientResponse()
              {
                 Bgroup=bGroup,
                 ReferenceNumber=refno
              }
          }
        };
    }
}

public static class GetPensionDetailsClientResponseFactory
{
    public static GetPensionDetailsClientResponse Create()
    {
        return new GetPensionDetailsClientResponse()
        {
            ElectionofIRBasis = "ElectionofIRBasis",
        };
    }
}

public static class GetMemberSummaryClientResponseFactory
{
    public static GetMemberSummaryClientResponse Create()
    {
        return new GetMemberSummaryClientResponse()
        {
            Bgroup = "WIP",
            Status = "PP"
        };
    }

    public static GetMemberSummaryClientResponse CreateNL(string bgroup)
    {
        return new GetMemberSummaryClientResponse()
        {
            Bgroup = bgroup,
            Status = "NL",
        };
    }
}

public static class GetMemberPersonalDetailClientResponseFactory
{
    public static GetMemberPersonalDetailClientResponse Create()
    {
        return new GetMemberPersonalDetailClientResponse()
        {
            Forenames = "John",
            Surname = "Doe",
            DateOfBirth = TestData.BirthDate
        };
    }
}

public static class GetMatchingMemberClientResponseFactory
{
    public static GetMatchingMemberClientResponse Create(string refno)
    {
        return new GetMatchingMemberClientResponse()
        {
            MemberList = new List<GetMatchingMemberClientResponse.MemberReferenceDto>()
            {
                new GetMatchingMemberClientResponse.MemberReferenceDto()
                {
                    ReferenceNumber =refno
                }
            }
        };
    }
}

public static class GetMemberMatchingClientRequestFactory
{
    public static GetMemberMatchingClientRequest Create()
    {
        return new GetMemberMatchingClientRequest()
        {
            DateOfBirth = TestData.BirthDate,
            NiNumber = TestData.RefNo,
            surname = TestData.Bgroup
        };
    }
}

public static class MemberContactDetailsClientResponseFactory
{
    public static MemberContactDetailsClientResponse Create()
    {
        return new MemberContactDetailsClientResponse()
        {
            Telephone = "0123456789",
            Address = new MemberContactDetailsAddressResponse
            {
                Line1 = "123 Test Street",
                PostCode = "TE1 2ST",
                CountryCode = "GB"
            }
        };
    }
}