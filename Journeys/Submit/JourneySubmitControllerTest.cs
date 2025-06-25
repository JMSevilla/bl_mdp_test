using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.IdentityVerification.Services;
using WTW.MdpService.Infrastructure.CasesApi;
using WTW.MdpService.Infrastructure.EmailConfirmation;
using WTW.MdpService.Infrastructure.IdvService;
using WTW.MdpService.Journeys.Submit;
using WTW.MdpService.Journeys.Submit.Services;
using WTW.TestCommon.FixieConfig;
using WTW.TestCommon.Helpers;
using WTW.Web.Errors;
using static WTW.Web.MdpConstants;

namespace WTW.MdpService.Test.Journeys.Submit;

public class JourneySubmitControllerTest
{
    private const string NonIncomeDrawdownC2STFCLContentAccessKey = "{\"tenantUrl\":\"lifesightdev.assure.wtwco.com\",\"isCalculationSuccessful\":true,\"hasAdditionalContributions\":false,\"schemeType\":\"DC\",\"memberStatus\":\"Deferred\",\"lifeStage\":\"EligibleToRetire\",\"retirementApplicationStatus\":\"EligibleToStart\",\"transferApplicationStatus\":\"Undefined\",\"wordingFlags\":[\"SMALLPOT\",\"TENANTIS2UFPLS\",\"scheme_0002\",\"category_0002\",\"PD-MONTH-DAY\",\"TRD_USED\",\"dcretirementapplication\",\"started\",\"dcretirementapplication-started\",\"DC_Annuity_Consent_q1-DC_Annuity_yes\",\"DC_pw_question-yes_contribution\",\"LTA_0-no\",\"DC_LTA_1-no\",\"DC_LTA_3_A-no\",\"DC_lta_question_ttfac_q-answer_ttfac_4\",\"TFCR_1-no\",\"DC_LTA_2_A-no\",\"DC_LTA_4-no\",\"incomeDrawdownOMTFC\",\"SelectedQuoteName-incomeDrawdownOMTFC\",\"MPAA\"],\"currentAge\":\"61Y1M\",\"dbCalculationStatus\":null,\"dcLifeStage\":\"exceededTRD\",\"isWebChatEnabled\":true}";
    private const string IncomeDrawdownC2STFCLContentAccessKey = "{\"tenantUrl\":\"lifesightdev.assure.wtwco.com\",\"isCalculationSuccessful\":true,\"hasAdditionalContributions\":false,\"schemeType\":\"DC\",\"memberStatus\":\"Deferred\",\"lifeStage\":\"EligibleToRetire\",\"retirementApplicationStatus\":\"EligibleToStart\",\"transferApplicationStatus\":\"Undefined\",\"wordingFlags\":[\"SMALLPOT\",\"TENANTIS2UFPLS\",\"scheme_0002\",\"category_0002\",\"PD-MONTH-DAY\",\"TRD_USED\",\"dcretirementapplication\",\"started\",\"dcretirementapplication-started\",\"DC_Annuity_Consent_q1-DC_Annuity_yes\",\"DC_pw_question-yes_contribution\",\"LTA_0-no\",\"DC_LTA_1-no\",\"DC_LTA_3_A-no\",\"DC_lta_question_ttfac_q-answer_ttfac_4\",\"TFCR_1-no\",\"DC_LTA_2_A-no\",\"DC_LTA_4-no\",\"incomeDrawdownC2STFCL\",\"SelectedQuoteName-incomeDrawdownC2STFCL\",\"MPAA\"],\"currentAge\":\"61Y1M\",\"dbCalculationStatus\":null,\"dcLifeStage\":\"exceededTRD\",\"isWebChatEnabled\":true}";

    private readonly Mock<ICaseRequestFactory> _caseApiRequestModelFactoryMock;
    private readonly Mock<ICaseService> _caseServiceMock;
    private readonly Mock<IDocumentRenderer> _documentRendererMock;
    private readonly Mock<IDocumentsUploaderService> _documentUploaderMock;
    private readonly Mock<IEmailConfirmationSmtpClient> _smtpClientMock;
    private readonly Mock<IGenericJourneyService> _genericJourneyServiceMock;
    private readonly Mock<IDocumentsRendererDataFactory> _documentsRendererDataFactoryMock;
    private readonly Mock<ILogger<JourneySubmitController>> _loggerMock;
    private readonly Mock<IIdvService> _idvServiceMock;
    private readonly JourneySubmitController _sut;

