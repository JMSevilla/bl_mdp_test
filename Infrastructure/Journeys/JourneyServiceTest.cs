using System;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using Moq;
using WTW.MdpService.Domain.Bereavement;
using WTW.MdpService.Domain.Common.Journeys;
using WTW.MdpService.Domain.Mdp;
using WTW.MdpService.Infrastructure.BereavementDb;
using WTW.MdpService.Infrastructure.Journeys;
using WTW.MdpService.Infrastructure.MdpDb;
using WTW.MdpService.Test.Domain.Bereavement;
using WTW.MdpService.Test.Domain.Mdp.Journeys;
using WTW.TestCommon.FixieConfig;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Infrastructure.Journeys;

public class JourneyServiceTest
{
    private readonly Mock<IRetirementJourneyRepository> _retirementJourneyRepositoryMock = new();
    private readonly Mock<ITransferJourneyRepository> _transferJourneyRepositoryMock = new();
    private readonly Mock<IBereavementJourneyRepository> _bereavementJourneyRepositoryMock = new();
    private readonly Mock<IJourneysRepository> _journeysRepositoryMock = new();
    private readonly JourneyService _journeyService;

    public JourneyServiceTest()
    {
        _journeyService = new JourneyService(
        _retirementJourneyRepositoryMock.Object,
        _transferJourneyRepositoryMock.Object,
        _bereavementJourneyRepositoryMock.Object,
        _journeysRepositoryMock.Object);
    }

    [Input("retirement")]
    [Input("dbretirementapplication")]
    public async Task GetJourney_ReturnsRetirementJourney_ForRetirementTypes(string journeyType)
    {
        var expectedJourney = new RetirementJourneyBuilder().Build();
        _retirementJourneyRepositoryMock.Setup(repo => repo.FindUnexpiredUnsubmittedJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(Option<RetirementJourney>.Some(expectedJourney));

        var result = await _journeyService.GetJourney(journeyType, "BG1", "RN1");

        result.IsSome.Should().BeTrue();
        result.Value().Should().Be(expectedJourney);
    }

    public async Task GetJourney_ReturnsTransferJourney_ForTransfer2Type()
    {
        var expectedJourney = new TransferJourneyBuilder().Build();
        _transferJourneyRepositoryMock.Setup(repo => repo.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(expectedJourney));

        var result = await _journeyService.GetJourney("transfer2", "BG2", "RN2");

        result.IsSome.Should().BeTrue();
        result.Value().Should().Be(expectedJourney);
    }

    public async Task GetJourney_ReturnsBereavementJourney_ForBereavementType()
    {
        var expectedJourney = new BereavementJourneyBuilder().Build().Right();
        _bereavementJourneyRepositoryMock.Setup(repo => repo.FindUnexpired(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(Option<BereavementJourney>.Some(expectedJourney));

        var result = await _journeyService.GetJourney("bereavement", "BG3", Guid.NewGuid().ToString());

        result.IsSome.Should().BeTrue();
        result.Value().Should().Be(expectedJourney);
    }

    public async Task GetJourney_ReturnsGenericJourney_ForGenericJourneyType()
    {
        var expectedJourney = new GenericJourney("RBS", "1234567", "transfer", "step1", "step2", true, "Started", DateTimeOffset.UtcNow);

        _journeysRepositoryMock.Setup(repo => repo.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<GenericJourney>.Some(expectedJourney));

        var result = await _journeyService.GetJourney("generic", "BG4", "RN4");

        result.IsSome.Should().BeTrue();
        result.Value().Should().Be(expectedJourney);
    }

    public async Task GetJourney_ReturnsNone_WhenJourneyTypeDoesNotExist()
    {
        var result = await _journeyService.GetJourney("nonexistentType", "BG5", "RN5");
        result.IsSome.Should().BeFalse();
    }

    public async Task FindUnexpiredOrSubmittedJourney_ReturnsRetirementJourney()
    {
        var expectedJourney = new RetirementJourneyBuilder().BuildWithSubmissionDate(DateTimeOffset.UtcNow);

        _retirementJourneyRepositoryMock.Setup(repo => repo.FindUnexpiredOrSubmittedJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
         .ReturnsAsync(Option<RetirementJourney>.Some(expectedJourney));

        var result = await _journeyService.FindUnexpiredOrSubmittedJourney("BG1", "RN1");

        result.IsSome.Should().BeTrue();
        result.Value().Should().Be(expectedJourney);
        result.Value().SubmissionDate.Should().Be(expectedJourney.SubmissionDate);
    }

    public async Task FindUnexpiredOrSubmittedJourney_ReturnsNone()
    {
        _retirementJourneyRepositoryMock.Setup(repo => repo.FindUnexpiredOrSubmittedJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
         .ReturnsAsync(It.IsAny<Option<RetirementJourney>>());

        var result = await _journeyService.FindUnexpiredOrSubmittedJourney("BG1", "RN1");
        result.IsSome.Should().BeFalse();
    }
}