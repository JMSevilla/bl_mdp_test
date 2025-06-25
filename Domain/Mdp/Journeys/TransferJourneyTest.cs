using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using WTW.MdpService.Domain;
using WTW.MdpService.Domain.Common;
using WTW.MdpService.Domain.Common.Journeys;
using WTW.MdpService.Domain.Mdp;
using WTW.TestCommon.FixieConfig;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Domain.Mdp.Journeys;

public class TransferJourneyTest
{
    public void CreatesInitialTransferJourney()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = new TransferJourneyBuilder()
            .Date(now)
            .NextPageKey("current")
            .NextPageKey("next")
            .BuildWithSteps();

        sut.Should().NotBeNull();
        sut.BusinessGroup.Should().Be("RBS");
        sut.ReferenceNumber.Should().Be("0304442");
        sut.StartDate.Should().Be(now);
        sut.SubmissionDate.Should().BeNull();
        sut.ExpirationDate.Should().Be(DateTimeOffset.MinValue);
        sut.JourneyBranches.Should().NotBeEmpty();
        sut.TransferImageId.Should().Be(10);
        sut.GbgId.Should().BeNull();
    }

    public void CreatesEpaTransferJourney()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = TransferJourney.CreateEpa("RBS", "1111111", "123456", now.AddDays(-2), now);

        sut.Should().NotBeNull();
        sut.BusinessGroup.Should().Be("RBS");
        sut.ReferenceNumber.Should().Be("1111111");
        sut.StartDate.Should().Be(now);
        sut.SubmissionDate.Should().Be(now.AddDays(-2));
        sut.ExpirationDate.Should().Be(default);
        sut.JourneyBranches.Should().NotBeEmpty();
        sut.TransferImageId.Should().Be(default);
        sut.GbgId.Should().BeNull();
        sut.NameOfPlan.Should().BeNull();
        sut.TypeOfPayment.Should().BeNull();
        sut.DateOfPayment.Should().BeNull();
    }

    public void CreatesInitialTransferJourney_WithoutPassingPageKays()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = new TransferJourneyBuilder()
            .Date(now)
            .Build();

        sut.Should().NotBeNull();
        sut.BusinessGroup.Should().Be("RBS");
        sut.ReferenceNumber.Should().Be("0304442");
        sut.StartDate.Should().Be(now);
        sut.SubmissionDate.Should().BeNull();
        sut.ExpirationDate.Should().Be(DateTimeOffset.MinValue);
        sut.JourneyBranches.Should().NotBeEmpty();
        sut.TransferImageId.Should().Be(10);
    }

    public void CanSubmitRetirementStepWithQuestionForm()
    {
        var currentPageKey = "retirement_start";
        var nextPageKey = "next_page_test";
        var date = DateTimeOffset.UtcNow;
        var questionKey = "test_question_key";
        var answerKey = "test_answer_key";
        var sut = new TransferJourneyBuilder()
            .Date(date)
            .CurrentPageKey("current")
            .NextPageKey(currentPageKey)
            .BuildWithSteps();

        var result = sut.TrySubmitStep(currentPageKey, nextPageKey, date, questionKey, answerKey);

        result.Right().Should().Be(true);
        sut.JourneyBranches.Count.Should().Be(1);
        sut.JourneyBranches.Single().JourneySteps.Count.Should().Be(2);
        sut.JourneyBranches.Single().JourneySteps[1].CurrentPageKey.Should().Be(currentPageKey);
        sut.JourneyBranches.Single().JourneySteps[1].NextPageKey.Should().Be(nextPageKey);
        sut.JourneyBranches.Single().JourneySteps[1].SubmitDate.Should().Be(date);
        sut.JourneyBranches.Single().JourneySteps[1].QuestionForm.QuestionKey.Should().Be(questionKey);
        sut.JourneyBranches.Single().JourneySteps[1].QuestionForm.AnswerKey.Should().Be(answerKey);
    }

    public void ReturnErrorMessageWhenInvalidCurrentStepProvided()
    {
        var date = DateTimeOffset.UtcNow;
        var sut = new TransferJourneyBuilder()
            .Date(date)
            .CurrentPageKey("current")
            .NextPageKey("retirement_start")
            .BuildWithSteps();
        sut.TrySubmitStep("retirement_start", "next_page_test", date, "test_question_key", "test_answer_key");

        var result = sut.TrySubmitStep("retirement_start_random", "next_page_test", DateTimeOffset.UtcNow, "test_question_key", "test_answer_key");

        result.Left().Message.Should().Be("Invalid \"currentPageKey\"");
        sut.JourneyBranches.Count.Should().Be(1);
        sut.JourneyBranches[0].JourneySteps.Count.Should().Be(2);
    }

    public void PreviousStepReturnSome_WhenStepCouldBeFoundInActiveBranch()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = new TransferJourneyBuilder()
            .Date(now)
            .CurrentPageKey("current")
            .NextPageKey("next_page_test")
            .BuildWithSteps();
        sut.TrySubmitStep("next_page_test", "next_page_test_2", DateTimeOffset.UtcNow, "test_question_key", "test_answer_key");

        var result = sut.PreviousStep("next_page_test");

        result.IsSome.Should().BeTrue();
        result.Value().Should().Be("current");
    }

    public void PreviousStepReturnNone_WhenNoStepCouldBeFoundInActiveBranch()
    {
        var sut = new TransferJourneyBuilder()
            .CurrentPageKey("current")
            .NextPageKey("next_page_test")
            .BuildWithSteps();
        sut.TrySubmitStep("next_page_test", "next_page_test_2", DateTimeOffset.UtcNow);
        sut.TrySubmitStep("next_page_tes_2", "next_page_test_3", DateTimeOffset.UtcNow);
        sut.TrySubmitStep("next_page_test", "next_page_test_2.1", DateTimeOffset.UtcNow);

        var result = sut.PreviousStep("next_page_test_2");

        result.IsNone.Should().BeTrue();
    }

    public void QuestionFormReturnSome_WhenActiveBranchExistAndCurrentStepHasQuestionForm()
    {
        var sut = new TransferJourneyBuilder()
            .CurrentPageKey("current")
            .NextPageKey("next_page_test")
            .BuildWithSteps();
        sut.TrySubmitStep("next_page_test", "next_page_test_2", DateTimeOffset.UtcNow, "test_question_key", "test_answer_key");
        sut.TrySubmitStep("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow);

        var result = sut.QuestionForm("next_page_test");

        result.IsSome.Should().BeTrue();
        result.Value().QuestionKey.Should().Be("test_question_key");
        result.Value().AnswerKey.Should().Be("test_answer_key");
    }

    public void QuestionFormReturnsEmpty_WhenStepIsLastInActiveBranch()
    {
        var sut = new TransferJourneyBuilder()
            .CurrentPageKey("current")
            .NextPageKey("next_page_test")
            .BuildWithSteps();

        var result = sut.QuestionForm("next_page_test");

        result.IsSome.Should().BeTrue();
        result.Value().QuestionKey.Should().BeNull();
        result.Value().AnswerKey.Should().BeNull();
        result.Value().AnswerValue.Should().BeNull();
    }

    public void QuestionFormReturnNone_NoStepCouldBeFoundWithQuestionForm()
    {
        var sut = new TransferJourneyBuilder().BuildWithSteps();

        var result = sut.QuestionForm("current");

        result.IsNone.Should().BeTrue();
    }

    public void ReturnsCorrectPageKey_WhenStepIsFoundInActiveBranch()
    {
        var sut = new TransferJourneyBuilder().BuildWithSteps();
        sut.TrySubmitStep("transfer_start_1", "next_page_test", DateTimeOffset.UtcNow, "q1", "a1");
        sut.TrySubmitStep("next_page_test", "next_page_test_2", DateTimeOffset.UtcNow, "q2", "a2");

        var result = sut.GetRedirectStepPageKey("next_page_test");

        result.Should().Be("next_page_test");
    }

    public void SkipsLastStep_WhenExistingStepNextPageKeyIsNotSet()
    {
        var sut = new TransferJourneyBuilder().BuildWithSteps();
        sut.TrySubmitStep("transfer_start_1", "next_page_test", DateTimeOffset.UtcNow, "q1", "a1");
        sut.TrySubmitStep("next_page_test", "next_page_test_2", DateTimeOffset.UtcNow, "q2", "a2");
        sut.TrySubmitStep("next_page_test_2", "next_page_is_not_set", DateTimeOffset.UtcNow);

        var result = sut.GetRedirectStepPageKey("page_key_test");

        result.Should().Be("next_page_test_2");
    }

    public void ReturnsCorrectPageKey_WhenStepIsFoundInNotActiveBranch()
    {
        var date = DateTimeOffset.UtcNow;
        var sut = new TransferJourneyBuilder()
            .Date(date)
            .CurrentPageKey("current")
            .NextPageKey("transfer_start_1")
            .BuildWithSteps();
        sut.TrySubmitStep("transfer_start_1", "next_page_test", date, "q1", "a1");
        sut.TrySubmitStep("next_page_test", "next_page_test_2", date, "q2", "a2");
        sut.TrySubmitStep("next_page_test_2", "next_page_test_3", date, "q3", "a3");
        sut.TrySubmitStep("transfer_start_1", "transfer_start_1_1", date, "q4", "a4");
        sut.TrySubmitStep("transfer_start_1_1", "transfer_start_1_2", date, "q5", "a5");

        var result = sut.GetRedirectStepPageKey("next_page_test");

        result.Should().Be("transfer_start_1_2");
    }

    public void ReturnsCorrectPageKey_WhenFirstStepCurrentPageKeyProvided()
    {
        var date = DateTimeOffset.UtcNow;
        var sut = new TransferJourneyBuilder().Date(date).BuildWithSteps();
        sut.TrySubmitStep("transfer_start_1", "next_page_test", date, "q1", "a1");
        sut.TrySubmitStep("next_page_test", "next_page_test_2", date, "q2", "a2");
        sut.TrySubmitStep("next_page_test_2", "next_page_test_3", date, "q3", "a3");
        sut.TrySubmitStep("transfer_start_1", "transfer_start_1_1", date, "q4", "a4");
        sut.TrySubmitStep("transfer_start_1_1", "transfer_start_1_2", date, "q5", "a5");

        var result = sut.GetRedirectStepPageKey("current");

        result.Should().Be("current");
    }

    public void UpdatesLastStepNextPageKey()
    {
        var date = DateTimeOffset.UtcNow;
        var sut = new TransferJourneyBuilder().Date(date).BuildWithSteps();
        sut.TrySubmitStep("transfer_start_1", "next_page_test", date, "q1", "a1");
        sut.TrySubmitStep("next_page_test", "next_page_test_2", date, "q2", "a2");
        sut.TrySubmitStep("next_page_test_2", "next_page_test_3", date, "q3", "a3");
        sut.TrySubmitStep("transfer_start_1", "transfer_start_1_1", date, "q4", "a4");
        sut.TrySubmitStep("transfer_start_1_1", "transfer_start_1_2", date, "q5", "a5");
        sut.JourneyBranches.Single(x => x.IsActive).JourneySteps.Single(x => x.CurrentPageKey == "transfer_start_1_1").NextPageKey.Should().Be("transfer_start_1_2");

        var result = sut.UpdateStep("transfer_start_1_2", "transfer_start_1_2_1");

        result.HasValue.Should().BeFalse();
        sut.JourneyBranches.Single(x => x.IsActive).JourneySteps.Single(x => x.CurrentPageKey == "transfer_start_1_1").NextPageKey.Should().Be("transfer_start_1_2_1");
    }

    public void ReturnsErrorIfStepNotFound()
    {
        var date = DateTimeOffset.UtcNow;
        var sut = new TransferJourneyBuilder().BuildWithSteps();
        sut.TrySubmitStep("transfer_start_1", "next_page_test", date, "q1", "a1");
        sut.TrySubmitStep("next_page_test", "next_page_test_2", date, "q2", "a2");
        sut.TrySubmitStep("next_page_test_2", "next_page_test_3", date, "q3", "a3");
        sut.TrySubmitStep("transfer_start_1", "transfer_start_1_1", date, "q4", "a4");
        sut.TrySubmitStep("transfer_start_1_1", "transfer_start_1_2", date, "q5", "a5");

        var result = sut.UpdateStep("transfer_start_1_2_1", "transfer_start_1_2_2");

        result.HasValue.Should().BeTrue();
        result.Value.Message.Should().Be("Previous step does not exist in retiremnt journey for given current page key.");
    }

    public void SetsCalculationType()
    {
        var date = DateTimeOffset.UtcNow;
        var sut = new TransferJourneyBuilder()
            .CurrentPageKey("current")
            .NextPageKey("transfer_start_1")
            .BuildWithSteps();

        sut.SetCalculationType("TestType");

        sut.CalculationType.Should().Be("TestType");
    }

    public void CanSubmitContact()
    {
        var date = DateTimeOffset.UtcNow;
        var orginalEffectiveDate = DateTimeOffset.UtcNow;

        var transferJourneyContact = new TransferJourneyContactFactory();
        var contact = transferJourneyContact.Create(
                     "tesName",
                     "Advisor name",
                     "testCompanyName",
                     Email.Create("test@gmail.com").Right(),
                     Phone.Create("370", "67878788").Right(),
                     "Ifa",
                     "Scheme name",
                     DateTimeOffset.UtcNow).Right();

        var sut = new TransferJourneyBuilder().BuildWithSteps();

        sut.SubmitContact(contact);

        sut.Contacts.Count.Should().Be(1);
        sut.Contacts[0].Name.Should().Be("tesName");
        sut.Contacts[0].CompanyName.Should().Be("testCompanyName");
        sut.Contacts[0].Email.Address.Should().Be("test@gmail.com");
        sut.Contacts[0].Phone.FullNumber.Should().Be("370 67878788");
        sut.Contacts[0].Address.Should().Be(Address.Empty());
        sut.Contacts[0].SchemeName.Should().Be("Scheme name");
        sut.Contacts[0].AdvisorName.Should().Be("Advisor name");
    }

    public void CanRemoveAllContacts()
    {
        var date = DateTimeOffset.UtcNow;
        var orginalEffectiveDate = DateTimeOffset.UtcNow;

        var transferJourneyContact = new TransferJourneyContactFactory();
        var contact = transferJourneyContact.Create(
                     "tesName",
                     null,
                     "testCompanyName",
                     null,
                     null,
                     null,
                     null,
                     DateTimeOffset.UtcNow).Right();

        var sut = new TransferJourneyBuilder().BuildWithSteps();

        sut.SubmitContact(contact);
        sut.RemoveAllContacts();

        sut.Contacts.Should().BeEmpty();
    }

    public void ClearsPensionWiseDate()
    {
        var date = DateTimeOffset.UtcNow;

        var sut = new TransferJourneyBuilder().BuildWithSteps();
        sut.SetPensionWiseDate(date);
        sut.PensionWiseDate.Should().Be(date);

        sut.ClearPensionWiseDate();

        sut.PensionWiseDate.Should().BeNull();
    }

    public void ClearsFinancialAdviseDate()
    {
        var date = DateTimeOffset.UtcNow;
        var sut = new TransferJourneyBuilder().BuildWithSteps();
        sut.SetFinancialAdviseDate(date);
        sut.FinancialAdviseDate.Should().Be(date);

        sut.ClearFinancialAdviseDate();

        sut.FinancialAdviseDate.Should().BeNull();
    }

    public void ClearsFlexibleBenefitsData()
    {
        var date = DateTimeOffset.UtcNow;
        var sut = new TransferJourneyBuilder().BuildWithSteps();
        sut.SaveFlexibleBenefits("test-plan", "tets-type-of-payment", date.AddDays(-1).Date, date);
        sut.NameOfPlan.Should().Be("test-plan");
        sut.TypeOfPayment.Should().Be("tets-type-of-payment");
        sut.DateOfPayment.Should().Be(date.AddDays(-1).Date);

        sut.ClearFlexibleBenefitsData();

        sut.NameOfPlan.Should().BeNull();
        sut.TypeOfPayment.Should().BeNull();
        sut.DateOfPayment.Should().BeNull();
    }

    public void CanSubmitContactAddress()
    {
        var date = DateTimeOffset.UtcNow;
        var orginalEffectiveDate = DateTimeOffset.UtcNow;

        var transferJourneyContact = new TransferJourneyContactFactory();
        var contact = transferJourneyContact.Create(
                     "tesName",
                     null,
                     "testCompanyName",
                     Email.Create("test@gmail.com").Right(),
                     Phone.Create("370", "67878788").Right(),
                     "Ifa",
                     "Scheme name",
                     DateTimeOffset.UtcNow).Right();

        var sut = new TransferJourneyBuilder().BuildWithSteps();

        sut.SubmitContact(contact);

        sut.Contacts.Count.Should().Be(1);
        sut.Contacts[0].Name.Should().Be("tesName");
        sut.Contacts[0].Address.Should().Be(Address.Empty());

        var error = sut.SubmitContactAddress(Address.Create("Towers Watson Westgate",
            "122-130 Station Road",
            "test dada for addres3",
            "Redhill",
            "Surrey",
            "United Kingdom",
            "GB",
            "RH1 1WS").Right(), "Ifa");

        error.HasValue.Should().BeFalse();
        sut.Contacts.Count.Should().Be(1);
        sut.Contacts[0].Name.Should().Be("tesName");
        sut.Contacts[0].Address.StreetAddress1.Should().Be("Towers Watson Westgate");
        sut.Contacts[0].Address.StreetAddress2.Should().Be("122-130 Station Road");
        sut.Contacts[0].Address.StreetAddress3.Should().Be("test dada for addres3");
        sut.Contacts[0].Address.StreetAddress4.Should().Be("Redhill");
        sut.Contacts[0].Address.StreetAddress5.Should().Be("Surrey");
        sut.Contacts[0].Address.Country.Should().Be("United Kingdom");
        sut.Contacts[0].Address.CountryCode.Should().Be("GB");
        sut.Contacts[0].Address.PostCode.Should().Be("RH1 1WS");
    }

    public void FailsToSubmitContactAddress_WhenAddressSubmitionIsBeforeContactSubmition()
    {
        var date = DateTimeOffset.UtcNow;

        var sut = new TransferJourneyBuilder().BuildWithSteps();

        var error = sut.SubmitContactAddress(Address.Create("Towers Watson Westgate",
            "122-130 Station Road",
            "test dada for addres3",
            "Redhill",
            "Surrey",
            "United Kingdom",
            "GB",
            "RH1 1WS").Right(), "Ifa");

        error.HasValue.Should().BeTrue();
        error.Value.Message.Should().Be("Contact details must be submitted, before submitting its address details.");
    }

    public void CanSubmitContact_WhenTheSameTypeOfContactExists()
    {
        var date = DateTimeOffset.UtcNow;

        var transferJourneyContact1 = new TransferJourneyContactFactory();
        var contact1 = transferJourneyContact1.Create(
                     "tesName",
                     null,
                     "testCompanyName",
                     Email.Create("test@gmail.com").Right(),
                     Phone.Create("370", "67878788").Right(),
                     "Ifa",
                     "Scheme name",
                     DateTimeOffset.UtcNow).Right();

        var sut = new TransferJourneyBuilder().BuildWithSteps();
        sut.SubmitContact(contact1);
        sut.SubmitContactAddress(Address.Create("Towers Watson Westgate",
            "122-130 Station Road",
            "test dada for addres3",
            "Redhill",
            "Surrey",
            "United Kingdom",
            "GB",
            "RH1 1WS").Right(), "Ifa");

        sut.Contacts.Count.Should().Be(1);
        sut.Contacts[0].Name.Should().Be("tesName");
        sut.Contacts[0].CompanyName.Should().Be("testCompanyName");
        sut.Contacts[0].Email.Address.Should().Be("test@gmail.com");
        sut.Contacts[0].Phone.FullNumber.Should().Be("370 67878788");
        sut.Contacts[0].Address.PostCode.Should().Be("RH1 1WS");

        var transferJourneyContact2 = new TransferJourneyContactFactory();
        var contact2 = transferJourneyContact2.Create(
                     "tesName2",
                     null,
                     "testCompanyName2",
                     Email.Create("test@gmail.com2").Right(),
                     Phone.Create("370", "67878789").Right(),
                     "Ifa",
                     "Scheme name",
                     DateTimeOffset.UtcNow).Right();

        sut.SubmitContact(contact2);

        sut.Contacts.Count.Should().Be(1);
        sut.Contacts[0].Name.Should().Be("tesName2");
        sut.Contacts[0].CompanyName.Should().Be("testCompanyName2");
        sut.Contacts[0].Email.Address.Should().Be("test@gmail.com2");
        sut.Contacts[0].Phone.FullNumber.Should().Be("370 67878789");
        sut.Contacts[0].Address.PostCode.Should().Be("RH1 1WS");
    }

    public void CanSetGbgId()
    {
        var gbgId = Guid.NewGuid();
        var sut = new TransferJourneyBuilder().BuildWithSteps();

        sut.SaveGbgId(gbgId);

        sut.GbgId.Should().Be(gbgId);
    }

    public void CanSetTransferSummaryImageId()
    {
        var imageId = 1;
        var sut = new TransferJourneyBuilder().BuildWithSteps();

        sut.SaveTransferSummaryImageId(imageId);

        sut.TransferSummaryImageId.Should().Be(imageId);
    }

    public void CanUpdateLastJourneyCurrentPageKeyToHub()
    {
        var date = DateTimeOffset.UtcNow;
        var questionForm = new QuestionForm("test_question_key", "test_answer_key");
        var sut = new TransferJourneyBuilder().BuildWithSteps();
        var journeyBranch = sut.JourneyBranches[0];

        journeyBranch.SubmitStep(JourneyStep.Create("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow, questionForm));
        journeyBranch.SubmitStep(JourneyStep.Create("retirement_start_3", "next_page_test_4", DateTimeOffset.UtcNow, questionForm));
        journeyBranch.SubmitStep(JourneyStep.Create("retirement_start_4", "next_page_test_5", DateTimeOffset.UtcNow, questionForm));

        sut.RemoveStepsAndUpdateLastStepCurrentPageKeyToHub();

        sut.JourneyBranches[0].JourneySteps.Should().HaveCount(1);
        sut.JourneyBranches[0].JourneySteps[0].CurrentPageKey.Should().Be("hub");
        sut.JourneyBranches[0].JourneySteps[0].NextPageKey.Should().Be("next_page_test_5");
    }

    public void ReplaceAllStepsToSingleStep()
    {
        var sut = new TransferJourneyBuilder().BuildWithSteps();
        sut.TrySubmitStep("transfer_start_1", "next_page_test", DateTimeOffset.UtcNow, "q1", "a1");
        sut.TrySubmitStep("next_page_test", "next_page_test_2", DateTimeOffset.UtcNow, "q2", "a2");
        sut.TrySubmitStep("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow, "q3", "a3");
        sut.JourneyBranches.Single(x => x.IsActive).JourneySteps.Count.Should().Be(4);
        var step = JourneyStep.Create("hub", "t2_guaranteed_value_2", DateTimeOffset.UtcNow);

        sut.ReplaceAllStepsTo(step);

        sut.JourneyBranches.Single(x => x.IsActive).JourneySteps.Count.Should().Be(1);
        sut.JourneyBranches.Single(x => x.IsActive).JourneySteps.Single().CurrentPageKey.Should().Be("hub");
        sut.JourneyBranches.Single(x => x.IsActive).JourneySteps.Single().NextPageKey.Should().Be("t2_guaranteed_value_2");
        sut.JourneyBranches.Single(x => x.IsActive).JourneySteps.Single().SequenceNumber.Should().Be(1);
    }

    public void MarksNextPageAsDeadEnd()
    {
        var sut = new TransferJourneyBuilder().BuildWithSteps();
        sut.TrySubmitStep("transfer_start_1", "next_page_test", DateTimeOffset.UtcNow, "q1", "a1");
        sut.TrySubmitStep("next_page_test", "next_page_test_2", DateTimeOffset.UtcNow, "q2", "a2");
        sut.TrySubmitStep("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow, "q3", "a3");

        sut.MarkNextPageAsDeadEnd("next_page_test");

        sut.JourneyBranches.Single(x => x.IsActive).JourneySteps.Single(x => x.CurrentPageKey == "next_page_test").IsNextPageAsDeadEnd.Should().BeTrue();
        sut.JourneyBranches.Single(x => x.IsActive).JourneySteps.Where(x => x.CurrentPageKey != "next_page_test").All(x => !x.IsNextPageAsDeadEnd).Should().BeTrue();
    }

    public void RemovesDeadEndandFollowingPages()
    {
        var sut = new TransferJourneyBuilder().BuildWithSteps();
        sut.TrySubmitStep("transfer_start_1", "next_page_test", DateTimeOffset.UtcNow, "q1", "a1");
        sut.TrySubmitStep("next_page_test", "next_page_test_2", DateTimeOffset.UtcNow, "q2", "a2");
        sut.TrySubmitStep("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow, "q3", "a3");

        sut.JourneyBranches.Single(x => x.IsActive).JourneySteps.Count.Should().Be(4);
        sut.MarkNextPageAsDeadEnd("next_page_test");
        sut.RemoveDeadEndSteps();

        sut.JourneyBranches.Single(x => x.IsActive).JourneySteps.Count.Should().Be(2);
        sut.JourneyBranches.Single(x => x.IsActive).JourneySteps.Max(x => x.SequenceNumber).Should().Be(2);
    }

    public void CanSubmitTransferApplication()
    {
        var date = DateTimeOffset.UtcNow;
        var sut = new TransferJourneyBuilder().Build();
        var caseNumber = "1234567";

        sut.Submit(caseNumber, date);

        sut.SubmissionDate.Should().NotBeNull();
        sut.SubmissionDate.Should().Be(date);
        sut.CaseNumber.Should().Be("1234567");
    }

    [Input("TAG1;TAG2", "TAG1;TAG2")]
    [Input(null, "")]
    public void CanAddDocumentsWithOrWithoutTags(string tags, string expectedResult)
    {
        var sut = new UploadedDocumentsBuilder()
            .FileName("TestFileName.pdf")
            .Tags(tags)
            .Build();

        sut.Tags.Should().Be(expectedResult);
    }

    public void CanUpdateDocumetTagsThenDocumetExists()
    {
        var sut = new UploadedDocumentsBuilder()
            .FileName("TestFileName.pdf")
            .Build();

        sut.UpdateTags(new List<string> { "TAG1", "TAG2" });

        sut.Tags.Should().Be("TAG1;TAG2");
    }

    [Input(33, true)]
    [Input(32, true)]
    [Input(31, true)]
    [Input(30, false)]
    [Input(29, false)]
    [Input(null, false)]
    public void ReturnsCorrectResultIfGbgStepOlderThan29Days(int? daysBefore, bool expectedResult)
    {
        var now = DateTimeOffset.UtcNow;

        var sut = new TransferJourneyBuilder().BuildWithSteps();
        sut.TrySubmitStep("transfer_start_1", "step2", DateTimeOffset.UtcNow.AddDays(-35));
        sut.TrySubmitStep("step2", "t2_submit_upload", DateTimeOffset.UtcNow.AddDays(-34));
        if (daysBefore.HasValue)
            sut.TrySubmitStep("t2_submit_upload", "step4", DateTimeOffset.UtcNow.AddDays(-daysBefore.Value));

        var result = sut.IsGbgStepOlderThan30Days(now);

        result.Should().Be(expectedResult);
    }

    [Input("", "", "2023-06-05")]
    [Input(null, null, null)]
    [Input("TestNameOfPlan", "TestTypeOfPayment", "2023-06-05")]
    public void SavesFlexibleBenefitsDetails_ThenValidDataIsProvided(string nameOfPlan, string typeOfPayment, string dateOfPayment)
    {
        var now = DateTimeOffset.UtcNow;

        var sut = new TransferJourneyBuilder().BuildWithSteps();
        sut.SaveFlexibleBenefits(nameOfPlan, typeOfPayment, dateOfPayment != null ? DateTime.Parse(dateOfPayment) : null, now);

        sut.NameOfPlan.Should().Be(nameOfPlan);
        sut.TypeOfPayment.Should().Be(typeOfPayment);
        sut.DateOfPayment.Should().Be(dateOfPayment != null ? DateTime.Parse(dateOfPayment) : null);
    }

    [Input("TestNameOfPlanTestNameOfPlanTestNameOfPlanPlanTestNameOfPlanTestNameOfPlan", "TestTypeOfPayment", "2023-06-05", "\'Name of Plan\' must be less or equal 50 characters length.")]
    [Input("TestNameOfPlan", "TestTypeOfPaymentTestTypeOfPaymentTestTypeOfPaymentTestTypeOfPaymentTestTypeOfPayment", "2023-06-05", "\'Type of Payment\' must be less or equal 50 characters length.")]
    [Input("TestNameOfPlan", "TestTypeOfPayment", "2023-06-16", "\'Date of Payment\' must be less or equal to today's date")]

    public void SavesFlexibleBenefitsReturnsCorrectError_ThenInvalidDataIsProvided(string nameOfPlan, string typeOfPayment, string dateOfPayment, string expectedErrorMessage)
    {
        var sut = new TransferJourneyBuilder().BuildWithSteps();
        var result = sut.SaveFlexibleBenefits(nameOfPlan, typeOfPayment, dateOfPayment != null ? DateTime.Parse(dateOfPayment) : null, DateTimeOffset.Parse("2023-06-05"));

        result.Value.Message.Should().Be(expectedErrorMessage);
    }

    public void ReturnsCorrectStepByKey()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = new TransferJourneyBuilder().BuildWithSteps();
        sut.TrySubmitStep("transfer_start_1", "step2", now.AddDays(-35));
        sut.TrySubmitStep("step2", "t2_submit_upload", now.AddDays(-34));

        var result = sut.GetStepByKey("transfer_start_1");

        result.IsSome.Should().BeTrue();
        result.Value().CurrentPageKey.Should().Be("transfer_start_1");
        result.Value().NextPageKey.Should().Be("step2");
        result.Value().SubmitDate.Should().Be(now.AddDays(-35));
    }

    public void ReturnsCorrectNextStepFromCurrentStep()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = new TransferJourneyBuilder().BuildWithSteps();
        sut.TrySubmitStep("transfer_start_1", "step2", now.AddDays(-35));
        sut.TrySubmitStep("step2", "t2_submit_upload", now.AddDays(-34));

        var step = sut.GetStepByKey("transfer_start_1");
        var result = sut.GetNextStepFrom(step.Value());

        result.IsSome.Should().BeTrue();
        result.Value().CurrentPageKey.Should().Be("step2");
        result.Value().NextPageKey.Should().Be("t2_submit_upload");
        result.Value().SubmitDate.Should().Be(now.AddDays(-34));
    }

    public void FindStepFromCurrentPageKeysReturnsCorrectStep()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = new TransferJourneyBuilder().BuildWithSteps();
        sut.TrySubmitStep("transfer_start_1", "step2", now.AddDays(-35));
        sut.TrySubmitStep("step2", "t2_submit_upload", now.AddDays(-34));

        var result = sut.FindStepFromCurrentPageKeys(new List<string> { "transfer_start_1", "next_page_test_11" });

        result.IsSome.Should().BeTrue();
        result.Value().CurrentPageKey.Should().Be("transfer_start_1");
        result.Value().NextPageKey.Should().Be("step2");
        result.Value().SubmitDate.Should().Be(now.AddDays(-35));
    }

    public void FindStepFromNextPageKeysReturnsCorrectStep()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = new TransferJourneyBuilder().BuildWithSteps();
        sut.TrySubmitStep("transfer_start_1", "step2", now.AddDays(-35));
        sut.TrySubmitStep("step2", "t2_submit_upload", now.AddDays(-34));

        var result = sut.FindStepFromNextPageKeys(new List<string> { "transfer_start_1111", "t2_submit_upload" });

        result.IsSome.Should().BeTrue();
        result.Value().CurrentPageKey.Should().Be("step2");
        result.Value().NextPageKey.Should().Be("t2_submit_upload");
        result.Value().SubmitDate.Should().Be(now.AddDays(-34));
    }

    public void ReturnNoneWhenStepWithRequiredKeyDoesNotExist()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = new TransferJourneyBuilder().BuildWithSteps();
        sut.TrySubmitStep("transfer_start_1", "step2", now.AddDays(-35));
        sut.TrySubmitStep("step2", "t2_submit_upload", now.AddDays(-34));

        var result = sut.GetStepByKey("transfer_start_11");

        result.IsNone.Should().BeTrue();
    }

    public void ReturnsCheckBoxesLists()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = new TransferJourneyBuilder().BuildWithSteps();
        sut.TrySubmitStep("transfer_start_1", "step2", now.AddDays(-35));
        sut.GetStepByKey("transfer_start_1")
            .Value()
            .AddCheckboxesList(new CheckboxesList("testKey1", new List<(string, bool)> { ("a1", true) }));

        var result = sut.CheckBoxesLists();

        result.Should().NotBeEmpty();
    }

    public void ReturnsJourneyGenericData()
    {
        var now = DateTimeOffset.UtcNow;
        var genericDataJson = "{\"test\":\"test\"}";
        var sut = new TransferJourneyBuilder().BuildWithSteps();
        sut.TrySubmitStep("transfer_start_1", "step2", now.AddDays(-35));
        sut.GetStepByKey("transfer_start_1")
            .Value()
            .UpdateGenericData("form_key_1", genericDataJson);

        var result = sut.GetJourneyGenericDataList().ToList();

        result.Should().NotBeEmpty();
        result[0].GenericDataJson.Should().Be(genericDataJson);
    }

    public void ReturnsJourneyStepsWithGenericData()
    {
        var now = DateTimeOffset.UtcNow;
        var genericDataJson = "{\"test\":\"test\"}";
        var sut = new TransferJourneyBuilder().BuildWithSteps();
        sut.TrySubmitStep("transfer_start_1", "step2", now.AddDays(-35));
        sut.TrySubmitStep("step2", "step3", now.AddDays(-35));
        sut.GetStepByKey("transfer_start_1")
            .Value()
            .UpdateGenericData("form_key_1", genericDataJson);

        var result = sut.GetJourneyStepsWithGenericData().ToList();

        result.Should().NotBeEmpty();
        result[0].CurrentPageKey.Should().Be("transfer_start_1");
        result[0].JourneyGenericDataList.Should().NotBeEmpty();
        result[0].JourneyGenericDataList[0].GenericDataJson.Should().Be(genericDataJson);
    }

    public void ReturnsJourneyStepsWithQuestionForms()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = new TransferJourneyBuilder().BuildWithSteps();
        sut.TrySubmitStep("transfer_start_1", "step2", now.AddDays(-35), "questionKey", "AnswerKey");
        sut.TrySubmitStep("step2", "step3", now.AddDays(-35));

        var result = sut.GetStepsWithQuestionForms().ToList();

        result.Should().HaveCount(1);
        result[0].CurrentPageKey.Should().Be("transfer_start_1");
        result[0].QuestionForm.QuestionKey.Should().Be("questionKey");
        result[0].QuestionForm.AnswerKey.Should().Be("AnswerKey");
        result[0].QuestionForm.AnswerValue.Should().BeNull();
    }

    public void ReturnsJourneyQuestionFormsWordingFlags()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = new TransferJourneyBuilder().BuildWithSteps();
        sut.TrySubmitStep("transfer_start_1", "step2", now.AddDays(-35), "questionKey", "AnswerKey");
        sut.TrySubmitStep("step2", "step3", now.AddDays(-35));

        var result = sut.GetQuestionFormsWordingFlags();

        result.Should().HaveCount(1);
        result.Single().Should().Be("questionKey-AnswerKey");
    }

    public void ReturnsJourneyStepsWithCheckboxLists()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = new TransferJourneyBuilder().BuildWithSteps();
        sut.TrySubmitStep("transfer_start_1", "step2", now.AddDays(-35), "questionKey", "AnswerKey");
        sut.TrySubmitStep("step2", "step3", now.AddDays(-35));
        sut.GetStepByKey("transfer_start_1")
           .Value()
           .AddCheckboxesList(new CheckboxesList("testKey1", new List<(string, bool)> { ("a1", true) }));

        var step2 = sut.GetStepByKey("step2").Value();
        step2.AddCheckboxesList(new CheckboxesList("testKey1", new List<(string, bool)> { ("b1", true), ("b2", false) }));
        step2.AddCheckboxesList(new CheckboxesList("testKey2", new List<(string, bool)> { ("c1", true) }));

        var result = sut.GetStepsWithCheckboxLists().ToList();

        result.Should().HaveCount(2);

        result.Single(x => x.PageKey == "transfer_start_1").Checkboxes.Should().HaveCount(1);
        result.Single(x => x.PageKey == "transfer_start_1").Checkboxes.Single().Key.Should().Be("testKey1");
        result.Single(x => x.PageKey == "transfer_start_1").Checkboxes.Single().Value.Single().Key.Should().Be("a1");
        result.Single(x => x.PageKey == "transfer_start_1").Checkboxes.Single().Value.Should().HaveCount(1);
        result.Single(x => x.PageKey == "transfer_start_1").Checkboxes.Single().Value.Single().AnswerValue.Should().Be(true);

        result.Single(x => x.PageKey == "step2").Checkboxes.Should().HaveCount(2);
        result.Single(x => x.PageKey == "step2").Checkboxes.Single(x => x.Key == "testKey1").Value.Should().HaveCount(2);
        result.Single(x => x.PageKey == "step2").Checkboxes.Single(x => x.Key == "testKey1").Value.Single(x => x.Key == "b1").AnswerValue.Should().Be(true);
        result.Single(x => x.PageKey == "step2").Checkboxes.Single(x => x.Key == "testKey1").Value.Single(x => x.Key == "b2").AnswerValue.Should().Be(false);

        result.Single(x => x.PageKey == "step2").Checkboxes.Single(x => x.Key == "testKey2").Value.Should().HaveCount(1);
        result.Single(x => x.PageKey == "step2").Checkboxes.Single(x => x.Key == "testKey2").Value.Single(x => x.Key == "c1").AnswerValue.Should().Be(true);
    }
}