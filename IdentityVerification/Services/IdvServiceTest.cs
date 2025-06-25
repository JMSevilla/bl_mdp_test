using System;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Domain.Common.Journeys;
using WTW.MdpService.Domain.Mdp;
using WTW.MdpService.IdentityVerification;
using WTW.MdpService.IdentityVerification.Services;
using WTW.MdpService.Infrastructure.IdvService;
using WTW.MdpService.Infrastructure.MdpDb;
using WTW.MdpService.Test.Domain.Mdp.Journeys;
using WTW.TestCommon.FixieConfig;
using WTW.TestCommon.Helpers;
using WTW.Web;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.IdentityVerification.Services;

public class IdvServiceTest
{
    private readonly IdvService _sut;
    private readonly Mock<IJourneysRepository> _journeysRepositoryMock;
    private readonly Mock<IRetirementJourneyRepository> _retirementRepositoryMock;
    private readonly Mock<ITransferJourneyRepository> _transferRepositoryMock;
    private readonly Mock<ILogger<IdvService>> _loggerMock;
    private readonly Mock<IIdentityVerificationClient> _identityVerificationClientMock;
    private readonly string stubBusinessGroup;
    private readonly string stubReferenceNumber;
    private string stubJourneyType;

    public IdvServiceTest()
    {
        _journeysRepositoryMock = new Mock<IJourneysRepository>();
        _retirementRepositoryMock = new Mock<IRetirementJourneyRepository>();
        _transferRepositoryMock = new Mock<ITransferJourneyRepository>();
        _loggerMock = new Mock<ILogger<IdvService>>();
        _identityVerificationClientMock = new Mock<IIdentityVerificationClient>();

        stubBusinessGroup = "ABC";
        stubReferenceNumber = "1234567";
        stubJourneyType = JourneyTypes.DbRetirementApplication;

        _sut = new IdvService(
            _journeysRepositoryMock.Object,
            _retirementRepositoryMock.Object,
            _transferRepositoryMock.Object,
            _loggerMock.Object,
            _identityVerificationClientMock.Object);
    }

    public async Task VerifyIdentityReturnsError_WhenUnsupportedJourneyTypeIsReceived()
    {
        string journeyType = "unknownjourney";
        string error = $"Unsupported journey type: {journeyType}.";

        var result = await _sut.VerifyIdentity(stubBusinessGroup, stubReferenceNumber, journeyType);

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be(error);

        _loggerMock.VerifyLogging($"Failed to retrieve GBG journey ID for journey type: {journeyType}. Error: {error}", LogLevel.Error, Times.Once());
    }

    [Input(JourneyTypes.DbRetirementApplication)]
    [Input(JourneyTypes.DbCoreRetirementApplication)]
    [Input(JourneyTypes.DcRetirementApplication)]
    [Input(JourneyTypes.TransferApplication)]
    public async Task VerifyIdentityReturnsError_WhenJourneyNotFound(string journeyType)
    {
        _journeysRepositoryMock
            .Setup(x => x.Find(stubBusinessGroup, stubReferenceNumber, stubJourneyType))
            .ReturnsAsync(Option<GenericJourney>.None);
        _retirementRepositoryMock
            .Setup(x => x.Find(stubBusinessGroup, stubReferenceNumber))
            .ReturnsAsync(Option<RetirementJourney>.None);
        _transferRepositoryMock
            .Setup(x => x.Find(stubBusinessGroup, stubReferenceNumber))
            .ReturnsAsync(Option<TransferJourney>.None);

        var result = await _sut.VerifyIdentity(stubBusinessGroup, stubReferenceNumber, journeyType);

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be($"Failed to retrieve journey with type {journeyType}.");

        _loggerMock.VerifyLogging($"Failed to retrieve journey with type {journeyType}.", LogLevel.Error, Times.Once());
    }

    [Input(JourneyTypes.DbCoreRetirementApplication)]
    [Input(JourneyTypes.DcRetirementApplication)]
    public async Task VerifyIdentityReturnsError_WhenGbgIdNotFoundForGenericJourney(string journeyType)
    {
        _journeysRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(GetGenericJourney(false, true));

        var result = await _sut.VerifyIdentity(stubBusinessGroup, stubReferenceNumber, journeyType);

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be($"Failed to retrieve gbg id. Journey type: {journeyType}.");

        _loggerMock.VerifyLogging($"Failed to retrieve gbg id with journey type {journeyType}. Error: Field gbgId not found in JSON string", LogLevel.Error, Times.Once());
    }

