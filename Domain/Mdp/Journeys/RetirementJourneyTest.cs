using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using WTW.MdpService.Domain.Mdp;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.Retirement;
using WTW.TestCommon.FixieConfig;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Domain.Mdp.Journeys
{
    public class RetirementJourneyTest
    {
        public void CreatesInitialRetirementJourney()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var quote = MemberQuote.Create(utcNow,
                "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 22, false, 85, 55,
                65, utcNow, utcNow, utcNow, "15", "16", 17, "PV", "SPD", 1, 9);

            quote.IsLeft.Should().Be(false);

            var sut = new RetirementJourney("0304442", "RBS", utcNow, "current", "next", quote.Right(), 7, 37);

            sut.Should().NotBeNull();
            sut.JourneyBranches.Should().NotBeNull();
            sut.JourneyBranches.Count.Should().Be(1);
            sut.StartDate.Should().Be(utcNow);
            sut.BusinessGroup.Should().Be("0304442");
            sut.ReferenceNumber.Should().Be("RBS");
            sut.Calculation.Should().BeNull();
            sut.JourneyBranches.Count.Should().Be(1);
            sut.JourneyBranches.Single().JourneySteps.Count.Should().Be(1);
            sut.JourneyBranches.Single().JourneySteps.Single().CurrentPageKey.Should().Be("current");
            sut.JourneyBranches.Single().JourneySteps.Single().NextPageKey.Should().Be("next");

            sut.MemberQuote.SearchedRetirementDate.Should().Be(utcNow);
            sut.MemberQuote.Label.Should().Be("label");
            sut.MemberQuote.AnnuityPurchaseAmount.Should().Be(1010.99m);
            sut.MemberQuote.LumpSumFromDb.Should().Be(10);
            sut.MemberQuote.LumpSumFromDc.Should().Be(11);
            sut.MemberQuote.SmallPotLumpSum.Should().Be(12);
            sut.MemberQuote.TaxFreeUfpls.Should().Be(13);
            sut.MemberQuote.TaxableUfpls.Should().Be(14);
            sut.MemberQuote.TotalLumpSum.Should().Be(15);
            sut.MemberQuote.TotalPension.Should().Be(16);
            sut.MemberQuote.TotalSpousePension.Should().Be(17);
            sut.MemberQuote.TotalUfpls.Should().Be(18);
            sut.MemberQuote.TransferValueOfDc.Should().Be(19);
            sut.MemberQuote.MinimumLumpSum.Should().Be(20);
            sut.MemberQuote.MaximumLumpSum.Should().Be(21);
            sut.MemberQuote.TrivialCommutationLumpSum.Should().Be(22);
            sut.MemberQuote.HasAvcs.Should().Be(false);
            sut.MemberQuote.LtaPercentage.Should().Be(85);
            sut.MemberQuote.EarliestRetirementAge.Should().Be(55);
            sut.MemberQuote.NormalRetirementAge.Should().Be(65);
            sut.MemberQuote.NormalRetirementDate.Should().Be(utcNow);
            sut.MemberQuote.DatePensionableServiceCommenced.Should().Be(utcNow);
            sut.MemberQuote.DateOfLeaving.Should().Be(utcNow);
            sut.MemberQuote.TransferInService.Should().Be("15");
            sut.MemberQuote.TotalPensionableService.Should().Be("16");
            sut.MemberQuote.FinalPensionableSalary.Should().Be(17);
            sut.MemberQuote.CalculationType.Should().Be("PV");
            sut.MemberQuote.WordingFlags.Should().Be("SPD");
            sut.MemberQuote.PensionOptionNumber.Should().Be(1);
            sut.MemberQuote.StatePensionDeduction.Should().Be(9);
        }

        public void CanSubmitRetirementStepWithQuestionForm()
        {
            var currentPageKey = "retirement_start";
            var nextPageKey = "next_page_test";
            var date = DateTimeOffset.UtcNow;
            var questionKey = "test_question_key";
            var answerKey = "test_answer_key";

            var quote = MemberQuote.Create(date,
                "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 22, false, 85, 55,
                65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);

            quote.IsLeft.Should().Be(false);
            var sut = new RetirementJourney("0304442", "RBS", date, "current", currentPageKey, quote.Right(), 7, 37);
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

        public void CanSubmitRetirementStepWithLifetimeAllowanceForm()
        {
            var currentPageKey = "retirement_start";
            var nextPageKey = "next_page_test";
            var date = DateTimeOffset.UtcNow;
            var percentage = 98;
            var quote = MemberQuote.Create(date,
                "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 22, false, 85, 55,
                65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);

            quote.IsLeft.Should().Be(false);
            var sut = new RetirementJourney("0304442", "RBS", date, currentPageKey, nextPageKey, quote.Right(), 7, 37);
            var result = sut.SetEnteredLtaPercentage(percentage);

            result.Right().Should().BeTrue();
            sut.JourneyBranches.Count.Should().Be(1);
            sut.JourneyBranches[0].JourneySteps.Count.Should().Be(1);

            var step = sut.JourneyBranches.Single().JourneySteps.Where(x => x.CurrentPageKey == currentPageKey);
            step.Count().Should().Be(1);
            step.Single().CurrentPageKey.Should().Be(currentPageKey);
            step.Single().NextPageKey.Should().Be(nextPageKey);
            step.Single().SubmitDate.Should().Be(date);
            result.IsRight.Should().BeTrue();
            result.Right().Should().BeTrue();
            sut.EnteredLtaPercentage.Should().Be(percentage);
        }

        public void CanSubmitRetirementStepWithLifetimeAllowanceForm_ZeroPercentagePassed()
        {
            var currentPageKey = "retirement_start";
            var date = DateTimeOffset.UtcNow;
            var percentage = 0;

            var quote = MemberQuote.Create(date,
                "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 22, false, 85, 55,
                65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);

            quote.IsLeft.Should().Be(false);
            var sut = new RetirementJourney("0304442", "RBS", date, "current", currentPageKey, quote.Right(), 7, 37);
            var result = sut.SetEnteredLtaPercentage(percentage);

            result.Left().Message.Should().Be("Percentage should be between 1 and 1000 and should have only 2 digits after dot.");
        }

        public void CanSubmitRetirementStepWithLifetimeAllowanceForm_NotValidPercentagePassed()
        {
            var currentPageKey = "retirement_start";
            var date = DateTimeOffset.UtcNow;
            var percentage = 1000;
            var quote = MemberQuote.Create(date,
                "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 22, false, 85, 55,
                65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);

            quote.IsLeft.Should().Be(false);
            var sut = new RetirementJourney("0304442", "RBS", date, "current", currentPageKey, quote.Right(), 7, 37);
            var result = sut.SetEnteredLtaPercentage(percentage);

            result.Left().Message.Should().Be("Percentage should be between 1 and 1000 and should have only 2 digits after dot.");
        }

        public void CanSubmitRetirementStepWithLifetimeAllowanceForm_FloatingPointPercentagePassed()
        {
            var currentPageKey = "retirement_start";
            var nextPageKey = "next_page_test";
            var date = DateTimeOffset.UtcNow;
            var percentage = 88.98m;
            var quote = MemberQuote.Create(date,
                "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 22, false, 85, 55,
                65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);

            quote.IsLeft.Should().Be(false);
            var sut = new RetirementJourney("0304442", "RBS", date, currentPageKey, nextPageKey, quote.Right(), 7, 37);
            var result = sut.SetEnteredLtaPercentage(percentage);
            result.Right().Should().BeTrue();
            sut.JourneyBranches.Count.Should().Be(1);
            sut.JourneyBranches[0].JourneySteps.Count.Should().Be(1);

            var step = sut.JourneyBranches.Single().JourneySteps.Where(x => x.CurrentPageKey == currentPageKey);
            step.Count().Should().Be(1);
            step.Single().CurrentPageKey.Should().Be(currentPageKey);
            step.Single().NextPageKey.Should().Be(nextPageKey);
            step.Single().SubmitDate.Should().Be(date);
            result.IsRight.Should().BeTrue();
            result.Right().Should().BeTrue();
            sut.EnteredLtaPercentage.Should().Be(percentage);
        }

        public void CanSubmitRetirementStepWithLifetimeAllowanceForm_MinimumFloatingPointPercentagePassed()
        {
            var currentPageKey = "retirement_start";
            var nextPageKey = "next_page_test";
            var date = DateTimeOffset.UtcNow;
            var percentage = 0.01m;
            var quote = MemberQuote.Create(date,
                "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 22, false, 85, 55,
                65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);

            quote.IsLeft.Should().Be(false);
            var sut = new RetirementJourney("0304442", "RBS", date, currentPageKey, nextPageKey, quote.Right(), 7, 37);
            var result = sut.SetEnteredLtaPercentage(percentage);
            result.Right().Should().BeTrue();
            sut.JourneyBranches.Count.Should().Be(1);
            sut.JourneyBranches[0].JourneySteps.Count.Should().Be(1);

            var step = sut.JourneyBranches.Single().JourneySteps.Where(x => x.CurrentPageKey == currentPageKey);
            step.Count().Should().Be(1);
            step.Single().CurrentPageKey.Should().Be(currentPageKey);
            step.Single().NextPageKey.Should().Be(nextPageKey);
            step.Single().SubmitDate.Should().Be(date);
            result.IsRight.Should().BeTrue();
            result.Right().Should().BeTrue();
            sut.EnteredLtaPercentage.Should().Be(percentage);
        }

        public void CanSubmitRetirementStepWithLifetimeAllowanceForm_NotValidFloatingPointPercentagePassed()
        {
            var currentPageKey = "retirement_start";
            var date = DateTimeOffset.UtcNow;
            var percentage = 88.989m;
            var quote = MemberQuote.Create(date,
                "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 22, false, 85, 55,
                65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);

            quote.IsLeft.Should().Be(false);
            var sut = new RetirementJourney("0304442", "RBS", date, "current", currentPageKey, quote.Right(), 7, 37);
            var result = sut.SetEnteredLtaPercentage(percentage);

            result.Left().Message.Should().Be("Percentage should be between 1 and 1000 and should have only 2 digits after dot.");
        }

        public void CanResubmitRetirementStepWithLifetimeAllowanceForm()
        {
            var currentPageKey = "retirement_start";
            var nextPageKey = "next_page_test";
            var date1 = new DateTimeOffset(new DateTime(2022, 01, 16, 10, 10, 10));
            var date2 = new DateTimeOffset(new DateTime(2022, 01, 18, 10, 10, 10));
            var percentage1 = 98;
            var percentage2 = 87;
            var quote = MemberQuote.Create(date1,
                "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 22, false, 85, 55,
                65, date2, date1, date2, "15", "16", 17, "PV", "SPD", 1, 9);

            quote.IsLeft.Should().Be(false);
            var sut = new RetirementJourney("0304442", "RBS", date1, currentPageKey, nextPageKey, quote.Right(), 7, 37);
            var submittedResult = sut.SetEnteredLtaPercentage(percentage1);

            submittedResult.Right().Should().BeTrue();
            sut.JourneyBranches.Count.Should().Be(1);
            sut.JourneyBranches[0].JourneySteps.Count.Should().Be(1);

            var submittedStep = sut.JourneyBranches.Single().JourneySteps.Where(x => x.CurrentPageKey == currentPageKey);
            submittedStep.Count().Should().Be(1);
            submittedStep.Single().CurrentPageKey.Should().Be(currentPageKey);
            submittedStep.Single().NextPageKey.Should().Be(nextPageKey);
            submittedStep.Single().SubmitDate.Should().Be(date1);
            sut.EnteredLtaPercentage.Should().Be(percentage1);

            var resubmittedResult = sut.SetEnteredLtaPercentage(percentage2);

            resubmittedResult.Right().Should().BeTrue();
            sut.JourneyBranches.Count.Should().Be(1);
            sut.JourneyBranches[0].JourneySteps.Count.Should().Be(1);

            var step = sut.JourneyBranches.Single().JourneySteps.Where(x => x.CurrentPageKey == currentPageKey);
            step.Count().Should().Be(1);
            step.Single().CurrentPageKey.Should().Be(currentPageKey);
            step.Single().NextPageKey.Should().Be(nextPageKey);
            sut.EnteredLtaPercentage.Should().Be(percentage2);
        }

        public void ReturnErrorMessageWhenInvalidCurrentStepProvided()
        {
            var date = DateTimeOffset.UtcNow;
            var quote = MemberQuote.Create(date,
                "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 22, false, 85, 55,
                65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);

            quote.IsLeft.Should().Be(false);
            var sut = new RetirementJourney("0304442", "RBS", date, "current", "currentPageKey", quote.Right(), 7, 37);
            sut.TrySubmitStep("retirement_start", "next_page_test", date, "test_question_key", "test_answer_key");

            var result = sut.TrySubmitStep("retirement_start_random", "next_page_test", DateTimeOffset.UtcNow, "test_question_key", "test_answer_key");

            result.Left().Message.Should().Be("Invalid \"currentPageKey\"");
            sut.JourneyBranches.Count.Should().Be(1);
            sut.JourneyBranches[0].JourneySteps.Count.Should().Be(1);
        }

        public void PreviousStepReturnSome_WhenActiveBranchExist()
        {
            var date = DateTimeOffset.UtcNow;
            var quote = MemberQuote.Create(date,
                "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 22, false, 85, 55,
                65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);

            quote.IsLeft.Should().Be(false);
            var sut = new RetirementJourney("0304442", "RBS", date, "current", "next_page_test", quote.Right(), 7, 37);
            sut.TrySubmitStep("next_page_test", "next_page_test_2", DateTimeOffset.UtcNow, "test_question_key", "test_answer_key");

            var result = sut.PreviousStep("next_page_test");

            result.IsSome.Should().BeTrue();
        }

        public void PreviousStepReturnNone_WhenActiveBranchNotExist()
        {
            var date = DateTimeOffset.UtcNow;
            var quote = MemberQuote.Create(date,
                "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 22, false, 85, 55,
                65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);

            quote.IsLeft.Should().Be(false);
            var sut = new RetirementJourney("0304442", "RBS", date, "current", "next_page_test", quote.Right(), 7, 37);

            var result = sut.PreviousStep("retirement_start");

            result.IsNone.Should().BeTrue();
        }

        public void PreviousStepReturnNone_WhenNoCurrentPageKeyExist()
        {
            var date = DateTimeOffset.UtcNow;
            var quote = MemberQuote.Create(date,
                "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 22, false, 85, 55,
                65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);

            quote.IsLeft.Should().Be(false);
            var sut = new RetirementJourney("0304442", "RBS", date, "current", "next_page_test", quote.Right(), 7, 37);
            sut.TrySubmitStep("retirement_start", "next_page_test", DateTimeOffset.UtcNow, "test_question_key", "test_answer_key");

            var result = sut.PreviousStep("retirement_start_random");

            result.IsNone.Should().BeTrue();
        }

        public void QuestionFormReturnSome_WhenActiveBranchExistAndCurrentStepHasQuestionForm()
        {
            var date = DateTimeOffset.UtcNow;
            var quote = MemberQuote.Create(date,
                "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 22, false, 85, 55,
                65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);

            quote.IsLeft.Should().Be(false);
            var sut = new RetirementJourney("0304442", "RBS", date, "current", "next_page_test", quote.Right(), 7, 37);
            sut.TrySubmitStep("retirement_start", "next_page_test", DateTimeOffset.UtcNow);
            sut.TrySubmitStep("next_page_test", "next_page_test_2", DateTimeOffset.UtcNow, "test_question_key", "test_answer_key");

            var result = sut.QuestionForm("next_page_test");

            result.IsSome.Should().BeTrue();
        }

        public void QuestionFormReturnsEmpty_WhenStepIsLastInActiveBranch()
        {
            var date = DateTimeOffset.UtcNow;
            var quote = MemberQuote.Create(date,
                "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 22, false, 85, 55,
                65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);

            quote.IsLeft.Should().Be(false);
            var sut = new RetirementJourney("0304442", "RBS", date, "current", "next_page_test", quote.Right(), 7, 37);
            sut.TrySubmitStep("retirement_start", "next_page_test", DateTimeOffset.UtcNow);

            var result = sut.QuestionForm("next_page_test");

            result.IsSome.Should().BeTrue();
            result.Value().QuestionKey.Should().BeNull();
            result.Value().AnswerKey.Should().BeNull();
            result.Value().AnswerValue.Should().BeNull();
        }

        public void QuestionFormReturnNone_WhenActiveBranchNotExist()
        {
            var date = DateTimeOffset.UtcNow;
            var quote = MemberQuote.Create(date,
                "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 22, false, 85, 55,
                65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);

            quote.IsLeft.Should().Be(false);
            var sut = new RetirementJourney("0304442", "RBS", date, "current", "next_page_test", quote.Right(), 7, 37);

            var result = sut.QuestionForm("retirement_start");

            result.IsNone.Should().BeTrue();
        }

        public void GetJourneyAnswer_Successful()
        {
            var questionKey1 = "question_key_1";
            var answerKey1 = "question_answer_1";
            var questionKey2 = "question_key_2";
            var answerKey2 = "question_answer_2";

            var date = DateTimeOffset.UtcNow;
            var quote = MemberQuote.Create(date,
                "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 22, false, 85, 55,
                65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);

            quote.IsLeft.Should().Be(false);
            var sut = new RetirementJourney("0304442", "RBS", date, "current", "retirement_start", quote.Right(), 7, 37);
            sut.TrySubmitStep("retirement_start", "next_page_test", DateTimeOffset.UtcNow, questionKey1, answerKey1);
            sut.TrySubmitStep("next_page_test", "next_page_test_2", DateTimeOffset.UtcNow, questionKey2, answerKey2);

            var result = sut.QuestionForms(new List<string>()).ToList();

            result.Count.Should().Be(2);
            result[0].QuestionKey.Should().Be(questionKey1);
            result[0].AnswerKey.Should().Be(answerKey1);
            result[1].QuestionKey.Should().Be(questionKey2);
            result[1].AnswerKey.Should().Be(answerKey2);
        }

        public void GetJourneyAnswer_SearchByQuestionKey()
        {
            var questionKey1 = "question_key_1";
            var answerKey1 = "question_answer_1";
            var questionKey2 = "question_key_2";
            var answerKey2 = "question_answer_2";

            var date = DateTimeOffset.UtcNow;
            var quote = MemberQuote.Create(date,
                "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 22, false, 85, 55,
                65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);

            quote.IsLeft.Should().Be(false);
            var sut = new RetirementJourney("0304442", "RBS", date, "retirement_start", "retirement_start1", quote.Right(), 7, 37);
            sut.TrySubmitStep("retirement_start1", "next_page_test", DateTimeOffset.UtcNow, questionKey1, answerKey1);
            sut.TrySubmitStep("next_page_test", "next_page_test_2", DateTimeOffset.UtcNow, questionKey2, answerKey2);

            var result = sut.QuestionForms(new List<string> { questionKey1 }).ToList();

            result.Single().QuestionKey.Should().Be(questionKey1);
            result.Single().AnswerKey.Should().Be(answerKey1);
        }

        public void GetJourneyAnswer_SearchByNonExistentQuestionKey()
        {
            var questionKey1 = "question_key_1";
            var answerKey1 = "question_answer_1";
            var questionKey2 = "question_key_2";
            var answerKey2 = "question_answer_2";

            var date = DateTimeOffset.UtcNow;
            var quote = MemberQuote.Create(date,
                "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 22, false, 85, 55,
                65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);

            quote.IsLeft.Should().Be(false);
            var sut = new RetirementJourney("0304442", "RBS", date, "current", "next_page_test", quote.Right(), 7, 37);
            sut.TrySubmitStep("retirement_start", "next_page_test", DateTimeOffset.UtcNow, questionKey1, answerKey1);
            sut.TrySubmitStep("next_page_test", "next_page_test_2", DateTimeOffset.UtcNow, questionKey2, answerKey2);

            var result = sut.QuestionForms(new List<string> { "non_existent_key" }).ToList();

            result.Count.Should().Be(0);
        }

        public void ReturnsCorrectPageKey_WhenStepIsFoundInActiveBranch()
        {
            var date = DateTimeOffset.UtcNow;
            var quote = MemberQuote.Create(date,
                "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 22, false, 85, 55,
                65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);

            quote.IsLeft.Should().Be(false);
            var sut = new RetirementJourney("0304442", "RBS", date, "current", "next_page_test", quote.Right(), 7, 37);
            sut.TrySubmitStep("retirement_start_1", "next_page_test", DateTimeOffset.UtcNow, "q1", "a1");
            sut.TrySubmitStep("next_page_test", "next_page_test_2", DateTimeOffset.UtcNow, "q2", "a2");

            var result = sut.GetRedirectStepPageKey("next_page_test");

            result.Should().Be("next_page_test");
        }

        public void ReturnsCorrectPageKey_WhenStepIsFoundInNotActiveBranch()
        {
            var date = DateTimeOffset.UtcNow;
            var quote = MemberQuote.Create(date,
                "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 22, false, 85, 55,
                65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);

            quote.IsLeft.Should().Be(false);
            var sut = new RetirementJourney("0304442", "RBS", date, "current", "retirement_start_1", quote.Right(), 7, 37);
            sut.TrySubmitStep("retirement_start_1", "next_page_test", DateTimeOffset.UtcNow, "q1", "a1");
            sut.TrySubmitStep("next_page_test", "next_page_test_2", DateTimeOffset.UtcNow, "q2", "a2");
            sut.TrySubmitStep("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow, "q3", "a3");
            sut.TrySubmitStep("retirement_start_1", "retirement_start_1_1", DateTimeOffset.UtcNow, "q4", "a4");
            sut.TrySubmitStep("retirement_start_1_1", "retirement_start_1_2", DateTimeOffset.UtcNow, "q5", "a5");

            var result = sut.GetRedirectStepPageKey("next_page_test_3");

            result.Should().Be("retirement_start_1_2");
        }

        [Input("retirement_start_1", 1)]
        [Input("retirement_start_1_1", 2)]
        [Input("next_page_test_2", 3)]
        [Input("current", 3)]
        public void RemovesStepsStartingWithFollowingKey(string pageKey, int expectedResult)
        {
            var date = DateTimeOffset.UtcNow;
            var quote = MemberQuote.Create(date,
                "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 22, false, 85, 55,
                65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);

            quote.IsLeft.Should().Be(false);
            var sut = new RetirementJourney("0304442", "RBS", date, "current", "retirement_start_1", quote.Right(), 7, 37);
            sut.TrySubmitStep("retirement_start_1", "next_page_test", DateTimeOffset.UtcNow, "q1", "a1");
            sut.TrySubmitStep("next_page_test", "next_page_test_2", DateTimeOffset.UtcNow, "q2", "a2");
            sut.TrySubmitStep("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow, "q3", "a3");
            sut.TrySubmitStep("retirement_start_1", "retirement_start_1_1", DateTimeOffset.UtcNow, "q4", "a4");
            sut.TrySubmitStep("retirement_start_1_1", "retirement_start_1_2", DateTimeOffset.UtcNow, "q5", "a5");

            sut.RemoveStepsStartingWith(pageKey);

            sut.JourneyBranches.Single(x => x.IsActive).JourneySteps.Count.Should().Be(expectedResult);
        }

        public void RemovesIncativeBranches()
        {
            var date = DateTimeOffset.UtcNow;
            var quote = MemberQuote.Create(date,
                "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 22, false, 85, 55,
                65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);

            quote.IsLeft.Should().Be(false);
            var sut = new RetirementJourney("0304442", "RBS", date, "current", "retirement_start_1", quote.Right(), 7, 37);
            sut.TrySubmitStep("retirement_start_1", "next_page_test", DateTimeOffset.UtcNow, "q1", "a1");
            sut.TrySubmitStep("next_page_test", "next_page_test_2", DateTimeOffset.UtcNow, "q2", "a2");
            sut.TrySubmitStep("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow, "q3", "a3");
            sut.TrySubmitStep("retirement_start_1", "retirement_start_1_1", DateTimeOffset.UtcNow, "q4", "a4");
            sut.TrySubmitStep("retirement_start_1_1", "retirement_start_1_2", DateTimeOffset.UtcNow, "q5", "a5");
            sut.JourneyBranches.Count.Should().Be(2);

            sut.RemoveInactiveBranches();

            sut.JourneyBranches.Count.Should().Be(1);
        }

        public void ReturnsCorrectPageKey_WhenFirstStepCuurentPageKeyProvided()
        {
            var date = DateTimeOffset.UtcNow;
            var quote = MemberQuote.Create(date,
                "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 22, false, 85, 55,
                65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);

            quote.IsLeft.Should().Be(false);
            var sut = new RetirementJourney("0304442", "RBS", date, "current", "retirement_start_1", quote.Right(), 7, 37);
            sut.TrySubmitStep("retirement_start_1", "next_page_test", DateTimeOffset.UtcNow, "q1", "a1");
            sut.TrySubmitStep("next_page_test", "next_page_test_2", DateTimeOffset.UtcNow, "q2", "a2");
            sut.TrySubmitStep("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow, "q3", "a3");
            sut.TrySubmitStep("retirement_start_1", "retirement_start_1_1", DateTimeOffset.UtcNow, "q4", "a4");
            sut.TrySubmitStep("retirement_start_1_1", "retirement_start_1_2", DateTimeOffset.UtcNow, "q5", "a5");

            var result = sut.GetRedirectStepPageKey("retirement_start_1");

            result.Should().Be("retirement_start_1");
        }

        public void CanSubmitRetirementJourney()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var quote = MemberQuote.Create(utcNow.AddDays(-7),
                "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 22, false, 85, 55,
                65, utcNow, utcNow, utcNow, "15", "16", 17, "PV", "SPD", 1, 9);
            var pdf = new byte[2];

            var sut = new RetirementJourney("0304442", "RBS", utcNow.AddDays(-7), "current", "retirement_start_1", quote.Right(), 7, 37);

            sut.Submit(pdf, utcNow, "1");

            sut.SubmissionDate.Should().Be(utcNow);
            sut.SummaryPdf.Should().BeEquivalentTo(pdf);
        }

        public void CanSetGbgId()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var quote = MemberQuote.Create(utcNow.AddDays(-7),
                "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 22, false, 85, 55,
                65, utcNow, utcNow, utcNow, "15", "16", 17, "PV", "SPD", 1, 9);
            var gbgId = Guid.NewGuid();

            var sut = new RetirementJourney("0304442", "RBS", utcNow.AddDays(-7), "current", "retirement_start_1", quote.Right(), 7, 37);

            sut.SaveGbgId(gbgId);

            sut.GbgId.Should().Be(gbgId);
        }

        public void CanSetFlags()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var quote = MemberQuote.Create(utcNow.AddDays(-7),
                "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 22, false, 85, 55,
                65, utcNow, utcNow, utcNow, "15", "16", 17, "PV", "SPD", 1, 9);

            var sut = new RetirementJourney("0304442", "RBS", utcNow.AddDays(-7), "current", "retirement_start_1", quote.Right(), 7, 37);
            sut.SetFlags(true, false);

            sut.AcknowledgeFinancialAdvisor.Should().BeTrue();
            sut.AcknowledgePensionWise.Should().BeFalse();
        }

        public void ReturnsExpiredRetirementApplicationStatus()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var quote = MemberQuote.Create(utcNow.AddDays(-7),
                "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 22, false, 85, 55,
                65, utcNow, utcNow, utcNow, "15", "16", 17, "PV", "SPD", 1, 9);

            var sut = new RetirementJourney("0304442", "RBS", utcNow.AddMonths(-3), "current", "retirement_start_1", quote.Right(), 2, 37);

            var result = sut.Status(utcNow);

            result.Should().Be(RetirementApplicationStatus.ExpiredRA);
        }

        public void ReturnsStartedRetirementApplicationStatus()
        {
            var utcNow = new DateTimeOffset(2022, 8, 2, 14, 59, 59, TimeSpan.FromHours(0));
            var quote = MemberQuote.Create(utcNow.AddDays(38),
                "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 22, false, 85, 55,
                65, utcNow, utcNow, utcNow, "15", "16", 17, "PV", "SPD", 1, 9);

            var sut = new RetirementJourney("0304442", "RBS", utcNow, "current", "retirement_start_1", quote.Right(), 7, 37);

            var result = sut.Status(utcNow);

            result.Should().Be(RetirementApplicationStatus.StartedRA);
        }

        public void ReturnsSubmitttedRetirementApplicationStatus()
        {
            var utcNow = new DateTimeOffset(2022, 8, 2, 14, 59, 59, TimeSpan.FromHours(0));
            var quote = MemberQuote.Create(utcNow.AddDays(38),
                "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 22, false, 85, 55,
                65, utcNow, utcNow, utcNow, "15", "16", 17, "PV", "SPD", 1, 9);

            var sut = new RetirementJourney("0304442", "RBS", utcNow, "current", "retirement_start_1", quote.Right(), 7, 37);
            sut.Submit(new byte[2], utcNow, "1");

            var result = sut.Status(utcNow);

            result.Should().Be(RetirementApplicationStatus.SubmittedRA);
        }

        public void CanSubmitFinancialAdviseForm_WhenFinancialDateIsLessThanToday()
        {
            var currentPageKey = "retirement_start";
            var nextPageKey = "next_page_test";
            var date = DateTimeOffset.UtcNow;
            var financialAdviseDate = DateTime.UtcNow.AddDays(-1);
            var quote = MemberQuote.Create(date,
                "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 22, false, 85, 55,
                65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);

            quote.IsLeft.Should().Be(false);
            var sut = new RetirementJourney("0304442", "RBS", date, currentPageKey, nextPageKey, quote.Right(), 7, 37);
            var result = sut.SetFinancialAdviseDate(financialAdviseDate);

            result.HasValue.Should().BeFalse();
            sut.JourneyBranches.Count.Should().Be(1);
            sut.JourneyBranches[0].JourneySteps.Count.Should().Be(1);

            var step = sut.JourneyBranches.Single().JourneySteps.Where(x => x.CurrentPageKey == currentPageKey);
            step.Count().Should().Be(1);
            step.Single().CurrentPageKey.Should().Be(currentPageKey);
            step.Single().NextPageKey.Should().Be(nextPageKey);
            step.Single().SubmitDate.Should().Be(date);
            sut.FinancialAdviseDate.Should().Be(financialAdviseDate);
        }

        public void CanSubmitFinancialAdviseForm_NotValidWhenFinancialDateIsGreaterThanToday()
        {
            var currentPageKey = "retirement_start";
            var date = DateTimeOffset.UtcNow;
            var financialAdviseDate = DateTime.UtcNow.AddDays(1);
            var quote = MemberQuote.Create(date,
                "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 22, false, 85, 55,
                65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);

            quote.IsLeft.Should().Be(false);
            var sut = new RetirementJourney("0304442", "RBS", date, "current", currentPageKey, quote.Right(), 7, 37);
            var result = sut.SetFinancialAdviseDate(financialAdviseDate);

            result.Value.Message.Should().Be("Financial advise date should be less than or equal to today.");
        }

        public void CanSubmitPensionWiseForm_WhenPensionWiseDateIsLessThanToday()
        {
            var currentPageKey = "retirement_start";
            var nextPageKey = "next_page_test";
            var date = DateTimeOffset.UtcNow;
            var pensionWiseDate = DateTime.UtcNow.AddDays(-1);
            var quote = MemberQuote.Create(date,
                "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 22, false, 85, 55,
                65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);

            quote.IsLeft.Should().Be(false);
            var sut = new RetirementJourney("0304442", "RBS", date, currentPageKey, nextPageKey, quote.Right(), 7, 37);
            var result = sut.SetPensionWiseDate(pensionWiseDate);

            result.HasValue.Should().BeFalse();
            sut.JourneyBranches.Count.Should().Be(1);
            sut.JourneyBranches[0].JourneySteps.Count.Should().Be(1);

            var step = sut.JourneyBranches.Single().JourneySteps.Where(x => x.CurrentPageKey == currentPageKey);
            step.Count().Should().Be(1);
            step.Single().CurrentPageKey.Should().Be(currentPageKey);
            step.Single().NextPageKey.Should().Be(nextPageKey);
            step.Single().SubmitDate.Should().Be(date);
            sut.PensionWiseDate.Should().Be(pensionWiseDate);
        }

        public void CanSubmitPensionWiseForm_NotValidWhenPensionWiseIsGreaterThanToday()
        {
            var currentPageKey = "retirement_start";
            var date = DateTimeOffset.UtcNow;
            var pensionWiseDate = DateTime.UtcNow.AddDays(+1);
            var quote = MemberQuote.Create(date,
                "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 22, false, 85, 55,
                65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);

            quote.IsLeft.Should().Be(false);
            var sut = new RetirementJourney("0304442", "RBS", date, "current", currentPageKey, quote.Right(), 7, 37);
            var result = sut.SetPensionWiseDate(pensionWiseDate);

            result.Value.Message.Should().Be("Pension wise date should be less than or equal to today.");
        }

        public void CanSetOptOutPensionWise()
        {
            var currentPageKey = "retirement_start";
            var nextPageKey = "next_page_test";
            var date = DateTimeOffset.UtcNow;
            var optOutPensionWise = true;
            var quote = MemberQuote.Create(date,
                "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
                18, 19, 20, 21, 22, false, 85, 55,
                65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);

            quote.IsLeft.Should().Be(false);
            var sut = new RetirementJourney("0304442", "RBS", date, currentPageKey, nextPageKey, quote.Right(), 7, 37);
            sut.SetOptOutPensionWise(optOutPensionWise);

            sut.JourneyBranches.Count.Should().Be(1);
            sut.JourneyBranches[0].JourneySteps.Count.Should().Be(1);

            var step = sut.JourneyBranches.Single().JourneySteps.Where(x => x.CurrentPageKey == currentPageKey);
            step.Count().Should().Be(1);
            step.Single().CurrentPageKey.Should().Be(currentPageKey);
            step.Single().NextPageKey.Should().Be(nextPageKey);
            step.Single().SubmitDate.Should().Be(date);
            sut.OptOutPensionWise.Should().Be(optOutPensionWise);
        }

        public void UpdatesLastStepNextPageKey()
        {
            var date = DateTimeOffset.UtcNow;
            var quote = MemberQuote.Create(date,
               "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
               18, 19, 20, 21, 22, false, 85, 55,
               65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);

            var sut = new RetirementJourney("RBS", "0304442", date, "current", "retirement_start_1", quote.Right(), 2, 37);
            sut.TrySubmitStep("retirement_start_1", "next_page_test", DateTimeOffset.UtcNow, "q1", "a1");
            sut.TrySubmitStep("next_page_test", "next_page_test_2", DateTimeOffset.UtcNow, "q2", "a2");
            sut.TrySubmitStep("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow, "q3", "a3");
            sut.TrySubmitStep("retirement_start_1", "retirement_start_1_1", DateTimeOffset.UtcNow, "q4", "a4");
            sut.TrySubmitStep("retirement_start_1_1", "retirement_start_1_2", DateTimeOffset.UtcNow, "q5", "a5");
            sut.JourneyBranches.Single(x => x.IsActive).JourneySteps.Single(x => x.CurrentPageKey == "retirement_start_1_1").NextPageKey.Should().Be("retirement_start_1_2");

            var result = sut.UpdateStep("retirement_start_1_2", "retirement_start_1_2_1");

            result.HasValue.Should().BeFalse();
            sut.JourneyBranches.Single(x => x.IsActive).JourneySteps.Single(x => x.CurrentPageKey == "retirement_start_1_1").NextPageKey.Should().Be("retirement_start_1_2_1");
        }

        public void ReturnsErrorIfStepNotFound()
        {
            var date = DateTimeOffset.UtcNow;
            var quote = MemberQuote.Create(date,
              "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
              18, 19, 20, 21, 22, false, 85, 55,
              65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);
            var sut = new RetirementJourney("RBS", "0304442", date, "current", "retirement_start_1", quote.Right(), 2, 37);
            sut.TrySubmitStep("retirement_start_1", "next_page_test", DateTimeOffset.UtcNow, "q1", "a1");
            sut.TrySubmitStep("next_page_test", "next_page_test_2", DateTimeOffset.UtcNow, "q2", "a2");
            sut.TrySubmitStep("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow, "q3", "a3");
            sut.TrySubmitStep("retirement_start_1", "retirement_start_1_1", DateTimeOffset.UtcNow, "q4", "a4");
            sut.TrySubmitStep("retirement_start_1_1", "retirement_start_1_2", DateTimeOffset.UtcNow, "q5", "a5");

            var result = sut.UpdateStep("retirement_start_1_2_1", "retirement_start_1_2_2");

            result.HasValue.Should().BeTrue();
            result.Value.Message.Should().Be("Previous step does not exist in retiremnt journey for given current page key.");
        }

        public void SetsProperExpireDate()
        {
            var offset = TimeSpan.FromHours(0);
            var now = new DateTimeOffset(2022, 8, 8, 14, 59, 59, TimeSpan.FromHours(0));
            var scenarios = new Dictionary<DateTimeOffset, DateTimeOffset>
            {
                { new DateTimeOffset(2022, 8, 22, 0,0,0, offset), new DateTimeOffset(2022, 8, 15, 0,0,0, offset)},
                { new DateTimeOffset(2023, 1, 8, 0,0,0, offset), new DateTimeOffset(2022, 11, 6, 0,0,0, offset)},
                { new DateTimeOffset(2022, 9, 14, 0,0,0, offset), new DateTimeOffset(2022, 8, 15, 0,0,0, offset)},
            };

            foreach (var scenario in scenarios)
            {
                var quote = MemberQuote.Create(scenario.Key,
                  "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
                  18, 19, 20, 21, 22, false, 85, 55,
                  65, now, now, now, "15", "16", 17, "PV", "SPD", 1, 9);
                var sut = new RetirementJourney("RBS", "0304442", now, "current", "retirement_start_1", quote.Right(), 90, RetirementConstants.RetirementProcessingPeriodInDays);
                sut.ExpirationDate.Should().Be(scenario.Value);
            }
        }

        public void ReturnsNoneQuestionForms()
        {
            var date = DateTimeOffset.UtcNow;
            var quote = MemberQuote.Create(date,
              "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
              18, 19, 20, 21, 22, false, 85, 55,
              65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);
            var sut = new RetirementJourney("RBS", "0304442", date, "retirement_start", "next_page_test_1", quote.Right(), 2, 37);
            sut.TrySubmitStep("next_page_test_1", "next_page_test_2", DateTimeOffset.UtcNow);

            var result = sut.JourneyQuestions();

            result.Count().Should().Be(0);
        }

        public void ReturnsAllQuestionForms()
        {
            var date = DateTimeOffset.UtcNow;
            var quote = MemberQuote.Create(date,
              "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
              18, 19, 20, 21, 22, false, 85, 55,
              65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);
            var sut = new RetirementJourney("RBS", "0304442", date, "retirement_start", "next_page_test_1", quote.Right(), 2, 37);
            sut.TrySubmitStep("next_page_test_1", "next_page_test_2", DateTimeOffset.UtcNow, "question_key_1", "question_answer_1");
            sut.TrySubmitStep("next_page_test_2", "next_page_test_3", DateTimeOffset.UtcNow, "question_key_2", "question_answer_2");

            var result = sut.JourneyQuestions();

            result.Count().Should().Be(2);
        }

        public void ReturnsTrue_WhenRetirementJourneySubmited()
        {
            var date = DateTimeOffset.UtcNow;
            var quote = MemberQuote.Create(date,
              "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
              18, 19, 20, 21, 22, false, 85, 55,
              65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);
            var sut = new RetirementJourney("RBS", "0304442", date, "retirement_start", "next_page_test_1", quote.Right(), 2, 37);
            sut.Submit(new byte[1], date, "123456");

            var result = sut.IsRetirementJourneySubmitted();

            result.Should().BeTrue();
        }

        public void ReturnsFalse_WhenRetirementJourneyNotSubmited()
        {
            var date = DateTimeOffset.UtcNow;
            var quote = MemberQuote.Create(date,
              "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
              18, 19, 20, 21, 22, false, 85, 55,
              65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);
            var sut = new RetirementJourney("RBS", "0304442", date, "retirement_start", "next_page_test_1", quote.Right(), 2, 37);

            var result = sut.IsRetirementJourneySubmitted();

            result.Should().BeFalse();
        }

        public void ReturnsTrue_WhenRetirementJourneyExpired()
        {
            var date = DateTimeOffset.UtcNow.AddMonths(-3);
            var quote = MemberQuote.Create(date,
              "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
              18, 19, 20, 21, 22, false, 85, 55,
              65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);
            var sut = new RetirementJourney("RBS", "0304442", date, "retirement_start", "next_page_test_1", quote.Right(), 2, 37);

            var result = sut.HasRetirementJourneyExpired(DateTimeOffset.UtcNow);

            result.Should().BeTrue();
        }

        public void ReturnsFalse_WhenRetirementJourneyNotExpired()
        {
            var date = DateTimeOffset.UtcNow;
            var quote = MemberQuote.Create(DateTimeOffset.UtcNow.AddMonths(2),
              "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
              18, 19, 20, 21, 22, false, 85, 55,
              65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);
            var sut = new RetirementJourney("RBS", "0304442", date, "retirement_start", "next_page_test_1", quote.Right(), 2, 37);

            var result = sut.HasRetirementJourneyExpired(DateTimeOffset.UtcNow);

            result.Should().BeFalse();
        }

        public void ReturnsLtaPercentage()
        {
            var date = DateTimeOffset.UtcNow.AddMonths(-3);
            var quote = MemberQuote.Create(date,
              "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
              18, 19, 20, 21, 22, false, 85, 55,
              65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);
            var sut = new RetirementJourney("RBS", "0304442", date, "retirement_start", "lta_enter_amount", quote.Right(), 2, 37);
            sut.TrySubmitStep("lta_enter_amount", "some_page_key", DateTimeOffset.UtcNow);
            sut.SetEnteredLtaPercentage(22.22m);

            var result = sut.ActiveLtaPercentage();

            result.Should().Be(22.22m);
        }

        public void ReturnsNoValue_WhenLtaPercentageIsNotSetInActiveBranch()
        {
            var date = DateTimeOffset.UtcNow.AddMonths(-3);
            var quote = MemberQuote.Create(date,
              "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
              18, 19, 20, 21, 22, false, 85, 55,
              65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);
            var sut = new RetirementJourney("RBS", "0304442", date, "retirement_start", "next_page_test_1", quote.Right(), 2, 37);
            sut.SetEnteredLtaPercentage(22.22m);

            var result = sut.ActiveLtaPercentage();

            result.HasValue.Should().BeFalse();
        }

        public void ReturnsCorrectExpiryDate()
        {
            var offset = TimeSpan.FromHours(0);
            var now = new DateTimeOffset(2022, 8, 8, 14, 59, 59, TimeSpan.FromHours(0));
            var scenarios = new Dictionary<DateTimeOffset, DateTimeOffset>
            {
                { new DateTimeOffset(2022, 8, 22, 0,0,0, offset), new DateTimeOffset(2022, 8, 15, 0,0,0, offset)},
                { new DateTimeOffset(2023, 1, 8, 0,0,0, offset), new DateTimeOffset(2022, 11, 6, 0,0,0, offset)},
                { new DateTimeOffset(2022, 9, 14, 0,0,0, offset), new DateTimeOffset(2022, 8, 15, 0,0,0, offset)},
            };


            foreach (var scenario in scenarios)
            {
                var result = RetirementJourney.CalculateExpireDate(scenario.Key.Date, now, RetirementConstants.RetirementProcessingPeriodInDays, 90);

                result.Should().Be(scenario.Value);
            }
        }

        [Input(33, true)]
        [Input(32, true)]
        [Input(31, true)]
        [Input(30, false)]
        [Input(29, false)]
        [Input(null, false)]
        public void ReturnsCorrectResultIfGbgStepOlderThan30Days(int? daysBefore, bool expectedResult)
        {
            var now = DateTimeOffset.UtcNow;

            var date = DateTimeOffset.Parse("2022-12-18");
            var quote = MemberQuote.Create(date,
              "label", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
              18, 19, 20, 21, 22, false, 85, 55,
              65, date, date, date, "15", "16", 17, "PV", "SPD", 1, 9);
            var sut = new RetirementJourney("RBS", "0304442", date, "retirement_start", "next_page_test_1", quote.Right(), 2, 37);
            sut.TrySubmitStep("next_page_test_1", "step2", DateTimeOffset.UtcNow.AddDays(-35));
            sut.TrySubmitStep("step2", "submit_document_info", DateTimeOffset.UtcNow.AddDays(-34));
            if (daysBefore.HasValue)
                sut.TrySubmitStep("submit_document_info", "step4", DateTimeOffset.UtcNow.AddDays(-daysBefore.Value));

            var result = sut.IsGbgStepOlderThan30Days(now);

            result.Should().Be(expectedResult);
        }
    }
}