using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Extensions;
using WTW.MdpService.Domain.Common.Journeys;
using WTW.TestCommon.FixieConfig;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Domain.Common;

public class JourneyBranchTest
{
    public void CreatesInitialJourneyBranch()
    {
        var questionForm = new QuestionForm("test_question_key", "test_answer_key");
        var journeyStep = JourneyStep.Create("retirement_start", "next_page_test", DateTimeOffset.UtcNow, questionForm);
        var sut = JourneyBranch.Create(journeyStep);

        sut.JourneySteps.Should().Contain(journeyStep);
        sut.IsActive.Should().BeTrue();
    }

    public void ReturnTrueIfNewBranchShouldBeCreateForStep()
    {
        var questionForm = new QuestionForm("test_question_key", "test_answer_key");
        var journeyStep = JourneyStep.Create("retirement_start", "next_page_test", DateTimeOffset.UtcNow, questionForm);
        var sut = JourneyBranch.Create(journeyStep);

        var result = sut.ShouldCreateNewBranch("retirement_start");

        result.Right().Should().BeTrue();
    }

    public void ReturnFalseIfNoNewBranchShouldBeCreateForStep()
    {
        var questionForm = new QuestionForm("test_question_key", "test_answer_key");
        var journeyStep = JourneyStep.Create("retirement_start", "next_page_test", DateTimeOffset.UtcNow, questionForm);
        var sut = JourneyBranch.Create(journeyStep);

        var result = sut.ShouldCreateNewBranch("next_page_test");

        result.Right().Should().BeFalse();
    }

    public void ReturnErrorIfSubmittedPageKeyIsInvalid()
    {
        var questionForm = new QuestionForm("test_question_key", "test_answer_key");
        var journeyStep = JourneyStep.Create("retirement_start", "next_page_test", DateTimeOffset.UtcNow, questionForm);
        var sut = JourneyBranch.Create(journeyStep);

        var result = sut.ShouldCreateNewBranch("next_page_test_random");

        result.Left().Message.Should().Be("Invalid \"currentPageKey\"");
    }

