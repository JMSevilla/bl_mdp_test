using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using MessageBird.Objects.Voice;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.DcRetirement.Services;
using WTW.MdpService.Domain.Common.Journeys;
using WTW.MdpService.Domain.Mdp.Calculations;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.MdpService.Infrastructure.MdpDb;
using WTW.MdpService.Journeys.Submit.Services;
using WTW.MdpService.Journeys.Submit.Services.Dto;
using WTW.MdpService.Test.Domain.Mdp.Calculations;
using WTW.MdpService.Test.Domain.Members;
using WTW.TestCommon.FixieConfig;
using WTW.Web.LanguageExt;
using WTW.Web.Serialization;

namespace WTW.MdpService.Test.Journeys.Submit.Services;

public class GenericJourneyServiceTest
{
    private readonly Mock<IJourneysRepository> _journeysRepositoryMock;
    private readonly Mock<IMdpUnitOfWork> _mdpUnitOfWorkMock;
    private readonly Mock<IJsonConversionService> _jsonConversionServiceMock;
    private readonly Mock<ICalculationsParser> _calculationsParserMock;
    private readonly Mock<IRetirementService> _retirementServiceMock;
    private readonly Mock<ILogger<GenericJourneyService>> _loggerMock;
    private readonly GenericJourneyService _sut;

    public GenericJourneyServiceTest()
    {
        _journeysRepositoryMock = new Mock<IJourneysRepository>();
        _mdpUnitOfWorkMock = new Mock<IMdpUnitOfWork>();
        _jsonConversionServiceMock = new Mock<IJsonConversionService>();
        _calculationsParserMock = new Mock<ICalculationsParser>();
        _retirementServiceMock = new Mock<IRetirementService>();
        _loggerMock = new Mock<ILogger<GenericJourneyService>>();
        _sut = new GenericJourneyService(
            _journeysRepositoryMock.Object,
            _mdpUnitOfWorkMock.Object,
            _jsonConversionServiceMock.Object,
            _calculationsParserMock.Object,
            _retirementServiceMock.Object,
            _loggerMock.Object);
    }

    public async Task SubmitsJourney()
    {
        var journey = new GenericJourney("RBS", "1234567", "transfer", "step1", "step2", true, "Started", DateTimeOffset.UtcNow);
        _journeysRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(journey);

        await _sut.SetStatusSubmitted(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>());

        journey.Status.Should().Be("Submitted");
        _mdpUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
    }

    public async Task SetStatusSubmittedThrowsIfJourneysDoesNotExist()
    {
        _journeysRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<GenericJourney>.None);

        var action = async () => await _sut.SetStatusSubmitted(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>());

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    public async Task SavesSubmissionDetailsToGenericData()
    {
        var journey = new GenericJourney("RBS", "1234567", "transfer", "step1", "step2", true, "Started", DateTimeOffset.UtcNow);
        _journeysRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(journey);
        var genericDataJsonString = "{\"caseNumber\": \"1111AAA\",\"summaryPdfEdmsImageId\":111111,\"submissionDate\": \"2024-10-01T13:52:00.5022609+00:00\"}";
        _jsonConversionServiceMock
           .Setup(x => x.Serialize(It.IsAny<object>()))
           .Returns(genericDataJsonString);

        await _sut.SaveSubmissionDetailsToGenericData(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), ("1234567", 123456));

