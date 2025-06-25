using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Domain.Common.Journeys;
using WTW.MdpService.Infrastructure.Compressions;
using WTW.MdpService.Infrastructure.Gbg;
using WTW.MdpService.Infrastructure.MdpDb;
using WTW.MdpService.Journeys.Submit.Services;
using WTW.Web.LanguageExt;
using static LanguageExt.Prelude;

namespace WTW.MdpService.Test.Journeys.Submit.Services;

public class GbgDocumentServiceTest
{
    private readonly Mock<IJourneysRepository> _journeysRepositoryMock;
    private readonly Mock<ICachedGbgClient> _gbgClientMock;
    private readonly Mock<ILogger<GbgDocumentService>> _loggerMock;
    private readonly GbgDocumentService _sut;

    public GbgDocumentServiceTest()
    {
        _journeysRepositoryMock = new Mock<IJourneysRepository>();
        _gbgClientMock = new Mock<ICachedGbgClient>();
        _loggerMock = new Mock<ILogger<GbgDocumentService>>();
        _sut = new GbgDocumentService(_journeysRepositoryMock.Object, _gbgClientMock.Object, _loggerMock.Object);
    }

    public async Task GetGbgFileReturnError_WhenJourneyIsNotFound()
    {
        _journeysRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<GenericJourney>.None);

        var result = await _sut.GetGbgFile(It.IsAny<string>(), It.IsAny<string>(), "test-journey-type", It.IsAny<string>());

        result.Left().Message.Should().Be("Failed to retrieve journey with type test-journey-type.");
    }

    public async Task GetGbgFileReturnError_WhenFailsToRetrieveGbgIdFromJourney()
    {
        _journeysRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(GetJourney());

        var result = await _sut.GetGbgFile(It.IsAny<string>(), It.IsAny<string>(), "test-journey-type", It.IsAny<string>());

        result.Left().Message.Should().Be("Failed to retrieve gbg id. Journey type: test-journey-type.");
    }

    public async Task GetGbgFileReturnError_WhenGbgClientFails()
    {
        _journeysRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(GetJourney(true));


        var result = await _sut.GetGbgFile(It.IsAny<string>(), It.IsAny<string>(), "test-journey-type", It.IsAny<string>());

        result.Left().Message.Should().Be("Failed to retrieve gbg document");
    }

    public async Task GetGbgFileReturnError_WhenGbgClientFailsPdfDocInGbgZip()
    {
        _journeysRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(GetJourney(true));

        _gbgClientMock.Setup(x => x.GetDocuments(It.IsAny<List<Guid>>())).Returns(async () => await CreateZip("test.txt"));

        var result = await _sut.GetGbgFile(It.IsAny<string>(), It.IsAny<string>(), "test-journey-type", "1234567");

        result.Left().Message.Should().Be("Case number: 1234567. Identity document not found");
    }

    public async Task GetGbgFileReturnStreamAndFileName()
    {
        _journeysRepositoryMock
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(GetJourney(true));

        _gbgClientMock.Setup(x => x.GetDocuments(It.IsAny<List<Guid>>())).Returns(async () => await CreateZip("IDscan_Journey_Report_1000117242-05102023103100.pdf"));

        var result = await _sut.GetGbgFile(It.IsAny<string>(), It.IsAny<string>(), "test-journey-type", "1234567");

        result.Right().DocumentStream.Should().NotBeNull();
        result.Right().FileName.Should().Be("GBG_1000117242-05102023103100.pdf");
    }

    private async Task<Stream> CreateZip(string fileName)
    {
        return await FileCompression.Zip(new List<StreamFile> { new StreamFile(fileName, new MemoryStream()) });
    }

    private static GenericJourney GetJourney(bool hasGbgId = false)
    {
        var journey = new GenericJourney("RBS", "1234567", "transfer", "step1", "step2", true, null, DateTimeOffset.UtcNow);
        if (hasGbgId)
        {
            var step = journey.GetStepByKey("step1");
            step.Value().UpdateGenericData("gbg_user_identification_form_id", "{\"gbgId\":\"c62020f0-4175-4d52-bedd-30756f2b41b4\"}");
        }

        return journey;
    }

    public async Task GetGbgFileById_WhenGbgClientFails_ReturnsError()
    {
        var gbgId = Guid.NewGuid();
        _gbgClientMock
            .Setup(x => x.GetDocuments(It.Is<List<Guid>>(list => list.Contains(gbgId))))
            .Returns(async () => await Task.FromException<Stream>(new Exception("GBG client error")));

        var result = await _sut.GetGbgFile(gbgId);

        result.Left().Message.Should().Contain("Failed to get gbg document");
    }

    public async Task GetGbgFileById_WhenNoPdfDocumentFound_ReturnsError()
    {
        var gbgId = Guid.NewGuid();
        _gbgClientMock
            .Setup(x => x.GetDocuments(It.Is<List<Guid>>(list => list.Contains(gbgId))))
            .Returns(() => TryAsync(CreateZip("test.txt")));

        var result = await _sut.GetGbgFile(gbgId);

        result.Left().Message.Should().Be("Identity document not found.");
    }

    public async Task GetGbgFileById_WhenPdfDocumentExists_ReturnsStreamAndFileName()
    {
        var gbgId = Guid.NewGuid();
        _gbgClientMock
            .Setup(x => x.GetDocuments(It.Is<List<Guid>>(list => list.Contains(gbgId))))
            .Returns(() => TryAsync(CreateZip("IDscan_Journey_Report_1000117242-05102023103100.pdf")));

        var result = await _sut.GetGbgFile(gbgId);

        result.Right().DocumentStream.Should().NotBeNull();
        result.Right().FileName.Should().Be("GBG_1000117242-05102023103100.pdf");
    }
}