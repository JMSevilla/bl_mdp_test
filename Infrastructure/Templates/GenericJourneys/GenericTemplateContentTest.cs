using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Domain.Common;
using WTW.MdpService.Domain.Common.Journeys;
using WTW.MdpService.Infrastructure.Journeys;
using WTW.MdpService.Infrastructure.MdpApi;
using WTW.MdpService.Infrastructure.Templates.GenericJourneys;
using WTW.MdpService.Journeys.Submit.Services.DocumentsRenderer;
using WTW.MdpService.Test.Domain.Mdp.Calculations;
using WTW.MdpService.Test.Domain.Members;
using WTW.Web.Serialization;

namespace WTW.MdpService.Test.Infrastructure.Templates.GenericJourneys;

public class GenericTemplateContentTest
{
    private readonly Mock<ILogger<GenericTemplateContent>> _loggerMock;
    private readonly GenericTemplateContent _sut;
    private Mock<IMdpClient> _mdpClientMock;
    private readonly Mock<ICmsDataParser> _cmsParserMock;
    private readonly Mock<IGenericJourneyDetails> _genericJourneyDetailsMock;

    public GenericTemplateContentTest()
    {
        _mdpClientMock = new Mock<IMdpClient>();
        _cmsParserMock = new Mock<ICmsDataParser>();
        _genericJourneyDetailsMock = new Mock<IGenericJourneyDetails>();
        _loggerMock = new Mock<ILogger<GenericTemplateContent>>();
        _sut = new GenericTemplateContent(_mdpClientMock.Object, _cmsParserMock.Object, _genericJourneyDetailsMock.Object, _loggerMock.Object);
    }

    public async Task CanCreateDataSummaryBlocks()
    {
        var obj = new System.Dynamic.ExpandoObject() { };
        obj.TryAdd("SystemDate", "2024-12-12");

        _cmsParserMock.Setup(x => x.GetDataSummaryBlockSourceUris(It.IsAny<JsonElement>())).Returns(new List<Uri> { new Uri("http://www.google.com") });
        _mdpClientMock.Setup(x => x.GetData(It.IsAny<List<Uri>>(), It.IsAny<(string, string, string)>())).ReturnsAsync(obj);

        var member = new MemberBuilder().Build();
        var genericJourney = new GenericJourney("RBS", "1234567", "dcretirementapplication", "step1", "step2", true, "Started", DateTimeOffset.UtcNow);
        var documents = new List<UploadedDocument>()
        {
            new UploadedDocument("1234567", "RBS", "dcretirementapplication", null, "fileName", "uuid", DocumentSource.Outgoing, false, "tag"),
        };
        var summaryBlock = JsonSerializer.Deserialize<JsonElement>(TestData.SummaryBlocks, SerialiationBuilder.Options());

        var summaryBlocks = await _sut.GetDataSummaryBlocks(summaryBlock, obj, ("Bearer 123456", "dev2", "LIF"));

        summaryBlocks[1].SummaryItems[1].Header.Should().Be("Pension income");
        summaryBlocks[1].SummaryItems[1].Value.Should().Be("reducedPension.totalPension");
        summaryBlocks[1].BottomInformation.Values[0].Content
           .Should().Be("<p>If given, permission to deal with the person above regarding this application was provided on 12 Dec 2024</p>");

        summaryBlocks[1].SummaryItems[2].Header.Should().Be("Tax-free lump sum");
        summaryBlocks[1].SummaryItems[2].Value.Should().Be("reducedPension.totalLumpSum");
    }

    public async Task CanCreateDataSummaryBlocksUlsBasedData()
    {
        var obj = new System.Dynamic.ExpandoObject() { };
        obj.TryAdd("reducedPension", new { totalPension = 1001 });
        obj.TryAdd("SystemDate", "2025-12-12");

        _cmsParserMock.Setup(x => x.GetDataSummaryBlockSourceUris(It.IsAny<JsonElement>())).Returns(new List<Uri> { new Uri("http://www.google.com") });
        _mdpClientMock.Setup(x => x.GetData(It.IsAny<List<Uri>>(), It.IsAny<(string, string, string)>())).ReturnsAsync(obj);
        var summaryBlock = JsonSerializer.Deserialize<JsonElement>(TestData.SummaryBlocks, SerialiationBuilder.Options());

        var summaryBlocks = await _sut.GetDataSummaryBlocks(summaryBlock, obj, ("Bearer 123456", "dev2", "LIF"));

        summaryBlocks[1].SummaryItems[1].Header.Should().Be("Pension income");
        summaryBlocks[1].SummaryItems[1].Value.Should().Be("1001");
        summaryBlocks[1].BottomInformation.Values[0].Content
            .Should().Be("<p>If given, permission to deal with the person above regarding this application was provided on 12 Dec 2025</p>");

        summaryBlocks[1].SummaryItems[2].Header.Should().Be("Tax-free lump sum");
        summaryBlocks[1].SummaryItems[2].Value.Should().Be("reducedPension.totalLumpSum");
    }

    public void CanCreateContentBlocks()
    {
        var member = new MemberBuilder().Build();
        var contentKeys = new List<string> { "gmp_explanation", "random_name" };
        var contentBlocksData = JsonSerializer.Deserialize<JsonElement>(TestData.ContentBlocks, SerialiationBuilder.Options());

        var obj = new System.Dynamic.ExpandoObject() { };
        obj.TryAdd("reducedPension", new { totalPension = 1001 });
        obj.TryAdd("SystemDate", "2025-12-12");
        obj.TryAdd("post88GMPAtGMPAge", "2000.1");

        var contentBlocks = _sut.GetContentBlockItems(new List<JsonElement> { contentBlocksData }, obj, ("Bearer 123456", "dev2", "LIF")).ToList();

        contentBlocks[0].Key.Should().Be("gmp_explanation");
        contentBlocks[0].Header.Should().Be("Guaranteed Minimum Pension (GMP)");
        contentBlocks[0].Value.Should().Contain("2000.10");//to ensure two decimal places are always added.
    }
}
