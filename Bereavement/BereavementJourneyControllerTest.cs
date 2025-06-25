using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.BereavementJourneys;
using WTW.MdpService.Domain.Bereavement;
using WTW.MdpService.Domain.Common;
using WTW.MdpService.Domain.Common.Journeys;
using WTW.MdpService.Infrastructure.BereavementDb;
using WTW.MdpService.Infrastructure.Content;
using WTW.MdpService.Infrastructure.Edms;
using WTW.MdpService.Infrastructure.EdmsIndexing;
using WTW.MdpService.Infrastructure.EmailConfirmation;
using WTW.MdpService.Infrastructure.Geolocation;
using WTW.MdpService.Infrastructure.MdpDb;
using WTW.MdpService.Infrastructure.MemberDb;
using WTW.MdpService.Infrastructure.PdfGenerator;
using WTW.MdpService.Infrastructure.Templates.Bereavement;
using WTW.MdpService.Test.Domain.Bereavement;
using WTW.Web.Errors;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Bereavement;

public class BereavementJourneyControllerTest
{
    private readonly Mock<IBereavementJourneyRepository> _repositoryMock;
    private readonly Mock<IEmailConfirmationSmtpClient> _smtpClientMock;
    private readonly Mock<ILoqateApiClient> _loqateApiClientMock;
    private readonly Mock<IContentClient> _contentClientMock;
    private readonly Mock<IEdmsDocumentsIndexing> _edmsDocumentsIndexingMock;
    private readonly Mock<IBereavementCase> _bereavementCaseMock;
    private readonly Mock<IBereavementUnitOfWork> _bereavementUnitOfWorkMock;
    private readonly Mock<IMdpUnitOfWork> _mdpUnitOfWorkMock;
    private readonly Mock<IJourneyDocumentsRepository> _journeyDocumentsRepositoryMock;
    private readonly Mock<IBereavementTemplate> _bereavementTemplateMock;
    private readonly Mock<IEdmsClient> _edmsClientMock;
    private readonly Mock<ILogger<BereavementJourneyController>> _loggerMock;
    private readonly Mock<IMemberDbUnitOfWork> _uowMock;
    private readonly BereavementJourneyController _sut;
    private readonly BereavementJourneySubmitRequestFactory _requestFactory;
    private readonly Mock<IPdfGenerator> _pdfGeneratorMock;

    public BereavementJourneyControllerTest()
    {
        _repositoryMock = new Mock<IBereavementJourneyRepository>();
        _smtpClientMock = new Mock<IEmailConfirmationSmtpClient>();
        _loqateApiClientMock = new Mock<ILoqateApiClient>();
        _contentClientMock = new Mock<IContentClient>();
        _edmsDocumentsIndexingMock = new Mock<IEdmsDocumentsIndexing>();
        _bereavementCaseMock = new Mock<IBereavementCase>();
        _bereavementUnitOfWorkMock = new Mock<IBereavementUnitOfWork>();
        _mdpUnitOfWorkMock = new Mock<IMdpUnitOfWork>();
        _journeyDocumentsRepositoryMock = new Mock<IJourneyDocumentsRepository>();
        _bereavementTemplateMock = new Mock<IBereavementTemplate>();
        _edmsClientMock = new Mock<IEdmsClient>();
        _loggerMock = new Mock<ILogger<BereavementJourneyController>>();
        _uowMock = new Mock<IMemberDbUnitOfWork>();
        _pdfGeneratorMock = new Mock<IPdfGenerator>();

        _sut = new BereavementJourneyController(_repositoryMock.Object, new BereavementJourneyConfiguration(10, 10, 10, 10, 10, 10),
            _smtpClientMock.Object, _loqateApiClientMock.Object, _contentClientMock.Object, _edmsDocumentsIndexingMock.Object,
            _bereavementCaseMock.Object, _bereavementUnitOfWorkMock.Object,
            _mdpUnitOfWorkMock.Object, _journeyDocumentsRepositoryMock.Object, _bereavementTemplateMock.Object, _edmsClientMock.Object,
            _loggerMock.Object, _uowMock.Object, _pdfGeneratorMock.Object);

        _requestFactory = new BereavementJourneySubmitRequestFactory();

        SetupControllerContext();
    }

