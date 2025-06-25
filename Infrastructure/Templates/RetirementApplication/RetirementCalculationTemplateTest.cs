using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WTW.MdpService.Domain.Mdp.Calculations;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.MdpService.Infrastructure.Content;
using WTW.MdpService.Infrastructure.EpaService;
using WTW.MdpService.Infrastructure.Templates.Common.CmsContentModels;
using WTW.MdpService.Infrastructure.Templates.RetirementApplication.Calculations;
using WTW.MdpService.Retirement;
using WTW.MdpService.Test.Domain.Mdp.Calculations;
using WTW.MdpService.Test.Domain.Members;
using WTW.Web.Serialization;

namespace WTW.MdpService.Test.Infrastructure.Templates.RetirementApplication
{
    public class RetirementCalculationTemplateTest
    {
        private readonly Mock<IRetirementCalculationQuotesV2> _retirementCalculationQuotesV2Mock;
        private readonly Mock<ILogger<ContentClient>> _loggerMock;
        private readonly RetirementCalculationTemplate _sut;
        protected Mock<IOptionsSnapshot<CalculationServiceOptions>> _optionsMock;
        protected readonly Mock<IEpaServiceClient> _serviceClientMock;
        public RetirementCalculationTemplateTest()
        {
            _retirementCalculationQuotesV2Mock = new Mock<IRetirementCalculationQuotesV2>();

            _loggerMock = new Mock<ILogger<ContentClient>>();
            _sut = new RetirementCalculationTemplate(
                _retirementCalculationQuotesV2Mock.Object);
            _optionsMock = new Mock<IOptionsSnapshot<CalculationServiceOptions>>();
            _serviceClientMock = new Mock<IEpaServiceClient>();
        }

        public async Task AppliesArgsFromCalculation()
        {
            var template = "{{member_quote_reference_number}}";
            var now = DateTimeOffset.UtcNow;
            var member = new MemberBuilder().Build();

            var response = JsonSerializer.Deserialize<RetirementResponseV2>(TestData.RetirementV2ResponseJson, SerialiationBuilder.Options());
            var (retirementV2, mdp) = new CalculationsParser(_optionsMock.Object, _serviceClientMock.Object).GetRetirementJsonV2(response, "PV");

            var calculation = new Calculation("0003994", "RBS", TestData.RetiremntDatesAgesJson, retirementV2, mdp, now.AddDays(60).Date, now, true);

            var sut = await _sut.Render(template, "label", JsonSerializer.Deserialize<JsonElement>("{}"), new CmsTokenInformationResponse(), calculation, member, JsonSerializer.Deserialize<IEnumerable<JsonElement>>("[{}]"), It.IsAny<(string, string, string)>());

            sut.Should().Be("0003994");
        }

        public async Task Render_AppliesArgsFromCalculation()
        {
            var template = "{{member_quote_pension_option_number}}";
            var calculation = new CalculationBuilder().BuildV2();

            _retirementCalculationQuotesV2Mock
                .Setup(x => x.FilterOptionsByKey(It.IsAny<JsonElement>(), It.IsAny<Calculation>(), It.IsAny<string>()))
                .Returns(new OptionBlock() { OrderNo = 1, Description = "des", Header = "head", SummaryItems = new List<SummaryItem>() });

            var mdpResponseV2 = JsonSerializer.Deserialize<MdpResponseV2>(TestData.RetirementQuotesJsonV2, SerialiationBuilder.Options());

            var sut = await _sut.Render(template, JsonSerializer.Deserialize<JsonElement>("{}"), mdpResponseV2.Options, new CmsTokenInformationResponse(), calculation, JsonSerializer.Deserialize<IEnumerable<JsonElement>>("[{}]"), It.IsAny<(string, string, string)>());

            sut.Should().Be("0");
        }

        public async Task Render_AppliesArgsFromCalculation_WhenFilterOptionsByKeyReturnsNone()
        {
            var template = "{{member_quote_pension_option_number}}";
            var calculation = new CalculationBuilder().BuildV2();

            _retirementCalculationQuotesV2Mock
                .Setup(x => x.FilterOptionsByKey(It.IsAny<JsonElement>(), It.IsAny<Calculation>(), It.IsAny<string>()))
                .Returns(Option<OptionBlock>.None);

            var mdpResponseV2 = JsonSerializer.Deserialize<MdpResponseV2>(TestData.RetirementQuotesJsonV2, SerialiationBuilder.Options());

            var sut = await _sut.Render(template, JsonSerializer.Deserialize<JsonElement>("{}"), mdpResponseV2.Options, new CmsTokenInformationResponse(), calculation, JsonSerializer.Deserialize<IEnumerable<JsonElement>>("[{}]"), It.IsAny<(string, string, string)>());

            sut.Should().Be("0");
        }
    }
}
