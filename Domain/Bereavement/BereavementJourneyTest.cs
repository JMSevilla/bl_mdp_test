using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using WTW.MdpService.Domain.Bereavement;
using WTW.TestCommon.FixieConfig;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Domain.Bereavement;

public class BereavementJourneyTest
{
    public void CreatesBereavementJourney_WhenValidDataProvided()
    {
        var date = DateTimeOffset.UtcNow;
        var referenceNumber = Guid.NewGuid();
        var sut = new BereavementJourneyBuilder()
            .ReferenceNumber(referenceNumber)
            .Date(date)
            .Build()
            .Right();

        sut.Should().NotBeNull();
        sut.ReferenceNumber.Should().Be(referenceNumber.ToString());
        sut.BusinessGroup.Should().Be("RBS");
        sut.StartDate.Should().Be(date);
        sut.ExpirationDate.Should().Be(date.AddMinutes(1));
        sut.SubmissionDate.Should().BeNull();
        sut.JourneyBranches.Count.Should().Be(1);
    }

    [Input("00000000-0000-0000-0000-000000000000", "RBS", "currentPageKey", "nextPageKey", "\"BereavementReferenceNumber\" can not be default value.", 1)]
    [Input("00000000-0000-0000-0000-000000000001", null, "currentPageKey", "nextPageKey", "\"BusinessGroup\" field is required. Must be 3 characters Length.", 1)]
    [Input("00000000-0000-0000-0000-000000000001", "", "currentPageKey", "nextPageKey", "\"BusinessGroup\" field is required. Must be 3 characters Length.", 1)]
    [Input("00000000-0000-0000-0000-000000000001", " ", "currentPageKey", "nextPageKey", "\"BusinessGroup\" field is required. Must be 3 characters Length.", 1)]
    [Input("00000000-0000-0000-0000-000000000001", "RB", "currentPageKey", "nextPageKey", "\"BusinessGroup\" field is required. Must be 3 characters Length.", 1)]
    [Input("00000000-0000-0000-0000-000000000001", "RBSS", "currentPageKey", "nextPageKey", "\"BusinessGroup\" field is required. Must be 3 characters Length.", 1)]
    [Input("00000000-0000-0000-0000-000000000001", "RBS", null, "nextPageKey", "\"CurrentPageKey\" field is required. Up to 25  characters Length.", 1)]
    [Input("00000000-0000-0000-0000-000000000001", "RBS", "", "nextPageKey", "\"CurrentPageKey\" field is required. Up to 25  characters Length.", 1)]
    [Input("00000000-0000-0000-0000-000000000001", "RBS", "  ", "nextPageKey", "\"CurrentPageKey\" field is required. Up to 25  characters Length.", 1)]
    [Input("00000000-0000-0000-0000-000000000001", "RBS", "it is absolutely too long current page key", "nextPageKey", "\"CurrentPageKey\" field is required. Up to 25  characters Length.", 1)]
    [Input("00000000-0000-0000-0000-000000000001", "RBS", "currentPageKey", null, "\"NexttPageKey\" field is required. Up to 25  characters Length.", 1)]
    [Input("00000000-0000-0000-0000-000000000001", "RBS", "currentPageKey", "", "\"NexttPageKey\" field is required. Up to 25  characters Length.", 1)]
    [Input("00000000-0000-0000-0000-000000000001", "RBS", "currentPageKey", "    ", "\"NexttPageKey\" field is required. Up to 25  characters Length.", 1)]
    [Input("00000000-0000-0000-0000-000000000001", "RBS", "currentPageKey", "it is absolutely too long next page key", "\"NexttPageKey\" field is required. Up to 25  characters Length.", 1)]
    [Input("00000000-0000-0000-0000-000000000001", "RBS", "currentPageKey", "nextPageKey", "\"validityPeriodInMin\" field must be greater or equel to 1.", 0)]
    public void ReturnsError_OnBereavementJourneyCreation_WhenInalidDataProvided(string referenceNumber,
        string businessGroup,
        string currentPageKey,
        string nextPageKey,
        string expectedErrorMessage,
        int validityPeriodInMin)
    {
        var date = DateTimeOffset.UtcNow;
        var sut = new BereavementJourneyBuilder()
            .ReferenceNumber(Guid.Parse(referenceNumber))
            .BusinessGroup(businessGroup)
            .Date(date)
            .CurrentPageKey(currentPageKey)
            .NextPageKey(nextPageKey)
            .CurrentPageKey(currentPageKey)
            .Date(date)
            .ValidityPeriodInMin(validityPeriodInMin)
            .Build();

        sut.IsLeft.Should().BeTrue();
        sut.Left().Message.Should().Be(expectedErrorMessage);
    }

