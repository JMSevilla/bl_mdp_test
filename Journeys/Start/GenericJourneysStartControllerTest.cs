using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Domain.Common.Journeys;
using WTW.MdpService.Domain.Mdp.Calculations;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.MdpService.Infrastructure.MdpDb;
using WTW.MdpService.Infrastructure.MemberDb;
using WTW.MdpService.Journeys.Start;
using WTW.MdpService.Journeys.Submit.Services;
using WTW.MdpService.Test.Domain.Mdp.Calculations;
using WTW.MdpService.Test.Domain.Members;
using WTW.TestCommon.Helpers;
using WTW.Web.LanguageExt;
using WTW.Web.Serialization;

namespace WTW.MdpService.Test.Journeys.Start;

public class GenericJourneysStartControllerTest
{
    private readonly Mock<IJourneysRepository> _journeysRepositoryMock;
    private readonly Mock<ICalculationsRepository> _calculationsRepositoryMock;
    private readonly Mock<IMdpUnitOfWork> _mdpUnitOfWorkMock;
    private readonly Mock<IJsonConversionService> _jsonConversionServiceMock;
    private readonly Mock<ICalculationsParser> _calculationsParserMock;
    private readonly Mock<ILogger<GenericJourneysStartController>> _loggerMock;
    private readonly Mock<IRetirementService> _retirementServiceMock;
    private readonly Mock<IGenericJourneyService> _genericJourneyServiceMock;
    private readonly Mock<IMemberRepository> _memberRepositoryMock;
    private readonly GenericJourneysStartController _sut;

    public GenericJourneysStartControllerTest()
    {
        _journeysRepositoryMock = new Mock<IJourneysRepository>();
        _calculationsRepositoryMock = new Mock<ICalculationsRepository>();
        _mdpUnitOfWorkMock = new Mock<IMdpUnitOfWork>();
        _jsonConversionServiceMock = new Mock<IJsonConversionService>();
        _calculationsParserMock = new Mock<ICalculationsParser>();
        _loggerMock = new Mock<ILogger<GenericJourneysStartController>>();
        _retirementServiceMock = new Mock<IRetirementService>();
        _genericJourneyServiceMock = new Mock<IGenericJourneyService>();
        _memberRepositoryMock = new Mock<IMemberRepository>();

        _sut = new GenericJourneysStartController(
            _journeysRepositoryMock.Object,
            _calculationsRepositoryMock.Object,
            _mdpUnitOfWorkMock.Object,
            _jsonConversionServiceMock.Object,
            _calculationsParserMock.Object,
            _loggerMock.Object,
            _retirementServiceMock.Object,
            _genericJourneyServiceMock.Object,
            _memberRepositoryMock.Object);

        _sut.SetupControllerContext();
    }

    public async Task Returns400WhenJourneyAndCalculationDoNotExists()
    {
        _journeysRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<GenericJourney>.None);

        var result = await _sut.StartDcJourney("dc-journey", new StartDcJourneyRequest
        {
            CurrentPageKey = "test1",
            NextPageKey = "test2",
            JourneyStatus = "started",
            SelectedQuoteName = "test"
        });

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    public async Task Returns204WhenJourneyExists()
    {
        _journeysRepositoryMock
            .Setup(x => x.FindUnexpired(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new GenericJourney("RBS", "1111122", "dc-journey", "page1", "page2", false, "started", DateTimeOffset.UtcNow));

        var result = await _sut.StartDcJourney("dc-journey", new StartDcJourneyRequest
        {
            CurrentPageKey = "test1",
            NextPageKey = "test2",
            JourneyStatus = "started",
            SelectedQuoteName = "test"
        });

        result.Should().BeOfType<NoContentResult>();
    }

    public async Task Returns204WhenCalculationExists()
    {
        var retirement = new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options()));
        var mocDcJourneyData = new GenericJourney("TestBusinessGroup", "TestReferenceNumber", "dc-journey", "test1", "test2", false, "started", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Calculation("1111122", "RBS", "{}", "{}", "{}", DateTime.UtcNow, DateTimeOffset.UtcNow, true));

        _calculationsParserMock
            .Setup(x => x.GetRetirementV2(It.IsAny<string>()))
            .Returns(retirement);

        _retirementServiceMock
           .Setup(x => x.GetSelectedQuoteDetails("fullPension", retirement))
           .Returns(new Dictionary<string, object>());

        _genericJourneyServiceMock.Setup(x => x.CreateJourney(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<string>()))
            .ReturnsAsync(mocDcJourneyData);

