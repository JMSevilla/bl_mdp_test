using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WTW.MdpService.IdentityVerification;
using WTW.MdpService.IdentityVerification.Services;
using WTW.MdpService.Infrastructure.Gbg;
using WTW.TestCommon.FixieConfig;
using WTW.Web.Errors;

namespace WTW.MdpService.Test.IdentityVerification;

public class IdentityVerificationControllerTest
{
    private readonly Mock<ICachedGbgScanClient> _cachedGbgScanClientMock;
    private readonly Mock<IIdvService> _idvServiceMock;
    private readonly IdentityVerificationController _sut;

    public IdentityVerificationControllerTest()
    {
        _cachedGbgScanClientMock = new Mock<ICachedGbgScanClient>();
        _idvServiceMock = new Mock<IIdvService>();
        _sut = new IdentityVerificationController(_cachedGbgScanClientMock.Object, _idvServiceMock.Object);
        _sut.SetupControllerContext();
    }

    public async Task CreateTokenReturns200_WhenTokenIsRetrievedFromGbg()
    {
        _cachedGbgScanClientMock.Setup(x => x.CreateToken()).ReturnsAsync(new GbgAccessTokenResponse { AccessToken = "test-access-token-15699" });

        var result = await _sut.CreateToken();

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as GbgTokenResponse;
        response.AccessToken.Should().Be("test-access-token-15699");

        _cachedGbgScanClientMock.Verify(x => x.CreateToken(), Times.Once);
    }

    [Input("PASS", "Passed")]
    [Input("PASS", "Refer")]
    [Input("REFER", "Passed")]
    [Input("REFER", "Refer")]
    public async Task VerifyIdentityReturns200_WhenServiceReturnsSuccess(string identityVerificationStatus, string documentValidationStatus)
    {
        var journeyType = "retirement";
        var verifyIdentityResponse = new VerifyIdentityResponse
        {
            IdentityVerificationStatus = identityVerificationStatus,
            DocumentValidationStatus = documentValidationStatus
        };

        _idvServiceMock
            .Setup(x => x.VerifyIdentity(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Prelude.Right<Error, VerifyIdentityResponse>(verifyIdentityResponse));

        var result = await _sut.VerifyIdentity(journeyType);

        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as VerifyIdentityResponse;
        response.Should().NotBeNull();
        response.Should().BeEquivalentTo(verifyIdentityResponse);

        _idvServiceMock.Verify(x => x.VerifyIdentity(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }
    public async Task VerifyIdentityReturns400_WhenServiceReturnsError()
    {
        var journeyType = "retirement";
        var error = Error.New("Invalid journey type");

        _idvServiceMock
            .Setup(x => x.VerifyIdentity(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Prelude.Left<Error, VerifyIdentityResponse>(error));


        var result = await _sut.VerifyIdentity(journeyType);

        result.Should().BeOfType<BadRequestObjectResult>();
        var response = ((BadRequestObjectResult)result).Value as ApiError;
        response.Should().NotBeNull();
        response.Errors[0].Message.Should().Be("Invalid journey type");

        _idvServiceMock.Verify(x => x.VerifyIdentity(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }
}