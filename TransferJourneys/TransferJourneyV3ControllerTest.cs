using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Domain.Common;
using WTW.MdpService.Domain.Mdp;
using WTW.MdpService.Domain.Mdp.Calculations;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.IdentityVerification.Services;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.MdpService.Infrastructure.Content;
using WTW.MdpService.Infrastructure.Edms;
using WTW.MdpService.Infrastructure.EdmsIndexing;
using WTW.MdpService.Infrastructure.EmailConfirmation;
using WTW.MdpService.Infrastructure.IdvService;
using WTW.MdpService.Infrastructure.MdpDb;
using WTW.MdpService.Infrastructure.MemberDb;
using WTW.MdpService.Infrastructure.MemberDb.Documents;
using WTW.MdpService.Infrastructure.PdfGenerator;
using WTW.MdpService.Infrastructure.Templates.TransferApplication;
using WTW.MdpService.Test.Domain.Mdp.Calculations;
using WTW.MdpService.Test.Domain.Mdp.Journeys;
using WTW.MdpService.Test.Domain.Members;
using WTW.MdpService.TransferJourneys;
using WTW.TestCommon.FixieConfig;
using WTW.TestCommon.Helpers;

namespace WTW.MdpService.Test.TransferJourneys;

public class TransferJourneyV3ControllerTest
{
    private readonly TransferJourneyV3Controller _sut;
    private readonly Mock<ITransferJourneyRepository> _transferJourneyRepositoryMock;
    private readonly Mock<IMdpUnitOfWork> _mdpDbUnitOfWorkMock;
    private readonly Mock<ITransferCalculationRepository> _transferCalculationRepositoryMock;
    private readonly Mock<IEdmsClient> _edmsClientMock;
    private readonly Mock<IMemberDbUnitOfWork> _uowMock;
    private readonly Mock<IMemberRepository> _memberRepositoryMock;
    private readonly Mock<IContentClient> _contentClientMock;
    private readonly Mock<IEmailConfirmationSmtpClient> _smtpClientMock;
    private readonly Mock<ITransferCase> _transferCaseMock;
    private readonly Mock<IEdmsDocumentsIndexing> _edmsDocumentsIndexingMock;
    private readonly Mock<ILogger<TransferJourneyV3Controller>> _loggerMock;
    private readonly Mock<ITransferV2Template> _transferV2TemplateMock;
    private readonly Mock<ITransferJourneySubmitEmailTemplate> _transferJourneySubmitEmailTemplateMock;
    private readonly Mock<IJourneyDocumentsRepository> _journeyDocumentsRepositoryMock;
    private readonly Mock<IDocumentsRepository> _documentsRepositoryMock;
    private readonly Mock<IDocumentFactoryProvider> _documentFactoryProviderMock;
    private readonly Mock<IPdfGenerator> _pdfgeneratorMock;
    private readonly Mock<ICalculationsRepository> _calculationsRepositoryMock;
    private readonly Mock<ICalculationsParser> _calculationsParserMock;
    private readonly Mock<IIdvService> _idvServiceMock;

    public TransferJourneyV3ControllerTest()
    {
        _transferJourneyRepositoryMock = new Mock<ITransferJourneyRepository>();
        _mdpDbUnitOfWorkMock = new Mock<IMdpUnitOfWork>();
        _transferCalculationRepositoryMock = new Mock<ITransferCalculationRepository>();
        _edmsClientMock = new Mock<IEdmsClient>();
        _uowMock = new Mock<IMemberDbUnitOfWork>();
        _memberRepositoryMock = new Mock<IMemberRepository>();
        _contentClientMock = new Mock<IContentClient>();
        _smtpClientMock = new Mock<IEmailConfirmationSmtpClient>();
        _transferCaseMock = new Mock<ITransferCase>();
        _edmsDocumentsIndexingMock = new Mock<IEdmsDocumentsIndexing>();
        _loggerMock = new Mock<ILogger<TransferJourneyV3Controller>>();
        _transferV2TemplateMock = new Mock<ITransferV2Template>();
        _transferJourneySubmitEmailTemplateMock = new Mock<ITransferJourneySubmitEmailTemplate>();
        _journeyDocumentsRepositoryMock = new Mock<IJourneyDocumentsRepository>();
        _documentsRepositoryMock = new Mock<IDocumentsRepository>();
        _documentFactoryProviderMock = new Mock<IDocumentFactoryProvider>();
        _pdfgeneratorMock = new Mock<IPdfGenerator>();
        _calculationsRepositoryMock = new Mock<ICalculationsRepository>();
        _calculationsParserMock = new Mock<ICalculationsParser>();
        _idvServiceMock = new Mock<IIdvService>();

        _sut = new TransferJourneyV3Controller(
            _transferCalculationRepositoryMock.Object,
            _edmsClientMock.Object,
            _uowMock.Object,
            _transferJourneyRepositoryMock.Object,
            _mdpDbUnitOfWorkMock.Object,
            _contentClientMock.Object,
            _memberRepositoryMock.Object,
            _smtpClientMock.Object,
            _transferCaseMock.Object,
            _edmsDocumentsIndexingMock.Object,
            _transferV2TemplateMock.Object,
            _transferJourneySubmitEmailTemplateMock.Object,
            _journeyDocumentsRepositoryMock.Object,
            _documentsRepositoryMock.Object,
            _loggerMock.Object,
            _documentFactoryProviderMock.Object,
            _pdfgeneratorMock.Object,
            _calculationsRepositoryMock.Object,
            _calculationsParserMock.Object,
            _idvServiceMock.Object);

        SetupControllerContext();
    }

