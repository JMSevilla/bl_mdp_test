using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using WTW.MdpService.Domain.Common;
using WTW.MdpService.Domain.Members;
using WTW.MdpService.Infrastructure.Edms;
using WTW.TestCommon;
using WTW.Web.LanguageExt;

namespace WTW.MdpService.Test.Infrastructure.Edms;

public class EdmsClientTest
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<EdmsClient>> _mockLogger;
    private readonly EdmsClient _edmsClient;
    private readonly string _userName = "testUser";
    private readonly string _password = "testPassword";

    public EdmsClientTest()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpMessageHandlerMock.Protected()
          .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/v1/login")),
              ItExpr.IsAny<CancellationToken>()
          )
          .ReturnsAsync(new HttpResponseMessage
          {
              StatusCode = HttpStatusCode.OK,
              Content = new StringContent("{\"access_token\":\"test_access_token\"}")
          });
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://api.example.com")
        };
        _mockLogger = new Mock<ILogger<EdmsClient>>();
        _edmsClient = new EdmsClient(_httpClient, _userName, _password, _mockLogger.Object);
    }

    public async Task GetDocumentOrError_ShouldReturnDocument_WhenDocumentExists()
    {
        var documentId = 1;
        var documentContent = "Document content";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(documentContent));

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/v1/images")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StreamContent(stream)
            });

        var result = await _edmsClient.GetDocumentOrError(documentId);

        result.IsRight.Should().BeTrue();
        result.Right().Should().NotBeNull();
    }

    public async Task GetDocumentOrError_ReturnsError()
    {
        var documentId = 1;
        var result = await _edmsClient.GetDocumentOrError(documentId);

        result.IsRight.Should().BeFalse();
        result.Left().Message.Should().Be("Handler did not return a response message.");
    }

    public async Task GetDocuments()
    {
        var documentContent = "Document content";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(documentContent));

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/v1/images/1")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StreamContent(stream)
            });

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/v1/images/2")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StreamContent(stream)
            });

        var now = DateTimeOffset.UtcNow;
        var documents = new List<Document>();
        documents.Add(new Document("RBS", "0003994", "pdf", now, "summary", "summary.pdf", 1, 1, "123456", "testScheme"));
        documents.Add(new Document("RBS", "0003994", "pdf", now, "summary", "summary.pdf", 2, 2, "123456", "testScheme"));

        var result = await _edmsClient.GetDocuments(documents);

        result.Should().HaveCount(2);
        result.Should().Contain(x => x.Id == 1);
        result.Should().Contain(x => x.Id == 2);
        result.Should().NotContain(x => x.Id == 3);
        result.Should().NotContain(x => x.Id == 0);
    }

    public async Task UploadDocument_ReturnsSuccessfulResponse()
    {
        var businessGroup = "TestGroup";
        var fileName = "test.txt";
        var content = "File content";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("v1/document/bgroup")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"docUuid\":\"test-docUuid\",\"message\":\"test-message\"}")
            });


        var result = await _edmsClient.UploadDocument(businessGroup, fileName, stream);

        result.IsRight.Should().BeTrue();
        result.Right().Uuid.Should().Be("test-docUuid");
        result.Right().Message.Should().Be("test-message");
    }

    public async Task UploadDocument_ReturnsErrorResponse()
    {
        var businessGroup = "TestGroup";
        var fileName = "test.txt";
        var content = "File content";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("v1/document/bgroup")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"message\":\"test-error-message\"}")
            });

        var result = await _edmsClient.UploadDocument(businessGroup, fileName, stream);

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be("test-error-message");
    }

    public async Task PreindexDocument_ReturnsSuccessfulResponse()
    {
        var businessGroup = "TestGroup";
        var referenceNumber = "TestRefnop";
        var client = "testClient";
        var content = "File content";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("v1/preinde")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"imageid\":222,\"message\":\"test-message\",\"batchno\":333}")
            });

        var result = await _edmsClient.PreindexDocument(businessGroup, referenceNumber, client, stream, 123456);

        result.IsRight.Should().BeTrue();
        result.Right().ImageId.Should().Be(222);
        result.Right().BatchNumber.Should().Be(333);
        result.Right().Message.Should().Be("test-message");
    }

    public async Task PreindexDocument_ReturnsErrorResponse()
    {
        var businessGroup = "TestGroup";
        var referenceNumber = "TestRefnop";
        var client = "testClient";
        var content = "File content";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("v1/preinde")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"message\":\"test-error-message\",\"documents\":[{\"docUuid\":\"test-docUuid\",\"message\":\"test-message\",\"indexed\":false,\"imageId\":444}]}")
            });

        var result = await _edmsClient.PreindexDocument(businessGroup, referenceNumber, client, stream, 123456);

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be("test-error-message");
        result.Left().Documents.Single().ImageId.Should().Be(444);
        result.Left().Documents.Single().DocUuid.Should().Be("test-docUuid");
        result.Left().Documents.Single().Indexed.Should().BeFalse();
        result.Left().Documents.Single().Message.Should().Be("test-message");
    }

    public async Task PostindexDocuments_ReturnsSuccessfulResponse()
    {
        var businessGroup = "TestGroup";
        var referenceNumber = "Ref123";
        var caseNumber = "Case456";
        var documents = new List<UploadedDocument>
            {
                new UploadedDocument(referenceNumber, businessGroup, "test-type", null, "test1.txt", "tets-Uuid", DocumentSource.Incoming, true, "tag1", "tag2"),
                new UploadedDocument(referenceNumber, businessGroup, "test-type", null, "test1.txt", "tets-Uuid", DocumentSource.Incoming, true, null),
            };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("v1/document-index/member-case")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"documents\":[{\"docUuid\":\"test-docUuid\",\"message\":\"test-message\",\"indexed\":false,\"imageId\":444}]}")
            });

        var result = await _edmsClient.PostindexDocuments(businessGroup, referenceNumber, caseNumber, documents);

        result.IsRight.Should().BeTrue();
        result.Right().Documents.Single().ImageId.Should().Be(444);
        result.Right().Documents.Single().DocUuid.Should().Be("test-docUuid");
        result.Right().Documents.Single().Indexed.Should().BeFalse();
        result.Right().Documents.Single().Message.Should().Be("test-message");
    }

    public async Task PostindexDocuments_ReturnsErrorResponse()
    {
        var businessGroup = "TestGroup";
        var referenceNumber = "Ref123";
        var caseNumber = "Case456";
        var documents = new List<UploadedDocument>
            {
                new UploadedDocument(referenceNumber, businessGroup, "test-type", null, "test1.txt", "tets-Uuid", DocumentSource.Incoming, true, "tag1", "tag2"),
                new UploadedDocument(referenceNumber, businessGroup, "test-type", null, "test1.txt", "tets-Uuid", DocumentSource.Incoming, true, null),
            };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("v1/document-index/member-case")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"message\":\"test-error-message\"}")
            });

        var result = await _edmsClient.PostindexDocuments(businessGroup, referenceNumber, caseNumber, documents);

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be("test-error-message");
    }

    public async Task PostIndexBereavementDocuments_ReturnsSuccessfulResponse()
    {
        var businessGroup = "TestGroup";
        var referenceNumber = "Ref123";
        var caseNumber = "Case456";
        var documents = new List<UploadedDocument>
            {
                new UploadedDocument(referenceNumber, businessGroup, "test-type", null, "test1.txt", "tets-Uuid", DocumentSource.Incoming, true, "tag1", "tag2"),
                new UploadedDocument(referenceNumber, businessGroup, "test-type", null, "test1.txt", "tets-Uuid", DocumentSource.Incoming, true, null),
            };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("v1/document-index/non-member")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"documents\":[{\"docUuid\":\"test-docUuid\",\"message\":\"test-message\",\"indexed\":false,\"imageId\":444}]}")
            });

        var result = await _edmsClient.PostIndexBereavementDocuments(businessGroup, caseNumber, documents);

        result.IsRight.Should().BeTrue();
        result.Right().Documents.Single().ImageId.Should().Be(444);
        result.Right().Documents.Single().DocUuid.Should().Be("test-docUuid");
        result.Right().Documents.Single().Indexed.Should().BeFalse();
        result.Right().Documents.Single().Message.Should().Be("test-message");
    }

    public async Task PostIndexBereavementDocuments_ReturnsErrorResponse()
    {
        var businessGroup = "TestGroup";
        var referenceNumber = "Ref123";
        var caseNumber = "Case456";
        var documents = new List<UploadedDocument>
            {
                new UploadedDocument(referenceNumber, businessGroup, "test-type", null, "test1.txt", "tets-Uuid", DocumentSource.Incoming, true, "tag1", "tag2"),
                new UploadedDocument(referenceNumber, businessGroup, "test-type", null, "test1.txt", "tets-Uuid", DocumentSource.Incoming, true, null),
            };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("v1/document-index/non-member")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"message\":\"test-error-message\"}")
            });

        var result = await _edmsClient.PostIndexBereavementDocuments(businessGroup, caseNumber, documents);

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be("test-error-message");
    }

    public async Task IndexRetirementDocument_ReturnsSuccessfulResponse()
    {
        var businessGroup = "TestGroup";
        var referenceNumber = "Ref123";
        var caseNumber = "Case456";
        var batchNumber = 123456;
        var caseCode = "case123";

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("v1/postindex")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"message\":\"test-message\"}")
            });

        var result = await _edmsClient.IndexRetirementDocument(businessGroup, referenceNumber, batchNumber, caseNumber, caseCode);

        result.IsRight.Should().BeTrue();
        result.Right().Message.Should().Be("test-message");
    }

    public async Task IndexRetirementDocument_ReturnsErrorResponse()
    {
        var businessGroup = "TestGroup";
        var referenceNumber = "Ref123";
        var caseNumber = "Case456";
        var batchNumber = 123456;
        var caseCode = "case123";

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("v1/postindex")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"message\":\"test-error-message\"}")
            });

        var result = await _edmsClient.IndexRetirementDocument(businessGroup, referenceNumber, batchNumber, caseNumber, caseCode);

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be("test-error-message");
    }

    public async Task IndexBereavementDocument_ReturnsSuccessfulResponse()
    {
        var businessGroup = "TestGroup";
        var caseNumber = "Case456";
        var batchNumber = 123456;

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("v1/postindex")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"message\":\"test-message\"}")
            });

        var result = await _edmsClient.IndexBereavementDocument(businessGroup, batchNumber, caseNumber);

        result.IsRight.Should().BeTrue();
        result.Right().Message.Should().Be("test-message");
    }

    public async Task IndexBereavementDocument_ReturnsErrorResponse()
    {
        var businessGroup = "TestGroup";
        var caseNumber = "Case456";
        var batchNumber = 123456;

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("v1/postindex")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"message\":\"test-error-message\"}")
            });

        var result = await _edmsClient.IndexBereavementDocument(businessGroup, batchNumber, caseNumber);

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be("test-error-message");
    }

    public async Task IndexDocument_ReturnsSuccessfulResponse()
    {
        var businessGroup = "TestGroup";
        var referenceNumber = "Ref123";
        var batchNumber = 123456;

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("v1/postindex")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"message\":\"test-message\"}")
            });

        var result = await _edmsClient.IndexDocument(businessGroup, referenceNumber, batchNumber);

        result.IsRight.Should().BeTrue();
        result.Right().Message.Should().Be("test-message");
    }

    public async Task IndexDocument_ReturnsErrorResponse()
    {
        var businessGroup = "TestGroup";
        var referenceNumber = "Ref123";
        var batchNumber = 123456;

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("v1/postindex")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"message\":\"test-error-message\"}")
            });

        var result = await _edmsClient.IndexDocument(businessGroup, referenceNumber, batchNumber);

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be("test-error-message");
    }

    public async Task IndexNonCaseDocuments_ReturnsSuccessfulResponse()
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("v1/document-index/member")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(PostIndexDocumentsResponseFactory.Create())
            });
        var documents = new List<UploadedDocument>
            {
                new UploadedDocument(TestData.RefNo, TestData.Bgroup, "test-type", null, "SARegistrationEmail.pdf", "tets-Uuid", DocumentSource.Outgoing, false, "tag1", "tag2")
            };

        var result = await _edmsClient.IndexNonCaseDocuments(TestData.Bgroup, TestData.RefNo, documents);

        result.IsRight.Should().BeTrue();
        result.Right().Should().BeEquivalentTo(PostIndexDocumentsResponseFactory.Create());
    }
    public async Task IndexNonCaseDocuments_ReturnsErrorResponse()
    {
        var error = "{\"message\":\"test-error-message\"}";
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("v1/document-index/member")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent(error)
            });
        var documents = new List<UploadedDocument>
            {
                new UploadedDocument(TestData.RefNo, TestData.Bgroup, "test-type", null, "SARegistrationEmail.pdf", "tets-Uuid", DocumentSource.Outgoing, false, "tag1", "tag2")
            };

        var result = await _edmsClient.IndexNonCaseDocuments(TestData.Bgroup, TestData.RefNo, documents);

        result.IsLeft.Should().BeTrue();
        result.Left().Message.Should().Be("test-error-message");
    }
}

public static class PostIndexDocumentsResponseFactory
{
    public static PostindexDocumentsResponse Create()
    {
        return new PostindexDocumentsResponse()
        {
            Documents = new List<PostindexDocumentResponse>()
            {
                new PostindexDocumentResponse()
                {
                    ImageId = TestData.ImageId
                }
            }
        };
    }
}