        var result = await _sut.StartDcJourney("dc-journey", new StartDcJourneyRequest
        {
            CurrentPageKey = "test1",
            NextPageKey = "test2",
            JourneyStatus = "started",
            SelectedQuoteName = "fullPension"
        });

        result.Should().BeOfType<NoContentResult>();
        _journeysRepositoryMock.Verify(x => x.Create(It.IsAny<GenericJourney>()), Times.Once);
        _mdpUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
    }

    public async Task RemovesJourneyWhenDcExploreOptionsDataExists()
    {
        var mockDcExploreOptionsData = new GenericJourney("TestBusinessGroup", "TestReferenceNumber", "dcexploreoptions", "page1", "page2", false, "started", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var mocDcJourneyData = new GenericJourney("TestBusinessGroup", "TestReferenceNumber", "dc-journey", "test1", "test2", false, "started", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        _journeysRepositoryMock
            .Setup(x => x.Find("TestBusinessGroup", "TestReferenceNumber", "dcexploreoptions"))
            .ReturnsAsync(mockDcExploreOptionsData);

        _journeysRepositoryMock
           .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), "dc-journey"))
           .ReturnsAsync(new GenericJourney("RBS", "1111122", "dc-journey", "page1", "page2", false, "started", DateTimeOffset.UtcNow));

        var retirement = new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options()));

        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Calculation("TestReferenceNumber", "TestBusinessGroup", "{}", "{}", "{}", DateTime.UtcNow, DateTimeOffset.UtcNow, true));

        _calculationsParserMock
            .Setup(x => x.GetRetirementV2(It.IsAny<string>()))
            .Returns(retirement);

        _retirementServiceMock
            .Setup(x => x.GetSelectedQuoteDetails("fullPension", retirement))
            .Returns(new Dictionary<string, object>());

        _genericJourneyServiceMock.Setup(x => x.CreateJourney(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<string>()))
            .ReturnsAsync(mocDcJourneyData);

        var result = await _sut.StartDcJourney("dc-journey", new StartDcJourneyRequest
        {
            CurrentPageKey = "test1",
            NextPageKey = "test2",
            JourneyStatus = "started",
            SelectedQuoteName = "fullPension"
        });

        result.Should().BeOfType<NoContentResult>();
        _journeysRepositoryMock.Verify(x => x.Remove(It.IsAny<GenericJourney>()), Times.Exactly(2));
    }

    public async Task DoesNotRemoveJourneyWhenDcExploreOptionsDataDoesNotExist()
    {
        var retirement = new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options()));
        var mocDcJourneyData = new GenericJourney("TestBusinessGroup", "TestReferenceNumber", "dc-journey", "test1", "test2", false, "started", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Calculation("1111122", "RBS", "{}", "{}", "{}", DateTime.UtcNow, DateTimeOffset.UtcNow, true));

        _calculationsParserMock
            .Setup(x => x.GetRetirementV2(It.IsAny<string>()))
            .Returns(retirement);

        _retirementServiceMock
            .Setup(x => x.GetSelectedQuoteDetails("fullPension", retirement))
            .Returns(new Dictionary<string, object>());

        _genericJourneyServiceMock.Setup(x => x.CreateJourney(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<string>()))
            .ReturnsAsync(mocDcJourneyData);

        var result = await _sut.StartDcJourney("dc-journey", new StartDcJourneyRequest
        {
            CurrentPageKey = "test1",
            NextPageKey = "test2",
            JourneyStatus = "started",
            SelectedQuoteName = "fullPension"
        });

        result.Should().BeOfType<NoContentResult>();
        _journeysRepositoryMock.Verify(x => x.Remove(It.IsAny<GenericJourney>()), Times.Never);
    }

    public async Task AppendsDataToStepDcExploreOptionsDataWhenExists()
    {
        var mockDcExploreOptionsData = new GenericJourney("TestBusinessGroup", "TestReferenceNumber", "dcexploreoptions", "page1", "page2", false, "started", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var mocDcJourneyData = new GenericJourney("TestBusinessGroup", "TestReferenceNumber", "dc-journey", "dc-page1", "dc-page2", false, "started", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        _journeysRepositoryMock
            .Setup(x => x.Find("TestBusinessGroup", "TestReferenceNumber", "dcexploreoptions"))
            .ReturnsAsync(mockDcExploreOptionsData);

        var retirement = new RetirementV2Mapper().MapFromDto(JsonSerializer.Deserialize<RetirementDtoV2>(TestData.RetirementJsonV2, SerialiationBuilder.Options()));

        _calculationsRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Calculation("TestReferenceNumber", "TestBusinessGroup", "{}", "{}", "{}", DateTime.UtcNow, DateTimeOffset.UtcNow, true));

        _calculationsParserMock
            .Setup(x => x.GetRetirementV2(It.IsAny<string>()))
            .Returns(retirement);

        _retirementServiceMock
            .Setup(x => x.GetSelectedQuoteDetails("fullPension", retirement))
            .Returns(new Dictionary<string, object>());

        var inOrder = new MockSequence();
        _journeysRepositoryMock
            .InSequence(inOrder)
            .Setup(x => x.Create(It.IsAny<GenericJourney>()));

        _journeysRepositoryMock
            .InSequence(inOrder)
            .Setup(x => x.Remove(It.IsAny<GenericJourney>()));

        _genericJourneyServiceMock.Setup(x => x.CreateJourney(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<string>()))
            .ReturnsAsync(mocDcJourneyData);

        var result = await _sut.StartDcJourney("dc-journey", new StartDcJourneyRequest
        {
            CurrentPageKey = "dc-page1",
            NextPageKey = "dc-page2",
            JourneyStatus = "started",
            SelectedQuoteName = "fullPension"
        });
        result.Should().BeOfType<NoContentResult>();
        var step = mocDcJourneyData.GetStepByKey("dc-page1").Value();
        step.JourneyGenericDataList.Count.Should().Be(1);
    }

    public async Task WhenStartDbCoreJourneyIsCalledAndJourneyIsFound_ThenNoContentResultIsReturned()
    {
        var stubDBExploreOptionsData = new GenericJourney("TestBusinessGroup", "TestReferenceNumber", "dbexploreoptions", "page1", "page2", false, "started", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        _journeysRepositoryMock.Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(Option<GenericJourney>.Some(stubDBExploreOptionsData));

        var result = await _sut.StartDbCoreJourney(It.IsAny<string>(), new StartDbCoreJourneyRequest
        {
            CurrentPageKey = "dbcore-page1",
            NextPageKey = "dbcore-page2",
            RemoveOnLogin = false,
            JourneyStatus = "started",
            RetirementDate = DateTime.Now,
        });

        var expectedMessage = $"A journey already exists for user with referenceNumber: TestReferenceNumber in businessGroup: TestBusinessGroup";

        _loggerMock.VerifyLogging(expectedMessage, LogLevel.Warning, Times.Once());
        result.Should().BeOfType<NoContentResult>();
    }

    public async Task WhenStartDbCoreJourneyIsCalledAndJourneyIsNotFound_ThenNewJourneyIsCreatedAndNoContentResultIsReturned()
    {

        _memberRepositoryMock.Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(Option<Member>.Some(new MemberBuilder().Build()));
        _journeysRepositoryMock.Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(Option<GenericJourney>.None);

        var result = await _sut.StartDbCoreJourney(It.IsAny<string>(), new StartDbCoreJourneyRequest
        {
            CurrentPageKey = "dbcore-page1",
            NextPageKey = "dbcore-page2",
            RemoveOnLogin = false,
            JourneyStatus = "started",
            RetirementDate = DateTime.Now,
        });

        var expectedMessage = "Creating DB Core journey for user with referenceNumber: TestReferenceNumber in businessGroup: TestBusinessGroup";

        _loggerMock.VerifyLogging(expectedMessage, LogLevel.Information, Times.Once());

        _memberRepositoryMock.Verify(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _journeysRepositoryMock.Verify(x => x.Create(It.IsAny<GenericJourney>()), Times.Once);
        _mdpUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);

        result.Should().BeOfType<NoContentResult>();

    }

    public async Task WhenStartDbCoreJourneyIsCalledAndJourneyIsNotFoundButMemberIsNotFound_ThenExpextedLoggingOccursAndBadRequestResultIsReturned()
    {
        _memberRepositoryMock.Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(Option<Member>.None);
        _journeysRepositoryMock.Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(Option<GenericJourney>.None);

        var result = await _sut.StartDbCoreJourney(It.IsAny<string>(), new StartDbCoreJourneyRequest
        {
            CurrentPageKey = "dbcore-page1",
            NextPageKey = "dbcore-page2",
            RemoveOnLogin = false,
            JourneyStatus = "started",
            RetirementDate = DateTime.Now,
        });

        var expectedMessage = "Member not found for referenceNumber: TestReferenceNumber in businessGroup: TestBusinessGroup";

        _loggerMock.VerifyLogging(expectedMessage, LogLevel.Warning, Times.Once());

        _memberRepositoryMock.Verify(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _journeysRepositoryMock.Verify(x => x.Create(It.IsAny<GenericJourney>()), Times.Never);
        _mdpUnitOfWorkMock.Verify(x => x.Commit(), Times.Never);

        result.Should().BeOfType<BadRequestObjectResult>();

    }
}