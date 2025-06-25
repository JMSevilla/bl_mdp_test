using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Application;
using WTW.MdpService.Domain.Common;
using WTW.MdpService.Domain.Common.Journeys;
using WTW.MdpService.Domain.Mdp;
using WTW.MdpService.Domain.Mdp.Calculations;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.MdpService.Infrastructure.MdpDb;
using WTW.MdpService.Infrastructure.Redis;
using WTW.MdpService.Journeys.Submit.Services;
using WTW.MdpService.Journeys.Submit.Services.Dto;
using WTW.MdpService.Test.Domain.Mdp;
using WTW.MdpService.Test.Domain.Mdp.Calculations;
using WTW.MdpService.Test.Domain.Mdp.Journeys;
using WTW.MdpService.Test.Domain.Members;
using WTW.MdpService.TransferJourneys;
using WTW.TestCommon.FixieConfig;
using WTW.TestCommon.Helpers;
using WTW.Web.Caching;

namespace WTW.MdpService.Test.Application;

public class ApplicationInitializationTest
{
    private Mock<ICalculationsRedisCache> _calculationsRedisCacheMock;
    private readonly Mock<ITransferCalculationRepository> _transferCalculationRepositoryMock;
    private readonly Mock<ITransferJourneyRepository> _transferJourneyRepositoryMock;
    private readonly Mock<ITransferOutsideAssure> _transferOutsideAssureMock;
    private readonly Mock<ICalculationsClient> _calculationsClientMock;
    private readonly Mock<IJourneyDocumentsRepository> _journeyDocumentsRepositoryMock;
    private readonly Mock<IJourneysRepository> _genericRepositoryMock;
    private readonly Mock<ILogger<ApplicationInitialization>> _loggerMock;
    private readonly Mock<IMdpUnitOfWork> _mdpUnitOfWorkMock;
    private readonly Mock<ICache> _cacheMock;
    private readonly Mock<ICalculationsRepository> _calculationsRepositoryMock;
    private readonly Mock<IGenericJourneyService> _genericJourneyServiceMock;
    private readonly ApplicationInitialization _sut;

    public ApplicationInitializationTest()
    {
        _calculationsRedisCacheMock = new Mock<ICalculationsRedisCache>();
        _transferCalculationRepositoryMock = new Mock<ITransferCalculationRepository>();
        _transferJourneyRepositoryMock = new Mock<ITransferJourneyRepository>();
        _transferOutsideAssureMock = new Mock<ITransferOutsideAssure>();
        _calculationsClientMock = new Mock<ICalculationsClient>();
        _journeyDocumentsRepositoryMock = new Mock<IJourneyDocumentsRepository>();
        _genericRepositoryMock = new Mock<IJourneysRepository>();
        _loggerMock = new Mock<ILogger<ApplicationInitialization>>();
        _mdpUnitOfWorkMock = new Mock<IMdpUnitOfWork>();
        _cacheMock = new Mock<ICache>();
        _calculationsRepositoryMock = new Mock<ICalculationsRepository>();
        _genericJourneyServiceMock = new Mock<IGenericJourneyService>();
        _sut = new ApplicationInitialization(_calculationsRedisCacheMock.Object,
            _transferCalculationRepositoryMock.Object,
            _transferJourneyRepositoryMock.Object,
            _transferOutsideAssureMock.Object,
            _calculationsClientMock.Object,
            _journeyDocumentsRepositoryMock.Object,
            _genericRepositoryMock.Object,
            _loggerMock.Object,
            _mdpUnitOfWorkMock.Object,
            _cacheMock.Object,
            _genericJourneyServiceMock.Object,
            _calculationsRepositoryMock.Object);
    }

