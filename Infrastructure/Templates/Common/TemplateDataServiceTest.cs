using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Domain.Mdp;
using WTW.MdpService.Domain.Mdp.Calculations;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.MdpService.Infrastructure.Content;
using WTW.MdpService.Infrastructure.MdpApi;
using WTW.MdpService.Infrastructure.MdpDb;
using WTW.MdpService.Infrastructure.Templates.Common;
using WTW.MdpService.Infrastructure.Templates.Common.CmsContentModels;
using WTW.MdpService.Infrastructure.Templates.GenericJourneys;
using WTW.MdpService.Infrastructure.Templates.RetirementApplication.Calculations;
using WTW.MdpService.Journeys.Submit.Services.DocumentsRenderer;
using WTW.MdpService.Test.Domain.Mdp.Calculations;
using WTW.MdpService.Test.Domain.Members;
using WTW.Web.Serialization;

namespace WTW.MdpService.Test.Infrastructure.Templates.Common;

public class TemplateDataServiceTest
{
    private readonly Mock<IGenericTemplateContent> _genericTemplateContentMock;
    private readonly Mock<ICmsDataParser> _cmsDataParserMock;
    private readonly Mock<IContentClient> _contentClientMock;
    private readonly Mock<IMdpClient> _mdpClientMock;
    private readonly Mock<ICalculationsParser> _calculationsParserMock;
    private readonly Mock<IRetirementCalculationQuotesV2> _retirementCalculationQuotesV2Mock;
    private readonly Mock<IQuoteSelectionJourneyRepository> _quoteSelectionJourneyRepositoryMock;
    private readonly Mock<ILogger<TemplateDataService>> _loggerMock;
    private readonly TemplateDataService _sut;

    public TemplateDataServiceTest()
    {
        _genericTemplateContentMock = new Mock<IGenericTemplateContent>();
        _cmsDataParserMock = new Mock<ICmsDataParser>();
        _contentClientMock = new Mock<IContentClient>();
        _mdpClientMock = new Mock<IMdpClient>();
        _calculationsParserMock = new Mock<ICalculationsParser>();
        _retirementCalculationQuotesV2Mock = new Mock<IRetirementCalculationQuotesV2>();
        _quoteSelectionJourneyRepositoryMock = new Mock<IQuoteSelectionJourneyRepository>();
        _loggerMock = new Mock<ILogger<TemplateDataService>>();
        _sut = new TemplateDataService(
            _genericTemplateContentMock.Object,
            _cmsDataParserMock.Object,
            _contentClientMock.Object,
            _mdpClientMock.Object,
            _calculationsParserMock.Object,
            _retirementCalculationQuotesV2Mock.Object,
            _quoteSelectionJourneyRepositoryMock.Object,
            _loggerMock.Object);
    }

    public void GetCmsTokensResponseModel_WhenCalculationDoesNotExist()
    {
        var result = _sut.GetCmsTokensResponseData(new MemberBuilder().Build(), Option<Calculation>.None);

        result.TotalPension.Should().BeNull();
    }