    [Input(true, false, false, true, false, false, typeof(BadRequestObjectResult))]
    [Input(false, true, false, true, false, false, typeof(BadRequestObjectResult))]
    [Input(false, false, true, true, false, false, typeof(BadRequestObjectResult))]
    [Input(false, false, true, true, true, false, typeof(BadRequestObjectResult))]
    [Input(false, false, false, true, false, true, typeof(NoContentResult))]
    [Input(false, false, false, false, false, false, typeof(NotFoundObjectResult))]
    public async Task Submit_ReturnsCorrectHttpStatusCode(bool isMemberNone, bool isUploadDocumentError, bool isPostindexError, bool journeyExists, bool caseApiReturnsError, bool calculationExists, Type type)
    {
        var calculation = new CalculationBuilder().RetirementJsonV2(null).BuildV2();

        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(calculationExists ? calculation : null);

        var transferCalculation = CreateTransferCalculation();

        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(CreateTransferCalculation());

        var transferJourney = new TransferJourneyBuilder().Build();
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(journeyExists ? transferJourney : null);

        var member = new MemberBuilder().Build();
        _journeyDocumentsRepositoryMock
            .Setup(x => x.List(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()))
            .ReturnsAsync(new List<UploadedDocument>());

        _transferV2TemplateMock
            .Setup(x => x.RenderHtml(It.IsAny<string>(), It.IsAny<TransferJourney>(), It.IsAny<Member>(), It.IsAny<TransferQuote>(), It.IsAny<TransferApplicationStatus>(), It.IsAny<DateTimeOffset>(), It.IsAny<IEnumerable<UploadedDocument>>(), It.IsAny<Option<Calculation>>()))
            .ReturnsAsync("<div></div>");
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(isMemberNone ? Option<Member>.None : Option<Member>.Some(member));

        _contentClientMock
            .Setup(x => x.FindTemplate("transfer_v2_application_submission_email", It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new TemplateResponse { HtmlBody = "<div></div>", HtmlHeader = "<div></div>", HtmlFooter = "<div></div>" });
        _contentClientMock
            .Setup(x => x.FindTemplate("transfer_v2_pdf", It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new TemplateResponse { HtmlBody = "<div></div>", HtmlHeader = "<div></div>", HtmlFooter = "<div></div>" });

        Either<Error, string> errorOrCaseNumber = "123456";
        if (caseApiReturnsError)
            errorOrCaseNumber = Error.New("");
        _transferCaseMock
            .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(errorOrCaseNumber);
        _edmsClientMock
            .Setup(x => x.UploadDocument(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(isUploadDocumentError ? new DocumentUploadError { Message = "test" } : new DocumentUploadResponse { Uuid = It.IsAny<string>() });
        _documentFactoryProviderMock
            .Setup(x => x.GetFactory(DocumentType.TransferV2))
            .Returns(new RetirementDocumentFactory());
        Error? error = null;
        if (isPostindexError)
            error = Error.New("Test message");

        _edmsDocumentsIndexingMock
            .Setup(x => x.PostIndexTransferDocuments(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TransferJourney>(), It.IsAny<IList<UploadedDocument>>()))
            .ReturnsAsync(isPostindexError ? Error.New("Test message") : new List<(int, string)> { (123456, It.IsAny<string>()) });
        _smtpClientMock
            .Setup(x => x.SendWithAttachment(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MemoryStream>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _idvServiceMock
            .Setup(x => x.SaveIdentityVerification(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Prelude.Right(new UpdateIdentityResultResponse
            {
                Message = "Success"
            }));

        var request = new TransferJourneySubmitRequest { ContentAccessKey = "TestContentAccessKey" };
        var result = await _sut.Submit(request);
        result.Should().BeOfType(type);
    }

    public async Task SubmitLogsWarning_WhenSaveIdentityVerificationFails()
    {
        var calculation = new CalculationBuilder().RetirementJsonV2(null).BuildV2();

        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(calculation);

        var transferCalculation = CreateTransferCalculation();

        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(CreateTransferCalculation());

        var transferJourney = new TransferJourneyBuilder().Build();
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(transferJourney);

        var member = new MemberBuilder().Build();
        _journeyDocumentsRepositoryMock
            .Setup(x => x.List(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()))
            .ReturnsAsync(new List<UploadedDocument>());

        _transferV2TemplateMock
            .Setup(x => x.RenderHtml(It.IsAny<string>(), It.IsAny<TransferJourney>(), It.IsAny<Member>(), It.IsAny<TransferQuote>(), It.IsAny<TransferApplicationStatus>(), It.IsAny<DateTimeOffset>(), It.IsAny<IEnumerable<UploadedDocument>>(), It.IsAny<Option<Calculation>>()))
            .ReturnsAsync("<div></div>");
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.Some(member));

        _contentClientMock
            .Setup(x => x.FindTemplate("transfer_v2_application_submission_email", It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new TemplateResponse { HtmlBody = "<div></div>", HtmlHeader = "<div></div>", HtmlFooter = "<div></div>" });
        _contentClientMock
            .Setup(x => x.FindTemplate("transfer_v2_pdf", It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new TemplateResponse { HtmlBody = "<div></div>", HtmlHeader = "<div></div>", HtmlFooter = "<div></div>" });

        Either<Error, string> errorOrCaseNumber = "123456";
        _transferCaseMock
            .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(errorOrCaseNumber);
        _journeyDocumentsRepositoryMock
            .Setup(x => x.List(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<UploadedDocument>());
        _edmsClientMock
            .Setup(x => x.UploadDocument(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(new DocumentUploadResponse { Uuid = It.IsAny<string>() });
        _documentFactoryProviderMock
            .Setup(x => x.GetFactory(DocumentType.TransferV2))
            .Returns(new RetirementDocumentFactory());

        _edmsDocumentsIndexingMock
            .Setup(x => x.PostIndexTransferDocuments(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TransferJourney>(), It.IsAny<IList<UploadedDocument>>()))
            .ReturnsAsync(new List<(int, string)> { (123456, It.IsAny<string>()) });
        _smtpClientMock
            .Setup(x => x.SendWithAttachment(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MemoryStream>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _idvServiceMock
            .Setup(x => x.SaveIdentityVerification(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Prelude.Left<LanguageExt.Common.Error, UpdateIdentityResultResponse>(Error.New("IDV save failed")));

        var request = new TransferJourneySubmitRequest { ContentAccessKey = "TestContentAccessKey" };
        var result = await _sut.Submit(request);
        result.Should().BeOfType<NoContentResult>();

        _loggerMock.VerifyLogging("Failed to save IDV results.Error: IDV save failed", LogLevel.Warning, Times.Once());
    }

    private void SetupControllerContext()
    {
        var id = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim("business_group", "TestBusinessGroup"),
            new Claim("reference_number", "TestReferenceNumber"),
        };

        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    private TransferCalculation CreateTransferCalculation(string transferQuote = null)
    {
        if (transferQuote == null)
            transferQuote = @"{""guaranteeDate"":""2023-01-27T00:00:00+00:00"",""guaranteePeriod"":""P3M"",
                            ""replyByDate"":""2023-01-06T00:00:00+00:00"",""totalPensionAtDOL"":4996.04,""maximumResidualPension"":5765.93,
                            ""minimumResidualPension"":0.0,""transferValues"":{""totalGuaranteedTransferValue"":513476.26,
                            ""totalNonGuaranteedTransferValue"":12039.02,""minimumPartialTransferValue"":302752.75,
                            ""totalTransferValue"":525515.28},""isGuaranteedQuote"":false,""originalEffectiveDate"":""2022-10-27T00:00:00+00:00""}";

        return new TransferCalculation("RBS", "1111124", transferQuote, DateTimeOffset.UtcNow);
    }
}
