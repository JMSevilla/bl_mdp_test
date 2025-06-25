using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using IronPdf;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Domain;
using WTW.MdpService.Domain.Common;
using WTW.MdpService.Domain.Mdp;
using WTW.MdpService.Domain.Mdp.Calculations;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.Infrastructure.Aws;
using WTW.MdpService.Infrastructure.Calculations;
using WTW.MdpService.Infrastructure.Content;
using WTW.MdpService.Infrastructure.Edms;
using WTW.MdpService.Infrastructure.EdmsIndexing;
using WTW.MdpService.Infrastructure.EmailConfirmation;
using WTW.MdpService.Infrastructure.Journeys.Documents;
using WTW.MdpService.Infrastructure.MdpDb;
using WTW.MdpService.Infrastructure.MemberDb;
using WTW.MdpService.Infrastructure.MemberDb.Documents;
using WTW.MdpService.Infrastructure.PdfGenerator;
using WTW.MdpService.Infrastructure.Redis;
using WTW.MdpService.Infrastructure.Templates.TransferApplication;
using WTW.MdpService.Templates;
using WTW.MdpService.Test.Domain.Mdp.Calculations;
using WTW.MdpService.Test.Domain.Mdp.Journeys;
using WTW.MdpService.Test.Domain.Members;
using WTW.MdpService.TransferJourneys;
using WTW.TestCommon.FixieConfig;
using WTW.Web;
using WTW.Web.Errors;
using WTW.Web.LanguageExt;
using WTW.Web.Serialization;
using static LanguageExt.Prelude;

namespace WTW.MdpService.Test.TransferJourneys;

public class TransferJourneyV2ControllerTest
{
    private readonly TransferJourneyV2Controller _sut;
    private readonly Mock<ITransferJourneyRepository> _transferJourneyRepositoryMock;
    private readonly Mock<IMdpUnitOfWork> _mdpDbUnitOfWorkMock;
    private readonly Mock<ICalculationsClient> _calculationsClientMock;
    private readonly Mock<ITransferCalculationRepository> _transferCalculationRepositoryMock;
    private readonly Mock<ICalculationsRedisCache> _calculationsRedisCacheMock;
    private readonly Mock<IAwsClient> _awsClientMock;
    private readonly Mock<IEdmsClient> _edmsClientMock;
    private readonly Mock<ICalculationHistoryRepository> _calculationHistoryRepositoryMock;
    private readonly Mock<IRetirementPostIndexEventRepository> _postIndexEventsRepositoryMock;
    private readonly Mock<IMemberDbUnitOfWork> _uowMock;
    private readonly Mock<IMemberRepository> _memberRepositoryMock;
    private readonly Mock<IContentClient> _contentClientMock;
    private readonly Mock<IEmailConfirmationSmtpClient> _smtpClientMock;
    private readonly Mock<ITransferCase> _transferCaseMock;
    private readonly Mock<IEdmsDocumentsIndexing> _edmsDocumentsIndexingMock;
    private readonly Mock<ILogger<TransferJourneyV2Controller>> _logger;
    private readonly Mock<IFormFile> _fileMock;
    private readonly Mock<ITransferV2Template> _transferV2TemplateMock;
    private readonly Mock<ITransferJourneySubmitEmailTemplate> _transferJourneySubmitEmailTemplateMock;
    private readonly Mock<IJourneyDocumentsRepository> _journeyDocumentsRepositoryMock;
    private readonly Mock<IDocumentsRepository> _documentsRepositoryMock;
    private readonly Mock<IDocumentFactoryProvider> _documentFactoryProviderMock;
    private readonly Mock<ITransferJourneyContactFactory> _transferJourneyContactFactoryMock;
    private readonly Mock<IPdfGenerator> _pdfGeneratorMock;
    private readonly Mock<ITemplateService> _templateServiceMock;
    private readonly Mock<ICalculationsRepository> _calculationsRepositoryMock;
    private readonly Mock<ICalculationsParser> _calculationsParserMock;
    private readonly Mock<IJourneyDocumentsHandlerService> _journeyDocumentsHandlerServiceMock;

    public TransferJourneyV2ControllerTest()
    {
        _transferJourneyRepositoryMock = new Mock<ITransferJourneyRepository>();
        _mdpDbUnitOfWorkMock = new Mock<IMdpUnitOfWork>();
        _calculationsClientMock = new Mock<ICalculationsClient>();
        _transferCalculationRepositoryMock = new Mock<ITransferCalculationRepository>();
        _calculationsRedisCacheMock = new Mock<ICalculationsRedisCache>();
        _awsClientMock = new Mock<IAwsClient>();
        _edmsClientMock = new Mock<IEdmsClient>();
        _calculationHistoryRepositoryMock = new Mock<ICalculationHistoryRepository>();
        _postIndexEventsRepositoryMock = new Mock<IRetirementPostIndexEventRepository>();
        _uowMock = new Mock<IMemberDbUnitOfWork>();
        _memberRepositoryMock = new Mock<IMemberRepository>();
        _contentClientMock = new Mock<IContentClient>();
        _smtpClientMock = new Mock<IEmailConfirmationSmtpClient>();
        _transferCaseMock = new Mock<ITransferCase>();
        _edmsDocumentsIndexingMock = new Mock<IEdmsDocumentsIndexing>();
        _logger = new Mock<ILogger<TransferJourneyV2Controller>>();
        _fileMock = new Mock<IFormFile>();
        _transferV2TemplateMock = new Mock<ITransferV2Template>();
        _transferJourneySubmitEmailTemplateMock = new Mock<ITransferJourneySubmitEmailTemplate>();
        _journeyDocumentsRepositoryMock = new Mock<IJourneyDocumentsRepository>();
        _documentsRepositoryMock = new Mock<IDocumentsRepository>();
        _documentFactoryProviderMock = new Mock<IDocumentFactoryProvider>();
        _transferJourneyContactFactoryMock = new Mock<ITransferJourneyContactFactory>();
        _pdfGeneratorMock = new Mock<IPdfGenerator>();
        _templateServiceMock = new Mock<ITemplateService>();
        _calculationsRepositoryMock = new Mock<ICalculationsRepository>();
        _calculationsParserMock = new Mock<ICalculationsParser>();
        _journeyDocumentsHandlerServiceMock = new Mock<IJourneyDocumentsHandlerService>();

        _sut = new TransferJourneyV2Controller(
            _calculationsClientMock.Object,
            _transferCalculationRepositoryMock.Object,
            _calculationsRedisCacheMock.Object,
            _awsClientMock.Object,
            _edmsClientMock.Object,
            _calculationHistoryRepositoryMock.Object,
            _postIndexEventsRepositoryMock.Object,
            _uowMock.Object,
            _transferJourneyRepositoryMock.Object,
            _mdpDbUnitOfWorkMock.Object,
            _contentClientMock.Object,
            _memberRepositoryMock.Object,
            _smtpClientMock.Object,
            _transferCaseMock.Object,
            _edmsDocumentsIndexingMock.Object,
            _transferV2TemplateMock.Object,
            _transferJourneySubmitEmailTemplateMock.Object,
            _journeyDocumentsRepositoryMock.Object,
            _documentsRepositoryMock.Object,
            _logger.Object,
            _documentFactoryProviderMock.Object,
            _transferJourneyContactFactoryMock.Object,
            _pdfGeneratorMock.Object,
            _templateServiceMock.Object,
            _calculationsRepositoryMock.Object,
            _calculationsParserMock.Object,
            _journeyDocumentsHandlerServiceMock.Object);

        SetupControllerContext();
    }

    public async Task SaveGbgId_ReturnsNoContent_WhenJourneyIsFound()
    {
        var journeyMock = new Mock<TransferJourney>();
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(journeyMock.Object));

        _mdpDbUnitOfWorkMock
            .Setup(x => x.Commit())
            .Returns(Task.CompletedTask);