    public void GetCmsTokensResponseModel_WhenCalculationExists()
    {
        _calculationsParserMock
            .Setup(x => x.GetRetirementV2(It.IsAny<string>()))
            .Returns(new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options())));

        var result = _sut.GetCmsTokensResponseData(new MemberBuilder().Build(), new CalculationBuilder().BuildV2());

        result.TotalPension.Should().Be(32369.4M);
    }

    public async Task ReturnsContentBlockItems()
    {
        var contentBlocksContent = new List<JsonElement>();
        _genericTemplateContentMock
            .Setup(x => x.GetContentBlockItems(contentBlocksContent, It.IsAny<ExpandoObject>(), It.IsAny<(string, string, string)>()))
            .Returns(new List<ContentBlockItem> { new ContentBlockItem("", "", ""), new ContentBlockItem("", "", "") });

        var result = await _sut.GetGenericContentBlockItems(It.IsAny<TemplateResponse>(), It.IsAny<string>(), It.IsAny<(string, string, string)>());

        result.Should().HaveCount(2);
    }

    public async Task ReturnsGenericDataSummaryBlocks()
    {
        var dataSummaryContent = new JsonElement();
        _contentClientMock
            .Setup(x => x.FindDataSummaryBlocks(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(dataSummaryContent);

        _genericTemplateContentMock
            .Setup(x => x.GetDataSummaryBlocks(dataSummaryContent, It.IsAny<ExpandoObject>(), It.IsAny<(string, string, string)>()))
            .ReturnsAsync(new List<SummaryBlock> { new SummaryBlock("", new BottomInformationItem()), new SummaryBlock("", new BottomInformationItem()) });

        var result = await _sut.GetGenericDataSummaryBlocks(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<(string, string, string)>());

        result.Should().HaveCount(2);
    }

    public async Task ReturnsEmptyGenericDataSummaryBlocksListWhenNotFoundInCms()
    {
        _contentClientMock
            .Setup(x => x.FindDataSummaryBlocks(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<JsonElement>.None);

        var result = await _sut.GetGenericDataSummaryBlocks(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<(string, string, string)>());

        result.Should().HaveCount(0);
    }

    public async Task ReturnsOptionListItems()
    {
        var optionsContent = new JsonElement();
        _contentClientMock.Setup(x => x.FindRetirementOptions(It.IsAny<string>())).ReturnsAsync(optionsContent);
        _calculationsParserMock
              .Setup(x => x.GetQuotesV2(It.IsAny<string>()))
              .Returns(JsonSerializer.Deserialize<MdpResponseV2>(TestData.RetirementQuotesJsonV2, SerialiationBuilder.Options()));
        _retirementCalculationQuotesV2Mock
            .Setup(x => x.FilterOptionsByKey(optionsContent, It.IsAny<Calculation>(), It.IsAny<string>()))
            .Returns(new OptionBlock() { OrderNo = 1, Description = "des", Header = "head", SummaryItems = new List<SummaryItem>() });

        var result = await _sut.GetOptionListItems(new CalculationBuilder().BuildV2(), It.IsAny<string>());

        result.Should().HaveCount(4);
        result.ToArray()[0].OptionNumber.Should().Be(1);
        result.ToArray()[0].Description.Should().Be("des");
        result.ToArray()[0].Header.Should().Be("head");
        result.ToArray()[0].SummaryItems.Should().HaveCount(0);
    }

    public async Task ReturnsOptionListItems_WhenFilterOptionsByKeyReturnsNone()
    {
        var optionsContent = new JsonElement();
        _contentClientMock.Setup(x => x.FindRetirementOptions(It.IsAny<string>())).ReturnsAsync(optionsContent);
        _calculationsParserMock
              .Setup(x => x.GetQuotesV2(It.IsAny<string>()))
              .Returns(JsonSerializer.Deserialize<MdpResponseV2>(TestData.RetirementQuotesJsonV2, SerialiationBuilder.Options()));
        _retirementCalculationQuotesV2Mock
            .Setup(x => x.FilterOptionsByKey(optionsContent, It.IsAny<Calculation>(), It.IsAny<string>()))
            .Returns(Option<OptionBlock>.None);

        var result = await _sut.GetOptionListItems(new CalculationBuilder().BuildV2(), It.IsAny<string>());

        result.Should().HaveCount(0);
    }

    public async Task ReturnsZeroOptionSummaryDataSummaryBlocks_WhenQuoteSelectionJourneyDoesNotExist()
    {
        var result = await _sut.GetOptionSummaryDataSummaryBlocks(new CalculationBuilder().BuildV2(), It.IsAny<string>());

        result.Should().BeEmpty();
    }

    public async Task ReturnsOptionSummaryDataSummaryBlocks_WhenQuoteSelectionJourneyExist()
    {
        var summaryBlocksContent = new JsonElement();
        _retirementCalculationQuotesV2Mock
            .Setup(x => x.Create(It.IsAny<Calculation>(), It.IsAny<string>(), summaryBlocksContent))
            .ReturnsAsync((null, new List<SummaryBlock> { new SummaryBlock("", new BottomInformationItem()), new SummaryBlock("", new BottomInformationItem()) }));
        _contentClientMock
            .Setup(x => x.FindDataSummaryBlocks(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(summaryBlocksContent);
        _quoteSelectionJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new QuoteSelectionJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), "a", "b", "selection"));

        var result = await _sut.GetOptionSummaryDataSummaryBlocks(new CalculationBuilder().BuildV2(), It.IsAny<string>());

        result.Should().HaveCount(2);
    }
}