    public async Task VerifyIdentityReturnsError_WhenGbgIdNotFoundForRetirementJourney()
    {
        _retirementRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(GetRetirementJourney(false));

        var result = await _sut.VerifyIdentity(stubBusinessGroup, stubReferenceNumber, JourneyTypes.DbRetirementApplication);

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be($"Failed to retrieve gbg id. Journey type: {JourneyTypes.DbRetirementApplication}.");

        _loggerMock.VerifyLogging($"Gbg id is null for journey type {JourneyTypes.DbRetirementApplication}.", LogLevel.Error, Times.Once());
    }

    public async Task VerifyIdentityReturnsError_WhenGbgIdNotFoundForTransferJourney()
    {
        _transferRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(GetTransferJourney(false));

        var result = await _sut.VerifyIdentity(stubBusinessGroup, stubReferenceNumber, JourneyTypes.TransferApplication);

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be($"Failed to retrieve gbg id. Journey type: {JourneyTypes.TransferApplication}.");

        _loggerMock.VerifyLogging($"Gbg id is null for journey type {JourneyTypes.TransferApplication}.", LogLevel.Error, Times.Once());
    }

    [Input(JourneyTypes.DbCoreRetirementApplication)]
    [Input(JourneyTypes.DcRetirementApplication)]
    public async Task VerifyIdentityReturnsError_WhenGbgIdIsInvalidForGenericJourney(string journeyType)
    {
        var gbgJourneyId = "invalid-guid";
        _journeysRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(GetGenericJourney(true, true, true));

        var result = await _sut.VerifyIdentity(stubBusinessGroup, stubReferenceNumber, journeyType);

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be($"Failed to retrieve gbg journey id. Journey type: {journeyType}.");

        _loggerMock.VerifyLogging($"Failed to parse {gbgJourneyId} with journey type {journeyType}", LogLevel.Error, Times.Once());
    }

    [Input("PASS", "Passed", JourneyTypes.DbCoreRetirementApplication)]
    [Input("PASS", "Refer", JourneyTypes.DbCoreRetirementApplication)]
    [Input("REFER", "Passed", JourneyTypes.DbCoreRetirementApplication)]
    [Input("REFER", "Refer", JourneyTypes.DbCoreRetirementApplication)]
    [Input("PASS", "Passed", JourneyTypes.DcRetirementApplication)]
    [Input("PASS", "Refer", JourneyTypes.DcRetirementApplication)]
    [Input("REFER", "Passed", JourneyTypes.DcRetirementApplication)]
    [Input("REFER", "Refer", JourneyTypes.DcRetirementApplication)]
    public async Task VerifyIdentityCallsIdentityVerificationClientForGenericJourney_WhenSuccessful(string identityVerificationStatus, string documentValidationStatus, string journeyType)
    {
        var gbgJourneyId = Guid.NewGuid();
        var journey = new Mock<GenericJourney>();

        _journeysRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(GetGenericJourney(true, true));

        var verifyIdentityResponse = new VerifyIdentityResponse
        {
            IdentityVerificationStatus = identityVerificationStatus,
            DocumentValidationStatus = documentValidationStatus
        };

        _identityVerificationClientMock
            .Setup(x => x.VerifyIdentity(stubBusinessGroup, stubReferenceNumber, It.IsAny<VerifyIdentityRequest>()))
            .ReturnsAsync(verifyIdentityResponse);

        var result = await _sut.VerifyIdentity(stubBusinessGroup, stubReferenceNumber, journeyType);

        result.IsRight.Should().BeTrue();
        result.Right().Should().BeEquivalentTo(verifyIdentityResponse);

        _identityVerificationClientMock.Verify(x => x.VerifyIdentity(stubBusinessGroup, stubReferenceNumber, It.IsAny<VerifyIdentityRequest>()), Times.Once);
    }

