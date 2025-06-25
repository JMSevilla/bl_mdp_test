using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Content.V2;
using WTW.MdpService.Domain.Mdp.Calculations;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.MdpService.Infrastructure.EngagementEvents;
using WTW.MdpService.Infrastructure.MdpDb;
using WTW.MdpService.Infrastructure.MemberDb;
using WTW.MdpService.Infrastructure.MemberWebInteractionService;
using WTW.MdpService.Members;
using WTW.MdpService.RetirementJourneys;
using WTW.MdpService.Test.Domain.Mdp.Calculations;
using WTW.MdpService.Test.Domain.Members;
using WTW.Web.Caching;
using WTW.Web.Serialization;

namespace WTW.MdpService.Test.Members;

public class MemberControllerTest
{
    private readonly Mock<ICalculationsClient> _calculationsClientMock;
    private readonly Mock<IMemberRepository> _memberRepositoryMock;
    private readonly Mock<ICalculationsRepository> _calculationsRepositoryMock;
    private readonly RetirementJourneyConfiguration _retirementJourneyConfiguration;
    private readonly Mock<IIfaReferralHistoryRepository> _ifaReferralHistoryRepositoryMock;
    private readonly Mock<ICalculationsParser> _calculationsParserMock;
    private readonly Mock<ICache> _cacheMock;
    private readonly Mock<ILogger<MemberController>> _loggerMock;
    private readonly Mock<IAccessKeyService> _accessKeyServiceMock;
    private readonly Mock<IMemberWebInteractionServiceClient> _memberWebInteractionServiceClient;
    private readonly MemberController _sut;

    public MemberControllerTest()
    {
        _calculationsClientMock = new Mock<ICalculationsClient>();
        _memberRepositoryMock = new Mock<IMemberRepository>();
        _calculationsRepositoryMock = new Mock<ICalculationsRepository>();
        _retirementJourneyConfiguration = new RetirementJourneyConfiguration(3);
        _ifaReferralHistoryRepositoryMock = new Mock<IIfaReferralHistoryRepository>();
        _calculationsParserMock = new Mock<ICalculationsParser>();
        _cacheMock = new Mock<ICache>();
        _loggerMock = new Mock<ILogger<MemberController>>();
        _accessKeyServiceMock = new Mock<IAccessKeyService>();
        _memberWebInteractionServiceClient = new Mock<IMemberWebInteractionServiceClient>();
        _sut = new MemberController(_calculationsClientMock.Object,
            _memberRepositoryMock.Object,
            _calculationsRepositoryMock.Object,
            _retirementJourneyConfiguration,
            _ifaReferralHistoryRepositoryMock.Object,
            _calculationsParserMock.Object,
            _cacheMock.Object,
            _loggerMock.Object,
            _accessKeyServiceMock.Object,
            _memberWebInteractionServiceClient.Object);
        _sut.SetupControllerContext();
    }

    public async Task GetScheme_Returns404()
    {
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.None);

        var result = await _sut.GetScheme();

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task GetScheme_Returns200()
    {
        var member = new MemberBuilder()
            .Build();

        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(member);

        var result = await _sut.GetScheme();

        result.Should().BeOfType<OkObjectResult>();
        var response = (result as OkObjectResult).Value as SchemeResponse;
        response.Code.Should().Be("schemeCode");
        response.Type.Should().Be("DB");
        response.Name.Should().Be("name");
    }

