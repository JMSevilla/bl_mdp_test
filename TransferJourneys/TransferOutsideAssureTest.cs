using System;
using System.Text.Json;
using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Domain.Mdp;
using WTW.MdpService.Domain.Mdp.Calculations;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.MdpService.Infrastructure.MdpDb;
using WTW.MdpService.Infrastructure.MemberDb;
using WTW.MdpService.Infrastructure.MemberDb.Documents;
using WTW.MdpService.Test.Domain.Mdp.Calculations;
using WTW.MdpService.Test.Domain.Members;
using WTW.MdpService.TransferJourneys;
using WTW.TestCommon.FixieConfig;
using WTW.Web.Serialization;

namespace WTW.MdpService.Test.TransferJourneys;

public class TransferOutsideAssureTest
{
    private readonly TransferOutsideAssure _sut;
    private readonly Mock<ICalculationsClient> _calculationsClientMock;
    private readonly Mock<ITransferCalculationRepository> _transferCalculationRepositoryMock;
    private readonly Mock<ITransferJourneyRepository> _transferJourneyRepositoryMock;
    private readonly Mock<IDocumentsRepository> _documentsRepositoryMock;
    private readonly Mock<IDocumentFactoryProvider> _documentFactoryProviderMock;
    private readonly Mock<IMemberDbUnitOfWork> _memberDbUnitOfWorkMock;
    private readonly Mock<IMdpUnitOfWork> _mdpUnitOfWorkMock;
    private readonly Mock<ICalculationsParser> _calculationsParserMock;
    private readonly Mock<ILogger<TransferOutsideAssure>> _loggerMock;

    public TransferOutsideAssureTest()
    {
        _calculationsClientMock = new Mock<ICalculationsClient>();
        _transferCalculationRepositoryMock = new Mock<ITransferCalculationRepository>();
        _transferJourneyRepositoryMock = new Mock<ITransferJourneyRepository>();
        _documentsRepositoryMock = new Mock<IDocumentsRepository>();
        _documentFactoryProviderMock = new Mock<IDocumentFactoryProvider>();
        _memberDbUnitOfWorkMock = new Mock<IMemberDbUnitOfWork>();
        _mdpUnitOfWorkMock = new Mock<IMdpUnitOfWork>();
        _calculationsParserMock = new Mock<ICalculationsParser>();
        _loggerMock = new Mock<ILogger<TransferOutsideAssure>>();

        _sut = new TransferOutsideAssure(_calculationsClientMock.Object,
            _transferJourneyRepositoryMock.Object,
            _transferCalculationRepositoryMock.Object,
            _documentsRepositoryMock.Object,
            _documentFactoryProviderMock.Object,
            _memberDbUnitOfWorkMock.Object,
            _mdpUnitOfWorkMock.Object,
            _calculationsParserMock.Object,
            _loggerMock.Object);
    }

    public async Task CreatesCalculationAndJourney_WhenQuoteIsLockedAndJourneyDoesNotExist()
    {
        _calculationsClientMock
           .Setup(x => x.RetirementDatesAges(It.IsAny<string>(), It.IsAny<string>()))
           .Returns(async () => new RetirementDatesAgesResponse
           {
               HasLockedInTransferQuote = true,
               LockedInTransferQuoteImageId = 123456
           });

        _documentFactoryProviderMock
           .Setup(x => x.GetFactory(DocumentType.TransferV2OutsideAssureQuoteLock))
           .Returns(new TransferV2OutsideAssureQuoteLockDocumentFactory());

        _calculationsClientMock
              .Setup(x => x.TransferCalculation(It.IsAny<string>(), It.IsAny<string>(), false))
              .ReturnsAsync(JsonSerializer.Deserialize<TransferResponse>(TestData.TransferQuoteResponseJson, SerialiationBuilder.Options()));

        _transferJourneyRepositoryMock.Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(Option<TransferJourney>.None);

        await _sut.CreateTransferForLockedQuote(It.IsAny<string>(), It.IsAny<string>());

        _transferCalculationRepositoryMock.Verify(x => x.Create(It.IsAny<TransferCalculation>()), Times.Once);
        _transferJourneyRepositoryMock.Verify(x => x.Create(It.IsAny<TransferJourney>()), Times.Once);
    }

    public async Task DoesNotCreatesCalculationAndJourney_WhenQuoteIsNotLockedOrJourneyDoesNotExist()
    {
        _calculationsClientMock
           .Setup(x => x.RetirementDatesAges(It.IsAny<string>(), It.IsAny<string>()))
           .Returns(async () => new RetirementDatesAgesResponse
           {
               HasLockedInTransferQuote = false,
               LockedInTransferQuoteImageId = null
           });

        await _sut.CreateTransferForLockedQuote(It.IsAny<string>(), It.IsAny<string>());

        _transferCalculationRepositoryMock.Verify(x => x.Create(It.IsAny<TransferCalculation>()), Times.Never);
        _transferJourneyRepositoryMock.Verify(x => x.Create(It.IsAny<TransferJourney>()), Times.Never);
    }

    [Input(false, "Failed to retrieve HasLockedInTransferQuote value from calc api. Business group:RBS, Refno:1234567.")]
    [Input(true, "Failed to retrieve Transfer data from calc api. Business group:RBS, Refno:1234567. Error: 'Calc api response failed.'.")]
    public async Task CreateTransferForLockedQuoteReturnsCorrectError(bool calcApiRetirementDatesAgesSucceeds, string expectedErrorMessage)
    {
        if (calcApiRetirementDatesAgesSucceeds)
            _calculationsClientMock
               .Setup(x => x.RetirementDatesAges("1234567", "RBS"))
               .Returns(async () => new RetirementDatesAgesResponse
               {
                   HasLockedInTransferQuote = true,
                   LockedInTransferQuoteImageId = 123456
               });

        _calculationsClientMock
             .Setup(x => x.TransferCalculation("RBS", "1234567", false))
             .ReturnsAsync(Error.New("Calc api response failed."));

        _transferJourneyRepositoryMock.Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(Option<TransferJourney>.None);

        await _sut.CreateTransferForLockedQuote("1234567", "RBS");

        _loggerMock.Verify(logger => logger.Log(
         It.Is<LogLevel>(logLevel => logLevel == LogLevel.Error),
         It.Is<EventId>(eventId => eventId.Id == 0),
         It.Is<It.IsAnyType>((@object, @type) => @object.ToString() == expectedErrorMessage && @type.Name == "FormattedLogValues"),
         It.IsAny<Exception>(),
         It.IsAny<Func<It.IsAnyType, Exception, string>>()),
     Times.Once);
    }

    public async Task CreatesCalculationAndJourneyForEpa_WhenPaperApplicationExist()
    {
        var paperApplication = new PaperRetirementApplication(null, "TOP9", null, null, null, null, DateTimeOffset.UtcNow.AddDays(-10), new BatchCreateDetails("paper"));
        var member = new MemberBuilder().PaperRetirementApplications(paperApplication).Build();

        await _sut.CreateTransferForEpa(member);

        _transferCalculationRepositoryMock.Verify(x => x.Create(It.IsAny<TransferCalculation>()), Times.Once);
        _transferJourneyRepositoryMock.Verify(x => x.Create(It.IsAny<TransferJourney>()), Times.Once);
    }
}