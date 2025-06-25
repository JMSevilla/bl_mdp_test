using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using WTW.MdpService.Domain.Mdp.Calculations;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.MdpService.Infrastructure.MdpApi;
using WTW.MdpService.Infrastructure.Templates.RetirementApplication.Calculations;
using WTW.MdpService.Journeys.Submit.Services.DocumentsRenderer;
using WTW.MdpService.Test.Domain.Mdp.Calculations;
using WTW.MdpService.Test.Domain.Mdp.Journeys;
using WTW.Web.Serialization;

namespace WTW.MdpService.Test.Infrastructure.Templates.RetirementApplication;

public class RetirementCalculationQuotesTest
{
    private readonly Mock<ICalculationsParser> _calculationsParserMock;
    private readonly Mock<IRetirementService> _retirementServiceMock;
    private readonly Mock<IMdpClient> _mdpClientMock;
    private readonly Mock<ICmsDataParser> _cmsParserMock;
    private readonly RetirementCalculationQuotesV2 _sut;

    public RetirementCalculationQuotesTest()
    {
        _calculationsParserMock = new Mock<ICalculationsParser>();
        _retirementServiceMock = new Mock<IRetirementService>();
        _mdpClientMock = new Mock<IMdpClient>();
        _cmsParserMock = new Mock<ICmsDataParser>();
        _sut = new RetirementCalculationQuotesV2(
            _calculationsParserMock.Object,
            _retirementServiceMock.Object,
            _mdpClientMock.Object,
            _cmsParserMock.Object);
    }

    public async Task CanCreateSummaryBlocksFromRetirementCalculation()
    {
        var retirementJourney = new RetirementJourneyBuilder().SelectedQuoteName("reducedPension").Build();
        var retirementCalculation = new CalculationBuilder().BuildV2();
        retirementJourney.SetCalculation(retirementCalculation);

        var summaryBlock = JsonSerializer.Deserialize<JsonElement>(TestData.SummaryBlocks, SerialiationBuilder.Options());
        var retirementV2 = new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options()));
        var selectedQuoteDetails = new Dictionary<string, object>
            {
                { "reducedPension_totalPension", "24801.96" }
            };

        _calculationsParserMock
            .Setup(x => x.GetRetirementV2(TestData.RetirementJsonV2))
            .Returns(retirementV2);

        _retirementServiceMock
            .Setup(x => x.GetSelectedQuoteDetails("reducedPension", retirementV2))
            .Returns(selectedQuoteDetails);

        var obj = new System.Dynamic.ExpandoObject() { };
        obj.TryAdd("SystemDate", "2024-12-12");

        _cmsParserMock
            .Setup(x => x.GetDataSummaryBlockSourceUris(It.IsAny<JsonElement>()))
            .Returns(new List<Uri> { new Uri("http://www.google.com") });

        _mdpClientMock
            .Setup(x => x.GetData(It.IsAny<List<Uri>>(), It.IsAny<(string, string, string)>()))
            .ReturnsAsync(obj);


        var (optionDetails, summaryBlocks) = await _sut.Create(retirementCalculation, "reducedPension", summaryBlock, ("token", "test", "RBS"));


        summaryBlocks[1].Header.Should().Be("Total retirement benefits");
        summaryBlocks[1].SummaryItems[0].Header.Should().Be("Your choice");
        summaryBlocks[1].SummaryItems[0].Value.Should().Be("A reduced pension");
        summaryBlocks[1].SummaryItems[1].Header.Should().Be("Pension income");
        summaryBlocks[1].SummaryItems[1].Value.Should().Be("24801.96");
        summaryBlocks[1].BottomInformation.Values[0].Content.Should().Be("<p>If given, permission to deal with the person above regarding this application was provided on [[data-date:systemDate]]</p>");
    }
}
