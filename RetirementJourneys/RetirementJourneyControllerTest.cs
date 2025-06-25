using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Domain.Mdp;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.Infrastructure.Content;
using WTW.MdpService.Infrastructure.Journeys.Documents;
using WTW.MdpService.Infrastructure.MdpDb;
using WTW.MdpService.Infrastructure.Templates.RetirementApplication.Submit;
using WTW.MdpService.RetirementJourneys;
using WTW.MdpService.Test.Domain.Mdp.Calculations;
using WTW.MdpService.Test.Domain.Mdp.Journeys;
using WTW.Web;
using WTW.Web.Errors;
using static LanguageExt.Prelude;

namespace WTW.MdpService.Test.RetirementJourneys;

public class RetirementJourneyControllerTest
{
    private readonly RetirementJourneyController _sut;
    private readonly Mock<IRetirementJourneyRepository> _retirementJourneyRepositoryMock;
    private readonly Mock<IMdpUnitOfWork> _mdpDbUnitOfWorkMock;
    private readonly Mock<IContentClient> _contentClientMock;
    private readonly Mock<IRetirementApplicationQuotesV2> _retirementApplicationQuotesV2Mock;
    private readonly Mock<ICalculationsRepository> _calculationsRepositoryMock;
    private readonly Mock<IJourneyDocumentsHandlerService> _journeyDocumentsHandlerServiceMock;
    private readonly Mock<ILogger<RetirementJourneyController>> _loggerMock;

    public RetirementJourneyControllerTest()
    {
        _retirementJourneyRepositoryMock = new Mock<IRetirementJourneyRepository>();
        _mdpDbUnitOfWorkMock = new Mock<IMdpUnitOfWork>();
        _contentClientMock = new Mock<IContentClient>();
        _retirementApplicationQuotesV2Mock = new Mock<IRetirementApplicationQuotesV2>();
        _calculationsRepositoryMock = new Mock<ICalculationsRepository>();
        _journeyDocumentsHandlerServiceMock = new Mock<IJourneyDocumentsHandlerService>();
        _loggerMock = new Mock<ILogger<RetirementJourneyController>>();

        _sut = new RetirementJourneyController(
            _retirementJourneyRepositoryMock.Object,
            _mdpDbUnitOfWorkMock.Object,
            _contentClientMock.Object,
            _retirementApplicationQuotesV2Mock.Object,
            _calculationsRepositoryMock.Object,
            _journeyDocumentsHandlerServiceMock.Object,
            _loggerMock.Object);

        SetupControllerContext();
    }

    public async Task DeleteJourney_ReturnsNoContent()
    {
        var journey = new RetirementJourneyBuilder().Build().SetCalculation(new CalculationBuilder().BuildV2());

        _retirementJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(journey);

        var result = await _sut.DeleteJourney();

        result.Should().BeOfType<NoContentResult>();
        _retirementJourneyRepositoryMock.Verify(x => x.Remove(journey), Times.Once);
        _calculationsRepositoryMock.Verify(x => x.Remove(journey.Calculation), Times.Once);
        _mdpDbUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
    }

    public async Task DeleteJourney_ReturnsNotFound()
    {
        _retirementJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<RetirementJourney>.None);

