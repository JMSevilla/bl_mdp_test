using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Domain.Common;
using WTW.MdpService.Infrastructure.Edms;
using WTW.MdpService.Infrastructure.Journeys.Documents;
using WTW.MdpService.Infrastructure.MdpDb;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Infrastructure.Journeys.Documents;

public class JourneyDocumentsHandlerTest
{
    private readonly Mock<IJourneyDocumentsRepository> _journeyDocumentsRepositoryMock;
    private readonly Mock<IEdmsClient> _edmsClientMock;
    private readonly Mock<ILogger<JourneyDocumentsHandlerService>> _loggerMock;
    private readonly JourneyDocumentsHandlerService _sut;
    public JourneyDocumentsHandlerTest()
    {
        _journeyDocumentsRepositoryMock = new Mock<IJourneyDocumentsRepository>();
        _edmsClientMock = new Mock<IEdmsClient>();
        _loggerMock = new Mock<ILogger<JourneyDocumentsHandlerService>>();
        _sut = new JourneyDocumentsHandlerService(
            _journeyDocumentsRepositoryMock.Object,
            _edmsClientMock.Object,
            _loggerMock.Object);

    }

    public async Task PostIndex_ReturnsNoContent_WhenDocumentsAreSuccessfullyPostIndexed()
    {
        var caseNumber = "caseNumber";

        var documents = new List<UploadedDocument>
        {
            new UploadedDocument("refNo", "businessGroup", "test-type", null, "test1.txt", "tets-Uuid", DocumentSource.Incoming, true, "tag1", "tag2"),
        };

        var postindexResponse = new PostindexDocumentsResponse
        {
            Documents = new List<PostindexDocumentResponse>
                {
                    new PostindexDocumentResponse { ImageId = 1, DocUuid = "failledDocId", Indexed = true },
                    new PostindexDocumentResponse { ImageId = 2, DocUuid = "failledDocId", Indexed = false },
                }
        };

        _journeyDocumentsRepositoryMock
            .Setup(x => x.List(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(documents);

        _edmsClientMock.Setup(client => client.PostindexDocuments(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IList<UploadedDocument>>()))
           .ReturnsAsync(postindexResponse);

        var result = await _sut.PostIndex("businessGroup", "refNo", caseNumber, "journeyType");

        result.IsRight.Should().BeTrue();
        _journeyDocumentsRepositoryMock.Verify(x => x.RemoveAll(documents), Times.Once);
    }

    public async Task PostIndex_ReturnsError_WhenCaseNumberIsNullOrEmpty()
    {
        var caseNumber = string.Empty;

        var result = await _sut.PostIndex("businessGroup", "refNo", caseNumber, "journeyType");

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be("journeyType case must be sumbitted.");
    }

    public async Task PostIndex_ReturnsError_WhenNoDocumentsFound()
    {
        var caseNumber = "caseNumber";

        _journeyDocumentsRepositoryMock
            .Setup(x => x.List(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<UploadedDocument>());

        var result = await _sut.PostIndex("businessGroup", "refNo", caseNumber, "journeyType");

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be("Cannot find any documents to postindex.");
    }

    public async Task PostIndex_ReturnsError_WhenPostIndexingFails()
    {
        var caseNumber = "caseNumber";

        var documents = new List<UploadedDocument>
        {
            new UploadedDocument("refNo", "businessGroup", "test-type", null, "test1.txt", "tets-Uuid", DocumentSource.Incoming, true, "tag1", "tag2"),
        };

        _journeyDocumentsRepositoryMock
          .Setup(x => x.List(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
          .ReturnsAsync(documents);

        _edmsClientMock
            .Setup(x => x.PostindexDocuments(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<UploadedDocument>>()))
            .ReturnsAsync(new PostIndexError { Message = "Postindexing failed" });

        var result = await _sut.PostIndex("businessGroup", "refNo", caseNumber, "journeyType");

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be("Error message: Postindexing failed.");
    }
}
