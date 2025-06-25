using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Domain.Common;
using WTW.MdpService.Domain.Common.UploadedDocuments;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.Infrastructure.Edms;
using WTW.MdpService.Infrastructure.MdpDb;
using WTW.MdpService.Infrastructure.MemberDb;
using WTW.MdpService.Infrastructure.MemberDb.Documents;
using WTW.MdpService.Journeys.Submit.Services;
using WTW.MdpService.Test.Domain.Mdp;
using WTW.TestCommon.FixieConfig;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Journeys.Submit.Services;

public class DocumentsUploaderServiceTest
{
    private readonly Mock<IEdmsClient> _edmsClientMock;
    private readonly Mock<IDocumentFactoryProvider> _documentFactoryProviderMock;
    private readonly Mock<IDocumentsRepository> _documentsRepositoryMock;
    private readonly Mock<IMemberDbUnitOfWork> _memberDbUnitOfWorkMock;
    private readonly Mock<IJourneyDocumentsRepository> _journeyDocumentsRepositoryMock;
    private readonly Mock<IUploadedDocumentFactory> _uploadedDocumentFactoryMock;
    private readonly Mock<ICalculationHistoryRepository> _calculationHistoryRepository;
    private readonly Mock<ILogger<DocumentsUploaderService>> _loggerMock;
    private readonly DocumentsUploaderService _sut;

    public DocumentsUploaderServiceTest()
    {
        _edmsClientMock = new Mock<IEdmsClient>();
        _documentFactoryProviderMock = new Mock<IDocumentFactoryProvider>();
        _documentsRepositoryMock = new Mock<IDocumentsRepository>();
        _memberDbUnitOfWorkMock = new Mock<IMemberDbUnitOfWork>();
        _journeyDocumentsRepositoryMock = new Mock<IJourneyDocumentsRepository>();
        _uploadedDocumentFactoryMock = new Mock<IUploadedDocumentFactory>();
        _calculationHistoryRepository = new Mock<ICalculationHistoryRepository>();
        _loggerMock = new Mock<ILogger<DocumentsUploaderService>>();
        _sut = new DocumentsUploaderService(
            _edmsClientMock.Object,
            _documentFactoryProviderMock.Object,
            _documentsRepositoryMock.Object,
            _memberDbUnitOfWorkMock.Object,
            _journeyDocumentsRepositoryMock.Object,
            _uploadedDocumentFactoryMock.Object,
            _calculationHistoryRepository.Object,
            _loggerMock.Object);
    }