    public void ThowsIfUnsuppurtedQuestionFormsMethodIsUsed()
    {
        var sut = new BereavementJourneyBuilder().Build().Right();

        var action = () => sut.QuestionForms(new List<string> { "retirement_start_random", "next_page_test" });

        action.Should().Throw<NotImplementedException>();
    }

    public void ThowsIfUnsuppurtedUpdateStepMethodIsUsed()
    {
        var sut = new BereavementJourneyBuilder().Build().Right();

        var action = () => sut.UpdateStep("retirement_start_random", "next_page_test");

        action.Should().Throw<NotImplementedException>();
    }

    public void UpdatesExpiryDate()
    {
        var date = DateTimeOffset.UtcNow;
        var referenceNumber = Guid.NewGuid();
        var sut = new BereavementJourneyBuilder()
            .Date(date)
            .Build()
            .Right();

        sut.UpdateExpiryDate(date.AddMinutes(15));

        sut.ExpirationDate.Should().NotBe(date.AddMinutes(1));
        sut.ExpirationDate.Should().Be(date.AddMinutes(15));
    }

    public void CanSubmitBereavementStepWithQuestionFormAndReturnAnswerValue()
    {
        var currentPageKey = "bereavement_email";
        var nextPageKey = "bereavement_email_confirm";
        var date = DateTimeOffset.UtcNow;
        var answerKey = "test_answer_key";
        var answerValue = "Test Answer Value";
        
        var sut = new BereavementJourneyBuilder()
            .CurrentPageKey("bereavement_start")
            .NextPageKey("bereavement_email")
            .Date(date)
            .Build()
            .Right();

        var result = sut.TrySubmitStep(currentPageKey, nextPageKey, date, "SelectedQuoteName", answerKey, "Test Answer Value", false);

        result.Right().Should().Be(true);
        sut.JourneyBranches.Count.Should().Be(1);
        sut.JourneyBranches.Single().JourneySteps[1].CurrentPageKey.Should().Be(currentPageKey);
        sut.JourneyBranches.Single().JourneySteps[1].NextPageKey.Should().Be(nextPageKey);
        sut.JourneyBranches.Single().JourneySteps[1].SubmitDate.Should().Be(date);
        sut.JourneyBranches.Single().JourneySteps[1].QuestionForm.QuestionKey.Should().Be("SelectedQuoteName");
        sut.JourneyBranches.Single().JourneySteps[1].QuestionForm.AnswerKey.Should().Be(answerKey);
        sut.JourneyBranches.Single().JourneySteps[1].QuestionForm.AnswerValue.Should().Be(answerValue);
    }

    public void CanSubmitBereavementStepWithQuestionFormWhenAvoidingBranching()
    {
        var currentPageKey = "bereavement_email";
        var nextPageKey = "bereavement_email_confirm";
        var date = DateTimeOffset.UtcNow;
        var answerKey = "test_answer_key";
        var answerValue = "Test Answer Value";
        var sut = new BereavementJourneyBuilder()
            .CurrentPageKey("bereavement_start")
            .NextPageKey("bereavement_email")
            .Date(date)
            .Build()
            .Right();

        var result = sut.TrySubmitStep(currentPageKey, nextPageKey, date, "SelectedQuoteName", answerKey, "Test Answer Value", false);

        result.Right().Should().Be(true);
        sut.JourneyBranches.Count.Should().Be(1);
        sut.JourneyBranches.Single().JourneySteps.Count.Should().Be(2);
        sut.JourneyBranches.Single().JourneySteps[1].CurrentPageKey.Should().Be(currentPageKey);
        sut.JourneyBranches.Single().JourneySteps[1].NextPageKey.Should().Be(nextPageKey);
        sut.JourneyBranches.Single().JourneySteps[1].SubmitDate.Should().Be(date);
        sut.JourneyBranches.Single().JourneySteps[1].QuestionForm.QuestionKey.Should().Be("SelectedQuoteName");
        sut.JourneyBranches.Single().JourneySteps[1].QuestionForm.AnswerKey.Should().Be(answerKey);
        sut.JourneyBranches.Single().JourneySteps[1].QuestionForm.AnswerValue.Should().Be(answerValue);

        var newResult = sut.TrySubmitStep(currentPageKey, nextPageKey, date, "SelectedQuoteName", answerKey + "test", "Test Answer Value" + "test", true);
        newResult.Right().Should().Be(true);
        sut.JourneyBranches.Count.Should().Be(1);
        sut.JourneyBranches.Single().JourneySteps.Count.Should().Be(2);
        sut.JourneyBranches.Single().JourneySteps[1].CurrentPageKey.Should().Be(currentPageKey);
        sut.JourneyBranches.Single().JourneySteps[1].NextPageKey.Should().Be(nextPageKey);
        sut.JourneyBranches.Single().JourneySteps[1].SubmitDate.Should().Be(date);
        sut.JourneyBranches.Single().JourneySteps[1].QuestionForm.QuestionKey.Should().Be("SelectedQuoteName");
        sut.JourneyBranches.Single().JourneySteps[1].QuestionForm.AnswerKey.Should().Be(answerKey + "test");
        sut.JourneyBranches.Single().JourneySteps[1].QuestionForm.AnswerValue.Should().Be(answerValue + "test");
    }

