using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Infrastructure.TelephoneNoteService;
using WTW.MdpService.TelephoneNote;
using WTW.Web;

namespace WTW.MdpService.Test.TelephoneNote;

public class TelephoneNoteControllerTest
{
    private readonly Mock<ITelephoneNoteServiceClient> _telephoneNoteServiceClientMock;
    private readonly Mock<ILogger<TelephoneNoteController>> _loggerMock;
    private readonly TelephoneNoteController _sut;

    public TelephoneNoteControllerTest()
    {
        _telephoneNoteServiceClientMock = new Mock<ITelephoneNoteServiceClient>();
        _loggerMock = new Mock<ILogger<TelephoneNoteController>>();
        _sut = new TelephoneNoteController(
            _telephoneNoteServiceClientMock.Object,
            _loggerMock.Object);

        _sut.SetupControllerContext();
    }

    public async Task GetIntentContext_ReturnsOk_WhenClientReturnsIntentContext()
    {
        // Arrange
        var expectedResponse = new IntentContextResponse
        {
            Intent = "caseUpdate",
            Ttl = "1730716603",
            SessionId = "6a29a4c2324b48308e813ec1421ef8af"
        };

        _telephoneNoteServiceClientMock
            .Setup(x => x.GetIntentContext("TestBusinessGroup", "TestReferenceNumber", MdpConstants.AppPlatform))
            .ReturnsAsync(Option<IntentContextResponse>.Some(expectedResponse));

        // Act
        var result = await _sut.GetIntentContext();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult.Value as IntentContextResponse;
        
        response.Should().NotBeNull();
        response.Intent.Should().Be("caseUpdate");
        response.Ttl.Should().Be("1730716603");
        response.SessionId.Should().Be("6a29a4c2324b48308e813ec1421ef8af");

        _telephoneNoteServiceClientMock.Verify(
            x => x.GetIntentContext("TestBusinessGroup", "TestReferenceNumber", MdpConstants.AppPlatform),
            Times.Once);
    }

    public async Task GetIntentContext_ReturnsNoContent_WhenClientReturnsNone()
    {
        // Arrange
        _telephoneNoteServiceClientMock
            .Setup(x => x.GetIntentContext("TestBusinessGroup", "TestReferenceNumber", MdpConstants.AppPlatform))
            .ReturnsAsync(Option<IntentContextResponse>.None);

        // Act
        var result = await _sut.GetIntentContext();

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _telephoneNoteServiceClientMock.Verify(
            x => x.GetIntentContext("TestBusinessGroup", "TestReferenceNumber", MdpConstants.AppPlatform),
            Times.Once);
    }

    public async Task GetIntentContext_LogsInformation_WhenCalled()
    {
        // Arrange
        _telephoneNoteServiceClientMock
            .Setup(x => x.GetIntentContext("TestBusinessGroup", "TestReferenceNumber", MdpConstants.AppPlatform))
            .ReturnsAsync(Option<IntentContextResponse>.None);

        // Act
        await _sut.GetIntentContext();

        // Assert
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Getting intent context for member TestReferenceNumber in business group TestBusinessGroup")),
                It.IsAny<System.Exception>(),
                It.IsAny<System.Func<It.IsAnyType, System.Exception, string>>()),
            Times.Once);
    }

    public async Task GetIntentContext_UsesCorrectUserClaimsFromHttpContext()
    {
        // Arrange
        _telephoneNoteServiceClientMock
            .Setup(x => x.GetIntentContext(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<IntentContextResponse>.None);

        // Act
        await _sut.GetIntentContext();

        // Assert
        _telephoneNoteServiceClientMock.Verify(
            x => x.GetIntentContext("TestBusinessGroup", "TestReferenceNumber", MdpConstants.AppPlatform),
            Times.Once);
    }

    public async Task GetIntentContext_ReturnsOkWithPartialData_WhenIntentIsNull()
    {
        // Arrange
        var responseWithNullIntent = new IntentContextResponse
        {
            Intent = null,
            Ttl = "1730716603",
            SessionId = "6a29a4c2324b48308e813ec1421ef8af"
        };

        _telephoneNoteServiceClientMock
            .Setup(x => x.GetIntentContext("TestBusinessGroup", "TestReferenceNumber", MdpConstants.AppPlatform))
            .ReturnsAsync(Option<IntentContextResponse>.Some(responseWithNullIntent));

        // Act
        var result = await _sut.GetIntentContext();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult.Value as IntentContextResponse;
        
        response.Should().NotBeNull();
        response.Intent.Should().BeNull();
        response.Ttl.Should().Be("1730716603");
        response.SessionId.Should().Be("6a29a4c2324b48308e813ec1421ef8af");
    }

    public async Task GetIntentContext_ReturnsOkWithEmptyResponse_WhenAllFieldsAreNull()
    {
        // Arrange
        var emptyResponse = new IntentContextResponse
        {
            Intent = null,
            Ttl = null,
            SessionId = null
        };

        _telephoneNoteServiceClientMock
            .Setup(x => x.GetIntentContext("TestBusinessGroup", "TestReferenceNumber", MdpConstants.AppPlatform))
            .ReturnsAsync(Option<IntentContextResponse>.Some(emptyResponse));

        // Act
        var result = await _sut.GetIntentContext();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult.Value as IntentContextResponse;
        
        response.Should().NotBeNull();
        response.Intent.Should().BeNull();
        response.Ttl.Should().BeNull();
        response.SessionId.Should().BeNull();
    }
} 