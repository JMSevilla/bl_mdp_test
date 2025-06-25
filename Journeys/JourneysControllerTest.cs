using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Domain;
using WTW.MdpService.Domain.Common;
using WTW.MdpService.Domain.Common.Journeys;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.Infrastructure.Edms;
using WTW.MdpService.Infrastructure.Journeys;
using WTW.MdpService.Infrastructure.MdpDb;
using WTW.MdpService.Infrastructure.MemberDb;
using WTW.MdpService.Infrastructure.MemberDb.Documents;
using WTW.MdpService.Journeys;
using WTW.MdpService.Journeys.Submit.Services;
using WTW.MdpService.Journeys.Submit.Services.Dto;
using WTW.MdpService.RetirementJourneys;
using WTW.MdpService.Test.Domain.Mdp;
using WTW.MdpService.Test.Domain.Mdp.Journeys;
using WTW.MdpService.Test.Domain.Members;
using WTW.TestCommon.FixieConfig;
using WTW.Web;
using WTW.Web.Errors;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Journeys;

public class JourneysControllerTest
{
    private readonly Mock<IJourneysRepository> _journeysRepositoryMock;
    private readonly Mock<IJourneyDocumentsRepository> _journeysDocumentsRepositoryMock;
    private readonly Mock<IMdpUnitOfWork> _mdpDbUnitOfWorkMock;
    private readonly Mock<ILogger<JourneysController>> _loggerMock;
    private readonly Mock<IMemberRepository> _memberRepositoryMock;
    private readonly Mock<IDocumentRenderer> _documentRendererMock;
    private readonly Mock<IDocumentsRendererDataFactory> _documentsRendererDataFactory;
    private readonly Mock<IGenericJourneyService> _genericJourneyServiceMock;
    private readonly Mock<IJourneyService> _journeyService;
    private readonly Mock<IEdmsClient> _edmsClientMock;
    private readonly Mock<IGenericJourneyDetails> _genericJourneyDetailsMock;
    private readonly Mock<IDocumentsRepository> _documentsRepositoryMock;
    private readonly JourneysController _sut;

    public JourneysControllerTest()
    {
        _journeysRepositoryMock = new Mock<IJourneysRepository>();
        _journeysDocumentsRepositoryMock = new Mock<IJourneyDocumentsRepository>();
        _mdpDbUnitOfWorkMock = new Mock<IMdpUnitOfWork>();
        _loggerMock = new Mock<ILogger<JourneysController>>();
        _memberRepositoryMock = new Mock<IMemberRepository>();
        _documentRendererMock = new Mock<IDocumentRenderer>();
        _documentsRendererDataFactory = new Mock<IDocumentsRendererDataFactory>();
        _genericJourneyServiceMock = new Mock<IGenericJourneyService>();
        _journeyService = new Mock<IJourneyService>();
        _edmsClientMock = new Mock<IEdmsClient>();
        _genericJourneyDetailsMock = new Mock<IGenericJourneyDetails>();
        _documentsRepositoryMock = new Mock<IDocumentsRepository>();

        _sut = new JourneysController(
            _journeysRepositoryMock.Object,
            _journeysDocumentsRepositoryMock.Object,
            _mdpDbUnitOfWorkMock.Object, _loggerMock.Object,
            _memberRepositoryMock.Object,
            _documentRendererMock.Object,
            _documentsRendererDataFactory.Object,
            _genericJourneyServiceMock.Object,
            _journeyService.Object,
            _edmsClientMock.Object,
            _genericJourneyDetailsMock.Object,
            _documentsRepositoryMock.Object);

        SetupControllerContext();
    }

    public async Task StartJourneyReturns204Code_WhenJourneyExists()
    {
        _journeysRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new GenericJourney("RBS", "1234567", "transfer", "step1", "step2", true, "Started", DateTimeOffset.UtcNow));

        var result = await _sut.Start("test-type", new StartJourneyRequest { CurrentPageKey = "step1", NextPageKey = "step2", RemoveOnLogin = true });

