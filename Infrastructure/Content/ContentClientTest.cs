using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using WTW.MdpService.Infrastructure.Content;
using WTW.MdpService.Templates;
using WTW.TestCommon.FixieConfig;
using System.Collections.Generic;

namespace WTW.MdpService.Test.Infrastructure.Content;

public class ContentClientTest
{
    private readonly HttpClient _httpClient;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly Mock<ILogger<ContentClient>> _contentLoggerMock;
    private readonly ContentClient _sut;
    private readonly string _baseAddress = "http://example.com/";
    public ContentClientTest()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri(_baseAddress)
        };
        _contentLoggerMock = new Mock<ILogger<ContentClient>>();
        _sut = new ContentClient(_httpClient, _contentLoggerMock.Object);
    }

    private void SetupMockHttpMessageHandler(string responseContent, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseContent)
            });
    }

    public async Task FindContentBlocks_ReturnsEmtyList_WhenHttpRequestExceptionIsThrown()
    {
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                           "SendAsync",
                           ItExpr.IsAny<HttpRequestMessage>(),
                           ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("message"));

        var result = await _sut.FindContentBlocks(new string[] { "key1", "key2" }, "contentAccessKey");

        result.Should().BeEmpty();
    }

    public async Task FindContentBlocks_ReturnsContentBlocks()
    {
        SetupMockHttpMessageHandler(ContentTestData.ContentBlockList1);

        var result = (await _sut.FindContentBlocks(new string[] { "key1", "key2" }, "contentAccessKey")).ToList();

        result[0].ToString().Should().BeEquivalentTo(ContentTestData.ContentBlockList1);
    }

    public async Task FindDataSummaryBlocks_ReturnsDataSummaryBlocks()
    {
        SetupMockHttpMessageHandler(ContentTestData.SummaryItemsList);

        var result = (await _sut.FindDataSummaryBlocks(new string[] { "key1", "key2" }, "contentAccessKey")).ToList();

        result[0].Key.Should().Be("key1");
        result[0].DataSummaryJsonElement.ToString().Should().NotBeNull();
    }

    [Input(HttpStatusCode.OK, true)]
    [Input(HttpStatusCode.NotFound, false)]
    public async Task FindDataSummaryBlocksReturnsExpectedResult(HttpStatusCode httpStatusCode, bool expectedResult)
    {
        var response = new HttpResponseMessage
        {
            Content = new StringContent("{}"),
            StatusCode = httpStatusCode
        };

        _mockHttpMessageHandler.Protected().Setup<Task<HttpResponseMessage>>(
                               "SendAsync",
                               ItExpr.IsAny<HttpRequestMessage>(),
                               ItExpr.IsAny<CancellationToken>())
                 .ReturnsAsync(response);

        var result = await _sut.FindDataSummaryBlocks(It.IsAny<string>(), It.IsAny<string>());

        result.IsSome.Should().Be(expectedResult);
    }

    public async Task FindDataSummaryBlocksThrowsWhenNot404Or200StatusCodes()
    {
        var response = new HttpResponseMessage
        {
            Content = new StringContent("{}"),
            StatusCode = HttpStatusCode.BadRequest
        };

        _mockHttpMessageHandler.Protected().Setup<Task<HttpResponseMessage>>(
                               "SendAsync",
                               ItExpr.IsAny<HttpRequestMessage>(),
                               ItExpr.IsAny<CancellationToken>())
                 .ReturnsAsync(response);

        var action = async () => await _sut.FindDataSummaryBlocks(It.IsAny<string>(), It.IsAny<string>());

        await action.Should().ThrowAsync<HttpRequestException>();
    }

    public async Task FindsTemplate()
    {
        _mockHttpMessageHandler.Protected().Setup<Task<HttpResponseMessage>>(
                               "SendAsync",
                               ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri.ToString().Contains("/api/templates/test-template-name")),
                               ItExpr.IsAny<CancellationToken>())
                 .ReturnsAsync(new HttpResponseMessage
                 {
                     Content = new StringContent(JsonSerializer.Serialize(new TemplateResponse
                     {
                         TemplateName = "test-template-name",
                         HtmlBody = "<p>test-body</p>",
                         HtmlHeader = "<p>test-header</p>",
                         HtmlFooter = "<p>test-footer</p>",
                         EmailSubject = "Hello!",
                         EmailFrom = "hello@wtw.com",
                         ContentBlockKeys = "test-block-key",
                     })),
                     StatusCode = HttpStatusCode.OK
                 });

        var result = await _sut.FindTemplate("test-template-name", It.IsAny<string>());

        result.TemplateName.Should().Be("test-template-name");
        result.HtmlBody.Should().Be("<p>test-body</p>");
        result.HtmlHeader.Should().Be("<p>test-header</p>");
        result.HtmlFooter.Should().Be("<p>test-footer</p>");
        result.EmailSubject.Should().Be("Hello!");
        result.EmailFrom.Should().Be("hello@wtw.com");
        result.ContentBlockKeys.Should().Be("test-block-key");
    }

    public async Task FindsUnauthorizedTemplate()
    {
        _mockHttpMessageHandler.Protected().Setup<Task<HttpResponseMessage>>(
                               "SendAsync",
                               ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri.ToString().Contains("unauthorized?tenantUrl=natwest.wtwco.com")),
                               ItExpr.IsAny<CancellationToken>())
                 .ReturnsAsync(new HttpResponseMessage
                 {
                     Content = new StringContent(JsonSerializer.Serialize(new TemplateResponse
                     {
                         TemplateName = "test-template-name",
                         HtmlBody = "<p>test-body</p>",
                         HtmlHeader = "<p>test-header</p>",
                         HtmlFooter = "<p>test-footer</p>",
                         EmailSubject = "Hello!",
                         EmailFrom = "hello@wtw.com",
                         ContentBlockKeys = "test-block-key",
                     })),
                     StatusCode = HttpStatusCode.OK
                 });

        var result = await _sut.FindUnauthorizedTemplate(It.IsAny<string>(), "natwest.wtwco.com");

        result.TemplateName.Should().Be("test-template-name");
        result.HtmlBody.Should().Be("<p>test-body</p>");
        result.HtmlHeader.Should().Be("<p>test-header</p>");
        result.HtmlFooter.Should().Be("<p>test-footer</p>");
        result.EmailSubject.Should().Be("Hello!");
        result.EmailFrom.Should().Be("hello@wtw.com");
        result.ContentBlockKeys.Should().Be("test-block-key");
    }

    public async Task FindsSummaryBlocks()
    {
        _mockHttpMessageHandler.Protected().Setup<Task<HttpResponseMessage>>(
                               "SendAsync",
                               ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri.ToString().Contains("authorized-option-summary")),
                               ItExpr.IsAny<CancellationToken>())
                 .ReturnsAsync(new HttpResponseMessage
                 {
                     Content = new StringContent("{\"testProp\":\"test-value\"}"),
                     StatusCode = HttpStatusCode.OK
                 });

        var result = await _sut.FindSummaryBlocks(It.IsAny<string>(), It.IsAny<string>());

        result.GetProperty("testProp").ToString().Should().Be("test-value");
    }

    public async Task FindsRetirementOptions_WhenSuccessfulResponse()
    {
        _mockHttpMessageHandler.Protected().Setup<Task<HttpResponseMessage>>(
                               "SendAsync",
                               ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri.ToString().Contains("authorized-option-list?contentAccessKey")),
                               ItExpr.IsAny<CancellationToken>())
                 .ReturnsAsync(new HttpResponseMessage
                 {
                     Content = new StringContent("{\"testProp\":\"test-value\"}"),
                     StatusCode = HttpStatusCode.OK
                 });

        var result = await _sut.FindRetirementOptions(It.IsAny<string>());

        result.GetProperty("testProp").ToString().Should().Be("test-value");
    }

    public async Task FindsRetirementOptions_ThrowsNonHttpException()
    {
        _mockHttpMessageHandler.Protected().Setup<Task<HttpResponseMessage>>(
                               "SendAsync",
                               ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri.ToString().Contains("authorized-option-list?contentAccessKey")),
                               ItExpr.IsAny<CancellationToken>())
                 .ReturnsAsync(new HttpResponseMessage
                 {
                     StatusCode = HttpStatusCode.OK
                 });

        var action = async () => await _sut.FindRetirementOptions(It.IsAny<string>());

        await action.Should().ThrowAsync<JsonException>();
        await action.Should().ThrowAsync<Exception>();
    }

    public async Task FindsRetirementOptions_ThrowsHttpException()
    {
        _mockHttpMessageHandler.Protected().Setup<Task<HttpResponseMessage>>(
                               "SendAsync",
                               ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri.ToString().Contains("authorized-option-list?contentAccessKey")),
                               ItExpr.IsAny<CancellationToken>())
                 .ReturnsAsync(new HttpResponseMessage
                 {
                     StatusCode = HttpStatusCode.BadGateway
                 });

        var action = async () => await _sut.FindRetirementOptions(It.IsAny<string>());

        await action.Should().ThrowAsync<HttpRequestException>();
    }

    public async Task FindsRetirementOptionsByQuoteName()
    {
        _mockHttpMessageHandler.Protected().Setup<Task<HttpResponseMessage>>(
                               "SendAsync",
                               ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri.ToString().Contains("authorized-option-list?selectedQuoteName=")),
                               ItExpr.IsAny<CancellationToken>())
                 .ReturnsAsync(new HttpResponseMessage
                 {
                     Content = new StringContent("{\"testProp\":\"test-value\"}"),
                     StatusCode = HttpStatusCode.OK
                 });

        var result = await _sut.FindRetirementOptions(It.IsAny<string>(), It.IsAny<string>());

        result.GetProperty("testProp").ToString().Should().Be("test-value");
    }

    public async Task FindsTenant()
    {
        _mockHttpMessageHandler.Protected().Setup<Task<HttpResponseMessage>>(
                               "SendAsync",
                               ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri.ToString().Contains("content/tenant-content?tenantUrl=natwest.wtwco.com")),
                               ItExpr.IsAny<CancellationToken>())
                 .ReturnsAsync(new HttpResponseMessage
                 {
                     Content = new StringContent("{\"testProp\":\"test-value\"}"),
                     StatusCode = HttpStatusCode.OK
                 });

        var result = await _sut.FindTenant("natwest.wtwco.com");

        result.GetProperty("testProp").ToString().Should().Be("test-value");
    }

    public async Task FindsTemplates()
    {
        var bytes = new byte[5];
        _mockHttpMessageHandler.Protected().Setup<Task<HttpResponseMessage>>(
                               "SendAsync",
                               ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri.ToString().Contains("/api/templates/templateKey?contentAccessKey=contentAccessKey")),
                               ItExpr.IsAny<CancellationToken>())
                 .ReturnsAsync(new HttpResponseMessage
                 {
                     Content = new StringContent(JsonSerializer.Serialize(new TemplatesResponse
                     {
                         Templates = bytes
                     })),
                     StatusCode = HttpStatusCode.OK
                 });

        var result = await _sut.FindTemplates("templateKey", "contentAccessKey");

        result.Templates.Should().BeEquivalentTo(bytes);
    }
}
