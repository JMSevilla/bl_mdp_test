using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Domain.Mdp;
using WTW.MdpService.Infrastructure.Content;
using WTW.MdpService.Infrastructure.Templates.Common.CmsContentModels;
using WTW.MdpService.Infrastructure.Templates.RetirementApplication.Submit;
using WTW.MdpService.Retirement;
using WTW.MdpService.Test.Domain.Members;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Infrastructure.Templates.RetirementApplication
{
    public class RetirementSubmissionTemplateTest
    {
        private readonly Mock<IRetirementApplicationQuotesV2> _retirementApplicationQuotesV2Mock;
        private readonly Mock<ILogger<ContentClient>> _loggerMock;
        private readonly RetirementApplicationSubmissionTemplate _sut;

        public RetirementSubmissionTemplateTest()
        {
            _retirementApplicationQuotesV2Mock = new Mock<IRetirementApplicationQuotesV2>();

            _loggerMock = new Mock<ILogger<ContentClient>>();
            _sut = new RetirementApplicationSubmissionTemplate(
                _retirementApplicationQuotesV2Mock.Object);
        }

        public async Task AppliesArgsFromRetirementJourney()
        {
            var template = "{{member_quote_reference_number}}";
            var now = DateTimeOffset.UtcNow;
            var member = new MemberBuilder().Build();
            var client = new ContentClient(new HttpClient(), _loggerMock.Object);

            var quote = MemberQuote.Create(now,
            "FullPension", 1010.99m, 10, 11, 12, 13, 14, 15, 16, 17,
            18, 19, 20, 21, 22, false, 85, 55,
            65, now, now, now, "15", "16", 17, "PV", "SPD", 1, 9);

            var journey = new RetirementJourney("RBS", "0304442", now, "retirement_start", "next_page_test_1", quote.Right(), 2, 37);

            var response = new RetirementApplicationSubmitionTemplateData
            {
                SelectedOptionData = new Dictionary<string, object>
                {
                    { "test", "test" }
                },
                SummaryBlocks = new List<SummaryBlock>
                {
                    new SummaryBlock("test", new BottomInformationItem())
                },
                ContentBlockItems = new List<ContentBlockItem>
                {
                    new ContentBlockItem("key", "header", "value")
                }
            };

            _retirementApplicationQuotesV2Mock
                .Setup(x => x.Create(It.IsAny<RetirementJourney>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<CmsTokenInformationResponse>()))
                .ReturnsAsync(response);

            string sut = await _sut.Render(template, "{}", new CmsTokenInformationResponse(), journey, member, "gmp_explanation;test_key;");

            sut.Should().Be("0304442");
        }
    }
}