        result.Should().BeOfType<NoContentResult>();
    }

    public async Task StartJourneyReturns204Code_WhenJourneyDoesNotExists()
    {
        _journeysRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<GenericJourney>.None);

        var result = await _sut.Start("test-type", new StartJourneyRequest { CurrentPageKey = "step1", NextPageKey = "step2" });

        result.Should().BeOfType<NoContentResult>();
        _genericJourneyServiceMock.Verify(x => x.CreateJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Once);
    }

    [Input(false, "Journey must be started to submit step. Journey type: test-type.")]
    [Input(true, "Invalid \"currentPageKey\"")]
    public async Task SubmitStepReturnsCorrectStatusCode_WhenJourneyDoesNotExist_OrInvalidAurrentPageKeyGiven(bool journeyExists, string expectedErrorMessage)
    {
        _journeysRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(journeyExists ? new GenericJourney("RBS", "1234567", "transfer", "step1", "step2", true, "Started", DateTimeOffset.UtcNow) : Option<GenericJourney>.None);

        var result = await _sut.SubmitStep("test-type", new SubmitGenericStepRequest { CurrentPageKey = "step222", NextPageKey = "step2" });

        if (journeyExists)
        {
            result.Should().BeOfType<BadRequestObjectResult>();
            (((BadRequestObjectResult)result).Value as ApiError).Errors[0].Message.Should().Be(expectedErrorMessage);
        }
        else
        {
            result.Should().BeOfType<NotFoundObjectResult>();
            (((NotFoundObjectResult)result).Value as ApiError).Errors[0].Message.Should().Be(expectedErrorMessage);
        }
    }

    public async Task SubmitStepReturns204Code_WhenJourneyExistsAndCurrentPageKeyValid()
    {
        _journeysRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new GenericJourney("RBS", "1234567", "transfer", "step1", "step2", true, "Started", DateTimeOffset.UtcNow));

        var result = await _sut.SubmitStep("test-type", new SubmitGenericStepRequest { CurrentPageKey = "step2", NextPageKey = "step3" });

        result.Should().BeOfType<NoContentResult>();
        _mdpDbUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
    }

    [Input(false, "Journey does not exist for given member. Journey type: test-type.")]
    [Input(true, "Previous step does not exist in journey for given current page key: random-page-key. Journey type: test-type.")]
    public async Task PreviousStepReturns404Code_WhenJourneyDoesNotExist_OrPreviousStepDoesNotExists(bool journeyExists, string expectedErrorMessage)
    {
        _journeysRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(journeyExists ? new GenericJourney("RBS", "1234567", "transfer", "step1", "step2", true, "Started", DateTimeOffset.UtcNow) : Option<GenericJourney>.None);

        var result = await _sut.PreviousStep("test-type", "random-page-key");

        result.Should().BeOfType<NotFoundObjectResult>();
        (((NotFoundObjectResult)result).Value as ApiError).Errors[0].Message.Should().Be(expectedErrorMessage);
    }

    public async Task PreviousStepReturns200Code_WhenJourneyExistsAndCurrentPageKeyValid()
    {
        _journeysRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new GenericJourney("RBS", "1234567", "transfer", "step1", "step2", true, "Started", DateTimeOffset.UtcNow));

        var result = await _sut.PreviousStep("test-type", "step2");

        result.Should().BeOfType<OkObjectResult>();
        (((OkObjectResult)result).Value as PreviousStepResponse).PreviousPageKey.Should().Be("step1");
    }

    public async Task CheckIntegrityReturns404Code_WhenJourneyDoesNotExist()
    {
        var result = await _sut.CheckJourneyIntegrity("test-type", "random-page-key");

        result.Should().BeOfType<NotFoundObjectResult>();
        (((NotFoundObjectResult)result).Value as ApiError).Errors[0].Message.Should().Be("Journey does not exist for given member. Journey type: test-type.");
    }

    [Input("step1", "step1")]
    [Input("step2", "step2")]
    [Input("random-step", "step2")]
    public async Task CheckIntegrityReturns200Code_WhenJourneyExists(string inputPageKey, string expectedRedirectPageKey)
    {
        _journeysRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new GenericJourney("RBS", "1234567", "transfer", "step1", "step2", true, "Started", DateTimeOffset.UtcNow));

        var result = await _sut.CheckJourneyIntegrity("test-type", inputPageKey);

        result.Should().BeOfType<OkObjectResult>();
        (((OkObjectResult)result).Value as IntegrityResponse).RedirectStepPageKey.Should().Be(expectedRedirectPageKey);
    }

    public async Task DeleteJourneyReturn404Code_WhenJourneyDoesNotExist()
    {
        var result = await _sut.Delete("random-type");

        result.Should().BeOfType<NotFoundObjectResult>();
        _mdpDbUnitOfWorkMock.Verify(x => x.Commit(), Times.Never);
        _journeysDocumentsRepositoryMock.Verify(x => x.RemoveAll(It.IsAny<List<UploadedDocument>>()), Times.Never);
    }

    public async Task DeleteJourneyReturns204Code_WhenJourneyExists()
    {
        _journeysRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new GenericJourney("RBS", "1234567", "transfer", "step1", "step2", true, "Started", DateTimeOffset.UtcNow));

        _journeysDocumentsRepositoryMock
            .Setup(x => x.List(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<UploadedDocument> { new UploadedDocumentsBuilder().Build() });

        var result = await _sut.Delete("test-type");

        result.Should().BeOfType<NoContentResult>();
        _mdpDbUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
        _journeysDocumentsRepositoryMock.Verify(x => x.RemoveAll(It.IsAny<List<UploadedDocument>>()), Times.Once);
    }

    public async Task SubmitQuestionStep_ReturnsNoContent_WhenJourneyTypeIsFoundAndStepIsSubmitted()
    {
        _journeysRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new GenericJourney("RBS", "1234567", "transfer", "step1", "step2", true, "Started", DateTimeOffset.UtcNow));

        _mdpDbUnitOfWorkMock
            .Setup(x => x.Commit())
            .Returns(Task.CompletedTask);

        var request = new SubmitJourneyQuestionStepRequest { CurrentPageKey = "step2", NextPageKey = "step3", QuestionKey = "TestQuestionKey", AnswerKey = "TestAnswerKey" };
        var result = await _sut.SubmitQuestionStep("transfer", request);
        result.Should().BeOfType<NoContentResult>();
    }

    public async Task SubmitQuestionStep_ReturnsNotFound_WhenJourneyTypeIsNotFound()
    {
        _journeysRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), "transfer"))
            .ReturnsAsync(new GenericJourney("RBS", "1234567", "transfer", "step1", "step2", true, "Started", DateTimeOffset.UtcNow));

        var request = new SubmitJourneyQuestionStepRequest { CurrentPageKey = "step2", NextPageKey = "step3", QuestionKey = "TestQuestionKey", AnswerKey = "TestAnswerKey" };
        var result = await _sut.SubmitQuestionStep("dcretirement", request);
        result.Should().BeOfType<NotFoundObjectResult>();
        var errorResponse = ((NotFoundObjectResult)result).Value as ApiError;
        errorResponse.Errors[0].Message.Should().Be("Member did not start dcretirement journey yet");
    }

    public async Task SubmitQuestionStep_ReturnsBadRequest_WhenJourneyTypeIsFoundButStepCannotBeSubmitted()
    {
        _journeysRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), "transfer"))
            .ReturnsAsync(new GenericJourney("RBS", "1234567", "transfer", "step1", "step2", true, "Started", DateTimeOffset.UtcNow));

        var request = new SubmitJourneyQuestionStepRequest { CurrentPageKey = "TestCurrentPageKey", NextPageKey = "TestNextPageKey", QuestionKey = "TestQuestionKey", AnswerKey = "TestAnswerKey" };
        var result = await _sut.SubmitQuestionStep("transfer", request);
        result.Should().BeOfType<BadRequestObjectResult>();
        var errorResponse = ((BadRequestObjectResult)result).Value as ApiError;
        errorResponse.Errors[0].Message.Should().Be("Invalid \"currentPageKey\"");
    }

    public async Task CanReturnQuestionForm_FromCurrentPageKey()
    {
        var journey = new GenericJourney("RBS", "1234567", "transfer", "step1", "step2", true, "Started", DateTimeOffset.UtcNow);
        journey.TrySubmitStep("step2", "step3", DateTimeOffset.UtcNow, "TestQuestionKey", "TestAnswerKey");

        _journeysRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(journey);

        _mdpDbUnitOfWorkMock
            .Setup(x => x.Commit())
            .Returns(Task.CompletedTask);

        var result = await _sut.QuestionForm("transfer", "step2");
        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as JourneyQuestionFormResponse;
        response.QuestionKey.Should().Be("TestQuestionKey");
        response.AnswerKey.Should().Be("TestAnswerKey");
    }

    [Input(true, "Not found")]
    [Input(false, "Journey does not exist for given member. Journey type: transfer.")]
    public async Task QuestionForm_Returns404(bool journeyExists, string errorMessage)
    {
        var journey = new GenericJourney("RBS", "1234567", "transfer", "step1", "step2", true, "Started", DateTimeOffset.UtcNow);
        journey.TrySubmitStep("step2", "step3", DateTimeOffset.UtcNow, "TestQuestionKey", "TestAnswerKey");

        if (journeyExists)
            _journeysRepositoryMock
                .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(journey);

        _mdpDbUnitOfWorkMock
            .Setup(x => x.Commit())
            .Returns(Task.CompletedTask);

        var result = await _sut.QuestionForm("transfer", "randomStepKey");
        result.Should().BeOfType<NotFoundObjectResult>();
        var response = ((NotFoundObjectResult)result).Value as ApiError;
        response.Errors[0].Message.Should().Be(errorMessage);
    }

    public async Task GetAllJourneyData_ReturnsAllJourneyData()
    {
        var journey = new GenericJourney("RBS", "1234567", "transfer", "step1", "step2", true, "Started", DateTimeOffset.UtcNow);
        journey.TrySubmitStep("step2", "step3", DateTimeOffset.UtcNow, "TestQuestionKey", "TestAnswerKey");
        journey.GetFirstStep().UpdateGenericData("preJourneyForm", "{\"formKey\":\"formValue\"}");

        var member = new MemberBuilder().EmailView(Email.Create("test.test@test.com").Right()).Build();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.Some(member));

        _journeysRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(journey);

        _journeysDocumentsRepositoryMock
            .Setup(x => x.List(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<UploadedDocument> 
            { 
                new UploadedDocumentsBuilder().DocumentType(null).FileName("regular_doc.pdf").Build(),
                new UploadedDocumentsBuilder().DocumentType(null).FileName("another_doc.pdf").Build(),
                new UploadedDocumentsBuilder().DocumentType(MdpConstants.IdentityDocumentType).FileName("passport.pdf").Build(),
                new UploadedDocumentsBuilder().DocumentType(MdpConstants.IdentityDocumentType).FileName("drivers_license.pdf").Build()
            });

        _genericJourneyDetailsMock
            .Setup(x => x.GetAll(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new GenericJourneyData
            {
                PreJourneyData = new Dictionary<string, object> { { "preJourneyForm", new { formKey = "formValue" } } },
                StepsWithQuestion = new Dictionary<string, object> { { "step2", new { TestQuestionKey =  new { AnswerKey = "TestAnswerKey", AnswerValue= "TestAnswerKey" } } } }
            });

        _mdpDbUnitOfWorkMock
            .Setup(x => x.Commit())
            .Returns(Task.CompletedTask);

        var result = await _sut.GetAllJourneyData("transfer");
        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as JourneyDataResponse;
        response.EmailAddress.Should().Be("test.test@test.com");
        response.Journey.PreJourneyData.Count.Should().Be(1);
        response.Journey.PreJourneyData.First().Key.Should().Be("preJourneyForm");
        var preJourneyForm = JsonSerializer.Serialize(response.Journey.PreJourneyData.First().Value);
        preJourneyForm.Should().Be("{\"formKey\":\"formValue\"}");
        response.Journey.StepsWithQuestion.Count.Should().Be(1);
        response.Journey.StepsWithQuestion.First().Key.Should().Be("step2");
        var stepQuestion = JsonSerializer.Serialize(response.Journey.StepsWithQuestion.First().Value);
        stepQuestion.Should().Be(@"{""TestQuestionKey"":{""AnswerKey"":""TestAnswerKey"",""AnswerValue"":""TestAnswerKey""}}");
        
        response.UploadedFilesNames.Should().NotBeNull();
        response.UploadedFilesNames.Should().HaveCount(2);
        response.UploadedFilesNames.Should().Contain("regular_doc.pdf");
        response.UploadedFilesNames.Should().Contain("another_doc.pdf");
        
        response.IdentityFilesNames.Should().NotBeNull();
        response.IdentityFilesNames.Should().HaveCount(2);
        response.IdentityFilesNames.Should().Contain("passport.pdf");
        response.IdentityFilesNames.Should().Contain("drivers_license.pdf");
    }

    public async Task GetStages_ReturnsStages()
    {
        var journey = new GenericJourney("RBS", "1234567", "transfer", "step1", "step1.1", true, "Started", DateTimeOffset.UtcNow);
        journey.TrySubmitStep("step1.1", "step1.2", DateTimeOffset.UtcNow);
        journey.TrySubmitStep("step1.2", "step2", DateTimeOffset.UtcNow);

        var member = new MemberBuilder().Build();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.Some(member));

        _journeysRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(journey);

        var request = new JourneyStageStatusRequest
        {
            Stages = new List<JourneyStageRequest>
            {
                new JourneyStageRequest
                {
                    Stage = "stage1",
                    Page = new JourneyStageStatusPageRequest
                    {
                        Start = new List<string> {"step1"},
                        End = new List<string> {"step1.2"}
                    }
                }
            }
        };

        var result = await _sut.StagesStatus("transfer", request);
        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as IEnumerable<JourneyStageStatusResponse>;
        response.First().Stage.Should().Be("stage1");
        response.First().InProgress.Should().BeFalse();
        response.First().FirstPageKey.Should().Be("step1");
    }

    public async Task GenerateJourneyPdf_ReturnsFileResult_WhenPdfGeneratedSuccessfully()
    {
        var expectedFileName = "TestFileName.pdf";

        var journey = new RetirementJourneyBuilder().Build();
        _journeyService.Setup(s => s.GetJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(journey);

        _memberRepositoryMock
            .Setup(x => x.ExistsMember(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _documentRendererMock
            .Setup(x => x.RenderGenericSummaryPdf(It.IsAny<DocumentsRendererData>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((new MemoryStream(), expectedFileName));

        _documentRendererMock
            .Setup(x => x.RenderDirectPdf(It.IsAny<DocumentsRendererData>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((new MemoryStream(), expectedFileName));

        var result = await _sut.GenerateJourneyPdf("dbretirementapplication", new JourneyPdfDownloadRequest
        {
            TemplateName = "TestTemplate",
            ContentAccessKey = "TestKey"
        });
        result.Should().BeOfType<FileContentResult>();
        (result as FileContentResult).ContentType.Should().Be("application/pdf");
        (result as FileContentResult).FileDownloadName.Should().Be(expectedFileName);
    }

    public async Task GenerateJourneyPdf_ReturnsFileResult_WhenPdfComesFromEdms()
    {
        var expectedFileName = "TestFileName.pdf";

        var journey = new RetirementJourneyBuilder().Build();
        _journeyService.Setup(s => s.GetJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(journey);

        _memberRepositoryMock
            .Setup(x => x.ExistsMember(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _genericJourneyServiceMock
            .Setup(x => x.GetSubmissionDetailsFromGenericData(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new SubmissionDetailsDto { SummaryPdfEdmsImageId = 123456789 });

        _edmsClientMock.Setup(x => x.GetDocument(It.IsAny<int>())).ReturnsAsync(new MemoryStream());

        _documentsRepositoryMock
            .Setup(x => x.FindByImageId(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new Document(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<string>(), "TestFileName.pdf", It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()));

        var result = await _sut.GenerateJourneyPdf("dbretirementapplication", new JourneyPdfDownloadRequest
        {
            TemplateName = "TestTemplate",
            ContentAccessKey = "TestKey"
        });
        result.Should().BeOfType<FileStreamResult>();
        (result as FileStreamResult).ContentType.Should().Be("application/pdf");
        (result as FileStreamResult).FileDownloadName.Should().Be(expectedFileName);
    }

    public async Task GenerateJourneyPdf_ReturnsBadRequest_WhenMemberNotFound()
    {
        var journey = new RetirementJourneyBuilder().Build();
        _journeyService.Setup(s => s.GetJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(journey);

        _memberRepositoryMock
            .Setup(x => x.ExistsMember(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var result = await _sut.GenerateJourneyPdf("dbretirementapplication", new JourneyPdfDownloadRequest
        {
            TemplateName = "TestTemplate",
            ContentAccessKey = "TestKey"
        });

        result.Should().BeOfType<BadRequestObjectResult>();
        (result as BadRequestObjectResult).Value.Should().BeOfType<ApiError>();
        ((ApiError)(result as BadRequestObjectResult).Value).Errors[0].Message.Should().Be("Member was not found.");
    }

    public async Task GenerateJourneyPdf_ReturnsNotFound_WhenJourneyDoesNotExist()
    {
        _journeyService.Setup(s => s.GetJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((Journey)null);

        var result = await _sut.GenerateJourneyPdf("nonexistentjourney", new JourneyPdfDownloadRequest
        {
            TemplateName = "TestTemplate",
            ContentAccessKey = "TestKey"
        });

        result.Should().BeOfType<NotFoundObjectResult>();
        (result as NotFoundObjectResult).Value.Should().BeOfType<ApiError>();
        ((ApiError)(result as NotFoundObjectResult).Value).Errors[0].Message.Should().Contain("Journey does not exist for given member.");
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
}