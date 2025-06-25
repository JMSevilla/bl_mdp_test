using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using Moq;
using WTW.MdpService.Domain.Mdp.Calculations;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.MdpService.Infrastructure.MdpApi;
using WTW.MdpService.Infrastructure.Templates.RetirementApplication.Calculations;
using WTW.MdpService.Journeys.Submit.Services.DocumentsRenderer;
using WTW.MdpService.Test.Domain.Mdp.Calculations;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Infrastructure.Templates.RetirementApplication;

public class RetirementCalculationQuotesV2Test
{
    private readonly Mock<ICalculationsParser> _mockCalculationsParser;
    private readonly Mock<IRetirementService> _mockRetirementService;
    private readonly Mock<IMdpClient> _mockMdpClient;
    private readonly Mock<ICmsDataParser> _mockCmsDataParser;

    private readonly RetirementCalculationQuotesV2 _retirementCalculationQuotesV2;

    public RetirementCalculationQuotesV2Test()
    {
        _mockCalculationsParser = new Mock<ICalculationsParser>();
        _mockRetirementService = new Mock<IRetirementService>();
        _mockMdpClient = new Mock<IMdpClient>();
        _mockCmsDataParser = new Mock<ICmsDataParser>();

        _retirementCalculationQuotesV2 = new RetirementCalculationQuotesV2(
            _mockCalculationsParser.Object,
            _mockRetirementService.Object,
            _mockMdpClient.Object,
            _mockCmsDataParser.Object);
    }

    public void FilterOptionsByKey_ShouldReturnOptionBlock()
    {
        var options = JsonDocument.Parse("[{ \"elements\": { \"summaryItems\": { \"value\": \"TestKey\" },\"key\": { \"value\": \"TestKey\" }, \"header\": { \"value\": \"TestHeader\" }, \"description\": { \"value\": \"TestDescription\" }, \"orderNo\": { \"value\": 1 } } }]").RootElement;
        var calculation = new CalculationBuilder().BuildV2();
        var key = "TestKey";

        var retirementV2 = new RetirementV2(new RetirementV2Params());
        var optionsDictionary = new Dictionary<string, object>();

        _mockCalculationsParser.Setup(x => x.GetRetirementV2(It.IsAny<string>())).Returns(retirementV2);
        _mockRetirementService.Setup(x => x.GetSelectedQuoteDetails(It.IsAny<string>(), It.IsAny<RetirementV2>())).Returns(optionsDictionary);

        var result = _retirementCalculationQuotesV2.FilterOptionsByKey(options, calculation, key);

        result.IsSome.Should().BeTrue();
        result.Value().Header.Should().Be("TestHeader");
        result.Value().Description.Should().Be("TestDescription");
        result.Value().OrderNo.Should().Be(1);
    }
}