        var result = await _sut.DeleteJourney();

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task PreviousStep_ReturnsOkObjectResult()
    {
        var journey = new RetirementJourneyBuilder()
            .BuildWithSteps()
            .SetCalculation(new CalculationBuilder().BuildV2());

        journey.TrySubmitStep("step2", "step3", DateTimeOffset.UtcNow);
        journey.TrySubmitStep("step3", "step4", DateTimeOffset.UtcNow);

        _retirementJourneyRepositoryMock
            .Setup(x => x.FindUnexpiredJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(journey);

        var result = await _sut.PreviousStep("step3");

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as PreviousStepResponse;
        response.PreviousPageKey.Should().Be("step2");
    }

    public async Task PreviousStep_ReturnsNotFound()
    {
        _retirementJourneyRepositoryMock
            .Setup(x => x.FindUnexpiredJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(Option<RetirementJourney>.None);

        var result = await _sut.PreviousStep("step3");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task SwitchStep_ReturnsNoContent()
    {
        var journey = new RetirementJourneyBuilder()
            .BuildWithSteps()
            .SetCalculation(new CalculationBuilder().BuildV2());

        journey.TrySubmitStep("step2", "step3", DateTimeOffset.UtcNow);

        _retirementJourneyRepositoryMock
            .Setup(x => x.FindUnexpiredJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(journey);

        var request = new SwitchStepRequest
        {
            SwitchPageKey = "step2",
            NextPageKey = "step3"
        };

        var result = await _sut.SwitchStep(request);

        result.Should().BeOfType<NoContentResult>();
        _mdpDbUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
    }

    public async Task SwitchStep_ReturnsBadRequest_WhenSwitchPageKeyIsWrong()
    {
        var journey = new RetirementJourneyBuilder()
            .BuildWithSteps()
            .SetCalculation(new CalculationBuilder().BuildV2());

        journey.TrySubmitStep("step2", "step3", DateTimeOffset.UtcNow);

        _retirementJourneyRepositoryMock
            .Setup(x => x.FindUnexpiredJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(journey);

        var request = new SwitchStepRequest
        {
            SwitchPageKey = "current",
            NextPageKey = "next"
        };

        var result = await _sut.SwitchStep(request);

        result.Should().BeOfType<NotFoundObjectResult>();
        (((NotFoundObjectResult)result).Value as ApiError).Errors[0].Code.Should().Be("previous_page_key_not_found");
    }

    public async Task SubmitStep_ReturnsNoContent()
    {
        var journey = new RetirementJourneyBuilder()
            .BuildWithSteps()
            .SetCalculation(new CalculationBuilder().BuildV2());

        _retirementJourneyRepositoryMock
            .Setup(x => x.FindUnexpiredJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(journey);

        var request = new SubmitStepRequest
        {
            CurrentPageKey = "step2",
            NextPageKey = "step3"
        };

        var result = await _sut.SubmitStep(request);

        result.Should().BeOfType<NoContentResult>();
        journey.JourneyBranches[0].JourneySteps[1].CurrentPageKey.Should().Be(request.CurrentPageKey);
        journey.JourneyBranches[0].JourneySteps[1].NextPageKey.Should().Be(request.NextPageKey);
        _mdpDbUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
    }

    public async Task SubmitStep_ReturnsBadRequest_WhenCurrentPageKeyIsWrong()
    {
        var journey = new RetirementJourneyBuilder()
            .BuildWithSteps()
            .SetCalculation(new CalculationBuilder().BuildV2());

        _retirementJourneyRepositoryMock
            .Setup(x => x.FindUnexpiredJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(journey);

        var request = new SubmitStepRequest
        {
            CurrentPageKey = "current",
            NextPageKey = "next"
        };

        var result = await _sut.SubmitStep(request);

        result.Should().BeOfType<BadRequestObjectResult>();
        (((BadRequestObjectResult)result).Value as ApiError).Errors[0].Message.Should().Be("Invalid \"currentPageKey\"");
    }

    public async Task SubmitQuestionStep_ReturnsNoContent()
    {
        var journey = new RetirementJourneyBuilder()
            .BuildWithSteps()
            .SetCalculation(new CalculationBuilder().BuildV2());

        _retirementJourneyRepositoryMock
            .Setup(x => x.FindUnexpiredJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(journey);

        var request = new SubmitQuestionStepRequest
        {
            CurrentPageKey = "step2",
            NextPageKey = "step3",
            QuestionKey = "questionId",
            AnswerKey = "answer",
            AnswerValue = "answerValue",

        };

        var result = await _sut.SubmitQuestionStep(request);

        result.Should().BeOfType<NoContentResult>();
        journey.JourneyBranches[0].JourneySteps[1].QuestionForm.AnswerKey.Should().Be(request.AnswerKey);
        journey.JourneyBranches[0].JourneySteps[1].QuestionForm.QuestionKey.Should().Be(request.QuestionKey);
        _mdpDbUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
    }

    public async Task SubmitQuestionStep_ReturnsBadRequest_WhenCurrentPageKeyIsWrong()
    {
        var journey = new RetirementJourneyBuilder()
            .BuildWithSteps()
            .SetCalculation(new CalculationBuilder().BuildV2());

        _retirementJourneyRepositoryMock
            .Setup(x => x.FindUnexpiredJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(journey);

        var request = new SubmitQuestionStepRequest
        {
            CurrentPageKey = "current",
            NextPageKey = "next",
            QuestionKey = "questionId",
            AnswerKey = "answer"
        };

        var result = await _sut.SubmitQuestionStep(request);

        result.Should().BeOfType<BadRequestObjectResult>();
        (((BadRequestObjectResult)result).Value as ApiError).Errors[0].Message.Should().Be("Invalid \"currentPageKey\"");
    }

    public async Task SubmitFinancialAdviseDate_ReturnsNoContent()
    {
        var journey = new RetirementJourneyBuilder()
            .BuildWithSteps()
            .SetCalculation(new CalculationBuilder().BuildV2());

        _retirementJourneyRepositoryMock
            .Setup(x => x.FindUnexpiredJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(journey);

        var request = new SubmitFinancialAdviseRequest
        {
            FinancialAdviseDate = DateTimeOffset.UtcNow.AddHours(-1),
        };

        var result = await _sut.SubmitFinancialAdviseDate(request);

        result.Should().BeOfType<NoContentResult>();
        journey.FinancialAdviseDate.Should().Be(request.FinancialAdviseDate);
        _mdpDbUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
    }

    public async Task SubmitFinancialAdviseDate_ReturnsBadRequest_WhenDateIsGreaterThanToday()
    {
        var journey = new RetirementJourneyBuilder()
            .BuildWithSteps()
            .SetCalculation(new CalculationBuilder().BuildV2());

        _retirementJourneyRepositoryMock
            .Setup(x => x.FindUnexpiredJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(journey);

        var request = new SubmitFinancialAdviseRequest
        {
            FinancialAdviseDate = DateTimeOffset.UtcNow.AddDays(1),
        };

        var result = await _sut.SubmitFinancialAdviseDate(request);

        result.Should().BeOfType<BadRequestObjectResult>();
        (((BadRequestObjectResult)result).Value as ApiError).Errors[0].Message.Should().Be("Financial advise date should be less than or equal to today.");
    }

    public async Task SubmitPensionWiseDate_ReturnsNoContent()
    {
        var journey = new RetirementJourneyBuilder()
            .BuildWithSteps()
            .SetCalculation(new CalculationBuilder().BuildV2());

        _retirementJourneyRepositoryMock
            .Setup(x => x.FindUnexpiredJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(journey);

        var request = new SubmitPensionWiseRequest
        {
            PensionWiseDate = DateTimeOffset.UtcNow.AddHours(-1),
        };

        var result = await _sut.SubmitPensionWiseDate(request);

        result.Should().BeOfType<NoContentResult>();
        journey.PensionWiseDate.Should().Be(request.PensionWiseDate);
        _mdpDbUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
    }

    public async Task SubmitPensionWiseDate_ReturnsBadRequest_WhenDateIsGreaterThanToday()
    {
        var journey = new RetirementJourneyBuilder()
            .BuildWithSteps()
            .SetCalculation(new CalculationBuilder().BuildV2());

        _retirementJourneyRepositoryMock
            .Setup(x => x.FindUnexpiredJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(journey);

        var request = new SubmitPensionWiseRequest
        {
            PensionWiseDate = DateTimeOffset.UtcNow.AddDays(1),
        };

        var result = await _sut.SubmitPensionWiseDate(request);

        result.Should().BeOfType<BadRequestObjectResult>();
        (((BadRequestObjectResult)result).Value as ApiError).Errors[0].Message.Should().Be("Pension wise date should be less than or equal to today.");
    }

    public async Task FinancialAdvise_ReturnsOkObjectResult()
    {
        var financialAdviseDate = DateTimeOffset.UtcNow.AddDays(-1);
        var journey = new RetirementJourneyBuilder()
            .BuildWithSteps()
            .SetCalculation(new CalculationBuilder().BuildV2());

        journey.SetFinancialAdviseDate(financialAdviseDate);

        _retirementJourneyRepositoryMock
            .Setup(x => x.FindUnexpiredJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(journey);

        var result = await _sut.FinancialAdvise();

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as FinancialAdviseResponse;
        response.FinancialAdviseDate.Should().Be(journey.FinancialAdviseDate);
    }

    public async Task FinancialAdvise_ReturnsNotFound()
    {
        _retirementJourneyRepositoryMock
            .Setup(x => x.FindUnexpiredJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(Option<RetirementJourney>.None);

        var result = await _sut.FinancialAdvise();

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task PensionWise_ReturnsOkObjectResult()
    {
        var pensionWiseDate = DateTimeOffset.UtcNow.AddDays(-1);
        var journey = new RetirementJourneyBuilder()
            .BuildWithSteps()
            .SetCalculation(new CalculationBuilder().BuildV2());

        journey.SetPensionWiseDate(pensionWiseDate);

        _retirementJourneyRepositoryMock
            .Setup(x => x.FindUnexpiredJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(journey);

        var result = await _sut.PensionWise();

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as PensionWiseResponse;
        response.PensionWiseDate.Should().Be(journey.PensionWiseDate);
    }

    public async Task PensionWise_ReturnsNotFound()
    {
        _retirementJourneyRepositoryMock
            .Setup(x => x.FindUnexpiredJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(Option<RetirementJourney>.None);

        var result = await _sut.PensionWise();

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task SubmitLifetimeAllowance_ReturnsNoContent()
    {
        var journey = new RetirementJourneyBuilder()
            .BuildWithSteps()
            .SetCalculation(new CalculationBuilder().BuildV2());

        var request = new SubmitLifetimeAllowanceRequest
        {
            Percentage = 85
        };

        _retirementJourneyRepositoryMock
            .Setup(x => x.FindUnexpiredJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(journey);

        var result = await _sut.SubmitLifetimeAllowance(request);

        result.Should().BeOfType<NoContentResult>();
        journey.EnteredLtaPercentage.Should().Be(request.Percentage);
        _mdpDbUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
    }

    public async Task SubmitLifetimeAllowance_ReturnsBadRequest_WhenPercentageIsWrong()
    {
        var journey = new RetirementJourneyBuilder()
            .BuildWithSteps()
            .SetCalculation(new CalculationBuilder().BuildV2());

        _retirementJourneyRepositoryMock
            .Setup(x => x.FindUnexpiredJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(journey);

        var request = new SubmitLifetimeAllowanceRequest
        {
            Percentage = 1001
        };

        var result = await _sut.SubmitLifetimeAllowance(request);

        result.Should().BeOfType<BadRequestObjectResult>();
        (((BadRequestObjectResult)result).Value as ApiError).Errors[0].Message.Should().Be("Percentage should be between 1 and 1000 and should have only 2 digits after dot.");
    }

    public async Task SubmitOptOutPensionWise_ReturnsNoContent()
    {
        var journey = new RetirementJourneyBuilder()
            .BuildWithSteps()
            .SetCalculation(new CalculationBuilder().BuildV2());

        var request = new SubmitOptOutPensionWiseRequest
        {
            OptOutPensionWise = true
        };

        _retirementJourneyRepositoryMock
            .Setup(x => x.FindUnexpiredJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(journey);

        var result = await _sut.SubmitOptOutPensionWise(request);

        result.Should().BeOfType<NoContentResult>();
        journey.OptOutPensionWise.Should().Be(request.OptOutPensionWise);
        _mdpDbUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
    }

    public async Task PensionWiseOptOut_ReturnsOkObjectResult()
    {
        var journey = new RetirementJourneyBuilder()
            .BuildWithSteps()
            .SetCalculation(new CalculationBuilder().BuildV2());

        journey.SetOptOutPensionWise(true);

        _retirementJourneyRepositoryMock
            .Setup(x => x.FindUnexpiredJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(journey);

        var result = await _sut.PensionWiseOptOut();

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as OptOutPensionWiseResponse;
        response.OptOutPensionWise.Should().Be(journey.OptOutPensionWise);
    }

    public async Task PensionWiseOptOut_ReturnsNotFound()
    {
        _retirementJourneyRepositoryMock
            .Setup(x => x.FindUnexpiredJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(Option<RetirementJourney>.None);

        var result = await _sut.PensionWiseOptOut();

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task QuestionForm_ReturnsOkObjectResult()
    {
        var currentPageKey = "step2";
        var journey = new RetirementJourneyBuilder()
            .BuildWithSteps()
            .SetCalculation(new CalculationBuilder().BuildV2());

        journey.TrySubmitStep(currentPageKey, "step3", DateTimeOffset.UtcNow, "questionKey", "answerKey", "answerValue");

        _retirementJourneyRepositoryMock
            .Setup(x => x.FindUnexpiredJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(journey);

        var result = await _sut.QuestionForm(currentPageKey);

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as QuestionFormResponse;
        response.QuestionKey.Should().Be(journey.JourneyBranches[0].JourneySteps[1].QuestionForm.QuestionKey);
        response.AnswerKey.Should().Be(journey.JourneyBranches[0].JourneySteps[1].QuestionForm.AnswerKey);
        response.AnswerValue.Should().Be(journey.JourneyBranches[0].JourneySteps[1].QuestionForm.AnswerValue);
    }

    public async Task QuestionForm_ReturnsNotFound()
    {
        var currentPageKey = "step2";
        _retirementJourneyRepositoryMock
            .Setup(x => x.FindUnexpiredJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(Option<RetirementJourney>.None);

        var result = await _sut.QuestionForm(currentPageKey);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task LifetimeAllowance_ReturnsOkObjectResult()
    {
        var journey = new RetirementJourneyBuilder()
            .BuildWithSteps()
            .SetCalculation(new CalculationBuilder().BuildV2());

        journey.TrySubmitStep("step2", "lta_enter_amount", DateTimeOffset.UtcNow);
        journey.TrySubmitStep("lta_enter_amount", "step4", DateTimeOffset.UtcNow);
        journey.SetEnteredLtaPercentage(85M);

        _retirementJourneyRepositoryMock
            .Setup(x => x.FindUnexpiredJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(journey);

        var result = await _sut.LifetimeAllowance();

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as LifetimeAllowanceResponse;
        response.Percentage.Should().Be(journey.EnteredLtaPercentage);
    }

    public async Task LifetimeAllowance_ReturnsNotFound()
    {
        _retirementJourneyRepositoryMock
            .Setup(x => x.FindUnexpiredJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(Option<RetirementJourney>.None);

        var result = await _sut.LifetimeAllowance();

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task GetJourneyAnswers_ReturnsOkObjectResult()
    {
        var journey = new RetirementJourneyBuilder()
            .BuildWithSteps()
            .SetCalculation(new CalculationBuilder().BuildV2());

        journey.TrySubmitStep("step2", "step3", DateTimeOffset.UtcNow, "questionKey", "answerKey", "answerValue");

        _retirementJourneyRepositoryMock
            .Setup(x => x.FindUnexpiredJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(journey);

        var result = await _sut.GetJourneyAnswers(new string[] { "questionKey" });

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as IEnumerable<QuestionFormResponse>;
        var responseList = response.ToList();
        responseList[0].QuestionKey.Should().BeEquivalentTo(journey.JourneyBranches[0].JourneySteps[1].QuestionForm.QuestionKey);
        responseList[0].AnswerKey.Should().BeEquivalentTo(journey.JourneyBranches[0].JourneySteps[1].QuestionForm.AnswerKey);
        responseList[0].AnswerValue.Should().BeEquivalentTo(journey.JourneyBranches[0].JourneySteps[1].QuestionForm.AnswerValue);
    }

    public async Task GetJourneyAnswers_ReturnsNotFound()
    {
        _retirementJourneyRepositoryMock
            .Setup(x => x.FindUnexpiredJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(Option<RetirementJourney>.None);

        var result = await _sut.GetJourneyAnswers(new string[] { "questionKey" });

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task CheckJourneyIntegrity_ReturnsOkObjectResult()
    {
        var journey = new RetirementJourneyBuilder()
            .BuildWithSteps()
            .SetCalculation(new CalculationBuilder().BuildV2());

        journey.TrySubmitStep("step2", "step3", DateTimeOffset.UtcNow, "questionKey", "answerKey", "answerValue");

        _retirementJourneyRepositoryMock
            .Setup(x => x.FindUnexpiredJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(journey);

        var result = await _sut.CheckJourneyIntegrity("step2");

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as IntegrityResponse;
        response.RedirectStepPageKey.Should().Be("step2");
    }

    public async Task CheckJourneyIntegrity_ReturnsOkObjectResult_LastStepNextPageKey()
    {
        var journey = new RetirementJourneyBuilder()
            .BuildWithSteps()
            .SetCalculation(new CalculationBuilder().BuildV2());

        journey.TrySubmitStep("step2", "step3", DateTimeOffset.UtcNow, "questionKey", "answerKey", "answerValue");

        _retirementJourneyRepositoryMock
            .Setup(x => x.FindUnexpiredJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(journey);

        var result = await _sut.CheckJourneyIntegrity("step3");

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as IntegrityResponse;
        response.RedirectStepPageKey.Should().Be("step3");
    }

    public async Task CheckJourneyIntegrity_ReturnsNotFound()
    {
        var currentPageKey = "step2";
        _retirementJourneyRepositoryMock
            .Setup(x => x.FindUnexpiredJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(Option<RetirementJourney>.None);

        var result = await _sut.CheckJourneyIntegrity(currentPageKey);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task RetirementApplicationData_ReturnsOkObjectResult()
    {
        var journey = new RetirementJourneyBuilder()
            .BuildWithSteps()
            .SetCalculation(new CalculationBuilder().BuildV2());

        journey.TrySubmitStep("step2", "step3", DateTimeOffset.UtcNow, "questionKey", "answerKey", "answerValue");

        _retirementJourneyRepositoryMock
            .Setup(x => x.FindUnexpiredOrSubmittedJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(journey);

        _contentClientMock
            .Setup(x => x.FindRetirementOptions(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new JsonElement());

        _retirementApplicationQuotesV2Mock
            .Setup(x => x.GetSummaryFigures(It.IsAny<RetirementJourney>(), It.IsAny<JsonElement>()))
            .ReturnsAsync(new RetirementSummary());

        var result = await _sut.RetirementApplicationData("{}");

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as RetirementApplicationResponse;
        response.Label.Should().Be(journey.MemberQuote.Label);
        response.RetirementApplicationStatus.Should().Be(RetirementApplicationStatus.StartedRA);
        response.SubmissionDate.Should().BeNull();
    }

    public async Task RetirementApplicationData_ReturnsNotFound()
    {
        _retirementJourneyRepositoryMock
            .Setup(x => x.FindUnexpiredOrSubmittedJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(Option<RetirementJourney>.None);

        var result = await _sut.RetirementApplicationData("{}");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task DownloadSummaryPdf_ReturnsFileContentResult()
    {
        var journey = new RetirementJourneyBuilder()
            .BuildWithSteps()
            .SetCalculation(new CalculationBuilder().BuildV2());

        journey.Submit(new byte[10], DateTimeOffset.UtcNow, "case_number");

        _retirementJourneyRepositoryMock
            .Setup(x => x.FindUnexpiredOrSubmittedJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(journey);

        var result = await _sut.DownloadSummaryPdf();
        result.Should().BeOfType<FileContentResult>();
    }

    public async Task DownloadSummaryPdf_ReturnsNotFound()
    {
        _retirementJourneyRepositoryMock
            .Setup(x => x.FindUnexpiredOrSubmittedJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(Option<RetirementJourney>.None);

        var result = await _sut.DownloadSummaryPdf();
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task SaveGbgId_ReturnsNoContent()
    {
        var guid = Guid.NewGuid();
        var journey = new RetirementJourneyBuilder()
            .BuildWithSteps()
            .SetCalculation(new CalculationBuilder().BuildV2());

        _retirementJourneyRepositoryMock
            .Setup(x => x.FindUnexpiredJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(journey);

        var result = await _sut.SaveGbgId(guid);

        result.Should().BeOfType<NoContentResult>();
        journey.GbgId.Should().Be(guid);
        _mdpDbUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
    }

    public async Task SaveGbgId_ReturnsNotFound()
    {
        var guid = Guid.NewGuid();
        _retirementJourneyRepositoryMock
            .Setup(x => x.FindUnexpiredJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(Option<RetirementJourney>.None);

        var result = await _sut.SaveGbgId(guid);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task AddPostSubmissionDocuments_ReturnsNoContent_WhenDocumentsAreSuccessfullyPostIndexed()
    {
        var journey = new RetirementJourneyBuilder()
            .BuildWithSteps()
            .SetCalculation(new CalculationBuilder().BuildV2());

        journey.Submit(new byte[10], DateTimeOffset.UtcNow, "case_number");

        _journeyDocumentsHandlerServiceMock
            .Setup(x => x.PostIndex(It.IsAny<string>(), It.IsAny<string>(), journey.CaseNumber, MdpConstants.JourneyTypeRetirement))
            .ReturnsAsync(Right<Error, Unit>(Unit.Default));

        _retirementJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(journey);

        var result = await _sut.AddPostSubmissionDocuments();

        result.Should().BeOfType<NoContentResult>();
        _mdpDbUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
    }

    public async Task AddPostSubmissionDocuments_ReturnsBadRequest_WhenCaseNumberIsNotSubmitted()
    {
        var journey = new RetirementJourneyBuilder()
            .BuildWithSteps()
            .SetCalculation(new CalculationBuilder().BuildV2());

        _retirementJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(journey);

        _journeyDocumentsHandlerServiceMock
            .Setup(x => x.PostIndex(It.IsAny<string>(), It.IsAny<string>(), null, MdpConstants.JourneyTypeRetirement))
            .ReturnsAsync(Left<Error, Unit>(Error.New($"{MdpConstants.JourneyTypeRetirement} case must be sumbitted.")));

        var result = await _sut.AddPostSubmissionDocuments();

        result.Should().BeOfType<BadRequestObjectResult>();
        (((BadRequestObjectResult)result).Value as ApiError).Errors[0].Message.Should().Be("retirement case must be sumbitted.");
    }

    public async Task AddPostSubmissionDocuments_ReturnsBadRequest_WhenNoDocumentsFound()
    {
        var caseNumber = "case_number";

        var journey = new RetirementJourneyBuilder()
            .BuildWithSteps()
            .SetCalculation(new CalculationBuilder().BuildV2());

        journey.Submit(new byte[10], DateTimeOffset.UtcNow, caseNumber);

        _retirementJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(journey);

        _journeyDocumentsHandlerServiceMock
          .Setup(x => x.PostIndex(It.IsAny<string>(), It.IsAny<string>(), caseNumber, MdpConstants.JourneyTypeRetirement))
          .ReturnsAsync(Left<Error, Unit>(Error.New("Cannot find any documents to postindex.")));

        var result = await _sut.AddPostSubmissionDocuments();

        result.Should().BeOfType<BadRequestObjectResult>();
        (((BadRequestObjectResult)result).Value as ApiError).Errors[0].Message.Should().Be("Cannot find any documents to postindex.");
    }

    public async Task AddPostSubmissionDocuments_ReturnsBadRequest_WhenPostIndexingFails()
    {
        var caseNumber = "case_number";

        var journey = new RetirementJourneyBuilder()
            .BuildWithSteps()
            .SetCalculation(new CalculationBuilder().BuildV2());

        journey.Submit(new byte[10], DateTimeOffset.UtcNow, caseNumber);

        _retirementJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(journey);

        _journeyDocumentsHandlerServiceMock
         .Setup(x => x.PostIndex(It.IsAny<string>(), It.IsAny<string>(), caseNumber, MdpConstants.JourneyTypeRetirement))
         .ReturnsAsync(Left<Error, Unit>(Error.New("Error message: Postindexing failed.")));

        var result = await _sut.AddPostSubmissionDocuments();

        result.Should().BeOfType<BadRequestObjectResult>();
        (((BadRequestObjectResult)result).Value as ApiError).Errors[0].Message.Should().Be("Error message: Postindexing failed.");
    }

    public async Task AddPostSubmissionDocuments_ReturnsNotFound_WhenJourneyNotFound()
    {
        _retirementJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<RetirementJourney>.None);

        var result = await _sut.AddPostSubmissionDocuments();

        result.Should().BeOfType<NotFoundObjectResult>();
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