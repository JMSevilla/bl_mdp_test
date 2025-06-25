using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Documents;
using WTW.MdpService.Domain.Common;
using WTW.MdpService.Domain.Common.Journeys;
using WTW.MdpService.Infrastructure.Edms;
using WTW.MdpService.Infrastructure.MdpDb;
using WTW.MdpService.Test.Domain.Mdp;
using WTW.TestCommon.FixieConfig;
using WTW.Web.Errors;
using static LanguageExt.Prelude;

namespace WTW.MdpService.Test.Documents;

public class JourneyDocumentsControllerTest
{
    private readonly JourneyDocumentsController _sut;
    private readonly Mock<IJourneyDocumentsRepository> _journeyDocumentsRepository;
    private readonly Mock<IEdmsClient> _edmsClientMock;
    private readonly Mock<IMdpUnitOfWork> _mdpDbUnitOfWorkMock;
    private readonly Mock<ILogger<JourneyDocumentsController>> _logger;
    private readonly Mock<IFormFile> _fileMock;

    public JourneyDocumentsControllerTest()
    {
        _journeyDocumentsRepository = new Mock<IJourneyDocumentsRepository>();
        _edmsClientMock = new Mock<IEdmsClient>();
        _mdpDbUnitOfWorkMock = new Mock<IMdpUnitOfWork>();
        _logger = new Mock<ILogger<JourneyDocumentsController>>();
        _fileMock = new Mock<IFormFile>();
        _sut = new JourneyDocumentsController(
            _edmsClientMock.Object,
            _mdpDbUnitOfWorkMock.Object,
            _logger.Object,
            _journeyDocumentsRepository.Object);

        SetupControllerContext();
    }

    public async Task CreateDocument_ReturnsFileUuid_WhenDocumentIsCreatedSuccessfully()
    {
        var fileGuid = Guid.NewGuid().ToString();

        var (fileName, ms) = CreateDummyFile();

        _fileMock.Setup(f => f.FileName).Returns(fileName);
        _fileMock.Setup(l => l.Length).Returns(ms.Length);
        _fileMock.Setup(s => s.OpenReadStream()).Returns(ms);

        _edmsClientMock
            .Setup(x => x.UploadDocument(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(Right<DocumentUploadError, DocumentUploadResponse>(new DocumentUploadResponse { Uuid = fileGuid }));
      
        _journeyDocumentsRepository
            .Setup(x => x.Add(It.IsAny<UploadedDocument>()));

        var request = new JourneyDocumentRequest { File = _fileMock.Object };
        var result = await _sut.CreateDocument(request);
        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as JourneyDocumentCreateResponse;
        response.Uuid.Should().Be(fileGuid);
    }

    public async Task CreateDocument_ReturnsBadRequest_WhenDocumentIsNotUploadedToEdms()
    {
        var fileGuid = Guid.NewGuid().ToString();

        var (fileName, ms) = CreateDummyFile();

        _fileMock.Setup(f => f.FileName).Returns(fileName);
        _fileMock.Setup(l => l.Length).Returns(ms.Length);
        _fileMock.Setup(s => s.OpenReadStream()).Returns(ms);

        _edmsClientMock
            .Setup(x => x.UploadDocument(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(Left<DocumentUploadError, DocumentUploadResponse>(new DocumentUploadError { Message = "Failed to upload" }));

        var request = new JourneyDocumentRequest { File = _fileMock.Object };
        var result = await _sut.CreateDocument(request);
        result.Should().BeOfType<BadRequestObjectResult>();
        var response = ((BadRequestObjectResult)result).Value as ApiError;
        response.Errors[0].Message.Should().Be("Failed to upload");
    }

    public async Task DeleteDocument_ReturnsNoConent_WhenDocumentExists()
    {
        var uuid = "TestUuid";

        var document = new UploadedDocumentsBuilder()
            .Uuid(uuid)
            .Build();

        _journeyDocumentsRepository
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Option<UploadedDocument>.Some(document));

        var request = new JourneyDocumentDeleteRequest { Uuid = uuid };
        var result = await _sut.DeleteDocument(request);
        result.Should().BeOfType<NoContentResult>();
    }

    public async Task DeleteDocument_ReturnsNotFound_WhenDocumentDoesNotExists()
    {
        var uuid = "TestUuid";
        var document = new UploadedDocumentsBuilder()
            .Uuid(uuid)
            .Build();

        _journeyDocumentsRepository
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), uuid))
            .ReturnsAsync(Option<UploadedDocument>.Some(document));

        var request = new JourneyDocumentDeleteRequest { Uuid = "OtherUuid" };
        var result = await _sut.DeleteDocument(request);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Input("1111-1111", "2222-2222")]
    public async Task DocumentsList_ReturnsJourneyDocuments(string uuid1, string uuid2)
    {

        var documents = new List<UploadedDocument>
        {
            new UploadedDocumentsBuilder()
            .Uuid(uuid1)
            .Build(),
            new UploadedDocumentsBuilder()
            .Uuid(uuid2)
            .Build(),
        };

        _journeyDocumentsRepository
            .Setup(x => x.List(It.IsAny<string>(), It.IsAny<string>(), "transfer2"))
            .ReturnsAsync(documents);

        var request = new JourneyDocumentListRequest { JourneyType = "transfer2" };
        var result = await _sut.ListDocuments(request);
        result.Should().BeOfType<OkObjectResult>();
        var response = ((OkObjectResult)result).Value as IEnumerable<JourneyDocumentsResponse>;
        response.ToList()[0].Uuid.Should().Be(uuid1);
        response.ToList()[1].Uuid.Should().Be(uuid2);
    }

