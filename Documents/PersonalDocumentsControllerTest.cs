using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Documents;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.Infrastructure.Edms;
using WTW.MdpService.Infrastructure.MemberDb;
using WTW.MdpService.Infrastructure.MemberDb.Documents;
using static LanguageExt.Prelude;

namespace WTW.MdpService.Test.Documents;

public class PersonalDocumentsControllerTest
{
    private readonly PersonalDocumentsController _sut;
    private readonly Mock<IDocumentsRepository> _documentsRepositoryMock;
    private readonly Mock<IEdmsClient> _edmsClientMock;
    private readonly Mock<IMemberDbUnitOfWork> _uowMock;
    private readonly Mock<ILogger<PersonalDocumentsController>> _loggerMock;

    public PersonalDocumentsControllerTest()
    {
        _documentsRepositoryMock = new Mock<IDocumentsRepository>();
        _edmsClientMock = new Mock<IEdmsClient>();
        _uowMock = new Mock<IMemberDbUnitOfWork>();
        _loggerMock = new Mock<ILogger<PersonalDocumentsController>>();

        _sut = new PersonalDocumentsController(
            _documentsRepositoryMock.Object,
            _edmsClientMock.Object,
            _uowMock.Object,
            _loggerMock.Object);

        SetupControllerContext();
    }

    public async Task DownloadDocument_ReturnsFile_WhenDocumentExistsInRepository()
    {
        // Arrange
        var id = 1;
        var document = new Document(
            "TestBusinessGroup",
            "TestReferenceNumber",
            "TestType",
            DateTimeOffset.UtcNow,
            "TestName",
            "TestFileName.pdf",
            id,
            123,
            "TypeId",
            "Schema");

        _documentsRepositoryMock
            .Setup(x => x.FindByDocumentId("TestReferenceNumber", "TestBusinessGroup", id))
            .ReturnsAsync(Option<Document>.Some(document));

        _edmsClientMock
            .Setup(x => x.GetDocument(document.ImageId))
            .ReturnsAsync(new MemoryStream());

        // Act
        var result = await _sut.DownloadDocument(id);

        // Assert
        result.Should().BeOfType<FileStreamResult>();
        var fileResult = result as FileStreamResult;
        fileResult.FileDownloadName.Should().Be(document.FileName);
        fileResult.ContentType.Should().Be("application/octet-stream");
        _uowMock.Verify(x => x.Commit(), Times.Once);
    }

    public async Task DownloadDocument_ReturnsNotFound_WhenDocumentDoesNotExistAnywhere()
    {
        var id = 1;

        _documentsRepositoryMock
            .Setup(x => x.FindByDocumentId("TestReferenceNumber", "TestBusinessGroup", id))
            .ReturnsAsync(Option<Document>.None);

        _edmsClientMock
            .Setup(x => x.GetDocumentOrError(id))
            .ReturnsAsync(Left<Error, Stream>(Error.New("Document not found")));

        var result = await _sut.DownloadDocument(id);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    public async Task DownloadProtectedQuoteDocument_ReturnsFile_WhenDocumentExistsInEdms()
    {
        var id = 1;

        _edmsClientMock
            .Setup(x => x.GetDocumentOrError(id))
            .ReturnsAsync(Right<Error, Stream>(new MemoryStream()));

        var result = await _sut.DownloadProtectedQuoteDocument(id);

        result.Should().BeOfType<FileStreamResult>();
        var fileResult = result as FileStreamResult;
        fileResult.FileDownloadName.Should().Be($"TestBusinessGroup-TestReferenceNumber-{id}.pdf");
        fileResult.ContentType.Should().Be("application/octet-stream");
    }

    public async Task DownloadProtectedQuoteDocument_ReturnsNotFound_WhenDocumentDoesNotExistInEdms()
    {
        var id = 1;

        _edmsClientMock
            .Setup(x => x.GetDocumentOrError(id))
            .ReturnsAsync(Left<Error, Stream>(Error.New("Document not found")));

        var result = await _sut.DownloadProtectedQuoteDocument(id);

        result.Should().BeOfType<NotFoundResult>();
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
