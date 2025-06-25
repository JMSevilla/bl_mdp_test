using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Domain.Common;
using WTW.MdpService.Domain.Mdp;
using WTW.MdpService.Infrastructure.Compressions;
using WTW.MdpService.Infrastructure.Edms;
using WTW.MdpService.Infrastructure.EdmsIndexing;
using WTW.MdpService.Infrastructure.Gbg;
using WTW.MdpService.Test.Domain.Mdp.Journeys;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Infrastructure;

public class EdmsDocumentsIndexingTest
{
    private readonly Mock<IEdmsClient> _mockEdmsClient;
    private readonly Mock<ICachedGbgClient> _mockGbgClient;
    private readonly Mock<ICachedGbgAdminClient> _mockGbgAdminClient;
    private readonly Mock<ILogger<EdmsDocumentsIndexing>> _mockLogger;
    private readonly EdmsDocumentsIndexing _sut;

    public EdmsDocumentsIndexingTest()
    {
        _mockEdmsClient = new Mock<IEdmsClient>();
        _mockGbgClient = new Mock<ICachedGbgClient>();
        _mockGbgAdminClient = new Mock<ICachedGbgAdminClient>();
        _mockLogger = new Mock<ILogger<EdmsDocumentsIndexing>>();

        _sut = new EdmsDocumentsIndexing(
            _mockEdmsClient.Object,
            _mockGbgClient.Object,
            _mockGbgAdminClient.Object,
            _mockLogger.Object);
    }

    public async Task PostIndexTransferDocuments_ShouldReturnError_WhenEdmsPostindexDocumentsFails()
    {
        var businessGroup = "TestGroup";
        var referenceNumber = "Ref123";
        var caseNumber = "Case456";
        var journey = TransferJourney.Create(businessGroup, referenceNumber, DateTimeOffset.UtcNow, 123456);
        var documents = new List<UploadedDocument>();

        _mockEdmsClient.Setup(client => client.PostindexDocuments(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IList<UploadedDocument>>()))
            .ReturnsAsync(new PostIndexError { Message = "test error message" });

        var result = await _sut.PostIndexTransferDocuments(businessGroup, referenceNumber, caseNumber, journey, documents);

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be("Error message: test error message.");

        _mockGbgClient.Verify(x => x.GetDocuments(It.IsAny<List<Guid>>()), Times.Never);
        _mockEdmsClient.Verify(x => x.UploadDocument(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<int?>()), Times.Never);
    }

    public async Task PostIndexTransferDocuments_ShouldReturnError_WhenEdmsPostindexDocumentsFailsOnIndividualDocument()
    {
        var businessGroup = "TestGroup";
        var referenceNumber = "Ref123";
        var caseNumber = "Case456";
        var journey = TransferJourney.Create(businessGroup, referenceNumber, DateTimeOffset.UtcNow, 123456);
        var documents = new List<UploadedDocument>();
        var pdfContent = "PDF content";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(pdfContent));
        var expectedImageId = 1;
        var expectedDocUuid = "DocUuid123";

        var uploadResponse = new DocumentUploadResponse { Uuid = expectedDocUuid };
        var postindexResponse = new PostindexDocumentsResponse
        {
            Documents = new List<PostindexDocumentResponse>
                {
                    new PostindexDocumentResponse { ImageId = expectedImageId, DocUuid = expectedDocUuid, Indexed = true },
                    new PostindexDocumentResponse { ImageId = 2, DocUuid = "failledDocId", Indexed = false },

                }
        };

        _mockEdmsClient.Setup(client => client.PostindexDocuments(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IList<UploadedDocument>>()))
            .ReturnsAsync(postindexResponse);

        var result = await _sut.PostIndexTransferDocuments(businessGroup, referenceNumber, caseNumber, journey, documents);

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be("Case number: Case456. Failed to postindex documents: Documents ids: failledDocId.Error: .");

