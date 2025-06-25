using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Application;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.Infrastructure.MemberDb;
using WTW.MdpService.Test.Domain.Members;
using WTW.Web.Errors;

namespace WTW.MdpService.Test.Application;

public class ApplicationControllerTest
{
    private readonly Mock<IApplicationInitialization> _applicationInitializationMock;
    private readonly Mock<IMemberRepository> _memberRepositoryMock;
    private readonly Mock<ILogger<ApplicationController>> _loggerMock;
    private readonly ApplicationController _sut;

    public ApplicationControllerTest()
    {
        _applicationInitializationMock = new Mock<IApplicationInitialization>();
        _memberRepositoryMock = new Mock<IMemberRepository>();
        _loggerMock = new Mock<ILogger<ApplicationController>>();
        _sut = new ApplicationController(_applicationInitializationMock.Object, _memberRepositoryMock.Object,_loggerMock.Object);
        _sut.SetupControllerContext();
    }

    public async Task InitializeReturns204Code()
    {
        _memberRepositoryMock.Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new MemberBuilder().Build());

        var result = await _sut.Initialize();        
        
        result.Should().BeOfType<NoContentResult>();
        _applicationInitializationMock.Verify(x => x.SetUpTransfer(It.IsAny<Member>()), Times.Once);
        _applicationInitializationMock.Verify(x => x.RemoveGenericJourneys(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _applicationInitializationMock.Verify(x => x.UpdateGenericJourneysStatuses(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _applicationInitializationMock.Verify(x => x.ClearSessionCache(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _applicationInitializationMock.Verify(x => x.SetUpDcRetirement(It.IsAny<Member>()), Times.Once);
    }

    public async Task InitializeReturns404CodeAndCorrectErrorMessage()
    {
        _memberRepositoryMock.Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(Option<Member>.None);

        var result = await _sut.Initialize();

        result.Should().BeOfType<NotFoundObjectResult>();
        _applicationInitializationMock.Verify(x => x.SetUpTransfer(It.IsAny<Member>()), Times.Never);
        _applicationInitializationMock.Verify(x => x.RemoveGenericJourneys(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _applicationInitializationMock.Verify(x => x.ClearSessionCache(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _applicationInitializationMock.Verify(x => x.SetUpDcRetirement(It.IsAny<Member>()), Times.Never);

        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult.Value.Should().BeOfType<ApiError>();
        var errorResponse = ((NotFoundObjectResult)result).Value as ApiError;
        errorResponse.Errors[0].Message.Should().Be("Member for the given access token is not found.");
        errorResponse.Errors[0].Code.Should().BeNull();
    }
}