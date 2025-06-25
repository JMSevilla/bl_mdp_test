using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using WTW.MdpService.Domain.Common;
using WTW.MdpService.Domain.Common.UploadedDocuments;
using WTW.MdpService.Infrastructure.Consumers;
using WTW.MdpService.Infrastructure.Edms;
using WTW.MdpService.Test.Infrastructure.Edms;
using WTW.MessageBroker.Contracts.Mdp;
using WTW.TestCommon.Helpers;
using TestData = WTW.TestCommon.TestData;

namespace WTW.MdpService.Test.Infrastructure.Consumers;

public class EdmsUploadConsumerTest
{
    protected readonly EdmsUploadConsumer _sut;
    protected readonly Mock<ILogger<EdmsUploadConsumer>> _loggerMock;
    protected readonly Mock<IEdmsClient> _mockEdmsClient;
    protected readonly Mock<IUploadedDocumentFactory> _mockUploadDocFactory;
    protected readonly Mock<ConsumeContext<EdmsUpload>> _mockContext;

    public EdmsUploadConsumerTest()
    {
        _loggerMock = new Mock<ILogger<EdmsUploadConsumer>>();
        _mockEdmsClient = new Mock<IEdmsClient>();
        _mockUploadDocFactory = new Mock<IUploadedDocumentFactory>();
        _mockContext = new Mock<ConsumeContext<EdmsUpload>>();
        _mockContext.SetupGet(x => x.CorrelationId).Returns(Guid.NewGuid());
        _mockContext.SetupGet(x => x.Message).Returns(new EdmsUpload()
        {
            Bgroup = TestData.Bgroup,
            Refno = TestData.RefNo,
            EventType = MdpEvent.SingleAuthRegistration,
            File = TestData.PdfDataBase64
        });
        _sut = new EdmsUploadConsumer(_loggerMock.Object, _mockEdmsClient.Object, _mockUploadDocFactory.Object);
    }
}

public class EdmsUploadConsumerValidTest : EdmsUploadConsumerTest
{
    public async Task WhenEventTypeIsSingleAuthRegistration_ThenUploadDocumentAndExpectedLoggingOccurred()
    {
        _mockEdmsClient.Setup(x => x.UploadDocumentBase64(TestData.Bgroup, It.IsAny<string>(), TestData.PdfDataBase64, null))
            .ReturnsAsync(new DocumentUploadResponse() { Uuid = TestData.Uuid });
        _mockEdmsClient.Setup(x => x.IndexNonCaseDocuments(TestData.Bgroup, TestData.RefNo, It.IsAny<List<UploadedDocument>>()))
            .ReturnsAsync(PostIndexDocumentsResponseFactory.Create());

        await _sut.Consume(_mockContext.Object);

        _loggerMock.VerifyLogging($"{nameof(EdmsUploadConsumer)} Execution started", LogLevel.Information);
        _loggerMock.VerifyLogging($"{nameof(EdmsUploadConsumer.UploadDocument)} document uploaded successful with id - {TestData.Uuid}", LogLevel.Information);
        _loggerMock.VerifyLogging($"{nameof(EdmsUploadConsumer.UploadDocument)} document indexed successful with image id - {TestData.ImageId}", LogLevel.Information);
        _loggerMock.VerifyLogging($"{nameof(EdmsUploadConsumer)} Execution successful", LogLevel.Information);
    }
}

public class EdmsUploadConsumerInValidTest : EdmsUploadConsumerTest
{
    public async Task WhenEventTypeNotConfiguredForEdmsConsumer_ThenDoNotProcessMessageExpectedLoggingOccurred()
    {
        _mockContext.SetupGet(x => x.Message).Returns(new EdmsUpload()
        {
            Bgroup = TestData.Bgroup,
            Refno = TestData.RefNo,
            EventType = MdpEvent.None
        });

        await _sut.Consume(_mockContext.Object);

        _loggerMock.VerifyLogging($"{nameof(EdmsUploadConsumer)} Execution started", LogLevel.Information);
        _loggerMock.VerifyLogging($"{nameof(EdmsUploadConsumer)} Invalid event type - {MdpEvent.None}", LogLevel.Error);

    }

    public async Task WhenEdmsUploadFails_ThenThrowExceptionAndExpectedLoggingOccurred()
    {
        var error = $"document upload failed with message - {TestData.ServerError}";
        _mockEdmsClient.Setup(x => x.UploadDocumentBase64(TestData.Bgroup, It.IsAny<string>(), TestData.PdfDataBase64, null))
            .ReturnsAsync(new DocumentUploadError() { Message = TestData.ServerError });
        var act = () => _sut.Consume(_mockContext.Object);

        await act.Should().ThrowAsync<MdpConsumerException>()
            .WithMessage(error);

        _loggerMock.VerifyLogging($"{nameof(EdmsUploadConsumer)} Execution started", LogLevel.Information);
        _loggerMock.VerifyLogging($"{nameof(EdmsUploadConsumer)} {error}", LogLevel.Error);
    }

    public async Task WhenEdmsIndexFails_ThenThrowExceptionAndExpectedLoggingOccurred()
    {
        _mockEdmsClient.Setup(x => x.UploadDocumentBase64(TestData.Bgroup, It.IsAny<string>(), TestData.PdfDataBase64, null))
           .ReturnsAsync(new DocumentUploadResponse() { Uuid = TestData.Uuid });
        var error = $"document index failed with message - {TestData.ServerError}";
        _mockEdmsClient.Setup(x => x.IndexNonCaseDocuments(TestData.Bgroup, TestData.RefNo, It.IsAny<List<UploadedDocument>>()))
           .ReturnsAsync(new PostIndexError() { Message = TestData.ServerError });
        var act = () => _sut.Consume(_mockContext.Object);

        await act.Should().ThrowAsync<MdpConsumerException>()
            .WithMessage(error);

        _loggerMock.VerifyLogging($"{nameof(EdmsUploadConsumer)} Execution started", LogLevel.Information);
        _loggerMock.VerifyLogging($"{nameof(EdmsUploadConsumer)} {error}", LogLevel.Error);
    }
}
