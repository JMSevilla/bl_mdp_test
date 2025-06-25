using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using WTW.MdpService.Templates;

namespace WTW.MdpService.Test.Templates;

public class TemplateServiceTest
{
    public async Task DownloadTemplates_ShouldReturnAll_WhenAllTemplatesDownloadedSuccessfully()
    {
        var mockTemplateProvider = new Mock<ITemplateProvider>();

        mockTemplateProvider
            .Setup(tp => tp.GetTemplate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationTokenSource>()))
            .ReturnsAsync(new byte[] { 0, 1, 2, 3, 4 });

        var sut = new TemplateService(mockTemplateProvider.Object);

        var result = await sut.DownloadTemplates("testAccessKey");

        result.Should().HaveCount(50);
    }

    public async Task DownloadTemplates_ShouldReturnNone_WhenNoTemplatesFound()
    {
        var mockTemplateProvider = new Mock<ITemplateProvider>();

        mockTemplateProvider
            .Setup(tp => tp.GetTemplate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationTokenSource>()))
            .ReturnsAsync((byte[])null);

        var sut = new TemplateService(mockTemplateProvider.Object);

        var result = await sut.DownloadTemplates("testAccessKey");

        result.Should().BeEmpty();
    }
}