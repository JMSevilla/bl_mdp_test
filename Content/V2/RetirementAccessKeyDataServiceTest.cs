using System;
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
using WTW.MdpService.Infrastructure.MdpDb;
using WTW.MdpService.Infrastructure.MemberDb;
using WTW.MdpService.Test.Domain.Mdp.Calculations;
using WTW.MdpService.Test.Domain.Mdp.Journeys;
using WTW.MdpService.Test.Domain.Members;
using WTW.Web.LanguageExt;
using WTW.Web.Serialization;

namespace WTW.MdpService.Test.Content.V2;

public class RetirementAccessKeyDataServiceTest
{
    private readonly Mock<ICalculationsRepository> _calculationsRepositoryMock;
    private readonly Mock<ICalculationsClient> _calculationsClientMock;
    private readonly Mock<IMemberRepository> _memberRepositoryMock;
    private readonly Mock<IRetirementJourneyRepository> _retirementJourneyRepositoryMock;
    private readonly Mock<ICalculationsParser> _calculationsParserMock;
    private readonly Mock<IMdpUnitOfWork> _mdpUnitOfWorkMock;
    private readonly Mock<ILogger<RetirementAccessKeyDataService>> _loggerMock;
    private readonly Mock<IJourneysRepository> _journeysRepositoryMock;
    private readonly RetirementAccessKeyDataService _sut;
    private readonly RetirementDatesAgesResponse _datesAgesResponse;
    private readonly Member _member;

    public RetirementAccessKeyDataServiceTest()
    {
        _calculationsRepositoryMock = new Mock<ICalculationsRepository>();
        _calculationsClientMock = new Mock<ICalculationsClient>();
        _memberRepositoryMock = new Mock<IMemberRepository>();
        _retirementJourneyRepositoryMock = new Mock<IRetirementJourneyRepository>();
        _calculationsParserMock = new Mock<ICalculationsParser>();
        _mdpUnitOfWorkMock = new Mock<IMdpUnitOfWork>();
        _loggerMock = new Mock<ILogger<RetirementAccessKeyDataService>>();
        _journeysRepositoryMock = new Mock<IJourneysRepository>();

        _sut = new RetirementAccessKeyDataService(
            _calculationsRepositoryMock.Object,
            _calculationsClientMock.Object,
            _memberRepositoryMock.Object,
            _retirementJourneyRepositoryMock.Object,
            _calculationsParserMock.Object,
            _mdpUnitOfWorkMock.Object,
            _loggerMock.Object,
            _journeysRepositoryMock.Object);

        _datesAgesResponse = new RetirementDatesAgesResponse
        {
            HasLockedInTransferQuote = false,
            RetirementAges = new RetirementAgesResponse
            {
                EarliestRetirementAge = 55,
                NormalRetirementAge = 60,
            },
            RetirementDates = new RetirementDatesResponse
            {
                EarliestRetirementDate = new DateTime(2017, 11, 07),
                NormalRetirementDate = new DateTime(2022, 11, 07),
            },
        };
        _member = new MemberBuilder().Build();
    }