        journey.JourneyBranches[0].JourneySteps[0].JourneyGenericDataList[0].FormKey.Should().Be("JourneySubmissionDetails");
        journey.JourneyBranches[0].JourneySteps[0].JourneyGenericDataList[0].GenericDataJson.Should().Be(genericDataJsonString);
        _mdpUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
    }

    public async Task SaveCaseNumberToGenericDataThrowsIfJourneysDoesNotExist()
    {
        _journeysRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<GenericJourney>.None);

        var action = async () => await _sut.SaveSubmissionDetailsToGenericData(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), ("1234567", 123456));

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    public async Task GetCaseNumberFromGenericData()
    {
        var journey = new GenericJourney("RBS", "1111111", "transfer", "step1", "step2", true, "Started", DateTimeOffset.UtcNow.AddMinutes(-1));
        journey.TrySubmitStep("step2", "step3", DateTimeOffset.UtcNow);
        journey.GetFirstStep().UpdateGenericData("JourneySubmissionDetails", "{\"caseNumber\":\"1234567899876\"}");
        journey.SubmitJourney(DateTimeOffset.UtcNow);

        _journeysRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), "transfer"))
            .ReturnsAsync(journey);

        _jsonConversionServiceMock
           .Setup(x => x.Deserialize<SubmissionDetailsDto>(It.IsAny<string>()))
           .Returns(new SubmissionDetailsDto { CaseNumber = "1234567899876" });

        var caseNumberOrError = await _sut.GetSubmissionDetailsFromGenericData(It.IsAny<string>(), It.IsAny<string>(), "transfer");

        caseNumberOrError.IsRight.Should().BeTrue();
        caseNumberOrError.Right().CaseNumber.Should().Be("1234567899876");
    }

    [Input(true, "Generic data with key JourneySubmissionDetails not found in journey with type transfer.")]
    [Input(false, "Generic Journey with type \"transfer\" is not found.")]
    public async Task GetCaseNumberFromGenericDataReturnsErrorIfGenericDataDoesNotExist(bool journeyExists, string expectedMessage)
    {
        var journey = new GenericJourney("RBS", "1111111", "transfer", "step1", "step2", true, "Started", DateTimeOffset.UtcNow.AddMinutes(-1));
        journey.TrySubmitStep("step2", "step3", DateTimeOffset.UtcNow);
        journey.SubmitJourney(DateTimeOffset.UtcNow);

        _journeysRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), "transfer"))
            .ReturnsAsync(journeyExists ? journey : Option<GenericJourney>.None);

        var caseNumberOrError = await _sut.GetSubmissionDetailsFromGenericData(It.IsAny<string>(), It.IsAny<string>(), "transfer");

        caseNumberOrError.IsLeft.Should().BeTrue();
        caseNumberOrError.Left().Message.Should().Be(expectedMessage);
    }

    public async Task CreatesJourneyWithoutExpiryDates()
    {
        var result = await _sut.CreateJourney("RBS", "1111111", "random", "step1", "step2", false, "Started");

        result.BusinessGroup.Should().Be("RBS");
        result.ReferenceNumber.Should().Be("1111111");
        result.Type.Should().Be("random");
        result.IsMarkedForRemoval.Should().BeFalse();
        result.Status.Should().Be("Started");
        result.StartDate.Date.Should().Be(DateTime.Now.Date);
        result.ExpirationDate.Should().BeNull();
    }

    public async Task CreatesDbCoreJourneyWithCalculatedExpiryDates()
    {
        var result = await _sut.CreateJourney("RBS", "1111111", "dbcoreretirementapplication", "step1", "step2", false, "Started");

        result.BusinessGroup.Should().Be("RBS");
        result.ReferenceNumber.Should().Be("1111111");
        result.Type.Should().Be("dbcoreretirementapplication");
        result.IsMarkedForRemoval.Should().BeFalse();
        result.Status.Should().Be("Started");
        result.StartDate.Date.Should().Be(DateTime.Now.Date);
        result.ExpirationDate.Value.Date.Should().Be(DateTime.Now.AddDays(90).Date);
    }

    public async Task CreatesDcRetirementJourneyWithCalculatedExpiryDates_WhenDcExploreOptionsDoesNotExist()
    {
        var result = await _sut.CreateJourney("RBS", "1111111", "dcretirementapplication", "step1", "step2", false, "Started");

        result.ExpirationDate.Value.Date.Should().Be(DateTime.Now.AddDays(90).Date);
    }

    public async Task CreatesDcRetirementJourneyWithCalculatedExpiryDates_WhenGenericDataDoesNotExist()
    {
        var journey = new GenericJourney(It.IsAny<string>(), It.IsAny<string>(), "dcexploreoptions", "DC_explore_options",
            "DC_explore_options-1", false, "started", DateTimeOffset.UtcNow);
        _journeysRepositoryMock
           .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(journey);

        var result = await _sut.CreateJourney("RBS", "1111111", "dcretirementapplication", "step1", "step2", false, "Started");


        result.ExpirationDate.Value.Date.Should().Be(DateTime.Now.AddDays(90).Date);
    }

    [Input(111)]
    [Input(112)]
    [Input(117)]
    [Input(125)]
    [Input(136)]
    [Input(147)]
    [Input(158)]
    public async Task CreatesDcRetirementJourneyWithCalculatedExpiryDates_WhenRetirementDate111AndMoreDaysAhead(int retiresInDays)
    {
        var journey = new GenericJourney(It.IsAny<string>(), It.IsAny<string>(), "dcexploreoptions", "DC_explore_options",
            "DC_explore_options-1", false, "started", DateTimeOffset.UtcNow);
        journey.GetFirstStep().AppendGenericDataList(new List<JourneyGenericData> {
            new JourneyGenericData("{\"retirementDate\":\"2024-09-01T00:00:00.000Z\",\"retirementAge\":60}", "DC_options_filter_retirement_date") });
        _jsonConversionServiceMock
            .Setup(x => x.Deserialize<DcSubmissionRetirementDateDto>(It.IsAny<string>()))
            .Returns(new DcSubmissionRetirementDateDto { RetirementDate = DateTimeOffset.UtcNow.AddDays(retiresInDays) });
        _journeysRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(journey);

        var result = await _sut.CreateJourney("RBS", "1111111", "dcretirementapplication", "step1", "step2", false, "Started");

        result.ExpirationDate.Value.Date.Should().Be(DateTime.Now.AddDays(90).Date);
    }

    [Input(110)]
    [Input(109)]
    [Input(101)]
    [Input(99)]
    public async Task CreatesDcRetirementJourneyWithCalculatedExpiryDates_WhenRetirementDateUpTo110DaysAhead(int retiresInDays)
    {
        var journey = new GenericJourney(It.IsAny<string>(), It.IsAny<string>(), "dcexploreoptions", "DC_explore_options",
            "DC_explore_options-1", false, "started", DateTimeOffset.UtcNow);
        journey.GetFirstStep().AppendGenericDataList(new List<JourneyGenericData> {
            new JourneyGenericData("{\"retirementDate\":\"2024-09-01T00:00:00.000Z\",\"retirementAge\":60}", "DC_options_filter_retirement_date") });
        _jsonConversionServiceMock
            .Setup(x => x.Deserialize<DcSubmissionRetirementDateDto>(It.IsAny<string>()))
            .Returns(new DcSubmissionRetirementDateDto { RetirementDate = DateTimeOffset.UtcNow.AddDays(retiresInDays) });
        _journeysRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(journey);

        var result = await _sut.CreateJourney("RBS", "1111111", "dcretirementapplication", "step1", "step2", false, "Started");


        result.ExpirationDate.Value.Date.Should().Be(DateTimeOffset.UtcNow.AddDays(retiresInDays - 21).Date);
    }

    [Input(true)]
    [Input(false)]
    public async Task ExistsJourneyReturnsExpectedResults(bool journeyExists)
    {
        var journey = new GenericJourney("RBS", "1111111", "transfer", "step1", "step2", true, "Started", DateTimeOffset.UtcNow.AddMinutes(-1));
        _journeysRepositoryMock
             .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
             .ReturnsAsync(journeyExists ? journey : Option<GenericJourney>.None);

        var result = await _sut.ExistsJourney(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>());

        result.Should().Be(journeyExists);
    }

    public async Task UpdatesDcRetirementSelectedJourneyQuoteDetails_WhenDcRetirementApplicationJourneyExists()
    {
        var calculation = new CalculationBuilder().BuildV2();
        var journey = new GenericJourney("RBS", "1111111", "dcretirementapplication", "DC_options_timetable", "step2", true, "Started", DateTimeOffset.UtcNow.AddMinutes(-1));
        journey.GetFirstStep().UpdateGenericData("SelectedQuoteDetails", "{}");
        _journeysRepositoryMock
             .Setup(x => x.FindUnexpired(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
             .ReturnsAsync(journey);

        _calculationsParserMock
            .Setup(x => x.GetRetirementV2(It.IsAny<string>()))
            .Returns(new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options())));
        _jsonConversionServiceMock
            .Setup(x => x.Deserialize<SelectedQuoteDetailsDto>(It.IsAny<string>()))
            .Returns(new SelectedQuoteDetailsDto { SelectedQuoteFullName = "test-quote-name" });
        _retirementServiceMock
            .Setup(x => x.GetSelectedQuoteDetails(It.IsAny<string>(), It.IsAny<RetirementV2>()))
            .Returns(new Dictionary<string, object>());
        _jsonConversionServiceMock
           .Setup(x => x.Serialize(It.IsAny<Dictionary<string, object>>()))
           .Returns("{\"selectedQuoteFullName\":\"test-quote-name\", \"testProp\":123}");

        await _sut.UpdateDcRetirementSelectedJourneyQuoteDetails(calculation);

        _mdpUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
        journey.GetFirstStep().GetGenericDataByKey("SelectedQuoteDetails").Value().GenericDataJson
            .Should().Be("{\"selectedQuoteFullName\":\"test-quote-name\", \"testProp\":123}");
    }

    public async Task UpdateDcRetirementSelectedJourneyQuoteDetailsReturns_WhenNoDcRetirementApplicationJourneyExists()
    {
        _journeysRepositoryMock
             .Setup(x => x.FindUnexpired(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
             .ReturnsAsync(Option<GenericJourney>.None);

        await _sut.UpdateDcRetirementSelectedJourneyQuoteDetails(new CalculationBuilder().BuildV2());

        _mdpUnitOfWorkMock.Verify(x => x.Commit(), Times.Never);      
    }
}