    public JourneySubmitControllerTest()
    {
        _caseApiRequestModelFactoryMock = new Mock<ICaseRequestFactory>();
        _caseServiceMock = new Mock<ICaseService>();
        _documentRendererMock = new Mock<IDocumentRenderer>();
        _documentUploaderMock = new Mock<IDocumentsUploaderService>();
        _smtpClientMock = new Mock<IEmailConfirmationSmtpClient>();
        _genericJourneyServiceMock = new Mock<IGenericJourneyService>();
        _smtpClientMock = new Mock<IEmailConfirmationSmtpClient>();
        _documentsRendererDataFactoryMock = new Mock<IDocumentsRendererDataFactory>();
        _loggerMock = new Mock<ILogger<JourneySubmitController>>();
        _idvServiceMock = new Mock<IIdvService>();

        _sut = new JourneySubmitController(_caseApiRequestModelFactoryMock.Object,
            _caseServiceMock.Object,
            _documentRendererMock.Object,
            _documentUploaderMock.Object,
            _smtpClientMock.Object,
            _genericJourneyServiceMock.Object,
            _documentsRendererDataFactoryMock.Object,
            _loggerMock.Object,
            _idvServiceMock.Object);
        _sut.SetupControllerContext();
    }

    public async Task SubmitQuoteRequestCaseReturns400Code_WhenFailsToCreateCaseApiRequestModel()
    {
        _caseApiRequestModelFactoryMock
            .Setup(x => x.CreateForQuoteRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Error.New("some error"));

        var result = await _sut.SubmitQuoteRequestCase(GetSubmitJourneyCaseRequest());

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    public async Task SubmitQuoteRequestCaseReturns400Code_WhenFailsToCreateCase()
    {
        _caseApiRequestModelFactoryMock
            .Setup(x => x.CreateForQuoteRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(GetCreateCaseRequest());

        _caseServiceMock
            .Setup(x => x.Create(It.IsAny<CreateCaseRequest>()))
            .ReturnsAsync(Error.New("some error"));

        var result = await _sut.SubmitQuoteRequestCase(GetSubmitJourneyCaseRequest());

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    public async Task SubmitQuoteRequestCaseReturns400Code_WhenFailsToUploadQuoteRequestSummary()
    {
        _caseApiRequestModelFactoryMock
            .Setup(x => x.CreateForQuoteRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(GetCreateCaseRequest());

        _caseServiceMock
            .Setup(x => x.Create(It.IsAny<CreateCaseRequest>()))
            .ReturnsAsync("1234567");

        _documentRendererMock
            .Setup(x => x.RenderGenericSummaryPdf(It.IsAny<DocumentsRendererData>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((new MemoryStream(), "test-file-name.pdf"));

        _documentUploaderMock
            .Setup(x => x.UploadQuoteRequestSummary(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>(), It.IsAny<string>()))
            .ReturnsAsync(Error.New("some error"));

        var result = await _sut.SubmitQuoteRequestCase(GetSubmitJourneyCaseRequest());

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    public async Task SubmitQuoteRequestCaseReturns204Code()
    {
        _caseApiRequestModelFactoryMock
            .Setup(x => x.CreateForQuoteRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(GetCreateCaseRequest());

        _caseServiceMock
            .Setup(x => x.Create(It.IsAny<CreateCaseRequest>()))
            .ReturnsAsync("1234567");

        _documentRendererMock
            .Setup(x => x.RenderGenericSummaryPdf(It.IsAny<DocumentsRendererData>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((new MemoryStream(), "test-file-name.pdf"));

        Error? error = null;
        _documentUploaderMock
            .Setup(x => x.UploadQuoteRequestSummary(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>(), It.IsAny<string>()))
            .ReturnsAsync(error);

        _documentRendererMock
            .Setup(x => x.RenderGenericJourneySummaryEmail(It.IsAny<DocumentsRendererData>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(("<div></div>", "test subject", "test@wtw.com", "some-member@wtw.com"));

        var result = await _sut.SubmitQuoteRequestCase(GetSubmitJourneyCaseRequest());

        result.Should().BeOfType<NoContentResult>();
        _smtpClientMock.Verify(x => x.Send(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    public async Task SubmitJourneyReturns404Code_WhenJourneyDoesNotExist()
    {
        _genericJourneyServiceMock.Setup(x => x.ExistsJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);

        var result = await _sut.SubmitJourney(new SubmitDcJourneyRequest { ContentAccessKey = "{}" }, "dbcoreretirementapplication");

        result.Should().BeOfType<NotFoundObjectResult>();
        ((ApiError)(result as NotFoundObjectResult).Value).Errors[0].Message.Should().Be("Journey with type \"dbcoreretirementapplication\" not found.");
    }

    public async Task SubmitJourneyReturns400Code_WhenFailsToCreateCase()
    {
        _genericJourneyServiceMock.Setup(x => x.ExistsJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

        _caseApiRequestModelFactoryMock
            .Setup(x => x.CreateForGenericRetirement(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(It.IsAny<CreateCaseRequest>());

        _caseServiceMock
            .Setup(x => x.Create(It.IsAny<CreateCaseRequest>()))
            .ReturnsAsync(Error.New("some error"));

        var result = await _sut.SubmitJourney(new SubmitDcJourneyRequest { ContentAccessKey = "{}" }, "dbcoreretirementapplication");

        result.Should().BeOfType<BadRequestObjectResult>();
        ((ApiError)(result as BadRequestObjectResult).Value).Errors[0].Message.Should().Be("Failed to create case.");
    }

    public async Task SubmitJourneyReturns400Code_WhenFailsToUploadDcRetirementDocuments()
    {
        _genericJourneyServiceMock.Setup(x => x.ExistsJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

        _caseApiRequestModelFactoryMock
              .Setup(x => x.CreateForGenericRetirement(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
              .Returns(It.IsAny<CreateCaseRequest>());

        _caseServiceMock
            .Setup(x => x.Create(It.IsAny<CreateCaseRequest>()))
            .ReturnsAsync("1234567");

        _documentRendererMock
            .Setup(x => x.RenderGenericSummaryPdf(It.IsAny<DocumentsRendererData>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((new MemoryStream(), "test-file-name.pdf"));

        _documentUploaderMock
            .Setup(x => x.UploadGenericRetirementDocuments(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>(), It.IsAny<string>()))
            .ReturnsAsync(Error.New("some error"));

        var result = await _sut.SubmitJourney(new SubmitDcJourneyRequest { ContentAccessKey = "{}" }, "dbcoreretirementapplication");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Input(IncomeDrawdownC2STFCLContentAccessKey, CaseCodes.TOP9)]
    [Input(NonIncomeDrawdownC2STFCLContentAccessKey, CaseCodes.RTP9)]
    public async Task SubmitJourneyReturns204Code(string accessKey, string expectedCaseCode)
    {
        _genericJourneyServiceMock.Setup(x => x.ExistsJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

        _caseApiRequestModelFactoryMock
            .Setup(x => x.CreateForGenericRetirement(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(It.IsAny<CreateCaseRequest>());

        _caseServiceMock
            .Setup(x => x.Create(It.IsAny<CreateCaseRequest>()))
            .ReturnsAsync("1234567");

        _documentRendererMock
            .Setup(x => x.RenderGenericSummaryPdf(It.IsAny<DocumentsRendererData>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((new MemoryStream(), "test-file-name.pdf"));

        _documentUploaderMock
            .Setup(x => x.UploadGenericRetirementDocuments(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>(), It.IsAny<string>()))
            .ReturnsAsync(123456);

        _documentRendererMock
            .Setup(x => x.RenderGenericJourneySummaryEmail(It.IsAny<DocumentsRendererData>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(("<div></div>", "test subject", "test@wtw.com", "some-member@wtw.com"));

        var result = await _sut.SubmitJourney(new SubmitDcJourneyRequest { ContentAccessKey = accessKey }, "dbcoreretirementapplication");

        result.Should().BeOfType<NoContentResult>();
        _smtpClientMock.Verify(x => x.Send(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _genericJourneyServiceMock.Verify(x => x.SaveSubmissionDetailsToGenericData(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<(string, int)>()), Times.Once);
        _genericJourneyServiceMock.Verify(x => x.SetStatusSubmitted(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

        _caseApiRequestModelFactoryMock.Verify(x => x.CreateForGenericRetirement(It.IsAny<string>(), It.IsAny<string>(), expectedCaseCode), Times.Once);

        _loggerMock.VerifyLogging("Journey Type: dbcoreretirementapplication, BusinessGroup: TestBusinessGroup, ReferenceNumber: TestReferenceNumber", LogLevel.Information, Times.Once());
    }
    public async Task SubmitJourneyLogsWarning_WhenSaveIdentityVerificationFails()
    {
        _genericJourneyServiceMock.Setup(x => x.ExistsJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

        _caseApiRequestModelFactoryMock
            .Setup(x => x.CreateForGenericRetirement(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(It.IsAny<CreateCaseRequest>());

        _caseServiceMock
            .Setup(x => x.Create(It.IsAny<CreateCaseRequest>()))
            .ReturnsAsync("1234567");

        _idvServiceMock
            .Setup(x => x.SaveIdentityVerification(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Prelude.Left<LanguageExt.Common.Error, UpdateIdentityResultResponse>(Error.New("IDV save failed")));

        _documentRendererMock
            .Setup(x => x.RenderGenericSummaryPdf(It.IsAny<DocumentsRendererData>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((new MemoryStream(), "test-file-name.pdf"));

        _documentUploaderMock
            .Setup(x => x.UploadGenericRetirementDocuments(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>(), It.IsAny<string>()))
            .ReturnsAsync(123456);

        var result = await _sut.SubmitJourney(new SubmitDcJourneyRequest { ContentAccessKey = "{}" }, "dbcoreretirementapplication");

        result.Should().BeOfType<NoContentResult>();
        _loggerMock.VerifyLogging("Failed to save IDV results.Error: IDV save failed", LogLevel.Warning, Times.Once());
    }

    public async Task SubmitJourneyLogsError_WhenEmailFailsToSend()
    {
        _genericJourneyServiceMock.Setup(x => x.ExistsJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

        _caseApiRequestModelFactoryMock
            .Setup(x => x.CreateForGenericRetirement(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(It.IsAny<CreateCaseRequest>());

        _caseServiceMock
            .Setup(x => x.Create(It.IsAny<CreateCaseRequest>()))
            .ReturnsAsync("1234567");

        _documentRendererMock
            .Setup(x => x.RenderGenericSummaryPdf(It.IsAny<DocumentsRendererData>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((new MemoryStream(), "test-file-name.pdf"));

        _documentUploaderMock
            .Setup(x => x.UploadGenericRetirementDocuments(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>(), It.IsAny<string>()))
            .ReturnsAsync(123456);

        _documentRendererMock
            .Setup(x => x.RenderGenericJourneySummaryEmail(It.IsAny<DocumentsRendererData>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(("<div></div>", "test subject", "test@wtw.com", "some-member@wtw.com"));

        _smtpClientMock
            .Setup(x => x.Send(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Failed to send email"));

        var result = await _sut.SubmitJourney(new SubmitDcJourneyRequest { ContentAccessKey = "{}" }, "dbcoreretirementapplication");

        result.Should().BeOfType<NoContentResult>();
        _loggerMock.VerifyLogging("Failed to send retirement submit email. Journey type: dbcoreretirementapplication.", LogLevel.Error, Times.Once());
    }

    private static CreateCaseRequest GetCreateCaseRequest() => new CreateCaseRequest
    {
        BusinessGroup = "RBS",
        ReferenceNumber = "1111122",
        CaseCode = "RTP9",
        BatchSource = "MDP",
        BatchDescription = "Case created by an online application",
        Narrative = "",
        Notes = $"Assure Calc fatal at 02/10/2023",
        StickyNotes = $"Assure Calc fatal at 02/10/2023"
    };

    private static SubmitQuoteRequestCaseRequest GetSubmitJourneyCaseRequest() => new SubmitQuoteRequestCaseRequest
    {
        AccessKey = "{}",
        CaseType = "transfer"
    };
}