    [Input("PASS", "Passed", JourneyTypes.DbRetirementApplication)]
    [Input("PASS", "Refer", JourneyTypes.DbRetirementApplication)]
    [Input("REFER", "Passed", JourneyTypes.DbRetirementApplication)]
    [Input("REFER", "Refer", JourneyTypes.DbRetirementApplication)]
    [Input("PASS", "Passed", JourneyTypes.GenericRetirementApplication)]
    [Input("PASS", "Refer", JourneyTypes.GenericRetirementApplication)]
    [Input("REFER", "Passed", JourneyTypes.GenericRetirementApplication)]
    [Input("REFER", "Refer", JourneyTypes.GenericRetirementApplication)]
    public async Task VerifyIdentityCallsIdentityVerificationClientForRetirementJourney_WhenSuccessful(string identityVerificationStatus, string documentValidationStatus, string journeyType)
    {
        _retirementRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(GetRetirementJourney(true));

        var verifyIdentityResponse = new VerifyIdentityResponse
        {
            IdentityVerificationStatus = identityVerificationStatus,
            DocumentValidationStatus = documentValidationStatus
        };

        _identityVerificationClientMock
            .Setup(x => x.VerifyIdentity(stubBusinessGroup, stubReferenceNumber, It.IsAny<VerifyIdentityRequest>()))
            .ReturnsAsync(verifyIdentityResponse);

        var result = await _sut.VerifyIdentity(stubBusinessGroup, stubReferenceNumber, journeyType);

        result.IsRight.Should().BeTrue();
        result.Right().Should().BeEquivalentTo(verifyIdentityResponse);

        _identityVerificationClientMock.Verify(x => x.VerifyIdentity(stubBusinessGroup, stubReferenceNumber, It.IsAny<VerifyIdentityRequest>()), Times.Once);
    }

    [Input("PASS", "Passed")]
    [Input("PASS", "Refer")]
    [Input("REFER", "Passed")]
    [Input("REFER", "Refer")]
    public async Task VerifyIdentityCallsIdentityVerificationClientForTransferJourney_WhenSuccessful(string identityVerificationStatus, string documentValidationStatus)
    {
        var gbgJourneyId = Guid.NewGuid();
        var journey = new Mock<GenericJourney>();

        _transferRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(GetTransferJourney(true));

        var verifyIdentityResponse = new VerifyIdentityResponse
        {
            IdentityVerificationStatus = identityVerificationStatus,
            DocumentValidationStatus = documentValidationStatus
        };

        _identityVerificationClientMock
            .Setup(x => x.VerifyIdentity(stubBusinessGroup, stubReferenceNumber, It.IsAny<VerifyIdentityRequest>()))
            .ReturnsAsync(verifyIdentityResponse);

        var result = await _sut.VerifyIdentity(stubBusinessGroup, stubReferenceNumber, JourneyTypes.TransferApplication);

        result.IsRight.Should().BeTrue();
        result.Right().Should().BeEquivalentTo(verifyIdentityResponse);

        _identityVerificationClientMock.Verify(x => x.VerifyIdentity(stubBusinessGroup, stubReferenceNumber, It.IsAny<VerifyIdentityRequest>()), Times.Once);
    }

    public async Task VerifyIdentity_ShouldTruncateRequestSourceToMaxLengthOf10()
    {
        var longJourneyType = JourneyTypes.DbCoreRetirementApplication;
        var expectedRequestSource = $"{MdpConstants.AppName}_{JourneyTypes.DbCoreRetirementApplication}".Substring(0, 10);

        _journeysRepositoryMock
            .Setup(x => x.Find(stubBusinessGroup, stubReferenceNumber, longJourneyType))
            .ReturnsAsync(GetGenericJourney(true, true));

        var verifyIdentityResponse = new VerifyIdentityResponse
        {
            IdentityVerificationStatus = "PASS",
            DocumentValidationStatus = "Passed"
        };

        _identityVerificationClientMock
            .Setup(x => x.VerifyIdentity(stubBusinessGroup, stubReferenceNumber, It.IsAny<VerifyIdentityRequest>()))
            .ReturnsAsync(verifyIdentityResponse);

        var result = await _sut.VerifyIdentity(stubBusinessGroup, stubReferenceNumber, longJourneyType);

        result.IsRight.Should().BeTrue();
        result.Right().Should().BeEquivalentTo(verifyIdentityResponse);

        _identityVerificationClientMock.Verify(x => x.VerifyIdentity(
            stubBusinessGroup,
            stubReferenceNumber,
            It.Is<VerifyIdentityRequest>(req => req.RequestSource == expectedRequestSource)
        ), Times.Once);
    }

    [Input(JourneyTypes.DbRetirementApplication)]
    [Input(JourneyTypes.DbCoreRetirementApplication)]
    [Input(JourneyTypes.DcRetirementApplication)]
    [Input(JourneyTypes.TransferApplication)]
    public async Task VerifyIdentityReturnsError_WhenHttpClientCallReturnsNull(string journeyType)
    {
        _journeysRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(GetGenericJourney(true, true));
        _retirementRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(GetRetirementJourney(true));
        _transferRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(GetTransferJourney(true));
        _identityVerificationClientMock
            .Setup(x => x.VerifyIdentity(stubBusinessGroup, stubReferenceNumber, It.IsAny<VerifyIdentityRequest>()))
            .ReturnsAsync((VerifyIdentityResponse)null);

        var result = await _sut.VerifyIdentity(stubBusinessGroup, stubReferenceNumber, journeyType);

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be("VerifyIdentity returned an error.");

        _loggerMock.VerifyLogging($"{nameof(_sut.VerifyIdentity)} returned non success response.", LogLevel.Error, Times.Once());
    }