    public void ReturnsNewBranchFromBaseBranchWithStepAddedBeforeCurrentPageKey()
    {
        var questionForm = new QuestionForm("test_question_key", "test_answer_key");
        var sut = JourneyBranch.Create(JourneyStep.Create("retirement_start", "next_page_test_1", DateTimeOffset.UtcNow, questionForm));
        sut.SubmitStep(JourneyStep.Create("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow, questionForm));
        sut.SubmitStep(JourneyStep.Create("retirement_start_3", "next_page_test_4", DateTimeOffset.UtcNow, questionForm));
        sut.SubmitStep(JourneyStep.Create("retirement_start_4", "next_page_test_5", DateTimeOffset.UtcNow, questionForm));
        sut.SubmitStep(JourneyStep.Create("retirement_start_5", "next_page_test_6", DateTimeOffset.UtcNow, questionForm));

        var result = JourneyBranch.CreateFromBase(sut, "retirement_start_4");

        result.JourneySteps.Count.Should().Be(3);
        result.JourneySteps.Should().NotBeSameAs(sut.JourneySteps);
    }

    public void ReturnsNewBranchFromBaseBranchWithStepAddedBeforeCurrentPageKey_InitialStepCheck()
    {
        var questionForm = new QuestionForm("test_question_key", "test_answer_key");
        var sut = JourneyBranch.Create(JourneyStep.Create("retirement_start", "next_page_test_1", DateTimeOffset.UtcNow, questionForm));
        sut.SubmitStep(JourneyStep.Create("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow, questionForm));

        var result = JourneyBranch.CreateFromBase(sut, "retirement_start");

        result.JourneySteps.Count.Should().Be(0);
        result.JourneySteps.Should().NotBeSameAs(sut.JourneySteps);
    }

    public void ThrowsWhenNoNeedToCreateNewBranchAndSameBranchCouldBeUsed()
    {
        var questionForm = new QuestionForm("test_question_key", "test_answer_key");
        var sut = JourneyBranch.Create(JourneyStep.Create("retirement_start", "next_page_test_1", DateTimeOffset.UtcNow, questionForm));
        sut.SubmitStep(JourneyStep.Create("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow, questionForm));
        sut.SubmitStep(JourneyStep.Create("retirement_start_3", "next_page_test_4", DateTimeOffset.UtcNow, questionForm));
        sut.SubmitStep(JourneyStep.Create("retirement_start_4", "next_page_test_5", DateTimeOffset.UtcNow, questionForm));
        sut.SubmitStep(JourneyStep.Create("retirement_start_5", "next_page_test_6", DateTimeOffset.UtcNow, questionForm));

        var action = () => JourneyBranch.CreateFromBase(sut, "retirement_start_6");

        action.Should().Throw<InvalidOperationException>();
    }

    public void ThrowsWhenNoNeedToCreateNewBranchAndInvalidCurrentPageWasProvided()
    {
        var questionForm = new QuestionForm("test_question_key", "test_answer_key");
        var sut = JourneyBranch.Create(JourneyStep.Create("retirement_start", "next_page_test_1", DateTimeOffset.UtcNow, questionForm));
        sut.SubmitStep(JourneyStep.Create("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow, questionForm));

        var action = () => JourneyBranch.CreateFromBase(sut, "retirement_some_random_key");

        action.Should().Throw<InvalidOperationException>();
    }

    public void CanSubmitNewStep()
    {
        var questionForm = new QuestionForm("test_question_key", "test_answer_key");
        var sut = JourneyBranch.Create(JourneyStep.Create("retirement_start", "next_page_test_1", DateTimeOffset.UtcNow, questionForm));
        sut.SubmitStep(JourneyStep.Create("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow, questionForm));

        sut.JourneySteps.Count.Should().Be(2);
        sut.JourneySteps.SingleOrDefault(s => s.CurrentPageKey == "next_page_test_2" && s.NextPageKey == "next_page_test_3").Should().NotBeNull();
        sut.JourneySteps.SingleOrDefault(s => s.CurrentPageKey == "retirement_start" && s.NextPageKey == "next_page_test_1").Should().NotBeNull();
    }

    public void SkipSubmitNewStep_whenTheSameNextPageKeyExistInCurrentBranch()
    {
        var questionForm = new QuestionForm("test_question_key", "test_answer_key");
        var sut = JourneyBranch.Create(JourneyStep.Create("explore_options_2", "explore_options_2_t2", DateTimeOffset.UtcNow, questionForm));
        sut.SubmitStep(JourneyStep.Create("explore_options_2_t2", "explore_options_2_summary", DateTimeOffset.UtcNow, questionForm));
        sut.SubmitStep(JourneyStep.Create("explore_options_2_summary", "explore_options_2_t2", DateTimeOffset.UtcNow));

        sut.JourneySteps.Count.Should().Be(2);
        sut.JourneySteps.SingleOrDefault(s => s.CurrentPageKey == "explore_options_2" && s.NextPageKey == "explore_options_2_t2").Should().NotBeNull();
        sut.JourneySteps.SingleOrDefault(s => s.CurrentPageKey == "explore_options_2_t2" && s.NextPageKey == "explore_options_2_summary").Should().NotBeNull();
        sut.JourneySteps.SingleOrDefault(s => s.CurrentPageKey == "explore_options_2_summary" && s.NextPageKey == "explore_options_2_t2").Should().BeNull();
    }

    public void InitialBranchIsSetBranchAsActive()
    {
        var questionForm = new QuestionForm("test_question_key", "test_answer_key");
        var sut = JourneyBranch.Create(JourneyStep.Create("retirement_start", "next_page_test_1", DateTimeOffset.UtcNow, questionForm));

        sut.IsActive.Should().BeTrue();
    }

    public void CanSetBranchAsNotActive()
    {
        var questionForm = new QuestionForm("test_question_key", "test_answer_key");
        var sut = JourneyBranch.Create(JourneyStep.Create("retirement_start", "next_page_test_1", DateTimeOffset.UtcNow, questionForm));

        var result = sut.Deactivate();

        sut.IsActive.Should().BeFalse();
        result.Should().BeTrue();
    }

    public void CanSetBranchAsActive()
    {
        var questionForm = new QuestionForm("test_question_key", "test_answer_key");
        var sut = JourneyBranch.Create(JourneyStep.Create("retirement_start", "next_page_test_1", DateTimeOffset.UtcNow, questionForm));
        sut.Deactivate();

        var result = sut.Activate();

        sut.IsActive.Should().BeTrue();
        result.Should().BeTrue();
    }

    public void CanGetPreviousStep_WhenCurrentPageInTheMiddle()
    {
        var questionForm = new QuestionForm("test_question_key", "test_answer_key");
        var sut = JourneyBranch.Create(JourneyStep.Create("retirement_start", "next_page_test_2", DateTimeOffset.UtcNow, questionForm));
        sut.SubmitStep(JourneyStep.Create("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow, questionForm));
        sut.SubmitStep(JourneyStep.Create("next_page_test_3", "next_page_test_4", DateTimeOffset.UtcNow, questionForm));
        sut.SubmitStep(JourneyStep.Create("next_page_test_4", "next_page_test_5", DateTimeOffset.UtcNow, questionForm));
        sut.SubmitStep(JourneyStep.Create("next_page_test_5", "next_page_test_6", DateTimeOffset.UtcNow, questionForm));

        var result = sut.PreviousStepKey("next_page_test_3");

        result.IsSome.Should().BeTrue();
        result.Value().Should().Be("next_page_test_2");
    }

    [Input("next_page_test_5", 4)]
    [Input("next_page_test_3", 2)]
    [Input("retirement_start", 5)]
    [Input("random", 5)]
    public void CanRemoveFollowingStepsFromCertainPageKay(string pageKey, int expectedResult)
    {
        var questionForm = new QuestionForm("test_question_key", "test_answer_key");
        var step1 = JourneyStep.Create("retirement_start", "next_page_test_2", DateTimeOffset.UtcNow, questionForm);
        step1.UpdateSequenceNumber(1);
        var step2 = JourneyStep.Create("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow, questionForm);
        step2.UpdateSequenceNumber(2);
        var step3 = JourneyStep.Create("next_page_test_3", "next_page_test_4", DateTimeOffset.UtcNow, questionForm);
        step3.UpdateSequenceNumber(3);
        var step4 = JourneyStep.Create("next_page_test_4", "next_page_test_5", DateTimeOffset.UtcNow, questionForm);
        step4.UpdateSequenceNumber(4);
        var step5 = JourneyStep.Create("next_page_test_5", "next_page_test_6", DateTimeOffset.UtcNow, questionForm);
        step5.UpdateSequenceNumber(5);

        var sut = JourneyBranch.Create(step1);
        sut.SubmitStep(step2);
        sut.SubmitStep(step3);
        sut.SubmitStep(step4);
        sut.SubmitStep(step5);

        sut.RemoveStepsStartingWith(pageKey);

        sut.JourneySteps.Count.Should().Be(expectedResult);
    }

    public void CanReplaceAllStepsToSingleStep()
    {
        var questionForm = new QuestionForm("test_question_key", "test_answer_key");
        var step1 = JourneyStep.Create("transfer_start", "next_page_test_2", DateTimeOffset.UtcNow, questionForm);
        step1.UpdateSequenceNumber(1);
        var step2 = JourneyStep.Create("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow, questionForm);
        step2.UpdateSequenceNumber(2);
        var step3 = JourneyStep.Create("next_page_test_3", "next_page_test_4", DateTimeOffset.UtcNow, questionForm);
        step3.UpdateSequenceNumber(3);
        var step4 = JourneyStep.Create("next_page_test_4", "next_page_test_5", DateTimeOffset.UtcNow, questionForm);
        step4.UpdateSequenceNumber(4);
        var step5 = JourneyStep.Create("next_page_test_5", "next_page_test_6", DateTimeOffset.UtcNow, questionForm);
        step5.UpdateSequenceNumber(5);

        var sut = JourneyBranch.Create(step1);
        sut.SubmitStep(step2);
        sut.SubmitStep(step3);
        sut.SubmitStep(step4);
        sut.SubmitStep(step5);
        sut.JourneySteps.Count.Should().Be(5);
        var step = JourneyStep.Create("hub", "t2_guaranteed_value_2", DateTimeOffset.UtcNow);

        sut.ReplaceAllStepsTo(step);

        sut.JourneySteps.Count.Should().Be(1);
        sut.JourneySteps.Single().CurrentPageKey.Should().Be("hub");
        sut.JourneySteps.Single().NextPageKey.Should().Be("t2_guaranteed_value_2");
    }

    public void CanNotGetPreviousStep_WhenCurrentPageInTheBeginning()
    {
        var questionForm = new QuestionForm("test_question_key", "test_answer_key");
        var sut = JourneyBranch.Create(JourneyStep.Create("retirement_start", "next_page_test_1", DateTimeOffset.UtcNow, questionForm));
        sut.SubmitStep(JourneyStep.Create("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow, questionForm));
        sut.SubmitStep(JourneyStep.Create("retirement_start_3", "next_page_test_4", DateTimeOffset.UtcNow, questionForm));
        sut.SubmitStep(JourneyStep.Create("retirement_start_4", "next_page_test_5", DateTimeOffset.UtcNow, questionForm));
        sut.SubmitStep(JourneyStep.Create("retirement_start_5", "next_page_test_6", DateTimeOffset.UtcNow, questionForm));

        var result = sut.PreviousStep("retirement_start");

        result.IsSome.Should().BeFalse();
    }

    public void CanGetPreviousAndNextStepNavigations_WhenCurrentPageInTheEnd()
    {
        var questionForm = new QuestionForm("test_question_key", "test_answer_key");
        var sut = JourneyBranch.Create(JourneyStep.Create("retirement_start", "next_page_test_1", DateTimeOffset.UtcNow, questionForm));
        sut.SubmitStep(JourneyStep.Create("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow, questionForm));
        sut.SubmitStep(JourneyStep.Create("retirement_start_3", "next_page_test_4", DateTimeOffset.UtcNow, questionForm));
        sut.SubmitStep(JourneyStep.Create("retirement_start_4", "next_page_test_5", DateTimeOffset.UtcNow, questionForm));
        sut.SubmitStep(JourneyStep.Create("retirement_start_5", "next_page_test_6", DateTimeOffset.UtcNow, questionForm));

        var result = sut.PreviousStepKey("next_page_test_5");

        result.IsSome.Should().BeTrue();
        result.Value().Should().Be("retirement_start_4");
    }

    public void PreviousStepReturnsNone_WhenCurrentPageNotFound()
    {
        var questionForm = new QuestionForm("test_question_key", "test_answer_key");
        var sut = JourneyBranch.Create(JourneyStep.Create("retirement_start", "next_page_test_1", DateTimeOffset.UtcNow, questionForm));
        sut.SubmitStep(JourneyStep.Create("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow, questionForm));
        sut.SubmitStep(JourneyStep.Create("retirement_start_3", "next_page_test_4", DateTimeOffset.UtcNow, questionForm));
        sut.SubmitStep(JourneyStep.Create("retirement_start_4", "next_page_test_5", DateTimeOffset.UtcNow, questionForm));
        sut.SubmitStep(JourneyStep.Create("retirement_start_5", "next_page_test_6", DateTimeOffset.UtcNow, questionForm));

        var result = sut.PreviousStep("next_page_test_random");

        result.IsSome.Should().BeFalse();
    }

    public void CanUpdateNextPageKeyValue()
    {
        var sut = JourneyBranch.Create(JourneyStep.Create("retirement_start", "next_page_test_1", DateTimeOffset.UtcNow));
        sut.SubmitStep(JourneyStep.Create("next_page_test_2", "next_page_is_not_set", DateTimeOffset.UtcNow));
        var lastStep = sut.GetLastStep();

        var newStep = JourneyStep.Create("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow);
        sut.UpdateNextPageKeyValue(lastStep, newStep);

        lastStep.NextPageKey.Should().Be("next_page_test_3");
    }

    public void CanFindStepWithNextPageKeyValueNotSet()
    {
        var sut = JourneyBranch.Create(JourneyStep.Create("retirement_start", "next_page_test_1", DateTimeOffset.UtcNow));
        sut.SubmitStep(JourneyStep.Create("next_page_test_2", "next_page_is_not_set", DateTimeOffset.UtcNow));
        var lastStep = sut.GetLastStep();

        var newStep = JourneyStep.Create("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow);
        var result = sut.StepWithNextPageKeyNotSet(newStep);

        result.IsSome.Should().BeTrue();
        result.Value().Should().Be(lastStep);

    }

    public void CanGetQuestionForm_WhenFormExistForCurrentStep()
    {
        var questionForm = new QuestionForm("test_question_key", "test_answer_key");
        var sut = JourneyBranch.Create(JourneyStep.Create("retirement_start", "next_page_test_1", DateTimeOffset.UtcNow));
        sut.SubmitStep(JourneyStep.Create("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow));
        sut.SubmitStep(JourneyStep.Create("next_page_test_3", "next_page_test_4", DateTimeOffset.UtcNow));
        sut.SubmitStep(JourneyStep.Create("next_page_test_4", "next_page_test_5", DateTimeOffset.UtcNow, questionForm));
        sut.SubmitStep(JourneyStep.Create("next_page_test_5", "next_page_test_6", DateTimeOffset.UtcNow));

        var result = sut.QuestionForm("next_page_test_4");

        result.IsSome.Should().BeTrue();
        result.Value().AnswerKey.Should().Be("test_answer_key");
        result.Value().QuestionKey.Should().Be("test_question_key");
    }

    public void CanGetEmptyQuestionForm_WhenGettingLastStepForm()
    {
        var sut = JourneyBranch.Create(JourneyStep.Create("retirement_start", "next_page_test_1", DateTimeOffset.UtcNow));

        var result = sut.QuestionForm("next_page_test_1");

        result.IsSome.Should().BeTrue();
        result.Value().QuestionKey.Should().BeNull();
        result.Value().AnswerKey.Should().BeNull();
        result.Value().AnswerValue.Should().BeNull();
    }

    public void QuestionFormReturnsNone_WhenCurrentPageNotFound()
    {
        var questionForm = new QuestionForm("test_question_key", "test_answer_key");
        var sut = JourneyBranch.Create(JourneyStep.Create("retirement_start", "next_page_test_1", DateTimeOffset.UtcNow, questionForm));
        sut.SubmitStep(JourneyStep.Create("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow, questionForm));
        sut.SubmitStep(JourneyStep.Create("retirement_start_3", "next_page_test_4", DateTimeOffset.UtcNow, questionForm));
        sut.SubmitStep(JourneyStep.Create("retirement_start_4", "next_page_test_5", DateTimeOffset.UtcNow, questionForm));
        sut.SubmitStep(JourneyStep.Create("retirement_start_5", "next_page_test_6", DateTimeOffset.UtcNow, questionForm));

        var result = sut.QuestionForm("next_page_test_random");

        result.IsSome.Should().BeFalse();
    }

    public void QuestionFormReturnsNone_WhenCurrentStepDoesNotContainQuestionForm()
    {
        var questionForm = new QuestionForm("test_question_key", "test_answer_key");
        var sut = JourneyBranch.Create(JourneyStep.Create("retirement_start", "next_page_test_1", DateTimeOffset.UtcNow, questionForm));
        sut.SubmitStep(JourneyStep.Create("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow));
        sut.SubmitStep(JourneyStep.Create("next_page_test_3", "next_page_test_4", DateTimeOffset.UtcNow));
        sut.SubmitStep(JourneyStep.Create("next_page_test_4", "next_page_test_5", DateTimeOffset.UtcNow));
        sut.SubmitStep(JourneyStep.Create("next_page_test_5", "next_page_test_6", DateTimeOffset.UtcNow));

        var result = sut.QuestionForm("next_page_test_4");

        result.IsSome.Should().BeFalse();
    }

    public void QuestionFormReturnsNone_WhenStepDoesNotHaveQuestionForm()
    {
        var sut = JourneyBranch.Create(JourneyStep.Create("retirement_start", "next_page_test_1", DateTimeOffset.UtcNow));
        sut.SubmitStep(JourneyStep.Create("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow));

        var result = sut.QuestionForms();

        result.Count().Should().Be(0);
    }

    public void QuestionFormReturnsAll()
    {
        var questionForm1 = new QuestionForm("question_key_1", "question_answer_1");
        var questionForm2 = new QuestionForm("question_key_2", "question_answer_2");

        var sut = JourneyBranch.Create(JourneyStep.Create("retirement_start", "next_page_test_1", DateTimeOffset.UtcNow, questionForm1));
        sut.SubmitStep(JourneyStep.Create("next_page_test_1", "next_page_test_2", DateTimeOffset.UtcNow, questionForm2));

        var result = sut.QuestionForms().ToList();

        result.Count.Should().Be(2);
        result[0].QuestionKey.Should().Be(questionForm1.QuestionKey);
        result[0].AnswerKey.Should().Be(questionForm1.AnswerKey);
        result[1].QuestionKey.Should().Be(questionForm2.QuestionKey);
        result[1].AnswerKey.Should().Be(questionForm2.AnswerKey);
    }

    public void RetrievesLastSubmittedStep()
    {
        var step1 = JourneyStep.Create("retirement_start", "next_page_test_1", DateTimeOffset.UtcNow);
        var step2 = JourneyStep.Create("next_page_test_1", "next_page_test_2", DateTimeOffset.UtcNow.AddMicroseconds(1));
        step2.UpdateSequenceNumber(2);
        var step3 = JourneyStep.Create("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow.AddMicroseconds(2), new QuestionForm("qKey", "aKey"));
        step3.UpdateSequenceNumber(3);
        var step4 = JourneyStep.Create("next_page_test_3", "next_page_test_4", DateTimeOffset.UtcNow.AddMicroseconds(3));
        step4.UpdateSequenceNumber(4);
        var sut = JourneyBranch.Create(step1);
        sut.SubmitStep(step2);
        sut.SubmitStep(step3);
        sut.SubmitStep(step4);

        var result = sut.LastStep();

        result.Should().Be(step4);
    }

    public void RetrievesFirstSubmittedStep()
    {
        var step1 = JourneyStep.Create("retirement_start", "next_page_test_1", DateTimeOffset.UtcNow);
        var step2 = JourneyStep.Create("next_page_test_1", "next_page_test_2", DateTimeOffset.UtcNow.AddMicroseconds(1));
        var step3 = JourneyStep.Create("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow.AddMicroseconds(2), new QuestionForm("qKey", "aKey"));
        var step4 = JourneyStep.Create("next_page_test_3", "next_page_test_4", DateTimeOffset.UtcNow.AddMicroseconds(3));
        var sut = JourneyBranch.Create(step1);
        sut.SubmitStep(step2);
        sut.SubmitStep(step3);
        sut.SubmitStep(step4);

        var result = sut.FirstStep();

        result.Should().Be(step1);
    }

    public void ReturnsTrue_WhenLifetimeAllowanceStepExists()
    {
        var step1 = JourneyStep.Create("retirement_start", "next_page_test_1", DateTimeOffset.UtcNow);
        var step2 = JourneyStep.Create("next_page_test_1", "lta_enter_amount", DateTimeOffset.UtcNow.AddMicroseconds(1));
        var step3 = JourneyStep.Create("lta_enter_amount", "next_page_test_3", DateTimeOffset.UtcNow.AddMicroseconds(2), new QuestionForm("qKey", "aKey"));
        var step4 = JourneyStep.Create("next_page_test_3", "next_page_test_4", DateTimeOffset.UtcNow.AddMicroseconds(3));
        var sut = JourneyBranch.Create(step1);
        sut.SubmitStep(step2);
        sut.SubmitStep(step3);
        sut.SubmitStep(step4);

        var result = sut.HasLifetimeAllowance();

        result.Should().BeTrue();
    }

    public void ReturnsFalse_WhenLifetimeAllowanceStepDoesNotExist()
    {
        var step1 = JourneyStep.Create("retirement_start", "next_page_test_1", DateTimeOffset.UtcNow);
        var step2 = JourneyStep.Create("next_page_test_1", "next_page_test_2", DateTimeOffset.UtcNow.AddMicroseconds(1));
        var step3 = JourneyStep.Create("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow.AddMicroseconds(2), new QuestionForm("qKey", "aKey"));
        var step4 = JourneyStep.Create("next_page_test_3", "lta_enter_amount", DateTimeOffset.UtcNow.AddMicroseconds(3));
        var sut = JourneyBranch.Create(step1);
        sut.SubmitStep(step2);
        sut.SubmitStep(step3);
        sut.SubmitStep(step4);

        var result = sut.HasLifetimeAllowance();

        result.Should().BeFalse();
    }

    public void CanUpdateSequenceNumber()
    {
        var step1 = JourneyStep.Create("retirement_start", "next_page_test_1", DateTimeOffset.UtcNow);
        var step2 = JourneyStep.Create("next_page_test_1", "next_page_test_2", DateTimeOffset.UtcNow.AddMicroseconds(1));
        var step3 = JourneyStep.Create("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow.AddMicroseconds(2), new QuestionForm("qKey", "aKey"));
        var step4 = JourneyStep.Create("next_page_test_3", "lta_enter_amount", DateTimeOffset.UtcNow.AddMicroseconds(3));
        var sut = JourneyBranch.Create(step1);
        sut.SubmitStep(step2);
        sut.SubmitStep(step3);
        sut.SubmitStep(step4);

        sut.SequenceNumber.Should().Be(1);

        sut.UpdateSequenceNumber(2);

        sut.SequenceNumber.Should().Be(2);
    }

    public void MarksNextPageAsDeadEnd()
    {
        var step1 = JourneyStep.Create("retirement_start", "next_page_test_1", DateTimeOffset.UtcNow);
        var step2 = JourneyStep.Create("next_page_test_1", "next_page_test_2", DateTimeOffset.UtcNow.AddMicroseconds(1));
        var step3 = JourneyStep.Create("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow.AddMicroseconds(2), new QuestionForm("qKey", "aKey"));
        var step4 = JourneyStep.Create("next_page_test_3", "lta_enter_amount", DateTimeOffset.UtcNow.AddMicroseconds(3));
        var sut = JourneyBranch.Create(step1);
        sut.SubmitStep(step2);
        sut.SubmitStep(step3);
        sut.SubmitStep(step4);

        sut.MarkNextPageAsDeadEnd("next_page_test_2");

        sut.JourneySteps.Single(x => x.CurrentPageKey == "next_page_test_2").IsNextPageAsDeadEnd.Should().BeTrue();
        sut.JourneySteps.Where(x => x.CurrentPageKey != "next_page_test_2").All(x => !x.IsNextPageAsDeadEnd).Should().BeTrue();
    }

    public void RemovesDeadEndandFollowingPages()
    {
        var step1 = JourneyStep.Create("retirement_start", "next_page_test_1", DateTimeOffset.UtcNow);
        var step2 = JourneyStep.Create("next_page_test_1", "next_page_test_2", DateTimeOffset.UtcNow.AddMicroseconds(1));
        step2.UpdateSequenceNumber(2);
        var step3 = JourneyStep.Create("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow.AddMicroseconds(2), new QuestionForm("qKey", "aKey"));
        step3.UpdateSequenceNumber(3);
        var step4 = JourneyStep.Create("next_page_test_3", "lta_enter_amount", DateTimeOffset.UtcNow.AddMicroseconds(3));
        step4.UpdateSequenceNumber(4);
        var sut = JourneyBranch.Create(step1);
        sut.SubmitStep(step2);
        sut.SubmitStep(step3);
        sut.SubmitStep(step4);

        sut.JourneySteps.Count().Should().Be(4);
        sut.MarkNextPageAsDeadEnd("next_page_test_2");
        sut.RemoveDeadEndSteps();

        sut.JourneySteps.Count().Should().Be(2);
        sut.JourneySteps.Max(x => x.SequenceNumber).Should().Be(2);
    }

    public void ReturnsCorrectStepByKey()
    {
        var now = DateTimeOffset.UtcNow;
        var step1 = JourneyStep.Create("next_page_test_1", "next_page_test_2", now.AddDays(-34));
        var sut = JourneyBranch.Create(step1);

        var result = sut.GetStepByKey("next_page_test_1");

        result.IsSome.Should().BeTrue();
        result.Value().CurrentPageKey.Should().Be("next_page_test_1");
        result.Value().NextPageKey.Should().Be("next_page_test_2");
        result.Value().SubmitDate.Should().Be(now.AddDays(-34));
    }

    public void ReturnNoneWhenStepWithRequiredKeyDoesNotExist()
    {
        var now = DateTimeOffset.UtcNow;
        var step1 = JourneyStep.Create("retirement_start1", "next_page_test_11", now.AddDays(-35));
        var sut = JourneyBranch.Create(step1);

        var result = sut.GetStepByKey("next_page_test_1");

        result.IsNone.Should().BeTrue();
    }

    public void FindStepUsingCurrentPageKeysReturnsNone_WhenNoStepFound()
    {
        var now = DateTimeOffset.UtcNow;
        var step1 = JourneyStep.Create("retirement_start1", "next_page_test_11", now.AddDays(-35));
        var step2 = JourneyStep.Create("next_page_test_11", "next_page_test_22", now.AddDays(-35));
        var sut = JourneyBranch.Create(step1);
        sut.SubmitStep(step2);

        var result = sut.FindStepUsingCurrentPageKeys(new List<string> { "non-exist-1", "non-exist-2" });

        result.IsNone.Should().BeTrue();
    }

    public void FindStepUsingCurrentPageKeysReturnsStep_WhenStepFound()
    {
        var now = DateTimeOffset.UtcNow;
        var step1 = JourneyStep.Create("retirement_start1", "next_page_test_11", now.AddDays(-35));
        var step2 = JourneyStep.Create("next_page_test_11", "next_page_test_22", now.AddDays(-35));
        var sut = JourneyBranch.Create(step1);
        sut.SubmitStep(step2);

        var result = sut.FindStepUsingCurrentPageKeys(new List<string> { "retirement_start1", "next_page_test_111" });

        result.IsNone.Should().BeFalse();
        result.Value().CurrentPageKey.Should().Be("retirement_start1");
        result.Value().NextPageKey.Should().Be("next_page_test_11");
    }

    public void FindStepUsingCurrentPageKeysReturnsThroes_WhenMoreThanOneStepFound()
    {
        var now = DateTimeOffset.UtcNow;
        var step1 = JourneyStep.Create("retirement_start1", "next_page_test_11", now.AddDays(-35));
        var step2 = JourneyStep.Create("next_page_test_11", "next_page_test_22", now.AddDays(-35));
        var sut = JourneyBranch.Create(step1);
        sut.SubmitStep(step2);

        var action = () => sut.FindStepUsingCurrentPageKeys(new List<string> { "retirement_start1", "next_page_test_11" });

        action.Should().Throw<InvalidOperationException>();
    }

    public void FindStepUsingNextPageKeysReturnsNone_WhenNoStepFound()
    {
        var now = DateTimeOffset.UtcNow;
        var step1 = JourneyStep.Create("retirement_start1", "next_page_test_11", now.AddDays(-35));
        var step2 = JourneyStep.Create("next_page_test_11", "next_page_test_22", now.AddDays(-35));
        var sut = JourneyBranch.Create(step1);
        sut.SubmitStep(step2);

        var result = sut.FindStepUsingNextPageKeys(new List<string> { "non-exist-1", "non-exist-2" });

        result.IsNone.Should().BeTrue();
    }

    public void FindStepUsingNextPageKeysReturnsStep_WhenStepFound()
    {
        var now = DateTimeOffset.UtcNow;
        var step1 = JourneyStep.Create("retirement_start1", "next_page_test_11", now.AddDays(-35));
        var step2 = JourneyStep.Create("next_page_test_11", "next_page_test_22", now.AddDays(-35));
        var sut = JourneyBranch.Create(step1);
        sut.SubmitStep(step2);

        var result = sut.FindStepUsingNextPageKeys(new List<string> { "retirement_start1", "next_page_test_22" });

        result.IsNone.Should().BeFalse();
        result.Value().CurrentPageKey.Should().Be("next_page_test_11");
        result.Value().NextPageKey.Should().Be("next_page_test_22");
    }

    public void FindStepUsingNextPageKeysReturnsThroes_WhenMoreThanOneStepFound()
    {
        var now = DateTimeOffset.UtcNow;
        var step1 = JourneyStep.Create("retirement_start1", "next_page_test_11", now.AddDays(-35));
        var step2 = JourneyStep.Create("next_page_test_11", "next_page_test_22", now.AddDays(-35));
        var sut = JourneyBranch.Create(step1);
        sut.SubmitStep(step2);

        var action = () => sut.FindStepUsingNextPageKeys(new List<string> { "next_page_test_22", "next_page_test_11" });

        action.Should().Throw<InvalidOperationException>();
    }

    public void GetNextStepFromNone_WhenNoStepFound()
    {
        var now = DateTimeOffset.UtcNow;
        var step1 = JourneyStep.Create("retirement_start1", "next_page_test_11", now.AddDays(-35));
        var step2 = JourneyStep.Create("next_page_test_11", "next_page_test_22", now.AddDays(-35));
        var sut = JourneyBranch.Create(step1);
        sut.SubmitStep(step2);

        var result = sut.GetNextStepFrom(step2);

        result.IsNone.Should().BeTrue();
    }

    public void GetNextStepFromReturnsStep_WhenStepFound()
    {
        var now = DateTimeOffset.UtcNow;
        var step1 = JourneyStep.Create("retirement_start1", "next_page_test_11", now.AddDays(-35));
        var step2 = JourneyStep.Create("next_page_test_11", "next_page_test_22", now.AddDays(-35));
        var sut = JourneyBranch.Create(step1);
        sut.SubmitStep(step2);

        var result = sut.GetNextStepFrom(step1);

        result.IsNone.Should().BeFalse();
        result.Value().CurrentPageKey.Should().Be("next_page_test_11");
        result.Value().NextPageKey.Should().Be("next_page_test_22");
    }
}