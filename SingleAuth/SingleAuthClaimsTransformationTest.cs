using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WTW.MdpService.SingleAuth;
using WTW.MdpService.SingleAuth.Services;
using WTW.MdpService.Test.SingleAuth.Services;
using WTW.TestCommon;
using WTW.TestCommon.Helpers;
using WTW.Web;
using WTW.Web.Authentication;

namespace WTW.MdpService.Test.SingleAuth;

public class SingleAuthClaimsTransformationTest
{
    private readonly SingleAuthClaimsTransformation _sut;
    private readonly Mock<ILogger<SingleAuthClaimsTransformation>> _mockLogger;
    private readonly Mock<ISingleAuthService> _mockSingleAuthService;
    private readonly Mock<IOptions<SingleAuthAuthenticationOptions>> _mockOptions;
    private readonly ClaimsPrincipal _stubClaimsPrincipal;
    public SingleAuthClaimsTransformationTest()
    {
        _mockLogger = new Mock<ILogger<SingleAuthClaimsTransformation>>();
        _mockSingleAuthService = new Mock<ISingleAuthService>();
        _mockOptions = new Mock<IOptions<SingleAuthAuthenticationOptions>>();
        SetupSingleAuthClient(TestData.Bgroup);
        _stubClaimsPrincipal = new ClaimsPrincipal(new[] { new ClaimsIdentity(
            new[]
            {
                new Claim(MdpConstants.AuthSchemeClaim, TestData.Bgroup),
                new Claim(ClaimTypes.NameIdentifier, TestData.Sub.ToString())
            }) });
        _sut = new SingleAuthClaimsTransformation(_mockLogger.Object, _mockOptions.Object, _mockSingleAuthService.Object);
    }

    public async Task WhenTransformAsyncForValidActiveSingleAuthUser_ThenAddsRequiredClaims()
    {
        _mockSingleAuthService.Setup(x => x.GetCurrentTenant()).Returns(LanguageExtHelper.SetRight(TestData.Bgroup));
        _mockSingleAuthService.Setup(x => x.GetSingleAuthClaim(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>())).Returns(LanguageExtHelper.SetRight(TestData.Sub));
        _mockSingleAuthService.Setup(x => x.CheckMemberAccess(It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync(BgroupRefnoDataFactory.Create());

        var result = await _sut.TransformAsync(_stubClaimsPrincipal);

        AssertHelper.AssertClaim(result, MdpConstants.MemberClaimNames.BusinessGroup, TestData.Bgroup);
        AssertHelper.AssertClaim(result, MdpConstants.MemberClaimNames.ReferenceNumber, TestData.RefNo);
        AssertHelper.AssertClaim(result, ClaimTypes.Role, MdpConstants.MemberRole);
        AssertHelper.AssertClaim(result, MdpConstants.MemberClaimNames.MainBusinessGroup, TestData.MainBgroup);
        AssertHelper.AssertClaim(result, MdpConstants.MemberClaimNames.MainReferenceNumber, TestData.MainRefNo);
        AssertHelper.AssertClaim(result, ClaimTypes.NameIdentifier, TestData.Bgroup + TestData.RefNo);
        AssertHelper.AssertClaim(result, MdpConstants.AuthSchemeClaim, TestData.Bgroup);
        _mockLogger.VerifyLogging($"Transforming claims for scheme {TestData.Bgroup}", LogLevel.Information, Times.Once());
    }

    public async Task WhenClientNotConfiguredForSingleAuth_ThenReturnDefaultPrincipal()
    {
        SetupSingleAuthClient("WIF");

        var result = await _sut.TransformAsync(_stubClaimsPrincipal);

        AssertHelper.AssertNullClaim(result, ClaimTypes.Role);
    }

    public async Task WhenIgnoreClaimTransformation_ThenReturnClaimPrincipalWithRoleClaim()
    {
        _mockSingleAuthService.Setup(x => x.IgnoreClaimTransformationCheck())
            .Returns(true);

        var result = await _sut.TransformAsync(_stubClaimsPrincipal);

        AssertHelper.AssertClaim(result, ClaimTypes.Role, MdpConstants.MemberRole);
        AssertHelper.AssertClaim(result, MdpConstants.MemberClaimNames.Sub, TestData.Sub.ToString());
        AssertHelper.AssertNullClaim(result, MdpConstants.MemberClaimNames.BusinessGroup);
    }

    public async Task WhenNoTenantHeaderFound_ThenReturnDefaultClaimPrincipal()
    {
        _mockSingleAuthService.Setup(x => x.GetSingleAuthClaim(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()))
            .Returns(LanguageExtHelper.SetRight(TestData.Sub));
        _mockSingleAuthService.Setup(x => x.GetCurrentTenant())
           .Returns(LanguageExtHelper.SetLeft<string>(""));

        var result = await _sut.TransformAsync(_stubClaimsPrincipal);

        AssertHelper.AssertNullClaim(result, ClaimTypes.Role);
    }

    public async Task WhenNoSingleAuthUserFoundInMemberAccess_ThenReturnDefaultClaimPrincipal()
    {
        _mockSingleAuthService.Setup(x => x.GetCurrentTenant()).Returns(LanguageExtHelper.SetRight(TestData.Bgroup));
        _mockSingleAuthService.Setup(x => x.GetSingleAuthClaim(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>())).Returns(LanguageExtHelper.SetRight(TestData.Sub));
        _mockSingleAuthService.Setup(x => x.CheckMemberAccess(TestData.Sub, TestData.Bgroup))
           .ReturnsAsync(LanguageExtHelper.SetLeft<BgroupRefnoData>(""));

        var result = await _sut.TransformAsync(_stubClaimsPrincipal);

        AssertHelper.AssertNullClaim(result, ClaimTypes.Role);
    }

    public async Task WhenTransformAsyncCalledForAnonRequest_ThenSkipTransformation()
    {
        _mockSingleAuthService.Setup(x => x.IsAnonRequest()).Returns(true);

        var result = await _sut.TransformAsync(_stubClaimsPrincipal);

        AssertHelper.AssertNullClaim(result, ClaimTypes.Role);
    }

    void SetupSingleAuthClient(string clientName)
    {
        _mockOptions.Setup(m => m.Value).Returns(new SingleAuthAuthenticationOptions
        {
            Client = new List<SingleAuthClient>
           {
               new SingleAuthClient
               {
                   Name = clientName
               }
           }
        });
    }
}
public static class AssertHelper
{
    public static AndConstraint<StringAssertions> AssertClaim(ClaimsPrincipal result, string claimName, string value)
    {
        var claim = result.FindFirst(claimName);
        return claim.Value.Should().BeEquivalentTo(value);
    }
    public static AndConstraint<ObjectAssertions> AssertNullClaim(ClaimsPrincipal result, string claimName)
    {
        var claim = result.FindFirst(claimName);
        return claim.Should().BeNull();
    }
}