    public async Task SaveIdentityVerificationShouldReturnSuccess_WhenClientCallSucceeds()
    {
        var caseCode = "RTP9";
        var caseNumber = "CASE123";
        var response = new UpdateIdentityResultResponse { Message = "Success" };

        _identityVerificationClientMock
            .Setup(x => x.SaveIdentityVerification(stubBusinessGroup, stubReferenceNumber, It.IsAny<SaveIdentityVerificationRequest>()))
            .ReturnsAsync(response);
        var result = await _sut.SaveIdentityVerification(stubBusinessGroup, stubReferenceNumber, caseCode, caseNumber);

        result.IsRight.Should().BeTrue();
        result.Right().Should().BeEquivalentTo(response);

        _identityVerificationClientMock.Verify(x => x.SaveIdentityVerification(stubBusinessGroup, stubReferenceNumber, It.IsAny<SaveIdentityVerificationRequest>()), Times.Once);

        _loggerMock.VerifyLogging($"Saving identity verification for business group: {stubBusinessGroup}, reference number: {stubReferenceNumber}, case code: {caseCode}, case number: {caseNumber}", LogLevel.Information, Times.Once());
    }
    public async Task SaveIdentityVerificationShouldReturnError_WhenClientReturnsNull()
    {
        var caseCode = "RTP9";
        var caseNumber = "CASE123";

        _identityVerificationClientMock
            .Setup(x => x.SaveIdentityVerification(stubBusinessGroup, stubReferenceNumber, It.IsAny<SaveIdentityVerificationRequest>()))
            .ReturnsAsync((UpdateIdentityResultResponse)null);
        var result = await _sut.SaveIdentityVerification(stubBusinessGroup, stubReferenceNumber, caseCode, caseNumber);

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be("SaveIdentityVerification returned an error.");

        _identityVerificationClientMock.Verify(x => x.SaveIdentityVerification(stubBusinessGroup, stubReferenceNumber, It.IsAny<SaveIdentityVerificationRequest>()), Times.Once);

        _loggerMock.VerifyLogging($"Saving identity verification for business group: {stubBusinessGroup}, reference number: {stubReferenceNumber}, case code: {caseCode}, case number: {caseNumber}", LogLevel.Information, Times.Once());
        _loggerMock.VerifyLogging($"{nameof(_sut.SaveIdentityVerification)} returned non success response.", LogLevel.Error, Times.Once());
    }

    private static GenericJourney GetGenericJourney(bool hasGbgId = false, bool hasGbgUserIdentificationFormId = false, bool invalidGuid = false)
    {
        var journey = new GenericJourney("ABC", "1234567", "retirement", "step1", "step2", true, null, DateTimeOffset.UtcNow);
        if (hasGbgUserIdentificationFormId)
        {
            var step = journey.GetStepByKey("step1");
            if (hasGbgId)
            {
                step.Value().UpdateGenericData("gbg_user_identification_form_id", "{\"gbgId\":\"c62020f0-4175-4d52-bedd-30756f2b41b4\"}");
            }
            else
            {
                step.Value().UpdateGenericData("gbg_user_identification_form_id", "{\"other\":\"test\"}");
            }
            if (invalidGuid)
            {
                step.Value().UpdateGenericData("gbg_user_identification_form_id", "{\"gbgId\":\"invalid-guid\"}");
            }
        }

        return journey;
    }
    private static TransferJourney GetTransferJourney(bool hasGbgId = false)
    {
        var transferJourney = new TransferJourneyBuilder().Build();
        if (hasGbgId)
        {
            transferJourney.SaveGbgId(Guid.Parse("c62020f0-4175-4d52-bedd-30756f2b41b4"));
        }

        return transferJourney;
    }
    private static RetirementJourney GetRetirementJourney(bool hasGbgId = false)
    {
        var retirementJourney = new RetirementJourneyBuilder().Build();
        if (hasGbgId)
        {
            retirementJourney.SaveGbgId(Guid.Parse("c62020f0-4175-4d52-bedd-30756f2b41b4"));
        }
        return retirementJourney;
    }
}
