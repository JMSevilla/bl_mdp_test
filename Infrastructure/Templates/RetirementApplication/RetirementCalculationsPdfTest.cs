using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using Moq;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.MdpService.Infrastructure.Content;
using WTW.MdpService.Infrastructure.PdfGenerator;
using WTW.MdpService.Infrastructure.Templates.RetirementApplication.Calculations;
using WTW.MdpService.Retirement;
using WTW.MdpService.Test.Domain.Mdp.Calculations;
using WTW.MdpService.Test.Domain.Members;
using WTW.Web.Serialization;

namespace WTW.MdpService.Test.Infrastructure.Templates.RetirementApplication;

public class RetirementCalculationsPdfTest
{
    private readonly Mock<IContentClient> _contentClientMock;
    private readonly Mock<IPdfGenerator> _pdfGeneratorMock;
    private readonly Mock<ICalculationsParser> _calculationsParserMock;
    private readonly Mock<IRetirementApplicationCalculationTemplate> _retirementApplicationCalculationTemplateMock;
    private readonly RetirementCalculationsPdf _sut;

    public RetirementCalculationsPdfTest()
    {
        _contentClientMock = new Mock<IContentClient>();
        _pdfGeneratorMock = new Mock<IPdfGenerator>();
        _calculationsParserMock = new Mock<ICalculationsParser>();
        _retirementApplicationCalculationTemplateMock = new Mock<IRetirementApplicationCalculationTemplate>();
        _sut = new RetirementCalculationsPdf(
                _contentClientMock.Object,
                _pdfGeneratorMock.Object,
                _calculationsParserMock.Object,
                _retirementApplicationCalculationTemplateMock.Object);
    }


    public async Task GenerateSummaryPdf_ShouldReturnMemoryStream()
    {
        var contentAccessKey = "testKey";
        var member = new MemberBuilder().Build();
        var summaryKey = "summaryKey";
        var businessGroup = "businessGroup";
        var auth = "auth";
        var env = "env";
        var template = new TemplateResponse
        {
            HtmlBody = "<html>Body</html>",
            HtmlHeader = "<html>Header</html>",
            HtmlFooter = "<html>Footer</html>"
        };
        var contentKeys = new List<string> { "random_name", "gmp_explanation" };
        var calculation = new CalculationBuilder().BuildV2();
        var memoryStream = new MemoryStream();
        var summaryBlock = JsonSerializer.Deserialize<JsonElement>(TestData.SummaryBlocks, SerialiationBuilder.Options());
        var contentBlocks = JsonSerializer.Deserialize<JsonElement>(TestData.ContentBlocks, SerialiationBuilder.Options());

        _contentClientMock
                .Setup(x => x.FindTemplate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(template);

        _contentClientMock
                .Setup(x => x.FindSummaryBlocks(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(summaryBlock);

        _contentClientMock
                .Setup(x => x.FindContentBlocks(contentKeys, It.IsAny<string>()))
                .ReturnsAsync(new List<JsonElement> { contentBlocks });

        _retirementApplicationCalculationTemplateMock
                .Setup(x => x.Render(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<JsonElement>(), It.IsAny<CmsTokenInformationResponse>(), calculation, member, It.IsAny<IEnumerable<JsonElement>>(), It.IsAny<(string, string, string)>()))
                .ReturnsAsync(template.HtmlBody);

        _pdfGeneratorMock
                .Setup(x => x.Generate(It.IsAny<string>(), It.IsAny<Option<string>>(), It.IsAny<Option<string>>()))
        .ReturnsAsync(memoryStream);

        var result = await _sut.GenerateSummaryPdf(
        contentAccessKey, calculation, member, summaryKey, businessGroup, auth, env);

        result.Should().NotBeNull();
        result.Should().BeOfType<MemoryStream>();

    }

    public async Task GenerateOptionsPdf_ShouldReturnMemoryStream()
    {
        var contentAccessKey = "testKey";
        var member = new MemberBuilder().Build();
        var businessGroup = "businessGroup";
        var auth = "auth";
        var env = "env";
        var template = new TemplateResponse
        {
            HtmlBody = "<html>Body</html>",
            HtmlHeader = "<html>Header</html>",
            HtmlFooter = "<html>Footer</html>"
        };
        var contentKeys = new List<string> { "random_name", "gmp_explanation" };
        var contentBlocks = JsonSerializer.Deserialize<JsonElement>(TestData.ContentBlocks, SerialiationBuilder.Options());
        var calculation = new CalculationBuilder().BuildV2();
        var memoryStream = new MemoryStream();
        var mdpResponseV2 = JsonSerializer.Deserialize<MdpResponseV2>(TestData.RetirementQuotesJsonV2, SerialiationBuilder.Options());

        _contentClientMock
                .Setup(x => x.FindTemplate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(template);

        _contentClientMock
                .Setup(x => x.FindRetirementOptions(It.IsAny<string>()))
                .ReturnsAsync(new JsonElement());

        _calculationsParserMock
                .Setup(x => x.GetQuotesV2(It.IsAny<string>()))
                .Returns(mdpResponseV2);

        _contentClientMock
                .Setup(x => x.FindContentBlocks(contentKeys, It.IsAny<string>()))
                .ReturnsAsync(new List<JsonElement> { contentBlocks });

        _retirementApplicationCalculationTemplateMock
               .Setup(x => x.Render(It.IsAny<string>(), It.IsAny<JsonElement>(), mdpResponseV2.Options, It.IsAny<CmsTokenInformationResponse>(), calculation, It.IsAny<IEnumerable<JsonElement>>(), It.IsAny<(string, string, string)>()))
               .ReturnsAsync(template.HtmlBody);

        _pdfGeneratorMock
                .Setup(x => x.Generate(It.IsAny<string>(), It.IsAny<Option<string>>(), It.IsAny<Option<string>>()))
        .ReturnsAsync(memoryStream);

        var result = await _sut.GenerateOptionsPdf(
            contentAccessKey, calculation, member, businessGroup, auth, env);

        result.Should().NotBeNull();
        result.Should().BeOfType<MemoryStream>();
    }
}