    public async Task Submit_ReturnsNotFound_WhenJourneyIsExpired()
    {
        var request = _requestFactory.CreateValidRequest();
        _repositoryMock.Setup(x => x.FindUnexpired(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>()))
            .Returns(Task.FromResult(Option<BereavementJourney>.None));

        var result = await _sut.Submit(request);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task Submit_ReturnsBadRequest_WhenDocumentUploadFails()
    {
        var request = _requestFactory.CreateValidRequest();
        _repositoryMock.Setup(x => x.FindUnexpired(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>()))
            .Returns(Task.FromResult(Option<BereavementJourney>.Some(new BereavementJourneyBuilder().Build().Right())));

        _edmsClientMock
            .Setup(x => x.UploadDocument(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(new DocumentUploadError { Message = "Error" });

        _bereavementCaseMock.Setup(x => x.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IList<string>>()))
            .ReturnsAsync("123456");

        _contentClientMock
            .Setup(x => x.FindUnauthorizedTemplate(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new TemplateResponse { HtmlBody = "<div></div>", HtmlHeader = "<div></div>", HtmlFooter = "<div></div>" });

        _bereavementTemplateMock.Setup(x => x.RenderHtml(It.IsAny<string>(), It.IsAny<IEnumerable<QuestionForm>>(), It.IsAny<BereavementJourneySubmitRequest.BereavementJourneyDeceasedPerson>(), It.IsAny<BereavementJourneySubmitRequest.BereavementJourneyPerson>(), It.IsAny<BereavementJourneySubmitRequest.BereavementJourneyPerson>(), It.IsAny<BereavementJourneySubmitRequest.BereavementJourneyPerson>(), It.IsAny<BereavementJourneySubmitRequest.BereavementJourneyPerson>(), It.IsAny<DateTimeOffset>(), It.IsAny<IEnumerable<UploadedDocument>>()))
            .ReturnsAsync(("test", "<div></div>"));

        _pdfGeneratorMock
            .Setup(x => x.Generate(
                It.IsAny<string>(),
                It.IsAny<Option<string>>(),
                It.IsAny<Option<string>>()))
            .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("This is a test PDF stream.")));

        var result = await _sut.Submit(request);

        result.Should().BeOfType<BadRequestObjectResult>();
        var response = (result as BadRequestObjectResult).Value as ApiError;
        response.Should().NotBeNull();
        response.Errors[0].Message.Should().Be("Error");
    }

    public async Task Submit_ReturnsBadRequest_WhenDocumentPostIndexFails()
    {
        var request = _requestFactory.CreateValidRequest();
        _repositoryMock.Setup(x => x.FindUnexpired(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>()))
            .Returns(Task.FromResult(Option<BereavementJourney>.Some(new BereavementJourneyBuilder().Build().Right())));

        _edmsClientMock
            .Setup(x => x.UploadDocument(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(new DocumentUploadResponse { Uuid = It.IsAny<string>() });

        _edmsDocumentsIndexingMock
            .Setup(x => x.PostIndexBereavementDocuments(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IList<UploadedDocument>>()))
            .ReturnsAsync(Error.New("Error"));

        _bereavementCaseMock.Setup(x => x.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IList<string>>()))
            .ReturnsAsync("123456");

        _contentClientMock
            .Setup(x => x.FindUnauthorizedTemplate(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new TemplateResponse { HtmlBody = "<div></div>", HtmlHeader = "<div></div>", HtmlFooter = "<div></div>" });

        _bereavementTemplateMock.Setup(x => x.RenderHtml(It.IsAny<string>(), It.IsAny<IEnumerable<QuestionForm>>(), It.IsAny<BereavementJourneySubmitRequest.BereavementJourneyDeceasedPerson>(), It.IsAny<BereavementJourneySubmitRequest.BereavementJourneyPerson>(), It.IsAny<BereavementJourneySubmitRequest.BereavementJourneyPerson>(), It.IsAny<BereavementJourneySubmitRequest.BereavementJourneyPerson>(), It.IsAny<BereavementJourneySubmitRequest.BereavementJourneyPerson>(), It.IsAny<DateTimeOffset>(), It.IsAny<IEnumerable<UploadedDocument>>()))
            .ReturnsAsync(("test", "<div></div>"));

        _journeyDocumentsRepositoryMock.Setup(x => x.List(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<UploadedDocument>());

        _pdfGeneratorMock
            .Setup(x => x.Generate(
                It.IsAny<string>(),
                It.IsAny<Option<string>>(),
                It.IsAny<Option<string>>()))
            .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("This is a test PDF stream.")));

        var result = await _sut.Submit(request);

        result.Should().BeOfType<BadRequestObjectResult>();
        var response = (result as BadRequestObjectResult).Value as ApiError;
        response.Should().NotBeNull();
        response.Errors[0].Message.Should().Be("Error");
    }

    public async Task Submit_ReturnsOk_WhenEverythingSucceeds()
    {
        var request = _requestFactory.CreateValidRequest();

        _repositoryMock.Setup(x => x.FindUnexpired(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>()))
            .Returns(Task.FromResult(Option<BereavementJourney>.Some(new BereavementJourneyBuilder().Build().Right())));

        _contentClientMock
            .Setup(x => x.FindUnauthorizedTemplate(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new TemplateResponse { HtmlBody = "<div></div>", HtmlHeader = "<div></div>", HtmlFooter = "<div></div>" });

        _edmsClientMock
            .Setup(x => x.UploadDocument(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(new DocumentUploadResponse { Uuid = It.IsAny<string>() });

        _bereavementCaseMock.Setup(x => x.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IList<string>>()))
            .ReturnsAsync("123456");

        _journeyDocumentsRepositoryMock.Setup(x => x.List(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<UploadedDocument>());

        _bereavementTemplateMock.Setup(x => x.RenderHtml(It.IsAny<string>(), It.IsAny<IEnumerable<QuestionForm>>(), It.IsAny<BereavementJourneySubmitRequest.BereavementJourneyDeceasedPerson>(), It.IsAny<BereavementJourneySubmitRequest.BereavementJourneyPerson>(), It.IsAny<BereavementJourneySubmitRequest.BereavementJourneyPerson>(), It.IsAny<BereavementJourneySubmitRequest.BereavementJourneyPerson>(), It.IsAny<BereavementJourneySubmitRequest.BereavementJourneyPerson>(), It.IsAny<DateTimeOffset>(), It.IsAny<IEnumerable<UploadedDocument>>()))
            .ReturnsAsync(("test", "<div></div>"));

        _edmsDocumentsIndexingMock
            .Setup(x => x.PostIndexBereavementDocuments(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IList<UploadedDocument>>()))
            .ReturnsAsync(new List<(int, string)> { (123456, It.IsAny<string>()) });

        _pdfGeneratorMock
            .Setup(x => x.Generate(
                It.IsAny<string>(),
                It.IsAny<Option<string>>(),
                It.IsAny<Option<string>>()))
            .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("This is a test PDF stream.")));

        var result = await _sut.Submit(request);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Value.Should().BeOfType<BereavementJourneySubmitResponse>();

        var response = okResult.Value as BereavementJourneySubmitResponse;
        response.CaseNumber.Should().Be("123456");

        _smtpClientMock.Verify(x => x.Send(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    public async Task Submit_ReturnsBadRequest_WhenGeneratedPdfStreamIsNull()
    {
        var request = _requestFactory.CreateValidRequest();
        _repositoryMock.Setup(x => x.FindUnexpired(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>()))
            .Returns(Task.FromResult(Option<BereavementJourney>.Some(new BereavementJourneyBuilder().Build().Right())));

        _edmsClientMock
            .Setup(x => x.UploadDocument(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(new DocumentUploadError { Message = "Error" });

        _bereavementCaseMock.Setup(x => x.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IList<string>>()))
            .ReturnsAsync("123456");

        _contentClientMock
            .Setup(x => x.FindUnauthorizedTemplate(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new TemplateResponse { HtmlBody = "<div></div>", HtmlHeader = "<div></div>", HtmlFooter = "<div></div>" });

        _bereavementTemplateMock.Setup(x => x.RenderHtml(It.IsAny<string>(), It.IsAny<IEnumerable<QuestionForm>>(), It.IsAny<BereavementJourneySubmitRequest.BereavementJourneyDeceasedPerson>(), It.IsAny<BereavementJourneySubmitRequest.BereavementJourneyPerson>(), It.IsAny<BereavementJourneySubmitRequest.BereavementJourneyPerson>(), It.IsAny<BereavementJourneySubmitRequest.BereavementJourneyPerson>(), It.IsAny<BereavementJourneySubmitRequest.BereavementJourneyPerson>(), It.IsAny<DateTimeOffset>(), It.IsAny<IEnumerable<UploadedDocument>>()))
            .ReturnsAsync(("test", "<div></div>"));

        _pdfGeneratorMock
            .Setup(x => x.Generate(
                It.IsAny<string>(),
                It.IsAny<Option<string>>(),
                It.IsAny<Option<string>>()))
            .ReturnsAsync((MemoryStream)null);  // Return null stream

        var result = await _sut.Submit(request);

        result.Should().BeOfType<BadRequestObjectResult>();
        var response = (result as BadRequestObjectResult).Value as ApiError;
        response.Should().NotBeNull();
        response.Errors[0].Message.Should().Be("Failed to generate PDF. The stream is null.");
    }

    public async Task Submit_ReturnsBadRequest_WhenGeneratedPdfStreamIsEmpty()
    {
        var request = _requestFactory.CreateValidRequest();
        _repositoryMock.Setup(x => x.FindUnexpired(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>()))
            .Returns(Task.FromResult(Option<BereavementJourney>.Some(new BereavementJourneyBuilder().Build().Right())));

        _edmsClientMock
            .Setup(x => x.UploadDocument(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(new DocumentUploadError { Message = "Error" });

        _bereavementCaseMock.Setup(x => x.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IList<string>>()))
            .ReturnsAsync("123456");

        _contentClientMock
            .Setup(x => x.FindUnauthorizedTemplate(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new TemplateResponse { HtmlBody = "<div></div>", HtmlHeader = "<div></div>", HtmlFooter = "<div></div>" });

        _bereavementTemplateMock.Setup(x => x.RenderHtml(It.IsAny<string>(), It.IsAny<IEnumerable<QuestionForm>>(), It.IsAny<BereavementJourneySubmitRequest.BereavementJourneyDeceasedPerson>(), It.IsAny<BereavementJourneySubmitRequest.BereavementJourneyPerson>(), It.IsAny<BereavementJourneySubmitRequest.BereavementJourneyPerson>(), It.IsAny<BereavementJourneySubmitRequest.BereavementJourneyPerson>(), It.IsAny<BereavementJourneySubmitRequest.BereavementJourneyPerson>(), It.IsAny<DateTimeOffset>(), It.IsAny<IEnumerable<UploadedDocument>>()))
            .ReturnsAsync(("test", "<div></div>"));

        _pdfGeneratorMock
            .Setup(x => x.Generate(
                It.IsAny<string>(),
                It.IsAny<Option<string>>(),
                It.IsAny<Option<string>>()))
            .ReturnsAsync(new MemoryStream());  // Return empty stream

        var result = await _sut.Submit(request);

        result.Should().BeOfType<BadRequestObjectResult>();
        var response = (result as BadRequestObjectResult).Value as ApiError;
        response.Should().NotBeNull();
        response.Errors[0].Message.Should().Be("Failed to generate PDF. The stream is empty.");
    }


    private void SetupControllerContext()
    {
        var id = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim("business_group", "TestBusinessGroup"),
            new Claim("bereavement_reference_number", Guid.NewGuid().ToString()),
        };

        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }
}