    public async Task UploadQuoteRequestSummaryReturnsError_WhenEdmdFailsToUploadDocument()
    {
        _edmsClientMock
            .Setup(x => x.UploadDocument(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(new DocumentUploadError { Message = "Error" });

        var result = await _sut.UploadQuoteRequestSummary(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), "retirement", It.IsAny<MemoryStream>(), It.IsAny<string>());

        result.Value.Message.Should().Be("Quote request documents upload process failed.");
    }

    public async Task UploadQuoteRequestSummaryReturnsError_WhenEdmdFailsToPostIndexDocuments()
    {
        _edmsClientMock
            .Setup(x => x.UploadDocument(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(new DocumentUploadResponse { Uuid = It.IsAny<string>() });

        _uploadedDocumentFactoryMock
           .Setup(x => x.CreateOutgoing(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()))
           .Returns(new UploadedDocumentsBuilder().Build());

        _edmsClientMock
           .Setup(x => x.PostindexDocuments(
               It.IsAny<string>(),
               It.IsAny<string>(),
               It.IsAny<string>(),
               It.IsAny<IList<UploadedDocument>>()))
           .ReturnsAsync(new PostIndexError { Message = "PostIndex failed.", Documents = new List<PostindexDocumentResponse>() });

        var result = await _sut.UploadQuoteRequestSummary(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), "retirement", It.IsAny<MemoryStream>(), It.IsAny<string>());

        result.Value.Message.Should().Be("Failed to post index documents.");
    }

    public async Task UploadQuoteRequestSummaryReturnsError_WhenEdmdFailsToPostIndexIndividualDocuments()
    {
        _edmsClientMock
            .Setup(x => x.UploadDocument(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(new DocumentUploadResponse { Uuid = It.IsAny<string>() });

        _edmsClientMock
           .Setup(x => x.PostindexDocuments(
               It.IsAny<string>(),
               It.IsAny<string>(),
               It.IsAny<string>(),
               It.IsAny<IList<UploadedDocument>>()))
           .ReturnsAsync(new PostindexDocumentsResponse
           {
               Documents = new List<PostindexDocumentResponse> { new PostindexDocumentResponse { DocUuid = "123", Indexed = false, Message = "Failed", ImageId = 123 } }
           });

        var result = await _sut.UploadQuoteRequestSummary(It.IsAny<string>(), It.IsAny<string>(), "7654321", "retirement", It.IsAny<MemoryStream>(), It.IsAny<string>());

        result.Value.Message.Should().Be("Case number: 7654321. Failed to post index documents: Documents ids: 123.Error: Failed.");
    }

    [Input("retirement", DocumentType.RetirementQuoteRequest)]
    [Input("transfer", DocumentType.TransferQuoteRequest)]
    public async Task UploadQuoteRequestSummarySucceeds(string quoteRequestCaseType, DocumentType documentType)
    {
        _edmsClientMock
            .Setup(x => x.UploadDocument(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(new DocumentUploadResponse { Uuid = It.IsAny<string>() });

        _edmsClientMock
           .Setup(x => x.PostindexDocuments(
               It.IsAny<string>(),
               It.IsAny<string>(),
               It.IsAny<string>(),
               It.IsAny<IList<UploadedDocument>>()))
           .ReturnsAsync(new PostindexDocumentsResponse
           {
               Documents = new List<PostindexDocumentResponse> { new PostindexDocumentResponse { DocUuid = "123", Indexed = true, Message = "", ImageId = 123 } }
           });
        _documentsRepositoryMock.Setup(x => x.NextId()).ReturnsAsync(1);

        _documentFactoryProviderMock
            .Setup(x => x.GetFactory(It.IsAny<DocumentType>()))
            .Returns(new TransferQuoteRequestFactory());

        var result = await _sut.UploadQuoteRequestSummary(It.IsAny<string>(), It.IsAny<string>(), "7654321", quoteRequestCaseType, It.IsAny<MemoryStream>(), It.IsAny<string>());

        result.Should().BeNull();
        _documentsRepositoryMock.Verify(x => x.Add(It.IsAny<Document>()), Times.Once);
        _memberDbUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
        _documentsRepositoryMock.Verify(x => x.NextId(), Times.Once);
        _documentFactoryProviderMock.Verify(x => x.GetFactory(documentType), Times.Once);
    }

    public async Task UploadQuoteRequestSummaryThrows_WhenUnsupportedQuoteRequestCaseType()
    {
        _edmsClientMock
            .Setup(x => x.UploadDocument(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(new DocumentUploadResponse { Uuid = It.IsAny<string>() });

        _edmsClientMock
           .Setup(x => x.PostindexDocuments(
               It.IsAny<string>(),
               It.IsAny<string>(),
               It.IsAny<string>(),
               It.IsAny<IList<UploadedDocument>>()))
           .ReturnsAsync(new PostindexDocumentsResponse
           {
               Documents = new List<PostindexDocumentResponse>
               {
                   new PostindexDocumentResponse { DocUuid = "123", Indexed = true, Message = "", ImageId = 123 },
                   new PostindexDocumentResponse { DocUuid = "321", Indexed = true, Message = "", ImageId = 321 },
               }
           });
        _documentsRepositoryMock.Setup(x => x.NextId()).ReturnsAsync(1);

        _documentFactoryProviderMock
            .Setup(x => x.GetFactory(It.IsAny<DocumentType>()))
            .Returns(new TransferQuoteRequestFactory());

        var action = async () => await _sut.UploadQuoteRequestSummary(It.IsAny<string>(), It.IsAny<string>(), "7654321", "UnsupportedCaseType", It.IsAny<MemoryStream>(), It.IsAny<string>());

        await action.Should().ThrowAsync<ArgumentException>().WithMessage("Unsupported Journey Type or Quote Request Case Type. Journey Type: null. Quote Request Case Type: UnsupportedCaseType. ");
    }

    public async Task UploadForDcRetirementReturnsError_WhenEdmdFailsToUploadSummaryDocument()
    {
        _edmsClientMock
            .Setup(x => x.UploadDocument(
                It.IsAny<string>(),
                "summary.pdf",
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(new DocumentUploadError { Message = "Error" });

        var result = await _sut.UploadGenericRetirementDocuments(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), "dcretirementapplication", It.IsAny<MemoryStream>(), "summary.pdf");

        result.Left().Message.Should().Be("Generic Retirement documents upload process failed. Journey Type: dcretirementapplication.");

        _edmsClientMock.Verify(x => x.UploadDocument(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), null), Times.Once);
        _edmsClientMock.Verify(x => x.PostindexDocuments(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<UploadedDocument>>()), Times.Never);
    }

    public async Task UploadForDcRetirementReturnsError_WhenEdmdFailsToPostIndexDocuments()
    {
        _edmsClientMock
            .Setup(x => x.UploadDocument(
                It.IsAny<string>(),
                "summary.pdf",
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(new DocumentUploadResponse { Uuid = It.IsAny<string>() });

        _edmsClientMock
            .Setup(x => x.UploadDocument(
                It.IsAny<string>(),
                "gbg-doc.pdf",
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(new DocumentUploadResponse { Uuid = It.IsAny<string>() });

        _uploadedDocumentFactoryMock
            .Setup(x => x.CreateOutgoing(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()))
            .Returns(new UploadedDocumentsBuilder().Build());

        _journeyDocumentsRepositoryMock.Setup(x => x.List(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new List<UploadedDocument>());

        _edmsClientMock
          .Setup(x => x.PostindexDocuments(
              It.IsAny<string>(),
              It.IsAny<string>(),
              It.IsAny<string>(),
              It.IsAny<IList<UploadedDocument>>()))
          .ReturnsAsync(new PostIndexError { Message = "PostIndex failed.", Documents = new List<PostindexDocumentResponse>() });

        var result = await _sut.UploadGenericRetirementDocuments(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>(), "summary.pdf");

        result.Left().Message.Should().Be("Failed to post index documents.");
        _edmsClientMock.Verify(x => x.UploadDocument(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), null), Times.Once);
        _edmsClientMock.Verify(x => x.PostindexDocuments(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<UploadedDocument>>()), Times.Once);
    }

    public async Task UploadForDcRetirementReturnsError_WhenEdmdFailsToPostIndexIndividualDocument()
    {
        _edmsClientMock
            .Setup(x => x.UploadDocument(
                It.IsAny<string>(),
                "summary.pdf",
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(new DocumentUploadResponse { Uuid = It.IsAny<string>() });

        _edmsClientMock
            .Setup(x => x.UploadDocument(
                It.IsAny<string>(),
                "gbg-doc.pdf",
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(new DocumentUploadResponse { Uuid = It.IsAny<string>() });

        _journeyDocumentsRepositoryMock.Setup(x => x.List(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new List<UploadedDocument>());

        _edmsClientMock
                   .Setup(x => x.PostindexDocuments(
                       It.IsAny<string>(),
                       It.IsAny<string>(),
                       It.IsAny<string>(),
                       It.IsAny<IList<UploadedDocument>>()))
                   .ReturnsAsync(new PostindexDocumentsResponse
                   {
                       Documents = new List<PostindexDocumentResponse> { new PostindexDocumentResponse { DocUuid = "123", Indexed = false, Message = "Failed", ImageId = 123 } }
                   });

        var result = await _sut.UploadGenericRetirementDocuments(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>(), "summary.pdf");

        result.Left().Message.Should().Be("Case number: . Failed to post index documents: Documents ids: 123.Error: Failed.");

        _edmsClientMock.Verify(x => x.UploadDocument(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), null), Times.Once);
        _edmsClientMock.Verify(x => x.PostindexDocuments(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<UploadedDocument>>()), Times.Once);
    }

    [Input("dcretirementapplication")]
    [Input("dbcoreretirementapplication")]
    public async Task UploadDocumentForGenericRetirementSucceeds(string journeyType)
    {
        _edmsClientMock
            .Setup(x => x.UploadDocument(
                It.IsAny<string>(),
                "summary.pdf",
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(new DocumentUploadResponse { Uuid = "321" });

        _edmsClientMock
            .Setup(x => x.UploadDocument(
                It.IsAny<string>(),
                "gbg-doc.pdf",
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(new DocumentUploadResponse { Uuid = It.IsAny<string>() });

        _journeyDocumentsRepositoryMock.Setup(x => x.List(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new List<UploadedDocument>());
        _documentsRepositoryMock.Setup(x => x.NextId()).ReturnsAsync(1);
        _edmsClientMock
           .Setup(x => x.PostindexDocuments(
               It.IsAny<string>(),
               It.IsAny<string>(),
               It.IsAny<string>(),
               It.IsAny<IList<UploadedDocument>>()))
           .ReturnsAsync(new PostindexDocumentsResponse
           {
               Documents = new List<PostindexDocumentResponse>
               {
                   new PostindexDocumentResponse { DocUuid = "123", Indexed = true, Message = "", ImageId = 123 },
                   new PostindexDocumentResponse { DocUuid = "321", Indexed = true, Message = "", ImageId = 321 },
               }
           });

        _documentFactoryProviderMock
           .Setup(x => x.GetFactory(It.IsAny<DocumentType>()))
           .Returns(new DcRetirementDocumentFactory());

        var result = await _sut.UploadGenericRetirementDocuments(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), journeyType, It.IsAny<MemoryStream>(), "summary.pdf");

        result.IsRight.Should().BeTrue();
        result.Right().Should().Be(321);
        _documentsRepositoryMock.Verify(x => x.Add(It.IsAny<Document>()), Times.Once);
        _memberDbUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
        _documentsRepositoryMock.Verify(x => x.NextId(), Times.Once);
        _edmsClientMock.Verify(x => x.UploadDocument(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), null), Times.Once);
        _edmsClientMock.Verify(x => x.PostindexDocuments(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<UploadedDocument>>()), Times.Once);
    }

    public async Task UploadDocumentForGenericRetirementThrows_WhenUnsupportedJourneyType()
    {
        _edmsClientMock
            .Setup(x => x.UploadDocument(
                It.IsAny<string>(),
                "summary.pdf",
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(new DocumentUploadResponse { Uuid = "321" });

        _edmsClientMock
            .Setup(x => x.UploadDocument(
                It.IsAny<string>(),
                "gbg-doc.pdf",
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(new DocumentUploadResponse { Uuid = It.IsAny<string>() });

        _journeyDocumentsRepositoryMock.Setup(x => x.List(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new List<UploadedDocument>());
        _documentsRepositoryMock.Setup(x => x.NextId()).ReturnsAsync(1);
        _edmsClientMock
           .Setup(x => x.PostindexDocuments(
               It.IsAny<string>(),
               It.IsAny<string>(),
               It.IsAny<string>(),
               It.IsAny<IList<UploadedDocument>>()))
           .ReturnsAsync(new PostindexDocumentsResponse
           {
               Documents = new List<PostindexDocumentResponse>
               {
                   new PostindexDocumentResponse { DocUuid = "123", Indexed = true, Message = "", ImageId = 123 },
                   new PostindexDocumentResponse { DocUuid = "321", Indexed = true, Message = "", ImageId = 321 },
               }
           });

        _documentFactoryProviderMock
           .Setup(x => x.GetFactory(It.IsAny<DocumentType>()))
           .Returns(new RetirementDocumentFactory());

        var action = async () => await _sut.UploadGenericRetirementDocuments(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), "UnsupportedJourneyType", It.IsAny<MemoryStream>(), "summary.pdf");

        await action.Should().ThrowAsync<ArgumentException>().WithMessage("Unsupported Journey Type or Quote Request Case Type. Journey Type: UnsupportedJourneyType. Quote Request Case Type: null. ");
    }

    public async Task UploadDBRetirementDocumentReturnsError_WhenEdmdFailsToUploadSummaryDocument()
    {
        _edmsClientMock
            .Setup(x => x.UploadDocument(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(new DocumentUploadError { Message = "Error" });

        var result = await _sut.UploadDBRetirementDocument(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>(), It.IsAny<string>());

        result.Left().Message.Should().Be("DB Retirement documents upload process failed.");

        _edmsClientMock.Verify(x => x.UploadDocument(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), null), Times.Once);
        _edmsClientMock.Verify(x => x.PostindexDocuments(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<UploadedDocument>>()), Times.Never);
    }


    public async Task UploadDBRetirementDocumentReturnsError_WhenEdmdFailsToPostIndexIndividualDocument()
    {
        _edmsClientMock
            .Setup(x => x.UploadDocument(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(new DocumentUploadResponse { Uuid = It.IsAny<string>() });

        _edmsClientMock
           .Setup(x => x.PostindexDocuments(
               It.IsAny<string>(),
               It.IsAny<string>(),
               It.IsAny<string>(),
               It.IsAny<IList<UploadedDocument>>()))
           .ReturnsAsync(new PostindexDocumentsResponse
           {
               Documents = new List<PostindexDocumentResponse> { new PostindexDocumentResponse { DocUuid = "123", Indexed = false, Message = "Failed", ImageId = 123 } }
           });

        var result = await _sut.UploadDBRetirementDocument(It.IsAny<string>(), It.IsAny<string>(), "7654321", It.IsAny<MemoryStream>(), It.IsAny<string>());

        result.Left().Message.Should().Be("Case number: 7654321. Failed to post index documents: Documents ids: 123.Error: Failed.");
    }

    public async Task UploadDBRetirementDocumentReturnsError_WhenEdmdFailsToPostIndexDocuments()
    {
        _edmsClientMock
            .Setup(x => x.UploadDocument(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(new DocumentUploadResponse { Uuid = It.IsAny<string>() });

        _uploadedDocumentFactoryMock
            .Setup(x => x.CreateOutgoing(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()))
            .Returns(new UploadedDocumentsBuilder().Build());

        _edmsClientMock
           .Setup(x => x.PostindexDocuments(
               It.IsAny<string>(),
               It.IsAny<string>(),
               It.IsAny<string>(),
               It.IsAny<IList<UploadedDocument>>()))
           .ReturnsAsync(new PostIndexError { Message = "PostIndex failed.", Documents = new List<PostindexDocumentResponse>() });

        var result = await _sut.UploadDBRetirementDocument(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>(), It.IsAny<string>());

        result.Left().Message.Should().Be("Failed to post index documents.");

        _edmsClientMock.Verify(x => x.UploadDocument(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), null), Times.Once);
        _edmsClientMock.Verify(x => x.PostindexDocuments(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<UploadedDocument>>()), Times.Once);
    }

    public async Task UploadDBRetirementDocumentSucceeds_WithoutGbgId()
    {
        _edmsClientMock
            .Setup(x => x.UploadDocument(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(new DocumentUploadResponse { Uuid = "321" });

        _documentsRepositoryMock.Setup(x => x.NextId()).ReturnsAsync(1);
        _edmsClientMock
           .Setup(x => x.PostindexDocuments(
               It.IsAny<string>(),
               It.IsAny<string>(),
               It.IsAny<string>(),
               It.IsAny<IList<UploadedDocument>>()))
           .ReturnsAsync(new PostindexDocumentsResponse
           {
               Documents = new List<PostindexDocumentResponse>
               {
                   new PostindexDocumentResponse { DocUuid = "321", Indexed = true, Message = "", ImageId = 321 }
               }
           });

        _documentFactoryProviderMock
           .Setup(x => x.GetFactory(It.IsAny<DocumentType>()))
           .Returns(new RetirementDocumentFactory());

        var result = await _sut.UploadDBRetirementDocument(It.IsAny<string>(), It.IsAny<string>(), "CASE1", It.IsAny<MemoryStream>(), It.IsAny<string>());

        result.IsRight.Should().BeTrue();
        result.Right().Should().Be(321);
        _documentsRepositoryMock.Verify(x => x.Add(It.IsAny<Document>()), Times.Once);
        _memberDbUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
        _documentsRepositoryMock.Verify(x => x.NextId(), Times.Once);

        _edmsClientMock.Verify(x => x.UploadDocument(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), null), Times.Once);
        _edmsClientMock.Verify(x => x.PostindexDocuments(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<UploadedDocument>>()), Times.Once);
    }

    public async Task UploadsNonCaseRetirementQuoteDocument()
    {
        _edmsClientMock
            .Setup(x => x.UploadDocument(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(new DocumentUploadResponse { Uuid = "321" });

        _uploadedDocumentFactoryMock
            .Setup(x => x.CreateIncoming(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()))
            .Returns(new UploadedDocumentsBuilder().Build());

        _edmsClientMock
           .Setup(x => x.IndexNonCaseDocuments(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IList<UploadedDocument>>()))
           .ReturnsAsync(new PostindexDocumentsResponse
           {
               Documents = new List<PostindexDocumentResponse>
               {
                   new PostindexDocumentResponse { DocUuid = "321", Indexed = true, Message = "", ImageId = 321 }
               }
           });

        _documentsRepositoryMock.Setup(x => x.NextId()).ReturnsAsync(1);
        _documentFactoryProviderMock
           .Setup(x => x.GetFactory(It.IsAny<DocumentType>()))
           .Returns(new RetirementQuoteWithoutCaseDocumantFactory());
        var calcHistory = new CalculationHistory("0003994", "RBS", "someEvent", 1, null, null);
        _calculationHistoryRepository
            .Setup(x => x.FindLatest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Int32>()))
            .ReturnsAsync(calcHistory);

        await _sut.UploadNonCaseRetirementQuoteDocument("WPS", "123456", It.IsAny<MemoryStream>(), It.IsAny<Int32>());

        _edmsClientMock.Verify(x => x.UploadDocument("WPS", "Retirement quote", It.IsAny<Stream>(), It.IsAny<int?>()), Times.Once);
        _uploadedDocumentFactoryMock.Verify(x => x.CreateIncoming("123456", "WPS", "Retirement quote", "321", false, "RETQU"), Times.Once);
        _edmsClientMock.Verify(x => x.IndexNonCaseDocuments("WPS", "123456", It.IsAny<IList<UploadedDocument>>()), Times.Once);
        _documentsRepositoryMock.Verify(x => x.NextId(), Times.Once);
        _documentsRepositoryMock.Verify(x => x.Add(It.IsAny<Document>()), Times.Once);
        _memberDbUnitOfWorkMock.Verify(x => x.Commit(), Times.AtLeast(2));
        calcHistory.ImageId.Should().Be(321);
        calcHistory.FileId.Should().BeNull();
    }

    public async Task UploadNonCaseRetirementQuoteDocumentThrows_WhenUploadToEdmsreturnsError()
    {
        _calculationHistoryRepository
            .Setup(x => x.FindLatest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Int32>()))
            .ReturnsAsync(Option<CalculationHistory>.Some(new CalculationHistory("0003994", "RBS", "someEvent", 1, null, null)));

        _edmsClientMock
            .Setup(x => x.UploadDocument(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MemoryStream>(), null))
            .ReturnsAsync(new DocumentUploadError { Message = "SomeError" });

        var action = async () => await _sut.UploadNonCaseRetirementQuoteDocument("WPS", "123456", It.IsAny<MemoryStream>(), 1);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Either does not have right value");
    }

    public async Task UploadNonCaseRetirementQuoteDocumentThrows_WhenIndexNonCasedDocumnetReturnsError()
    {
        _calculationHistoryRepository
            .Setup(x => x.FindLatest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Int32>()))
            .ReturnsAsync(Option<CalculationHistory>.Some(new CalculationHistory("0003994", "RBS", "someEvent", 1, null, null)));

        _edmsClientMock
             .Setup(x => x.UploadDocument(
                 It.IsAny<string>(),
                 It.IsAny<string>(),
                 It.IsAny<MemoryStream>(), null))
             .ReturnsAsync(new DocumentUploadResponse { Uuid = "321" });

        _edmsClientMock
           .Setup(x => x.IndexNonCaseDocuments(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IList<UploadedDocument>>()))
           .ReturnsAsync(new PostIndexError { Message = "Non cased Document Index failed.", Documents = new List<PostindexDocumentResponse>() });

        var action = async () => await _sut.UploadNonCaseRetirementQuoteDocument("WPS", "123456", It.IsAny<MemoryStream>(), 1);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Either does not have right value");
    }
}