        var result = await _sut.SaveGbgId(Guid.NewGuid());
        result.Should().BeOfType<NoContentResult>();
    }

    public async Task SaveGbgId_ReturnsNotFound_WhenJourneyNotFound()
    {
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.None);

        var result = await _sut.SaveGbgId(Guid.NewGuid());
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task SaveGbgId_CommitsChanges_WhenJourneyFound()
    {
        var journey = new TransferJourneyBuilder().BuildWithSteps();
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(journey));

        await _sut.SaveGbgId(Guid.NewGuid());
        _mdpDbUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
    }

    public async Task StartTransferJourney_ReturnsNoContent_WhenAllOperationsSucceed()
    {
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.None);

        _calculationsClientMock
            .Setup(x => x.HardQuote(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Right<Error, string>("test"));

        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferCalculation>.Some(new TransferCalculation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), DateTimeOffset.UtcNow)));

        _calculationsRedisCacheMock
            .Setup(x => x.ClearRetirementDateAges(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _awsClientMock
            .Setup(x => x.File(It.IsAny<string>()))
            .ReturnsAsync(Right<Error, MemoryStream>(new MemoryStream()));

        _edmsClientMock
            .Setup(x => x.PreindexDocument(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(Right<PreindexError, PreindexResponse>(new PreindexResponse { ImageId = 1, BatchNumber = 1 }));

        _calculationsClientMock
            .Setup(x => x.TransferEventType(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(() => TryAsync(async () =>
            {
                return new TypeResponse { Type = "TestType" };
            }));

        var datesResponse = new RetirementDatesAgesResponse()
        {
            HasLockedInTransferQuote = true,
            LockedInTransferQuoteSeqno = 1,
        };
        _calculationsClientMock
           .Setup(x => x.RetirementDatesAges(It.IsAny<string>(), It.IsAny<string>()))
           .Returns(async () => datesResponse);

        _calculationHistoryRepositoryMock
            .Setup(x => x.FindByEventTypeAndSeqNumber(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(Option<CalculationHistory>.Some(new CalculationHistory("0003994", "RBS", "someEvent", 1, null, null)));

        _mdpDbUnitOfWorkMock
            .Setup(x => x.Commit())
            .Returns(Task.CompletedTask);

        _uowMock
            .Setup(x => x.Commit())
            .Returns(Task.CompletedTask);

        _templateServiceMock
            .Setup(x => x.DownloadTemplates(It.IsAny<string>()))
            .Returns(Task.FromResult<IList<byte[]>>(new List<byte[]>()));

        _pdfGeneratorMock
            .Setup(x => x.MergePdfs(
                It.IsAny<byte[]>(),
                It.IsAny<PdfDocument>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes("This is a test PDF stream."));

        var request = new StartTransferJourneyRequest
        {
            CurrentPageKey = "current_page_test",
            NextPageKey = "next_page_test"
        };
        var result = await _sut.StartTransferJourney(request);
        result.Should().BeOfType<NoContentResult>();
    }

    public async Task StartTransferJourney_ReturnsBadRequest_WhenAwsClientFileFails()
    {
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.None);

        _calculationsClientMock
            .Setup(x => x.HardQuote(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Right<Error, string>("test"));

        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferCalculation>.Some(new TransferCalculation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), DateTimeOffset.UtcNow)));

        _calculationsRedisCacheMock
            .Setup(x => x.ClearRetirementDateAges(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _awsClientMock
            .Setup(x => x.File(It.IsAny<string>()))
            .ReturnsAsync(Error.New("AWS File error"));

        _edmsClientMock
            .Setup(x => x.PreindexDocument(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(Right<PreindexError, PreindexResponse>(new PreindexResponse { ImageId = 1, BatchNumber = 1 }));

        _calculationsClientMock
            .Setup(x => x.TransferEventType(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(() => TryAsync(async () =>
            {
                return new TypeResponse { Type = "TestType" };
            }));

        var datesResponse = new RetirementDatesAgesResponse()
        {
            HasLockedInTransferQuote = true,
            LockedInTransferQuoteSeqno = 1,
        };
        _calculationsClientMock
           .Setup(x => x.RetirementDatesAges(It.IsAny<string>(), It.IsAny<string>()))
           .Returns(async () => datesResponse);

        _calculationHistoryRepositoryMock
            .Setup(x => x.FindByEventTypeAndSeqNumber(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(Option<CalculationHistory>.Some(new CalculationHistory("0003994", "RBS", "someEvent", 1, null, null)));

        _mdpDbUnitOfWorkMock
            .Setup(x => x.Commit())
            .Returns(Task.CompletedTask);

        _uowMock
            .Setup(x => x.Commit())
            .Returns(Task.CompletedTask);

        _templateServiceMock
            .Setup(x => x.DownloadTemplates(It.IsAny<string>()))
            .Returns(Task.FromResult<IList<byte[]>>(new List<byte[]>()));

        var request = new StartTransferJourneyRequest
        {
            CurrentPageKey = "current_page_test",
            NextPageKey = "next_page_test"
        };
        var result = await _sut.StartTransferJourney(request);
        result.Should().BeOfType<BadRequestObjectResult>();
        (result as BadRequestObjectResult).Value.Should().BeOfType<ApiError>();
    }

    public async Task StartTransferJourney_ReturnsBadRequest_WhenEdmsClientPreindexDocumentFails()
    {
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.None);

        _calculationsClientMock
            .Setup(x => x.HardQuote(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Right<Error, string>("test"));

        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferCalculation>.Some(new TransferCalculation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), DateTimeOffset.UtcNow)));

        _calculationsRedisCacheMock
            .Setup(x => x.ClearRetirementDateAges(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _awsClientMock
            .Setup(x => x.File(It.IsAny<string>()))
            .ReturnsAsync(Right<Error, MemoryStream>(new MemoryStream()));

        _calculationsClientMock
            .Setup(x => x.TransferEventType(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(() => TryAsync(async () =>
            {
                return new TypeResponse { Type = "TestType" };
            }));

        var datesResponse = new RetirementDatesAgesResponse()
        {
            HasLockedInTransferQuote = true,
            LockedInTransferQuoteSeqno = 1,
        };
        _calculationsClientMock
           .Setup(x => x.RetirementDatesAges(It.IsAny<string>(), It.IsAny<string>()))
           .Returns(async () => datesResponse);

        _calculationHistoryRepositoryMock
            .Setup(x => x.FindByEventTypeAndSeqNumber(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(Option<CalculationHistory>.Some(new CalculationHistory("0003994", "RBS", "someEvent", 1, null, null)));

        _mdpDbUnitOfWorkMock
            .Setup(x => x.Commit())
            .Returns(Task.CompletedTask);

        _uowMock
            .Setup(x => x.Commit())
            .Returns(Task.CompletedTask);

        _edmsClientMock
            .Setup(x => x.PreindexDocument(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(Left<PreindexError, PreindexResponse>(new PreindexError { Message = "EDMS Preindex error" }));

        _templateServiceMock
            .Setup(x => x.DownloadTemplates(It.IsAny<string>()))
            .Returns(Task.FromResult<IList<byte[]>>(new List<byte[]>()));

        var request = new StartTransferJourneyRequest
        {
            CurrentPageKey = "current_page_test",
            NextPageKey = "next_page_test"
        };
        var result = await _sut.StartTransferJourney(request);
        result.Should().BeOfType<BadRequestObjectResult>();
        (result as BadRequestObjectResult).Value.Should().BeOfType<ApiError>();
    }


    public async Task DownloadGuaranteedTransferQuote_ReturnsFile_WhenAllOperationsSucceed()
    {
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.None);

        _calculationsClientMock
            .Setup(x => x.GetGuaranteedTransfer(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Right<Error, string>("test"));

        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferCalculation>.Some(new TransferCalculation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), DateTimeOffset.UtcNow)));

        _calculationsRedisCacheMock
            .Setup(x => x.ClearRetirementDateAges(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _awsClientMock
            .Setup(x => x.File(It.IsAny<string>()))
            .ReturnsAsync(Right<Error, MemoryStream>(new MemoryStream()));

        _edmsClientMock
            .Setup(x => x.PreindexDocument(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(Right<PreindexError, PreindexResponse>(new PreindexResponse { ImageId = 1, BatchNumber = 1 }));

        _calculationsClientMock
            .Setup(x => x.TransferEventType(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(() => TryAsync(async () =>
            {
                return new TypeResponse { Type = "TestType" };
            }));

        var datesResponse = new RetirementDatesAgesResponse()
        {
            HasLockedInTransferQuote = true,
            LockedInTransferQuoteSeqno = 1,
        };
        _calculationsClientMock
           .Setup(x => x.RetirementDatesAges(It.IsAny<string>(), It.IsAny<string>()))
           .Returns(async () => datesResponse);

        _calculationHistoryRepositoryMock
            .Setup(x => x.FindByEventTypeAndSeqNumber(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(Option<CalculationHistory>.Some(new CalculationHistory("0003994", "RBS", "someEvent", 1, null, null)));

        _mdpDbUnitOfWorkMock
            .Setup(x => x.Commit())
            .Returns(Task.CompletedTask);

        _uowMock
            .Setup(x => x.Commit())
            .Returns(Task.CompletedTask);

        _templateServiceMock
            .Setup(x => x.DownloadTemplates(It.IsAny<string>()))
            .Returns(Task.FromResult<IList<byte[]>>(new List<byte[]>()));

        _pdfGeneratorMock
            .Setup(x => x.MergePdfs(
                It.IsAny<byte[]>(),
                It.IsAny<PdfDocument>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes("This is a test PDF stream."));

        var transferJourney = new TransferJourneyBuilder().Build();
        var transferCalculation = CreateTransferCalculation();
        var dummyFile = CreateDummyFile();

        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(transferJourney));

        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferCalculation>.Some(transferCalculation));

        transferJourney.SaveTransferSummaryImageId(1);
        _edmsClientMock
            .Setup(x => x.GetDocumentOrError(It.IsAny<int>()))
            .ReturnsAsync(Right<Error, Stream>(dummyFile.stream));

        var contentAccessKey = "content_access_key";
        var result = await _sut.DownloadGuaranteedTransferQuote(contentAccessKey);
        result.Should().BeOfType<FileStreamResult>();
        var fileResult = result as FileStreamResult;
        fileResult.Should().NotBeNull();
        fileResult.FileDownloadName.Should().Be("transfer-journey.pdf");
    }

    public async Task DownloadGuaranteedTransferQuote_ReturnsBadRequest_WhenAwsClientFileFails()
    {
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.None);

        _calculationsClientMock
            .Setup(x => x.GetGuaranteedTransfer(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Right<Error, string>("test"));

        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferCalculation>.Some(new TransferCalculation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), DateTimeOffset.UtcNow)));

        _calculationsRedisCacheMock
            .Setup(x => x.ClearRetirementDateAges(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _awsClientMock
            .Setup(x => x.File(It.IsAny<string>()))
            .ReturnsAsync(Error.New("AWS File error"));

        _edmsClientMock
            .Setup(x => x.PreindexDocument(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(Right<PreindexError, PreindexResponse>(new PreindexResponse { ImageId = 1, BatchNumber = 1 }));

        _calculationsClientMock
            .Setup(x => x.TransferEventType(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(() => TryAsync(async () =>
            {
                return new TypeResponse { Type = "TestType" };
            }));

        var datesResponse = new RetirementDatesAgesResponse()
        {
            HasLockedInTransferQuote = true,
            LockedInTransferQuoteSeqno = 1,
        };
        _calculationsClientMock
           .Setup(x => x.RetirementDatesAges(It.IsAny<string>(), It.IsAny<string>()))
           .Returns(async () => datesResponse);

        _calculationHistoryRepositoryMock
            .Setup(x => x.FindByEventTypeAndSeqNumber(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(Option<CalculationHistory>.Some(new CalculationHistory("0003994", "RBS", "someEvent", 1, null, null)));

        _mdpDbUnitOfWorkMock
            .Setup(x => x.Commit())
            .Returns(Task.CompletedTask);

        _uowMock
            .Setup(x => x.Commit())
            .Returns(Task.CompletedTask);

        _templateServiceMock
            .Setup(x => x.DownloadTemplates(It.IsAny<string>()))
            .Returns(Task.FromResult<IList<byte[]>>(new List<byte[]>()));

        var contentAccessKey = "content_access_key";
        var result = await _sut.DownloadGuaranteedTransferQuote(contentAccessKey);
        result.Should().BeOfType<BadRequestObjectResult>();
        (result as BadRequestObjectResult).Value.Should().BeOfType<ApiError>();
    }

    public async Task DownloadGuaranteedTransferQuote_ReturnsBadRequest_WhenEdmsClientPreindexDocumentFails()
    {
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.None);

        _calculationsClientMock
            .Setup(x => x.GetGuaranteedTransfer(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Right<Error, string>("test"));

        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferCalculation>.Some(new TransferCalculation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), DateTimeOffset.UtcNow)));

        _calculationsRedisCacheMock
            .Setup(x => x.ClearRetirementDateAges(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _awsClientMock
            .Setup(x => x.File(It.IsAny<string>()))
            .ReturnsAsync(Right<Error, MemoryStream>(new MemoryStream()));

        _calculationsClientMock
            .Setup(x => x.TransferEventType(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(() => TryAsync(async () =>
            {
                return new TypeResponse { Type = "TestType" };
            }));

        _pdfGeneratorMock
            .Setup(x => x.MergePdfs(
                It.IsAny<byte[]>(),
                It.IsAny<PdfDocument>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes("This is a test PDF stream."));

        var datesResponse = new RetirementDatesAgesResponse()
        {
            HasLockedInTransferQuote = true,
            LockedInTransferQuoteSeqno = 1,
        };
        _calculationsClientMock
           .Setup(x => x.RetirementDatesAges(It.IsAny<string>(), It.IsAny<string>()))
           .Returns(async () => datesResponse);

        _calculationHistoryRepositoryMock
            .Setup(x => x.FindByEventTypeAndSeqNumber(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(Option<CalculationHistory>.Some(new CalculationHistory("0003994", "RBS", "someEvent", 1, null, null)));

        _mdpDbUnitOfWorkMock
            .Setup(x => x.Commit())
            .Returns(Task.CompletedTask);

        _uowMock
            .Setup(x => x.Commit())
            .Returns(Task.CompletedTask);

        _edmsClientMock
            .Setup(x => x.PreindexDocument(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(Left<PreindexError, PreindexResponse>(new PreindexError { Message = "EDMS Preindex error" }));

        _templateServiceMock
            .Setup(x => x.DownloadTemplates(It.IsAny<string>()))
            .Returns(Task.FromResult<IList<byte[]>>(new List<byte[]>()));

        var contentAccessKey = "content_access_key";

        var result = await _sut.DownloadGuaranteedTransferQuote(contentAccessKey);

        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("EDMS preIndex failed: EDMS Preindex error")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()
            ),
            Times.Once);

        result.Should().BeOfType<BadRequestObjectResult>();
        (result as BadRequestObjectResult).Value.Should().BeOfType<ApiError>();
    }


    public async Task DownloadGuaranteedTransferQuote_ReturnsBadRequest_WheCalculationsClientHardQuoteFails()
    {
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.None);

        _calculationsClientMock
            .Setup(x => x.GetGuaranteedTransfer(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Left<Error, string>("GetGuaranteedTransfer Failed"));


        var contentAccessKey = "content_access_key";

        var result = await _sut.DownloadGuaranteedTransferQuote(contentAccessKey);

        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to lock transfer quote. Error: GetGuaranteedTransfer Failed")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()
            ),
            Times.Once);

        result.Should().BeOfType<BadRequestObjectResult>();
        (result as BadRequestObjectResult).Value.Should().BeOfType<ApiError>();
    }


    public async Task DownloadGuaranteedTransferQuote_ReturnsBadRequest_WhenPdfGenerateMergePdfsReturnsEmptyByteArray()
    {
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.None);

        _calculationsClientMock
            .Setup(x => x.GetGuaranteedTransfer(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Right<Error, string>("test"));

        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferCalculation>.Some(new TransferCalculation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), DateTimeOffset.UtcNow)));

        _calculationsRedisCacheMock
            .Setup(x => x.ClearRetirementDateAges(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _awsClientMock
            .Setup(x => x.File(It.IsAny<string>()))
            .ReturnsAsync(Right<Error, MemoryStream>(new MemoryStream()));

        _edmsClientMock
            .Setup(x => x.PreindexDocument(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(Right<PreindexError, PreindexResponse>(new PreindexResponse { ImageId = 1, BatchNumber = 1 }));

        _calculationsClientMock
            .Setup(x => x.TransferEventType(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(() => TryAsync(async () =>
            {
                return new TypeResponse { Type = "TestType" };
            }));

        var datesResponse = new RetirementDatesAgesResponse()
        {
            HasLockedInTransferQuote = true,
            LockedInTransferQuoteSeqno = 1,
        };
        _calculationsClientMock
           .Setup(x => x.RetirementDatesAges(It.IsAny<string>(), It.IsAny<string>()))
           .Returns(async () => datesResponse);

        _calculationHistoryRepositoryMock
            .Setup(x => x.FindByEventTypeAndSeqNumber(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(Option<CalculationHistory>.Some(new CalculationHistory("0003994", "RBS", "someEvent", 1, null, null)));

        _mdpDbUnitOfWorkMock
            .Setup(x => x.Commit())
            .Returns(Task.CompletedTask);

        _uowMock
            .Setup(x => x.Commit())
            .Returns(Task.CompletedTask);

        _templateServiceMock
            .Setup(x => x.DownloadTemplates(It.IsAny<string>()))
            .Returns(Task.FromResult<IList<byte[]>>(new List<byte[]>()));

        _pdfGeneratorMock
            .Setup(x => x.MergePdfs(
                It.IsAny<byte[]>(),
                It.IsAny<PdfDocument>()))
            .ReturnsAsync(new byte[0]);

        var transferJourney = new TransferJourneyBuilder().Build();
        var transferCalculation = CreateTransferCalculation();
        var dummyFile = CreateDummyFile();

        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(transferJourney));

        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferCalculation>.Some(transferCalculation));

        transferJourney.SaveTransferSummaryImageId(1);
        _edmsClientMock
            .Setup(x => x.GetDocumentOrError(It.IsAny<int>()))
            .ReturnsAsync(Right<Error, Stream>(dummyFile.stream));

        var contentAccessKey = "content_access_key";

        var result = await _sut.DownloadGuaranteedTransferQuote(contentAccessKey);

        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to generate merged PDF. ")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()
            ),
            Times.Once);

        result.Should().BeOfType<BadRequestObjectResult>();
        (result as BadRequestObjectResult).Value.Should().BeOfType<ApiError>();
    }


    public async Task DownloadGuaranteedTransferQuote_ReturnsBadRequest_WhenCalculationsClientRetirementDatesAgesReturnsNotSuccess()
    {
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.None);

        _calculationsClientMock
            .Setup(x => x.GetGuaranteedTransfer(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Right<Error, string>("test"));

        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferCalculation>.Some(new TransferCalculation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), DateTimeOffset.UtcNow)));

        _calculationsRedisCacheMock
            .Setup(x => x.ClearRetirementDateAges(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _awsClientMock
            .Setup(x => x.File(It.IsAny<string>()))
            .ReturnsAsync(Right<Error, MemoryStream>(new MemoryStream()));

        _edmsClientMock
            .Setup(x => x.PreindexDocument(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(Right<PreindexError, PreindexResponse>(new PreindexResponse { ImageId = 1, BatchNumber = 1 }));

        _calculationsClientMock
            .Setup(x => x.TransferEventType(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(() => TryAsync(async () =>
            {
                return new TypeResponse { Type = "TestType" };
            }));

        var failureResponse = new Result<RetirementDatesAgesResponse>(new Exception());
        _calculationsClientMock
           .Setup(x => x.RetirementDatesAges(It.IsAny<string>(), It.IsAny<string>()))
           .Returns(async () => failureResponse);

        _calculationHistoryRepositoryMock
            .Setup(x => x.FindByEventTypeAndSeqNumber(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(Option<CalculationHistory>.Some(new CalculationHistory("0003994", "RBS", "someEvent", 1, null, null)));

        _mdpDbUnitOfWorkMock
            .Setup(x => x.Commit())
            .Returns(Task.CompletedTask);

        _uowMock
            .Setup(x => x.Commit())
            .Returns(Task.CompletedTask);

        _templateServiceMock
            .Setup(x => x.DownloadTemplates(It.IsAny<string>()))
            .Returns(Task.FromResult<IList<byte[]>>(new List<byte[]>()));

        _pdfGeneratorMock
            .Setup(x => x.MergePdfs(
                It.IsAny<byte[]>(),
                It.IsAny<PdfDocument>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes("This is a test PDF stream."));

        var transferJourney = new TransferJourneyBuilder().Build();
        var transferCalculation = CreateTransferCalculation();
        var dummyFile = CreateDummyFile();

        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(transferJourney));

        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferCalculation>.Some(transferCalculation));

        transferJourney.SaveTransferSummaryImageId(1);
        _edmsClientMock
            .Setup(x => x.GetDocumentOrError(It.IsAny<int>()))
            .ReturnsAsync(Right<Error, Stream>(dummyFile.stream));

        var contentAccessKey = "content_access_key";
        var result = await _sut.DownloadGuaranteedTransferQuote(contentAccessKey);


        _logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to retrieve LockedInTransferQuoteSeqno value from calc api.")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()
            ),
            Times.Once);
        result.Should().BeOfType<BadRequestObjectResult>();
        (result as BadRequestObjectResult).Value.Should().BeOfType<ApiError>();
    }

    public async Task DownloadGuaranteedTransferQuote_ReturnsBadRequest_WhenCalculationsClientRetirementDatesAgesReturnsNullLockedInTransferQuoteSeqno()
    {
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.None);

        _calculationsClientMock
            .Setup(x => x.GetGuaranteedTransfer(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Right<Error, string>("test"));

        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferCalculation>.Some(new TransferCalculation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), DateTimeOffset.UtcNow)));

        _calculationsRedisCacheMock
            .Setup(x => x.ClearRetirementDateAges(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _awsClientMock
            .Setup(x => x.File(It.IsAny<string>()))
            .ReturnsAsync(Right<Error, MemoryStream>(new MemoryStream()));

        _edmsClientMock
            .Setup(x => x.PreindexDocument(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(Right<PreindexError, PreindexResponse>(new PreindexResponse { ImageId = 1, BatchNumber = 1 }));

        _calculationsClientMock
            .Setup(x => x.TransferEventType(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(() => TryAsync(async () =>
            {
                return new TypeResponse { Type = "TestType" };
            }));

        var datesResponse = new RetirementDatesAgesResponse()
        {
            HasLockedInTransferQuote = true,
            LockedInTransferQuoteSeqno = null,
        };

        _calculationsClientMock
           .Setup(x => x.RetirementDatesAges(It.IsAny<string>(), It.IsAny<string>()))
           .Returns(async () => datesResponse);

        _calculationHistoryRepositoryMock
            .Setup(x => x.FindByEventTypeAndSeqNumber(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(Option<CalculationHistory>.Some(new CalculationHistory("0003994", "RBS", "someEvent", 1, null, null)));

        _mdpDbUnitOfWorkMock
            .Setup(x => x.Commit())
            .Returns(Task.CompletedTask);

        _uowMock
            .Setup(x => x.Commit())
            .Returns(Task.CompletedTask);

        _templateServiceMock
            .Setup(x => x.DownloadTemplates(It.IsAny<string>()))
            .Returns(Task.FromResult<IList<byte[]>>(new List<byte[]>()));

        _pdfGeneratorMock
            .Setup(x => x.MergePdfs(
                It.IsAny<byte[]>(),
                It.IsAny<PdfDocument>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes("This is a test PDF stream."));

        var transferJourney = new TransferJourneyBuilder().Build();
        var transferCalculation = CreateTransferCalculation();
        var dummyFile = CreateDummyFile();

        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(transferJourney));

        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferCalculation>.Some(transferCalculation));

        transferJourney.SaveTransferSummaryImageId(1);
        _edmsClientMock
            .Setup(x => x.GetDocumentOrError(It.IsAny<int>()))
            .ReturnsAsync(Right<Error, Stream>(dummyFile.stream));

        var contentAccessKey = "content_access_key";
        var result = await _sut.DownloadGuaranteedTransferQuote(contentAccessKey);


        _logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("LockedInTransferQuoteSeqno value is null.")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()
            ),
            Times.Once);
        result.Should().BeOfType<BadRequestObjectResult>();
        (result as BadRequestObjectResult).Value.Should().BeOfType<ApiError>();
    }

    public async Task DownloadGuaranteedTransferQuote_ReturnsBadRequest_WhenCalculationsHistoryRepoFindLatestIsNone()
    {
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.None);

        _calculationsClientMock
            .Setup(x => x.GetGuaranteedTransfer(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Right<Error, string>("test"));

        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferCalculation>.Some(new TransferCalculation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), DateTimeOffset.UtcNow)));

        _calculationsRedisCacheMock
            .Setup(x => x.ClearRetirementDateAges(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _awsClientMock
            .Setup(x => x.File(It.IsAny<string>()))
            .ReturnsAsync(Right<Error, MemoryStream>(new MemoryStream()));

        _edmsClientMock
            .Setup(x => x.PreindexDocument(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(Right<PreindexError, PreindexResponse>(new PreindexResponse { ImageId = 1, BatchNumber = 1 }));

        _calculationsClientMock
            .Setup(x => x.TransferEventType(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(() => TryAsync(async () =>
            {
                return new TypeResponse { Type = "TestType" };
            }));

        var datesResponse = new RetirementDatesAgesResponse()
        {
            HasLockedInTransferQuote = true,
            LockedInTransferQuoteSeqno = 1,
        };

        _calculationsClientMock
           .Setup(x => x.RetirementDatesAges(It.IsAny<string>(), It.IsAny<string>()))
           .Returns(async () => datesResponse);

        _calculationHistoryRepositoryMock
            .Setup(x => x.FindByEventTypeAndSeqNumber(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(Option<CalculationHistory>.None);

        _mdpDbUnitOfWorkMock
            .Setup(x => x.Commit())
            .Returns(Task.CompletedTask);

        _uowMock
            .Setup(x => x.Commit())
            .Returns(Task.CompletedTask);

        _templateServiceMock
            .Setup(x => x.DownloadTemplates(It.IsAny<string>()))
            .Returns(Task.FromResult<IList<byte[]>>(new List<byte[]>()));

        _pdfGeneratorMock
            .Setup(x => x.MergePdfs(
                It.IsAny<byte[]>(),
                It.IsAny<PdfDocument>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes("This is a test PDF stream."));

        var transferJourney = new TransferJourneyBuilder().Build();
        var transferCalculation = CreateTransferCalculation();
        var dummyFile = CreateDummyFile();

        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(transferJourney));

        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferCalculation>.Some(transferCalculation));

        transferJourney.SaveTransferSummaryImageId(1);
        _edmsClientMock
            .Setup(x => x.GetDocumentOrError(It.IsAny<int>()))
            .ReturnsAsync(Right<Error, Stream>(dummyFile.stream));

        var contentAccessKey = "content_access_key";
        var result = await _sut.DownloadGuaranteedTransferQuote(contentAccessKey);


        _mdpDbUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Calc history record cannnot be found")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()
            ),
            Times.Once);
        result.Should().BeOfType<BadRequestObjectResult>();
        (result as BadRequestObjectResult).Value.Should().BeOfType<ApiError>();
    }

    public async Task DownloadGuaranteedTransferQuote_ReturnsBadRequest_WhenEdmsClientGetDocumentOrErrorIsLeft()
    {
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.None);

        _calculationsClientMock
            .Setup(x => x.GetGuaranteedTransfer(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Right<Error, string>("test"));

        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferCalculation>.Some(new TransferCalculation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), DateTimeOffset.UtcNow)));

        _calculationsRedisCacheMock
            .Setup(x => x.ClearRetirementDateAges(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _awsClientMock
            .Setup(x => x.File(It.IsAny<string>()))
            .ReturnsAsync(Right<Error, MemoryStream>(new MemoryStream()));

        _edmsClientMock
            .Setup(x => x.PreindexDocument(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(Right<PreindexError, PreindexResponse>(new PreindexResponse { ImageId = 1, BatchNumber = 1 }));

        _calculationsClientMock
            .Setup(x => x.TransferEventType(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(() => TryAsync(async () =>
            {
                return new TypeResponse { Type = "TestType" };
            }));

        var datesResponse = new RetirementDatesAgesResponse()
        {
            HasLockedInTransferQuote = true,
            LockedInTransferQuoteSeqno = 1,
        };

        _calculationsClientMock
           .Setup(x => x.RetirementDatesAges(It.IsAny<string>(), It.IsAny<string>()))
           .Returns(async () => datesResponse);

        _calculationHistoryRepositoryMock
            .Setup(x => x.FindByEventTypeAndSeqNumber(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(Option<CalculationHistory>.Some(new CalculationHistory("0003994", "RBS", "someEvent", 1, null, null)));

        _mdpDbUnitOfWorkMock
            .Setup(x => x.Commit())
            .Returns(Task.CompletedTask);

        _uowMock
            .Setup(x => x.Commit())
            .Returns(Task.CompletedTask);

        _templateServiceMock
            .Setup(x => x.DownloadTemplates(It.IsAny<string>()))
            .Returns(Task.FromResult<IList<byte[]>>(new List<byte[]>()));

        _pdfGeneratorMock
            .Setup(x => x.MergePdfs(
                It.IsAny<byte[]>(),
                It.IsAny<PdfDocument>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes("This is a test PDF stream."));

        var transferJourney = new TransferJourneyBuilder().Build();
        var transferCalculation = CreateTransferCalculation();
        var dummyFile = CreateDummyFile();

        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(transferJourney));

        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferCalculation>.Some(transferCalculation));

        transferJourney.SaveTransferSummaryImageId(1);
        _edmsClientMock
            .Setup(x => x.GetDocumentOrError(It.IsAny<int>()))
            .ReturnsAsync(Left<Error, Stream>(Error.New("EDMS Failed")));

        var contentAccessKey = "content_access_key";
        var result = await _sut.DownloadGuaranteedTransferQuote(contentAccessKey);

        result.Should().BeOfType<BadRequestObjectResult>();
        (result as BadRequestObjectResult).Value.Should().BeOfType<ApiError>();
    }


    public async Task SubmitStep_ReturnsNoContent_WhenJourneyExistsAndSubmitStepSucceeds()
    {
        var journey = new TransferJourneyBuilder()
            .CurrentPageKey("current_page_test")
            .NextPageKey("next_page_test")
            .BuildWithSteps();

        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(journey));

        _mdpDbUnitOfWorkMock
            .Setup(x => x.Commit())
            .Returns(Task.CompletedTask);

        var request = new SubmitTransferStepRequest
        {
            CurrentPageKey = "current_page_test",
            NextPageKey = "next_page_test"
        };
        var result = await _sut.SubmitStep(request);
        result.Should().BeOfType<NoContentResult>();
    }

    public async Task SubmitStep_ReturnsBadRequest_WhenJourneyExistsAndSubmitStepFails()
    {
        var journey = new TransferJourneyBuilder().BuildWithSteps();

        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(journey));

        var request = new SubmitTransferStepRequest
        {
            CurrentPageKey = "current_page_test1",
            NextPageKey = "next_page_test1"
        };
        var result = await _sut.SubmitStep(request);
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    public async Task SubmitStep_ReturnsBadRequest_WhenJourneyNotFound()
    {
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.None);

        var request = new SubmitTransferStepRequest
        {
            CurrentPageKey = "current_page_test",
            NextPageKey = "next_page_test"
        };
        var result = await _sut.SubmitStep(request);
        result.Should().BeOfType<BadRequestObjectResult>();
        var errorResponse = ((BadRequestObjectResult)result).Value as ApiError;
        errorResponse.Errors[0].Message.Should().Be("Member did not start transfer journey yet");
    }

    public async Task SubmitQuestionStep_ReturnsNoContent_WhenJourneyIsFoundAndStepIsSubmitted()
    {
        var journey = new TransferJourneyBuilder()
            .CurrentPageKey("current")
            .NextPageKey("TestCurrentPageKey")
            .BuildWithSteps();
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(journey));

        _mdpDbUnitOfWorkMock
            .Setup(x => x.Commit())
            .Returns(Task.CompletedTask);

        var request = new SubmitTransferQuestionStepRequest { CurrentPageKey = "TestCurrentPageKey", NextPageKey = "TestNextPageKey", QuestionKey = "TestQuestionKey", AnswerKey = "TestAnswerKey" };
        var result = await _sut.SubmitQuestionStep(request);
        result.Should().BeOfType<NoContentResult>();
    }

    public async Task SubmitQuestionStep_ReturnsBadRequest_WhenJourneyIsNotFound()
    {
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.None);

        var request = new SubmitTransferQuestionStepRequest { CurrentPageKey = "TestCurrentPageKey", NextPageKey = "TestNextPageKey", QuestionKey = "TestQuestionKey", AnswerKey = "TestAnswerKey" };
        var result = await _sut.SubmitQuestionStep(request);
        result.Should().BeOfType<BadRequestObjectResult>();
        var errorResponse = ((BadRequestObjectResult)result).Value as ApiError;
        errorResponse.Errors[0].Message.Should().Be("Member did not start transfer journey yet");
    }

    public async Task SubmitQuestionStep_ReturnsBadRequest_WhenJourneyIsFoundButStepCannotBeSubmitted()
    {
        var journey = new TransferJourneyBuilder()
            .CurrentPageKey("TestCurrentPageKey")
            .NextPageKey("TestNextPageKey")
            .BuildWithSteps();
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(journey));

        var request = new SubmitTransferQuestionStepRequest { CurrentPageKey = "TestCurrentPageKey", NextPageKey = "TestNextPageKey", QuestionKey = "TestQuestionKey", AnswerKey = "TestAnswerKey" };
        var result = await _sut.SubmitQuestionStep(request);
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    public async Task PreviousStep_ReturnsOk_WhenJourneyIsFoundAndPreviousStepExists()
    {
        var currentPageKey = "TestCurrentPageKey";
        var previousPageKey = "TestPreviousPageKey";
        var journey = new TransferJourneyBuilder()
            .CurrentPageKey(previousPageKey)
            .NextPageKey(currentPageKey)
            .BuildWithSteps();
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(journey));

        var result = await _sut.PreviousStep(currentPageKey);
        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as TransferPreviousStepResponse;
        response.PreviousPageKey.Should().Be(previousPageKey);
    }

    public async Task PreviousStep_ReturnsNotFound_WhenJourneyIsNotFound()
    {
        var currentPageKey = "TestCurrentPageKey";
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.None);

        var result = await _sut.PreviousStep(currentPageKey);
        result.Should().BeOfType<NotFoundObjectResult>();
        var errorResponse = ((NotFoundObjectResult)result).Value as ApiError;
        errorResponse.Errors[0].Code.Should().Be("previous_page_key_not_found");
    }

    public async Task PreviousStep_ReturnsNotFound_WhenJourneyIsFoundButPreviousStepDoesNotExist()
    {
        var currentPageKey = "TestCurrentPageKey";
        var nonExistingPageKey = "NonExistingPageKey";
        var journey = new TransferJourneyBuilder()
            .CurrentPageKey(currentPageKey)
            .NextPageKey("TestNextPageKey")
            .BuildWithSteps();
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(journey));

        var result = await _sut.PreviousStep(nonExistingPageKey);
        result.Should().BeOfType<NotFoundObjectResult>();
        var errorResponse = ((NotFoundObjectResult)result).Value as ApiError;
        errorResponse.Errors[0].Code.Should().Be("previous_page_key_not_found");
    }

    public async Task CheckJourneyIntegrity_ReturnsOk_WhenJourneyIsFound()
    {
        // TODO: I'm not clear how it works, so just copied from existing tests, might not even make sense in current conntext, needs rechecking
        var pageKey = "TestPageKey";
        var redirectPageKey = "RedirectPageKey";
        var journey = new TransferJourneyBuilder()
            .CurrentPageKey("current")
            .NextPageKey("transfer_start_1")
            .BuildWithSteps();
        journey.TrySubmitStep("transfer_start_1", "next_page_test", DateTimeOffset.UtcNow, "q1", "a1");
        journey.TrySubmitStep("next_page_test", redirectPageKey, DateTimeOffset.UtcNow, "q2", "a2");
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(journey));

        var result = await _sut.CheckJourneyIntegrity(pageKey);
        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as TransferIntegrityResponse;
        response.RedirectStepPageKey.Should().Be(redirectPageKey);
    }

    public async Task CheckJourneyIntegrity_ReturnsNotFound_WhenJourneyIsNotFound()
    {
        var pageKey = "TestPageKey";
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.None);

        var result = await _sut.CheckJourneyIntegrity(pageKey);
        result.Should().BeOfType<NotFoundObjectResult>();
        var errorResponse = ((NotFoundObjectResult)result).Value as ApiError;
        errorResponse.Errors[0].Message.Should().Be("Not found");
    }

    public async Task SubmitContact_ReturnsNoContent_WhenContactSubmittedSuccessfully()
    {
        var journey = new TransferJourneyBuilder().Build();
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(journey));

        var request = new TransferJourneyContactRequest
        {
            Name = "Test Tester",
            CompanyName = "Test Company",
            Email = "Test.Tester@example.com",
            PhoneNumber = "7544111111",
            PhoneCode = "44",
            Type = "TestType"
        };

        var response = new TransferJourneyContact(
            request.Name,
            null,
            request.CompanyName,
            Email.Create(request.Email).Right(),
            Phone.Create(request.PhoneCode, request.PhoneNumber).Right(),
            null,
            null,
            DateTimeOffset.Now);

        _transferJourneyContactFactoryMock
            .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Email>(), It.IsAny<Phone>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .Returns(Right<Error, TransferJourneyContact>(response));

        var result = await _sut.SubmitContact(request);
        result.Should().BeOfType<NoContentResult>();
    }

    public async Task SubmitContact_ReturnsBadRequest_WhenEmailIsInvalid()
    {
        var journey = new TransferJourneyBuilder().Build();
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(journey));

        var request = new TransferJourneyContactRequest
        {
            Name = "Test Tester",
            CompanyName = "Test Company",
            Email = "invalid email",
            PhoneNumber = "7544111111",
            PhoneCode = "44",
            Type = "TestType"
        };

        var result = await _sut.SubmitContact(request);
        result.Should().BeOfType<BadRequestObjectResult>();
        var errorResponse = ((BadRequestObjectResult)result).Value as ApiError;
        errorResponse.Errors[0].Message.Should().Be("Invalid email address format.");
    }

    public async Task SubmitContact_ReturnsBadRequest_WhenPhoneIsInvalid()
    {
        var journey = new TransferJourneyBuilder().Build();
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(journey));

        var request = new TransferJourneyContactRequest
        {
            Name = "Test Tester",
            CompanyName = "Test Company",
            Email = "Test.Tester@example.com",
            PhoneNumber = "invalid-number",
            PhoneCode = "44",
            Type = "TestType"
        };

        var result = await _sut.SubmitContact(request);
        result.Should().BeOfType<BadRequestObjectResult>();
        var errorResponse = ((BadRequestObjectResult)result).Value as ApiError;
        errorResponse.Errors[0].Message.Should().Be("Invalid number format. It must contain only numbers.");
    }

    public async Task SubmitContact_ReturnsBadRequest_WhenContactCannotBeCreated()
    {
        var journey = new TransferJourneyBuilder().Build();
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(journey));

        var request = new TransferJourneyContactRequest
        {
            Name = "Test Tester",
            CompanyName = "PLLgf5ytGAsQOyz3zupijCg3bY5dmVONCPhmA1rARt7hPf8UYUf",
            Email = "Test.Tester@example.com",
            PhoneNumber = "7544111111",
            PhoneCode = "44",
            Type = "TestType"
        };

        _transferJourneyContactFactoryMock
            .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<string>(), request.CompanyName, It.IsAny<Email>(), It.IsAny<Phone>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .Returns(Left<Error, TransferJourneyContact>(Error.New("Company name must be up to 50 characters length.")));

        var result = await _sut.SubmitContact(request);
        result.Should().BeOfType<BadRequestObjectResult>();
        var errorResponse = ((BadRequestObjectResult)result).Value as ApiError;
        errorResponse.Errors[0].Message.Should().Be("Company name must be up to 50 characters length.");
    }

    public async Task SubmitContact_ReturnsNotFound_WhenJourneyNotFound()
    {
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.None);
        var request = new TransferJourneyContactRequest
        {
            Name = "Test Tester",
            CompanyName = "Test Company",
            Email = "Test.Tester@example.com",
            PhoneNumber = "7544111111",
            PhoneCode = "44",
            Type = "TestType"
        };

        var result = await _sut.SubmitContact(request);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task SubmitContactAddress_ReturnsBadRequest_WhenAddressIsInvalid()
    {
        var journey = new TransferJourneyBuilder().Build();
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(journey));

        var request = new TransferJourneyContactAddressRequest
        {
            Line1 = "Test 1",
            Line2 = "Test 2",
            Line3 = "Test 3",
            Line4 = "Test 4",
            Line5 = "Test 5",
            Country = "United Kingdom",
            CountryCode = "Test",
            PostCode = "1234"
        };

        var result = await _sut.SubmitContactAddress(request);
        result.Should().BeOfType<BadRequestObjectResult>();
        var errorResponse = ((BadRequestObjectResult)result).Value as ApiError;
        errorResponse.Errors[0].Message.Should().Be("CountryCode must be up to 3 characters length.");
    }

    public async Task SubmitContactAddress_ReturnsNotFound_WhenJourneyNotFound()
    {
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.None);

        var request = new TransferJourneyContactAddressRequest
        {
            Line1 = "Test 1",
            Line2 = "Test 2",
            Line3 = "Test 3",
            Line4 = "Test 4",
            Line5 = "Test 5",
            Country = "United Kingdom",
            CountryCode = "GB",
            PostCode = "1234"
        };

        var result = await _sut.SubmitContactAddress(request);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task SubmitContactAddress_ReturnsNoContent_WhenRequestIsSuccessful()
    {
        var journey = new TransferJourneyBuilder().Build();
        var transferJourneyContact = new TransferJourneyContactFactory();
        var contact = transferJourneyContact.Create(
            "tesName",
            "Advisor name",
            "testCompanyName",
            Email.Create("test@gmail.com").Right(),
            Phone.Create("370", "67878788").Right(),
            "Ifa",
            null,
            DateTimeOffset.UtcNow).Right();
        journey.SubmitContact(contact);
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(journey));

        var request = new TransferJourneyContactAddressRequest
        {
            Line1 = "Test 1",
            Line2 = "Test 2",
            Line3 = "Test 3",
            Line4 = "Test 4",
            Line5 = "Test 5",
            Country = "United Kingdom",
            CountryCode = "GB",
            PostCode = "1234",
            Type = "Ifa"
        };

        _mdpDbUnitOfWorkMock.Setup(x => x.Commit()).Returns(Task.CompletedTask);

        var result = await _sut.SubmitContactAddress(request);
        result.Should().BeOfType<NoContentResult>();
    }

    public async Task SubmitContactAddress_ReturnsBadRequest_WhenContactAddressCannotBeSubmitted()
    {
        var journey = new TransferJourneyBuilder().Build();
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(journey));

        var request = new TransferJourneyContactAddressRequest
        {
            Line1 = "Test 1",
            Line2 = "Test 2",
            Line3 = "Test 3",
            Line4 = "Test 4",
            Line5 = "Test 5",
            Country = "United Kingdom",
            CountryCode = "GB",
            PostCode = "1234",
        };

        _mdpDbUnitOfWorkMock.Setup(x => x.Commit()).Returns(Task.CompletedTask);

        var result = await _sut.SubmitContactAddress(request);
        result.Should().BeOfType<BadRequestObjectResult>();
        var errorResponse = ((BadRequestObjectResult)result).Value as ApiError;
        errorResponse.Errors[0].Message.Should().Be("Contact details must be submitted, before submitting its address details.");
    }

    public async Task GetTransferApplicationStatus_ReturnsNotFound_WhenMemberNotFound()
    {
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.None);

        var result = await _sut.GetTransferApplicationStatus();
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task GetTransferApplicationStatus_ReturnsOk_WhenMemberFound()
    {
        var member = new MemberBuilder().Build();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.Some(member));

        var transferCalculation = CreateTransferCalculation();
        transferCalculation.LockTransferQoute();
        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferCalculation>.Some(transferCalculation));

        var result = await _sut.GetTransferApplicationStatus();
        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as TransferApplicationStatusResponse;
        response.Status.Should().Be(TransferApplicationStatus.StartedTA);
    }

    public async Task SubmitTaStatus_ReturnsNotFound_WhenTransferJourneyNotFound()
    {
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.None);

        var request = new TransferApplicationStatusRequest { Status = TransferApplicationStatus.SubmitStarted };

        var result = await _sut.SubmitTaStatus(request);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task SubmitTaStatus_ReturnsNotFound_WhenTransferCalculationNotFound()
    {
        var transferJourney = new TransferJourneyBuilder().Build();
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(transferJourney));

        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferCalculation>.None);

        var request = new TransferApplicationStatusRequest { Status = TransferApplicationStatus.SubmitStarted };

        var result = await _sut.SubmitTaStatus(request);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task SubmitTaStatus_ReturnsBadRequest_WhenStatusIsInvalid()
    {
        var transferJourney = new TransferJourneyBuilder().Build();
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(transferJourney));

        var transferCalculation = CreateTransferCalculation();
        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferCalculation>.Some(transferCalculation));

        var request = new TransferApplicationStatusRequest { Status = TransferApplicationStatus.StartedTA };

        var result = await _sut.SubmitTaStatus(request);
        result.Should().BeOfType<BadRequestObjectResult>();
        var errorResponse = ((BadRequestObjectResult)result).Value as ApiError;
        errorResponse.Errors[0].Message.Should().Be("Transfer journey must be started");
    }

    public async Task SubmitTaStatus_ReturnsNoContent_WhenRequestIsSuccessful()
    {
        var transferJourney = new TransferJourneyBuilder().Build();
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(transferJourney));

        var transferCalculation = CreateTransferCalculation();
        transferCalculation.LockTransferQoute();
        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferCalculation>.Some(transferCalculation));

        var request = new TransferApplicationStatusRequest { Status = TransferApplicationStatus.StartedTA };

        var result = await _sut.SubmitTaStatus(request);
        result.Should().BeOfType<NoContentResult>();
    }

    [Input(false, false, typeof(NotFoundObjectResult))]
    [Input(true, true, typeof(OkObjectResult))]
    [Input(true, false, typeof(OkObjectResult))]
    public async Task TransferApplication_ReturnsCorrectStatusCoded(bool transferJourneyExist, bool isTransferQuoteNull, Type expectedType)
    {
        var calc = isTransferQuoteNull ? new TransferCalculation("RBS", "1111124", null, DateTimeOffset.UtcNow) : CreateTransferCalculation(
            @"{""guaranteeDate"":""2023-04-04T00:00:00+00:00"",""guaranteePeriod"":""P3M"",""replyByDate"":""2023-03-14T00:00:00+00:00"",
            ""totalPensionAtDOL"":0,""maximumResidualPension"":0,""minimumResidualPension"":0,
            ""transferValues"":{""totalGuaranteedTransferValue"":374409.71,""totalNonGuaranteedTransferValue"":10687.18,
                ""minimumPartialTransferValue"":0,""totalTransferValue"":385096.89},""isGuaranteedQuote"":false,
                ""originalEffectiveDate"":""2023-01-04T00:00:00+00:00""}");
        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferCalculation>.Some(calc));

        var member = new MemberBuilder().Build();
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.Some(member));
        var transferJourney = transferJourneyExist ? new TransferJourneyBuilder().Build() : Option<TransferJourney>.None;
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(transferJourney);

        _calculationsParserMock
                .Setup(x => x.GetTransferQuote(It.IsAny<string>()))
                .Returns(new TransferQuote(JsonSerializer.Deserialize<TransferQuoteDto>(TestData.TransferQuoteJson, SerialiationBuilder.Options())));

        var result = await _sut.TransferApplication();
        result.Should().BeOfType(expectedType);
    }

    public async Task Submit_ReturnsNotFound_WhenTransferJourneyNotFound()
    {
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.FromResult(Option<TransferJourney>.None));

        var request = new TransferJourneySubmitRequest { ContentAccessKey = "TestContentAccessKey" };
        var result = await _sut.Submit(request);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task Submit_ReturnsBadRequest_WhenMemberNotFound()
    {
        var transferJourney = new TransferJourneyBuilder().Build();
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(transferJourney));

        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(Option<Member>.None);

        var request = new TransferJourneySubmitRequest { ContentAccessKey = "TestContentAccessKey" };
        var result = await _sut.Submit(request);
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Input(true, false, false, typeof(BadRequestObjectResult))]
    [Input(false, true, false, typeof(BadRequestObjectResult))]
    [Input(false, false, true, typeof(BadRequestObjectResult))]
    [Input(false, false, false, typeof(NoContentResult))]
    public async Task Submit_ReturnsCorrectHttpStatusCode(bool isMemberNone, bool isUploadDocumentError, bool isPostindexError, Type type)
    {
        var transferCalculation = CreateTransferCalculation();

        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.FromResult(Option<TransferCalculation>.Some(transferCalculation)));

        var transferJourney = new TransferJourneyBuilder().Build();
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(transferJourney));

        var member = new MemberBuilder().Build();
        
        _transferV2TemplateMock
            .Setup(x => x.RenderHtml(It.IsAny<string>(), It.IsAny<TransferJourney>(), It.IsAny<Member>(), It.IsAny<TransferQuote>(), It.IsAny<TransferApplicationStatus>(), It.IsAny<DateTimeOffset>(), It.IsAny<IEnumerable<UploadedDocument>>(), Option<Calculation>.None))
            .ReturnsAsync("<div></div>");
        _memberRepositoryMock
            .Setup(x => x.FindMember(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(isMemberNone ? Option<Member>.None : Option<Member>.Some(member));

        _contentClientMock
            .Setup(x => x.FindTemplate("transfer_v2_application_submission_email", It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new TemplateResponse { HtmlBody = "<div></div>", HtmlHeader = "<div></div>", HtmlFooter = "<div></div>" });
        _contentClientMock
            .Setup(x => x.FindTemplate("transfer_v2_pdf", It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new TemplateResponse { HtmlBody = "<div></div>", HtmlHeader = "<div></div>", HtmlFooter = "<div></div>" });
        _transferCaseMock
            .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("123456");
        _journeyDocumentsRepositoryMock
            .Setup(x => x.List(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()))
            .ReturnsAsync(new List<UploadedDocument>());
        _edmsClientMock
            .Setup(x => x.UploadDocument(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(isUploadDocumentError ? new DocumentUploadError { Message = "test" } : new DocumentUploadResponse { Uuid = It.IsAny<string>() });
        _documentFactoryProviderMock
            .Setup(x => x.GetFactory(DocumentType.TransferV2))
            .Returns(new RetirementDocumentFactory());
        Error? error = null;
        if (isPostindexError)
            error = Error.New("Test message");
        _edmsDocumentsIndexingMock
            .Setup(x => x.PostIndexTransferDocuments(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TransferJourney>(), It.IsAny<IList<UploadedDocument>>()))
            .ReturnsAsync(isPostindexError ? Error.New("Test message") : new List<(int, string)> { (123456, It.IsAny<string>()) });
        _smtpClientMock
            .Setup(x => x.SendWithAttachment(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MemoryStream>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var request = new TransferJourneySubmitRequest { ContentAccessKey = "TestContentAccessKey" };
        var result = await _sut.Submit(request);
        result.Should().BeOfType(type);
    }

    public async Task DeleteApplication_ReturnsNoContent_WhenRequestIsSuccessful()
    {
        var transferJourney = new TransferJourneyBuilder().Build();
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(transferJourney));

        var transferCalculation = CreateTransferCalculation();
        transferCalculation.LockTransferQoute();
        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferCalculation>.Some(transferCalculation));

        var result = await _sut.DeleteApplication();
        result.Should().BeOfType<NoContentResult>();
    }

    public async Task DeleteApplication_ReturnsNotFound_WhenTransferJourneyIsNotFound()
    {
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.None);

        var transferCalculation = CreateTransferCalculation();
        transferCalculation.LockTransferQoute();
        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferCalculation>.Some(transferCalculation));

        var result = await _sut.DeleteApplication();
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task DeleteApplication_ReturnsBadRequest_WhenTransferQouteIsNotLocked()
    {
        var transferJourney = new TransferJourneyBuilder().Build();
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(transferJourney));

        var transferCalculation = CreateTransferCalculation();
        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferCalculation>.Some(transferCalculation));

        var result = await _sut.DeleteApplication();
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    public async Task RemoveSteps_ReturnsNoContent_WhenRequestIsSuccessful()
    {
        var transferJourney = new TransferJourneyBuilder().BuildWithSteps();
        transferJourney.TrySubmitStep("transfer_start_1", "step2", DateTimeOffset.UtcNow.AddDays(-35));
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(transferJourney));

        var transferCalculation = new TransferCalculation("RBS", "1111124", "{}", DateTimeOffset.UtcNow);
        transferCalculation.LockTransferQoute();
        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferCalculation>.Some(transferCalculation));

        var result = await _sut.RemoveSteps("step2");
        result.Should().BeOfType<NoContentResult>();
    }

    public async Task RemoveSteps_ReturnsNotFound_WhenTransferJourneyIsNotFound()
    {
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.None);

        var transferCalculation = CreateTransferCalculation();
        transferCalculation.LockTransferQoute();
        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferCalculation>.Some(transferCalculation));

        var result = await _sut.RemoveSteps("tests-page-key");
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task GetPdf_ReturnsNotFound_WhenTransferJourneyNotFound()
    {
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.FromResult(Option<TransferJourney>.None));

        var result = await _sut.GetPdf("TestContentAccessKey");
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task GetPdf_ReturnsBadRequest_WhenFileNotFound()
    {
        var transferJourney = new TransferJourneyBuilder().Build();
        transferJourney.SaveTransferSummaryImageId(1);
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(transferJourney));

        _edmsClientMock
            .Setup(x => x.GetDocumentOrError(transferJourney.TransferSummaryImageId.Value))
            .ReturnsAsync(Left<Error, Stream>(Error.New("File download error")));

        var result = await _sut.GetPdf("TestContentAccessKey");
        var response = (result as BadRequestObjectResult).Value as ApiError;
        result.Should().BeOfType<BadRequestObjectResult>();
        response.Should().NotBeNull();
        response.Errors[0].Message.Should().Be("File download error");
    }

    public async Task GetPdf_ReturnsBadRequest_WhenSummaryImageIdIsNull()
    {
        var transferJourney = new TransferJourneyBuilder().Build();
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(transferJourney));

        var result = await _sut.GetPdf("TestContentAccessKey");
        var response = (result as BadRequestObjectResult).Value as ApiError;
        result.Should().BeOfType<BadRequestObjectResult>();
        response.Should().NotBeNull();
        response.Errors[0].Message.Should().Be("Transfer journey summary imageId does not exists");
    }

    public async Task GetPdf_ReturnsPdfFileResult()
    {
        var transferCalculation = CreateTransferCalculation();
        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.FromResult(Option<TransferCalculation>.Some(transferCalculation)));

        var transferJourney = new TransferJourneyBuilder().Build();
        transferJourney.SaveTransferSummaryImageId(1);
        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(transferJourney));

        _edmsClientMock
            .Setup(x => x.GetDocumentOrError(transferJourney.TransferSummaryImageId.Value))
            .ReturnsAsync(Right<Error, Stream>(new MemoryStream()));

        var result = await _sut.GetPdf("TestContentAccessKey");
        result.Should().BeOfType<FileContentResult>();
        (result as FileContentResult).ContentType.Should().Be("application/pdf");
        (result as FileContentResult).FileDownloadName.Should().Be("TransferApplicationSummary.pdf");
    }

    public async Task PensionTranchesV2_ReturnsPartialTransferResponse_WhenCalculationGenerated()
    {
        var mdpResponse = new PartialTransferResponse.MdpResponse
        {
            PensionTranchesResidual = new PartialTransferResponse.PensionTranchesResidualResponse { Total = 1 },
            TransferValuesFull = new PartialTransferResponse.TransferValuesFullResponse { NonGuaranteed = 2 },
        };

        _calculationsClientMock
           .Setup(x => x.PensionTranches(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()))
           .ReturnsAsync(Right<Error, PartialTransferResponse.MdpResponse>(mdpResponse));

        var result = await _sut.PensionTranchesV2(new PensionTranchesRequest { RequestedTransferValue = 1000 });
        result.Should().BeOfType<OkObjectResult>();
        var response = (result as OkObjectResult).Value as TransferValueResponseV2;
        response.PensionTranchesResidualTotal.Should().Be(1);
        response.NonGuaranteed.Should().Be(2);
    }

    public async Task PensionTranchesV2_ReturnsBadRequestForPartialTransfer_WhenCalculationThrowsError()
    {
        _calculationsClientMock
           .Setup(x => x.PensionTranches(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()))
           .ReturnsAsync(Left<Error, PartialTransferResponse.MdpResponse>(Error.New("Calculation error")));

        var result = await _sut.PensionTranchesV2(new PensionTranchesRequest { RequestedTransferValue = 1000 });
        var response = (result as BadRequestObjectResult).Value as ApiError;
        result.Should().BeOfType<BadRequestObjectResult>();
        response.Should().NotBeNull();
        response.Errors[0].Message.Should().Be("Calculation error");
    }

    public async Task TransferValuesV2_ReturnsTransferValuesResponse_WhenCalculationGenerated()
    {
        var mdpResponse = new PartialTransferResponse.MdpResponse
        {
            TransferValuesPartial = new PartialTransferResponse.TransferValuesPartialResponse { Total = 1 },
            TransferValuesFull = new PartialTransferResponse.TransferValuesFullResponse { Total = 2, NonGuaranteed = 3 },
        };

        _calculationsClientMock
           .Setup(x => x.TransferValues(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()))
           .ReturnsAsync(Right<Error, PartialTransferResponse.MdpResponse>(mdpResponse));

        var result = await _sut.TransferValuesV2(new TransferValuesRequest { RequestedResidualPension = 1000 });
        result.Should().BeOfType<OkObjectResult>();
        var response = (result as OkObjectResult).Value as PensionIncomeResponseV2;
        response.TransferValuesFullTotal.Should().Be(2);
        response.NonGuaranteed.Should().Be(3);
    }

    public async Task TransferValuesV2_ReturnsBadRequest_WhenCalculationThrowsError()
    {
        _calculationsClientMock
           .Setup(x => x.TransferValues(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()))
           .ReturnsAsync(Left<Error, PartialTransferResponse.MdpResponse>(Error.New("Calculation error")));

        var result = await _sut.TransferValuesV2(new TransferValuesRequest { RequestedResidualPension = 1000 });
        var response = (result as BadRequestObjectResult).Value as ApiError;
        result.Should().BeOfType<BadRequestObjectResult>();
        response.Should().NotBeNull();
        response.Errors[0].Message.Should().Be("Calculation error");
    }

    public async Task AddPostSubmissionDocuments_ReturnsNoContent_WhenAllDocumetsArePostIndexed()
    {
        var caseNumber = "caseNumber";

        var transferJourney = new TransferJourneyBuilder().Build();
        transferJourney.Submit(caseNumber, DateTimeOffset.Now);

        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(transferJourney));

        _journeyDocumentsHandlerServiceMock
            .Setup(x => x.PostIndex(It.IsAny<string>(), It.IsAny<string>(), caseNumber, MdpConstants.JourneyTypeTransferV2))
            .ReturnsAsync(Right<Error, Unit>(Unit.Default));

        var result = await _sut.AddPostSubmissionDocuments();
        result.Should().BeOfType<NoContentResult>();
    }

    public async Task AddPostSubmissionDocuments_ReturnsBadRequest_WhenDocumetsPostIndexFailed()
    {
        var caseNumber = "caseNumber";

        var transferJourney = new TransferJourneyBuilder().Build();
        transferJourney.Submit(caseNumber, DateTimeOffset.Now);

        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(transferJourney));

        _journeyDocumentsHandlerServiceMock
         .Setup(x => x.PostIndex(It.IsAny<string>(), It.IsAny<string>(), caseNumber, MdpConstants.JourneyTypeTransferV2))
         .ReturnsAsync(Left<Error, Unit>(Error.New("Error message: Postindexing failed.")));

        var result = await _sut.AddPostSubmissionDocuments();
        var response = (result as BadRequestObjectResult).Value as ApiError;
        result.Should().BeOfType<BadRequestObjectResult>();
        response.Errors[0].Message.Should().Be("Error message: Postindexing failed.");
    }

    public async Task AddPostSubmissionDocuments_ReturnsBadRequest_WhenNoDocumentsExists()
    {
        var caseNumber = "caseNumber";

        var transferJourney = new TransferJourneyBuilder().Build();
        transferJourney.Submit(caseNumber, DateTimeOffset.Now);

        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(transferJourney));

        _journeyDocumentsHandlerServiceMock
          .Setup(x => x.PostIndex(It.IsAny<string>(), It.IsAny<string>(), caseNumber, MdpConstants.JourneyTypeTransferV2))
          .ReturnsAsync(Left<Error, Unit>(Error.New("Cannot find any documents to postindex.")));

        var result = await _sut.AddPostSubmissionDocuments();
        var response = (result as BadRequestObjectResult).Value as ApiError;
        result.Should().BeOfType<BadRequestObjectResult>();
        response.Errors[0].Message.Should().Be("Cannot find any documents to postindex.");
    }

    [Input(false, "NameOfPlan", typeof(NotFoundObjectResult))]
    [Input(true, "NameOfPlanNameOfPlanNameOfPlanNameOfPlanNameOfPlanNameOfPlan", typeof(BadRequestObjectResult))]
    [Input(true, "NameOfPlan", typeof(NoContentResult))]
    public async Task SaveFlexibleBenefitsReturnsCorrectStatusCode(bool transferJourneyExists, string nameOfPlan, Type expectedType)
    {
        var transferJourney = new TransferJourneyBuilder().Build();

        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(transferJourneyExists ? Option<TransferJourney>.Some(transferJourney) : Option<TransferJourney>.None);

        var result = await _sut.SaveFlexibleBenefits(new FlexibleBenefitsRequest { DateOfPayment = DateTime.Now.Date, NameOfPlan = nameOfPlan, TypeOfPayment = "Test2" });
        result.Should().BeOfType(expectedType);
    }

    [Input(false, typeof(NotFoundObjectResult))]
    [Input(true, typeof(OkObjectResult))]
    public async Task FlexibleBenefitsReturnsCorrectStatusCode_AndResponseResult(bool transferJourneyExists, Type expectedType)
    {
        var now = DateTime.Now;
        var transferJourney = new TransferJourneyBuilder().Build();
        transferJourney.SaveFlexibleBenefits("Test1", "Test2", now.AddDays(-2), now);

        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(transferJourneyExists ? Option<TransferJourney>.Some(transferJourney) : Option<TransferJourney>.None);

        var result = await _sut.FlexibleBenefits();
        result.Should().BeOfType(expectedType);
        if (transferJourneyExists)
        {
            var response = (result as OkObjectResult).Value as FlexibleBenefitsResponse;
            response.NameOfPlan.Should().Be("Test1");
            response.TypeOfPayment.Should().Be("Test2");
            response.DateOfPayment.Should().Be(now.AddDays(-2));
        }
    }

    public async Task AddPostSubmissionDocuments_ReturnsBadRequest_WhenCaseNumberDoesNotExists()
    {
        var transferJourney = new TransferJourneyBuilder().Build();

        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferJourney>.Some(transferJourney));

        _journeyDocumentsHandlerServiceMock
          .Setup(x => x.PostIndex(It.IsAny<string>(), It.IsAny<string>(), null, MdpConstants.JourneyTypeTransferV2))
          .ReturnsAsync(Left<Error, Unit>(Error.New($"{MdpConstants.JourneyTypeTransferV2} case must be sumbitted.")));

        var result = await _sut.AddPostSubmissionDocuments();
        var response = (result as BadRequestObjectResult).Value as ApiError;
        result.Should().BeOfType<BadRequestObjectResult>();
        response.Errors[0].Message.Should().Be($"{MdpConstants.JourneyTypeTransferV2} case must be sumbitted.");
    }

    [Input(false, TransferApplicationStatus.SubmitStarted, "2023-06-14", typeof(NotFoundObjectResult))]
    [Input(true, TransferApplicationStatus.SubmitStarted, "2023-06-14", typeof(NoContentResult))]
    [Input(true, TransferApplicationStatus.NotStartedTA, "2023-06-14", typeof(BadRequestObjectResult))]
    public async Task SavePensionWiseReturnsCorrectStatusCode(bool transferJourneyExists, TransferApplicationStatus status, string pwDate, Type expectedType)
    {
        var transferJourney = new TransferJourneyBuilder().Build();

        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(transferJourneyExists ? Option<TransferJourney>.Some(transferJourney) : Option<TransferJourney>.None);

        var transferCalculation = CreateTransferCalculation();
        transferCalculation.LockTransferQoute();
        transferCalculation.SetStatus(status);
        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferCalculation>.Some(transferCalculation));

        var result = await _sut.SubmitPensionWiseDate(new SubmitTransferPensionWiseRequest { PensionWiseDate = DateTime.Parse(pwDate) });
        result.Should().BeOfType(expectedType);
    }

    [Input(false, TransferApplicationStatus.SubmitStarted, "2023-06-14", typeof(NotFoundObjectResult))]
    [Input(true, TransferApplicationStatus.SubmitStarted, "2023-06-14", typeof(NoContentResult))]
    [Input(true, TransferApplicationStatus.NotStartedTA, "2023-06-14", typeof(BadRequestObjectResult))]
    public async Task SaveFinancialAdviseReturnsCorrectStatusCode(bool transferJourneyExists, TransferApplicationStatus status, string pwDate, Type expectedType)
    {
        var transferJourney = new TransferJourneyBuilder().Build();

        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(transferJourneyExists ? Option<TransferJourney>.Some(transferJourney) : Option<TransferJourney>.None);

        var transferCalculation = CreateTransferCalculation();
        transferCalculation.LockTransferQoute();
        transferCalculation.SetStatus(status);
        _transferCalculationRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<TransferCalculation>.Some(transferCalculation));

        var result = await _sut.SubmitFinancialAdviseDate(new SubmitTransferFinancialAdviseRequest { FinancialAdviseDate = DateTime.Parse(pwDate) });
        result.Should().BeOfType(expectedType);
    }

    [Input(false, typeof(NotFoundObjectResult))]
    [Input(true, typeof(OkObjectResult))]
    public async Task PensionWiseReturnsCorrectStatusCode_AndResponseResult(bool transferJourneyExists, Type expectedType)
    {
        var now = DateTime.Now;
        var transferJourney = new TransferJourneyBuilder().Build();
        transferJourney.SetPensionWiseDate(now.AddDays(-2));

        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(transferJourneyExists ? Option<TransferJourney>.Some(transferJourney) : Option<TransferJourney>.None);

        var result = await _sut.PensionWise();
        result.Should().BeOfType(expectedType);
        if (transferJourneyExists)
        {
            var response = (result as OkObjectResult).Value as TransferPensionWiseResponse;
            response.PensionWiseDate.Should().Be(now.AddDays(-2));
        }
    }

    [Input(false, typeof(NotFoundObjectResult))]
    [Input(true, typeof(OkObjectResult))]
    public async Task FinancialAdviseReturnsCorrectStatusCode_AndResponseResult(bool transferJourneyExists, Type expectedType)
    {
        var now = DateTime.Now;
        var transferJourney = new TransferJourneyBuilder().Build();
        transferJourney.SetFinancialAdviseDate(now.AddDays(-2));

        _transferJourneyRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(transferJourneyExists ? Option<TransferJourney>.Some(transferJourney) : Option<TransferJourney>.None);

        var result = await _sut.FinancialAdvise();
        result.Should().BeOfType(expectedType);
        if (transferJourneyExists)
        {
            var response = (result as OkObjectResult).Value as TransferFinancialAdviseResponse;
            response.FinancialAdviseDate.Should().Be(now.AddDays(-2));
        }
    }

    private void SetupControllerContext()
    {
        var id = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim("business_group", "TestBusinessGroup"),
            new Claim("reference_number", "TestReferenceNumber"),
        };

        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    private TransferCalculation CreateTransferCalculation(string transferQuote = null)
    {
        if (transferQuote == null)
            transferQuote = @"{""guaranteeDate"":""2023-01-27T00:00:00+00:00"",""guaranteePeriod"":""P3M"",
                            ""replyByDate"":""2023-01-06T00:00:00+00:00"",""totalPensionAtDOL"":4996.04,""maximumResidualPension"":5765.93,
                            ""minimumResidualPension"":0.0,""transferValues"":{""totalGuaranteedTransferValue"":513476.26,
                            ""totalNonGuaranteedTransferValue"":12039.02,""minimumPartialTransferValue"":302752.75,
                            ""totalTransferValue"":525515.28},""isGuaranteedQuote"":false,""originalEffectiveDate"":""2022-10-27T00:00:00+00:00""}";

        return new TransferCalculation("RBS", "1111124", transferQuote, DateTimeOffset.UtcNow);
    }

    private (string fileName, MemoryStream stream) CreateDummyFile()
    {
        var content = "Test content";
        var fileName = "test.pdf";
        var ms = new MemoryStream();
        var writer = new StreamWriter(ms);
        writer.Write(content);
        writer.Flush();
        ms.Position = 0;

        return (fileName, ms);
    }
}