    public async Task LinkedMembers_Returns404()
    {
        _memberRepositoryMock
            .Setup(x => x.FindLinkedMembers(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<List<LinkedMember>>.None);

        var result = await _sut.LinkedMembers();

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task LinkedMembers_Returns200()
    {
        var linkedMembers = new List<LinkedMember> { new LinkedMember("refNo", "bgroup", "linkedRefNo", "linkedBgroup") };
        _memberRepositoryMock
            .Setup(x => x.FindLinkedMembers(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(linkedMembers);

        var result = await _sut.LinkedMembers();

        result.Should().BeOfType<OkObjectResult>();
        var response = (result as OkObjectResult).Value as LinkedMemberResponse;
        response.LinkedMembers.Count.Should().Be(1);
    }

    public async Task GetMemberAnalyticsDetails_Returns404_WhenMemberNotFound()
    {
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.None);

        var accessKey = "{\"tenantUrl\":\"natwestdev.assure.wtwco.com\",\"isCalculationSuccessful\":true";

        var result = await _sut.GetMemberAnalyticsDetails(accessKey);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task GetMemberAnalyticsDetails_Returns200_WhenMemberFound_WithoutCalculations()
    {
        var member = new MemberBuilder()
            .BusinessGroup("TestBusinessGroup")
            .SchemeCode("TestSchemeCode")
            .Category("TestCategory")
            .LocationCode("TestLocation")
            .EmployerCode("TestEmployer")
            .SchemeType("DB")
            .NormalRetirementAge(65)
            .MinimumPensionAge(55)
            .Build();

        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(member);

        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<Calculation>.None);

        var accessKeyJson = "{\"tenantUrl\":\"natwestdev.assure.wtwco.com\",\"isCalculationSuccessful\":true,\"hasAdditionalContributions\":false,\"schemeType\":\"DB\",\"memberStatus\":\"Active\",\"lifeStage\":\"NotEligibleToRetire\",\"retirementApplicationStatus\":\"Undefined\",\"transferApplicationStatus\":\"Undefined\",\"wordingFlags\":[],\"currentAge\":\"35\",\"dbCalculationStatus\":\"success\",\"dcLifeStage\":\"undefined\",\"isWebChatEnabled\":false}";
        var accessKey = JsonSerializer.Deserialize<AccessKey>(accessKeyJson, SerialiationBuilder.Options());
        _accessKeyServiceMock
            .Setup(x => x.ParseJsonToAccessKey(accessKeyJson))
            .Returns(accessKey);
        _accessKeyServiceMock
            .Setup(x => x.GetDcJourneyStatus(It.IsAny<IEnumerable<string>>()))
            .Returns((string)null);

        var result = await _sut.GetMemberAnalyticsDetails(accessKeyJson);

        result.Should().BeOfType<OkObjectResult>();
        var response = (result as OkObjectResult).Value as MemberAnalyticsResponse;
        response.IsAvc.Should().BeFalse();
    }

    public async Task GetMemberAnalyticsDetails_Returns200_WhenMemberFound_WithCalculations()
    {
        var member = new MemberBuilder()
            .BusinessGroup("TestBusinessGroup")
            .SchemeCode("TestSchemeCode")
            .Category("TestCategory")
            .LocationCode("TestLocation")
            .EmployerCode("TestEmployer")
            .SchemeType("DB")
            .NormalRetirementAge(65)
            .MinimumPensionAge(55)
            .Build();

        var calculation = new CalculationBuilder()
            .BuildV2();

        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(member);

        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(calculation);

        _calculationsParserMock
            .Setup(x => x.GetRetirementV2(It.IsAny<string>()))
            .Returns(new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options())));

        var accessKeyJson = "{\"tenantUrl\":\"natwestdev.assure.wtwco.com\",\"isCalculationSuccessful\":true,\"hasAdditionalContributions\":false,\"schemeType\":\"DB\",\"memberStatus\":\"Active\",\"lifeStage\":\"NotEligibleToRetire\",\"retirementApplicationStatus\":\"Undefined\",\"transferApplicationStatus\":\"Undefined\",\"wordingFlags\":[],\"currentAge\":\"35\",\"dbCalculationStatus\":\"success\",\"dcLifeStage\":\"undefined\",\"isWebChatEnabled\":false}";
        var accessKey = JsonSerializer.Deserialize<AccessKey>(accessKeyJson, SerialiationBuilder.Options());
        _accessKeyServiceMock
            .Setup(x => x.ParseJsonToAccessKey(accessKeyJson))
            .Returns(accessKey);
        _accessKeyServiceMock
            .Setup(x => x.GetDcJourneyStatus(It.IsAny<IEnumerable<string>>()))
            .Returns((string)null);

        var result = await _sut.GetMemberAnalyticsDetails(accessKeyJson);

        result.Should().BeOfType<OkObjectResult>();
        var response = (result as OkObjectResult).Value as MemberAnalyticsResponse;
        response.IsAvc.Should().BeFalse();
    }

    public async Task GetMemberAnalyticsDetails_Returns200_WhenMemberFound_WithEmptyJson()
    {
        var member = new MemberBuilder()
            .BusinessGroup("TestBusinessGroup")
            .SchemeCode("TestSchemeCode")
            .Category("TestCategory")
            .LocationCode("TestLocation")
            .EmployerCode("TestEmployer")
            .SchemeType("DB")
            .NormalRetirementAge(65)
            .MinimumPensionAge(55)
            .Build();

        var calculation = new CalculationBuilder()
            .RetirementJsonV2(string.Empty)
            .BuildV2();

        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(member);

        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(calculation);

        var accessKeyJson = "{\"tenantUrl\":\"natwestdev.assure.wtwco.com\",\"isCalculationSuccessful\":true,\"hasAdditionalContributions\":false,\"schemeType\":\"DB\",\"memberStatus\":\"Active\",\"lifeStage\":\"NotEligibleToRetire\",\"retirementApplicationStatus\":\"Undefined\",\"transferApplicationStatus\":\"Undefined\",\"wordingFlags\":[],\"currentAge\":\"35\",\"dbCalculationStatus\":\"success\",\"dcLifeStage\":\"undefined\",\"isWebChatEnabled\":false}";
        var accessKey = JsonSerializer.Deserialize<AccessKey>(accessKeyJson, SerialiationBuilder.Options());
        _accessKeyServiceMock
            .Setup(x => x.ParseJsonToAccessKey(accessKeyJson))
            .Returns(accessKey);
        _accessKeyServiceMock
            .Setup(x => x.GetDcJourneyStatus(It.IsAny<IEnumerable<string>>()))
            .Returns((string)null);

        var result = await _sut.GetMemberAnalyticsDetails(accessKeyJson);

        result.Should().BeOfType<OkObjectResult>();
        var response = (result as OkObjectResult).Value as MemberAnalyticsResponse;
        response.IsAvc.Should().BeFalse();
    }

    public async Task GetAlerts_Returns200()
    {
        var member = new MemberBuilder()
            .BusinessGroup("TestBusinessGroup")
            .SchemeCode("TestSchemeCode")
            .Category("TestCategory")
            .LocationCode("TestLocation")
            .EmployerCode("TestEmployer")
            .SchemeType("DB")
            .NormalRetirementAge(65)
            .MinimumPensionAge(55)
            .Build();

        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(member);

        var memberMessages = new MemberMessagesResponse
        {
            Messages = new List<MemberMessage>
            {
                new MemberMessage
                {
                    MessageNo = 9,
                    MessageText = "[[type:Warning]]<p><strong>Important:</strong>This is an important 'Warning' type message about something you need to be informed of as a priority.</p>",
                    EffectiveDate = DateTime.Parse("2025-02-26"),
                    Title = "Warning"
                },
                new MemberMessage
                {
                    MessageNo = 10,
                    MessageText = "[[type:Info]]<p><strong>Info:</strong>This is an important 'Info' type message about something you need to be informed of as a priority.</p>",
                    EffectiveDate = DateTime.Parse("2025-02-26"),
                    Title = "Info"
                }
            }
        };

        _memberWebInteractionServiceClient
            .Setup(x => x.GetMessages(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<MemberMessagesResponse>.Some(memberMessages));


        var result = await _sut.GetAlerts();

        result.Should().BeOfType<OkObjectResult>();
        var response = (result as OkObjectResult).Value as MemberAlertsResponse;
        response.Alerts.Should().HaveCount(2);
        var alertsList = response.Alerts.ToList();
        alertsList[0].AlertID.Should().Be(9);
        alertsList[0].MessageText.Should().Be("[[type:Warning]]<p><strong>Important:</strong>This is an important 'Warning' type message about something you need to be informed of as a priority.</p>");
        alertsList[0].EffectiveDate.Should().Be(DateTime.Parse("2025-02-26"));
    }

    public async Task GetAlerts_Returns404_WhenMenberInfoNotFound()
    {
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.None);

        var result = await _sut.GetAlerts();

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task GetAlerts_Returns400_WhenMessagesApiReturnsError()
    {
        var member = new MemberBuilder()
             .BusinessGroup("TestBusinessGroup")
             .SchemeType("DB")
             .NormalRetirementAge(65)
             .MinimumPensionAge(55)
             .Build();

        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(member);

        _memberWebInteractionServiceClient
           .Setup(x => x.GetMessages(It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(Option<MemberMessagesResponse>.None);

        var result = await _sut.GetAlerts();

        result.Should().BeOfType<NoContentResult>();
    }
}