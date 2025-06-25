using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Infrastructure.CasesApi;
using WTW.MdpService.Journeys.Submit.Services;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Journeys.Submit.Services;

public class CaseServiceTest
{
    private readonly Mock<ICaseRequestFactory> _caseApiRequestServiceMock;
    private readonly Mock<ICasesClient> _caseClientMock;
    private readonly Mock<ILogger<CaseService>> _loggerMock;
    private readonly CaseService _sut;

    public CaseServiceTest()
    {
        _caseApiRequestServiceMock = new Mock<ICaseRequestFactory>();
        _caseClientMock = new Mock<ICasesClient>();
        _loggerMock = new Mock<ILogger<CaseService>>();
        _sut = new CaseService(_caseClientMock.Object, _loggerMock.Object);
    }

    public async Task CreateRetirementCase()
    {
        _caseClientMock
            .Setup(x => x.CreateForMember(It.IsAny<CreateCaseRequest>()))
            .ReturnsAsync(new CreateCaseResponse { CaseNumber = "123456" });

        _caseClientMock
            .Setup(x => x.Exists(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new CaseExistsResponse { CaseExists = true });

        var result = await _sut.Create(GetCreateCaseRequest());

        result.Right().Should().Be("123456");
        _caseClientMock.Verify(x => x.CreateForMember(It.IsAny<CreateCaseRequest>()), Times.Once);
        _caseClientMock.Verify(x => x.Exists(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    public async Task ReturnsError_WhenFailsToCreateCase()
    {
        _caseClientMock
            .Setup(x => x.CreateForMember(It.IsAny<CreateCaseRequest>()))
            .ReturnsAsync(new CreateCaseError { Message = "test reason." });

        var result = await _sut.Create(GetCreateCaseRequest());

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("test reason.");
    }

    public async Task ThrowsError_WhenCreateCaseThrows()
    {
        _caseClientMock
            .Setup(x => x.CreateForMember(It.IsAny<CreateCaseRequest>()))
            .Throws<HttpRequestException>();

        var act = async () => await _sut.Create(GetCreateCaseRequest());

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    public async Task ThrowsError_WhenCaseExistsThrows()
    {
        _caseClientMock
            .Setup(x => x.CreateForMember(It.IsAny<CreateCaseRequest>()))
            .ReturnsAsync(new CreateCaseResponse { CaseNumber = "123456" });
        _caseClientMock
            .Setup(x => x.Exists(It.IsAny<string>(), It.IsAny<string>()))
            .Throws<HttpRequestException>();

        var act = async () => await _sut.Create(GetCreateCaseRequest());

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    private static CreateCaseRequest GetCreateCaseRequest() => new CreateCaseRequest
    {
        BusinessGroup = "RBS",
        ReferenceNumber = "1111122",
        CaseCode = "RTQ9",
        BatchSource = "MDP",
        BatchDescription = "Case created by an online application",
        Narrative = $"Assure Calc fatal at 03/02/2024",
        Notes = $"Assure Calc fatal at 03/02/2024",
        StickyNotes = $"Case created by an online application"
    };
}