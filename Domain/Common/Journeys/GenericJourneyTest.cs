using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using WTW.MdpService.Domain.Common.Journeys;
using WTW.TestCommon.FixieConfig;
using WTW.Web.Extensions;
using WTW.Web.LanguageExt;
using LanguageExt;
using LanguageExt.Common;

namespace WTW.MdpService.Test.Domain.Common.Journeys;

public class GenericJourneyTest
{
    public void CanCreateGenericJourney()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = new GenericJourney("RBS", "1234567", "transfer", "step1", "step2", true, null, now);

        sut.BusinessGroup.Should().Be("RBS");
        sut.ReferenceNumber.Should().Be("1234567");
        sut.Type.Should().Be("transfer");
        sut.Status.Should().Be("Started");
        sut.StartDate.Should().Be(now);
        sut.SubmissionDate.Should().BeNull();
        sut.JourneyBranches.Count.Should().Be(1);
        sut.IsMarkedForRemoval.Should().BeTrue();
        sut.ExpirationDate.Should().BeNull();
    }

    public void ClonesGenericDataAndCheckBoxlistsOnNewBranchCreation()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = new GenericJourney("RBS", "1234567", "transfer", "0", "1", true, "Started", now, now.AddDays(30));
        sut.TrySubmitStep("1", "2", DateTimeOffset.UtcNow);
        sut.TrySubmitStep("2", "3", DateTimeOffset.UtcNow);
        sut.TrySubmitStep("3", "4", "InProgress", DateTimeOffset.UtcNow);
        var step = sut.GetStepByKey("2");
        step.Value().UpdateGenericData("test_form_key", "data");
        step.Value().AddCheckboxesList(new CheckboxesList("test-checkboxlist-key", new List<(string, bool)> { ("key1", true), ("key2", true) }));
        sut.TrySubmitStep("3", "4.1", DateTimeOffset.UtcNow);

        sut.ExpirationDate.Should().Be(now.AddDays(30));
        sut.Status.Should().Be("InProgress");
        sut.JourneyBranches.Should().HaveCount(2);
        sut.JourneyBranches[0].IsActive.Should().BeFalse();
        sut.JourneyBranches[1].IsActive.Should().BeTrue();
        sut.JourneyBranches[1].JourneySteps.Single(x => x.CurrentPageKey == "2").JourneyGenericDataList.Should().HaveCount(1);
        sut.JourneyBranches[1].JourneySteps.Single(x => x.CurrentPageKey == "2").CheckboxesLists.Should().HaveCount(1);
        sut.JourneyBranches[1].JourneySteps.Single(x => x.CurrentPageKey == "2").CheckboxesLists[0].Checkboxes.Should().HaveCount(2);
    }

    [Input("invalid-form-key", "{\"gbgId\":\"c62020f0-4175-4d52-bedd-30756f2b41b4\"}", "JSON string is null")]
    [Input("gbg_user_identification_form_id", "{\"gbgId-invalid-property-name\":\"c62020f0-4175-4d52-bedd-30756f2b41b4\"}", "Field gbgId not found in JSON string")]
    public void GetGbgIdReturnsNone(string formKey, string genericDataJson, string errorMessage)
    {
        var now = DateTimeOffset.UtcNow;
        var sut = new GenericJourney("RBS", "1234567", "transfer", "step1", "step2", true, null, now);
        var step = sut.GetStepByKey("step1");
        step.Value().UpdateGenericData(formKey, genericDataJson);
        var genericData = sut.GetGenericDataByFormKey("gbg_user_identification_form_id")?.GenericDataJson;

        var result = genericData.GetValueFromJson("gbgId");

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be(errorMessage);
    }

    public void GetGbgIdReturnsGbgId()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = new GenericJourney("RBS", "1234567", "transfer", "step1", "step2", true, null, now);
        var step = sut.GetStepByKey("step1");
        step.Value().UpdateGenericData("gbg_user_identification_form_id", "{\"gbgId\":\"c62020f0-4175-4d52-bedd-30756f2b41b4\"}");

        var result = sut.GetGenericDataByFormKey("gbg_user_identification_form_id").GenericDataJson.GetValueFromJson("gbgId");

        result.Right().Should().Be("c62020f0-4175-4d52-bedd-30756f2b41b4");
    }

    public void OverRidesNextPageKeyValueWhenExistingStepNextPageKeyNotSet()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = new GenericJourney("RBS", "1234567", "transfer", "0", "1", true, "Started", now, now.AddDays(30));
        sut.TrySubmitStep("1", "2", DateTimeOffset.UtcNow);
        sut.TrySubmitStep("2", "3", DateTimeOffset.UtcNow);
        sut.TrySubmitStep("3", "next_page_is_not_set", DateTimeOffset.UtcNow);
        sut.TrySubmitStep("3", "4.1", DateTimeOffset.UtcNow);

        sut.JourneyBranches.Should().HaveCount(1);
        sut.JourneyBranches[0].IsActive.Should().BeTrue();
        sut.JourneyBranches[0].JourneySteps.Single(x => x.CurrentPageKey == "3").NextPageKey.Should().Be("4.1");
        sut.JourneyBranches[0].JourneySteps.Single(x => x.CurrentPageKey == "3").SequenceNumber.Should().Be(4);
    }

    public void SubmitsJourney()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = new GenericJourney("RBS", "1234567", "transfer", "0", "1", true, "Started", now, now.AddDays(30));

        sut.SubmitJourney(now.AddDays(3));

        sut.Status.Should().Be("Submitted");
        sut.SubmissionDate.Should().Be(now.AddDays(3));
    }

    public void SetsExpiredStatus()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = new GenericJourney("RBS", "1234567", "transfer", "0", "1", true, "Started", now, now.AddDays(30));

        sut.SetExpiredStatus();

        sut.Status.Should().Be("expired");
    }

    public void GetsFirstStepOfJourney()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = new GenericJourney("RBS", "1234567", "transfer", "0", "1", true, "Started", now, now.AddDays(30));
        sut.TrySubmitStep("1", "2", DateTimeOffset.UtcNow);
        sut.TrySubmitStep("2", "3", DateTimeOffset.UtcNow);

        var result = sut.GetFirstStep();

        result.CurrentPageKey.Should().Be("0");
        result.NextPageKey.Should().Be("1");
    }

    [Input("0.1", "1", false, null)]
    [Input("1.0", "1.4", true, null)]
    [Input("2.1", "2.2", true, null)]
    public void GetIncompleteStageStatus(string stageStartStep, string stageEndStep, bool inProgress, string firstPage)
    {
        var now = DateTimeOffset.UtcNow;
        var sut = new GenericJourney("RBS", "1234567", "transfer", "0", "1.0", true, "Started", now.AddMinutes(-5));
        sut.TrySubmitStep("1.0", "1.1", now.AddMinutes(-4));
        sut.TrySubmitStep("1.1", "1.2", now.AddMinutes(-3));
        sut.TrySubmitStep("1.2", "2", now.AddMinutes(-2));
        sut.TrySubmitStep("2", "2.1", now.AddMinutes(-1));
        var result = sut.GetStageStatus(new List<GenericJourneyStage>
        {
            new GenericJourneyStage
            {
                Stage = "Stage1",
                Page = new GenericJourneyStagePage
                {
                    StageStartSteps = new List<string> { stageStartStep },
                    StageEndSteps = new List<string> { stageEndStep }
                }
            }
        });

        result.Should().HaveCount(1);
        result.First().Stage.Should().Be("Stage1");
        result.First().InProgress.Should().Be(inProgress);
        result.First().FirstPageKey.Should().Be(firstPage);
        result.First().CompletedDate.Should().Be(null);
    }

    [Input("1.0", "1.2", false, "1.0")]
    [Input("2", "2.1", false, "2")]
    public void GetCompletedteStageStatus(string stageStartStep, string stageEndStep, bool inProgress, string firstPage)
    {
        var now = DateTime.UtcNow;
        var sut = new GenericJourney("RBS", "1234567", "transfer", "0", "1.0", true, "Started", now.AddMinutes(-5));
        sut.TrySubmitStep("1.0", "1.1", now.AddMinutes(-4));
        sut.TrySubmitStep("1.1", "1.2", now);
        sut.TrySubmitStep("1.2", "2", now.AddMinutes(-2));
        sut.TrySubmitStep("2", "2.1", now);
        var result = sut.GetStageStatus(new List<GenericJourneyStage>
        {
            new GenericJourneyStage
            {
                Stage = "Stage1",
                Page = new GenericJourneyStagePage
                {
                    StageStartSteps = new List<string> { stageStartStep },
                    StageEndSteps = new List<string> { stageEndStep }
                }
            }
        });

        result.Should().HaveCount(1);
        result.First().Stage.Should().Be("Stage1");
        result.First().InProgress.Should().Be(inProgress);
        result.First().FirstPageKey.Should().Be(firstPage);
        result.First().CompletedDate.Should().Be(now);
    }

    [Input("2024-04-17", false)]
    [Input("2024-05-11", true)]
    public void GetsJourneyExpiryStatus(string todaysDate, bool expectedResult)
    {
        var sut = new GenericJourney("RBS", "1234567", "transfer", "0", "1", true, "Started",
            DateTimeOffset.Parse("2024-04-11"), DateTimeOffset.Parse("2024-05-01"));

        var result = sut.IsExpired(DateTimeOffset.Parse(todaysDate));

        result.Should().Be(expectedResult);
    }

    public void GetsWordingFlags_WhenNoFlagsExists()
    {
        var sut = new GenericJourney("RBS", "1234567", "transfer", "0", "1", true, "Started",
            DateTimeOffset.Parse("2024-04-11"), DateTimeOffset.Parse("2024-05-01"));

        var flags = sut.GetWordingFlags();

        flags.Should().BeEmpty();
    }

    public void GetsWordingFlags_WhenWordingFlagsExists()
    {
        var sut = new GenericJourney("RBS", "1234567", "transfer", "0", "1", true, "Started",
            DateTimeOffset.Parse("2024-04-11"), DateTimeOffset.Parse("2024-05-01"));
        sut.SetWordingFlags(new List<string> { "a-b", "c-d", " ", "d-e", "" });

        var flags = sut.GetWordingFlags();

        flags.Should().HaveCount(3);
        flags.Should().Contain("a-b", "c-d", "d-e");
    }

    public void SetsWordingFlags_WhenCollectionIsNotEmpty()
    {
        var sut = new GenericJourney("RBS", "1234567", "transfer", "0", "1", true, "Started",
            DateTimeOffset.Parse("2024-04-11"), DateTimeOffset.Parse("2024-05-01"));

        sut.SetWordingFlags(new List<string> { "a-b", "c-d", " ", "d-e", "" });

        sut.WordingFlags.Should().Be("a-b;c-d; ;d-e;");
    }

    public void SetsWordingFlags_WhenCollectionIsEmpty()
    {
        var sut = new GenericJourney("RBS", "1234567", "transfer", "0", "1", true, "Started",
            DateTimeOffset.Parse("2024-04-11"), DateTimeOffset.Parse("2024-05-01"));

        sut.SetWordingFlags(new List<string>());

        sut.WordingFlags.Should().BeNull();
    }

    public void SetsWordingFlags_WhenCollectionIsNull()
    {
        var sut = new GenericJourney("RBS", "1234567", "transfer", "0", "1", true, "Started",
            DateTimeOffset.Parse("2024-04-11"), DateTimeOffset.Parse("2024-05-01"));

        sut.SetWordingFlags(null);

        sut.WordingFlags.Should().BeNull();
    }

    public void RenewsStepUpdateDate()
    {
        var sut = new GenericJourney("RBS", "1234567", "transfer", "step1", "step2", true, "Started",
            DateTimeOffset.Parse("2024-04-11"), DateTimeOffset.Parse("2024-05-30"));
        sut.TrySubmitStep("step2", "step3", DateTimeOffset.Parse("2024-06-01"));

        sut.RenewStepUpdatedDate("step2", "step3", DateTimeOffset.Parse("2024-06-02"));

        sut.GetStepByKey("step1").Value().SubmitDate.Should().Be(DateTimeOffset.Parse("2024-04-11"));
        sut.GetStepByKey("step1").Value().UpdateDate.Should().Be(DateTimeOffset.Parse("2024-04-11"));
        sut.GetStepByKey("step2").Value().SubmitDate.Should().Be(DateTimeOffset.Parse("2024-06-01"));
        sut.GetStepByKey("step2").Value().UpdateDate.Should().Be(DateTimeOffset.Parse("2024-06-02"));
    }
}