    public async Task GetNewRetirementCalculationReturnsError_WhenMemberIsNotValidForRaCalculation()
    {
        _memberRepositoryMock
            .Setup(x => x.IsMemberValidForRaCalculation(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var result = await _sut.GetNewRetirementCalculation(_datesAgesResponse, _member);

        result.Right().CalculationStatus.Should().Be("forbidden");

        _calculationsRepositoryMock.Verify(x => x.Remove(It.IsAny<Calculation>()), Times.Never);
        _calculationsRepositoryMock.Verify(x => x.Create(It.IsAny<Calculation>()), Times.Once);
        _mdpUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
    }

    public async Task GetNewRetirementCalculationReturnsError_WhenCalcApiFails()
    {
        _memberRepositoryMock
            .Setup(x => x.IsMemberValidForRaCalculation(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _calculationsClientMock
            .Setup(x => x.RetirementCalculationV2(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Error.New("test"));

        var result = await _sut.GetNewRetirementCalculation(_datesAgesResponse, _member);

        result.Left().Message.Should().Be("test");

        _calculationsRepositoryMock.Verify(x => x.Remove(It.IsAny<Calculation>()), Times.Never);
        _calculationsRepositoryMock.Verify(x => x.Create(It.IsAny<Calculation>()), Times.Never);
        _mdpUnitOfWorkMock.Verify(x => x.Commit(), Times.Never);
    }

    public async Task GetNewRetirementCalculationReturnsError_WhenCalcApiReturnsNoFigures()
    {
        var noFiguresCalculationStatus = "noFigures";
        _memberRepositoryMock
            .Setup(x => x.IsMemberValidForRaCalculation(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _calculationsClientMock
            .Setup(x => x.RetirementCalculationV2(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Error.New(noFiguresCalculationStatus, noFiguresCalculationStatus));

        var result = await _sut.GetNewRetirementCalculation(_datesAgesResponse, _member);

        result.Right().CalculationStatus.Should().Be(noFiguresCalculationStatus);

        _calculationsRepositoryMock.Verify(x => x.Remove(It.IsAny<Calculation>()), Times.Never);
        _calculationsRepositoryMock.Verify(x => x.Create(It.IsAny<Calculation>()), Times.Once);
        _mdpUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
    }

    public async Task GetNewRetirementCalculationForDcMember()
    {
        _memberRepositoryMock
            .Setup(x => x.IsMemberValidForRaCalculation(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _calculationsParserMock
           .Setup(x => x.GetRetirementDatesAgesJson(It.IsAny<RetirementDatesAgesResponse>()))
           .Returns("{}");

        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new CalculationBuilder().BuildV2());

        _journeysRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), "dcretirementapplication"))
            .ReturnsAsync(Option<GenericJourney>.None);

        var result = await _sut.GetNewRetirementCalculation(_datesAgesResponse, new MemberBuilder().SchemeType("DC").Build());

        result.Right().ReferenceNumber.Should().Be("0003994");
        result.Right().BusinessGroup.Should().Be("RBS");
        result.Right().RetirementDatesAgesJson.Should().Be("{}");
        result.Right().RetirementJsonV2.Should().BeEmpty();
        result.Right().QuotesJsonV2.Should().BeEmpty();
        result.Right().EffectiveRetirementDate.Should().Be(DateTimeOffset.UtcNow.Date.AddMonths(6).ToUniversalTime());
        result.Right().IsCalculationSuccessful.Should().BeNull();

        _calculationsRepositoryMock.Verify(x => x.Remove(It.IsAny<Calculation>()), Times.Once);
        _calculationsRepositoryMock.Verify(x => x.Create(It.IsAny<Calculation>()), Times.Once);
        _mdpUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
    }

    public async Task GetNewRetirementCalculationForDbMember()
    {
        _memberRepositoryMock
            .Setup(x => x.IsMemberValidForRaCalculation(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _calculationsParserMock
           .Setup(x => x.GetRetirementDatesAgesJson(It.IsAny<RetirementDatesAgesResponse>()))
           .Returns("{}");

        var calculationResponse = JsonSerializer.Deserialize<RetirementResponseV2>(TestData.RetirementV2ResponseJson, SerialiationBuilder.Options());
        _calculationsClientMock
           .Setup(x => x.RetirementCalculationV2(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<bool>()))
           .ReturnsAsync((calculationResponse, "PV"));

        _calculationsParserMock
           .Setup(x => x.GetRetirementJsonV2(It.IsAny<RetirementResponseV2>(), It.IsAny<string>()))
           .Returns(("{}", "test"));

        var result = await _sut.GetNewRetirementCalculation(_datesAgesResponse, _member);

        result.Right().ReferenceNumber.Should().Be("0003994");
        result.Right().BusinessGroup.Should().Be("RBS");
        result.Right().RetirementDatesAgesJson.Should().Be("{}");
        result.Right().RetirementJsonV2.Should().Be("{}");
        result.Right().QuotesJsonV2.Should().Be("test");
        result.Right().EffectiveRetirementDate.Should().Be(DateTimeOffset.UtcNow.Date.ToUniversalTime());
        result.Right().IsCalculationSuccessful.Should().BeTrue();

        _calculationsRepositoryMock.Verify(x => x.Remove(It.IsAny<Calculation>()), Times.Never);
        _calculationsRepositoryMock.Verify(x => x.Create(It.IsAny<Calculation>()), Times.Once);
        _mdpUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
    }

    public void GetsUndefinedRetirementApplicationStatus_WhenNoRetirementCalculationExists()
    {
        var result = _sut.GetRetirementApplicationStatus(_member, Error.New(""), It.IsAny<int>(), It.IsAny<int>());

        result.Should().Be(RetirementApplicationStatus.Undefined);
    }

    public void GetsRetirementApplicationStatus_WhenAllDataIsValid()
    {
        _calculationsParserMock
            .Setup(x => x.GetRetirementDatesAges(It.IsAny<string>()))
            .Returns(GetRetirementDatesAges());

        var result = _sut.GetRetirementApplicationStatus(_member, new CalculationBuilder().BuildV2(), It.IsAny<int>(), It.IsAny<int>());

        result.Should().Be(RetirementApplicationStatus.NotEligibleToStart);
    }

    public async Task GetExistingRetirementJourneyTypeReturnsDcRetirementApplication_WhenDcCalculationExists()
    {
        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new CalculationBuilder().BuildV2());

        var result = await _sut.GetExistingRetirementJourneyType(new MemberBuilder().SchemeType("DC").Build());

        result.Should().Be(ExistingRetirementJourneyType.DcRetirementApplication);
    }

    public async Task GetExistingRetirementJourneyTypeReturnsNone_WhenNoCalculationAndNoDcJourneyExist()
    {
        _calculationsRepositoryMock
            .Setup(x => x.FindWithJourney(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<Calculation>.None);

        var result = await _sut.GetExistingRetirementJourneyType(_member);

        result.Should().Be(ExistingRetirementJourneyType.None);
    }

    public async Task GetExistingRetirementJourneyTypeReturnsNone_WhenDcJourneyExpired()
    {
        _journeysRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), "dcretirementapplication"))
            .ReturnsAsync(new GenericJourney("RBS", "1111122", "dc-journey", "page1", "page2", false, "started", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(-10)));

        _calculationsRepositoryMock
            .Setup(x => x.FindWithJourney(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<Calculation>.None);

        var result = await _sut.GetExistingRetirementJourneyType(new MemberBuilder().SchemeType("DC").Build());

        result.Should().Be(ExistingRetirementJourneyType.None);
    }

    public async Task GetExistingRetirementJourneyTypeReturnsNone_WhenCalculationAndDbJourneyIsSetToDelete()
    {
        var journey = new RetirementJourneyBuilder().Build();
        journey.Submit(new byte[2], DateTimeOffset.UtcNow, "1");
        var calculation = new CalculationBuilder().BuildV2();
        calculation.SetJourney(journey, "label");

        _calculationsRepositoryMock
              .Setup(x => x.FindWithJourney(It.IsAny<string>(), It.IsAny<string>()))
              .ReturnsAsync(calculation);

        var result = await _sut.GetExistingRetirementJourneyType(GetMemberBuilderWithLastRTP9ClosedOrAbandoned().Build());

        result.Should().Be(ExistingRetirementJourneyType.None);
    }

    public async Task GetExistingRetirementJourneyTypeReturnsDbRetirementApplication_WhenCalculationExists()
    {
        var journey = new RetirementJourneyBuilder().Build();
        journey.Submit(new byte[2], DateTimeOffset.UtcNow, "1");
        var calculation = new CalculationBuilder().BuildV2();
        calculation.SetJourney(journey, "label");

        _calculationsRepositoryMock
              .Setup(x => x.FindWithJourney(It.IsAny<string>(), It.IsAny<string>()))
              .ReturnsAsync(calculation);

        var result = await _sut.GetExistingRetirementJourneyType(_member);

        result.Should().Be(ExistingRetirementJourneyType.DbRetirementApplication);
    }

    public async Task GetRetirementCalculationWithJourneyThrows_WhenCalculationIsNotFound()
    {
        _calculationsRepositoryMock
              .Setup(x => x.FindWithJourney(It.IsAny<string>(), It.IsAny<string>()))
              .ReturnsAsync(Option<Calculation>.None);

        var action = async () => await _sut.GetRetirementCalculationWithJourney(_datesAgesResponse, It.IsAny<string>(), It.IsAny<string>());

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    public async Task GetRetirementCalculationWithJourneyUpdates_WhenJourneyIsSubmittedWithRetirementJsonV2Null()
    {
        var journey = new RetirementJourneyBuilder().Build();
        journey.Submit(new byte[2], DateTimeOffset.UtcNow, "1");
        var calculation = new CalculationBuilder().BuildV1();
        calculation.SetJourney(journey, "label");

        _calculationsRepositoryMock
              .Setup(x => x.FindWithJourney(It.IsAny<string>(), It.IsAny<string>()))
              .ReturnsAsync(calculation);

        _calculationsParserMock
           .Setup(x => x.GetRetirementDatesAges(It.IsAny<string>()))
           .Returns(GetRetirementDatesAges());

        _calculationsParserMock
           .Setup(x => x.GetRetirementJsonV2(It.IsAny<RetirementResponseV2>(), It.IsAny<string>()))
           .Returns(("{\"updated\":\"updated\"}", "test"));

        var calculationResponse = JsonSerializer.Deserialize<RetirementResponseV2>(TestData.RetirementV2ResponseJson, SerialiationBuilder.Options());
        _calculationsClientMock
           .Setup(x => x.RetirementCalculationV2(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<bool>()))
           .ReturnsAsync((calculationResponse, "PV"));

        var result = await _sut.GetRetirementCalculationWithJourney(_datesAgesResponse, It.IsAny<string>(), "RBS");

        result.Right().RetirementJsonV2.Should().Be("{\"updated\":\"updated\"}");
        _mdpUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
    }

    public async Task GetRetirementCalculationWithJourneyAndUpdates_WhenJourneyIsNotSubmittedAndExpired_AndCalcApiSucceeds()
    {
        var journey = new RetirementJourneyBuilder().daysToExpire(-90).Build();
        var calculation = new CalculationBuilder().BuildV1();
        calculation.SetJourney(journey, "label");

        _calculationsRepositoryMock
              .Setup(x => x.FindWithJourney(It.IsAny<string>(), It.IsAny<string>()))
              .ReturnsAsync(calculation);

        _calculationsParserMock
           .Setup(x => x.GetRetirementDatesAgesJson(It.IsAny<RetirementDatesAgesResponse>()))
           .Returns("{datesAgesJson}");

        _calculationsParserMock
           .Setup(x => x.GetRetirementJsonV2(It.IsAny<RetirementResponseV2>(), It.IsAny<string>()))
           .Returns(("{\"updated\":\"updated\"}", "test"));

        var calculationResponse = JsonSerializer.Deserialize<RetirementResponseV2>(TestData.RetirementV2ResponseJson, SerialiationBuilder.Options());
        _calculationsClientMock
           .Setup(x => x.RetirementCalculationV2(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<bool>()))
           .ReturnsAsync((calculationResponse, "PV"));

        _memberRepositoryMock
            .Setup(x => x.IsMemberValidForRaCalculation(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var result = await _sut.GetRetirementCalculationWithJourney(_datesAgesResponse, It.IsAny<string>(), "RBS");

        result.Right().EffectiveRetirementDate.Date.Should().Be(DateTimeOffset.UtcNow.Date.ToUniversalTime().Date);
        result.Right().RetirementDatesAgesJson.Should().Be("{datesAgesJson}");
        result.Right().IsCalculationSuccessful.Should().BeTrue();
        _mdpUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
    }

    public async Task GetRetirementCalculationWithJourneyAndUpdates_WhenJourneyIsNotSubmittedAndExpired_AndCalcApiFails()
    {
        var journey = new RetirementJourneyBuilder().daysToExpire(-90).Build();
        var calculation = new CalculationBuilder().BuildV2();
        calculation.SetJourney(journey, "label");

        _calculationsRepositoryMock
              .Setup(x => x.FindWithJourney(It.IsAny<string>(), It.IsAny<string>()))
              .ReturnsAsync(calculation);

        _calculationsParserMock
           .Setup(x => x.GetRetirementDatesAgesJson(It.IsAny<RetirementDatesAgesResponse>()))
           .Returns("{datesAgesJson}");

        _calculationsParserMock
           .Setup(x => x.GetRetirementJsonV2(It.IsAny<RetirementResponseV2>(), It.IsAny<string>()))
           .Returns(("{\"updated\":\"updated\"}", "test"));

        var calculationResponse = JsonSerializer.Deserialize<RetirementResponseV2>(TestData.RetirementV2ResponseJson, SerialiationBuilder.Options());
        _calculationsClientMock
           .Setup(x => x.RetirementCalculationV2(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<bool>()))
           .ReturnsAsync(Error.New("test"));

        _calculationsParserMock
           .Setup(x => x.GetRetirementV2(It.IsAny<string>()))
           .Returns(new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options())));

        _memberRepositoryMock
            .Setup(x => x.IsMemberValidForRaCalculation(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var result = await _sut.GetRetirementCalculationWithJourney(_datesAgesResponse, It.IsAny<string>(), "RBS");

        result.Left().Message.Should().Be("test");
        _mdpUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
    }

    public async Task GetRetirementCalculationWithJourneyAndUpdates_WhenJourneyIsNotSubmittedAndExpired_AndCalcIsForbidden()
    {
        var journey = new RetirementJourneyBuilder().daysToExpire(-90).Build();
        var calculation = new CalculationBuilder().BuildV2();
        calculation.SetJourney(journey, "label");

        _calculationsRepositoryMock
              .Setup(x => x.FindWithJourney(It.IsAny<string>(), It.IsAny<string>()))
              .ReturnsAsync(calculation);

        _calculationsParserMock
           .Setup(x => x.GetRetirementDatesAgesJson(It.IsAny<RetirementDatesAgesResponse>()))
           .Returns("{datesAgesJson}");

        _calculationsParserMock
           .Setup(x => x.GetRetirementJsonV2(It.IsAny<RetirementResponseV2>(), It.IsAny<string>()))
           .Returns(("{\"updated\":\"updated\"}", "test"));

        _calculationsParserMock
           .Setup(x => x.GetRetirementV2(It.IsAny<string>()))
           .Returns(new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options())));

        _memberRepositoryMock
            .Setup(x => x.IsMemberValidForRaCalculation(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var result = await _sut.GetRetirementCalculationWithJourney(_datesAgesResponse, It.IsAny<string>(), "RBS");

        result.Left().Inner.Value().Message.Should().Be("forbidden");
        _mdpUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
    }

    public async Task GetRetirementCalculationWithJourneyAndUpdates_WhenJourneyGbgStepIsOlderThan30Days()
    {
        var journey = new RetirementJourneyBuilder().BuildWithSteps();
        journey.TrySubmitStep("step2", "submit_document_info", DateTimeOffset.UtcNow.AddDays(-37));
        journey.TrySubmitStep("submit_document_info", "submit_review", DateTimeOffset.UtcNow.AddDays(-36));
        journey.TrySubmitStep("submit_review", "step5", DateTimeOffset.UtcNow.AddDays(-36));
        var calculation = new CalculationBuilder().BuildV2();
        calculation.SetJourney(journey, "label");

        _calculationsRepositoryMock
              .Setup(x => x.FindWithJourney(It.IsAny<string>(), It.IsAny<string>()))
              .ReturnsAsync(calculation);

        journey.JourneyBranches.Single().JourneySteps.Count.Should().Be(4);
        var result = await _sut.GetRetirementCalculationWithJourney(_datesAgesResponse, It.IsAny<string>(), "RBS");

        result.IsRight.Should().BeTrue();
        journey.JourneyBranches.Single().JourneySteps.Count.Should().Be(3);
        _mdpUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
    }

    public async Task GetsRetirementCalculationWithJourneyReturnsSome_WhenCalculationExist()
    {
        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new CalculationBuilder().BuildV2());

        var result = await _sut.GetRetirementCalculation(It.IsAny<string>(), It.IsAny<string>());

        result.IsSome.Should().BeTrue();
    }

    public async Task GetsRetirementCalculationWithJourneyReturnsNone_WhenCalculationNotExist()
    {
        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<Calculation>.None);

        var result = await _sut.GetRetirementCalculation(It.IsAny<string>(), It.IsAny<string>());

        result.IsSome.Should().BeFalse();
    }

    public async Task UpdatesRetirementDatesAgesJson()
    {
        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<Calculation>.None);

        await _sut.UpdateRetirementDatesAges(new CalculationBuilder().BuildV2(), new RetirementDatesAgesResponse());

        _mdpUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
    }

    private static MemberBuilder GetMemberBuilderWithLastRTP9ClosedOrAbandoned()
    {
        var paperApplication1 = new PaperRetirementApplication(
           "AC", "RTP9", "11111111", "someType", "someStatus", null, null, new BatchCreateDetails("paper"));
        var paperApplication2 = new PaperRetirementApplication(
            "AC", "RTP9", "11111112", "someType", "someStatus", DateTimeOffset.UtcNow.AddDays(-23), null, new BatchCreateDetails("paper"));

        return new MemberBuilder()
                .PaperRetirementApplications(paperApplication1, paperApplication2);
    }

    private static RetirementDatesAges GetRetirementDatesAges()
    {
        return new RetirementDatesAges(new RetirementDatesAgesDto
        {
            EarliestRetirementAge = 50,
            NormalMinimumPensionAge = 55,
            NormalRetirementAge = 62,
            EarliestRetirementDate = new DateTimeOffset(DateTime.Parse("2018-06-30")),
            NormalMinimumPensionDate = new DateTimeOffset(DateTime.Parse("2023-06-30")),
            NormalRetirementDate = new DateTimeOffset(DateTime.Parse("2030-06-30")),
            TargetRetirementDate = new DateTimeOffset(DateTime.Parse("2030-06-30"))
        });
    }
}