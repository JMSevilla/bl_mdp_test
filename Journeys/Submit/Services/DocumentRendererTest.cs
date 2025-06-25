using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Domain.Common.Journeys;
using WTW.MdpService.Domain.Mdp.Calculations;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.Infrastructure.Content;
using WTW.MdpService.Infrastructure.MdpDb;
using WTW.MdpService.Infrastructure.MemberDb;
using WTW.MdpService.Infrastructure.PdfGenerator;
using WTW.MdpService.Infrastructure.Templates.Common;
using WTW.MdpService.Infrastructure.Templates.Common.CmsContentModels;
using WTW.MdpService.Infrastructure.Templates.GenericJourneys;
using WTW.MdpService.Journeys.Submit.Services;
using WTW.MdpService.Journeys.Submit.Services.DocumentsRenderer;
using WTW.MdpService.Retirement;
using WTW.MdpService.Test.Domain.Mdp.Calculations;
using WTW.MdpService.Test.Domain.Mdp.Journeys;
using WTW.MdpService.Test.Domain.Members;

namespace WTW.MdpService.Test.Journeys.Submit.Services;

public class DocumentRendererTest
{
    private const string DataSummaryBlocksJson = @"{""elements"":{""dataSourceUrl"":{""elementType"":""text"",""value"":""https://apim.awstas.net/mdp-api/api/journeys/requestquote/data""},""key"":{""elementType"":""text"",""value"":""retirement_quote_data_summary""},""summaryBlocks"":{""values"":[{""elements"":{""header"":{""elementType"":""text"",""value"":""Personal details""},""highlightedBackground"":{""elementType"":""toggle"",""value"":false},""summaryItems"":{""values"":[{""elements"":{""callToAction"":{""elementType"":""reference""},""description"":{""elementType"":""text""},""divider"":{""elementType"":""toggle"",""value"":false},""explanationSummaryItems"":{""elementType"":""reference""},""format"":{""elementType"":""optionselection"",""value"":{""label"":""Text"",""selection"":""Text""}},""header"":{""elementType"":""text"",""value"":""Name""},""link"":{""elementType"":""text"",""value"":""""},""linkText"":{""elementType"":""text"",""value"":""""},""tooltip"":{""elementType"":""reference""},""value"":{""elementType"":""text"",""value"":""fullName""}},""type"":""Summary item""},{""elements"":{""callToAction"":{""elementType"":""reference""},""description"":{""elementType"":""text"",""value"":""""},""divider"":{""elementType"":""toggle"",""value"":false},""explanationSummaryItems"":{""elementType"":""reference""},""format"":{""elementType"":""optionselection"",""value"":{""label"":""Date"",""selection"":""Date""}},""header"":{""elementType"":""text"",""value"":""Date of birth""},""link"":{""elementType"":""text""},""linkText"":{""elementType"":""text""},""tooltip"":{""elementType"":""reference""},""value"":{""elementType"":""text"",""value"":""dateOfBirth""}},""type"":""Summary item""},{""elements"":{""callToAction"":{""elementType"":""reference""},""description"":{""elementType"":""text""},""divider"":{""elementType"":""toggle"",""value"":false},""explanationSummaryItems"":{""elementType"":""reference""},""format"":{""elementType"":""optionselection"",""value"":{""label"":""Text"",""selection"":""Text""}},""header"":{""elementType"":""text"",""value"":""Address""},""link"":{""elementType"":""text""},""linkText"":{""elementType"":""text""},""tooltip"":{""elementType"":""reference""},""value"":{""elementType"":""text"",""value"":""address.lines""}},""type"":""Summary item""},{""elements"":{""callToAction"":{""elementType"":""reference""},""description"":{""elementType"":""text""},""divider"":{""elementType"":""toggle"",""value"":false},""explanationSummaryItems"":{""elementType"":""reference""},""format"":{""elementType"":""optionselection"",""value"":{""label"":""Text"",""selection"":""Text""}},""header"":{""elementType"":""text"",""value"":""Post code""},""link"":{""elementType"":""text""},""linkText"":{""elementType"":""text""},""tooltip"":{""elementType"":""reference""},""value"":{""elementType"":""text"",""value"":""address.postCode""}},""type"":""Summary item""},{""elements"":{""callToAction"":{""elementType"":""reference""},""description"":{""elementType"":""text""},""divider"":{""elementType"":""toggle"",""value"":false},""explanationSummaryItems"":{""elementType"":""reference""},""format"":{""elementType"":""optionselection"",""value"":{""label"":""Text"",""selection"":""Text""}},""header"":{""elementType"":""text"",""value"":""Country""},""link"":{""elementType"":""text""},""linkText"":{""elementType"":""text""},""tooltip"":{""elementType"":""reference""},""value"":{""elementType"":""text"",""value"":""address.country""}},""type"":""Summary item""},{""elements"":{""callToAction"":{""elementType"":""reference""},""description"":{""elementType"":""text""},""divider"":{""elementType"":""toggle"",""value"":false},""explanationSummaryItems"":{""elementType"":""reference""},""format"":{""elementType"":""optionselection"",""value"":{""label"":""Text"",""selection"":""Text""}},""header"":{""elementType"":""text"",""value"":""Email address""},""link"":{""elementType"":""text"",""value"":""""},""linkText"":{""elementType"":""text""},""tooltip"":{""elementType"":""reference""},""value"":{""elementType"":""text"",""value"":""emailAddress""}},""type"":""Summary item""},{""elements"":{""callToAction"":{""elementType"":""reference""},""description"":{""elementType"":""text""},""divider"":{""elementType"":""toggle"",""value"":false},""explanationSummaryItems"":{""elementType"":""reference""},""format"":{""elementType"":""optionselection"",""value"":{""label"":""Text"",""selection"":""Text""}},""header"":{""elementType"":""text"",""value"":""Mobile phone number""},""link"":{""elementType"":""text""},""linkText"":{""elementType"":""text""},""tooltip"":{""elementType"":""reference""},""value"":{""elementType"":""text"",""value"":""phone""}},""type"":""Summary item""}]}},""type"":""Summary block""}]}},""type"":""Data summary""}";

