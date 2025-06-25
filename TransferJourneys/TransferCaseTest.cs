using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Infrastructure.CasesApi;
using WTW.MdpService.TransferJourneys;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.TransferJourneys;

public class TransferCaseTest
{
    private readonly Mock<ICasesClient> _caseClientMock;
    private readonly Mock<ILogger<TransferCase>> _loggerMock;

    public TransferCaseTest()
    {
        _caseClientMock = new Mock<ICasesClient>();
        _loggerMock = new Mock<ILogger<TransferCase>>();
    }

    public async Task CanCaseTransferCaseCreate()
    {
        _caseClientMock
            .Setup(x => x.CreateForMember(It.IsAny<CreateCaseRequest>()))
            .ReturnsAsync(new CreateCaseResponse { CaseNumber = "123456" });
        _caseClientMock
            .Setup(x => x.Exists(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new CaseExistsResponse { CaseExists = true });
        var sut = new TransferCase(_caseClientMock.Object, _loggerMock.Object);

        var result = await sut.Create("RBS", "1111122");

        result.IsRight.Should().BeTrue();
        result.Right().Should().Be("123456");
    }

    public async Task ReturnsErrorWhenFailsToCreateCase()
    {
        _caseClientMock
            .Setup(x => x.CreateForMember(It.IsAny<CreateCaseRequest>()))
            .ReturnsAsync(new CreateCaseError { Message = "test reason." });
      
        var sut = new TransferCase(_caseClientMock.Object, _loggerMock.Object);

        var result = await sut.Create("RBS", "1111122");

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("test reason.");
    }

    public async Task ReturnsThrowsErrorWhenCreateCaseCaseApiThrows()
    {
        _caseClientMock
            .Setup(x => x.CreateForMember(It.IsAny<CreateCaseRequest>()))
            .Throws<HttpRequestException>();

        var sut = new TransferCase(_caseClientMock.Object, _loggerMock.Object);

        var act = async () => await sut.Create("RBS", "1111122");

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    public async Task ReturnsThrowsErrorWhenCaseExistsCaseApiThrows()
    {
        _caseClientMock
            .Setup(x => x.CreateForMember(It.IsAny<CreateCaseRequest>()))
            .ReturnsAsync(new CreateCaseResponse { CaseNumber = "123456" });
        _caseClientMock
            .Setup(x => x.Exists(It.IsAny<string>(), It.IsAny<string>()))
            .Throws<HttpRequestException>();

        var sut = new TransferCase(_caseClientMock.Object, _loggerMock.Object);

        var act = async () => await sut.Create("RBS", "1111122");

        await act.Should().ThrowAsync<HttpRequestException>();
    }
}