using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Amazon.Runtime.Internal.Transform;
using FluentAssertions;
using Moq;
using WTW.MdpService.Infrastructure.MdpApi;
using WTW.MdpService.Infrastructure.Templates.Common;
using WTW.MdpService.Infrastructure.Templates.Common.CmsContentModels;
using WTW.MdpService.Journeys.Submit.Services.DocumentsRenderer;
using WTW.MdpService.Test.Infrastructure.Content;
using WTW.Web.Serialization;

namespace WTW.MdpService.Test.Infrastructure.Templates.Common;

public class CmsTemplateContentJsonParserTest
{
    private readonly Mock<IMdpClient> _mdpClientMock;
    private readonly Mock<ICmsDataParser> _cmsParserMock;
    private readonly CmsTemplateContentJsonParser _sut;

    public CmsTemplateContentJsonParserTest()
    {
        _mdpClientMock = new Mock<IMdpClient>();
        _cmsParserMock = new Mock<ICmsDataParser>();
        _sut = new CmsTemplateContentJsonParser(_mdpClientMock.Object, _cmsParserMock.Object);
    }

    public void GetContentBlock_ReturnsContentBlockItem()
    {
        var content = JsonSerializer.Deserialize<JsonElement>(ContentTestData.ContentBlockList1, SerialiationBuilder.Options());

        var result = (_sut.GetContentBlock(new List<JsonElement> { content })).ToList();

        result[0].Header.Should().Be("Guaranteed Minimum Pension (GMP)");
        result[0].Key.Should().Be("gmp_explanation");
        result[0].Value.Should().Be("While you were an active member of the Fund");
    }

    public void GetContentBlock_ReturnsContentBlockItems()
    {
        var content1 = JsonSerializer.Deserialize<JsonElement>(ContentTestData.ContentBlockList1, SerialiationBuilder.Options());
        var content2 = JsonSerializer.Deserialize<JsonElement>(ContentTestData.ContentBlockList2, SerialiationBuilder.Options());

        var result = (_sut.GetContentBlock(new List<JsonElement> { content1, content2 })).ToList();

        result[0].Header.Should().Be("Guaranteed Minimum Pension (GMP)");
        result[0].Key.Should().Be("gmp_explanation");
        result[0].Value.Should().Be("While you were an active member of the Fund");
        result[1].Header.Should().Be("Death after retirement – what happens?");
        result[1].Key.Should().Be("view_death_benefits");
        result[1].Value.Should().Be("Pension: this is the pension income your spouse");
    }

    public void GetSummaryItems_ReturnsSummaryItemsWithQuotesValueReplaced()
    {
        var summary = JsonSerializer.Deserialize<JsonElement>(ContentTestData.SummaryItemsList, SerialiationBuilder.Options());
        var props = new Dictionary<string, object>
        {
            { "fullPensionDCAsLumpSum_totalPension", "12345" },
            { "fullPensionDCAsLumpSum_pensionTranches_pre88GMP", "67890" },
            { "quoteExpiryDate", "20 Jul 2025"}
        };

        var result = new CmsTemplateContentJsonParser(_mdpClientMock.Object, _cmsParserMock.Object).GetSummaryItems(summary, props);

        result[0].Header.Should().Be("Pension income");
        result[0].Format.Should().Be("Currency per year");
        result[0].Divider.Should().Be("False");
        result[0].Description.Should().Be("When you reach your State Pension Age");
        result[0].Value.Should().Be("12345");
        result[0].ExplanationSummaryItems[0].Header.Should().Be("Guaranteed Minimum Pension accrued before 6 April 1988");
        result[0].ExplanationSummaryItems[0].Value.Should().Be("67890");
        result[1].Description.Should().Be("[[badge:PROTECTED UNTIL 20 Jul 2025]]");
    }

    public void GetSummaryItems_ReturnsSummaryItemsWithValuesReplaced()
    {
        var summary = JsonSerializer.Deserialize<JsonElement>(ContentTestData.SummaryItemsList, SerialiationBuilder.Options());
        var props = new Dictionary<string, object>
        {
            { "fullPensionDCAsLumpSum_totalPension", "12345" }
        };

        List<SummaryItem> result = new CmsTemplateContentJsonParser(_mdpClientMock.Object, _cmsParserMock.Object).GetSummaryItems(summary, props);

        result[0].Header.Should().Be("Pension income");
        result[0].Format.Should().Be("Currency per year");
        result[0].Divider.Should().Be("False");
        result[0].Description.Should().Be("When you reach your State Pension Age");
        result[0].Value.Should().Be("12345");
        result[0].ExplanationSummaryItems[0].Header.Should().Be("Guaranteed Minimum Pension accrued before 6 April 1988");
        result[0].ExplanationSummaryItems[0].Value.Should().BeNullOrEmpty();
    }
}