    public async Task SetsUpTransferData_WhenTransferJourneyDoesNotExists()
    {
        _transferJourneyRepositoryMock
            .Setup(m => m.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.None);
        _transferCalculationRepositoryMock
            .Setup(m => m.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new TransferCalculation("RBS", "1111124", "{}", DateTimeOffset.UtcNow));

        await _sut.SetUpTransfer(new MemberBuilder().Build());

        _transferCalculationRepositoryMock.Verify(x => x.Remove(It.IsAny<TransferCalculation>()), Times.Once);
        _transferOutsideAssureMock.Verify(x => x.CreateTransferForEpa(It.IsAny<Member>()), Times.Once);
        _transferOutsideAssureMock.Verify(x => x.CreateTransferForLockedQuote(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _mdpUnitOfWorkMock.Verify(x => x.Commit(), Times.AtLeast(2));
        _mdpUnitOfWorkMock.Verify(x => x.Commit(), Times.AtMost(2));
    }

    [Input(true)]
    [Input(false)]
    public async Task SetsUpTransferData_WhenTransferJourneyExists(bool hasPaperCase)
    {
        var paperApplication = new PaperRetirementApplication(null, hasPaperCase ? "TOP9" : "RTP9", null, null, null, null, DateTimeOffset.UtcNow.AddDays(-10), new BatchCreateDetails("paper"));
        var member = new MemberBuilder().PaperRetirementApplications(paperApplication).Build();

        var journey = new TransferJourneyBuilder().BuildWithSteps();
        journey.TrySubmitStep("transfer_start_1", "t2_submit_upload", DateTimeOffset.UtcNow.AddDays(-35));
        journey.TrySubmitStep("t2_submit_upload", "t2_submit_upload1", DateTimeOffset.UtcNow.AddDays(-34));

        _transferJourneyRepositoryMock
            .Setup(m => m.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(journey);

        var calculation = new TransferCalculation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), DateTimeOffset.UtcNow);
        calculation.LockTransferQoute();
        calculation.SetStatus(TransferApplicationStatus.SubmitStarted);
        _transferCalculationRepositoryMock
            .Setup(m => m.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(calculation);

        _journeyDocumentsRepositoryMock
            .Setup(x => x.List(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<UploadedDocument>
            {
                new UploadedDocumentsBuilder().Build(),
                new UploadedDocumentsBuilder().Build(),
            });

        _calculationsClientMock
            .Setup(x => x.RetirementDatesAges(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(async () => new RetirementDatesAgesResponse
            {
                HasLockedInTransferQuote = false,
                LockedInTransferQuoteImageId = 123456
            });

        await _sut.SetUpTransfer(member);

        _transferCalculationRepositoryMock.Verify(x => x.Remove(It.IsAny<TransferCalculation>()), Times.Once);
        _transferJourneyRepositoryMock.Verify(x => x.Remove(It.IsAny<TransferJourney>()), Times.Once);
        _journeyDocumentsRepositoryMock.Verify(x => x.List(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _journeyDocumentsRepositoryMock.Verify(x => x.RemoveAll(It.IsAny<List<UploadedDocument>>()), Times.Once);
        _mdpUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);

        if (hasPaperCase)
            _transferOutsideAssureMock.Verify(x => x.CreateTransferForEpa(It.IsAny<Member>()), Times.Once);
    }

    [Input(true)]
    [Input(false)]
    public async Task RemovesGenericJourneyData(bool journeysExists)
    {
        _genericRepositoryMock
            .Setup(x => x.FindAllMarkedForRemoval(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(journeysExists ? new List<GenericJourney>
            {
                    new GenericJourney("RBS", "1234567", "transfer1", "step1", "step2",true, "Started", DateTimeOffset.UtcNow),
                    new GenericJourney("RBS", "1234567", "transfer2", "step1", "step2",true, "Started", DateTimeOffset.UtcNow),
                    new GenericJourney("RBS", "1234567", "transfer3", "step1", "step2",true, "Started", DateTimeOffset.UtcNow),
            } : new List<GenericJourney>());

        _journeyDocumentsRepositoryMock
           .Setup(x => x.List(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()))
           .ReturnsAsync(new List<UploadedDocument>
           {
                new UploadedDocumentsBuilder().Build(),
                new UploadedDocumentsBuilder().Build(),
           });

        await _sut.RemoveGenericJourneys(It.IsAny<string>(), It.IsAny<string>());

        if (journeysExists)
        {
            _mdpUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
            _genericRepositoryMock.Verify(x => x.Remove(It.IsAny<List<GenericJourney>>()), Times.Once);
            _journeyDocumentsRepositoryMock.Verify(x => x.RemoveAll(It.IsAny<List<UploadedDocument>>()), Times.Once);
        }
        else
        {
            _mdpUnitOfWorkMock.Verify(x => x.Commit(), Times.Never);
            _genericRepositoryMock.Verify(x => x.Remove(It.IsAny<List<GenericJourney>>()), Times.Never);
            _journeyDocumentsRepositoryMock.Verify(x => x.RemoveAll(It.IsAny<List<UploadedDocument>>()), Times.Never);
        }
    }

    public async Task UpdatesGenericJourneysStatuses()
    {
        var journeys = new List<GenericJourney>
            {
                    new GenericJourney("RBS", "1234567", "transfer1", "step1", "step2",true, "Started", DateTimeOffset.UtcNow),
                    new GenericJourney("RBS", "1234567", "transfer2", "step1", "step2",true, "Started", DateTimeOffset.UtcNow),
                    new GenericJourney("RBS", "1234567", "transfer3", "step1", "step2",true, "Started", DateTimeOffset.UtcNow),
            };

        _genericRepositoryMock
            .Setup(x => x.FindAllExpiredUnsubmitted(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(journeys);

        await _sut.UpdateGenericJourneysStatuses(It.IsAny<string>(), It.IsAny<string>());

        journeys.All(x => x.Status == "expired").Should().BeTrue();
        _mdpUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
        _genericRepositoryMock.Verify(x => x.FindAllExpiredUnsubmitted(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    public async Task UpdatesGenericJourneysStatusesReturns_WhenEmptyCollectionOfExpiredJourneyFound()
    {
        _genericRepositoryMock
            .Setup(x => x.FindAllExpiredUnsubmitted(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<GenericJourney>());

        await _sut.UpdateGenericJourneysStatuses(It.IsAny<string>(), It.IsAny<string>());

        _genericRepositoryMock.Verify(x => x.FindAllExpiredUnsubmitted(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _mdpUnitOfWorkMock.Verify(x => x.Commit(), Times.Never);
    }

    public async Task ClearsSessionCache()
    {
        await _sut.ClearSessionCache("1111122", "RBS");

        _cacheMock.Verify(x => x.RemoveByPrefix($"{CachedPerSessionAttribute.CachedPerSessionKeyPrefix}RBS-1111122"), Times.Once);
    }

    [Input(true, false, "DC")]
    [Input(false, true, "DB")]
    public async Task RemoveAbandonedDcJourneyReturnsWithNoAction_WhenCaseNumberDoesNotExistsOrCaseIsNotAbandoned(bool caseNumberExists, bool isCaseAbandoned, string schemeType)
    {
        Either<Error, SubmissionDetailsDto> detailsOrError = Error.New("Case number is not found");
        if (caseNumberExists)
            detailsOrError = new SubmissionDetailsDto { CaseNumber = "123456789" };
        _genericJourneyServiceMock
              .Setup(x => x.GetSubmissionDetailsFromGenericData(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
              .ReturnsAsync(detailsOrError);

        var paperApplication1 = new PaperRetirementApplication(
            "AC", "RTP9", "11111111", "someType", "someStatus", null, null, new BatchCreateDetails("paper"));
        var paperApplication2 = new PaperRetirementApplication(
            "AC", "RTP9", "11111112", "someType", "someStatus", DateTimeOffset.UtcNow.AddDays(-23), null, new BatchCreateDetails("paper"));
        var member = new MemberBuilder()
            .PaperRetirementApplications(paperApplication1, paperApplication2)
            .SchemeType(schemeType)
            .Build();

        if (schemeType == "DC")
        {
            await _sut.SetUpDcRetirement(member);
        }
        else if (schemeType == "DB")
        {
            await _sut.SetUpDbRetirement(member);
        }

        _genericRepositoryMock.Verify(x => x.Remove(It.IsAny<GenericJourney>()), Times.Never);
        _journeyDocumentsRepositoryMock.Verify(x => x.RemoveAll(It.IsAny<List<UploadedDocument>>()), Times.Never);
        _mdpUnitOfWorkMock.Verify(x => x.Commit(), Times.Never);
        _calculationsRepositoryMock.Verify(x => x.Remove(It.IsAny<Calculation>()), Times.Never);
    }

    [Input("DC", JourneyTypes.DcRetirementApplication)]
    [Input("DB", JourneyTypes.DbCoreRetirementApplication)]

    public async Task RemovesAbandonedJourneyAndDocuments_WhenCaseIsAbandoned(string schemeType, string journeyType)
    {
        _calculationsRepositoryMock.Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new CalculationBuilder().BuildV2());
        _genericJourneyServiceMock
              .Setup(x => x.GetSubmissionDetailsFromGenericData(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
              .ReturnsAsync(new SubmissionDetailsDto { CaseNumber = "123456789" });

        var paperApplication1 = new PaperRetirementApplication(
            "AC", "RTP9", "11111111", "someType", "someStatus", null, null, new BatchCreateDetails("paper"));
        var paperApplication2 = new PaperRetirementApplication(
            "AC", "RTP9", "123456789", "someType", "someStatus", DateTimeOffset.UtcNow.AddDays(-23), null, new BatchCreateDetails("paper"));
        var member = new MemberBuilder()
            .PaperRetirementApplications(paperApplication1, paperApplication2)
            .SchemeType(schemeType)
            .Build();

        _journeyDocumentsRepositoryMock
           .Setup(x => x.List(It.IsAny<string>(), It.IsAny<string>(), journeyType))
           .ReturnsAsync(new List<UploadedDocument>
           {
                new UploadedDocumentsBuilder().Build(),
                new UploadedDocumentsBuilder().Build(),
           });

        _genericRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new GenericJourney("RBS", "1234567", "transfer1", "step1", "step2", true, "Started", DateTimeOffset.UtcNow));

        if (schemeType == "DC")
        {
            await _sut.SetUpDcRetirement(member);
        }
        else if (schemeType == "DB")
        {
            await _sut.SetUpDbRetirement(member);
        }

        _genericRepositoryMock.Verify(x => x.Remove(It.IsAny<GenericJourney>()), Times.Once);
        _journeyDocumentsRepositoryMock.Verify(x => x.RemoveAll(It.IsAny<List<UploadedDocument>>()), Times.Once);
        _calculationsRepositoryMock.Verify(x => x.Remove(It.IsAny<Calculation>()), Times.Once);
        _mdpUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
    }

    public async Task SetUpRetirementReturns_WhenRetirementJourneyIsNotNull()
    {
        var member = new MemberBuilder().Build();
        var calculation = new CalculationBuilder().BuildV2();
        calculation.SetJourney(new RetirementJourneyBuilder().Build(), "reducedPension");

        _calculationsRepositoryMock.Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(calculation);

        await _sut.SetUpDbRetirement(member);

        var expectedMessage = $"JourneyType is not a generic type and uses RetirementJourney table, Refno: {member.ReferenceNumber}, BGroup: {member.BusinessGroup}";

        _loggerMock.VerifyLogging(expectedMessage, LogLevel.Information, Times.Once());
    }

    public async Task SetUpDcRetirementReturns_WhenSchemeTypeIsNotDc()
    {
        await _sut.SetUpDcRetirement(new MemberBuilder().Build());

        _genericRepositoryMock.Verify(x => x.Remove(It.IsAny<GenericJourney>()), Times.Never);
        _journeyDocumentsRepositoryMock.Verify(x => x.RemoveAll(It.IsAny<List<UploadedDocument>>()), Times.Never);
        _mdpUnitOfWorkMock.Verify(x => x.Commit(), Times.Never);
        _calculationsRepositoryMock.Verify(x => x.Find(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    public async Task RemovesCalculation_WhenDcJourneyDesNotExist()
    {
        _calculationsRepositoryMock.Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new CalculationBuilder().BuildV2());
        _genericJourneyServiceMock
             .Setup(x => x.GetSubmissionDetailsFromGenericData(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
             .ReturnsAsync(Error.New("Case number is not found"));

        var member = new MemberBuilder()
            .SchemeType("DC")
            .Build();

        _genericRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<GenericJourney>.None);

        await _sut.SetUpDcRetirement(member);

        _calculationsRepositoryMock.Verify(x => x.Remove(It.IsAny<Calculation>()), Times.Once);
        _mdpUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
    }

    public async Task RemovesCalculation_WhenDcJourneyExpiredAndUnsubmitted()
    {
        _calculationsRepositoryMock.Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new CalculationBuilder().BuildV2());
        _genericJourneyServiceMock
             .Setup(x => x.GetSubmissionDetailsFromGenericData(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
             .ReturnsAsync(Error.New("Case number is not found"));

        var member = new MemberBuilder()
            .SchemeType("DC")
            .Build();

        _genericRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new GenericJourney("RBS", "1234567", "transfer1", "step1", "step2", true, "Started", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(-100)));

        await _sut.SetUpDcRetirement(member);

        _calculationsRepositoryMock.Verify(x => x.Remove(It.IsAny<Calculation>()), Times.Once);
        _mdpUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
    }

    [Input("DC")]
    [Input("DB")]
    public async Task SkipRemovingCalculation_WhenItIsEmpty(string schemeType)
    {
        _genericJourneyServiceMock
             .Setup(x => x.GetSubmissionDetailsFromGenericData(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
             .ReturnsAsync(Error.New("Case number is not found"));

        var member = new MemberBuilder()
            .SchemeType(schemeType)
            .Build();

        _genericRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new GenericJourney("RBS", "1234567", "transfer1", "step1", "step2", true, "Started", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(-100)));

        if (schemeType == "DC")
        {
            await _sut.SetUpDcRetirement(member);
        }
        else if (schemeType == "DB")
        {
            await _sut.SetUpDbRetirement(member);
        }

        _calculationsRepositoryMock.Verify(x => x.Remove(It.IsAny<Calculation>()), Times.Never);
    }
}