    public void CanMergeBranchesOnSubmitingNextPageStepThatExixtedInOlderBranch()
    {
        var sut = new BereavementJourneyBuilder()
            .CurrentPageKey("step0")
            .NextPageKey("step1")
            .Build()
            .Right();

        sut.TrySubmitStep("step1", "step2", DateTimeOffset.UtcNow, "questionkey1", "answerKey1", "AnswerValue1", false);
        sut.TrySubmitStep("step2", "step3", DateTimeOffset.UtcNow, "questionkey2", "answerKey2", "AnswerValue2", false);
        sut.TrySubmitStep("step3", "step4", DateTimeOffset.UtcNow, "questionkey3", "answerKey3", "AnswerValue3", false);
        sut.TrySubmitStep("step4", "step5", DateTimeOffset.UtcNow, "questionkey4", "answerKey4", "AnswerValue4", false);
        sut.TrySubmitStep("step5", "step6", DateTimeOffset.UtcNow, "questionkey5", "answerKey5", "AnswerValue5", false);
        sut.TrySubmitStep("step6", "step7", DateTimeOffset.UtcNow, "questionkey6", "answerKey6", "AnswerValue6", false);
        var result = sut.TrySubmitStep("step7", "step8", DateTimeOffset.UtcNow, "questionkey7", "answerKey7", "AnswerValue7", false);

        result.Right().Should().BeTrue();
        sut.JourneyBranches.Count.Should().Be(1);
        sut.JourneyBranches.Count(x => x.IsActive).Should().Be(1);
        sut.JourneyBranches.Single().SequenceNumber.Should().Be(1);
        sut.JourneyBranches.Single().JourneySteps.Count.Should().Be(8);

        sut.TrySubmitStep("step2", "step3.1.", DateTimeOffset.UtcNow, "questionkey2", "AnswerValue2", "AnswerValue2", false);
        sut.TrySubmitStep("step3.1.", "step4.1.", DateTimeOffset.UtcNow, "questionkey3.1", "AnswerValue3.1", "AnswerValue3.1", false);
        var newResult = sut.TrySubmitStep("step4.1.", "step5.1.", DateTimeOffset.UtcNow, "questionkey4.1", "AnswerValue4.1", "AnswerValue4.1", false);
        newResult.Right().Should().BeTrue();
        sut.JourneyBranches.Count.Should().Be(2);
        sut.JourneyBranches.Count(x => x.IsActive).Should().Be(1);
        sut.JourneyBranches.Single(x => x.IsActive).SequenceNumber.Should().Be(2);
        sut.JourneyBranches.Single(x => x.IsActive).JourneySteps.Count.Should().Be(5);

        sut.TrySubmitStep("step3.1.", "step4.1.", DateTimeOffset.UtcNow, "questionkey3.1", "AnswerValue3.1", "AnswerValue3.1", false);
        sut.TrySubmitStep("step4.1.", "step5.2.", DateTimeOffset.UtcNow, "questionkey4.1", "AnswerValue4.1", "AnswerValue4.1", false);
        var new2Result = sut.TrySubmitStep("step5.2.", "step6.2.", DateTimeOffset.UtcNow, "questionkey5.2.", "AnswerValue5.2.", "AnswerValue5.2.", false);
        new2Result.Right().Should().BeTrue();
        sut.JourneyBranches.Count.Should().Be(3);
        sut.JourneyBranches.Count(x => x.IsActive).Should().Be(1);
        sut.JourneyBranches.Single(x => x.IsActive).SequenceNumber.Should().Be(3);
        sut.JourneyBranches.Single(x => x.IsActive).JourneySteps.Count.Should().Be(6);

        var new3Result = sut.TrySubmitStep("step6.2.", "step6", DateTimeOffset.UtcNow, "questionkey6.2.", "AnswerValue6.2.", "AnswerValue6.2.", true);
        new3Result.Right().Should().BeTrue();
        sut.JourneyBranches.Count.Should().Be(1);
        sut.JourneyBranches.Count(x => x.IsActive).Should().Be(1);
        sut.JourneyBranches.Single(x => x.IsActive).SequenceNumber.Should().Be(3);
        sut.JourneyBranches.Single(x => x.IsActive).JourneySteps.Count.Should().Be(9);

    }
}