using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using WTW.MdpService.Domain.Common;
using WTW.MdpService.Domain.Common.Journeys;
using WTW.MdpService.Infrastructure.Templates.Common.CmsContentModels;
using WTW.MdpService.Infrastructure.Templates.GenericJourneys;
using WTW.MdpService.Retirement;
using WTW.MdpService.Test.Domain.Mdp;
using WTW.MdpService.Test.Domain.Members;

namespace WTW.MdpService.Test.Infrastructure.Templates.GenericJourneys;

public class GenericJourneysTemplateTest
{
    private GenericJourneysTemplate _sut;

    public GenericJourneysTemplateTest()
    {
        _sut = new GenericJourneysTemplate();
    }

    public async Task AppliesArgsFromCalculation()
    {
        var template = "{{forenames}} - {{cms_tokens.state_pension_deduction}}";
        var now = DateTimeOffset.UtcNow;
        var member = new MemberBuilder().Build();
        var journey = new GenericJourney("RBS", "1234567", "transfer1", "step1", "step2", true, "Started", DateTimeOffset.UtcNow);
        var documents = new List<UploadedDocument>
            {
                new UploadedDocumentsBuilder().Build(),
                new UploadedDocumentsBuilder().Build(),
            };
        var summaryBlocks = new List<SummaryBlock> ();
        var contentBlocks = new List<ContentBlockItem> ();
        var dataSummaries = new List<DataSummaryItem> ();
        var statePensionDeductionValue = (decimal?)783.45;
        var cmsTokens = new CmsTokenInformationResponse { StatePensionDeduction = statePensionDeductionValue };
        var expectedResult = $"{member.PersonalDetails.Forenames} - {statePensionDeductionValue.ToString()}";

        var sut = await _sut.RenderHtml(template, new 
        { Journey = journey,
          Forenames = member.PersonalDetails.Forenames,
          SystemDate = now,
          CaseNumber = "123456",
          Documents = documents,
          SummaryBlocks = summaryBlocks,
          ContentBlocks = contentBlocks,
          DataSummaries = dataSummaries,
          CmsTokens = cmsTokens
        });

        sut.Should().Be(expectedResult);
    }

    public async Task AppliesArgsFromSystemDate()
    {
        var template = "{{member_data.title}}";
        var now = DateTimeOffset.UtcNow;

        var sut = await _sut.RenderHtml(template, new { MemberData = new { Title = "Mister" } });

        sut.Should().NotBeNullOrWhiteSpace();
        sut.Should().Be("Mister");
    }

    public async Task AppliesArgsToFileNameFromMetaData()
    {
        var template = "<head><meta name=\"filename\" content=\"filename_test_{{member_surname}}.pdf\" /></head>";
        var now = DateTimeOffset.UtcNow;

        var sut = await _sut.RenderHtml(template, new { MemberSurname = "surname" });

        sut.Should().NotBeNullOrWhiteSpace();
        sut.Should().Be("<head><meta name=\"filename\" content=\"filename_test_surname.pdf\" /></head>");
    }
}