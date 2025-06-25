using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using WTW.MdpService.Domain.Common.Journeys;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Domain.Common;

public class JourneyStepTest
{
    public void CreatesJourneyStep()
    {
        var utcNow = DateTimeOffset.UtcNow;

        var sut = JourneyStep.Create("retirement_start", "next_page_test", utcNow);

        sut.CurrentPageKey.Should().Be("retirement_start");
        sut.NextPageKey.Should().Be("next_page_test");
        sut.SubmitDate.Should().Be(utcNow);
        sut.UpdateDate.Should().Be(utcNow);
        sut.QuestionForm.Should().BeNull();
        sut.IsNextPageAsDeadEnd.Should().BeFalse();
    }

    public void SetsNewNextPageKey()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var sut = JourneyStep.Create("retirement_start", "next_page_test", utcNow);

        sut.NextPageKey.Should().Be("next_page_test");
        sut.UpdateNextPageKey("next_page_test_new");

        sut.CurrentPageKey.Should().Be("retirement_start");
        sut.NextPageKey.Should().Be("next_page_test_new");
        sut.SubmitDate.Should().Be(utcNow);
        sut.QuestionForm.Should().BeNull();
    }

    public void CreatesJourneyStepWithQuestionForm()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var questionForm = new QuestionForm("test_question_key", "test_answer_key");

        var sut = JourneyStep.Create("retirement_start", "next_page_test", utcNow, questionForm);

        sut.CurrentPageKey.Should().Be("retirement_start");
        sut.NextPageKey.Should().Be("next_page_test");
        sut.SubmitDate.Should().Be(utcNow);
        sut.UpdateDate.Should().Be(utcNow);
        sut.QuestionForm.QuestionKey.Should().Be(questionForm.QuestionKey);
        sut.QuestionForm.AnswerKey.Should().Be(questionForm.AnswerKey);
    }

    public void CreatesJourneyStepFromOtherJourneyStep()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var questionForm = new QuestionForm("test_question_key", "test_answer_key");
        var otherJourneyStep = JourneyStep.Create("retirement_start", "next_page_test", utcNow, questionForm);

        var sut = otherJourneyStep.Duplicate();

        sut.Should().NotBeNull();
        sut.CurrentPageKey.Should().Be("retirement_start");
        sut.NextPageKey.Should().Be("next_page_test");
        sut.SubmitDate.Should().Be(utcNow);
        sut.UpdateDate.Should().Be(utcNow);
        sut.QuestionForm.QuestionKey.Should().Be(questionForm.QuestionKey);
        sut.QuestionForm.AnswerKey.Should().Be(questionForm.AnswerKey);
    }

    public void CreatesJourneyStepFromOtherJourneyStepWithGenericData()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var questionForm = new QuestionForm("test_question_key", "test_answer_key");
        var otherJourneyStep = JourneyStep.Create("retirement_start", "next_page_test", utcNow, questionForm);
        otherJourneyStep.UpdateGenericData("test_key", "test_value");

        var sut = otherJourneyStep.Duplicate();

        sut.Should().NotBeNull();
        sut.CurrentPageKey.Should().Be("retirement_start");
        sut.NextPageKey.Should().Be("next_page_test");
        sut.SubmitDate.Should().Be(utcNow);
        sut.QuestionForm.QuestionKey.Should().Be(questionForm.QuestionKey);
        sut.QuestionForm.AnswerKey.Should().Be(questionForm.AnswerKey);
        sut.JourneyGenericDataList.Count.Should().Be(1);
        sut.JourneyGenericDataList[0].FormKey.Should().Be("test_key");
        sut.JourneyGenericDataList[0].GenericDataJson.Should().Be("test_value");
    }

    public void CreatesJourneyStepFromOtherJourneyStepWithAnswerValue()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var questionForm = new QuestionForm("test_question_key", "test_answer_key", "test_answer_value");
        var otherJourneyStep = JourneyStep.Create("retirement_start", "next_page_test", utcNow, questionForm);

        var sut = otherJourneyStep.Duplicate();

        sut.Should().NotBeNull();
        sut.QuestionForm.AnswerValue.Should().Be(questionForm.AnswerValue);
    }

    public void CanUpdateSequenceNumber()
    {
        var sut = JourneyStep.Create("retirement_start", "next_page_test_1", DateTimeOffset.UtcNow);

        sut.SequenceNumber.Should().Be(1);

        sut.UpdateSequenceNumber(2);

        sut.SequenceNumber.Should().Be(2);
    }

    public void MarksNextPageAsDeadEnd()
    {
        var sut = JourneyStep.Create("retirement_start", "next_page_test_1", DateTimeOffset.UtcNow);

        sut.MarkNextPageAsDeadEnd();

        sut.IsNextPageAsDeadEnd.Should().BeTrue();
    }

    public void AddsCheckboxesList_WhenValidDataIsProvided()
    {
        var checkboxesList = new CheckboxesList("testKey", new List<(string, bool)> { ("a1", true) });
        var sut = JourneyStep.Create("retirement_start", "next_page_test_1", DateTimeOffset.UtcNow);

        sut.AddCheckboxesList(checkboxesList);

        sut.CheckboxesLists.Count.Should().Be(1);
    }

    public void OverridesCheckboxesList_WhenListWithSameKeyIsGiven()
    {
        var checkboxesList1 = new CheckboxesList("testKey", new List<(string, bool)> { ("a1", true) });
        var checkboxesList2 = new CheckboxesList("testKey", new List<(string, bool)> { ("b1", false) });
        var sut = JourneyStep.Create("retirement_start", "next_page_test_1", DateTimeOffset.UtcNow);
        sut.AddCheckboxesList(checkboxesList1);

        sut.CheckboxesLists.Count.Should().Be(1);
        sut.CheckboxesLists.Single().CheckboxesListKey.Should().Be("testKey");
        sut.CheckboxesLists.Single().Checkboxes.Single().Key.Should().Be("a1");
        sut.CheckboxesLists.Single().Checkboxes.Single().AnswerValue.Should().BeTrue();


        sut.AddCheckboxesList(checkboxesList2);

        sut.CheckboxesLists.Count.Should().Be(1);
        sut.CheckboxesLists.Single().CheckboxesListKey.Should().Be("testKey");
        sut.CheckboxesLists.Single().Checkboxes.Single().Key.Should().Be("b1");
        sut.CheckboxesLists.Single().Checkboxes.Single().AnswerValue.Should().BeFalse();
    }

    public void ReturnsCorrectCheckboxesListByKey()
    {
        var checkboxesList1 = new CheckboxesList("testKey1", new List<(string, bool)> { ("a1", true) });
        var checkboxesList2 = new CheckboxesList("testKey2", new List<(string, bool)> { ("b1", false) });
        var sut = JourneyStep.Create("retirement_start", "next_page_test_1", DateTimeOffset.UtcNow);
        sut.AddCheckboxesList(checkboxesList1);
        sut.AddCheckboxesList(checkboxesList2);

        var result = sut.GetCheckboxesListByKey("testKey2");

        result.IsSome.Should().BeTrue();
        result.Value().Checkboxes.Count.Should().Be(1);
        result.Value().Checkboxes.Single().Key.Should().Be("b1");
        result.Value().Checkboxes.Single().AnswerValue.Should().BeFalse();
    }

    public void ReturnNone_WhenCheckboxesListWithRequiredKeyDoesNotExist()
    {
        var checkboxesList1 = new CheckboxesList("testKey1", new List<(string, bool)> { ("a1", true) });
        var sut = JourneyStep.Create("retirement_start", "next_page_test_1", DateTimeOffset.UtcNow);
        sut.AddCheckboxesList(checkboxesList1);

        var result = sut.GetCheckboxesListByKey("testKey2");

        result.IsNone.Should().BeTrue();
    }

    public void CanUpdateGenericDataList()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var questionForm = new QuestionForm("test_question_key", "test_answer_key");
        var sut = JourneyStep.Create("retirement_start", "next_page_test", utcNow, questionForm);
        sut.UpdateGenericData("test_key", "test_value");

        var newGenericDataList = new List<JourneyGenericData>
        {
            new("data_json_1", "form_key_1"),
            new("data_json_2", "form_key_2")
        };

        sut.AppendGenericDataList(newGenericDataList);

        sut.JourneyGenericDataList.Count.Should().Be(3);
        sut.JourneyGenericDataList.Should().Contain(x => x.FormKey == "form_key_2" && x.GenericDataJson == "data_json_2");
    }

    public void RenewsUpdateDate()
    {
        var utcNow = DateTimeOffset.UtcNow;

        var sut = JourneyStep.Create("retirement_start", "next_page_test", utcNow);

        sut.RenewUpdateDate(utcNow.AddMinutes(5));

        sut.SubmitDate.Should().Be(utcNow);
        sut.UpdateDate.Should().Be(utcNow.AddMinutes(5));
    }
}