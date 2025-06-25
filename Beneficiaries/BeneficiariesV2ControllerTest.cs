using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Beneficiaries;
using WTW.MdpService.Infrastructure.MemberDb;
using WTW.MdpService.Infrastructure.MemberService;
using WTW.TestCommon.Helpers;

namespace WTW.MdpService.Test.Beneficiaries;

public class BeneficiariesV2ControllerTest
{

    private readonly Mock<IMemberRepository> _memberRepositoryMock;
    private readonly Mock<IMemberServiceClient> _memberServiceClientMock;
    private readonly Mock<ILogger<BeneficiariesV2Controller>> _loggerMock;
    private readonly BeneficiariesV2Controller _sut;

    public BeneficiariesV2ControllerTest()
    {
        _memberRepositoryMock = new Mock<IMemberRepository>();
        _memberServiceClientMock = new Mock<IMemberServiceClient>();
        _loggerMock = new Mock<ILogger<BeneficiariesV2Controller>>();

        _sut = new BeneficiariesV2Controller(_loggerMock.Object,
                                             _memberServiceClientMock.Object,
                                             _memberRepositoryMock.Object);

        _sut.SetupControllerContext();
    }

    public async Task BeneficiariesReturnsOkObjectResult()
    {
        var beneficiariesResponse = new BeneficiariesV2Response
        {
            People = new List<BeneficiaryPersonResponse>()
            {
                new () { Relationship = "CIVIL PARTNER" },
                new () { Relationship = "charity" },
                new () { Relationship = "Ex-Spouse" },
                new () { Relationship = "step child" },
                new () { Relationship = "TRUST" },
            },
            Organizations = new List<BeneficiaryOrganizationResponse>()
            {
                new ()
            }
        };

        _memberRepositoryMock
            .Setup(x => x.ExistsMember(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _memberServiceClientMock
            .Setup(x => x.GetBeneficiaries(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Option<BeneficiariesV2Response>.Some(beneficiariesResponse));

        var result = await _sut.Beneficiaries();

        var expectedMessage = $"Beneficiaries returned successfully for TestBusinessGroup TestReferenceNumber";
        _loggerMock.VerifyLogging(expectedMessage, LogLevel.Information, Times.Once());
        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as BeneficiariesV2Response;
        var people = response.People.ToList();
        people[0].Relationship.Should().Be("Civil partner");
        people[1].Relationship.Should().Be("Charity");
        people[2].Relationship.Should().Be("Ex-spouse");
        people[3].Relationship.Should().Be("Step child");
        people[4].Relationship.Should().Be("Trust");
        response.Organizations.Should().HaveCount(1);
        people[0].LumpSumPercentage.Should().BeNull();
        people[0].PensionPercentage.Should().BeNull();
    }


    public async Task BeneficiariesReturnsNoContent()
    {
        _memberRepositoryMock
            .Setup(x => x.ExistsMember(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _memberServiceClientMock
            .Setup(x => x.GetBeneficiaries(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new BeneficiariesV2Response());

        var result = await _sut.Beneficiaries();

        var expectedMessage = $"No beneficiaries found for TestBusinessGroup TestReferenceNumber";
        _loggerMock.VerifyLogging(expectedMessage, LogLevel.Information, Times.Once());
        result.Should().BeOfType<NoContentResult>();
    }


    public async Task BeneficiariesReturnsNotFound()
    {
        _memberServiceClientMock
            .Setup(x => x.GetBeneficiaries(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new BeneficiariesV2Response());

        var result = await _sut.Beneficiaries();

        var expectedMessage = $"Member not found for reference number TestReferenceNumber and business group TestBusinessGroup";
        _loggerMock.VerifyLogging(expectedMessage, LogLevel.Error, Times.Once());
        result.Should().BeOfType<NotFoundObjectResult>();
    }


    public async Task BeneficiariesReturnsBadRequestResult()
    {
        _memberRepositoryMock
            .Setup(x => x.ExistsMember(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var result = await _sut.Beneficiaries();

        var expectedMessage = $"Failed to retrieve beneficiaries data for TestBusinessGroup TestReferenceNumber";
        _loggerMock.VerifyLogging(expectedMessage, LogLevel.Error, Times.Once());
        result.Should().BeOfType<BadRequestObjectResult>();
    }
}