    private readonly Mock<IContentClient> _contentClientMock;
    private readonly Mock<IPdfGenerator> _pdfGeneratorMock;
    private readonly Mock<IMemberRepository> _memberRepositoryMock;
    private readonly Mock<IGenericJourneysTemplate> _genericJourneysTemplateMock;
    private readonly Mock<ILogger<DocumentRenderer>> _loggerMock;
    private readonly Mock<ICalculationsRepository> _calculationsRepositoryMock;
    private readonly Mock<ITemplateDataService> _templateDataServiceMock;
    private readonly Mock<ICmsDataParser> _cmsDataParserMock;
    private readonly Mock<IJourneysRepository> _journeysRepositoryMock;
    private readonly MemoryStream _testStream;
    private readonly DocumentRenderer _sut;

    public DocumentRendererTest()
    {
        _contentClientMock = new Mock<IContentClient>();
        _pdfGeneratorMock = new Mock<IPdfGenerator>();
        _memberRepositoryMock = new Mock<IMemberRepository>();
        _journeysRepositoryMock = new Mock<IJourneysRepository>();
        _genericJourneysTemplateMock = new Mock<IGenericJourneysTemplate>();
        _loggerMock = new Mock<ILogger<DocumentRenderer>>();
        _calculationsRepositoryMock = new Mock<ICalculationsRepository>();
        _templateDataServiceMock = new Mock<ITemplateDataService>();
        _cmsDataParserMock = new Mock<ICmsDataParser>();
        _testStream = new MemoryStream(Encoding.UTF8.GetBytes("This is a test PDF stream."));
        _sut = new DocumentRenderer(
            _contentClientMock.Object,
            _pdfGeneratorMock.Object,
            _memberRepositoryMock.Object,
            _genericJourneysTemplateMock.Object,
            _loggerMock.Object,
            _calculationsRepositoryMock.Object,
            _templateDataServiceMock.Object,
            _cmsDataParserMock.Object,
            _journeysRepositoryMock.Object
           );
    }

    public async Task RendersQuoteRequestSummaryPdf_ForRetirement()
    {
        SetUpMocks();

        var result = await _sut.RenderGenericSummaryPdf(GetData(), It.IsAny<string>(), It.IsAny<string>());

        result.FileName.Should().Be("request_retirement_quote_pdf.pdf");
        result.PdfStream.Should().BeSameAs(_testStream);
    }

    public async Task RendersDBSummaryPdf_ForRetirement()
    {
        SetUpMocks();

        var journey = new RetirementJourneyBuilder().Build().SetCalculation(new CalculationBuilder().BuildV2());
        journey.Submit(new byte[2], DateTimeOffset.UtcNow, "1");

        var result = await _sut.RenderDirectPdf(GetData("dbretirementapplication", "request_retirement_quote_pdf"), It.IsAny<string>(), It.IsAny<string>());

        result.FileName.Should().Be("request_retirement_quote_pdf.pdf");
        result.PdfStream.Should().BeSameAs(_testStream);
    }

