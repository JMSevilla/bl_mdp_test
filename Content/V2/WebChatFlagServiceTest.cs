using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Content.V2;
using WTW.MdpService.Domain.Common.Journeys;
using WTW.MdpService.Domain.Mdp.Calculations;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.MdpService.Infrastructure.CasesApi;
using WTW.MdpService.Infrastructure.MdpDb;
using WTW.MdpService.Infrastructure.MemberDb;
using WTW.MdpService.Infrastructure.MemberDb.IfaReferrals;
using WTW.MdpService.Test.Domain.Mdp.Calculations;
using WTW.MdpService.Test.Domain.Mdp.Journeys;
using WTW.MdpService.Test.Domain.Members;
using WTW.TestCommon.FixieConfig;
using WTW.Web.Serialization;

namespace WTW.MdpService.Test.Content.V2;

public class WebChatFlagServiceTest
{
    private readonly Mock<IWebChatFlagRepository> _webChatFlagRepoMock;
    private readonly WebChatFlagService _sut;

    private readonly string _stubBgroup = "XXX";
    private readonly string _stubStatusCode = "XX";
    private readonly string _stubSchemeCode = "XXXX";
    public WebChatFlagServiceTest()
    {
        _webChatFlagRepoMock = new Mock<IWebChatFlagRepository>();

        _sut = new WebChatFlagService(_webChatFlagRepoMock.Object);
    }

    public async Task WhenIsWebChatEnabledForBusinessGroupIsCalledWithValidData_ReturnsTrue()
    {
        _webChatFlagRepoMock.Setup(x => x.CheckBgroup(_stubBgroup)).ReturnsAsync("Y");

        var result = await _sut.IsWebChatEnabledForBusinessGroup(_stubBgroup);

        result.Should().BeTrue(); 
    }

    public async Task WhenIsWebChatEnabledForMemberCriteriaIsCalledWithValidData_ReturnsTrue()
    {
        _webChatFlagRepoMock.Setup(x => x.CheckMemberCriteria(_stubBgroup, _stubSchemeCode, _stubStatusCode)).ReturnsAsync("1");

        var result = await _sut.IsWebChatEnabledForMemberCriteria(_stubBgroup, _stubSchemeCode, _stubStatusCode);

        result.Should().BeTrue();
    }


    public async Task WhenIsWebChatEnabledForBusinessGroupIsCalledAndRepoReturnNull_ReturnsFalse()
    {
        string nullReturn = null;
        _webChatFlagRepoMock.Setup(x => x.CheckBgroup(_stubBgroup)).ReturnsAsync(nullReturn);

        var result = await _sut.IsWebChatEnabledForBusinessGroup(_stubBgroup);

        result.Should().BeFalse();
    }

    public async Task WhenIsWebChatEnabledForMemberCriteriaIsCalledAndRepoReturnNull_ReturnsTrue()
    {
        string nullReturn = null;
        _webChatFlagRepoMock.Setup(x => x.CheckBgroup(_stubBgroup)).ReturnsAsync(nullReturn);

        var result = await _sut.IsWebChatEnabledForMemberCriteria(_stubBgroup, _stubSchemeCode, _stubStatusCode);

        result.Should().BeFalse();
    }

}