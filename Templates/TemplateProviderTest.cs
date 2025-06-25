using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Infrastructure.Content;
using WTW.MdpService.Templates;

namespace WTW.MdpService.Test.Templates;

public class TemplateProviderTest
{
    public async Task GetTemplate_ShouldReturnByteArray_WhenTemplateIsFound()
    {
        var mockContentClient = new Mock<IContentClient>();
        var mockLogger = new Mock<ILogger<TemplateProvider>>();

        var expectedTemplate = new TemplatesResponse
        {
            Templates = new byte[] { 0, 1, 2, 3, 4 }
        };
        mockContentClient
            .Setup(cc => cc.FindTemplates(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(expectedTemplate);

        var sut = new TemplateProvider(mockContentClient.Object, mockLogger.Object);

        var result = await sut.GetTemplate("testTemplateName", "testAccessKey", new CancellationTokenSource());

        result.Should().Equal(expectedTemplate.Templates);
    }
    
    public async Task GetTemplate_ShouldReturnNull_WhenTemplateIsNotFound()
    {
        var mockContentClient = new Mock<IContentClient>();
        var mockLogger = new Mock<ILogger<TemplateProvider>>();

        mockContentClient
            .Setup(cc => cc.FindTemplates(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((TemplatesResponse)null);

        var sut = new TemplateProvider(mockContentClient.Object, mockLogger.Object);

        var result = await sut.GetTemplate("testTemplateName", "testAccessKey", new CancellationTokenSource());

        result.Should().BeNull();
    }
    
    public async Task GetTemplate_ShouldReturnNull_WhenExceptionIsThrown()
    {
        var mockContentClient = new Mock<IContentClient>();
        var mockLogger = new Mock<ILogger<TemplateProvider>>();

        mockContentClient
            .Setup(cc => cc.FindTemplates(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception());

        var sut = new TemplateProvider(mockContentClient.Object, mockLogger.Object);

        var result = await sut.GetTemplate("testTemplateName", "testAccessKey", new CancellationTokenSource());

        result.Should().BeNull();
    }
    
    public async Task GetTemplate_ShouldReturnNull_WhenOperationIsCancelled()
    {
        var mockContentClient = new Mock<IContentClient>();
        var mockLogger = new Mock<ILogger<TemplateProvider>>();

        var sut = new TemplateProvider(mockContentClient.Object, mockLogger.Object);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await sut.GetTemplate("testTemplateName", "testAccessKey", cts);

        result.Should().BeNull();
    }
}