using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using WTW.MdpService.Domain.Mdp;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Domain.Mdp.Journeys;

public class QuoteSelectionJourneyTest
{
    public void CreatesInitialQuoteSelectionJourney()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = new QuoteSelectionJourney("RBS", "0304442", now, "current", "next", "TestQouteName");

        sut.Should().NotBeNull();
        sut.BusinessGroup.Should().Be("RBS");
        sut.ReferenceNumber.Should().Be("0304442");
        sut.StartDate.Should().Be(now);
        sut.SubmissionDate.Should().BeNull();
        sut.ExpirationDate.Should().Be(DateTimeOffset.MinValue);
        sut.JourneyBranches.Should().NotBeEmpty();
        sut.JourneyBranches.ToList()[0].JourneySteps.Should().NotBeEmpty();
        sut.JourneyBranches.ToList()[0].JourneySteps.ToList()[0].CurrentPageKey.Should().Be("current");
        sut.JourneyBranches.ToList()[0].JourneySteps.ToList()[0].NextPageKey.Should().Be("next");
        sut.JourneyBranches.ToList()[0].JourneySteps.ToList()[0].QuestionForm.QuestionKey.Should().Be(QuoteSelectionJourney.QuestionKey);
        sut.JourneyBranches.ToList()[0].JourneySteps.ToList()[0].QuestionForm.AnswerKey.Should().Be("TestQouteName");
    }

    public void CanSubmitRetirementStepWithQuestionForm()
    {
        var currentPageKey = "retirement_start";
        var nextPageKey = "next_page_test";
        var date = DateTimeOffset.UtcNow;
        var answerKey = "TestQouteName";
        var sut = new QuoteSelectionJourney("RBS", "0304442", date, "current", currentPageKey, "TestQouteName");

        var result = sut.TrySubmitStep(currentPageKey, nextPageKey, date, "SelectedQuoteName", answerKey);

        result.Right().Should().Be(true);
        sut.JourneyBranches.Count.Should().Be(1);
        sut.JourneyBranches.Single().JourneySteps.Count.Should().Be(2);
        sut.JourneyBranches.Single().JourneySteps[1].CurrentPageKey.Should().Be(currentPageKey);
        sut.JourneyBranches.Single().JourneySteps[1].NextPageKey.Should().Be(nextPageKey);
        sut.JourneyBranches.Single().JourneySteps[1].SubmitDate.Should().Be(date);
        sut.JourneyBranches.Single().JourneySteps[1].QuestionForm.QuestionKey.Should().Be("SelectedQuoteName");
        sut.JourneyBranches.Single().JourneySteps[1].QuestionForm.AnswerKey.Should().Be(answerKey);
    }

    public void CanSubmitRetirementStepWithoutQuestionForm()
    {
        var date = DateTimeOffset.UtcNow;
        var sut = new QuoteSelectionJourney("RBS", "0304442", date.AddMilliseconds(-20), "current", "next_page_test", "TestQouteName");

        var result = sut.TrySubmitStep("next_page_test", "next_page_test1", date);

        result.Right().Should().Be(true);
        sut.JourneyBranches.Count.Should().Be(1);
        sut.JourneyBranches.Single().JourneySteps.Count.Should().Be(2);
        sut.JourneyBranches.Single().JourneySteps[1].CurrentPageKey.Should().Be("next_page_test");
        sut.JourneyBranches.Single().JourneySteps[1].NextPageKey.Should().Be("next_page_test1");
        sut.JourneyBranches.Single().JourneySteps[1].SubmitDate.Should().Be(date);
    }

    public void ReturnsCorrentQuoteSelection()
    {
        var date = DateTimeOffset.UtcNow;
        var sut = new QuoteSelectionJourney("RBS", "0304442", date.AddMilliseconds(-20), "current", "next_page_test", "TestQouteName1");
        sut.TrySubmitStep("next_page_test", "next_page_test1", date, QuoteSelectionJourney.QuestionKey, "TestQouteName1.TestQouteName2");

        var result = sut.QuoteSelection();

        result.IsSome.Should().Be(true);
        result.Value().Should().Be("TestQouteName1.TestQouteName2");
    }

    public void ReturnErrorMessageWhenInvalidCurrentStepProvided()
    {
        var date = DateTimeOffset.UtcNow;
        var sut = new QuoteSelectionJourney("RBS", "0304442", date, "current", "currentPageKey", "TestQouteName");

        var result = sut.TrySubmitStep("retirement_start_random", "next_page_test", DateTimeOffset.UtcNow, QuoteSelectionJourney.QuestionKey, "test_answer_key");

        result.Left().Message.Should().Be("Invalid \"currentPageKey\"");
        sut.JourneyBranches.Count.Should().Be(1);
        sut.JourneyBranches[0].JourneySteps.Count.Should().Be(1);
    }

    public void ThowsIfUnsuppurtedUpdateStepMethodIsUsed()
    {
        var date = DateTimeOffset.UtcNow;
        var sut = new QuoteSelectionJourney("RBS", "0304442", date, "current", "currentPageKey", "TestQouteName");

        var action = () => sut.UpdateStep("retirement_start_random", "next_page_test");

        action.Should().Throw<NotImplementedException>();
    }

    public void ThowsIfUnsuppurtedQuestionFormsMethodIsUsed()
    {
        var date = DateTimeOffset.UtcNow;
        var sut = new QuoteSelectionJourney("RBS", "0304442", date, "current", "currentPageKey", "TestQouteName");

        var action = () => sut.QuestionForms(new List<string> { "retirement_start_random", "next_page_test" });

        action.Should().Throw<NotImplementedException>();
    }

    public void PreviousStepReturnSome_WhenStepCouldBeFoundInActiveBranch()
    {
        var date = DateTimeOffset.UtcNow;
        var sut = new QuoteSelectionJourney("RBS", "0304442", date, "current", "next_page_test", "TestQouteName");
        sut.TrySubmitStep("next_page_test", "next_page_test_2", DateTimeOffset.UtcNow, "test_question_key", "test_answer_key");

        var result = sut.PreviousStep("next_page_test");

        result.IsSome.Should().BeTrue();
        result.Value().Should().Be("current");
    }

    public void PreviousStepReturnNone_WhenNoStepCouldBeFoundInActiveBranch()
    {
        var date = DateTimeOffset.UtcNow;
        var sut = new QuoteSelectionJourney("RBS", "0304442", date, "current", "next_page_test", "TestQouteName");
        sut.TrySubmitStep("next_page_test", "next_page_test_2", DateTimeOffset.UtcNow, QuoteSelectionJourney.QuestionKey, "TestQouteName1");
        sut.TrySubmitStep("next_page_tes_2", "next_page_test_3", DateTimeOffset.UtcNow, QuoteSelectionJourney.QuestionKey, "TestQouteName2");
        sut.TrySubmitStep("next_page_test", "next_page_test_2.1", DateTimeOffset.UtcNow, QuoteSelectionJourney.QuestionKey, "TestQouteName3");

        var result = sut.PreviousStep("next_page_test_2");

        result.IsNone.Should().BeTrue();
    }

    public void QuestionFormReturnSome_WhenActiveBranchExistAndCurrentStepHasQuestionForm()
    {
        var date = DateTimeOffset.UtcNow;
        var sut = new QuoteSelectionJourney("RBS", "0304442", date, "current", "next_page_test", "TestQouteName");
        sut.TrySubmitStep("next_page_test", "next_page_test_2", DateTimeOffset.UtcNow, QuoteSelectionJourney.QuestionKey, "TestQouteName1");
        sut.TrySubmitStep("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow, QuoteSelectionJourney.QuestionKey, "TestQouteName2");

        var result = sut.QuestionForm("next_page_test");

        result.IsSome.Should().BeTrue();
        result.Value().QuestionKey.Should().Be(QuoteSelectionJourney.QuestionKey);
        result.Value().AnswerKey.Should().Be("TestQouteName1");
    }

    public void QuestionFormReturnsEmpty_WhenStepIsLastInActiveBranch()
    {
        var date = DateTimeOffset.UtcNow;
        var sut = new QuoteSelectionJourney("RBS", "0304442", date, "current", "next_page_test", "TestQouteName");

        var result = sut.QuestionForm("next_page_test");

        result.IsSome.Should().BeTrue();
        result.Value().QuestionKey.Should().BeNull();
        result.Value().AnswerKey.Should().BeNull();
        result.Value().AnswerValue.Should().BeNull();
    }

    public void QuestionFormReturnNone_NoStepCouldBeFoundWithQuestionForm()
    {
        var date = DateTimeOffset.UtcNow;
        var sut = new QuoteSelectionJourney("RBS", "0304442", date, "current", "next_page_test", "TestQouteName");

        var result = sut.QuestionForm("current_random");

        result.IsNone.Should().BeTrue();
    }

    public void ReturnsCorrectPageKey_WhenStepIsFoundInActiveBranch()
    {
        var date = DateTimeOffset.UtcNow;
        var sut = new QuoteSelectionJourney("RBS", "0304442", date, "current", "next_page_test", "TestQouteName");
        sut.TrySubmitStep("next_page_test", "next_page_test_1", DateTimeOffset.UtcNow, QuoteSelectionJourney.QuestionKey, "TestQouteName1");
        sut.TrySubmitStep("next_page_test_1", "next_page_test_2", DateTimeOffset.UtcNow, QuoteSelectionJourney.QuestionKey, "TestQouteName2");

        var result = sut.GetRedirectStepPageKey("next_page_test_1");

        result.Should().Be("next_page_test_1");
    }

    public void ReturnsCorrectPageKey_WhenStepIsFoundInNotActiveBranch()
    {
        var date = DateTimeOffset.UtcNow;
        var sut = new QuoteSelectionJourney("RBS", "0304442", date, "current", "retirement_start_1", "TestQouteName");
        sut.TrySubmitStep("retirement_start_1", "next_page_test", DateTimeOffset.UtcNow, QuoteSelectionJourney.QuestionKey, "TestQouteName1");
        sut.TrySubmitStep("next_page_test", "next_page_test_2", DateTimeOffset.UtcNow, QuoteSelectionJourney.QuestionKey, "TestQouteName2");
        sut.TrySubmitStep("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow, QuoteSelectionJourney.QuestionKey, "TestQouteName3");
        sut.TrySubmitStep("retirement_start_1", "retirement_start_1_1", DateTimeOffset.UtcNow, QuoteSelectionJourney.QuestionKey, "TestQouteName4");
        sut.TrySubmitStep("retirement_start_1_1", "retirement_start_1_2", DateTimeOffset.UtcNow, QuoteSelectionJourney.QuestionKey, "TestQouteName5");

        var result = sut.GetRedirectStepPageKey("next_page_test");

        result.Should().Be("retirement_start_1_2");
    }

    public void ReturnsCorrectPageKey_WhenFirstStepCurrentPageKeyProvided()
    {
        var date = DateTimeOffset.UtcNow;
        var sut = new QuoteSelectionJourney("RBS", "0304442", date, "current", "retirement_start_1", "TestQouteName");
        sut.TrySubmitStep("retirement_start_1", "next_page_test", DateTimeOffset.UtcNow, QuoteSelectionJourney.QuestionKey, "TestQouteName1");
        sut.TrySubmitStep("next_page_test", "next_page_test_2", DateTimeOffset.UtcNow, QuoteSelectionJourney.QuestionKey, "TestQouteName2");
        sut.TrySubmitStep("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow, QuoteSelectionJourney.QuestionKey, "TestQouteName3");
        sut.TrySubmitStep("retirement_start_1", "retirement_start_1_1", DateTimeOffset.UtcNow, QuoteSelectionJourney.QuestionKey, "TestQouteName4");
        sut.TrySubmitStep("retirement_start_1_1", "retirement_start_1_2", DateTimeOffset.UtcNow, QuoteSelectionJourney.QuestionKey, "TestQouteName5");

        var result = sut.GetRedirectStepPageKey("current");

        result.Should().Be("current");
    }
}