    public async Task RendersGenericDirectPdf_FileNameFromTemplateMetaData()
    {
        SetUpMocks();

        var journey = new RetirementJourneyBuilder().Build().SetCalculation(new CalculationBuilder().BuildV2());
        journey.Submit(new byte[2], DateTimeOffset.UtcNow, "1");

        _genericJourneysTemplateMock
            .Setup(x => x.RenderHtml(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync("<head><meta name=\"filename\" content=\"filename_test.pdf\" /></head><div></div>");

        var result = await _sut.RenderDirectPdf(GetData("any-journey-type", "random_template_key_pdf"), It.IsAny<string>(), It.IsAny<string>());

        result.FileName.Should().Be("filename_test.pdf");
        result.PdfStream.Should().BeSameAs(_testStream);
    }

    public async Task RendersGenericDirectPdf_WhenDcPensionPotSummaryTemplate()
    {
        SetUpMocks();

        var calculation = new CalculationBuilder().BuildV2();
        _calculationsRepositoryMock.Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(calculation);

        var result = await _sut.RenderDirectPdf(GetData("any-journey-type", "dc_pension_pot_summary_pdf"), It.IsAny<string>(), It.IsAny<string>());

        result.FileName.Should().Be("dc_pension_pot_summary_pdf.pdf");
        result.PdfStream.Should().BeSameAs(_testStream);

        _contentClientMock.Verify(x => x.FindTemplate("dc_pension_pot_summary_pdf", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _memberRepositoryMock.Verify(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null), Times.Once);
        _calculationsRepositoryMock.Verify(x => x.Find(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _templateDataServiceMock.Verify(x => x.GetCmsTokensResponseData(It.IsAny<Member>(), calculation), Times.Once);
        _templateDataServiceMock.Verify(x => x.GetGenericDataSummaryBlocks(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<(string, string, string)>()), Times.Once);
        _templateDataServiceMock.Verify(x => x.GetGenericContentBlockItems(It.IsAny<TemplateResponse>(), It.IsAny<string>(), It.IsAny<(string, string, string)>()), Times.Once);
        _templateDataServiceMock.Verify(x => x.GetOptionListItems(It.IsAny<Calculation>(), It.IsAny<string>()), Times.Once);
        _templateDataServiceMock.Verify(x => x.GetOptionSummaryDataSummaryBlocks(It.IsAny<Calculation>(), It.IsAny<string>()), Times.Never);
        _genericJourneysTemplateMock.Verify(x => x.RenderHtml(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        _pdfGeneratorMock.Verify(x => x.Generate(It.IsAny<string>(), "<div></div>", "<div></div>"), Times.Once);
    }

    public async Task RendersGenericDirectPdf_WhenOptionSummaryTemplate()
    {
        SetUpMocks();

        var calculation = new CalculationBuilder().BuildV2();
        _calculationsRepositoryMock.Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(calculation);

        var result = await _sut.RenderDirectPdf(GetData("any-journey-type", "option_summary_pdf"), It.IsAny<string>(), It.IsAny<string>());

        result.FileName.Should().Be("option_summary_pdf.pdf");
        result.PdfStream.Should().BeSameAs(_testStream);

        _contentClientMock.Verify(x => x.FindTemplate("option_summary_pdf", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _memberRepositoryMock.Verify(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null), Times.Once);
        _calculationsRepositoryMock.Verify(x => x.Find(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _templateDataServiceMock.Verify(x => x.GetCmsTokensResponseData(It.IsAny<Member>(), calculation), Times.Once);
        _templateDataServiceMock.Verify(x => x.GetGenericDataSummaryBlocks(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<(string, string, string)>()), Times.Once);
        _templateDataServiceMock.Verify(x => x.GetGenericContentBlockItems(It.IsAny<TemplateResponse>(), It.IsAny<string>(), It.IsAny<(string, string, string)>()), Times.Once);
        _templateDataServiceMock.Verify(x => x.GetOptionListItems(It.IsAny<Calculation>(), It.IsAny<string>()), Times.Never);
        _templateDataServiceMock.Verify(x => x.GetOptionSummaryDataSummaryBlocks(It.IsAny<Calculation>(), It.IsAny<string>()), Times.Once);
        _genericJourneysTemplateMock.Verify(x => x.RenderHtml(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        _pdfGeneratorMock.Verify(x => x.Generate(It.IsAny<string>(), "<div></div>", "<div></div>"), Times.Once);
    }

    public async Task RendersGenericDirectPdf_NonExceptionTemplateGiven()
    {
        SetUpMocks();

        var calculation = new CalculationBuilder().BuildV2();
        _calculationsRepositoryMock.Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(calculation);

        var result = await _sut.RenderDirectPdf(GetData("any-journey-type", "not_exception_summary_pdf"), It.IsAny<string>(), It.IsAny<string>());

        result.FileName.Should().Be("not_exception_summary_pdf.pdf");
        result.PdfStream.Should().BeSameAs(_testStream);

        _contentClientMock.Verify(x => x.FindTemplate("not_exception_summary_pdf", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _memberRepositoryMock.Verify(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null), Times.Once);
        _calculationsRepositoryMock.Verify(x => x.Find(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _templateDataServiceMock.Verify(x => x.GetCmsTokensResponseData(It.IsAny<Member>(), calculation), Times.Once);
        _templateDataServiceMock.Verify(x => x.GetGenericDataSummaryBlocks(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<(string, string, string)>()), Times.Once);
        _templateDataServiceMock.Verify(x => x.GetGenericContentBlockItems(It.IsAny<TemplateResponse>(), It.IsAny<string>(), It.IsAny<(string, string, string)>()), Times.Once);
        _templateDataServiceMock.Verify(x => x.GetOptionListItems(It.IsAny<Calculation>(), It.IsAny<string>()), Times.Never);
        _templateDataServiceMock.Verify(x => x.GetOptionSummaryDataSummaryBlocks(It.IsAny<Calculation>(), It.IsAny<string>()), Times.Never);
        _genericJourneysTemplateMock.Verify(x => x.RenderHtml(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        _pdfGeneratorMock.Verify(x => x.Generate(It.IsAny<string>(), "<div></div>", "<div></div>"), Times.Once);
    }

    public async Task RendersGenericJourneySummaryEmail()
    {
        _genericJourneysTemplateMock
           .Setup(x => x.RenderHtml(It.IsAny<string>(), It.IsAny<GenericJourney>(), It.IsAny<Member>(), It.IsAny<DateTimeOffset>(), It.IsAny<string>(), It.IsAny<IEnumerable<SummaryBlock>>(), It.IsAny<IEnumerable<ContentBlockItem>>()))
           .ReturnsAsync("<div></div>");

        SetUpMocks();

        var journey = new GenericJourney("RBS", "1234567", "transfer", "step1", "step2", true, "Started", DateTimeOffset.UtcNow);

        _journeysRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(journey);

        var result = await _sut.RenderGenericJourneySummaryEmail(GetData(), It.IsAny<string>(), It.IsAny<string>());

        result.EmailHtmlBody.Should().Be("<div></div>");
        result.EmailSubject.Should().Be("test-subject");
        result.EmailFrom.Should().Be("test@wtw.com");
        result.EmailTo.Should().Be("test@gmail.com");
    }

    private static DocumentsRendererData GetData()
    {
        return new DocumentsRendererData(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), "request_retirement_quote_pdf", "quote_request_submission_email", It.IsAny<string>(), It.IsAny<string>());
    }

    private static DocumentsRendererData GetData(string journeyType, string templateKey)
    {
        return new DocumentsRendererData(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), templateKey, "quote_request_submission_email", It.IsAny<string>(), journeyType);
    }

    private void SetUpMocks()
    {
        _contentClientMock
                    .Setup(x => x.FindTemplate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(new TemplateResponse { HtmlBody = "<div></div>", HtmlHeader = "<div></div>", HtmlFooter = "<div></div>", EmailSubject = "test-subject", EmailFrom = "test@wtw.com" });
        _contentClientMock
            .Setup(x => x.FindDataSummaryBlocks(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(JsonSerializer.Deserialize<JsonElement>(DataSummaryBlocksJson));
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(new MemberBuilder().Build());
        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new CalculationBuilder().BuildV2());
        _templateDataServiceMock
            .Setup(x => x.GetCmsTokensResponseData(It.IsAny<Member>(), It.IsAny<Option<Calculation>>()))
            .Returns(new CmsTokenInformationResponse());
        _templateDataServiceMock
            .Setup(x => x.GetGenericContentBlockItems(It.IsAny<TemplateResponse>(), It.IsAny<string>(), It.IsAny<(string, string, string)>()))
            .ReturnsAsync(new List<ContentBlockItem>());
        _genericJourneysTemplateMock
            .Setup(x => x.RenderHtml(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync("<div></div>");
        _pdfGeneratorMock
            .Setup(x => x.Generate(It.IsAny<string>(), It.IsAny<Option<string>>(), It.IsAny<Option<string>>()))
            .ReturnsAsync(_testStream);
    }
}