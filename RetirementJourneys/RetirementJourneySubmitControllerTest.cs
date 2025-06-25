using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Domain.Mdp;
using WTW.MdpService.Domain.Mdp.Calculations;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.IdentityVerification.Services;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.MdpService.Infrastructure.CasesApi;
using WTW.MdpService.Infrastructure.Content;
using WTW.MdpService.Infrastructure.EmailConfirmation;
using WTW.MdpService.Infrastructure.IdvService;
using WTW.MdpService.Infrastructure.MdpDb;
using WTW.MdpService.Infrastructure.MemberDb;
using WTW.MdpService.Infrastructure.Templates.RetirementApplication.Submit;
using WTW.MdpService.Journeys.Submit.Services;
using WTW.MdpService.RetirementJourneys;
using WTW.MdpService.Test.Domain.Mdp.Calculations;
using WTW.MdpService.Test.Domain.Mdp.Journeys;
using WTW.TestCommon.Helpers;
using WTW.Web.Errors;

namespace WTW.MdpService.Test.RetirementJourneys;

public class RetirementJourneySubmitControllerTest
{
    private readonly Mock<IRetirementJourneyRepository> _repositoryMock;
    private readonly Mock<IEmailConfirmationSmtpClient> _emailConfirmationSmtpClientMock;
    private readonly Mock<IMemberRepository> _memberRepositoryMock;
    private readonly Mock<IMemberDbUnitOfWork> _memberDbUnitOfWorkMock;
    private readonly Mock<IMdpUnitOfWork> _mdpUnitOfWorkMock;
    private readonly Mock<ILogger<RetirementJourneySubmitController>> _loggerMock;
    private readonly Mock<ICalculationsParser> _calculationsParserMock;
    private readonly Mock<IContentClient> _contentClientMock;
    private readonly Mock<IDocumentsRendererDataFactory> _documentsRendererDataFactory;
    private readonly Mock<IDocumentRenderer> _documentRendererMock;
    private readonly Mock<ICaseRequestFactory> _caseApiRequestModelFactoryMock;
    private readonly Mock<ICaseService> _caseServiceMock;
    private readonly Mock<IDocumentsUploaderService> _documentUploaderMock;
    private readonly Mock<IIdvService> _idvServiceMock;
    private readonly RetirementJourney _journey;
    private readonly RetirementJourneySubmitController _sut;

