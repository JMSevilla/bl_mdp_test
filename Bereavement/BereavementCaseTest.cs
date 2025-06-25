using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.BereavementJourneys;
using WTW.MdpService.Infrastructure.CasesApi;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Bereavement;

public class BereavementCaseTest
{
    private readonly Mock<ICasesClient> _caseClientMock;
    private readonly Mock<ILogger<BereavementCase>> _loggerMock;

    public BereavementCaseTest()
    {
        _caseClientMock = new Mock<ICasesClient>();
        _loggerMock = new Mock<ILogger<BereavementCase>>();
    }

    public async Task CanCaseBereavementCaseCreate()
    {
        _caseClientMock
            .Setup(x => x.CreateForNonMember(It.IsAny<CreateCaseRequest>()))
            .ReturnsAsync(new CreateCaseResponse { CaseNumber = "123456" });
        _caseClientMock
            .Setup(x => x.Exists(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new CaseExistsResponse { CaseExists = true });
        var sut = new BereavementCase(_caseClientMock.Object, _loggerMock.Object);

        var result = await CreateCase(sut);

        result.IsRight.Should().BeTrue();
        result.Right().Should().Be("123456");

        _caseClientMock.Verify(x => x.CreateForNonMember(new CreateCaseRequest
        {
            BusinessGroup = "RBS",
            CaseCode = "NDD9",
            BatchSource = "MDP",
            BatchDescription = "Death notification case created by an online application",
            Narrative = "",
            Notes = "Case created by an online bereavement application",
            StickyNotes = $"The deceased member details are: Name: testName, Surname: testSurname, DOB:" +
                $" {DateTime.Now.AddYears(-44):dd/MM/yyyy}, Date of Death: {DateTime.Now.AddDays(-4):dd/MM/yyyy}" + 
                $", Member Reference Number: test",
        }), Times.Once);
    }

    public async Task ReturnsErrorWhenFailsToCreateCase()
    {
        _caseClientMock
            .Setup(x => x.CreateForNonMember(It.IsAny<CreateCaseRequest>()))
            .ReturnsAsync(new CreateCaseError { Message = "test reason." });

        var sut = new BereavementCase(_caseClientMock.Object, _loggerMock.Object);

        var result = await CreateCase(sut);

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("test reason.");
    }

    public async Task ReturnsThrowsErrorWhenCreateCaseCaseApiThrows()
    {
        _caseClientMock
            .Setup(x => x.CreateForNonMember(It.IsAny<CreateCaseRequest>()))
            .Throws<HttpRequestException>();

        var sut = new BereavementCase(_caseClientMock.Object, _loggerMock.Object);

        var act = async () => await CreateCase(sut);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    public async Task ReturnsThrowsErrorWhenCaseExistsCaseApiThrows()
    {
        _caseClientMock
               .Setup(x => x.CreateForNonMember(It.IsAny<CreateCaseRequest>()))
               .ReturnsAsync(new CreateCaseResponse { CaseNumber = "123456" });
        _caseClientMock
            .Setup(x => x.Exists(It.IsAny<string>(), It.IsAny<string>()))
            .Throws<HttpRequestException>();

        var sut = new BereavementCase(_caseClientMock.Object, _loggerMock.Object);

        var act = async () => await CreateCase(sut);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    private static Task<LanguageExt.Either<LanguageExt.Common.Error, string>> CreateCase(BereavementCase sut)
    {
        return sut.Create("RBS", "testName", "testSurname", DateTime.Now.AddYears(-44), DateTime.Now.AddDays(-4), new List<string> { "test" });
    }
}