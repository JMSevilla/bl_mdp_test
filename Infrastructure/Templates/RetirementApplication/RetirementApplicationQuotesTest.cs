using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using WTW.MdpService.Domain.Mdp.Calculations;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.MdpService.Infrastructure.Content;
using WTW.MdpService.Infrastructure.MdpApi;
using WTW.MdpService.Infrastructure.Templates.RetirementApplication.Submit;
using WTW.MdpService.Journeys.Submit.Services.DocumentsRenderer;
using WTW.MdpService.Retirement;
using WTW.MdpService.Test.Domain.Mdp.Calculations;
using WTW.MdpService.Test.Domain.Mdp.Journeys;
using WTW.Web.Serialization;

namespace WTW.MdpService.Test.Infrastructure.Templates.RetirementApplication
{
    public class RetirementApplicationQuotesTest
    {
        private readonly Mock<ICalculationsParser> _calculationsParserMock;
        private readonly Mock<IRetirementService> _retirementServiceMock;
        private readonly Mock<IContentClient> _contentClientMock;
        private readonly Mock<IMdpClient> _mdpClientMock;
        private readonly Mock<ICmsDataParser> _cmsParserMock;
        private readonly RetirementApplicationQuotesV2 _sut;

        public RetirementApplicationQuotesTest()
        {
            _calculationsParserMock = new Mock<ICalculationsParser>();
            _retirementServiceMock = new Mock<IRetirementService>();
            _contentClientMock = new Mock<IContentClient>();
            _mdpClientMock = new Mock<IMdpClient>();
            _cmsParserMock = new Mock<ICmsDataParser>();
            _sut = new RetirementApplicationQuotesV2(
                _calculationsParserMock.Object,
                _retirementServiceMock.Object,
                _contentClientMock.Object,
                _mdpClientMock.Object,
                _cmsParserMock.Object);
        }

        public async Task CanCreateSummaryAndContentBlocksFromRetirementApplication()
        {
            var retirementJourney = new RetirementJourneyBuilder().SelectedQuoteName("reducedPension").Build();
            var retirementCalculation = new CalculationBuilder().BuildV2();
            retirementJourney.SetCalculation(retirementCalculation);

            var contentAccessKey = "{}";
            var summaryBlock = JsonSerializer.Deserialize<JsonElement>(TestData.SummaryBlocks, SerialiationBuilder.Options());
            var contentBlocks = JsonSerializer.Deserialize<JsonElement>(TestData.ContentBlocks, SerialiationBuilder.Options());
            var retirementV2 = new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options()));
            var selectedQuoteDetails = new Dictionary<string, object>
            {
                { "reducedPension_totalPension", "24801.96" }
            };
            var contentKeys = new List<string> { "random_name", "gmp_explanation" };

            _contentClientMock
                .Setup(x => x.FindSummaryBlocks(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(summaryBlock);

            _contentClientMock
                .Setup(x => x.FindContentBlocks(contentKeys, It.IsAny<string>()))
                .ReturnsAsync(new List<JsonElement> { contentBlocks });

            _calculationsParserMock
                .Setup(x => x.GetRetirementV2(TestData.RetirementJsonV2))
                .Returns(retirementV2);

            _retirementServiceMock
                .Setup(x => x.GetSelectedQuoteDetails("reducedPension", retirementV2))
                .Returns(selectedQuoteDetails);

            var result = await _sut.Create(retirementJourney, contentKeys, contentAccessKey, new CmsTokenInformationResponse());

            result.SelectedOptionData.Count.Should().Be(1);
            result.SummaryBlocks.Count.Should().Be(2);

            result.SelectedOptionData["reducedPension_totalPension"].Should().Be("24801.96");
            result.SummaryBlocks[1].SummaryItems[0].Header.Should().Be("Your choice");
            result.SummaryBlocks[1].SummaryItems[0].Value.Should().Be("A reduced pension");
            result.SummaryBlocks[1].BottomInformation.Values.Should().HaveCount(1);


            result.SummaryBlocks[1].SummaryItems[1].Header.Should().Be("Pension income");
            result.SummaryBlocks[1].SummaryItems[1].Value.Should().Be("24801.96");

            result.ContentBlockItems[0].Key.Should().Be("gmp_explanation");
            result.ContentBlockItems[0].Header.Should().Be("Guaranteed Minimum Pension (GMP)");
        }
    }
}