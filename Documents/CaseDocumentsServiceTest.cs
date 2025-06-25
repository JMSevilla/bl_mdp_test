using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Documents;
using WTW.MdpService.Domain.Common.Journeys;
using WTW.MdpService.Domain.Mdp;
using WTW.MdpService.Infrastructure.CasesApi;
using WTW.MdpService.Infrastructure.MdpDb;
using WTW.MdpService.Journeys.Submit.Services;
using WTW.MdpService.Journeys.Submit.Services.Dto;
using WTW.MdpService.Test.Domain.Mdp.Journeys;
using WTW.TestCommon.FixieConfig;
using WTW.Web;
using WTW.Web.LanguageExt;
using static LanguageExt.Prelude;

namespace WTW.MdpService.Test.Documents;

public class CaseDocumentsServiceTest
{
    private readonly Mock<ITransferJourneyRepository> _transferJourneyRepositoryMock;
    private readonly Mock<IJourneysRepository> _journeysRepositoryMock;
    private readonly Mock<ILogger<CaseDocumentsService>> _loggerMock;
    private readonly Mock<IGenericJourneyService> _genericJourneyServiceMock;
    private readonly Mock<IRetirementJourneyRepository> _retirementJourneyRepositoryMock;
    private readonly Mock<ICasesClient> _caseClientMock;
    private readonly CaseDocumentsService _sut;

    public CaseDocumentsServiceTest()
    {
        _transferJourneyRepositoryMock = new Mock<ITransferJourneyRepository>();
        _journeysRepositoryMock = new Mock<IJourneysRepository>();
        _loggerMock = new Mock<ILogger<CaseDocumentsService>>();
        _genericJourneyServiceMock = new Mock<IGenericJourneyService>();
        _retirementJourneyRepositoryMock = new Mock<IRetirementJourneyRepository>();
        _caseClientMock = new Mock<ICasesClient>();
        _sut = new CaseDocumentsService(
            _transferJourneyRepositoryMock.Object,
            _journeysRepositoryMock.Object,
            _loggerMock.Object,
            _genericJourneyServiceMock.Object,
            _retirementJourneyRepositoryMock.Object,
            _caseClientMock.Object);
    }

    [Input("transfer", true, true, "123456")]
    [Input("transfer", true, false, "Transfer journey is not submitted yet.")]
    [Input("transfer", false, false, "Transfer journey not found.")]
    [Input("retirement", true, true, "123456")]
    [Input("retirement", true, false, "Retirement journey is not submitted yet.")]
    [Input("retirement", false, false, "Retirement journey not found.")]
    [Input("generic", true, true, "123456")]
    [Input("generic", true, false, "generic journey is not submitted yet.")]
    [Input("generic", false, false, "generic journey not found.")]
    [Input("paperretirementapplication", false, false, "123456")]
    [Input("papertransferapplication", false, false, "123456")]
    public async Task GetCaseNumberReturnsCorrectResult(string caseCode, bool journeyExists, bool isJourneySubmitted, string expectedMessage)
    {
        var transferJourney = TransferJourney.Create("TestBusinessGroup", "TestReferenceNumber", DateTimeOffset.UtcNow, 1);
        var journeyOption = journeyExists ? transferJourney : Option<TransferJourney>.None;
        if (isJourneySubmitted)
            transferJourney.Submit("123456", DateTimeOffset.UtcNow);

        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(journeyOption);

        var retirementJourney = new RetirementJourneyBuilder().Build();
        var retirementJourneyOption = journeyExists ? retirementJourney : Option<RetirementJourney>.None;
        if (isJourneySubmitted)
            retirementJourney.Submit(new byte[0], DateTimeOffset.UtcNow, "123456");

        _retirementJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(retirementJourneyOption);

        var genericJourney = new GenericJourney("RBS", "1111111", "generic", "step1", "step2", true, "Started", DateTimeOffset.UtcNow.AddMinutes(-1));
        var genericJourneyOption = journeyExists ? genericJourney : Option<GenericJourney>.None;
        if (isJourneySubmitted)
        {
            genericJourney.TrySubmitStep("step2", "step3", DateTimeOffset.UtcNow);
            genericJourney.GetFirstStep().UpdateGenericData("JourneySubmissionDetails", "{\"caseNumber\":\"123456\"}");
            genericJourney.SubmitJourney(DateTimeOffset.UtcNow);

            _genericJourneyServiceMock
                .Setup(x => x.GetSubmissionDetailsFromGenericData(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(Right<Error, SubmissionDetailsDto>(new SubmissionDetailsDto { CaseNumber = "123456" }));
        }

        _journeysRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(genericJourneyOption);

        _caseClientMock
            .Setup(x => x.GetRetirementOrTransferCases(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<CasesResponse> {
                new CasesResponse
                {
                    CaseCode = MdpConstants.CaseCodes.RTP9,
                    CaseStatus = "Open",
                    CaseNumber = "123456",
                    CaseSource = "Test",
                },
                new CasesResponse
                {
                    CaseCode = MdpConstants.CaseCodes.TOP9,
                    CaseStatus = "Open",
                    CaseNumber = "123456",
                    CaseSource = "Test",
                }});

        var result = await _sut.GetCaseNumber("TestBusinessGroup", "TestReferenceNumber", caseCode);
        if (result.IsRight)
            result.Right().Should().Be(expectedMessage);
        else
            result.Left().Message.Should().Be(expectedMessage);
    }

    [Input("paperretirementapplication", true, false, "123456", "")]
    [Input("papertransferapplication", true, false, "1234567", "")]
    [Input("paperretirementapplication", false, false, null, "Paper retirement case not found.")]
    [Input("papertransferapplication", false, false, null, "Paper transfer case not found.")]
    [Input("paperretirementapplication", false, true, null, "Paper retirement case not found.")]
    [Input("papertransferapplication", false, true, null, "Paper transfer case not found.")]
    public async Task GetCaseNumberReturnsCorrectResultForPaperAppliation(string caseCode, bool paperCaseExists, bool otherCaseExits, string caseNumber, string expectedMessage)
    {
        _caseClientMock
            .Setup(x => x.GetRetirementOrTransferCases(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(
              paperCaseExists ?
                  new List<CasesResponse> {
                    new CasesResponse
                    {
                        CaseCode = MdpConstants.CaseCodes.RTP9,
                        CaseStatus = "Open",
                        CaseNumber = caseNumber,
                        CaseSource = "Test",
                    },
                    new CasesResponse
                    {
                        CaseCode = MdpConstants.CaseCodes.TOP9,
                        CaseStatus = "Open",
                        CaseNumber = caseNumber,
                        CaseSource = "Test",
                    }
                  }
              : otherCaseExits ?
                  new List<CasesResponse> {
                    new CasesResponse
                    {
                        CaseCode = MdpConstants.CaseCodes.RTP9,
                        CaseStatus = "Open",
                        CaseNumber = caseNumber,
                        CaseSource = "MDP",
                    }
                  }
              :
               new List<CasesResponse>());

        var result = await _sut.GetCaseNumber("TestBusinessGroup", "TestReferenceNumber", caseCode);
        if (result.IsRight)
            result.Right().Should().Be(caseNumber);
        else
            result.Left().Message.Should().Be(expectedMessage);
    }
}
