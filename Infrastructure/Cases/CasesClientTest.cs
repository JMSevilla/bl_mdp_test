using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using WTW.MdpService.Infrastructure.CasesApi;
using WTW.Web.LanguageExt;
using WTW.Web.Serialization;

namespace WTW.MdpService.Test.Infrastructure.Cases;

public class CasesClientTest
{
    private readonly CasesClient _sut;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<ILogger<CasesClient>> _loggerMock;

    public CasesClientTest()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _loggerMock = new Mock<ILogger<CasesClient>>();

        _httpClientFactoryMock
            .Setup(x => x.CreateClient("CasesApi"))
            .Returns(new HttpClient(_httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri("https://casework-api-st01a-dev.awstas.net")
            });

        _sut = new CasesClient(_httpClientFactoryMock.Object.CreateClient("CasesApi"), _loggerMock.Object);
    }

    public async Task GetCaseList_ReturnsCasesResponse()
    {
        var businessGroup = "LIF";
        var referenceNumber = "0240707";

        var response = new HttpResponseMessage
        {
            Content = new StringContent(CasesTestData.CaseListResponseJson),
            StatusCode = HttpStatusCode.OK
        };

        var responseModel = JsonSerializer.Deserialize<IEnumerable<CasesResponse>>(CasesTestData.CaseListResponseJson, SerialiationBuilder.Options());

        _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
                               "SendAsync",
                               ItExpr.IsAny<HttpRequestMessage>(),
                               ItExpr.IsAny<CancellationToken>())
                 .ReturnsAsync(response);

        var result = await _sut.GetCaseList(businessGroup, referenceNumber);

        result.Right().Should().BeEquivalentTo(responseModel);
    }

    public async Task GetCaseList_ReturnsCasesErrorResponse_When_NoCasesFound()
    {
        var businessGroup = "LIF";
        var referenceNumber = "0240707";

        var response = new HttpResponseMessage
        {
            Content = new StringContent(CasesTestData.CaseListErrorResponseJson),
            StatusCode = HttpStatusCode.NotFound
        };

        var responseModel = JsonSerializer.Deserialize<CasesErrorResponse>(CasesTestData.CaseListErrorResponseJson, SerialiationBuilder.Options());

        _httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
                               "SendAsync",
                               ItExpr.IsAny<HttpRequestMessage>(),
                               ItExpr.IsAny<CancellationToken>())
                 .ReturnsAsync(response);

        var result = await _sut.GetCaseList(businessGroup, referenceNumber);

        result.Left().Should().BeEquivalentTo(responseModel);
    }

    public async Task RetirementOrTransferCasesExists_ReturnsTrue_When_CasesFound()
    {
        var businessGroup = "LIF";
        var referenceNumber = "0240707";

        var response = new HttpResponseMessage
        {
            Content = new StringContent(CasesTestData.CaseListResponseJson),
            StatusCode = HttpStatusCode.OK
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                 "SendAsync",
                 ItExpr.IsAny<HttpRequestMessage>(),
                 ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var result = await _sut.GetRetirementOrTransferCases(businessGroup, referenceNumber);

        result.IsSome.Should().BeTrue();
    }

    public async Task RetirementOrTransferCasesExists_ReturnsFalse_When_CasesNotFound()
    {
        var businessGroup = "LIF";
        var referenceNumber = "0240707";

        var response = new HttpResponseMessage
        {
            Content = new StringContent(CasesTestData.CaseListErrorResponseJson),
            StatusCode = HttpStatusCode.NotFound
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                 "SendAsync",
                 ItExpr.IsAny<HttpRequestMessage>(),
                 ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var result = await _sut.GetRetirementOrTransferCases(businessGroup, referenceNumber);

        result.IsSome.Should().BeFalse();
    }

    public async Task RetirementOrTransferCasesExists_ReturnsFalse_When_ExceptionThrown()
    {
        var businessGroup = "LIF";
        var referenceNumber = "0240707";

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                 "SendAsync",
                 ItExpr.IsAny<HttpRequestMessage>(),
                 ItExpr.IsAny<CancellationToken>())
            .Throws(new ArgumentException());

        var result = await _sut.GetRetirementOrTransferCases(businessGroup, referenceNumber);

        result.IsSome.Should().BeFalse();
    }

    public async Task CreateForNonMember_WhenApiReturnsSuccessfulResponse()
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri.ToString().Contains("create/non-member-case")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new CreateCaseResponse { BusinessGroup = "RBS", CaseNumber = "123456", BatchNumber = 123, Error = null }))
            });

        var result = await _sut.CreateForNonMember(It.IsAny<CreateCaseRequest>());

        result.IsRight.Should().BeTrue();
        result.Right().BusinessGroup.Should().Be("RBS");
        result.Right().CaseNumber.Should().Be("123456");
        result.Right().BatchNumber.Should().Be(123);
        result.Right().Error.Should().BeNull();
    }

    public async Task CreateForNonMember_WhenApiReturnsErrorResponse()
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri.ToString().Contains("create/non-member-case")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent(JsonSerializer.Serialize(
                    new CreateCaseError { Errors = new CreateCaseError.CreateCaseInnerError { Message = "test-inner-message" }, Message = "test-message" }))
            });

        var result = await _sut.CreateForNonMember(It.IsAny<CreateCaseRequest>());

        result.IsRight.Should().BeFalse();
        result.Left().Errors.Message.Should().Be("test-inner-message");
        result.Left().Message.Should().Be("test-message");
    }

    public async Task CreateForMember_WhenApiReturnsSuccessfulResponse()
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri.ToString().Contains("create/member-case")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new CreateCaseResponse { BusinessGroup = "RBS", CaseNumber = "123456", BatchNumber = 123, Error = null }))
            });

        var result = await _sut.CreateForMember(It.IsAny<CreateCaseRequest>());

        result.IsRight.Should().BeTrue();
        result.Right().BusinessGroup.Should().Be("RBS");
        result.Right().CaseNumber.Should().Be("123456");
        result.Right().BatchNumber.Should().Be(123);
        result.Right().Error.Should().BeNull();
    }

    public async Task CreateForMember_WhenApiReturnsErrorResponse()
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri.ToString().Contains("create/member-case")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent(JsonSerializer.Serialize(
                    new CreateCaseError { Errors = new CreateCaseError.CreateCaseInnerError { Message = "test-inner-message" }, Message = "test-message" }))
            });

        var result = await _sut.CreateForMember(It.IsAny<CreateCaseRequest>());

        result.IsRight.Should().BeFalse();
        result.Left().Errors.Message.Should().Be("test-inner-message");
        result.Left().Message.Should().Be("test-message");
    }

    public async Task Exists_WhenApiReturnsSuccessfulResponse()
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri.ToString().Contains("case-exists")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"caseExists\":true}")
            });

        var result = await _sut.Exists(It.IsAny<string>(), It.IsAny<string>());

        result.IsRight.Should().BeTrue();
        result.Right().CaseExists.Should().BeTrue();
    }

    public async Task Exists_WhenApiReturnsErrorResponse()
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri.ToString().Contains("case-exists")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent(JsonSerializer.Serialize(
                    new CreateCaseError { Errors = new CreateCaseError.CreateCaseInnerError { Message = "test-inner-message" }, Message = "test-message" }))
            });

        var result = await _sut.Exists(It.IsAny<string>(), It.IsAny<string>());

        result.IsRight.Should().BeFalse();
        result.Left().Errors.Message.Should().Be("test-inner-message");
        result.Left().Message.Should().Be("test-message");
    }

    public async Task ListDocuments_WhenApiReturnsSuccessfulResponse()
    {
        var date = DateTimeOffset.UtcNow;
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri.ToString().Contains("/document")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new DocumentsResponse
                {
                    BusinessGroup = "RBS",
                    ReferenceNumber = "1234567",
                    CaseNumber = "111111",
                    CaseCode = "222222",
                    Documents = new List<DocumentResponse>
                    {
                        new DocumentResponse
                        {
                            DocId="doc-id-test",
                            ImageId=123,
                            Narrative="narrative-test",
                            DateReceived=date,
                            Status="status-test",
                            Notes="note-test",
                        }
                    },
                }))
            });

        var result = await _sut.ListDocuments(It.IsAny<string>(), It.IsAny<string>());

        result.IsRight.Should().BeTrue();
        result.Right().BusinessGroup.Should().Be("RBS");
        result.Right().ReferenceNumber.Should().Be("1234567");
        result.Right().CaseNumber.Should().Be("111111");
        result.Right().CaseCode.Should().Be("222222");
        result.Right().Documents[0].DocId.Should().Be("doc-id-test");
        result.Right().Documents[0].ImageId.Should().Be(123);
        result.Right().Documents[0].Narrative.Should().Be("narrative-test");
        result.Right().Documents[0].DateReceived.Should().Be(date);
        result.Right().Documents[0].Status.Should().Be("status-test");
        result.Right().Documents[0].Notes.Should().Be("note-test");
    }

    public async Task ListDocuments_WhenApiReturnsErrorResponse()
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri.ToString().Contains("/document")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent(JsonSerializer.Serialize(new DocumentsErrorResponse { Message = "test-message", Error = "test-error", Detail = "test-detail" }))
            });

        var result = await _sut.ListDocuments(It.IsAny<string>(), It.IsAny<string>());

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("test-message");
        result.Left().Error.Should().Be("test-error");
        result.Left().Detail.Should().Be("test-detail");
    }
}