        _mockGbgClient.Verify(x => x.GetDocuments(It.IsAny<List<Guid>>()), Times.Never);
        _mockEdmsClient.Verify(x => x.UploadDocument(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<int?>()), Times.Never);
    }

    public async Task PostIndexTransferDocuments_ShouldReturnDocuments_WhenSuccessful()
    {
        var businessGroup = "TestGroup";
        var referenceNumber = "Ref123";
        var caseNumber = "Case456";
        var journey = TransferJourney.Create(businessGroup, referenceNumber, DateTimeOffset.UtcNow, 123456);
        var documents = new List<UploadedDocument>();
        var pdfContent = "PDF content";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(pdfContent));
        var expectedImageId = 1;
        var expectedDocUuid = "DocUuid123";

        var uploadResponse = new DocumentUploadResponse { Uuid = expectedDocUuid };
        var postindexResponse = new PostindexDocumentsResponse
        {
            Documents = new List<PostindexDocumentResponse>
                {
                    new PostindexDocumentResponse { ImageId = expectedImageId, DocUuid = expectedDocUuid, Indexed = true }
                }
        };

        _mockEdmsClient.Setup(client => client.PostindexDocuments(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IList<UploadedDocument>>()))
            .ReturnsAsync(postindexResponse);

        var result = await _sut.PostIndexTransferDocuments(businessGroup, referenceNumber, caseNumber, journey, documents);

        result.IsRight.Should().BeTrue();
        result.Right()[0].ImageId.Should().Be(expectedImageId);
        result.Right()[0].DocUuid.Should().Be(expectedDocUuid);

        _mockGbgClient.Verify(x => x.GetDocuments(It.IsAny<List<Guid>>()), Times.Never);
        _mockEdmsClient.Verify(x => x.UploadDocument(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<int?>()), Times.Never);
    }

    public async Task PreIndexRetirement_ShouldReturnError_WhenPreindexDocumentFails()
    {
        var journey = new RetirementJourneyBuilder().Build();
        var referenceNumber = "ref123";
        var businessGroup = "bg";
        var summaryPdf = new MemoryStream();

        _mockEdmsClient.Setup(c => c.PreindexDocument(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>(), It.IsAny<int?>()))
            .ReturnsAsync(new PreindexError { Message = "Preindex failed." });

        var result = await _sut.PreIndexRetirement(journey, referenceNumber, businessGroup, summaryPdf);

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be("Preindex failed.");
    }

    public async Task PreIndexRetirement_ShouldReturnSuccessfulResponse_WhenPreindexSucceededNoGbgIdExist()
    {
        var journey = new RetirementJourneyBuilder().Build();
        var referenceNumber = "ref123";
        var businessGroup = "bg";
        var summaryPdf = new MemoryStream();

        _mockEdmsClient.Setup(c => c.PreindexDocument(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>(), It.IsAny<int?>()))
            .ReturnsAsync(new PreindexResponse { Message = "ok", BatchNumber = 123456, ImageId = 654321 });

        var result = await _sut.PreIndexRetirement(journey, referenceNumber, businessGroup, summaryPdf);

        result.IsRight.Should().BeTrue();
        result.Right().BatchNumber.Should().Be(123456);
        result.Right().ApplicationImageId.Should().Be(654321);
    }

    public async Task PreIndexRetirement_ShouldReturnError_WhenFailsToGetDocsByGbgId()
    {
        var journey = new RetirementJourneyBuilder().Build();
        journey.SaveGbgId(Guid.NewGuid());
        var referenceNumber = "ref123";
        var businessGroup = "bg";
        var summaryPdf = new MemoryStream();

        _mockEdmsClient.Setup(c => c.PreindexDocument(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>(), It.IsAny<int?>()))
            .ReturnsAsync(new PreindexResponse { Message = "ok", BatchNumber = 123456, ImageId = 654321 });

        var result = await _sut.PreIndexRetirement(journey, referenceNumber, businessGroup, summaryPdf);

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().StartWith("Failed to get gbg document:");
    }

    public async Task PreIndexRetirement_ShouldReturnError_WhenFailsUnzipPdf()
    {
        var journey = new RetirementJourneyBuilder().Build();
        journey.SaveGbgId(Guid.NewGuid());
        var referenceNumber = "ref123";
        var businessGroup = "bg";
        var summaryPdf = new MemoryStream();

        _mockEdmsClient.Setup(c => c.PreindexDocument(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>(), It.IsAny<int?>()))
            .ReturnsAsync(new PreindexResponse { Message = "ok", BatchNumber = 123456, ImageId = 654321 });

        _mockGbgClient
            .Setup(client => client.GetDocuments(It.IsAny<List<Guid>>()))
            .Returns(async () => await FileCompression.Zip(new List<StreamFile> { new StreamFile("tets.txt", new MemoryStream()) }));

        var result = await _sut.PreIndexRetirement(journey, referenceNumber, businessGroup, summaryPdf);

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().StartWith("Identity document not found");
    }

    public async Task PreIndexRetirement_ShouldReturnError_WhenFailsToPreindexEdmsDocs()
    {
        var journey = new RetirementJourneyBuilder().Build();
        journey.SaveGbgId(Guid.NewGuid());
        var referenceNumber = "ref123";
        var businessGroup = "bg";
        var summaryPdf = new MemoryStream();

        _mockEdmsClient.Setup(c => c.PreindexDocument(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(new PreindexResponse { Message = "ok", BatchNumber = 123456, ImageId = 654321 });

        _mockGbgClient
            .Setup(client => client.GetDocuments(It.IsAny<List<Guid>>()))
            .Returns(async () => await FileCompression.Zip(new List<StreamFile> { new StreamFile("tets.pdf", new MemoryStream()) }));

        _mockEdmsClient.Setup(c => c.PreindexDocument(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>(), 123456))
          .ReturnsAsync(new PreindexError { Message = "Preindex failed." });

        var result = await _sut.PreIndexRetirement(journey, referenceNumber, businessGroup, summaryPdf);

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be("Preindex failed.");
    }

    public async Task PreIndexRetirement_ShouldReturnSuccessfulResponse_WhenPreindexAndGbgSucceeded()
    {
        var journey = new RetirementJourneyBuilder().Build();
        journey.SaveGbgId(Guid.NewGuid());
        var referenceNumber = "ref123";
        var businessGroup = "bg";
        var summaryPdf = new MemoryStream();

        _mockEdmsClient.Setup(c => c.PreindexDocument(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(new PreindexResponse { Message = "ok", BatchNumber = 123456, ImageId = 654321 });

        _mockGbgClient
            .Setup(client => client.GetDocuments(It.IsAny<List<Guid>>()))
            .Returns(async () => await FileCompression.Zip(new List<StreamFile> { new StreamFile("tets.pdf", new MemoryStream()) }));

        _mockEdmsClient.Setup(c => c.PreindexDocument(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>(), 123456))
          .ReturnsAsync(new PreindexResponse { Message = "ok", BatchNumber = 123456, ImageId = 654321 });

        var result = await _sut.PreIndexRetirement(journey, referenceNumber, businessGroup, summaryPdf);

        result.IsRight.Should().BeTrue();
        result.Right().BatchNumber.Should().Be(123456);
        result.Right().ApplicationImageId.Should().Be(654321);
    }

    public async Task PostIndexesRetirement()
    {
        _mockEdmsClient.Setup(c => c.IndexRetirementDocument(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
          .ReturnsAsync(new IndexResponse { Message = "ok" });

        var result = await _sut.PostIndexRetirement(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>());

        result.HasValue.Should().BeFalse();
    }

    public async Task PostIndexRetirementFails()
    {
        _mockEdmsClient.Setup(c => c.IndexRetirementDocument(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
          .ReturnsAsync(new PostIndexError { Message = "failed" });

        var result = await _sut.PostIndexRetirement(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>());

        result.HasValue.Should().BeTrue();
        result.Value.Message.Should().Be("Error message: failed.");
    }

    public async Task PostIndexedBereavement()
    {
        _mockEdmsClient.Setup(c => c.IndexBereavementDocument(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
          .ReturnsAsync(new IndexResponse { Message = "ok" });

        var result = await _sut.PostIndexBereavement(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>());

        result.HasValue.Should().BeFalse();
    }

    public async Task PostIndexBereavementFails()
    {
        _mockEdmsClient.Setup(c => c.IndexBereavementDocument(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
          .ReturnsAsync(new PostIndexError { Message = "failed" });

        var result = await _sut.PostIndexBereavement(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>());

        result.HasValue.Should().BeTrue();
        result.Value.Message.Should().Be("Error message: failed.");
    }

    public async Task PostIndexesBereavementDocuments()
    {
        var businessGroup = "TestGroup";
        var caseNumber = "Case456";
        var documents = new List<UploadedDocument>();
        var expectedImageId = 1;
        var expectedDocUuid = "DocUuid123";

        var uploadResponse = new DocumentUploadResponse { Uuid = expectedDocUuid };
        var postindexResponse = new PostindexDocumentsResponse
        {
            Documents = new List<PostindexDocumentResponse>
                {
                    new PostindexDocumentResponse { ImageId = expectedImageId, DocUuid = expectedDocUuid, Indexed = true }
                }
        };

        _mockEdmsClient.Setup(client => client.PostIndexBereavementDocuments(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IList<UploadedDocument>>()))
            .ReturnsAsync(postindexResponse);


        var result = await _sut.PostIndexBereavementDocuments(businessGroup, caseNumber, documents);

        result.IsRight.Should().BeTrue();
        result.Right()[0].imageId.Should().Be(expectedImageId);
        result.Right()[0].docUuid.Should().Be(expectedDocUuid);
    }

    public async Task PostIndexBereavementDocuments_ReturnsError_WhenSomeDocsUploadFails()
    {
        var businessGroup = "TestGroup";
        var caseNumber = "Case456";
        var documents = new List<UploadedDocument>();
        var expectedImageId = 1;
        var expectedDocUuid = "DocUuid123";

        var uploadResponse = new DocumentUploadResponse { Uuid = expectedDocUuid };
        var postindexResponse = new PostindexDocumentsResponse
        {
            Documents = new List<PostindexDocumentResponse>
                {
                    new PostindexDocumentResponse { ImageId = expectedImageId, DocUuid = expectedDocUuid, Indexed = true },
                    new PostindexDocumentResponse { ImageId = expectedImageId, DocUuid = expectedDocUuid, Indexed = false },
                }
        };

        _mockEdmsClient.Setup(client => client.PostIndexBereavementDocuments(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IList<UploadedDocument>>()))
            .ReturnsAsync(postindexResponse);


        var result = await _sut.PostIndexBereavementDocuments(businessGroup, caseNumber, documents);

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be("Case number: Case456. Failed to postindex documents: Documents ids: DocUuid123.Error: .");
    }

    public async Task PostIndexBereavementDocuments_ReturnsError_WhenEdmasFails()
    {
        var businessGroup = "TestGroup";
        var caseNumber = "Case456";
        var documents = new List<UploadedDocument>();


        _mockEdmsClient.Setup(client => client.PostIndexBereavementDocuments(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IList<UploadedDocument>>()))
            .ReturnsAsync(new PostIndexError { Message = "Some error message" });


        var result = await _sut.PostIndexBereavementDocuments(businessGroup, caseNumber, documents);

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be("Error message: Some error message.");
    }

    public async Task CleansAfterPostIndex()
    {
        _mockGbgAdminClient
             .Setup(x => x.DeleteJourneyPerson(It.IsAny<string>()))
             .Returns(async () => HttpStatusCode.OK);

        var result = await _sut.CleanAfterPostIndex(Guid.NewGuid());

        result.IsRight.Should().BeTrue();
        result.Right().Should().Be(HttpStatusCode.OK);
    }

    public async Task CleanAfterPostIndex_ReturnsError()
    {
        var result = await _sut.CleanAfterPostIndex(Guid.NewGuid());

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().StartWith("Failed to delete journey person: System.ArgumentNullException");
    }
}