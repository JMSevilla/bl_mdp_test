using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.EngagementEvents;
using WTW.MdpService.Infrastructure.EngagementEvents;
using WTW.MdpService.Infrastructure.MemberDb;
using WTW.MdpService.Infrastructure.MemberWebInteractionService;
using WTW.MdpService.Test.Domain.Members;
using WTW.TestCommon.Helpers;

namespace WTW.MdpService.Test.EngagementEventsTests;

public class EngagementEventsControllerTest
{

    private readonly Mock<IMemberRepository> _memberRepositoryMock;
    private readonly Mock<IMemberWebInteractionServiceClient> _memberWebInteractionServiceClientMock;
    private readonly Mock<ILogger<EngagementEventsController>> _loggerMock;
    private readonly EngagementEventsController _sut;

    public EngagementEventsControllerTest()
    {
        _memberRepositoryMock = new Mock<IMemberRepository>();
        _memberWebInteractionServiceClientMock = new Mock<IMemberWebInteractionServiceClient>();
        _loggerMock = new Mock<ILogger<EngagementEventsController>>();

        _sut = new EngagementEventsController(_loggerMock.Object,
                                             _memberRepositoryMock.Object,
                                             _memberWebInteractionServiceClientMock.Object);

        _sut.SetupControllerContext();
    }

    public async Task EngagementEventsReturnsOkObjectResult()
    {
        var engagementEventsResponse = new MemberWebInteractionEngagementEventsResponse
        {
            Events = new List<MemberEngagementEvent>()
            {
                new MemberEngagementEvent()
            }
        };

        var member = new MemberBuilder().Build();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.Some(member));

        _memberWebInteractionServiceClientMock
            .Setup(x => x.GetEngagementEvents(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<MemberWebInteractionEngagementEventsResponse>.Some(engagementEventsResponse));

        var result = await _sut.EngagementEvents();

        var expectedMessage = $"Engagement events returned successfully for TestBusinessGroup TestReferenceNumber";
        _loggerMock.VerifyLogging(expectedMessage, LogLevel.Information, Times.Once());
        result.Should().BeOfType<OkObjectResult>();
    }


    public async Task EngagementEventsReturnsNoContent()
    {
        var engagementEventsResponse = new MemberWebInteractionEngagementEventsResponse
        {
            Events = new List<MemberEngagementEvent>()
        };

        var member = new MemberBuilder().Build();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.Some(member));

        _memberWebInteractionServiceClientMock
            .Setup(x => x.GetEngagementEvents(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<MemberWebInteractionEngagementEventsResponse>.Some(engagementEventsResponse));

        var result = await _sut.EngagementEvents();

        var expectedMessage = $"No engagement events found for TestBusinessGroup TestReferenceNumber";
        _loggerMock.VerifyLogging(expectedMessage, LogLevel.Information, Times.Once());
        result.Should().BeOfType<NoContentResult>();
    }


    public async Task EngagementEventsReturnsNotFound()
    {
        var engagementEventsResponse = new MemberWebInteractionEngagementEventsResponse
        {
        };

        _memberWebInteractionServiceClientMock
            .Setup(x => x.GetEngagementEvents(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<MemberWebInteractionEngagementEventsResponse>.Some(engagementEventsResponse));

        var result = await _sut.EngagementEvents();

        var expectedMessage = $"Member not found for reference number TestReferenceNumber and business group TestBusinessGroup";
        _loggerMock.VerifyLogging(expectedMessage, LogLevel.Error, Times.Once());
        result.Should().BeOfType<NotFoundObjectResult>();
    }


    public async Task EngagementEventsReturnsBadRequestResult()
    {
        var member = new MemberBuilder().Build();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.Some(member));

        var result = await _sut.EngagementEvents();

        var expectedMessage = $"Failed to retrieve engagement events for TestBusinessGroup TestReferenceNumber";
        _loggerMock.VerifyLogging(expectedMessage, LogLevel.Error, Times.Once());
        result.Should().BeOfType<BadRequestObjectResult>();
    }
}