    public RetirementJourneySubmitControllerTest()
    {
        _repositoryMock = new Mock<IRetirementJourneyRepository>();
        _emailConfirmationSmtpClientMock = new Mock<IEmailConfirmationSmtpClient>();
        _memberRepositoryMock = new Mock<IMemberRepository>();
        _memberDbUnitOfWorkMock = new Mock<IMemberDbUnitOfWork>();
        _mdpUnitOfWorkMock = new Mock<IMdpUnitOfWork>();
        _loggerMock = new Mock<ILogger<RetirementJourneySubmitController>>();
        _calculationsParserMock = new Mock<ICalculationsParser>();
        _contentClientMock = new Mock<IContentClient>();
        _documentsRendererDataFactory = new Mock<IDocumentsRendererDataFactory>();
        _documentRendererMock = new Mock<IDocumentRenderer>();
        _caseApiRequestModelFactoryMock = new Mock<ICaseRequestFactory>();
        _caseServiceMock = new Mock<ICaseService>();
        _documentUploaderMock = new Mock<IDocumentsUploaderService>();
        _idvServiceMock = new Mock<IIdvService>();

        _journey = new RetirementJourneyBuilder().Build().SetCalculation(new CalculationBuilder().BuildV2());

        _contentClientMock = new Mock<IContentClient>();

        _repositoryMock
            .Setup(x => x.FindUnexpiredUnsubmittedJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(Option<RetirementJourney>.Some(_journey));
        var retirementV2Params = new RetirementV2Params()
        {
            TotalAvcFundValue = 1000,
        };
        var retirementV2 = new RetirementV2(retirementV2Params);
        _calculationsParserMock.Setup(x => x.GetRetirementV2(It.IsAny<string>()))
            .Returns(retirementV2);
        var mockTransaction = new Mock<IDbContextTransaction>();
        _mdpUnitOfWorkMock
            .Setup(x => x.BeginTransactionAsync())
            .ReturnsAsync(mockTransaction.Object);

        _memberDbUnitOfWorkMock
            .Setup(x => x.BeginTransactionAsync())
            .ReturnsAsync(mockTransaction.Object);


        _sut = new RetirementJourneySubmitController(
            _repositoryMock.Object,
            _emailConfirmationSmtpClientMock.Object,
            _memberRepositoryMock.Object,
            _memberDbUnitOfWorkMock.Object,
            _mdpUnitOfWorkMock.Object,
            _contentClientMock.Object,
            _loggerMock.Object,
            _calculationsParserMock.Object,
            Mock.Of<IRetirementApplicationSubmissionTemplate>(),
            _documentsRendererDataFactory.Object,
            _documentRendererMock.Object,
            _caseApiRequestModelFactoryMock.Object,
            _caseServiceMock.Object,
            _documentUploaderMock.Object,
            _idvServiceMock.Object
        );

        _sut.SetupControllerContext();
    }
    public async Task SubmitV2ReturnsNotFound_WhenJourneyNotFound()
    {
        _repositoryMock
            .Setup(x => x.FindUnexpiredUnsubmittedJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(Option<RetirementJourney>.None);

        var result = await _sut.SubmitV2(new SubmitRetirementJourneyV2Request());

        result.Should().BeOfType<NotFoundObjectResult>();
        var apiError = (result as NotFoundObjectResult)?.Value as ApiError;
        apiError.Should().NotBeNull();
        apiError.Errors[0].Message.Should().Be("Not found");
    }

    public async Task SubmitV2ReturnsBadRequest_WhenAcknowledgementIsRequired()
    {
        var request = new SubmitRetirementJourneyV2Request
        {
            Acknowledgement = false
        };

        var result = await _sut.SubmitV2(request);

        result.Should().BeOfType<BadRequestObjectResult>();
        var apiError = (result as BadRequestObjectResult)?.Value as ApiError;
        apiError.Should().NotBeNull();
        apiError.Errors[0].Message.Should().Be("Acknowledgement must be confirmed.");
    }

    public async Task SubmitV2ReturnsBadRequest_WhenMemberNotFound()
    {

        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<Member>.None);

        var request = new SubmitRetirementJourneyV2Request
        {
            Acknowledgement = true
        };

        var result = await _sut.SubmitV2(request);

        result.Should().BeOfType<BadRequestObjectResult>();
        var apiError = (result as BadRequestObjectResult)?.Value as ApiError;
        apiError.Should().NotBeNull();
        apiError.Errors[0].Message.Should().Be("Member was not found.");
    }

    public async Task SubmitV2LogsWarning_WhenSaveIdentiyVerificationFails()
    {
        var member = new Mock<Member>().Object;
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<Member>.Some(member));

        _caseServiceMock
            .Setup(x => x.Create(It.IsAny<CreateCaseRequest>()))
            .ReturnsAsync(Prelude.Right(new string("CASE123")));

        _documentUploaderMock
            .Setup(x => x.UploadDBRetirementDocument(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>(), It.IsAny<string>(), It.IsAny<Guid?>()))
            .ReturnsAsync(Prelude.Right(1));

        _idvServiceMock
            .Setup(x => x.SaveIdentityVerification(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Prelude.Left<LanguageExt.Common.Error, UpdateIdentityResultResponse>(LanguageExt.Common.Error.New("Error Message")));

        _contentClientMock
           .Setup(x => x.FindTemplate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(new TemplateResponse { HtmlBody = "<div></div>", HtmlHeader = "<div></div>", HtmlFooter = "<div></div>" });

        _documentRendererMock
            .Setup(x => x.RenderGenericSummaryPdf(It.IsAny<DocumentsRendererData>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((new MemoryStream(), "test.pdf"));

        var request = new SubmitRetirementJourneyV2Request
        {
            Acknowledgement = true
        };

        var result = await _sut.SubmitV2(request);

        result.Should().BeOfType<NoContentResult>();

        var expectedMessage = "Failed to save IDV results.Error: Error Message";

        _loggerMock.VerifyLogging(expectedMessage, LogLevel.Warning, Times.Once());
    }

    public async Task SubmitV2ReturnsNoContent_WhenSuccessful()
    {
        var member = new Mock<Member>().Object;
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<Member>.Some(member));

        _caseServiceMock
            .Setup(x => x.Create(It.IsAny<CreateCaseRequest>()))
            .ReturnsAsync(Prelude.Right(new string("CASE123")));

        _documentUploaderMock
            .Setup(x => x.UploadDBRetirementDocument(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>(), It.IsAny<string>(), It.IsAny<Guid?>()))
            .ReturnsAsync(Prelude.Right(1));

        _idvServiceMock
            .Setup(x => x.SaveIdentityVerification(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Prelude.Right(new UpdateIdentityResultResponse
            {
                Message = "Success"
            }));

        _contentClientMock
           .Setup(x => x.FindTemplate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(new TemplateResponse { HtmlBody = "<div></div>", HtmlHeader = "<div></div>", HtmlFooter = "<div></div>" });

        _documentRendererMock
            .Setup(x => x.RenderGenericSummaryPdf(It.IsAny<DocumentsRendererData>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((new MemoryStream(), "test.pdf"));

        var request = new SubmitRetirementJourneyV2Request
        {
            Acknowledgement = true
        };

        var result = await _sut.SubmitV2(request);

        result.Should().BeOfType<NoContentResult>();
    }
}