    [Input("1111-1111", "2222-2222")]
    public async Task DeleteAllDocuments_ReturnsNoConentet(string uuid1, string uuid2)
    {

        var documents = new List<UploadedDocument>
        {
            new UploadedDocumentsBuilder()
            .Uuid(uuid1)
            .Build(),
            new UploadedDocumentsBuilder()
            .Uuid(uuid2)
            .Build(),
        };

        _journeyDocumentsRepository
            .Setup(x => x.List(It.IsAny<string>(), It.IsAny<string>(), "transfer2"))
            .ReturnsAsync(documents);

        _journeyDocumentsRepository
            .Setup(x => x.RemoveAll(documents));

        var request = new JourneyDocumentDeleteAllRequest { JourneyType = "transfer2" };
        var result = await _sut.DeleteAllDocuments(request);
        result.Should().BeOfType<NoContentResult>();
    }

    public async Task UpdateDocumentTags_ReturnsNoContent_WhenDocumentIsFound()
    {
        var uuid = "TestUuid";

        var document = new UploadedDocumentsBuilder()
            .Uuid(uuid)
            .Build();

        _journeyDocumentsRepository
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), uuid))
            .ReturnsAsync(Option<UploadedDocument>.Some(document));

        var request = new JourneyDocumentTagUpdateRequest { FileUuid = uuid, Tags = new List<string> { "TAG1", "TAG2" } };
        var result = await _sut.UpdateDocumentTags(request);
        result.Should().BeOfType<NoContentResult>();
    }

    public async Task UpdateDocumentTags_ReturnsNotFound_WhenDocumentNotFound()
    {
        var uuid = "TestUuid";

        var document = new UploadedDocumentsBuilder()
            .Uuid(uuid)
            .Build();

        _journeyDocumentsRepository
            .Setup(x => x.Find(It.IsAny<string>(), It.IsAny<string>(), uuid))
            .ReturnsAsync(Option<UploadedDocument>.Some(document));

        var request = new JourneyDocumentTagUpdateRequest { FileUuid = "BadUuid", Tags = new List<string> { "TAG1", "TAG2" } };
        var result = await _sut.UpdateDocumentTags(request);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    private static (string fileName, MemoryStream stream) CreateDummyFile()
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
}
